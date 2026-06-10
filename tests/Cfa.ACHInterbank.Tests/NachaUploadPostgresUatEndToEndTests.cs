using System.Data;
using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Calendar;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Tests.NachaFunctional;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Npgsql;
using Quartz;
using Quartz.Simpl;

namespace Cfa.ACHInterbank.Tests;

[Trait("Category", "Postgres")]
[Trait("Category", "Integration")]
[Trait("Category", "UAT")]
public class NachaUploadPostgresUatEndToEndTests
{
    private const string UatFixtureFileName = "RRRRTTT.ZZZ.1.ach";
    private const string UatTaskCode = "IncomingNachaPostProcessing";
    private const string UatCycleId = "CYCLE-ACH-20260524-1";

    [Fact]
    public async Task Upload_ShouldPersistOperationalArtifacts_AndQuartzShouldWriteLogs()
    {
        if (!ShouldRunUat())
        {
            return;
        }

        await using var harness = await PostgresHarness.CreateAsync();
        if (harness.IsDisabled)
        {
            return;
        }

        await SeedOperationalUatGraphAsync(harness.Context);
        await harness.Context.SaveChangesAsync();

        var uploadFixture = BuildUploadFixture(harness.Context);
        var controller = new NachaUploadController(
            uploadFixture.IngestionService,
            harness.Context,
            NullLogger<NachaUploadController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = BuildUatPrincipal()
            }
        };

        var file = BuildUploadFile(NachaTestDataPaths.AchColombiaIncoming001, UatFixtureFileName);
        var uploadResult = await controller.UploadNachaFile(new NachaUploadRequest { File = file }, CancellationToken.None);

        var uploadPayload = ExtractUploadResponse(uploadResult);
        Assert.NotNull(uploadPayload);
        Assert.NotEmpty(uploadPayload.TraceId);
        var ingestionStatus = uploadPayload.IngestionStatus ?? string.Empty;
        var cycleResolutionStatus = uploadPayload.CycleResolutionStatus ?? string.Empty;
        Assert.Contains(ingestionStatus, new[]
        {
            IncomingNachaIngestionStatus.Completado.ToString(),
            IncomingNachaIngestionStatus.PendienteResolucion.ToString(),
            IncomingNachaIngestionStatus.Bloqueado.ToString()
        });
        Assert.NotEqual(IncomingNachaCycleResolutionStatus.NoIntentado.ToString(), cycleResolutionStatus);

        await using var postUploadContext = CreateContext(harness.ConnectionString);
        var ingestion = await postUploadContext.IncomingNachaFileIngestions
            .AsNoTracking()
            .SingleAsync(x => x.CorrelationId == uploadPayload.TraceId || x.FileName == UatFixtureFileName);
        var ingestionId = ingestion.Id;

        Assert.Equal(UatFixtureFileName, ingestion.FileName);
        Assert.Equal(1060, ingestion.FileSize);
        Assert.Equal("application/octet-stream", ingestion.ContentType);
        Assert.Equal(1, await postUploadContext.NachaHeaders.CountAsync(x => x.IncomingNachaFileIngestionId == ingestionId));
        Assert.Equal(1, await postUploadContext.BatchHeaders.CountAsync(x => x.NachaHeader != null && x.NachaHeader.IncomingNachaFileIngestionId == ingestionId));
        Assert.Equal(1, await postUploadContext.EntryDetails.CountAsync(x => x.NachaHeader != null && x.NachaHeader.IncomingNachaFileIngestionId == ingestionId));
        Assert.Equal(1, await postUploadContext.AddendaRecords.CountAsync(x => x.NachaHeader != null && x.NachaHeader.IncomingNachaFileIngestionId == ingestionId));
        Assert.Equal(1, await postUploadContext.BatchControls.CountAsync(x => x.NachaHeader != null && x.NachaHeader.IncomingNachaFileIngestionId == ingestionId));
        Assert.Equal(1, await postUploadContext.FileControls.CountAsync(x => x.NachaHeader != null && x.NachaHeader.IncomingNachaFileIngestionId == ingestionId));
        Assert.Equal(1, await postUploadContext.IncomingNachaEntryClassifications.CountAsync(x => x.IncomingNachaFileIngestionId == ingestionId));
        Assert.Equal(1, await postUploadContext.IncomingNachaTransactionLinks.CountAsync(x => x.IncomingNachaFileIngestionId == ingestionId));

        var queue = await postUploadContext.IncomingNachaDispatchQueue
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.IncomingNachaFileIngestionId == ingestionId);

        Assert.NotNull(queue);
        Assert.Equal(UatCycleId, queue!.AchCycleId);
        Assert.Equal(1, queue.ClearingHouseId);
        Assert.Equal(ingestionId, queue.IncomingNachaFileIngestionId);

        var quartzFixture = BuildQuartzFixture(harness.ConnectionString);
        await quartzFixture.Scheduler.Start();

        var taskDefinition = await postUploadContext.TaskDefinitions
            .AsNoTracking()
            .SingleAsync(x => x.Code == UatTaskCode);

        var job = JobBuilder.Create<NonConcurrentDynamicJob>()
            .WithIdentity($"job:{taskDefinition.Id}", "db-tasks")
            .UsingJobData("TaskId", taskDefinition.Id)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"trg:{taskDefinition.Id}", "db-tasks")
            .StartNow()
            .Build();

        await quartzFixture.Scheduler.ScheduleJob(job, trigger);

        await WaitUntilAsync(async () =>
        {
            await using var verifyContext = CreateContext(harness.ConnectionString);
            return await verifyContext.TaskExecutionLogs.AnyAsync(x => x.TaskDefinitionId == taskDefinition.Id)
                   && await verifyContext.IncomingNachaIntegrationExecution.AnyAsync(x => x.DispatchQueue.IncomingNachaFileIngestionId == ingestion.Id);
        }, TimeSpan.FromSeconds(30));

        await using var assertContext = CreateContext(harness.ConnectionString);
        var taskExecution = await assertContext.TaskExecutionLogs
            .AsNoTracking()
            .SingleAsync(x => x.TaskDefinitionId == taskDefinition.Id);
        Assert.True(taskExecution.Success);
        Assert.NotNull(taskExecution.Output);

        var integrationExecution = await assertContext.IncomingNachaIntegrationExecution
            .AsNoTracking()
            .SingleAsync(x => x.DispatchQueue.IncomingNachaFileIngestionId == ingestion.Id);
        Assert.NotEmpty(integrationExecution.MethodName);
        Assert.NotNull(integrationExecution.CorrelationId);
        Assert.NotEmpty(integrationExecution.ResponseCode);

        var processingEvents = await assertContext.IncomingNachaProcessingEvents
            .AsNoTracking()
            .Where(x => x.IncomingNachaFileIngestionId == ingestion.Id)
            .ToListAsync();
        Assert.NotEmpty(processingEvents);

        var uploadedRecord = await controller.GetUploadedRecords(null, null, null, queue.AchCycleId, null, CancellationToken.None);
        var uploadedRecordPayload = Assert.IsAssignableFrom<IReadOnlyList<NachaUploadRecordResponse>>(uploadedRecord.Value);
        Assert.Single(uploadedRecordPayload);
        Assert.Equal(queue.AchCycleId, uploadedRecordPayload[0].AchCycleId);
        Assert.Equal("ACH Colombia", uploadedRecordPayload[0].ClearingHouseName);

        await quartzFixture.Scheduler.Shutdown(waitForJobsToComplete: true);
    }

    [Fact]
    public async Task Upload_WithoutMatchingCycle_ShouldRespectTheActualResolutionState()
    {
        if (!ShouldRunUat())
        {
            return;
        }

        await using var harness = await PostgresHarness.CreateAsync();
        if (harness.IsDisabled)
        {
            return;
        }

        await SeedUatGraphWithoutCycleAsync(harness.Context);
        await harness.Context.SaveChangesAsync();

        var uploadFixture = BuildUploadFixture(harness.Context);
        var controller = new NachaUploadController(
            uploadFixture.IngestionService,
            harness.Context,
            NullLogger<NachaUploadController>.Instance);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = BuildUatPrincipal()
            }
        };

        var file = BuildUploadFile(NachaTestDataPaths.AchColombiaIncoming001, UatFixtureFileName);
        var result = await controller.UploadNachaFile(new NachaUploadRequest { File = file }, CancellationToken.None);
        var payload = ExtractUploadResponse(result);

        var ingestionStatus = payload.IngestionStatus ?? string.Empty;
        var cycleResolutionStatus = payload.CycleResolutionStatus ?? string.Empty;

        Assert.Contains(ingestionStatus, new[]
        {
            IncomingNachaIngestionStatus.PendienteResolucion.ToString(),
            IncomingNachaIngestionStatus.Bloqueado.ToString()
        });
        Assert.Contains(cycleResolutionStatus, new[]
        {
            IncomingNachaCycleResolutionStatus.NoResuelto.ToString(),
            IncomingNachaCycleResolutionStatus.Ambiguo.ToString()
        });

        await using var verifyContext = CreateContext(harness.ConnectionString);
        Assert.Equal(0, await verifyContext.IncomingNachaDispatchQueue.CountAsync());
        Assert.Equal(0, await verifyContext.TaskExecutionLogs.CountAsync());
        Assert.Equal(0, await verifyContext.IncomingNachaIntegrationExecution.CountAsync());
    }

    private static bool ShouldRunUat()
        => string.Equals(Environment.GetEnvironmentVariable("RUN_UAT_NACHA_UPLOAD"), "true", StringComparison.OrdinalIgnoreCase);

    private static UploadFixture BuildUploadFixture(AchDbContext context)
    {
        var stateTransition = new AchStateTransitionService(context);
        var cycleResolver = new IncomingNachaCycleResolver(context);
        var classifier = new IncomingNachaFunctionalClassifier();
        var linker = new IncomingNachaTransactionLinker(context);
        var prenotificationResolver = new IncomingNachaPrenotificationResolver(context);
        var dispatchPlanner = new IncomingNachaDispatchPlanner(context, new IncomingNachaDispatchEligibilityPolicy());
        var regulatoryCatalog = new AchRegulatoryCatalogService(context);
        var parser = new NachaParserService(context, NullLogger<NachaParserService>.Instance, stateTransition, regulatoryCatalog);
        var postParseProcessor = new IncomingNachaPostParseProcessor(
            context,
            classifier,
            linker,
            prenotificationResolver,
            dispatchPlanner,
            regulatoryCatalog,
            stateTransition);

        var externalPolicy = new Mock<IExternalFileNamePolicy>();
        externalPolicy
            .Setup(x => x.GenerateExternalNameAsync(It.IsAny<ExternalFileNameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalFileNameContext request, CancellationToken _) => new ExternalFileNamePolicyResult
            {
                ExternalFileName = request.ProvidedExternalFileName ?? request.InternalFileName ?? UatFixtureFileName,
                Validation = new ExternalFileNameValidationResult { Disposition = ExternalFileValidationDisposition.Passed },
                CorrelationEvidence = new ExternalFileNameCorrelationEvidence(),
                Components = new ExternalFileNameComponents { FullName = request.ProvidedExternalFileName ?? UatFixtureFileName }
            });

        var ingestion = new IncomingNachaIngestionAppService(
            context,
            cycleResolver,
            parser,
            postParseProcessor,
            externalPolicy.Object,
            NullLogger<IncomingNachaIngestionAppService>.Instance);

        return new UploadFixture(ingestion);
    }

    private static QuartzFixture BuildQuartzFixture(string connectionString)
    {
        var mapper = new Mock<IProcTransaccionesRequestMapper>();
        mapper.Setup(x => x.ResolveAsync(
                It.IsAny<IncomingNachaDispatchQueue>(),
                It.IsAny<IncomingNachaFileIngestion>(),
                It.IsAny<IncomingNachaEntryClassification>(),
                It.IsAny<AchTransaction>(),
                It.IsAny<AchCycle>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcTransaccionesRequestResolution(
                new ProcTransaccionesRequestContract(new Dictionary<string, string>
                {
                    ["TREG"] = "6",
                    ["TIPTRAN"] = "22",
                    ["MONTO"] = "100",
                    ["IDTRAN"] = "100",
                    ["IDCAMCOMPE"] = "1"
                }),
                Guid.NewGuid(),
                1,
                "uat-mapper-hash"));
        mapper.Setup(x => x.BuildSoapBody(It.IsAny<ProcTransaccionesRequestContract>()))
            .Returns("<Proc_Transacciones><IDTRAN>100</IDTRAN></Proc_Transacciones>");

        var soapClient = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AchDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IIncomingNachaPostProcessingOrchestrator>(sp =>
            new IncomingNachaPostProcessingOrchestrator(
                sp.GetRequiredService<AchDbContext>(),
                mapper.Object,
                new ProcTransaccionesResponseParser(),
                soapClient.Object,
                Options.Create(new IncomingNachaDispatchResilienceOptions { MaxAttempts = 1 }),
                Options.Create(new ProcTransaccionesDispatchOptions { Mode = "Disabled" })));
        services.AddTransient<ITaskHandler, IncomingNachaPostProcessingHandler>();
        services.AddSingleton<QuartzTaskCalendarEvaluator>();
        services.AddTransient<DynamicJobExecutor>();
        services.AddTransient<DynamicJob>();
        services.AddTransient<NonConcurrentDynamicJob>();
        services.AddQuartz(q =>
        {
            q.UseJobFactory<MicrosoftDependencyInjectionJobFactory>();
        });

        var provider = services.BuildServiceProvider();
        return new QuartzFixture(provider, provider.GetRequiredService<ISchedulerFactory>().GetScheduler().GetAwaiter().GetResult(), soapClient);
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        throw new TimeoutException("No se obtuvo evidencia Quartz/UAT dentro de la ventana acotada.");
    }

    private static DefaultHttpContext BuildUatHttpContext()
    {
        var context = new DefaultHttpContext();
        context.User = BuildUatPrincipal();
        return context;
    }

    private static System.Security.Claims.ClaimsPrincipal BuildUatPrincipal()
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.Name, "uat.nacha.upload"),
                new Claim("permission", "CanManageAch"),
                new Claim("permission", "CanReadAch"),
                new Claim(ClaimTypes.Role, "Admin")
            },
            authenticationType: "UAT");

        return new ClaimsPrincipal(identity);
    }

    private static IFormFile BuildUploadFile(string path, string fileName)
    {
        var bytes = File.ReadAllBytes(path);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };
    }

    private static NachaUploadResponseDto ExtractUploadResponse(IActionResult result)
    {
        return result switch
        {
            OkObjectResult ok => Assert.IsType<NachaUploadResponseDto>(ok.Value),
            UnprocessableEntityObjectResult unprocessable => Assert.IsType<NachaUploadResponseDto>(unprocessable.Value),
            ObjectResult objectResult when objectResult.Value is NachaUploadResponseDto dto => dto,
            _ => throw new Xunit.Sdk.XunitException($"Unexpected result type: {result.GetType().Name}")
        };
    }

    private static async Task SeedOperationalUatGraphAsync(AchDbContext context)
    {
        await SeedCommonReferenceDataAsync(context);

        if (!await context.AchCycles.AnyAsync(x => x.Id == UatCycleId))
        {
            context.AchCycles.Add(new AchCycle
            {
                Id = UatCycleId,
                CycleName = "Ciclo 1 UAT",
                ProcessingDate = new DateTime(2026, 05, 24),
                CutoffTime = new TimeSpan(12, 0, 0),
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                RescheduleOnHoliday = false,
                ClearingHouseId = 1
            });
        }

        SeedOperationalTransactionGraph(context, UatCycleId);
        SeedQuartzTask(context);
    }

    private static async Task SeedUatGraphWithoutCycleAsync(AchDbContext context)
    {
        await SeedCommonReferenceDataAsync(context);
        SeedOperationalTransactionGraph(context, "CYCLE-ACH-20260524-1");
        SeedQuartzTask(context);

        var cycles = await context.AchCycles.Where(x => x.Id == UatCycleId).ToListAsync();
        context.AchCycles.RemoveRange(cycles);
    }

    private static void SeedOperationalTransactionGraph(AchDbContext context, string cycleId)
    {
        if (!context.AchBatches.Any(x => x.AchCycleId == cycleId))
        {
            context.AchBatches.Add(new AchBatch
            {
                Id = 900,
                AchCycleId = cycleId,
                ServiceClassCode = "220",
                CompanyName = "EMPRESA UAT",
                CompanyIdentification = "1234567800",
                CompanyEntryDescription = "PAGOS",
                CompanyEntryDescriptionId = 1,
                OriginOrOdfi = "12345678",
                EffectiveEntryDate = new DateTime(2026, 05, 24),
                BatchSequenceNumber = 1,
                TotalDebitAmount = 100m,
                TotalCreditAmount = 0m
            });
        }

        if (!context.AchTransactions.Any(x => x.Id == 100))
        {
            context.AchTransactions.Add(new AchTransaction
            {
                Id = 100,
                Amount = 100m,
                TransactionExternalId = "TX-100",
                Reference = "REF-100",
                Type = TransactionTypeEnum.Credit,
                TransactionCode = "22",
                ServiceClassCode = "220",
                CompanyEntryDescriptionId = 1,
                CompanyName = "EMPRESA UAT",
                CompanyIdentification = "1234567800",
                OriginatingDFI = "12345678",
                ReceivingDFI = "876543210",
                TraceNumber = "8765432100000100",
                TraceSequenceNumber = 100,
                EffectiveEntryDate = new DateTime(2026, 05, 24),
                AddendaRecordIndicator = true,
                IsPrenotification = false,
                State = AchTransferStateEnum.Pending,
                StateChangedAtUtc = new DateTime(2026, 05, 24, 12, 0, 0, DateTimeKind.Utc),
                SourceAccountNumber = "000123456789",
                DestinationAccountNumber = "000987654321",
                SourceInstitutionId = 1,
                DestinationInstitutionId = 2,
                AchCycleId = cycleId,
                AchBatchId = 900,
                RecipientIdNumber = "900000001",
                OriginalTraceRef = "8765432100000100",
                DiscretionaryData = "UAT"
            });
        }
    }

    private static async Task SeedCommonReferenceDataAsync(AchDbContext context)
    {
        if (!await context.ClearingHouseConfigs.AnyAsync())
        {
            context.ClearingHouseConfigs.Add(new ClearingHouseConfig
            {
                Id = 1,
                ClearingHouseId = 1,
                HolidayStrategy = "Colombian"
            });
        }

        if (!await context.ClearingHouses.AnyAsync())
        {
            context.ClearingHouses.Add(new ClearingHouse
            {
                Id = 1,
                Name = "ACH Colombia",
                Code = "ACH",
                OriginCode = "12345678",
                ClearingHouseId = 1
            });
        }

        if (!await context.CompanyEntryDescriptionCatalogs.AnyAsync())
        {
            context.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog
            {
                Id = 1,
                Term = "PAGOS",
                Description = "Pagos UAT",
                StandardEntryClassCode = "PPD",
                IsActive = true
            });
        }

        if (!await context.FinancialInstitutions.AnyAsync())
        {
            var source = new FinancialInstitution
            {
                Id = 1,
                Name = "Banco Origen UAT",
                RoutingNumber = "1234567",
                TransitCode = "8",
                IsDefaultSource = true,
                Status = FinancialInstitutionStatus.Active
            };
            source.CalculateCheckDigit();

            var destination = new FinancialInstitution
            {
                Id = 2,
                Name = "Banco Destino UAT",
                RoutingNumber = "8765432",
                TransitCode = "1",
                IsDefaultSource = false,
                Status = FinancialInstitutionStatus.Active
            };
            destination.CalculateCheckDigit();

            context.FinancialInstitutions.AddRange(source, destination);
        }

        if (!await context.Customers.AnyAsync())
        {
            var customer = new Customer
            {
                FirstName = "Cliente",
                LastName = "UAT",
                DocumentType = "CC",
                DocumentNumber = "900000001",
                PersonType = "NAT",
                Gender = "N"
            };
            context.Customers.Add(customer);
            context.CustomerAccounts.Add(new CustomerAccount { Customer = customer, AccountNumber = "000987654321" });

            var paddedCustomer = new Customer
            {
                FirstName = "Cliente",
                LastName = "UAT Padding",
                DocumentType = "CC",
                DocumentNumber = "      900000001",
                PersonType = "NAT",
                Gender = "N"
            };
            context.Customers.Add(paddedCustomer);
            context.CustomerAccounts.Add(new CustomerAccount { Customer = paddedCustomer, AccountNumber = "000987654321" });
        }

        if (!await context.AchFileRejectionCodes.AnyAsync())
        {
            context.AchFileRejectionCodes.AddRange(
                new AchFileRejectionCode { Code = "D01", Description = "Duplicado", IsActive = true, AppliesToStage = "Integration", IsRetryable = false },
                new AchFileRejectionCode { Code = "D02", Description = "Padding invalido", IsActive = true, AppliesToStage = "Integration", IsRetryable = false },
                new AchFileRejectionCode { Code = "D04", Description = "Conteo invalido", IsActive = true, AppliesToStage = "Integration", IsRetryable = false },
                new AchFileRejectionCode { Code = "D05", Description = "Hash invalido", IsActive = true, AppliesToStage = "Integration", IsRetryable = false });
        }
    }

    private static void SeedQuartzTask(AchDbContext context)
    {
        if (!context.TaskDefinitions.Any(x => x.Code == UatTaskCode))
        {
            context.TaskDefinitions.Add(new TaskDefinition
            {
                Id = 901,
                Code = UatTaskCode,
                Name = "UAT Incoming NACHA Post Processing",
                PeriodicityType = PeriodicityTypeEnum.EveryNMinutes,
                N = 3,
                TimeZoneId = "America/Bogota",
                StartAt = new DateTimeOffset(2026, 05, 24, 0, 0, 0, TimeSpan.Zero),
                Status = TaskStatusEnum.Enabled,
                ConcurrencyPolicy = ConcurrencyPolicyEnum.SkipIfRunning
            });
        }

        if (!context.TaskParameters.Any(x => x.TaskDefinitionId == 901 && x.Key == "ChunkSize"))
        {
            context.TaskParameters.Add(new TaskParameter
            {
                TaskDefinitionId = 901,
                Key = "ChunkSize",
                Value = "100"
            });
        }
    }

    private static AchDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new AchDbContext(options);
    }

    private sealed record UploadFixture(IIncomingNachaIngestionAppService IngestionService);

    private sealed record QuartzFixture(ServiceProvider Provider, IScheduler Scheduler, Mock<IWscfaachSoapClient> SoapClient) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            Provider.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class PostgresHarness : IAsyncDisposable
    {
        private readonly NpgsqlConnection _adminConnection;
        private readonly string _schemaName;
        private readonly AchDbContext? _context;

        private PostgresHarness(string connectionString, NpgsqlConnection adminConnection, string schemaName, AchDbContext? context = null)
        {
            ConnectionString = connectionString;
            _adminConnection = adminConnection;
            _schemaName = schemaName;
            _context = context;
        }

        public bool IsDisabled { get; private set; }
        public string ConnectionString { get; }
        public AchDbContext Context => _context ?? throw new InvalidOperationException("Postgres harness is disabled in this environment.");

        public static async Task<PostgresHarness> CreateAsync()
        {
            var cs = Environment.GetEnvironmentVariable("POSTGRES_TEST_CONNECTION_STRING")
                     ?? Environment.GetEnvironmentVariable("ConnectionStrings__PostgresConnection");

            if (string.IsNullOrWhiteSpace(cs))
            {
                if (ShouldRunUat())
                {
                    throw new InvalidOperationException("RUN_UAT_NACHA_UPLOAD=true pero no se proporcionó una cadena de conexión PostgreSQL. Define POSTGRES_TEST_CONNECTION_STRING o ConnectionStrings__PostgresConnection.");
                }

                return new PostgresHarness(string.Empty, new NpgsqlConnection(), string.Empty) { IsDisabled = true };
            }

            var adminConnection = new NpgsqlConnection(cs);
            await adminConnection.OpenAsync();

            var schemaName = $"it_{Guid.NewGuid():N}";
            await using (var cmd = adminConnection.CreateCommand())
            {
                cmd.CommandText = $"CREATE SCHEMA IF NOT EXISTS \"{schemaName}\";";
                await cmd.ExecuteNonQueryAsync();
            }

            var builder = new NpgsqlConnectionStringBuilder(cs)
            {
                SearchPath = schemaName
            };

            var options = new DbContextOptionsBuilder<AchDbContext>()
                .UseNpgsql(builder.ConnectionString)
                .Options;

            var context = new AchDbContext(options);
            await context.Database.MigrateAsync();

            return new PostgresHarness(builder.ConnectionString, adminConnection, schemaName, context);
        }

        public async ValueTask DisposeAsync()
        {
            if (IsDisabled)
            {
                return;
            }

            if (_context is not null)
            {
                await _context.DisposeAsync();
            }

            if (_adminConnection.State != ConnectionState.Open)
            {
                await _adminConnection.OpenAsync();
            }

            await using var cmd = _adminConnection.CreateCommand();
            cmd.CommandText = $"DROP SCHEMA IF EXISTS \"{_schemaName}\" CASCADE;";
            await cmd.ExecuteNonQueryAsync();

            await _adminConnection.DisposeAsync();
        }
    }
}
