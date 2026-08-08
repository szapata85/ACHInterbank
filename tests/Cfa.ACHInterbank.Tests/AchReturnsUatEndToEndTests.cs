using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class AchReturnsUatEndToEndTests
{
    [Theory]
    [InlineData(7002, "ACHCOL", "ACH Colombia", 101, "ACH-CYCLE-UAT", "DEV14", TransactionTypeEnum.Debit, "27", 3200)]
    public async Task GenerateReturnsFileAsync_ShouldUseOfficialReturnPolicy_AndPersistUatArtifacts(
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
        await using var harness = await CreateHarnessAsync();
        await SeedUatFixtureAsync(harness.Context);
        SeedReturnScenario(
            harness.Context,
            clearingHouseId,
            clearingHouseCode,
            clearingHouseName,
            transactionId,
            cycleId,
            transactionType,
            transactionCode,
            amount);

        await new NachaFileNamingRuleSeeder(harness.Context).SeedAsync();

        var fixedNow = new DateTimeOffset(2026, 06, 06, 10, 30, 00, TimeSpan.Zero);
        var expectedOriginCode = await GetExpectedOriginCodeAsync(harness.Context);
        var expectedReturnFileName = BuildExpectedReturnFileName(expectedOriginCode, 1);
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
        Assert.Equal(expectedReturnFileName, await harness.Context.Set<AchReturnGenerated>().Where(x => x.OriginalTransactionId == transactionId).Select(x => x.FileName).SingleAsync());
        Assert.Equal(transactionType, await harness.Context.AchTransactions.Where(x => x.Id == transactionId).Select(x => x.Type).SingleAsync());
        Assert.Equal(AchTransferStateEnum.ReturnedByEpr, await harness.Context.AchTransactions.Where(x => x.Id == transactionId).Select(x => x.State).SingleAsync());
        Assert.Equal(1, await harness.Context.AchReturnsGenerated.CountAsync(x => x.OriginalTransactionId == transactionId));
        Assert.Equal(1, await harness.Context.AchTransactionStateEvents.CountAsync(x => x.AchTransactionId == transactionId));
        Assert.Equal(0, await harness.Context.AchReturnOfReturnGeneratedFileAudits.CountAsync());

        var generatedRow = await harness.Context.AchReturnsGenerated.SingleAsync(x => x.OriginalTransactionId == transactionId);
        Assert.Equal(cycleId, generatedRow.ReturnCycleId);
        Assert.Equal(returnReasonCode, generatedRow.ReturnReasonCode);
        Assert.Equal(expectedReturnFileName, generatedRow.FileName);

        var stateEvent = await harness.Context.AchTransactionStateEvents.SingleAsync(x => x.AchTransactionId == transactionId);
        Assert.Equal(AchTransferStateEnum.Pending, stateEvent.FromState);
        Assert.Equal(AchTransferStateEnum.ReturnedByEpr, stateEvent.ToState);
        Assert.Equal(AchStateEventSourceEnum.Epr, stateEvent.Source);
        Assert.Contains("ReturnFileGenerated", stateEvent.PayloadJson, StringComparison.Ordinal);
        using (var payload = JsonDocument.Parse(stateEvent.PayloadJson!))
        {
            Assert.True(payload.RootElement.GetProperty("stateChanged").GetBoolean());
            Assert.Equal("Pending", payload.RootElement.GetProperty("previousState").GetString());
            Assert.Equal("ReturnedByEpr", payload.RootElement.GetProperty("newState").GetString());
        }

        var returnNameContext = new ExternalFileNameContext
        {
            ClearingHouseId = clearingHouseId,
            ClearingHouseCode = clearingHouseCode,
            ClearingHouseOriginCode = expectedOriginCode,
            ProcessingDate = fixedNow.UtcDateTime.Date,
            ExternalFileType = ExternalFileType.ReturnOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        };

        var nachaOutContext = new ExternalFileNameContext
        {
            ClearingHouseId = clearingHouseId,
            ClearingHouseCode = clearingHouseCode,
            ClearingHouseOriginCode = expectedOriginCode,
            ProcessingDate = fixedNow.UtcDateTime.Date,
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        };

        var returnSequenceService = CreateSequenceService(harness.Context);
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
    public async Task GenerateReturnsFileAsync_CenitUat_ShouldRemainBlockedWithoutSideEffects()
    {
        await using var harness = await CreateHarnessAsync();
        await SeedUatFixtureAsync(harness.Context);
        SeedReturnScenario(
            harness.Context,
            7001,
            "CENIT",
            "CENIT",
            201,
            "CEN-CYCLE-UAT",
            TransactionTypeEnum.Credit,
            "22",
            4100m);

        var fixedNow = new DateTimeOffset(2026, 06, 06, 10, 30, 00, TimeSpan.Zero);
        var sut = BuildReturnsService(
            harness.Context,
            fixedNow,
            BuildEligibilityService(harness.Context),
            BuildOfficialReturnOutPolicy(harness.Context));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateReturnsFileAsync(
            new GenerateReturnsFileRequest("CEN-CYCLE-UAT", [new ReturnSelectionItemDto(201, "R01")]),
            CancellationToken.None));

        Assert.Contains("RETURN_OUT_CENIT_TECHNICAL_HOMOLOGATION_REQUIRED", ex.Message, StringComparison.Ordinal);
        Assert.False(await harness.Context.AchReturnsGenerated.AnyAsync(x => x.OriginalTransactionId == 201));
        Assert.False(await harness.Context.AchTransactionStateEvents.AnyAsync(x => x.AchTransactionId == 201));
        Assert.Equal(AchTransferStateEnum.Pending, await harness.Context.AchTransactions.Where(x => x.Id == 201).Select(x => x.State).SingleAsync());
        Assert.False(await harness.Context.ExternalFileSequences.AnyAsync(x => x.ClearingHouseId == 7001));
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_ShouldRejectSecondAttempt_ForSameTransaction()
    {
        await using var harness = await CreateHarnessAsync();
        await SeedUatFixtureAsync(harness.Context);
        SeedReturnScenario(
            harness.Context,
            7002,
            "ACHCOL",
            "ACH Colombia",
            301,
            "ACH-CYCLE-IDEMPOTENCY",
            TransactionTypeEnum.Debit,
            "27",
            1200m);

        await new NachaFileNamingRuleSeeder(harness.Context).SeedAsync();

        var fixedNow = new DateTimeOffset(2026, 06, 06, 11, 00, 00, TimeSpan.Zero);
        var expectedOriginCode = await GetExpectedOriginCodeAsync(harness.Context);
        var expectedReturnFileName = BuildExpectedReturnFileName(expectedOriginCode, 1);
        var policy = BuildOfficialReturnOutPolicy(harness.Context);
        var eligibility = BuildEligibilityService(harness.Context);
        var sut = BuildReturnsService(harness.Context, fixedNow, eligibility, policy);
        var request = new GenerateReturnsFileRequest("ACH-CYCLE-IDEMPOTENCY", [new ReturnSelectionItemDto(301, "DEV14")]);

        var first = await sut.GenerateReturnsFileAsync(request, CancellationToken.None);
        Assert.Equal(expectedReturnFileName, first.FileName);

        var ex = await Assert.ThrowsAsync<AchReturnAlreadyGeneratedException>(() => sut.GenerateReturnsFileAsync(request, CancellationToken.None));
        Assert.Contains("ya cuenta con una devoluci", ex.Message, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(1, await harness.Context.AchReturnsGenerated.CountAsync(x => x.OriginalTransactionId == 301));
        Assert.Equal(1, await harness.Context.AchTransactionStateEvents.CountAsync(x => x.AchTransactionId == 301));
        Assert.Equal(AchTransferStateEnum.ReturnedByEpr, await harness.Context.AchTransactions.Where(x => x.Id == 301).Select(x => x.State).SingleAsync());
        var stateEvent = await harness.Context.AchTransactionStateEvents.SingleAsync(x => x.AchTransactionId == 301);
        Assert.Equal(AchTransferStateEnum.Pending, stateEvent.FromState);
        Assert.Equal(AchTransferStateEnum.ReturnedByEpr, stateEvent.ToState);
        Assert.Equal(AchStateEventSourceEnum.Epr, stateEvent.Source);
        using (var payload = JsonDocument.Parse(stateEvent.PayloadJson!))
        {
            Assert.True(payload.RootElement.GetProperty("stateChanged").GetBoolean());
            Assert.Equal("Pending", payload.RootElement.GetProperty("previousState").GetString());
            Assert.Equal("ReturnedByEpr", payload.RootElement.GetProperty("newState").GetString());
        }
        Assert.Equal(1, await harness.Context.ExternalFileSequences.CountAsync(x => x.ClearingHouseId == 7002 && x.ScopeCode == "ACH_RETURN_EXTERNAL_NAME"));
    }

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

    private static string BuildExpectedReturnFileName(string originCode, int sequence)
        => $"{originCode}.{sequence:D3}.1";

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
        var sequenceService = CreateSequenceService(context);
        var identifierMapService = new NachaFileIdentifierMapService(context);
        var namingRuleService = new NachaFileNamingRuleService(context);
        var builder = new ExternalFileNameBuilder(sequenceService, identifierMapService, namingRuleService);
        var duplicateGuard = new ExternalFileDuplicateGuard(context);
        var correlationService = new ExternalFileNameCorrelationService(identifierMapService);
        var validator = new ExternalFileNameValidator(duplicateGuard, correlationService, identifierMapService);
        var auditService = new ExternalFileNameAuditService(context);
        return new ExternalFileNamePolicy(builder, validator, correlationService, auditService, duplicateGuard);
    }

    private static ExternalFileNameSequenceService CreateSequenceService(AchDbContext context)
    {
        var resolver = new ExternalFileNameSequenceProviderResolver([new EfGenericExternalFileNameSequenceService(context)]);
        return new ExternalFileNameSequenceService(context, resolver);
    }

    private static async Task SeedUatFixtureAsync(AchDbContext context)
    {
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = 1,
            ClearingHouseId = 1,
            HolidayStrategy = "Colombian"
        });

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

        var source = new FinancialInstitution
        {
            Id = 1,
            Name = "Origen UAT Controlado",
            RoutingNumber = "98765",
            TransitCode = "321",
            IsDefaultSource = true,
            Status = FinancialInstitutionStatus.Active
        };
        source.CalculateCheckDigit();
        context.FinancialInstitutions.Add(source);

        SeedIdentifierMaps(context);
        SeedReturnCatalogs(context);

        await context.SaveChangesAsync();
    }

    private static void SeedIdentifierMaps(AchDbContext context)
    {
        if (context.NachaFileIdentifierMaps.Any())
        {
            return;
        }

        for (var sequence = 1; sequence <= 36; sequence++)
        {
            context.NachaFileIdentifierMaps.Add(new NachaFileIdentifierMap
            {
                Sequence = sequence,
                Identifier = sequence <= 26
                    ? ((char)('A' + (sequence - 1))).ToString()
                    : ((char)('0' + (sequence - 27))).ToString()
            });
        }
    }

    private static void SeedReturnCatalogs(AchDbContext context)
    {
        if (!context.AchReturnCodes.Any())
        {
            context.AchReturnCodes.AddRange(
                new AchReturnCode
                {
                    ClearingHouseId = 7001,
                    Code = "R01",
                    Description = "Insufficient funds",
                    AppliesToCredit = true,
                    AppliesToDebit = false,
                    AppliesToPrenotification = false,
                    AppliesToReturn = true,
                    RequiresAddenda = true,
                    MaxDaysAllowed = 60,
                    IsActive = true,
                    EffectiveFrom = new DateTime(2026, 01, 01)
                },
                new AchReturnCode
                {
                    ClearingHouseId = 7002,
                    Code = "DEV14",
                    Description = "No consentimiento",
                    AppliesToCredit = false,
                    AppliesToDebit = true,
                    AppliesToPrenotification = false,
                    AppliesToReturn = true,
                    RequiresAddenda = true,
                    MaxDaysAllowed = 60,
                    IsActive = true,
                    EffectiveFrom = new DateTime(2026, 01, 01)
                });
        }

        if (!context.AchReturnPolicies.Any())
        {
            context.AchReturnPolicies.AddRange(
                new AchReturnPolicy
                {
                    ClearingHouseId = 7001,
                    TransactionType = "Credit",
                    Direction = AchReturnDirection.Any,
                    FlowType = AchReturnFlowType.Return,
                    AllowedReturnCodesCsv = "R01",
                    MaxDays = 60,
                    RequiredOriginalTransactionState = "Pending",
                    RequiresAddenda = true,
                    IsActive = true,
                    EffectiveFrom = new DateTime(2026, 01, 01)
                },
                new AchReturnPolicy
                {
                    ClearingHouseId = 7002,
                    TransactionType = "Debit",
                    Direction = AchReturnDirection.Any,
                    FlowType = AchReturnFlowType.Return,
                    AllowedReturnCodesCsv = "DEV14",
                    MaxDays = 60,
                    RequiredOriginalTransactionState = "Pending",
                    RequiresAddenda = true,
                    IsActive = true,
                    EffectiveFrom = new DateTime(2026, 01, 01)
                });
        }
    }

    private static void SeedReturnScenario(
        AchDbContext context,
        int clearingHouseId,
        string clearingHouseCode,
        string clearingHouseName,
        int transactionId,
        string cycleId,
        TransactionTypeEnum transactionType,
        string transactionCode,
        decimal amount)
    {
        if (!context.AchCycles.Any(x => x.Id == cycleId))
        {
            context.AchCycles.Add(new AchCycle
            {
                Id = cycleId,
                CycleName = cycleId,
                ProcessingDate = new DateTime(2026, 06, 06),
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(10, 0, 0),
                CutoffTime = new TimeSpan(10, 0, 0),
                ClearingHouseId = clearingHouseId
            });
        }

        if (!context.AchTransactions.Any(x => x.Id == transactionId))
        {
            context.AchTransactions.Add(new AchTransaction
            {
                Id = transactionId,
                AchCycleId = cycleId,
                Type = transactionType,
                State = AchTransferStateEnum.Pending,
                EffectiveEntryDate = new DateTime(2026, 06, 06),
                TransactionCode = transactionCode,
                ServiceClassCode = transactionType is TransactionTypeEnum.Debit or TransactionTypeEnum.Reversal or TransactionTypeEnum.Return ? "225" : "220",
                CompanyName = clearingHouseName,
                CompanyIdentification = clearingHouseCode,
                OriginatingDFI = "12345678",
                ReceivingDFI = "87654321",
                TraceNumber = $"{transactionId:000000000000000}",
                TraceSequenceNumber = transactionId,
                Amount = amount,
                Reference = $"REF-{transactionId}",
                SourceAccountNumber = "111122223333",
                DestinationAccountNumber = "999988887777",
                RecipientIdNumber = transactionType == TransactionTypeEnum.Debit ? "900000014" : "900000001",
                SourceInstitutionId = 1,
                DestinationInstitutionId = 1,
                AddendaRecordIndicator = true,
                AchBatch = new AchBatch
                {
                    Id = transactionId,
                    AchCycleId = cycleId,
                    ServiceClassCode = transactionType is TransactionTypeEnum.Debit or TransactionTypeEnum.Reversal or TransactionTypeEnum.Return ? "225" : "220",
                    CompanyName = clearingHouseName,
                    CompanyIdentification = clearingHouseCode,
                    CompanyEntryDescription = "UAT DEVOLUCIONES",
                    EffectiveEntryDate = new DateTime(2026, 06, 06),
                    OriginOrOdfi = "12345678"
                }
            });
        }

        context.SaveChanges();
    }

    private static async Task<SqliteHarness> CreateHarnessAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return new SqliteHarness(connection, context);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
        public override long GetTimestamp() => now.UtcDateTime.Ticks;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class SqliteHarness(SqliteConnection connection, AchDbContext context) : IAsyncDisposable
    {
        public SqliteConnection Connection { get; } = connection;
        public AchDbContext Context { get; } = context;

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
