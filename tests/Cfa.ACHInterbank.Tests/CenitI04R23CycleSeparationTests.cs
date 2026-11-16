using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Tests;

/// <summary>
/// I04 / PT-07 and PT-17: executable policy scenarios (require dotnet test).
/// In particular, an earlier value date must never forge a prior-day R23 exception.
/// These tests do not substitute for generation of a physical NACHA-M file or operator ACK.
/// </summary>
public sealed class CenitI04R23CycleSeparationTests
{
    private readonly CenitIncomingReturnPolicy _policy = new();
    private static readonly DateTime Monday = new(2026, 8, 10);
    private static readonly DateTime Tuesday = new(2026, 8, 11);

    private static CenitIncomingReturnPolicyRequest Request(
        DateTime originalValueDate, DateTime receivedDate, DateTime returnedDate,
        int originalCycle, int returnCycle, bool? immediate = true,
        DateTime? notification = null, DateTime? computedDeadline = null)
        => new(
            TransactionTypeEnum.Credit, "R23", originalValueDate, returnedDate,
            originalCycle, returnCycle, 5, 100m, 100m,
            ReturnRequestDate: returnedDate,
            ImmediateReturnCycleConfirmed: immediate,
            OriginalReceivedDate: receivedDate,
            ReceiverNotificationReceivedDate: notification,
            CalculatedNextBusinessDayDeadline: computedDeadline,
            ReturnOperationalDate: returnedDate);

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    [InlineData(3, 4)]
    [InlineData(4, 5)]
    public void SameDay_R23_AcceptsOnlyImmediatelyFollowingSession(int originalCycle, int nextCycle)
    {
        var result = _policy.Evaluate(Request(Monday, Monday, Monday, originalCycle, nextCycle));
        Assert.True(result.IsAllowed);
        Assert.Equal("CENIT_R23_SAME_DAY_NEXT_CYCLE_ALLOWED", result.Code);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(1, 3)]
    [InlineData(2, 5)]
    [InlineData(4, 4)]
    public void SameDay_R23_RejectsSameOrSkippedSessions(int originalCycle, int returnCycle)
    {
        var result = _policy.Evaluate(Request(Monday, Monday, Monday, originalCycle, returnCycle));
        Assert.False(result.IsAllowed);
        Assert.Equal(returnCycle == 1
            ? "CENIT_CYCLE1_R23_PRIOR_DAY_REQUIRED"
            : "CENIT_R23_SAME_DAY_NOT_NEXT_CYCLE", result.Code);
    }

    [Fact]
    public void C1_PriorValueDateButReceivedToday_CannotUsePriorDayException()
    {
        // Pre-I04, the earlier value date alone could authorize this as a later R23.
        var result = _policy.Evaluate(Request(Monday, Tuesday, Tuesday, 1, 1,
            notification: Tuesday, computedDeadline: Tuesday.AddDays(1)));
        Assert.False(result.IsAllowed);
        Assert.Equal("CENIT_CYCLE1_R23_PRIOR_DAY_REQUIRED", result.Code);
    }

    [Fact]
    public void C2_PriorValueDateButReceivedToday_DoesNotUseLaterClaimPath()
    {
        var result = _policy.Evaluate(Request(Monday, Tuesday, Tuesday, 1, 2,
            notification: Tuesday, computedDeadline: Tuesday.AddDays(1)));
        Assert.False(result.IsAllowed);
        Assert.Equal("CENIT_R23_SAME_DAY_VALUE_DATE_MISMATCH", result.Code);
    }

    [Fact]
    public void C4_PriorValueDateReceivedToday_CannotUseSkippedNextCycle()
    {
        var result = _policy.Evaluate(Request(Monday, Tuesday, Tuesday, 1, 4,
            notification: Tuesday, computedDeadline: Tuesday.AddDays(1)));
        Assert.False(result.IsAllowed);
        Assert.Equal("CENIT_R23_SAME_DAY_VALUE_DATE_MISMATCH", result.Code);
    }

    [Fact]
    public void C1_GenuinePriorDayReceiptAndReceiverClaim_IsStillAllowed()
    {
        var result = _policy.Evaluate(Request(Monday, Monday, Tuesday, 4, 1,
            notification: Tuesday, computedDeadline: Tuesday.AddDays(1)));
        Assert.True(result.IsAllowed);
        Assert.Equal("CENIT_R23_LATER_ALLOWED", result.Code);
    }

    [Fact]
    public void C5_GenuinePriorDayReceiptAndReceiverClaim_IsStillAllowed()
    {
        var result = _policy.Evaluate(Request(Monday, Monday, Tuesday, 1, 5,
            notification: Tuesday, computedDeadline: Tuesday.AddDays(1)));
        Assert.True(result.IsAllowed);
        Assert.Equal("CENIT_R23_LATER_ALLOWED", result.Code);
    }

    [Fact]
    public void PriorDayR23_MissingReceiptEvidence_RemainsManualReview()
    {
        var request = Request(Monday, Monday, Tuesday, 4, 1,
            notification: Tuesday, computedDeadline: Tuesday.AddDays(1)) with
        {
            OriginalReceivedDate = null
        };
        var result = _policy.Evaluate(request);
        Assert.True(result.RequiresManualReview);
        Assert.Equal("CENIT_R23_ORIGINAL_RECEIPT_REQUIRED", result.Code);
    }

    [Fact]
    public void SameDayR23_MissingImmediateEvidence_RemainsManualReview()
    {
        var result = _policy.Evaluate(Request(Monday, Monday, Monday, 1, 2, immediate: null));
        Assert.True(result.RequiresManualReview);
        Assert.Equal("CENIT_R23_REQUEST_EVIDENCE_REQUIRED", result.Code);
    }

    [Fact]
    public void ForgedOriginalCycleOutsideOperationalWindow_IsRejected()
    {
        var result = _policy.Evaluate(Request(Monday, Monday, Tuesday, 6, 1,
            notification: Tuesday, computedDeadline: Tuesday.AddDays(1)));
        Assert.False(result.IsAllowed);
        Assert.Equal("CENIT_RETURN_CYCLE_INVALID", result.Code);
    }

    [Fact]
    public void SameDayR23_PartialReturnIsRejectedBeforeSessionRules()
    {
        var request = Request(Monday, Monday, Monday, 1, 2) with { ReturnedAmount = 99m };
        Assert.Equal("CENIT_R23_PARTIAL_OR_AMOUNT_MISMATCH", _policy.Evaluate(request).Code);
    }

    [Fact]
    public void PriorDayR23_DoesNotTrustSuppliedReceiverDeadline()
    {
        var request = Request(Monday, Monday, Tuesday.AddDays(2), 1, 3,
            notification: Tuesday, computedDeadline: Tuesday.AddDays(1)) with
        {
            ReceiverRejectionDeadlineDate = Tuesday.AddDays(30),
            ReturnRequestDate = Tuesday.AddDays(2)
        };
        Assert.Equal("CENIT_R23_NOTIFICATION_DEADLINE_EXCEEDED", _policy.Evaluate(request).Code);
    }
}
