using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.StrategyImplementation;
using Moq;

namespace Cfa.ACHInterbank.Tests;

/// <summary>
/// I09 / HC-CENIT-009 / PT-08: R23 is bounded by the receiver's original
/// operational receipt date, the actual return processing date, and the first
/// Colombian banking business day following recorded receiver notification.
/// These tests require dotnet test; no operator or database certification is implied.
/// </summary>
public sealed class CenitI09R23DeadlineTests
{
    private readonly CenitIncomingReturnPolicy _policy = new();
    private static readonly DateTime OriginalMonday = new(2026, 8, 10);

    private static CenitIncomingReturnPolicyRequest Claim(
        DateTime submitted, DateTime notified,
        DateTime? computedDeadline = null,
        DateTime? returnValueDate = null,
        DateTime? actualProcessingDate = null,
        DateTime? originalReceivedDate = null)
        => new(
            TransactionTypeEnum.Credit, "R23", OriginalMonday,
            returnValueDate ?? submitted, 1, 3, 5, 100m, 100m,
            ReturnRequestDate: notified,
            ImmediateReturnCycleConfirmed: true,
            OriginalReceivedDate: originalReceivedDate ?? OriginalMonday,
            ReceiverNotificationReceivedDate: notified,
            CalculatedNextBusinessDayDeadline: computedDeadline ?? submitted,
            ReturnOperationalDate: actualProcessingDate ?? submitted);

    [Fact]
    public void DayZero_OnlyNextSessionIsAllowed_NotThePriorDayException()
    {
        var request = Claim(OriginalMonday, OriginalMonday) with { ReturnCycleNumber = 2 };
        Assert.Equal("CENIT_R23_SAME_DAY_NEXT_CYCLE_ALLOWED", _policy.Evaluate(request).Code);
        Assert.Equal("CENIT_R23_SAME_DAY_NOT_NEXT_CYCLE",
            _policy.Evaluate(request with { ReturnCycleNumber = 3 }).Code);
    }

    [Theory]
    [InlineData(14, "CENIT_R23_LATER_ALLOWED")]
    [InlineData(15, "CENIT_R23_LATER_ALLOWED")]
    [InlineData(16, "CENIT_R23_MAX_CALENDAR_DAYS_EXCEEDED")]
    public void FifteenCalendarDays_InclusiveFromActualReceipt(int day, string expected)
    {
        var submission = OriginalMonday.AddDays(day);
        var result = _policy.Evaluate(Claim(submission, submission.AddDays(-1)));
        Assert.Equal(expected, result.Code);
    }

    [Fact]
    public void BackdatedReturnValueDate_CannotConcealSubmissionOnDaySixteen()
    {
        var day15 = OriginalMonday.AddDays(15);
        var day16 = OriginalMonday.AddDays(16);
        var request = Claim(day16, day15, day16, returnValueDate: day15);
        Assert.Equal("CENIT_R23_LATER_VALUE_DATE_MISMATCH", _policy.Evaluate(request).Code);
    }

    [Fact]
    public void ForwardDatedReturnValueDate_CannotReplaceActualSubmissionDay()
    {
        var request = Claim(OriginalMonday.AddDays(10), OriginalMonday.AddDays(9),
            returnValueDate: OriginalMonday.AddDays(11));
        Assert.Equal("CENIT_R23_LATER_VALUE_DATE_MISMATCH", _policy.Evaluate(request).Code);
    }

    [Fact]
    public void MissingResolvedOperationalDay_RequiresManualReview()
    {
        var request = Claim(OriginalMonday.AddDays(10), OriginalMonday.AddDays(9))
            with { ReturnOperationalDate = null };
        var result = _policy.Evaluate(request);
        Assert.True(result.RequiresManualReview);
        Assert.Equal("CENIT_R23_RETURN_OPERATIONAL_DATE_REQUIRED", result.Code);
    }

    [Fact]
    public void MissingOriginalReceipt_RequiresManualReviewEvenWithCalendarDeadline()
    {
        var request = Claim(OriginalMonday.AddDays(10), OriginalMonday.AddDays(9))
            with { OriginalReceivedDate = null };
        Assert.Equal("CENIT_R23_ORIGINAL_RECEIPT_REQUIRED", _policy.Evaluate(request).Code);
    }

    [Fact]
    public void MissingNotification_RequiresManualReviewEvenWithSuppliedExternalDeadline()
    {
        var request = Claim(OriginalMonday.AddDays(10), OriginalMonday.AddDays(9)) with
        {
            ReceiverNotificationReceivedDate = null,
            ReceiverRejectionDeadlineDate = OriginalMonday.AddDays(100)
        };
        Assert.Equal("CENIT_R23_NOTIFICATION_RECEIPT_REQUIRED", _policy.Evaluate(request).Code);
    }

    [Fact]
    public void MissingCalendarDeadline_CannotBeReplacedByExternalDeadline()
    {
        var request = Claim(OriginalMonday.AddDays(10), OriginalMonday.AddDays(9)) with
        {
            CalculatedNextBusinessDayDeadline = null,
            ReceiverRejectionDeadlineDate = OriginalMonday.AddDays(100)
        };
        Assert.Equal("CENIT_R23_CALENDAR_VALIDATION_REQUIRED", _policy.Evaluate(request).Code);
    }

    [Fact]
    public void CalendarDeadlineIsInclusiveAndNextBankingDayExceededIsRejected()
    {
        var friday = new DateTime(2026, 8, 14);
        var tuesday = new DateTime(2026, 8, 18); // 17 Aug is holiday in Bogotá.
        Assert.Equal("CENIT_R23_LATER_ALLOWED", _policy.Evaluate(Claim(tuesday, friday, tuesday)).Code);
        Assert.Equal("CENIT_R23_NOTIFICATION_DEADLINE_EXCEEDED",
            _policy.Evaluate(Claim(tuesday.AddDays(1), friday, tuesday) with
                { ReturnRequestDate = tuesday.AddDays(1) }).Code);
    }

    [Fact]
    public void ActualSubmissionAfterDeadline_IsNotDisguisedByEarlierValueDate()
    {
        var friday = new DateTime(2026, 8, 14);
        var tuesday = new DateTime(2026, 8, 18);
        var wednesday = tuesday.AddDays(1);
        var request = Claim(wednesday, friday, tuesday, returnValueDate: tuesday) with
        {
            ReturnRequestDate = tuesday
        };
        Assert.False(_policy.Evaluate(request).IsAllowed);
    }

    [Fact]
    public void OriginalValueDateEqualToReturnValueDate_DoesNotImplyIntradayIfReceivedEarlier()
    {
        // The return value date coincides with the original value date, but the
        // actual original entry was processed the preceding day.  It cannot use
        // the intraday shortcut to avoid notification evidence.
        var returnDay = OriginalMonday.AddDays(1);
        var request = Claim(returnDay, returnDay) with
        {
            OriginalValueDate = returnDay,
            CalculatedNextBusinessDayDeadline = null
        };
        // A pre-value-date original receipt is not recognized as an eligible
        // earlier-day claim under existing CENIT receipt chronology safeguards.
        Assert.False(_policy.Evaluate(request).IsAllowed);
    }

    [Fact]
    public void RecordedNotificationAfterActualSubmission_IsRejected()
    {
        var submission = OriginalMonday.AddDays(10);
        var request = Claim(submission, submission.AddDays(1)) with
        {
            ReturnRequestDate = submission
        };
        Assert.Equal("CENIT_R23_NOTIFICATION_DATE_INVALID", _policy.Evaluate(request).Code);
    }

    [Fact]
    public void UtcAfterMidnight_UsesPreviousBogotaBankingDate_NotTheUtcCalendarDate()
    {
        // 02:00 UTC on Friday 14 August is Thursday 13 August at 21:00 Bogotá.
        // Thursday notification expires Friday 14, not Tuesday 18 (Monday is a holiday).
        var utcNotice = new DateTime(2026, 8, 14, 2, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateOnly(2026, 8, 13),
            CenitR23BankingDate.FromNotificationTimestamp(utcNotice));
        Assert.Equal(new DateOnly(2026, 8, 14),
            CenitR23BankingDate.FromNotificationTimestamp(
                DateTime.SpecifyKind(utcNotice, DateTimeKind.Unspecified)));
        var friday = new DateTime(2026, 8, 14);
        var tuesday = new DateTime(2026, 8, 18);
        Assert.Equal("CENIT_R23_LATER_ALLOWED", _policy.Evaluate(Claim(friday, utcNotice, friday)).Code);
        Assert.Equal("CENIT_R23_NOTIFICATION_DEADLINE_EXCEEDED",
            _policy.Evaluate(Claim(tuesday, utcNotice, friday) with
            {
                ReturnRequestDate = tuesday
            }).Code);
    }

    [Fact]
    public async Task OutboundDeadline_ConvertsUtcNotificationToBogotaBeforeCalendarLookup()
    {
        var notice = new DateTime(2026, 8, 14, 2, 0, 0, DateTimeKind.Utc);
        var origin = new AchCycle { Id = "r23-i09-source-utc", ProcessingDate = OriginalMonday, ClearingHouseId = 9 };
        var destination = new AchCycle { Id = "r23-i09-target-utc", ProcessingDate = new DateTime(2026, 8, 18), ClearingHouseId = 9 };
        var tx = new AchTransaction
        {
            Id = 910, Amount = 100m, Type = TransactionTypeEnum.Credit,
            EffectiveEntryDate = OriginalMonday, AchCycle = origin
        };
        var scope = new AchReturnGenerationScope(origin, destination, null, false, true);
        var correlation = new AchReturnCorrelationResult(scope, new[] { tx.Id },
            new Dictionary<int, AchTransaction> { [tx.Id] = tx },
            new Dictionary<int, AchReturnCenitOriginalSource>(),
            new AchReturnCenitCycleEvidence(1, 3, 5));
        var calendar = new Mock<IOperationalCalendarService>(MockBehavior.Strict);
        calendar.Setup(x => x.GetNextBusinessDayAsync(new DateOnly(2026, 8, 14), 9,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DateOnly(2026, 8, 14));
        var validator = new AchReturnEligibilityPolicyComponent(
            null!, Mock.Of<IAchReturnEligibilityService>(), new CenitIncomingReturnPolicy(), calendar.Object);
        var item = new ReturnSelectionItemDto(tx.Id, "R23", new CenitIncomingReturnOperationalEvidence(
            ReturnRequestDate: destination.ProcessingDate,
            ImmediateReturnCycleConfirmed: true,
            ReceiverNotificationReceivedDate: notice));
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            validator.ValidateCenitOperationalPolicyAsync(tx, scope, correlation, item,
                "R23", 100m, CancellationToken.None));
        Assert.StartsWith("CENIT_R23_NOTIFICATION_DEADLINE_EXCEEDED:", exception.Message);
        calendar.VerifyAll();
    }

    [Fact]
    public void ColombianHolidayCalendar_ContainsEmilianiHolidayAndNewYear()
    {
        var generator = new ColombianHolidayStrategy();
        Assert.Contains(generator.GenerateHolidays(2026), holiday =>
            holiday.Date == new DateOnly(2026, 8, 17));
        Assert.Contains(generator.GenerateHolidays(2027), holiday =>
            holiday.Date == new DateOnly(2027, 1, 1));
    }

    [Theory]
    [InlineData(2026, 8, 14, 2026, 8, 18)]   // Fri + Sat/Sun + Mon holiday
    [InlineData(2026, 12, 31, 2027, 1, 4)]  // Thu + New Year + weekend
    public async Task OutboundCalendarCalculation_UsesNextBankingDayAfterNotification(
        int y, int m, int d, int ey, int em, int ed)
    {
        var notification = new DateTime(y, m, d);
        var expected = new DateOnly(ey, em, ed);
        var start = DateOnly.FromDateTime(notification.AddDays(1));
        var originDay = notification.AddDays(-8);
        var targetDay = expected.ToDateTime(TimeOnly.MinValue);
        var originalCycle = new AchCycle { Id = "origin-i09", ProcessingDate = originDay, ClearingHouseId = 9 };
        var returnCycle = new AchCycle { Id = "return-i09", ProcessingDate = targetDay, ClearingHouseId = 9 };
        var tx = new AchTransaction
        {
            Id = 909, Amount = 100m, Type = TransactionTypeEnum.Credit,
            EffectiveEntryDate = originDay, AchCycle = originalCycle
        };
        var scope = new AchReturnGenerationScope(originalCycle, returnCycle, null, false, true);
        var correlation = new AchReturnCorrelationResult(scope, new[] { tx.Id },
            new Dictionary<int, AchTransaction> { [tx.Id] = tx },
            new Dictionary<int, AchReturnCenitOriginalSource>(),
            new AchReturnCenitCycleEvidence(1, 3, 5));
        var evidence = new CenitIncomingReturnOperationalEvidence(
            ReturnRequestDate: notification,
            ImmediateReturnCycleConfirmed: true,
            ReceiverNotificationReceivedDate: notification,
            ReceiverRejectionDeadlineDate: targetDay.AddDays(40));
        var calendar = new Mock<IOperationalCalendarService>(MockBehavior.Strict);
        calendar.Setup(x => x.GetNextBusinessDayAsync(start, 9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var sut = new AchReturnEligibilityPolicyComponent(
            null!, Mock.Of<IAchReturnEligibilityService>(), new CenitIncomingReturnPolicy(), calendar.Object);
        await sut.ValidateCenitOperationalPolicyAsync(tx, scope, correlation,
            new ReturnSelectionItemDto(tx.Id, "R23", evidence), "R23", 100m, CancellationToken.None);
        calendar.VerifyAll();
    }
}
