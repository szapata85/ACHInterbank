using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class NachaDesagregadoIntegrationMappingTests
{
    [Fact]
    public async Task MappingSourceCatalog_ShouldExpose_NachaDesagregadoSources()
    {
        await using var fixture = await Fixture.CreateAsync();

        var source = await fixture.Catalog.GetSourceCatalogAsync(null);
        var entityNames = source.Select(x => x.EntityName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var fieldPaths = source.Select(x => x.FieldPath).ToList();

        Assert.Contains(nameof(NachaHeader), entityNames);
        Assert.Contains(nameof(BatchHeader), entityNames);
        Assert.Contains(nameof(EntryDetail), entityNames);
        Assert.Contains(nameof(AddendaRecord), entityNames);
        Assert.Contains(nameof(BatchControl), entityNames);
        Assert.Contains(nameof(FileControl), entityNames);
        Assert.Contains("entryDetails.sequenceNumber", fieldPaths);
        Assert.Contains("batchHeaders.companyId", fieldPaths);
        Assert.Contains("nachaHeaders.immediateOrigin", fieldPaths);
        Assert.DoesNotContain(fieldPaths, x => x.Contains("select ", StringComparison.OrdinalIgnoreCase) || x.Contains(" from ", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(fieldPaths, x => x.Contains("password", StringComparison.OrdinalIgnoreCase) || x.Contains("token", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ProcTransacciones_ShouldMapFields_FromNachaDesagregado()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PublishProcTransaccionesMappingAsync();

        var mapper = new ProcTransaccionesRequestMapper(fixture.Context);

        var resolution = await mapper.ResolveAsync(
            fixture.Queue,
            fixture.Ingestion,
            fixture.Classification,
            fixture.Transaction,
            fixture.Cycle,
            new DateTime(2026, 5, 23, 10, 0, 0, DateTimeKind.Utc));

        Assert.Equal("22", resolution.Contract.Parameters["TIPTRAN"]);
        Assert.Equal("4321.5", resolution.Contract.Parameters["MONTO"]);
        Assert.Equal("999900001234567", resolution.Contract.Parameters["IDTRAN"]);
        Assert.Equal("BANCO EXTERNO UAT", resolution.Contract.Parameters["NORIG"]);
        Assert.Equal("PAGO UAT DESAGREGADO", resolution.Contract.Parameters["INFPAG"]);
        Assert.Equal("0001283", resolution.Contract.Parameters["BCORECEP"]);
        Assert.Equal("9999000", resolution.Contract.Parameters["BCOORIG"]);
        Assert.Equal("22", resolution.Contract.SourceValues!["entryDetails.transactionCode"]);
        Assert.Equal("999900001234567", resolution.Contract.SourceValues!["entryDetails.sequenceNumber"]);
    }

    [Fact]
    public async Task ProcTransacciones_ShouldResolveAddenda_BySevenDigitEntrySequenceSuffix()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PublishProcTransaccionesMappingAsync();

        fixture.Classification.AddendaRecordId = null;
        var addenda = await fixture.Context.AddendaRecords.SingleAsync(x => x.AddendaID == 602);
        addenda.EntryDetailSequenceNumber = "1234567";
        addenda.InfofromOriginator = null;
        addenda.CollectorId = "PAGO UAT DESAGREGADO";
        await fixture.Context.SaveChangesAsync();

        var mapper = new ProcTransaccionesRequestMapper(fixture.Context);
        var resolution = await mapper.ResolveAsync(
            fixture.Queue,
            fixture.Ingestion,
            fixture.Classification,
            fixture.Transaction,
            fixture.Cycle,
            new DateTime(2026, 5, 23, 10, 0, 0, DateTimeKind.Utc));

        Assert.Equal("PAGO UAT DESAGREGADO", resolution.Contract.Parameters["INFPAG"]);
    }

    [Fact]
    public async Task ProcTransacciones_ShouldPersistTrace_WithNachaSourceValues()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PublishProcTransaccionesMappingAsync();

        var mapper = new ProcTransaccionesRequestMapper(fixture.Context);
        var resolution = await mapper.ResolveAsync(
            fixture.Queue,
            fixture.Ingestion,
            fixture.Classification,
            fixture.Transaction,
            fixture.Cycle,
            new DateTime(2026, 5, 23, 10, 0, 0, DateTimeKind.Utc));

        var writer = new IntegrationMappingTraceWriter(fixture.Context, fixture.Catalog);
        var operation = new TransactionIntegrationOperationResult(
            fixture.Transaction.Id,
            fixture.Transaction.Reference,
            IntegrationGuaranteeConstants.Wscfaach,
            IntegrationGuaranteeConstants.ProcTransacciones,
            IntegrationGuaranteeConstants.MonetaryCreditRequest,
            IntegrationGuaranteeConstants.OutboundRequest,
            "Credito monetario",
            "Entidad financiera externa; CFA receptora",
            true,
            "Credito externo desde NACHA-M desagregado.",
            true,
            []);

        var result = await writer.WriteAsync(operation, resolution.Contract, fixture.Transaction.Id, fixture.Transaction.Reference, "corr-proc-transacciones", true, false);

        Assert.Empty(result.MissingRequiredFields);
        var trace = await fixture.Context.IntegrationMappingTraces.Include(x => x.Entries).SingleAsync(x => x.Id == result.TraceId);
        Assert.False(trace.ExternalTransmission);
        Assert.Contains(trace.Entries, x => x.TargetField == "TIPTRAN" && x.SourceField == "entryDetails.transactionCode" && x.SourceValueSanitized == "22");
        Assert.Contains(trace.Entries, x => x.TargetField == "IDTRAN" && x.SourceField == "entryDetails.sequenceNumber" && x.SourceValueSanitized == "999900001234567");
        Assert.Contains(trace.Entries, x => x.TargetField == "INFPAG" && x.SourceField == "addendaRecords.infofromOriginator" && x.SourceValueSanitized == "PAGO UAT DESAGREGADO");
    }

    [Fact]
    public async Task ProcTransacciones_DryRun_ShouldGenerateNonEmptySanitizedEnvelope()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PublishProcTransaccionesMappingAsync();

        var mapper = new ProcTransaccionesRequestMapper(fixture.Context);
        var resolution = await mapper.ResolveAsync(
            fixture.Queue,
            fixture.Ingestion,
            fixture.Classification,
            fixture.Transaction,
            fixture.Cycle,
            new DateTime(2026, 5, 23, 10, 0, 0, DateTimeKind.Utc));

        var envelope = mapper.BuildSoapBody(resolution.Contract);

        Assert.False(string.IsNullOrWhiteSpace(envelope));
        Assert.Contains("Proc_Transacciones", envelope);
        Assert.Contains("999900001234567", envelope);
        Assert.Contains("PAGO UAT DESAGREGADO", envelope);
        Assert.DoesNotContain("password", envelope, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bearer", envelope, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private Fixture(SqliteConnection connection, AchDbContext context)
        {
            _connection = connection;
            Context = context;
            Catalog = new IntegrationCatalogService(context);
        }

        public AchDbContext Context { get; }
        public IntegrationCatalogService Catalog { get; }
        public IncomingNachaFileIngestion Ingestion { get; private set; } = null!;
        public IncomingNachaEntryClassification Classification { get; private set; } = null!;
        public IncomingNachaDispatchQueue Queue { get; private set; } = null!;
        public AchTransaction Transaction { get; private set; } = null!;
        public AchCycle Cycle { get; private set; } = null!;

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new AchDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var fixture = new Fixture(connection, context);
            await fixture.SeedAsync();
            return fixture;
        }

        public async Task PublishProcTransaccionesMappingAsync()
        {
            await Catalog.GetMethodsAsync();
            var method = await Context.IntegrationMethods.SingleAsync(x => x.Code == "WSCFAACH.Proc_Transacciones");
            var parameters = await Context.IntegrationMethodParameters
                .Where(x => x.MethodId == method.Id && x.IsActive && x.Direction == IntegrationParameterDirectionEnum.Input)
                .ToListAsync();

            var set = new IntegrationMappingSet
            {
                Id = Guid.NewGuid(),
                MethodId = method.Id,
                Name = "ProcTransacciones NACHA desagregado",
                Version = 1,
                Status = IntegrationMappingSetStatusEnum.Published,
                IsActive = true,
                PublishedAtUtc = DateTime.UtcNow,
                PublishedBy = "test"
            };
            Context.IntegrationMappingSets.Add(set);

            foreach (var parameter in parameters)
            {
                var sourcePath = SourcePathFor(parameter.ParameterPath);
                Context.IntegrationMappingRules.Add(new IntegrationMappingRule
                {
                    MappingSetId = set.Id,
                    MethodId = method.Id,
                    ParameterId = parameter.Id,
                    SourceKind = SourceKindFor(sourcePath),
                    SourceFieldPath = sourcePath ?? string.Empty,
                    FixedValue = sourcePath is null ? DefaultFor(parameter) : null,
                    Priority = 1,
                    Enabled = true
                });
            }

            await Context.SaveChangesAsync();
        }

        private async Task SeedAsync()
        {
            await Catalog.GetMethodsAsync();

            Context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, HolidayStrategy = "Colombian" });
            Context.ClearingHouses.Add(new ClearingHouse { Id = 1, Name = "ACH Colombia", Code = "ACH", OriginCode = "0001283", ClearingHouseId = 1 });
            var cfa = new FinancialInstitution
            {
                Id = 1,
                Name = "Cooperativa Financiera de Antioquia",
                RoutingNumber = "0001",
                TransitCode = "0283",
                IsDefaultSource = true,
                Status = FinancialInstitutionStatus.Active
            };
            cfa.CalculateCheckDigit();
            var external = new FinancialInstitution
            {
                Id = 2,
                Name = "Banco Externo UAT",
                RoutingNumber = "9999",
                TransitCode = "0000",
                IsDefaultSource = false,
                Status = FinancialInstitutionStatus.Active
            };
            external.CalculateCheckDigit();
            Context.FinancialInstitutions.AddRange(cfa, external);
            Cycle = new AchCycle
            {
                Id = "NACHA-CYCLE",
                CycleName = "NACHA",
                ClearingHouseId = 1,
                ProcessingDate = new DateTime(2026, 5, 23),
                StartTime = TimeSpan.Zero,
                EndTime = new TimeSpan(23, 59, 0),
                CutoffTime = new TimeSpan(23, 0, 0)
            };
            Context.AchCycles.Add(Cycle);
            Context.AchBatches.Add(new AchBatch { Id = 77, AchCycleId = Cycle.Id, EffectiveEntryDate = Cycle.ProcessingDate, CompanyEntryDescriptionId = 1 });

            Transaction = new AchTransaction
            {
                Id = 701,
                Amount = 4321.50m,
                Type = TransactionTypeEnum.Credit,
                TransactionCode = "22",
                TransactionExternalId = "UAT-NACHA-EXT-701",
                Reference = "UAT-NACHA-EXT-701",
                SourceInstitutionId = 2,
                DestinationInstitutionId = 1,
                SourceAccountNumber = "999000111",
                DestinationAccountNumber = "0000003202",
                OriginatingDFI = "9999000",
                ReceivingDFI = "0001283",
                TraceNumber = "999900001234567",
                CompanyIdentification = "900999000",
                AchCycleId = Cycle.Id,
                AchBatchId = 77,
                EffectiveEntryDate = Cycle.ProcessingDate,
                State = AchTransferStateEnum.Pending
            };
            Context.AchTransactions.Add(Transaction);

            Ingestion = new IncomingNachaFileIngestion
            {
                Id = Guid.NewGuid(),
                FileName = "9999000.001.1",
                FileHashSha256 = "HASH-UAT",
                FileSize = 1234,
                ContentType = "text/plain",
                UploadedBy = "test",
                CorrelationId = "corr-nacha"
            };
            Context.IncomingNachaFileIngestions.Add(Ingestion);

            Context.NachaHeaders.Add(new NachaHeader
            {
                NachaID = "NACHA-DES-001",
                IncomingNachaFileIngestionId = Ingestion.Id,
                ImmediateOrigin = "9999000",
                ImmediateDestination = "0001283",
                FileIdModifier = "A",
                ReferenceCode = "REF-UAT",
                ClearingHouseId = 1,
                AchCycleId = Cycle.Id
            });
            Context.BatchHeaders.Add(new BatchHeader
            {
                BatchID = 501,
                NachaID = "NACHA-DES-001",
                CompanyId = "900999000",
                CompanyName = "BANCO EXTERNO UAT",
                StandardEntryClassCode = "PPD",
                CompanyEntryDescription = "PAGO UAT",
                EffectiveEntryDate = "260523",
                OriginParticipantEntityCode = "9999000",
                BatchNumber = 1
            });
            Context.EntryDetails.Add(new EntryDetail
            {
                EntryDetailID = 601,
                NachaID = "NACHA-DES-001",
                TransactionCode = "22",
                ReceivingParticipantEntityCode = "0001283",
                AccountNumber = "0000003202",
                Amount = 4321.50m,
                RecipIdNumber = "900003201",
                RecipUserName = "USUARIO UAT",
                SequenceNumber = "999900001234567"
            });
            Context.AddendaRecords.Add(new AddendaRecord
            {
                AddendaID = 602,
                NachaID = "NACHA-DES-001",
                InfofromOriginator = "PAGO UAT DESAGREGADO",
                InvoiceOrAccountNumber = "FAC-UAT-001",
                EntryDetailSequenceNumber = "999900001234567"
            });
            Context.BatchControls.Add(new BatchControl
            {
                BatchControlID = 603,
                NachaID = "NACHA-DES-001",
                EntryAddendaCount = 2,
                EntryHash = 1283,
                TotalCreditAmount = 4321.50m,
                TotalDebitAmount = 0m
            });
            Context.FileControls.Add(new FileControl
            {
                FileControlID = 604,
                NachaID = "NACHA-DES-001",
                BatchCount = 1,
                BlockCount = 1,
                EntryAddendaCount = 2,
                EntryHash = 1283,
                TotalCreditAmount = 4321.50m,
                TotalDebitAmount = 0m
            });

            Classification = new IncomingNachaEntryClassification
            {
                Id = Guid.NewGuid(),
                IncomingNachaFileIngestionId = Ingestion.Id,
                EntryDetailId = 601,
                AddendaRecordId = 602,
                FunctionalClass = IncomingNachaFunctionalClass.CreditoEntrante,
                EligibilityStatus = IncomingNachaEligibilityStatus.Elegible,
                RequiresLink = true
            };
            Context.IncomingNachaEntryClassifications.Add(Classification);
            var link = new IncomingNachaTransactionLink
            {
                Id = Guid.NewGuid(),
                IncomingNachaFileIngestionId = Ingestion.Id,
                EntryDetailId = 601,
                AddendaRecordId = 602,
                AchTransactionId = Transaction.Id,
                LinkType = IncomingNachaLinkType.ExactTrace15,
                ConfidenceScore = 1,
                LinkedBy = "test",
                IsFinal = true
            };
            Context.IncomingNachaTransactionLinks.Add(link);
            Queue = new IncomingNachaDispatchQueue
            {
                Id = Guid.NewGuid(),
                IncomingNachaFileIngestionId = Ingestion.Id,
                IncomingNachaEntryClassificationId = Classification.Id,
                IncomingNachaTransactionLinkId = link.Id,
                AchTransactionId = Transaction.Id,
                AchCycleId = Cycle.Id,
                ClearingHouseId = 1,
                OperationalDate = Cycle.ProcessingDate,
                QueueStatus = IncomingNachaDispatchQueueStatus.Queued,
                Priority = 1,
                IdempotencyDispatchKey = "queue-uat"
            };
            Context.IncomingNachaDispatchQueue.Add(Queue);
            await Context.SaveChangesAsync();
        }

        private static string? SourcePathFor(string parameterPath)
            => parameterPath switch
            {
                "TIPTRAN" => "entryDetails.transactionCode",
                "BCORECEP" => "nachaHeaders.immediateDestination",
                "BCOORIG" => "nachaHeaders.immediateOrigin",
                "NORIG" => "batchHeaders.companyName",
                "NCTAORIG" => "batchHeaders.companyId",
                "IDORIG" => "batchHeaders.companyId",
                "DESTRAN" => "batchHeaders.companyEntryDescription",
                "FECEFEC" => "batchHeaders.effectiveEntryDate",
                "NCTARECEP" => "entryDetails.accountNumber",
                "MONTO" => "entryDetails.amount",
                "NRECEP" => "entryDetails.recipUserName",
                "IDRECEP" => "entryDetails.recipIdNumber",
                "INFPAG" => "addendaRecords.infofromOriginator",
                "IDTRAN" => "entryDetails.sequenceNumber",
                "IDLOTE" => "batchHeaders.batchNumber",
                "REGLOTE" => "batchControls.entryAddendaCount",
                "LIBRE1" => "fileControls.blockCount",
                _ => null
            };

        private static IntegrationSourceKindEnum SourceKindFor(string? sourcePath)
        {
            if (sourcePath is null) return IntegrationSourceKindEnum.Constant;
            if (sourcePath.StartsWith("nachaHeaders.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.NachaHeader;
            if (sourcePath.StartsWith("batchHeaders.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.BatchHeader;
            if (sourcePath.StartsWith("entryDetails.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.EntryDetail;
            if (sourcePath.StartsWith("addendaRecords.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.AddendaRecord;
            if (sourcePath.StartsWith("batchControls.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.BatchControl;
            if (sourcePath.StartsWith("fileControls.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.FileControl;
            return IntegrationSourceKindEnum.Constant;
        }

        private static string DefaultFor(IntegrationMethodParameter parameter)
            => parameter.ParameterPath switch
            {
                "TREG" => "6",
                "DISCRE" => "UAT",
                "CONV" => "CNV-UAT",
                "PROD" => "ACH",
                "IREVER" => "0",
                "LIBRE" => "UAT",
                "IDCAMCOMPE" => "1",
                "DIRECCIONIP" => "127.0.0.1",
                "RTAACH" => "PENDING",
                "RTALOC" => "PENDING",
                _ => parameter.DataType.ToLowerInvariant() switch
                {
                    "int" or "long" => "1",
                    "decimal" or "double" or "float" => "1.00",
                    _ => "UAT"
                }
            };

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
