using Cfa.ACHInterbank.Application.ACH.Claims.Models;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Claims;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class ClaimCaseApplicationTests
{
    [Fact]
    public async Task CreateDev14Async_CreatesCaseAndDerivesParticipantsFromTransaction()
    {
        await using var context = CreateContext();
        var seeded = await SeedAsync(context, transactionCount: 1);
        var stateBefore = seeded.Transactions[0].State;
        var (service, calendar) = Service(context, Snapshot(2026, 9, 30, 10, 15), new DateOnly(2026, 12, 29));

        var created = await service.CreateDev14Async(
            new CreateDev14ClaimCaseRequest([seeded.Transactions[0].Id], "EXT-CASE-1"),
            " DEV14-KEY-1 ");

        Assert.Equal(ClaimCauseCode.DEV14, created.CauseCode);
        Assert.Equal(seeded.Requesting.Id, created.RequestingParticipant.Id);
        Assert.Equal(seeded.Responding.Id, created.RespondingParticipant.Id);
        Assert.Single(created.Transactions);
        Assert.Equal("dev14-key-1", (await context.ClaimCases.SingleAsync()).CreationIdempotencyKey);
        Assert.Equal(stateBefore, (await context.AchTransactions.SingleAsync()).State);
        calendar.Verify(x => x.ShiftBusinessDaysAsync(
            new DateOnly(2026, 9, 30), 60, seeded.ClearingHouse.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateDev14Async_AllowsMultipleCompatibleTransactions()
    {
        await using var context = CreateContext();
        var seeded = await SeedAsync(context, transactionCount: 2);
        var (service, _) = Service(context, Snapshot(2026, 9, 30, 10, 15), new DateOnly(2026, 12, 29));

        var created = await service.CreateDev14Async(
            new CreateDev14ClaimCaseRequest(seeded.Transactions.Select(x => x.Id).ToArray()),
            null);

        Assert.Equal(2, created.Transactions.Count);
        Assert.Equal(2, await context.ClaimTransactions.CountAsync());
    }

    [Fact]
    public async Task CreateDev14Async_RejectsTransactionsWithIncompatibleParticipants()
    {
        await using var context = CreateContext();
        var seeded = await SeedAsync(context, transactionCount: 2);
        var other = new FinancialInstitution
        {
            Id = 30,
            Name = "Otra EPR",
            RoutingNumber = "30000000",
            TransitCode = "003"
        };
        context.FinancialInstitutions.Add(other);
        seeded.Transactions[1].DestinationInstitutionId = other.Id;
        seeded.Transactions[1].DestinationInstitution = other;
        await context.SaveChangesAsync();
        var (service, _) = Service(context, Snapshot(2026, 9, 30, 10, 15), new DateOnly(2026, 12, 29));

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateDev14Async(
            new CreateDev14ClaimCaseRequest(seeded.Transactions.Select(x => x.Id).ToArray()),
            null));

        Assert.Contains("solicitante", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateDev14Async_CalculatesBusinessDay60AndPreservesFiledLocalTime()
    {
        await using var context = CreateContext();
        var seeded = await SeedAsync(context, transactionCount: 1);
        var (service, calendar) = Service(context, Snapshot(2026, 9, 30, 10, 37), new DateOnly(2026, 12, 31));

        var created = await service.CreateDev14Async(
            new CreateDev14ClaimCaseRequest([seeded.Transactions[0].Id]),
            null);

        Assert.Equal(new DateTimeOffset(2026, 9, 30, 10, 37, 0, TimeSpan.FromHours(-5)), created.FiledAt);
        Assert.Equal(new DateTimeOffset(2026, 12, 31, 10, 37, 0, TimeSpan.FromHours(-5)), created.DeadlineAt);
        calendar.VerifyAll();
    }

    [Fact]
    public async Task CreateDev14Async_IsIdempotentForSameKeyAndPayload()
    {
        await using var context = CreateContext();
        var seeded = await SeedAsync(context, transactionCount: 1);
        var (service, _) = Service(context, Snapshot(2026, 9, 30, 10, 15), new DateOnly(2026, 12, 29));
        var request = new CreateDev14ClaimCaseRequest([seeded.Transactions[0].Id], "CASE-REF");

        var first = await service.CreateDev14Async(request, "idem-1");
        var second = await service.CreateDev14Async(request, " IDEM-1 ");

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, await context.ClaimCases.CountAsync());
    }

    [Fact]
    public async Task CreateDev14Async_SameKeyDifferentPayloadIsConflict()
    {
        await using var context = CreateContext();
        var seeded = await SeedAsync(context, transactionCount: 2);
        var (service, _) = Service(context, Snapshot(2026, 9, 30, 10, 15), new DateOnly(2026, 12, 29));

        await service.CreateDev14Async(new CreateDev14ClaimCaseRequest([seeded.Transactions[0].Id]), "idem-conflict");

        await Assert.ThrowsAsync<ClaimCaseConflictException>(() => service.CreateDev14Async(
            new CreateDev14ClaimCaseRequest([seeded.Transactions[1].Id]),
            "idem-conflict"));
    }

    [Fact]
    public async Task RecordProgressAndResolve_PersistLifecycleWithoutChangingTransactionState()
    {
        await using var context = CreateContext();
        var seeded = await SeedAsync(context, transactionCount: 1);
        var initialState = seeded.Transactions[0].State;
        var (createService, _) = Service(context, Snapshot(2026, 9, 30, 10, 15), new DateOnly(2026, 12, 29));
        var created = await createService.CreateDev14Async(new CreateDev14ClaimCaseRequest([seeded.Transactions[0].Id]), "create-lifecycle");

        var (progressService, _) = Service(context, Snapshot(2026, 10, 1, 9, 0), new DateOnly(2026, 12, 29));
        var progressed = await progressService.RecordProgressAsync(
            created.Id, new RecordClaimProgressRequest("Primer avance"), "user-1", "progress-1");
        Assert.Equal(ClaimCaseStatus.Open, progressed.Status);
        Assert.Contains(progressed.History, x => x.EventType == ClaimCaseEventType.Progress && x.Actor == "user-1");

        var (resolveService, _) = Service(context, Snapshot(2026, 10, 2, 9, 0), new DateOnly(2026, 12, 29));
        var resolved = await resolveService.ResolveAsync(
            created.Id, new ResolveClaimCaseRequest("Atendido"), "user-1", "resolve-1");

        Assert.Equal(ClaimCaseStatus.Resolved, resolved.Status);
        Assert.Equal("Atendido", resolved.ResolutionNotes);
        Assert.Equal(initialState, (await context.AchTransactions.SingleAsync()).State);
    }

    [Fact]
    public async Task ExpireAsync_RejectsBeforeDeadlineAndAllowsAtDeadline()
    {
        await using var context = CreateContext();
        var seeded = await SeedAsync(context, transactionCount: 1);
        var deadlineDate = new DateOnly(2026, 12, 29);
        var (createService, _) = Service(context, Snapshot(2026, 9, 30, 10, 15), deadlineDate);
        var created = await createService.CreateDev14Async(new CreateDev14ClaimCaseRequest([seeded.Transactions[0].Id]), "expire-create");

        var (earlyService, _) = Service(context, Snapshot(2026, 12, 29, 10, 14), deadlineDate);
        await Assert.ThrowsAsync<InvalidOperationException>(() => earlyService.ExpireAsync(
            created.Id, new ExpireClaimCaseRequest(), "user-1", "expire-early"));

        context.ChangeTracker.Clear();
        var (dueService, _) = Service(context, Snapshot(2026, 12, 29, 10, 15), deadlineDate);
        var expired = await dueService.ExpireAsync(
            created.Id, new ExpireClaimCaseRequest("Venció el plazo"), "user-1", "expire-due");

        Assert.Equal(ClaimCaseStatus.Expired, expired.Status);
        Assert.Contains(expired.History, x => x.EventType == ClaimCaseEventType.Expired);
    }

    [Fact]
    public async Task MutationIdempotency_ReplayReturnsSingleEvent_AndDifferentPayloadConflicts()
    {
        await using var context = CreateContext();
        var seeded = await SeedAsync(context, transactionCount: 1);
        var (createService, _) = Service(context, Snapshot(2026, 9, 30, 10, 15), new DateOnly(2026, 12, 29));
        var created = await createService.CreateDev14Async(new CreateDev14ClaimCaseRequest([seeded.Transactions[0].Id]), "progress-create");
        var (service, _) = Service(context, Snapshot(2026, 10, 1, 9, 0), new DateOnly(2026, 12, 29));

        await service.RecordProgressAsync(created.Id, new RecordClaimProgressRequest("Avance"), "user-1", "event-key");
        var replay = await service.RecordProgressAsync(created.Id, new RecordClaimProgressRequest("Avance"), "user-1", " EVENT-KEY ");

        Assert.Single(replay.History.Where(x => x.EventType == ClaimCaseEventType.Progress));
        await Assert.ThrowsAsync<ClaimCaseConflictException>(() => service.RecordProgressAsync(
            created.Id, new RecordClaimProgressRequest("Otro avance"), "user-1", "event-key"));
    }

    [Fact]
    public async Task GetByTransactionAsync_ReturnsRelatedClaimsOnly()
    {
        await using var context = CreateContext();
        var seeded = await SeedAsync(context, transactionCount: 2);
        var (service, _) = Service(context, Snapshot(2026, 9, 30, 10, 15), new DateOnly(2026, 12, 29));
        var first = await service.CreateDev14Async(new CreateDev14ClaimCaseRequest([seeded.Transactions[0].Id]), "tx-query-1");
        await service.CreateDev14Async(new CreateDev14ClaimCaseRequest([seeded.Transactions[1].Id]), "tx-query-2");

        var result = await service.GetByTransactionAsync(seeded.Transactions[0].Id);

        Assert.Single(result);
        Assert.Equal(first.Id, result[0].Id);
    }

    [Fact]
    public void ClaimCaseService_DoesNotDependOnTransactionStateTransitionOrTraceabilityServices()
    {
        var dependencyTypes = typeof(ClaimCaseService).GetConstructors().Single().GetParameters().Select(x => x.ParameterType.Name).ToArray();

        Assert.DoesNotContain("IAchStateTransitionService", dependencyTypes);
        Assert.DoesNotContain("IAchTraceabilityService", dependencyTypes);
    }

    private static AchDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase($"claims-app-{Guid.NewGuid():N}")
            .Options;
        return new AchDbContext(options);
    }

    private static (ClaimCaseService Service, Mock<IOperationalCalendarService> Calendar) Service(
        AchDbContext context,
        OperationalTimeSnapshot snapshot,
        DateOnly deadlineDate)
    {
        var time = new Mock<IOperationalTimeSnapshotProvider>(MockBehavior.Strict);
        time.Setup(x => x.CaptureNow()).Returns(snapshot);
        var calendar = new Mock<IOperationalCalendarService>(MockBehavior.Strict);
        calendar.Setup(x => x.ShiftBusinessDaysAsync(
                snapshot.OperationalDate,
                60,
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deadlineDate);
        return (new ClaimCaseService(context, time.Object, calendar.Object), calendar);
    }

    private static OperationalTimeSnapshot Snapshot(int year, int month, int day, int hour, int minute)
    {
        var bogota = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
        var utc = bogota.AddHours(5);
        return new OperationalTimeSnapshot(
            DateTime.SpecifyKind(utc, DateTimeKind.Utc),
            bogota,
            new DateOnly(year, month, day),
            "America/Bogota");
    }

    private static async Task<SeededClaimsData> SeedAsync(AchDbContext context, int transactionCount)
    {
        var clearingHouse = new ClearingHouse
        {
            Id = 1,
            Name = "ACH Colombia",
            Code = "ACHCOL",
            OriginCode = "ACH",
            ClearingHouseId = 1
        };
        var requesting = new FinancialInstitution
        {
            Id = 10,
            Name = "EPR",
            RoutingNumber = "10000000",
            TransitCode = "001"
        };
        var responding = new FinancialInstitution
        {
            Id = 20,
            Name = "EPO",
            RoutingNumber = "20000000",
            TransitCode = "002"
        };
        var cycle = new AchCycle
        {
            Id = "ACHCOL-20260930-01",
            CycleName = "Ciclo 1 - ACH Colombia",
            ClearingHouseId = clearingHouse.Id,
            ClearingHouse = clearingHouse,
            ProcessingDate = new DateTime(2026, 9, 30),
            CutoffTime = TimeSpan.FromHours(7),
            StartTime = TimeSpan.FromHours(5),
            EndTime = TimeSpan.FromHours(7),
            OutputReleaseTime = TimeSpan.FromHours(8)
        };

        var transactions = Enumerable.Range(1, transactionCount)
            .Select(i => new AchTransaction
            {
                Id = 500 + i,
                Amount = 100_000m + i,
                Type = TransactionTypeEnum.Debit,
                Reference = $"TX-{500 + i}",
                TransactionExternalId = $"EXT-{500 + i}",
                TraceNumber = $"00000000{500 + i:D7}",
                SourceAccountNumber = "10001",
                DestinationAccountNumber = "20002",
                AchCycleId = cycle.Id,
                AchCycle = cycle,
                AchBatchId = 1,
                SourceInstitutionId = responding.Id,
                SourceInstitution = responding,
                DestinationInstitutionId = requesting.Id,
                DestinationInstitution = requesting
            })
            .ToArray();

        context.AddRange(clearingHouse, requesting, responding, cycle);
        context.AchTransactions.AddRange(transactions);
        await context.SaveChangesAsync();

        return new SeededClaimsData(clearingHouse, requesting, responding, transactions);
    }

    private sealed record SeededClaimsData(
        ClearingHouse ClearingHouse,
        FinancialInstitution Requesting,
        FinancialInstitution Responding,
        AchTransaction[] Transactions);
}
