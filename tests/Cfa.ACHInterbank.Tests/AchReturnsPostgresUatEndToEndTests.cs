using System.Data;
using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Cfa.ACHInterbank.Tests;

[Trait("Category", "Postgres")]
[Trait("Category", "Integration")]
[Trait("Category", "ReturnOut")]
public class AchReturnsPostgresUatEndToEndTests
{
    private const string BackfillTargetMigration = "20260521225311_AddIntegrationMappingTrace";

    [Theory]
    [InlineData(7002, "ACHCOL", "ACH Colombia", 101, "ACH-CYCLE-UAT", "DEV14", TransactionTypeEnum.Debit, "27", 3200)]
    [InlineData(7001, "CENIT", "CENIT", 201, "CEN-CYCLE-UAT", "R01", TransactionTypeEnum.Credit, "22", 4100)]
    public async Task GenerateReturnsFileAsync_ShouldUseOfficialReturnPolicy_AndPersistPostgresArtifacts(
        int clearingHouseId,
        string clearingHouseCode,
        string clearingHouseName,
        int transactionId,
        string cycleId,
        string returnReasonCode,
        TransactionTypeEnum transactionType,
        string transactionCode,
        decimal amount)
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

        await SeedReturnScenarioAsync(
            harness.Context,
            clearingHouseId,
            clearingHouseCode,
            clearingHouseName,
            transactionId,
            cycleId,
            transactionType,
            transactionCode,
            amount,
            returnReasonCode);

        Assert.Equal(2, await harness.Context.NachaFileNamingRules.CountAsync(x => x.IsActive));

        var fixedNow = new DateTimeOffset(2026, 06, 06, 10, 30, 00, TimeSpan.Zero);
        var expectedOriginCode = await GetExpectedOriginCodeAsync(harness.Context);
        var expectedReturnFileName = await BuildExpectedReturnFileNameAsync(harness.Context, 1);
        var policy = BuildOfficialReturnOutPolicy(harness.Context);
        var eligibility = BuildEligibilityService(harness.Context);
        var sut = BuildReturnsService(harness.Context, fixedNow, eligibility, policy);

        var response = await sut.GenerateReturnsFileAsync(
            new GenerateReturnsFileRequest(cycleId, [new ReturnSelectionItemDto(transactionId, returnReasonCode)]),
            CancellationToken.None);

        Assert.Equal(expectedReturnFileName, response.FileName);
        Assert.Equal("text/plain", response.ContentType);
        Assert.True(response.TotalRecords > 0);
        Assert.Equal(1, response.TotalReturns);

        var generatedRow = await harness.Context.AchReturnsGenerated.SingleAsync(x => x.OriginalTransactionId == transactionId);
        Assert.Equal(expectedReturnFileName, generatedRow.FileName);
        Assert.Equal(cycleId, generatedRow.ReturnCycleId);
        Assert.Equal(returnReasonCode, generatedRow.ReturnReasonCode);

        Assert.Equal(transactionType, await harness.Context.AchTransactions.Where(x => x.Id == transactionId).Select(x => x.Type).SingleAsync());
        Assert.Equal(AchTransferStateEnum.Pending, await harness.Context.AchTransactions.Where(x => x.Id == transactionId).Select(x => x.State).SingleAsync());
        Assert.Equal(1, await harness.Context.AchReturnsGenerated.CountAsync(x => x.OriginalTransactionId == transactionId));
        Assert.Equal(1, await harness.Context.AchTransactionStateEvents.CountAsync(x => x.AchTransactionId == transactionId));
        Assert.Equal(0, await harness.Context.AchReturnOfReturnGeneratedFileAudits.CountAsync());

        var stateEvent = await harness.Context.AchTransactionStateEvents.SingleAsync(x => x.AchTransactionId == transactionId);
        Assert.Equal(AchTransferStateEnum.Pending, stateEvent.FromState);
        Assert.Equal(AchTransferStateEnum.Pending, stateEvent.ToState);
        Assert.Equal(AchStateEventSourceEnum.System, stateEvent.Source);
        Assert.Contains("ReturnFileGenerated", stateEvent.PayloadJson, StringComparison.Ordinal);

        var returnNameContext = new ExternalFileNameContext
        {
            ClearingHouseId = clearingHouseId,
            ClearingHouseCode = clearingHouseCode,
            ClearingHouseOriginCode = expectedOriginCode,
            ProcessingDate = fixedNow.UtcDateTime.Date,
            ExternalFileType = ExternalFileType.ReturnOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound,
            RequestedBy = "postgres-uat"
        };

        var nachaOutContext = new ExternalFileNameContext
        {
            ClearingHouseId = clearingHouseId,
            ClearingHouseCode = clearingHouseCode,
            ClearingHouseOriginCode = expectedOriginCode,
            ProcessingDate = fixedNow.UtcDateTime.Date,
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound,
            RequestedBy = "postgres-uat"
        };

        var returnSequenceService = BuildSequenceService(harness.Context);
        Assert.Equal(2, await returnSequenceService.ReserveNextSequenceAsync(returnNameContext, CancellationToken.None));
        Assert.Equal(1, await returnSequenceService.ReserveNextSequenceAsync(nachaOutContext, CancellationToken.None));

        var sequences = await harness.Context.ExternalFileSequences
            .AsNoTracking()
            .Where(x => x.ClearingHouseId == clearingHouseId && x.SequenceDate == DateOnly.FromDateTime(fixedNow.UtcDateTime.Date))
            .OrderBy(x => x.ScopeCode)
            .ToListAsync();

        Assert.Equal(2, sequences.Count);
        Assert.Contains(sequences, row => row.ScopeCode == "ACH_RETURN_EXTERNAL_NAME" && row.LastValue == 2);
        Assert.Contains(sequences, row => row.ScopeCode == "ACH_EXTERNAL_NAME" && row.LastValue == 1);

        var records = Encoding.UTF8.GetString(response.Content).Chunk(106).Select(x => new string(x)).ToArray();
        Assert.Equal(10, records.Length);
        Assert.Equal(new[] { '1', '5', '6', '7', '8', '9' }, records.Take(6).Select(x => x[0]).ToArray());
        Assert.All(records, record => Assert.Equal(106, record.Length));

        var original = await harness.Context.AchTransactions.AsNoTracking().SingleAsync(x => x.Id == transactionId);
        Assert.Equal(AchTransferStateEnum.ReturnedByEpr, original.State);
        Assert.Equal(amount, original.Amount);
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldRejectSecondAttempt_ForSameTransaction()
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

        await SeedReturnScenarioAsync(
            harness.Context,
            7002,
            "ACHCOL",
            "ACH Colombia",
            301,
            "ACH-CYCLE-IDEMPOTENCY",
            TransactionTypeEnum.Debit,
            "27",
            1200m,
            "DEV14");

        var fixedNow = new DateTimeOffset(2026, 06, 06, 11, 00, 00, TimeSpan.Zero);
        var expectedReturnFileName = await BuildExpectedReturnFileNameAsync(harness.Context, 1);
        var policy = BuildOfficialReturnOutPolicy(harness.Context);
        var eligibility = BuildEligibilityService(harness.Context);
        var sut = BuildReturnsService(harness.Context, fixedNow, eligibility, policy);
        var request = new GenerateReturnsFileRequest("ACH-CYCLE-IDEMPOTENCY", [new ReturnSelectionItemDto(301, "DEV14")]);

        var first = await sut.GenerateReturnsFileAsync(request, CancellationToken.None);
        Assert.Equal(expectedReturnFileName, first.FileName);

        var ex = await Assert.ThrowsAsync<AchReturnAlreadyGeneratedException>(() => sut.GenerateReturnsFileAsync(request, CancellationToken.None));
        Assert.Contains("ya cuenta con una devoluci", ex.Message, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(expectedReturnFileName, await harness.Context.AchReturnsGenerated.Where(x => x.OriginalTransactionId == 301).Select(x => x.FileName).SingleAsync());
        Assert.Equal(1, await harness.Context.AchReturnsGenerated.CountAsync(x => x.OriginalTransactionId == 301));
        Assert.Equal(1, await harness.Context.AchTransactionStateEvents.CountAsync(x => x.AchTransactionId == 301));
        Assert.Equal(AchTransferStateEnum.Pending, await harness.Context.AchTransactions.Where(x => x.Id == 301).Select(x => x.State).SingleAsync());
        Assert.Equal(1, await harness.Context.ExternalFileSequences.CountAsync(x => x.ClearingHouseId == 7002 && x.ScopeCode == "ACH_RETURN_EXTERNAL_NAME"));
        Assert.Equal(0, await harness.Context.AchReturnOfReturnGeneratedFileAudits.CountAsync());
    }

    private static bool ShouldRunUat()
        => string.Equals(Environment.GetEnvironmentVariable("RUN_POSTGRES_RETURNOUT_UAT"), "true", StringComparison.OrdinalIgnoreCase);

    private static async Task<string> GetExpectedOriginCodeAsync(AchDbContext context)
    {
        var source = await context.FinancialInstitutions
            .AsNoTracking()
            .SingleAsync(x => x.IsDefaultSource);

        var routingNumber = new string((source.RoutingNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        var transitCode = new string((source.TransitCode ?? string.Empty).Where(char.IsDigit).ToArray());

        if (routingNumber.Length < 4)
        {
            throw new InvalidOperationException("La institucion financiera origen no permite derivar RRRRTTT.");
        }

        if (transitCode.Length != 3)
        {
            throw new InvalidOperationException("La institucion financiera origen no permite derivar RRRRTTT.");
        }

        return $"{routingNumber[^4..]}{transitCode}";
    }

    private static async Task<string> BuildExpectedReturnFileNameAsync(AchDbContext context, int sequence)
    {
        var originCode = await GetExpectedOriginCodeAsync(context);
        return $"{originCode}.{sequence:D3}.1";
    }

    private static AchReturnsService BuildReturnsService(
        AchDbContext context,
        DateTimeOffset fixedNow,
        IAchReturnEligibilityService eligibilityService,
        IExternalFileNamePolicy externalFileNamePolicy)
    {
        return new AchReturnsService(
            context,
            new FixedTimeProvider(fixedNow),
            new AchRegulatoryCatalogService(context),
            eligibilityService,
            new TestReturnGenerationLockService(),
            externalFileNamePolicy: externalFileNamePolicy,
            nachaFileBuilder: ReturnOutNachaFileBuilderFactory.Create());
    }

    private static IAchReturnEligibilityService BuildEligibilityService(AchDbContext context)
        => new AchReturnEligibilityService(context, new AchRegulatoryCatalogService(context));

    private static IExternalFileNamePolicy BuildOfficialReturnOutPolicy(AchDbContext context)
    {
        var sequenceService = BuildSequenceService(context);
        var identifierMapService = new NachaFileIdentifierMapService(context);
        var namingRuleService = new NachaFileNamingRuleService(context);
        var builder = new ExternalFileNameBuilder(sequenceService, identifierMapService, namingRuleService);
        var duplicateGuard = new ExternalFileDuplicateGuard(context);
        var correlationService = new ExternalFileNameCorrelationService(identifierMapService);
        var validator = new ExternalFileNameValidator(duplicateGuard, correlationService, identifierMapService);
        var auditService = new ExternalFileNameAuditService(context);

        return new ExternalFileNamePolicy(builder, validator, correlationService, auditService, duplicateGuard);
    }

    private static ExternalFileNameSequenceService BuildSequenceService(AchDbContext context)
    {
        var resolver = new ExternalFileNameSequenceProviderResolver(
        [
            new PostgresExternalFileNameSequenceService(context),
            new EfGenericExternalFileNameSequenceService(context)
        ]);

        return new ExternalFileNameSequenceService(context, resolver);
    }

    private static async Task SeedReturnScenarioAsync(
        AchDbContext context,
        int clearingHouseId,
        string clearingHouseCode,
        string clearingHouseName,
        int transactionId,
        string cycleId,
        TransactionTypeEnum transactionType,
        string transactionCode,
        decimal amount,
        string returnReasonCode)
    {
        var companyEntryDescriptionId = await context.CompanyEntryDescriptionCatalogs
            .AsNoTracking()
            .Where(x => x.Term == "NOMINAS")
            .Select(x => x.Id)
            .SingleAsync();

        var defaultSourceId = await context.FinancialInstitutions
            .AsNoTracking()
            .Where(x => x.IsDefaultSource)
            .Select(x => x.Id)
            .SingleAsync();

        var destinationId = await context.FinancialInstitutions
            .AsNoTracking()
            .Where(x => !x.IsDefaultSource)
            .Select(x => x.Id)
            .SingleAsync();

        var effectiveDate = new DateTime(2026, 06, 06, 0, 0, 0, DateTimeKind.Utc);

        if (!await context.AchReturnCodes.AnyAsync(x => x.ClearingHouseId == clearingHouseId && x.Code == returnReasonCode))
        {
            context.AchReturnCodes.Add(new AchReturnCode
            {
                ClearingHouseId = clearingHouseId,
                Code = returnReasonCode,
                Description = $"Causal {returnReasonCode} sintética",
                AppliesToDebit = transactionType == TransactionTypeEnum.Debit || transactionType == TransactionTypeEnum.Return,
                AppliesToCredit = transactionType == TransactionTypeEnum.Credit || transactionType == TransactionTypeEnum.Return,
                AppliesToPrenotification = false,
                AppliesToReturn = true,
                RequiresAddenda = true,
                MaxDaysAllowed = 60,
                EffectiveFrom = effectiveDate,
                EffectiveTo = null,
                IsActive = true,
                RegulatorySource = clearingHouseCode
            });
        }

        if (!await context.AchReturnPolicies.AnyAsync(x => x.ClearingHouseId == clearingHouseId && x.TransactionType == transactionType.ToString()))
        {
            context.AchReturnPolicies.Add(new AchReturnPolicy
            {
                ClearingHouseId = clearingHouseId,
                TransactionType = transactionType.ToString(),
                Direction = AchReturnDirection.Any,
                FlowType = AchReturnFlowType.Return,
                AllowedReturnCodesCsv = returnReasonCode,
                MaxDays = 60,
                RequiredOriginalTransactionState = AchTransferStateEnum.Pending.ToString(),
                AllowsReturnOfReturn = true,
                RequiresAddenda = true,
                IsActive = true,
                EffectiveFrom = effectiveDate,
                EffectiveTo = null
            });
        }

        if (!await context.AchCycles.AnyAsync(x => x.Id == cycleId))
        {
            context.AchCycles.Add(new AchCycle
            {
                Id = cycleId,
                CycleName = cycleId,
                ProcessingDate = effectiveDate,
                CutoffTime = new TimeSpan(12, 0, 0),
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                RescheduleOnHoliday = false,
                ClearingHouseId = clearingHouseId
            });
        }

        await context.SaveChangesAsync();

        if (!await context.AchBatches.AnyAsync(x => x.AchCycleId == cycleId && x.CompanyEntryDescriptionId == companyEntryDescriptionId))
        {
            context.AchBatches.Add(new AchBatch
            {
                AchCycleId = cycleId,
                ServiceClassCode = transactionType == TransactionTypeEnum.Debit ? "220" : "220",
                CompanyName = clearingHouseName,
                CompanyIdentification = $"CID-{clearingHouseId}",
                CompanyEntryDescription = "NOMINAS",
                CompanyEntryDescriptionId = companyEntryDescriptionId,
                OriginOrOdfi = clearingHouseCode,
                EffectiveEntryDate = effectiveDate,
                BatchSequenceNumber = 1,
                TotalDebitAmount = amount,
                TotalCreditAmount = 0m
            });

            await context.SaveChangesAsync();
        }

        var batch = await context.AchBatches
            .AsNoTracking()
            .SingleAsync(x => x.AchCycleId == cycleId && x.CompanyEntryDescriptionId == companyEntryDescriptionId);

        if (!await context.AchTransactions.AnyAsync(x => x.Id == transactionId))
        {
            context.AchTransactions.Add(new AchTransaction
            {
                Id = transactionId,
                Amount = amount,
                TransactionExternalId = $"TX-{transactionId}",
                Reference = $"REF-{transactionId}",
                Type = transactionType,
                TransactionCode = transactionCode,
                ServiceClassCode = "220",
                CompanyEntryDescriptionId = companyEntryDescriptionId,
                CompanyName = clearingHouseName,
                CompanyIdentification = $"CID-{clearingHouseId}",
                OriginatingDFI = "12345678",
                ReceivingDFI = "87654321",
                TraceNumber = $"8765432100{transactionId:0000000}",
                TraceSequenceNumber = transactionId,
                EffectiveEntryDate = effectiveDate,
                AddendaRecordIndicator = true,
                IsPrenotification = false,
                State = AchTransferStateEnum.Pending,
                StateChangedAtUtc = effectiveDate,
                SourceAccountNumber = "000123456789",
                DestinationAccountNumber = "000987654321",
                SourceInstitutionId = defaultSourceId,
                DestinationInstitutionId = destinationId,
                AchCycleId = cycleId,
                AchBatchId = batch.Id,
                RecipientIdNumber = string.Empty,
                OriginalTraceRef = $"ORIG-{transactionId}",
                DiscretionaryData = string.Empty
            });

            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedReferenceDataAsync(AchDbContext context)
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
            context.ClearingHouses.AddRange(
                new ClearingHouse
                {
                    Id = 7001,
                    Name = "CENIT",
                    Code = "CENIT",
                    OriginCode = "000101006",
                    ClearingHouseId = 1
                },
                new ClearingHouse
                {
                    Id = 7002,
                    Name = "ACH Colombia",
                    Code = "ACHCOL",
                    OriginCode = "000101006",
                    ClearingHouseId = 1
                });
        }

        if (!await context.FinancialInstitutions.AnyAsync())
        {
            var source = new FinancialInstitution
            {
                Id = 901,
                Name = "Banco Origen UAT",
                RoutingNumber = "98765",
                TransitCode = "321",
                Status = FinancialInstitutionStatus.Active,
                IsDefaultSource = true
            };
            source.CalculateCheckDigit();

            var destination = new FinancialInstitution
            {
                Id = 902,
                Name = "Banco Destino UAT",
                RoutingNumber = "12345",
                TransitCode = "678",
                Status = FinancialInstitutionStatus.Active,
                IsDefaultSource = false
            };
            destination.CalculateCheckDigit();

            context.FinancialInstitutions.AddRange(source, destination);
        }

        await context.SaveChangesAsync();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
        public override long GetTimestamp() => now.UtcDateTime.Ticks;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
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
                    throw new InvalidOperationException("RUN_POSTGRES_RETURNOUT_UAT=true pero no se proporcionó una cadena de conexión PostgreSQL. Define POSTGRES_TEST_CONNECTION_STRING o ConnectionStrings__PostgresConnection.");
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
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
                .Options;

            var context = new AchDbContext(options);
            var migrator = context.Database.GetService<IMigrator>();
            await migrator.MigrateAsync(BackfillTargetMigration);
            await SeedReferenceDataAsync(context);
            await migrator.MigrateAsync();
            await new NachaFileNamingRuleSeeder(context).SeedAsync();

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
