using Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Tests;

public sealed class CycleTransactionPolicyTests
{
    [Fact]
    public async Task DifferentChamberCounts_ResolveIndependently()
    {
        var resolver = new StubPolicyResolver(
            Policy(1, "ACHCOL", "ACH-V1", 7),
            Policy(2, "CENIT", "CENIT-V1", 5));

        Assert.Equal(7, (await resolver.ResolveAsync(1, DateTime.Today)).Cycles.Count);
        Assert.Equal(5, (await resolver.ResolveAsync(2, DateTime.Today)).Cycles.Count);
    }

    [Fact]
    public async Task SameCycleNumber_DifferentEligibility_DoesNotLeakAcrossChambers()
    {
        var ach = Policy(1, "ACHCOL", "ACH-V1", 7);
        ach.Cycles.Single(cycle => cycle.CycleName == "Ciclo 1").AllowsReturn = true;
        var cenit = Policy(2, "CENIT", "CENIT-V1", 5);
        cenit.Cycles.Single(cycle => cycle.CycleName == "Ciclo 1").AllowsReturn = false;
        var sut = CreateSut(new StubPolicyResolver(ach, cenit));

        var achDecision = await EvaluateAsync(sut, 1, 101, "ACHCOL", "Ciclo 1", TransactionTypeEnum.Return);
        var cenitDecision = await EvaluateAsync(sut, 2, 201, "CENIT", "Ciclo 1", TransactionTypeEnum.Return);

        Assert.True(achDecision.IsAllowed);
        Assert.False(cenitDecision.IsAllowed);
        Assert.Equal(CycleTransactionPolicy.NotAllowedReasonCode, cenitDecision.ReasonCode);
    }

    [Theory]
    [InlineData(TransactionTypeEnum.Credit, false, "MonetaryCredit")]
    [InlineData(TransactionTypeEnum.Debit, false, "MonetaryDebit")]
    [InlineData(TransactionTypeEnum.Credit, true, "CreditPrenotification")]
    [InlineData(TransactionTypeEnum.Debit, true, "DebitPrenotification")]
    [InlineData(TransactionTypeEnum.Return, false, "Return")]
    public async Task ExistingTaxonomy_MapsToConfiguredEligibility(
        TransactionTypeEnum type,
        bool prenotification,
        string expectedClass)
    {
        var policy = Policy(1, "ACHCOL", "V1", 1);
        var sut = CreateSut(new StubPolicyResolver(policy));

        var result = await EvaluateAsync(sut, 1, 101, "ACHCOL", "Ciclo 1", type, prenotification);

        Assert.True(result.IsAllowed);
        Assert.Equal(expectedClass, result.FunctionalClass);
    }

    [Fact]
    public async Task AmbiguousPrenotificationDirection_FailsClosed()
    {
        var sut = CreateSut(new StubPolicyResolver(Policy(1, "ACHCOL", "V1", 1)));

        var result = await EvaluateAsync(
            sut,
            1,
            101,
            "ACHCOL",
            "Ciclo 1",
            TransactionTypeEnum.Prenotification,
            true);

        Assert.False(result.IsAllowed);
        Assert.Equal(CycleTransactionPolicy.AmbiguousClassReasonCode, result.ReasonCode);
    }

    [Theory]
    [InlineData("23", true, "CreditPrenotification")]
    [InlineData("28", false, "DebitPrenotification")]
    public async Task PersistedPrenotification_UsesExistingTransactionCodeTaxonomy(
        string transactionCode,
        bool expectedAllowed,
        string expectedClass)
    {
        var policy = Policy(1, "ACHCOL", "V1", 1);
        policy.Cycles[0].AllowsCreditPrenotification = true;
        policy.Cycles[0].AllowsDebitPrenotification = false;
        var sut = CreateSut(new StubPolicyResolver(policy));

        var result = await EvaluateAsync(
            sut,
            1,
            101,
            "ACHCOL",
            "Ciclo 1",
            TransactionTypeEnum.Prenotification,
            true,
            transactionCode);

        Assert.Equal(expectedAllowed, result.IsAllowed);
        Assert.Equal(expectedClass, result.FunctionalClass);
    }

    private static CycleTransactionPolicy CreateSut(IClearingHouseCyclePolicyResolver resolver)
        => new(resolver, new ClearingHouseToPaymentRailMapper(), new CycleNumberResolver());

    private static Task<CycleTransactionPolicyResult> EvaluateAsync(
        CycleTransactionPolicy sut,
        int clearingHouseId,
        int configId,
        string clearingHouseCode,
        string cycleName,
        TransactionTypeEnum type,
        bool prenotification = false,
        string? transactionCode = null)
        => sut.EvaluateAsync(new CycleTransactionPolicyRequest(
            clearingHouseId,
            configId,
            new DateTime(2026, 8, 25),
            clearingHouseCode,
            null,
            cycleName,
            type,
            prenotification,
            TransactionCode: transactionCode));

    private static ResolvedClearingHouseCyclePolicy Policy(
        int clearingHouseId,
        string code,
        string version,
        int cycleCount)
    {
        var cycles = Enumerable.Range(1, cycleCount)
            .Select(number => new ClearingHouseCycleConfig
            {
                Id = clearingHouseId * 100 + number,
                ClearingHouseId = clearingHouseId,
                PolicyVersion = version,
                CycleName = $"Ciclo {number}",
                StartTime = TimeSpan.FromHours(number),
                CutoffTime = TimeSpan.FromHours(number + 1),
                EndTime = TimeSpan.FromHours(number + 1),
                OutputReleaseTime = TimeSpan.FromHours(number + 1),
                EffectiveFrom = new DateTime(2026, 1, 1),
                IsActive = true
            })
            .ToList();
        return new(clearingHouseId, code, version, new DateTime(2026, 8, 25), "America/Bogota", cycles);
    }

    private sealed class StubPolicyResolver(params ResolvedClearingHouseCyclePolicy[] policies)
        : IClearingHouseCyclePolicyResolver
    {
        public Task<ResolvedClearingHouseCyclePolicy> ResolveAsync(
            int clearingHouseId,
            DateTime operationalDate,
            CancellationToken ct = default)
            => Task.FromResult(policies.Single(policy => policy.ClearingHouseId == clearingHouseId));

        public Task<ResolvedClearingHouseCyclePolicy> ResolveAtInstantAsync(
            int clearingHouseId,
            DateTimeOffset instant,
            CancellationToken ct = default)
            => ResolveAsync(clearingHouseId, instant.Date, ct);
    }
}
