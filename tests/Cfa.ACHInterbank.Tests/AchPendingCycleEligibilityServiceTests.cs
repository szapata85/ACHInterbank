using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

/// <summary>Pruebas escritas en I01; su ejecución queda pendiente de un ambiente con dotnet.</summary>
public sealed class AchPendingCycleEligibilityServiceTests
{
    private static readonly DateTime Monday = new(2026, 10, 26);
    private static readonly DateTimeOffset MondayEleven = new(2026, 10, 26, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PendingUnexported_AfterCutoff_SelectsNextEligibleSameChamber()
    {
        await using var test = await Scenario.CreateAsync(MondayEleven);
        var result = await test.Service.EvaluateAsync(101);
        Assert.True(result.IsEligible);
        Assert.Equal("ELIGIBLE", result.ReasonCode);
        Assert.Equal("C1", result.FromCycleId);
        Assert.Equal("C2", result.ToCycleId);
        Assert.Equal(1, result.ClearingHouseId);
        Assert.Equal("C1", (await test.Context.AchTransactions.SingleAsync()).AchCycleId);
    }

    [Fact]
    public async Task SaturdayBeforeProjectedMonday_IsNotExpired()
    {
        await using var test = await Scenario.CreateAsync(new DateTimeOffset(2026, 10, 24, 18, 0, 0, TimeSpan.Zero));
        Assert.Equal("CYCLE_NOT_EXPIRED", (await test.Service.EvaluateAsync(101)).ReasonCode);
    }

    [Fact]
    public async Task WhenCycleIsStillOpen_DoesNotMove()
    {
        await using var test = await Scenario.CreateAsync(new DateTimeOffset(2026, 10, 26, 13, 0, 0, TimeSpan.Zero));
        Assert.Equal("CYCLE_NOT_EXPIRED", (await test.Service.EvaluateAsync(101)).ReasonCode);
    }

    [Fact]
    public async Task PolicyRejectsSecondCycle_SkipsToThird()
    {
        await using var test = await Scenario.CreateAsync(MondayEleven, deniedCycle: "Ciclo 2");
        Assert.Equal("C3", (await test.Service.EvaluateAsync(101)).ToCycleId);
    }

    [Fact]
    public async Task WhenNoLaterEligibleCycleOnValueDate_Declines()
    {
        await using var test = await Scenario.CreateAsync(
            new DateTimeOffset(2026, 10, 26, 21, 0, 0, TimeSpan.Zero));
        Assert.Equal("NO_ELIGIBLE_TARGET_SAME_VALUE_DATE", (await test.Service.EvaluateAsync(101)).ReasonCode);
    }

    [Fact]
    public async Task LastEligibleCycleExpired_DoesNotChangeValueDateOrSelectNextDay()
    {
        await using var test = await Scenario.CreateAsync(
            new DateTimeOffset(2026, 10, 26, 21, 0, 0, TimeSpan.Zero),
            assignedCycle: "C3");
        var result = await test.Service.EvaluateAsync(101);
        Assert.False(result.IsEligible);
        Assert.Equal("NO_ELIGIBLE_TARGET_SAME_VALUE_DATE", result.ReasonCode);
        var transaction = await test.Context.AchTransactions.SingleAsync();
        Assert.Equal("C3", transaction.AchCycleId);
        Assert.Equal(Monday, transaction.EffectiveEntryDate);
    }

    [Fact]
    public async Task SourceCycleOnNonBusinessDay_DeniesRegardlessOfUpcomingCycles()
    {
        await using var test = await Scenario.CreateAsync(MondayEleven, sourceBusinessDay: false);
        var result = await test.Service.EvaluateAsync(101);
        Assert.False(result.IsEligible);
        Assert.Equal("NOT_BUSINESS_DAY", result.ReasonCode);
    }

    [Fact]
    public async Task ExportMembership_AlwaysDenies()
    {
        await using var test = await Scenario.CreateAsync(MondayEleven);
        test.Context.AchFileExportTransactions.Add(new AchFileExportTransaction
        {
            AchTransactionId = 101, AchCycleId = "C1", AchBatchId = 51, AchFileExportId = 99
        });
        await test.Context.SaveChangesAsync();
        Assert.Equal("EXPORT_EVIDENCE_PRESENT", (await test.Service.EvaluateAsync(101)).ReasonCode);
    }

    [Fact]
    public async Task DispatchEvidence_DeniesEvenPending()
    {
        await using var test = await Scenario.CreateAsync(MondayEleven);
        test.Context.ContrapartidaDispatchItems.Add(new ContrapartidaDispatchItem
        {
            AchTransactionId = 101, AchCycleId = "C1", AchBatchId = 51, ClearingHouseId = 1
        });
        await test.Context.SaveChangesAsync();
        Assert.Equal("PROCESSING_EVIDENCE_PRESENT", (await test.Service.EvaluateAsync(101)).ReasonCode);
    }

    [Fact]
    public async Task CenitQueueEvidence_Denies()
    {
        await using var test = await Scenario.CreateAsync(MondayEleven);
        test.Context.CenitCycleQueues.Add(new CenitCycleQueue
        {
            AchTransactionId = 101, TargetAchCycleId = "C2", OriginalAchCycleId = "C1"
        });
        await test.Context.SaveChangesAsync();
        Assert.Equal("PROCESSING_EVIDENCE_PRESENT", (await test.Service.EvaluateAsync(101)).ReasonCode);
    }

    [Fact]
    public async Task LegacyPublishExportWithoutMembership_Denies()
    {
        await using var test = await Scenario.CreateAsync(MondayEleven);
        test.Context.AchFileExports.Add(new AchFileExport
        {
            AchCycleId = "C1", ClearingHouseId = 1, ExportKind = "NACHA", TotalTransactions = 1
        });
        await test.Context.SaveChangesAsync();
        Assert.Equal("SOURCE_CYCLE_EXPORT_EXISTS", (await test.Service.EvaluateAsync(101)).ReasonCode);
    }

    [Fact]
    public async Task CompleteExportOfAnotherTransactionInSourceCycle_DeniesConservatively()
    {
        await using var test = await Scenario.CreateAsync(MondayEleven);
        test.Context.AchFileExports.Add(new AchFileExport
        {
            AchCycleId = "C1", ClearingHouseId = 1, ExportKind = "NACHA", TotalTransactions = 1,
            Transactions = new List<AchFileExportTransaction>
            {
                new() { AchTransactionId = 999, AchCycleId = "C1", AchBatchId = 51 }
            }
        });
        await test.Context.SaveChangesAsync();
        Assert.Equal("SOURCE_CYCLE_EXPORT_EXISTS", (await test.Service.EvaluateAsync(101)).ReasonCode);
    }

    [Fact]
    public async Task CenitCycleExecution_DeniesEvenWithoutEvidenceForTransaction()
    {
        await using var test = await Scenario.CreateAsync(MondayEleven);
        (await test.Context.ClearingHouses.SingleAsync()).Code = "CENIT";
        test.Context.CenitCycleExecutions.Add(new CenitCycleExecution { AchCycleId = "C1", Status = "Pending" });
        await test.Context.SaveChangesAsync();
        Assert.Equal("CENIT_CYCLE_PROCESSING_EVIDENCE", (await test.Service.EvaluateAsync(101)).ReasonCode);
    }

    [Fact]
    public async Task NonPending_Denies()
    {
        await using var test = await Scenario.CreateAsync(MondayEleven);
        (await test.Context.AchTransactions.SingleAsync()).State = AchTransferStateEnum.Certified;
        await test.Context.SaveChangesAsync();
        Assert.Equal("NOT_PENDING", (await test.Service.EvaluateAsync(101)).ReasonCode);
    }

    [Fact]
    public async Task UnknownClassification_Denies()
    {
        await using var test = await Scenario.CreateAsync(MondayEleven);
        (await test.Context.AchTransactions.SingleAsync()).Direction = AchTransactionDirection.Unknown;
        await test.Context.SaveChangesAsync();
        Assert.Equal("OUT_OF_SCOPE", (await test.Service.EvaluateAsync(101)).ReasonCode);
    }

    [Fact]
    public async Task DifferentValueDate_DeniesWithoutImplicitChange()
    {
        await using var test = await Scenario.CreateAsync(MondayEleven);
        (await test.Context.AchTransactions.SingleAsync()).EffectiveEntryDate = Monday.AddDays(1);
        await test.Context.SaveChangesAsync();
        Assert.Equal("SOURCE_ASSIGNMENT_INCONSISTENT", (await test.Service.EvaluateAsync(101)).ReasonCode);
    }

    private sealed class Scenario : IAsyncDisposable
    {
        private readonly string? _deniedCycle;
        private readonly bool _sourceBusinessDay;
        private readonly TimeProvider _clock;
        public AchDbContext Context { get; }
        public IAchPendingCycleEligibilityService Service { get; }

        private Scenario(AchDbContext context, TimeProvider clock, string? deniedCycle, bool sourceBusinessDay)
        {
            Context = context;
            _clock = clock;
            _deniedCycle = deniedCycle;
            _sourceBusinessDay = sourceBusinessDay;
            var calendar = new Mock<IOperationalCalendarService>();
            calendar.Setup(x => x.IsBusinessDayAsync(It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_sourceBusinessDay);
            var policy = new Mock<ICycleTransactionPolicy>();
            policy.Setup(x => x.EvaluateAsync(It.IsAny<CycleTransactionPolicyRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CycleTransactionPolicyRequest request, CancellationToken _) =>
                    new CycleTransactionPolicyResult(request.CycleName != _deniedCycle,
                        request.CycleName == _deniedCycle ? "DENIED" : "ALLOWED", "", "ACH", null, "MonetaryCredit"));
            Service = new AchPendingCycleEligibilityService(Context, calendar.Object,
                new OperationalCycleWindowResolver(), policy.Object, _clock);
        }

        public static async Task<Scenario> CreateAsync(DateTimeOffset instant, string? deniedCycle = null,
            string assignedCycle = "C1", bool sourceBusinessDay = true)
        {
            var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
                .UseInMemoryDatabase($"ach-cr-i01-{Guid.NewGuid():N}").Options);
            var house = new ClearingHouse { Id = 1, Name = "ACH Colombia", Code = "ACHCOL", OriginCode = "001" };
            context.ClearingHouses.Add(house);
            context.AchCycles.AddRange(
                Cycle("C1", "Ciclo 1", 7, 9),
                Cycle("C2", "Ciclo 2", 9, 12),
                Cycle("C3", "Ciclo 3", 13, 15));
            context.AchBatches.Add(new AchBatch { Id = 51, AchCycleId = assignedCycle, EffectiveEntryDate = Monday });
            context.AchTransactions.Add(new AchTransaction
            {
                Id = 101, AchCycleId = assignedCycle, AchBatchId = 51, State = AchTransferStateEnum.Pending,
                Direction = AchTransactionDirection.Outgoing, Origin = AchTransactionOrigin.Cfa,
                ClassificationStatus = AchTransactionClassificationStatus.Determined,
                Type = TransactionTypeEnum.Credit, EffectiveEntryDate = Monday, TransactionCode = "22",
                Reference = "T101", SourceAccountNumber = "1", DestinationAccountNumber = "2"
            });
            await context.SaveChangesAsync();
            return new Scenario(context, new FixedTimeProvider(instant), deniedCycle, sourceBusinessDay);
        }

        private static AchCycle Cycle(string id, string name, int startHour, int cutoffHour)
            => new()
            {
                Id = id, CycleName = name, ClearingHouseId = 1, ProcessingDate = Monday,
                StartTime = TimeSpan.FromHours(startHour), CutoffTime = TimeSpan.FromHours(cutoffHour),
                EndTime = TimeSpan.FromHours(cutoffHour), OperationalStatus = AchCycleOperationalStatus.Open
            };

        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
