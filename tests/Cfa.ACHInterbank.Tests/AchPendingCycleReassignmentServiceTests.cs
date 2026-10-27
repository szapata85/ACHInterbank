using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Repositories.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

/// <summary>
/// I02: pruebas escritas, no ejecutadas en el ambiente actual. InMemory verifica
/// comportamiento funcional; solo un proveedor relacional valida aislamiento/rollback real.
/// </summary>
public sealed class AchPendingCycleReassignmentServiceTests
{
    private static readonly DateTime Monday = new(2026, 10, 26);
    private static readonly DateTimeOffset MondayEleven = new(2026, 10, 26, 16, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task UnexportedPending_MovesWithAuditAndRecomputedBatches()
    {
        await using var fixture = await Fixture.CreateAsync();
        var result = await fixture.Command.ReassignAsync(101);
        Assert.True(result.Moved);
        Assert.Equal("C1", result.FromCycleId);
        Assert.Equal("C2", result.ToCycleId);
        Assert.Equal(51, result.FromBatchId);
        var transaction = await fixture.Context.AchTransactions.SingleAsync();
        Assert.Equal("C2", transaction.AchCycleId);
        Assert.Equal(result.ToBatchId, transaction.AchBatchId);
        Assert.Equal(100m, transaction.Amount);
        Assert.Equal("123456780000001", transaction.TraceNumber);
        Assert.Equal(Monday, transaction.EffectiveEntryDate);
        Assert.Equal(AchTransferStateEnum.Pending, transaction.State);
        Assert.Equal(new DateTime(2026, 10, 24), transaction.StateChangedAtUtc);
        var source = await fixture.Context.AchBatches.SingleAsync(x => x.Id == 51);
        var target = await fixture.Context.AchBatches.SingleAsync(x => x.Id == transaction.AchBatchId);
        Assert.Equal(0m, source.TotalCreditAmount);
        Assert.Equal(100m, target.TotalCreditAmount);
        Assert.Equal("220", target.ServiceClassCode);
        var audit = await fixture.Context.AchTransactionStateEvents.SingleAsync();
        Assert.Equal("CYCLE_REASSIGNED", audit.ReasonCode);
        Assert.Equal(AchTransferStateEnum.Pending, audit.FromState);
        Assert.Equal(AchTransferStateEnum.Pending, audit.ToState);
        Assert.Contains("C1", audit.PayloadJson);
        Assert.Contains("C2", audit.PayloadJson);
        Assert.Equal($"ACH-CR-001:101:C1:C2", audit.IdempotencyKey);
    }

    [Fact]
    public async Task SecondInvocation_DoesNotDuplicateEventsOrBatches()
    {
        await using var fixture = await Fixture.CreateAsync();
        Assert.True((await fixture.Command.ReassignAsync(101)).Moved);
        var second = await fixture.Command.ReassignAsync(101);
        Assert.False(second.Moved);
        Assert.Single(await fixture.Context.AchTransactionStateEvents.ToListAsync());
        Assert.Equal(2, await fixture.Context.AchBatches.CountAsync());
    }

    [Fact]
    public async Task ReusesCompatibleExistingBatch()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Context.AchBatches.Add(new AchBatch
        {
            Id = 52, AchCycleId = "C2", EffectiveEntryDate = Monday,
            CompanyName = "CFA", CompanyIdentification = "123", CompanyEntryDescription = "PAGOS",
            CompanyEntryDescriptionId = 9, OriginOrOdfi = "12345678", ServiceClassCode = "220"
        });
        await fixture.Context.SaveChangesAsync();
        var result = await fixture.Command.ReassignAsync(101);
        Assert.True(result.Moved);
        Assert.Equal(52, result.ToBatchId);
        Assert.Equal(2, await fixture.Context.AchBatches.CountAsync());
    }

    [Fact]
    public async Task IncompatibleTargetBatch_SkipsWithoutMutation()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Context.AchBatches.Add(new AchBatch
        {
            Id = 52, AchCycleId = "C2", EffectiveEntryDate = Monday,
            CompanyName = "CFA", CompanyIdentification = "123", CompanyEntryDescription = "PAGOS",
            CompanyEntryDescriptionId = 9, OriginOrOdfi = "99999999", ServiceClassCode = "220"
        });
        await fixture.Context.SaveChangesAsync();
        Assert.Equal("TARGET_BATCH_INCOMPATIBLE", (await fixture.Command.ReassignAsync(101)).ReasonCode);
        Assert.Equal("C1", (await fixture.Context.AchTransactions.SingleAsync()).AchCycleId);
        Assert.Empty(await fixture.Context.AchTransactionStateEvents.ToListAsync());
    }

    [Fact]
    public async Task MembershipEvidence_NoMoveNoEvent()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Context.AchFileExportTransactions.Add(new AchFileExportTransaction
        {
            AchTransactionId = 101, AchCycleId = "C1", AchBatchId = 51, AchFileExportId = 99
        });
        await fixture.Context.SaveChangesAsync();
        Assert.Equal("EXPORT_EVIDENCE_PRESENT", (await fixture.Command.ReassignAsync(101)).ReasonCode);
        Assert.Equal("C1", (await fixture.Context.AchTransactions.SingleAsync()).AchCycleId);
        Assert.Empty(await fixture.Context.AchTransactionStateEvents.ToListAsync());
    }

    [Fact]
    public async Task NonPending_NoMutation()
    {
        await using var fixture = await Fixture.CreateAsync();
        (await fixture.Context.AchTransactions.SingleAsync()).State = AchTransferStateEnum.Certified;
        await fixture.Context.SaveChangesAsync();
        Assert.Equal("NOT_PENDING", (await fixture.Command.ReassignAsync(101)).ReasonCode);
        Assert.Empty(await fixture.Context.AchTransactionStateEvents.ToListAsync());
    }

    [Fact]
    public async Task BeforeOriginalCutoff_NoMutation()
    {
        await using var fixture = await Fixture.CreateAsync(new DateTimeOffset(2026, 10, 26, 13, 0, 0, TimeSpan.Zero));
        Assert.Equal("CYCLE_NOT_EXPIRED", (await fixture.Command.ReassignAsync(101)).ReasonCode);
        Assert.Equal("C1", (await fixture.Context.AchTransactions.SingleAsync()).AchCycleId);
    }

    [Fact]
    public async Task InvalidId_NoWrite()
    {
        await using var fixture = await Fixture.CreateAsync();
        Assert.Equal("INVALID_TRANSACTION_ID", (await fixture.Command.ReassignAsync(-1)).ReasonCode);
        Assert.Empty(await fixture.Context.AchTransactionStateEvents.ToListAsync());
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public AchDbContext Context { get; }
        public IAchPendingCycleReassignmentService Command { get; }

        private Fixture(AchDbContext context, TimeProvider clock)
        {
            Context = context;
            var calendar = new Mock<IOperationalCalendarService>();
            calendar.Setup(x => x.IsBusinessDayAsync(It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var policy = new Mock<ICycleTransactionPolicy>();
            policy.Setup(x => x.EvaluateAsync(It.IsAny<CycleTransactionPolicyRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((CycleTransactionPolicyRequest request, CancellationToken _) =>
                    new CycleTransactionPolicyResult(true, "ALLOWED", "", "ACH", null, "MonetaryCredit"));
            var eligibility = new AchPendingCycleEligibilityService(context, calendar.Object,
                new OperationalCycleWindowResolver(), policy.Object, clock);
            var batches = new AchBatchRepository(context);
            var repository = new AchTransactionRepository(context);
            var persister = new TransactionPersister(repository, batches, Mock.Of<ITransactionValidator>());
            Command = new AchPendingCycleReassignmentService(context, eligibility, batches, persister, clock);
        }

        public static async Task<Fixture> CreateAsync(DateTimeOffset? instant = null)
        {
            var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
                .UseInMemoryDatabase($"ach-cr-i02-{Guid.NewGuid():N}").Options);
            context.ClearingHouses.Add(new ClearingHouse
                { Id = 1, Name = "ACH Colombia", Code = "ACHCOL", OriginCode = "001" });
            context.AchCycles.AddRange(
                Cycle("C1", "Ciclo 1", 7, 9), Cycle("C2", "Ciclo 2", 9, 12));
            context.AchBatches.Add(new AchBatch
            {
                Id = 51, AchCycleId = "C1", EffectiveEntryDate = Monday,
                CompanyName = "CFA", CompanyIdentification = "123", CompanyEntryDescription = "PAGOS",
                CompanyEntryDescriptionId = 9, OriginOrOdfi = "12345678", ServiceClassCode = "220",
                TotalCreditAmount = 100m
            });
            context.AchTransactions.Add(new AchTransaction
            {
                Id = 101, AchCycleId = "C1", AchBatchId = 51,
                Amount = 100m, TraceNumber = "123456780000001", TraceSequenceNumber = 1,
                State = AchTransferStateEnum.Pending, StateChangedAtUtc = new DateTime(2026, 10, 24),
                Direction = AchTransactionDirection.Outgoing, Origin = AchTransactionOrigin.Cfa,
                ClassificationStatus = AchTransactionClassificationStatus.Determined,
                Type = TransactionTypeEnum.Credit, EffectiveEntryDate = Monday, TransactionCode = "22",
                Reference = "T101", SourceAccountNumber = "1", DestinationAccountNumber = "2"
            });
            await context.SaveChangesAsync();
            return new Fixture(context, new FixedClock(instant ?? MondayEleven));
        }

        private static AchCycle Cycle(string id, string name, int start, int cutoff)
            => new()
            {
                Id = id, CycleName = name, ClearingHouseId = 1, ProcessingDate = Monday,
                StartTime = TimeSpan.FromHours(start), CutoffTime = TimeSpan.FromHours(cutoff),
                EndTime = TimeSpan.FromHours(cutoff), OperationalStatus = AchCycleOperationalStatus.Open
            };

        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
