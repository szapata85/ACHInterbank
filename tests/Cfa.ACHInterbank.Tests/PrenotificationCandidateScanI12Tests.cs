using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class PrenotificationCandidateScanI12Tests
{
    private static readonly DateTimeOffset Now =
        new(2026, 11, 3, 20, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task I12_NullStoredDeadlineIsReevaluated_WhenCalendarWasInitiallyIncomplete()
    {
        await using var db = NewContext();
        AddCanonical(db, 1201, null); // the calendar had no last valid cycle
        AddCanonical(db, 1202, Now.AddHours(-1));
        AddCanonical(db, 1203, Now.AddHours(1)); // not due yet
        await db.SaveChangesAsync();

        var due = await PrenotificationCandidateScan.LoadPageAsync(
            db, Now, 0, int.MaxValue, 10);
        Assert.Equal(new[] { 1201, 1202 }, due);
        // This query never applies a state change. All safety gates remain in the job.
        Assert.All(await db.AchTransactions.ToListAsync(),
            t => Assert.Equal(AchTransferStateEnum.Pending, t.State));
    }

    [Fact]
    public async Task I12_KeysetPagesDoNotSkipLegacyOrCanonicalRows_WhenEarlierRowsDisappear()
    {
        await using var db = NewContext();
        AddCanonical(db, 1301, Now.AddMinutes(-1));
        AddLegacy(db, 1302);
        AddCanonical(db, 1303, null);
        AddLegacy(db, 1304);
        await db.SaveChangesAsync();

        var first = await PrenotificationCandidateScan.LoadPageAsync(
            db, Now, 0, int.MaxValue, 2);
        Assert.Equal(new[] { 1301, 1302 }, first);

        // Acceptance removes one of the first page's rows from the pending query.
        (await db.AchTransactions.SingleAsync(t => t.Id == 1301)).State =
            AchTransferStateEnum.AppliedTacitly;
        await db.SaveChangesAsync();
        var next = await PrenotificationCandidateScan.LoadPageAsync(
            db, Now, first[^1], int.MaxValue, 2);
        Assert.Equal(new[] { 1303, 1304 }, next);
    }

    [Fact]
    public async Task I12_RotationWrapsOnce_AndDoesNotRevisitAlreadyScannedHigherIds()
    {
        await using var db = NewContext();
        foreach (var id in new[] { 1401, 1402, 1403, 1404 }) AddLegacy(db, id);
        await db.SaveChangesAsync();
        var firstPass = await PrenotificationCandidateScan.LoadPageAsync(
            db, Now, 1402, int.MaxValue, 20);
        var wrapPass = await PrenotificationCandidateScan.LoadPageAsync(
            db, Now, 0, 1402, 20);
        Assert.Equal(new[] { 1403, 1404 }, firstPass);
        Assert.Equal(new[] { 1401, 1402 }, wrapPass);
        Assert.Equal(4, firstPass.Concat(wrapPass).Distinct().Count());
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("not-an-id", 0)]
    [InlineData("-10", 0)]
    [InlineData("0", 0)]
    [InlineData("1403", 1403)]
    public void I12_CursorParsing_FailsSafe(string? raw, int expected)
        => Assert.Equal(expected, PrenotificationCandidateScan.ParseCursor(raw));

    private static void AddLegacy(AchDbContext db, int id) =>
        db.AchTransactions.Add(new AchTransaction
        {
            Id = id, IsPrenotification = true, State = AchTransferStateEnum.Pending,
            TraceNumber = $"0000100700{id:D5}", TransactionCode = "23",
            Type = TransactionTypeEnum.Credit, Amount = 0m
        });

    private static void AddCanonical(AchDbContext db, int id, DateTimeOffset? deadline)
    {
        AddLegacy(db, id);
        var rule = new ClearingHouseTransactionRule
        {
            Id = id, ClearingHouseId = 1, TacitAcceptanceEnabled = true,
            EffectiveFrom = new DateTime(2026, 1, 1), NormativeReference = "ACHCOL-7C-20261014"
        };
        var validation = new ThirdPartyAccountValidation
        {
            Id = id, ThirdPartyAccountId = id, ClearingHouseId = 1,
            PolicyRuleId = id, PolicyRule = rule,
            Status = ThirdPartyAccountValidationStatus.PendingResponse
        };
        db.PrenotificationValidationAttempts.Add(new PrenotificationValidationAttempt
        {
            Id = id, AchTransactionId = id, ThirdPartyAccountValidationId = id,
            ThirdPartyAccountValidation = validation,
            Status = PrenotificationValidationAttemptStatus.AwaitingResponse,
            Resolution = PrenotificationValidationResolution.None,
            ResponseDeadlineAtUtc = deadline
        });
    }

    private static AchDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options) { AuditEnabled = false };
}
