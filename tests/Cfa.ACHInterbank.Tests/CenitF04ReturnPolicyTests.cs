using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Tests;

public class CenitF04ReturnPolicyTests
{
    private readonly CenitIncomingReturnPolicy _policy = new();
    private static readonly DateTime Monday = new(2026, 8, 10);

    private static CenitIncomingReturnPolicyRequest R23(
        DateTime returnDate, int originCycle = 1, int returnCycle = 3,
        DateTime? notification = null, DateTime? calculated = null,
        DateTime? externalDeadline = null, DateTime? receipt = null,
        bool immediate = true)
        => new(TransactionTypeEnum.Credit, "R23", Monday, returnDate,
            originCycle, returnCycle, 5, 100m, 100m,
            ReturnRequestDate: notification ?? returnDate,
            ImmediateReturnCycleConfirmed: immediate,
            ReceiverRejectionDeadlineDate: externalDeadline,
            OriginalReceivedDate: receipt ?? Monday,
            ReceiverNotificationReceivedDate: notification,
            CalculatedNextBusinessDayDeadline: calculated);

    [Theory]
    [InlineData(1, 2, true)]
    [InlineData(2, 3, true)]
    [InlineData(4, 5, true)]
    [InlineData(1, 3, false)]
    [InlineData(2, 5, false)]
    [InlineData(4, 4, false)]
    public void R23SameDay_OnlyFollowingCycleUsesOrdinaryRule(int original, int target, bool permitted)
    {
        var result = _policy.Evaluate(R23(Monday, original, target));
        Assert.Equal(permitted, result.IsAllowed);
        Assert.Equal(permitted ? "CENIT_R23_SAME_DAY_NEXT_CYCLE_ALLOWED" : "CENIT_R23_SAME_DAY_NOT_NEXT_CYCLE", result.Code);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void R06FifthCycle_DisallowsOtherSourceCyclesEvenWithOperationalProof(int origin)
    {
        var result = _policy.Evaluate(new CenitIncomingReturnPolicyRequest(
            TransactionTypeEnum.Credit, "R06", Monday, Monday,
            origin, 5, 5, 100, 100,
            ReturnRequestDate: Monday, ImmediateReturnCycleConfirmed: true,
            FundsAvailabilityRequired: true, FundsAvailabilityConfirmed: true,
            ConfirmationToOriginatorRecorded: true));
        Assert.Equal("CENIT_CYCLE5_R06_ORIGIN_C4_REQUIRED", result.Code);
    }

    [Theory]
    [InlineData(TransactionTypeEnum.Credit)]
    [InlineData(TransactionTypeEnum.Debit)]
    public void R06FifthCycle_C4WithCompleteEvidenceAllowed(TransactionTypeEnum type)
    {
        var result = _policy.Evaluate(new CenitIncomingReturnPolicyRequest(
            type, "R06", Monday, Monday, 4, 5, 5, 100, 100,
            ReturnRequestDate: Monday, ImmediateReturnCycleConfirmed: true,
            FundsAvailabilityRequired: type == TransactionTypeEnum.Credit ? true : null,
            FundsAvailabilityConfirmed: type == TransactionTypeEnum.Credit ? true : null,
            ConfirmationToOriginatorRecorded: true));
        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void R23Later_ExternalDeadlineCannotAuthorizeAfterCalculatedDeadline()
    {
        var thursday = new DateTime(2026, 8, 13);
        var result = _policy.Evaluate(R23(thursday,
            notification: Monday.AddDays(1), calculated: Monday.AddDays(2),
            externalDeadline: Monday.AddDays(30)));
        Assert.Equal("CENIT_R23_NOTIFICATION_DEADLINE_EXCEEDED", result.Code);
    }

    [Fact]
    public void R23Later_RequiresIndependentCalculatedDeadlineEvenWithExternalDeadline()
    {
        var result = _policy.Evaluate(R23(Monday.AddDays(2),
            notification: Monday.AddDays(1), externalDeadline: Monday.AddDays(50)));
        Assert.True(result.RequiresManualReview);
        Assert.Equal("CENIT_R23_CALENDAR_VALIDATION_REQUIRED", result.Code);
    }

    [Fact]
    public void R23Later_RequiresNotificationReceiptEvidence()
    {
        var result = _policy.Evaluate(R23(Monday.AddDays(1),
            calculated: Monday.AddDays(2), externalDeadline: Monday.AddDays(40)));
        Assert.Equal("CENIT_R23_NOTIFICATION_RECEIPT_REQUIRED", result.Code);
    }

    [Fact]
    public void R23Later_RequiresOriginalReceiptEvidence()
    {
        var request = R23(Monday.AddDays(1), notification: Monday.AddDays(1),
            calculated: Monday.AddDays(2)) with { OriginalReceivedDate = null };
        Assert.Equal("CENIT_R23_ORIGINAL_RECEIPT_REQUIRED", _policy.Evaluate(request).Code);
    }

    [Fact]
    public void R23Later_CalculatedDeadlineIsInclusive()
    {
        var friday = new DateTime(2026, 8, 14);
        // With Monday 2026-08-17 a national holiday, next business day is Tuesday 18.
        // The date is calculated by IOperationalCalendarService in the production callers.
        var tuesday = new DateTime(2026, 8, 18);
        var result = _policy.Evaluate(R23(tuesday,
            notification: friday, calculated: tuesday));
        Assert.Equal("CENIT_R23_LATER_ALLOWED", result.Code);
    }

    [Fact]
    public void R23Later_RejectsDay16EvenWithLenientExternalDeadline()
    {
        var day16 = Monday.AddDays(16);
        Assert.Equal("CENIT_R23_MAX_CALENDAR_DAYS_EXCEEDED",
            _policy.Evaluate(R23(day16, notification: day16,
                calculated: day16.AddDays(1), externalDeadline: day16.AddDays(40))).Code);
    }

    [Fact]
    public void R23Later_ReturnDateCannotPrecedeNotification()
    {
        var result = _policy.Evaluate(R23(Monday.AddDays(1),
            notification: Monday.AddDays(2), calculated: Monday.AddDays(3)));
        Assert.Equal("CENIT_R23_REQUEST_DATE_INVALID", result.Code);
    }

    [Fact]
    public void R06FifthCycle_C4PriorBusinessDayDoesNotOverrideWindow()
    {
        var result = _policy.Evaluate(new CenitIncomingReturnPolicyRequest(
            TransactionTypeEnum.Credit, "R06", Monday, Monday.AddDays(1),
            4, 5, 5, 100, 100, ReturnRequestDate: Monday.AddDays(1),
            ImmediateReturnCycleConfirmed: true, ConfirmationToOriginatorRecorded: true,
            FundsAvailabilityRequired: true, FundsAvailabilityConfirmed: true));
        Assert.Equal("CENIT_CYCLE5_R06_ORIGIN_C4_REQUIRED", result.Code);
    }
}
