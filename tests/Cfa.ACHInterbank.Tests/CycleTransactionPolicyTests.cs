using Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class CycleTransactionPolicyTests
{
    private readonly CycleTransactionPolicy _sut = new(
        new ClearingHouseToPaymentRailMapper(),
        new CycleNumberResolver());

    [Fact]
    public void Evaluate_AchColombiaCycleFiveOrdinaryMonetaryDebit_RejectsWithStableReasonCode()
    {
        var result = Evaluate("ACHCOL", "Ciclo 5", TransactionTypeEnum.Debit);

        Assert.False(result.IsAllowed);
        Assert.Equal(CycleTransactionPolicy.OrdinaryDebitNotAllowedReasonCode, result.ReasonCode);
        Assert.Equal(PaymentRailCodes.AchColombia, result.RailCode);
        Assert.Equal("OrdinaryMonetaryDebit", result.FunctionalClass);
    }

    [Theory]
    [InlineData(TransactionTypeEnum.Debit, true, null, null, "Prenotification")]
    [InlineData(TransactionTypeEnum.Credit, false, null, null, "MonetaryCredit")]
    [InlineData(TransactionTypeEnum.Credit, true, null, null, "Prenotification")]
    [InlineData(TransactionTypeEnum.Return, false, "R01", "TRACE", "Return")]
    [InlineData(TransactionTypeEnum.Debit, false, "R01", "TRACE", "Return")]
    [InlineData(TransactionTypeEnum.Debit, true, "R01", "TRACE", "PrenotificationReturn")]
    public void Evaluate_AchColombiaCycleFiveAllowedFunctionalClass_DoesNotApplyDebitBlock(
        TransactionTypeEnum type,
        bool isPrenotification,
        string? returnReason,
        string? originalTrace,
        string expectedFunctionalClass)
    {
        var result = Evaluate("ACHCOL", "Ciclo 5", type, isPrenotification, returnReason, originalTrace);

        Assert.True(result.IsAllowed);
        Assert.Equal(expectedFunctionalClass, result.FunctionalClass);
    }

    [Fact]
    public void Evaluate_AchColombiaCycleFourOrdinaryDebit_DoesNotApplyCycleFiveRule()
        => Assert.True(Evaluate("ACHCOL", "Ciclo 4", TransactionTypeEnum.Debit).IsAllowed);

    [Fact]
    public void Evaluate_CenitCycleFiveOrdinaryDebit_DoesNotInheritAchColombiaRule()
        => Assert.True(Evaluate("CENIT", "Ciclo 5", TransactionTypeEnum.Debit).IsAllowed);

    [Fact]
    public void Evaluate_FutureClearingHouseCycleFiveOrdinaryDebit_DoesNotInheritAchColombiaRule()
        => Assert.True(Evaluate("FUTURE", "Ciclo 5", TransactionTypeEnum.Debit).IsAllowed);

    private CycleTransactionPolicyResult Evaluate(
        string clearingHouseCode,
        string cycleName,
        TransactionTypeEnum type,
        bool isPrenotification = false,
        string? returnReason = null,
        string? originalTrace = null)
        => _sut.Evaluate(new CycleTransactionPolicyRequest(
            clearingHouseCode,
            null,
            cycleName,
            type,
            isPrenotification,
            returnReason,
            originalTrace));
}
