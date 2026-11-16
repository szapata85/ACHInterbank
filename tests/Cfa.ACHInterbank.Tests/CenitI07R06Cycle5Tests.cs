using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Tests;

/// <summary>
/// I07 / PT-09. Unit cases for C5 R06. Physical NACHA output, admission and
/// official acknowledgments still require .NET and an authorized environment.
/// </summary>
public sealed class CenitI07R06Cycle5Tests
{
    private static readonly DateTime OperationalDay = new(2026, 11, 13);
    private readonly CenitIncomingReturnPolicy _policy = new();

    private static CenitIncomingReturnPolicyRequest R06(
        TransactionTypeEnum transactionType = TransactionTypeEnum.Credit,
        int sourceCycle = 4, int destinationCycle = 5)
        => new(
            transactionType,
            "R06",
            OperationalDay,
            OperationalDay,
            sourceCycle,
            destinationCycle,
            5,
            100m,
            100m,
            ReturnRequestDate: OperationalDay,
            ImmediateReturnCycleConfirmed: true,
            FundsAvailabilityRequired: transactionType == TransactionTypeEnum.Credit,
            FundsAvailabilityConfirmed: transactionType == TransactionTypeEnum.Credit,
            ConfirmationToOriginatorRecorded: true,
            OriginalReceivedDate: OperationalDay,
            ReturnOperationalDate: OperationalDay);

    [Theory]
    [InlineData(TransactionTypeEnum.Credit)]
    [InlineData(TransactionTypeEnum.Debit)]
    public void C4SameDayIntoC5_WithCompleteEvidence_IsAllowed(TransactionTypeEnum type)
    {
        var result = _policy.Evaluate(R06(type));
        Assert.True(result.IsAllowed);
        Assert.Equal("CENIT_R06_ALLOWED", result.Code);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public void NonC4SourceIntoC5_IsRejected_EvenWithFullEvidence(int sourceCycle)
    {
        var result = _policy.Evaluate(R06(sourceCycle: sourceCycle));
        Assert.False(result.IsAllowed);
        Assert.Equal("CENIT_CYCLE5_R06_ORIGIN_C4_REQUIRED", result.Code);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void C4FromAnotherOperationalDay_IsRejected_EvenWithEqualValueDates(int daysDifference)
    {
        var result = _policy.Evaluate(R06() with
        {
            OriginalReceivedDate = OperationalDay.AddDays(daysDifference)
        });
        Assert.False(result.IsAllowed);
        Assert.Equal("CENIT_CYCLE5_R06_ORIGIN_C4_REQUIRED", result.Code);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void MissingVerifiedCycleDate_NeedsManualReview(bool originalKnown, bool returnKnown)
    {
        var request = R06() with
        {
            OriginalReceivedDate = originalKnown ? OperationalDay : null,
            ReturnOperationalDate = returnKnown ? OperationalDay : null
        };
        var result = _policy.Evaluate(request);
        Assert.True(result.RequiresManualReview);
        Assert.Equal("CENIT_CYCLE5_R06_OPERATIONAL_DAY_EVIDENCE_REQUIRED", result.Code);
    }

    [Fact]
    public void ValueDateAgreementDoesNotSubstituteForOperationalEvidence()
    {
        var request = R06() with
        {
            OriginalReceivedDate = OperationalDay.AddDays(-1),
            ReturnOperationalDate = OperationalDay
        };
        Assert.Equal("CENIT_CYCLE5_R06_ORIGIN_C4_REQUIRED", _policy.Evaluate(request).Code);
    }

    [Fact]
    public void ValueDateMismatchRemainsRejectedDespiteMatchingOperationalDates()
    {
        var request = R06() with { ReturnValueDate = OperationalDay.AddDays(1) };
        Assert.Equal("CENIT_CYCLE5_R06_ORIGIN_C4_REQUIRED", _policy.Evaluate(request).Code);
    }

    [Fact]
    public void PartialReturnCannotPassC5OperationalGuard()
    {
        Assert.Equal("CENIT_R06_PARTIAL_OR_AMOUNT_MISMATCH",
            _policy.Evaluate(R06() with { ReturnedAmount = 99m }).Code);
    }

    [Fact]
    public void NonImmediateRequestCannotPassC5OperationalGuard()
    {
        Assert.Equal("CENIT_R06_NOT_IMMEDIATE",
            _policy.Evaluate(R06() with { ImmediateReturnCycleConfirmed = false }).Code);
    }

    [Theory]
    [InlineData(null, "CENIT_R06_FUNDS_APPLICABILITY_REQUIRED")]
    [InlineData(false, "CENIT_R06_FUNDS_NOT_AVAILABLE")]
    public void CreditFundsRulesRemainEnforced(bool? requiredOrAvailable, string expected)
    {
        var request = requiredOrAvailable is null
            ? R06() with { FundsAvailabilityRequired = null }
            : R06() with { FundsAvailabilityConfirmed = requiredOrAvailable };
        Assert.Equal(expected, _policy.Evaluate(request).Code);
    }

    [Fact]
    public void OriginatorConfirmationStillRequired()
    {
        var result = _policy.Evaluate(R06() with { ConfirmationToOriginatorRecorded = false });
        Assert.True(result.RequiresManualReview);
        Assert.Equal("CENIT_R06_CONFIRMATION_REQUIRED", result.Code);
    }

    [Fact]
    public void C1ReturnStillRejectsR06()
    {
        Assert.Equal("CENIT_CYCLE1_RETURN_NOT_ALLOWED",
            _policy.Evaluate(R06(sourceCycle: 4, destinationCycle: 1)).Code);
    }

    [Fact]
    public void C4ReturnBehaviorRemainsUnchangedWhenNoNewC5EvidenceExists()
    {
        var request = R06(sourceCycle: 3, destinationCycle: 4) with
        {
            OriginalReceivedDate = null,
            ReturnOperationalDate = null
        };
        Assert.True(_policy.Evaluate(request).IsAllowed);
    }

    [Fact]
    public void R23DoesNotInheritR06C5AdditionalEvidence()
    {
        var request = R06() with
        {
            ReturnReasonCode = "R23",
            OriginalReceivedDate = null,
            ReturnOperationalDate = null
        };
        Assert.Equal("CENIT_R23_SAME_DAY_NEXT_CYCLE_ALLOWED", _policy.Evaluate(request).Code);
    }
}
