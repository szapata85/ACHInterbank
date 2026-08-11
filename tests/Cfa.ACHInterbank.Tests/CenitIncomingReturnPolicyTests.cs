using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Tests;

public class CenitIncomingReturnPolicyTests
{
    private readonly CenitIncomingReturnPolicy _sut = new();

    public static TheoryData<string, bool, bool, bool, bool> CauseApplicability => new()
    {
        { "R01", false, true, false, false },
        { "R02", true, true, true, true },
        { "R03", true, true, true, true },
        { "R04", true, true, true, true },
        { "R06", false, true, false, true },
        { "R07", false, true, false, false },
        { "R08", false, true, false, false },
        { "R09", false, true, false, false },
        { "R10", false, true, false, false },
        { "R12", false, true, false, false },
        { "R13", false, true, false, false },
        { "R14", true, true, false, false },
        { "R15", true, true, false, false },
        { "R16", true, true, true, true },
        { "R17", true, true, true, true },
        { "R20", true, true, true, true },
        { "R23", false, false, false, true },
        { "R29", true, true, false, false },
        { "R31", true, false, false, false },
        { "R32", false, false, false, true },
        { "R33", false, true, false, true },
        { "R34", true, true, true, true },
        { "R35", true, true, true, true }
    };

    [Theory]
    [MemberData(nameof(CauseApplicability))]
    public void CauseCatalog_ShouldMatchCenitAnnexA(
        string code,
        bool debitPrenote,
        bool debitMonetary,
        bool creditPrenote,
        bool creditMonetary)
    {
        var cause = Assert.Single(CenitIncomingReturnPolicy.CauseDefinitions, x => x.Code == code);

        Assert.Equal(debitPrenote, cause.AppliesToDebitPrenotification);
        Assert.Equal(debitMonetary, cause.AppliesToDebitMonetary);
        Assert.Equal(creditPrenote, cause.AppliesToCreditPrenotification);
        Assert.Equal(creditMonetary, cause.AppliesToCreditMonetary);
    }

    [Fact]
    public void OrdinaryCredit_ShouldRequireSameValueDateAndImmediateCycle()
    {
        Assert.True(_sut.Evaluate(Request(TransactionTypeEnum.Credit, "R32", cycle: 2)).IsAllowed);
        Assert.Equal("CENIT_ORDINARY_RETURN_VALUE_DATE_MISMATCH",
            _sut.Evaluate(Request(TransactionTypeEnum.Credit, "R32", cycle: 2, returnDate: ValueDate.AddDays(1))).Code);
        Assert.Equal("CENIT_ORDINARY_RETURN_NOT_NEXT_CYCLE",
            _sut.Evaluate(Request(TransactionTypeEnum.Credit, "R32", cycle: 3)).Code);
    }

    [Fact]
    public void OrdinaryDebit_ShouldNotUseFourCycleWindow()
    {
        Assert.True(_sut.Evaluate(Request(TransactionTypeEnum.Debit, "R01", originalCycle: 4, cycle: 5)).IsAllowed);
        Assert.Equal("CENIT_ORDINARY_RETURN_NOT_NEXT_CYCLE",
            _sut.Evaluate(Request(TransactionTypeEnum.Debit, "R01", originalCycle: 1, cycle: 5)).Code);
    }

    [Fact]
    public void Prenotification_ShouldRequireSameValueDateAndLastDailyReturnCycleAtMost()
    {
        var allowed = _sut.Evaluate(Request(
            TransactionTypeEnum.Prenotification,
            "R02",
            cycle: 5,
            prenoteDirection: CenitPrenotificationDirection.Credit));
        var late = _sut.Evaluate(Request(
            TransactionTypeEnum.Prenotification,
            "R02",
            cycle: 6,
            prenoteDirection: CenitPrenotificationDirection.Credit));

        Assert.True(allowed.IsAllowed);
        Assert.Equal("CENIT_RETURN_CYCLE_INVALID", late.Code);
    }

    [Fact]
    public void PrenotificationDirectionSpecificCause_ShouldRequireNormalizedDirection()
    {
        var unknown = _sut.Evaluate(Request(TransactionTypeEnum.Prenotification, "R31", cycle: 2));
        var debit = _sut.Evaluate(Request(
            TransactionTypeEnum.Prenotification,
            "R31",
            cycle: 2,
            prenoteDirection: CenitPrenotificationDirection.Debit));

        Assert.True(unknown.RequiresManualReview);
        Assert.Equal("CENIT_PRENOTE_DIRECTION_REQUIRED", unknown.Code);
        Assert.True(debit.IsAllowed);
    }

    [Fact]
    public void R06_ShouldRequireOriginalAmountAndOperationalEvidence()
    {
        var partial = _sut.Evaluate(Request(TransactionTypeEnum.Credit, "R06", cycle: 3, returnedAmount: 99m));
        var missingEvidence = _sut.Evaluate(Request(TransactionTypeEnum.Credit, "R06", cycle: 3));
        var allowed = _sut.Evaluate(Request(
            TransactionTypeEnum.Credit,
            "R06",
            cycle: 3,
            returnRequestDate: ValueDate,
            immediateReturnCycle: true,
            fundsRequired: true,
            fundsAvailable: true,
            originatorConfirmation: true));

        Assert.Equal("CENIT_R06_PARTIAL_OR_AMOUNT_MISMATCH", partial.Code);
        Assert.True(missingEvidence.RequiresManualReview);
        Assert.True(allowed.IsAllowed);
    }

    [Fact]
    public void R06_Debit_ShouldNotInventFundsAvailabilityRequirement()
    {
        var result = _sut.Evaluate(Request(
            TransactionTypeEnum.Debit,
            "R06",
            cycle: 3,
            returnRequestDate: ValueDate,
            immediateReturnCycle: true,
            originatorConfirmation: true));

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public void R23_ShouldSeparateSameDayFromLaterReceiverRejection()
    {
        var sameDay = _sut.Evaluate(Request(
            TransactionTypeEnum.Credit,
            "R23",
            cycle: 2,
            returnRequestDate: ValueDate,
            immediateReturnCycle: true));
        var later = _sut.Evaluate(Request(
            TransactionTypeEnum.Credit,
            "R23",
            cycle: 3,
            returnDate: ValueDate.AddDays(10),
            returnRequestDate: ValueDate.AddDays(10),
            immediateReturnCycle: true,
            receiverRejectionDeadline: ValueDate.AddDays(11)));
        var noNotificationEvidence = _sut.Evaluate(Request(
            TransactionTypeEnum.Credit,
            "R23",
            cycle: 3,
            returnDate: ValueDate.AddDays(10),
            returnRequestDate: ValueDate.AddDays(10),
            immediateReturnCycle: true));
        var overFifteenDays = _sut.Evaluate(Request(
            TransactionTypeEnum.Credit,
            "R23",
            cycle: 3,
            returnDate: ValueDate.AddDays(16),
            returnRequestDate: ValueDate.AddDays(16),
            immediateReturnCycle: true,
            receiverRejectionDeadline: ValueDate.AddDays(17)));

        Assert.True(sameDay.IsAllowed);
        Assert.True(later.IsAllowed);
        Assert.True(noNotificationEvidence.RequiresManualReview);
        Assert.Equal("CENIT_R23_MAX_CALENDAR_DAYS_EXCEEDED", overFifteenDays.Code);
        Assert.Equal("CENIT_R23_NOT_IMMEDIATE", _sut.Evaluate(Request(
            TransactionTypeEnum.Credit,
            "R23",
            returnRequestDate: ValueDate,
            immediateReturnCycle: false)).Code);
    }

    [Fact]
    public void Dxx_ShouldRemainFileRejection_NotTransactionalReturn()
    {
        var result = _sut.Evaluate(Request(TransactionTypeEnum.Credit, "D01", cycle: 2));

        Assert.False(result.IsAllowed);
        Assert.Equal("CENIT_FILE_REJECTION_NOT_RETURN", result.Code);
    }

    private static readonly DateTime ValueDate = new(2026, 8, 10);

    private static CenitIncomingReturnPolicyRequest Request(
        TransactionTypeEnum type,
        string code,
        int originalCycle = 1,
        int cycle = 2,
        DateTime? returnDate = null,
        decimal returnedAmount = 100m,
        CenitPrenotificationDirection prenoteDirection = CenitPrenotificationDirection.Unknown,
        DateTime? returnRequestDate = null,
        bool? immediateReturnCycle = null,
        bool? fundsRequired = null,
        bool? fundsAvailable = null,
        bool? originatorConfirmation = null,
        DateTime? receiverRejectionDeadline = null)
        => new(
            type,
            code,
            ValueDate,
            returnDate ?? ValueDate,
            originalCycle,
            cycle,
            5,
            100m,
            returnedAmount,
            prenoteDirection,
            returnRequestDate,
            immediateReturnCycle,
            fundsRequired,
            fundsAvailable,
            originatorConfirmation,
            receiverRejectionDeadline);
}
