using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class ThirdPartyAccountValidationLifecycleTests
{
    [Fact]
    public async Task Policies_AchColombiaCreditDebit_AndCenitCreditDebit_RemainIndependent()
    {
        await using var context = CreateContext();
        SeedRule(context, 1, TransactionNature.Credit, PrenotificationRequirementMode.Optional, 3,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "ACH-V35-CREDIT");
        SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, 3,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "ACH-V35-DEBIT");
        SeedRule(context, 2, TransactionNature.Credit, PrenotificationRequirementMode.Optional, null,
            PrenotificationResponseDeadlineMode.OperationalDayEnd, null, true, null, "CENIT-CREDIT");
        SeedRule(context, 2, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, null,
            PrenotificationResponseDeadlineMode.OperationalDayEnd, null, true, 13, "CENIT-DEBIT");
        await context.SaveChangesAsync();

        var service = CreatePolicyService(context);
        var date = new DateTime(2026, 4, 20);
        var achCredit = (await service.ResolveAsync(1, TransactionNature.Credit, date)).Rule!;
        var achDebit = (await service.ResolveAsync(1, TransactionNature.Debit, date)).Rule!;
        var cenitCredit = (await service.ResolveAsync(2, TransactionNature.Credit, date)).Rule!;
        var cenitDebit = (await service.ResolveAsync(2, TransactionNature.Debit, date)).Rule!;

        Assert.Equal(PrenotificationRequirementMode.Optional, achCredit.PrenotificationMode);
        Assert.Equal(3, achCredit.PrenotificationLeadBusinessDays);
        Assert.Equal(PrenotificationResponseDeadlineMode.CycleCount, achCredit.PrenotificationResponseDeadlineMode);
        Assert.Equal(4, achCredit.PrenotificationResponseWindowCycles);

        Assert.Equal(PrenotificationRequirementMode.Mandatory, achDebit.PrenotificationMode);
        Assert.Equal(3, achDebit.PrenotificationLeadBusinessDays);
        Assert.Equal(PrenotificationResponseDeadlineMode.CycleCount, achDebit.PrenotificationResponseDeadlineMode);

        Assert.Equal(PrenotificationRequirementMode.Optional, cenitCredit.PrenotificationMode);
        Assert.Null(cenitCredit.PrenotificationLeadBusinessDays);
        Assert.Equal(PrenotificationResponseDeadlineMode.OperationalDayEnd, cenitCredit.PrenotificationResponseDeadlineMode);

        Assert.Equal(PrenotificationRequirementMode.Mandatory, cenitDebit.PrenotificationMode);
        Assert.Null(cenitDebit.PrenotificationLeadBusinessDays);
        Assert.Equal(13, cenitDebit.ValidationRetentionMonthsAfterLastMonetaryUse);
    }

    [Fact]
    public async Task PolicyResolution_UsesEffectiveVersionByDate()
    {
        await using var context = CreateContext();
        var old = SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, 2,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "OLD");
        old.EffectiveFrom = new DateTime(2025, 1, 1);
        old.EffectiveTo = new DateTime(2025, 12, 31);
        var current = SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, 3,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "V35");
        current.EffectiveFrom = new DateTime(2026, 1, 1);
        await context.SaveChangesAsync();

        var service = CreatePolicyService(context);
        Assert.Equal("OLD", (await service.ResolveAsync(1, TransactionNature.Debit, new DateTime(2025, 6, 1))).Rule!.NormativeReference);
        Assert.Equal("V35", (await service.ResolveAsync(1, TransactionNature.Debit, new DateTime(2026, 6, 1))).Rule!.NormativeReference);
    }

    [Fact]
    public async Task Eligibility_FunctionalPolicyVersionChange_RequiresRevalidation()
    {
        await using var context = CreateContext();
        var account = SeedEligibilityAccount(context);
        var old = SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, 2,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "OLD");
        old.EffectiveFrom = new DateTime(2025, 1, 1);
        old.EffectiveTo = new DateTime(2025, 12, 31);
        var current = SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, 3,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "V35");
        current.EffectiveFrom = new DateTime(2026, 1, 1);
        await context.SaveChangesAsync();

        var validation = new ThirdPartyAccountValidation
        {
            ThirdPartyAccountId = account.Id,
            ClearingHouseId = 1,
            TransactionNature = TransactionNature.Debit,
            Status = ThirdPartyAccountValidationStatus.Accepted,
            PolicyRuleId = old.Id,
            PolicyEffectiveFrom = old.EffectiveFrom,
            EffectiveDate = new DateTime(2025, 12, 15)
        };
        context.ThirdPartyAccountValidations.Add(validation);
        await context.SaveChangesAsync();
        context.PrenotificationValidationAttempts.Add(new PrenotificationValidationAttempt
        {
            ThirdPartyAccountValidationId = validation.Id,
            AttemptNumber = 1,
            IdempotencyKey = "old-policy-validation",
            Status = PrenotificationValidationAttemptStatus.Accepted,
            Resolution = PrenotificationValidationResolution.Accepted,
            SentAtUtc = new DateTimeOffset(2025, 12, 15, 12, 0, 0, TimeSpan.Zero)
        });
        await context.SaveChangesAsync();

        var eligibility = new ThirdPartyAccountEligibilityService(context, CreatePolicyService(context));
        var result = await eligibility.EvaluateAsync(new(account.Id, 1, TransactionNature.Debit, new DateTime(2026, 1, 15)));

        Assert.Equal(ThirdPartyAccountEligibilityDecision.RequiresRevalidation, result.Decision);
        Assert.Equal("POLICY_VERSION_CHANGED", result.Code);
        Assert.Equal(current.Id, result.PolicyRule?.Id);
    }

    [Fact]
    public async Task Eligibility_IsContextual_NotGlobalBoolean_AndAchCreditLeadAppliesOnlyWhenPrenotified()
    {
        await using var context = CreateContext();
        var account = SeedEligibilityAccount(context);
        var achCredit = SeedRule(context, 1, TransactionNature.Credit, PrenotificationRequirementMode.Optional, 3,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "ACH-CREDIT");
        SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, 3,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "ACH-DEBIT");
        SeedRule(context, 2, TransactionNature.Credit, PrenotificationRequirementMode.Optional, null,
            PrenotificationResponseDeadlineMode.OperationalDayEnd, null, true, null, "CENIT-CREDIT");
        SeedRule(context, 2, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, null,
            PrenotificationResponseDeadlineMode.OperationalDayEnd, null, true, 13, "CENIT-DEBIT");
        await context.SaveChangesAsync();

        var eligibility = new ThirdPartyAccountEligibilityService(context, CreatePolicyService(context));
        var date = new DateTime(2026, 1, 14);

        Assert.Equal(ThirdPartyAccountEligibilityDecision.AllowedWithoutPrenotification,
            (await eligibility.EvaluateAsync(new(account.Id, 1, TransactionNature.Credit, date))).Decision);
        Assert.Equal(ThirdPartyAccountEligibilityDecision.RequiresPrenotification,
            (await eligibility.EvaluateAsync(new(account.Id, 1, TransactionNature.Debit, date))).Decision);
        Assert.Equal(ThirdPartyAccountEligibilityDecision.AllowedWithoutPrenotification,
            (await eligibility.EvaluateAsync(new(account.Id, 2, TransactionNature.Credit, date))).Decision);
        Assert.Equal(ThirdPartyAccountEligibilityDecision.RequiresPrenotification,
            (await eligibility.EvaluateAsync(new(account.Id, 2, TransactionNature.Debit, date))).Decision);

        var validation = new ThirdPartyAccountValidation
        {
            ThirdPartyAccountId = account.Id,
            ClearingHouseId = 1,
            TransactionNature = TransactionNature.Credit,
            ServiceType = "PPD",
            Status = ThirdPartyAccountValidationStatus.Accepted,
            PolicyRuleId = achCredit.Id,
            EffectiveDate = new DateTime(2026, 1, 12)
        };
        context.ThirdPartyAccountValidations.Add(validation);
        await context.SaveChangesAsync();
        context.PrenotificationValidationAttempts.Add(new PrenotificationValidationAttempt
        {
            ThirdPartyAccountValidationId = validation.Id,
            AttemptNumber = 1,
            IdempotencyKey = "credit-lead",
            Status = PrenotificationValidationAttemptStatus.Accepted,
            Resolution = PrenotificationValidationResolution.Accepted,
            SentAtUtc = new DateTimeOffset(2026, 1, 12, 12, 0, 0, TimeSpan.Zero)
        });
        await context.SaveChangesAsync();

        var early = await eligibility.EvaluateAsync(new(account.Id, 1, TransactionNature.Credit, date, "ppd"));
        var mature = await eligibility.EvaluateAsync(new(account.Id, 1, TransactionNature.Credit, new DateTime(2026, 1, 15), "PPD"));
        Assert.Equal(ThirdPartyAccountEligibilityDecision.PrenotificationPending, early.Decision);
        Assert.Equal(ThirdPartyAccountEligibilityDecision.Allowed, mature.Decision);
    }

    [Fact]
    public async Task Eligibility_CenitDebitRetentionExpiresFromLastMonetaryUse_WithoutLeakingToAchColombia()
    {
        await using var context = CreateContext();
        var account = SeedEligibilityAccount(context);
        var cenitRule = SeedRule(context, 2, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, null,
            PrenotificationResponseDeadlineMode.OperationalDayEnd, null, true, 13, "CENIT-DEBIT");
        var achRule = SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, null,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "ACH-DEBIT");
        await context.SaveChangesAsync();

        var lastUse = new DateTimeOffset(2025, 1, 10, 15, 0, 0, TimeSpan.Zero);
        var cenitValidation = new ThirdPartyAccountValidation
        {
            ThirdPartyAccountId = account.Id,
            ClearingHouseId = 2,
            TransactionNature = TransactionNature.Debit,
            Status = ThirdPartyAccountValidationStatus.Accepted,
            PolicyRuleId = cenitRule.Id,
            EffectiveDate = lastUse.UtcDateTime.Date,
            LastMonetaryUseAtUtc = lastUse
        };
        var achValidation = new ThirdPartyAccountValidation
        {
            ThirdPartyAccountId = account.Id,
            ClearingHouseId = 1,
            TransactionNature = TransactionNature.Debit,
            Status = ThirdPartyAccountValidationStatus.Accepted,
            PolicyRuleId = achRule.Id,
            EffectiveDate = lastUse.UtcDateTime.Date,
            LastMonetaryUseAtUtc = lastUse
        };
        context.ThirdPartyAccountValidations.AddRange(cenitValidation, achValidation);
        await context.SaveChangesAsync();
        context.PrenotificationValidationAttempts.AddRange(
            new PrenotificationValidationAttempt
            {
                ThirdPartyAccountValidationId = cenitValidation.Id,
                AttemptNumber = 1,
                IdempotencyKey = "cenit-retention",
                Status = PrenotificationValidationAttemptStatus.Accepted,
                Resolution = PrenotificationValidationResolution.Accepted,
                SentAtUtc = lastUse.AddDays(-1)
            },
            new PrenotificationValidationAttempt
            {
                ThirdPartyAccountValidationId = achValidation.Id,
                AttemptNumber = 1,
                IdempotencyKey = "ach-no-retention",
                Status = PrenotificationValidationAttemptStatus.Accepted,
                Resolution = PrenotificationValidationResolution.Accepted,
                SentAtUtc = lastUse.AddDays(-1)
            });
        await context.SaveChangesAsync();

        var eligibility = new ThirdPartyAccountEligibilityService(context, CreatePolicyService(context));
        var effectiveDate = new DateTime(2026, 2, 11);
        var cenit = await eligibility.EvaluateAsync(new(account.Id, 2, TransactionNature.Debit, effectiveDate));
        var ach = await eligibility.EvaluateAsync(new(account.Id, 1, TransactionNature.Debit, effectiveDate));

        Assert.Equal(ThirdPartyAccountEligibilityDecision.ValidationExpired, cenit.Decision);
        Assert.Equal(ThirdPartyAccountEligibilityDecision.Allowed, ach.Decision);
    }

    [Fact]
    public async Task Eligibility_MissingPolicy_FailsClosedWithoutInventingAuthorization()
    {
        await using var context = CreateContext();
        var account = SeedEligibilityAccount(context);
        await context.SaveChangesAsync();

        var policy = new Mock<IClearingHouseValidationPolicyService>();
        policy.Setup(x => x.ResolveAsync(99, TransactionNature.Debit, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClearingHouseValidationPolicyResolution(null, new DateTime(2026, 1, 15), TransactionNature.Debit, 99));
        var eligibility = new ThirdPartyAccountEligibilityService(context, policy.Object);

        var result = await eligibility.EvaluateAsync(new(account.Id, 99, TransactionNature.Debit, new DateTime(2026, 1, 15), "PPD"));

        Assert.Equal(ThirdPartyAccountEligibilityDecision.Unsupported, result.Decision);
        Assert.Equal("POLICY_NOT_CONFIGURED", result.Code);
    }

    [Fact]
    public async Task Eligibility_InactiveCanonicalMaster_BlocksAndDoesNotUseLegacyActiveAsAuthority()
    {
        var legacy = new CustomerThirdParty();
        legacy.LinkPendingPrenotification(new AchTransaction { Id = 9100 });
        legacy.ApplyAutomaticNachaResult(CustomerThirdPartyStatusEnum.Active, 9100, "C1", DateTime.UtcNow,
            "automatic", "legacy-active-evidence");

        await using var context = CreateContext();
        var account = SeedEligibilityAccount(context);
        account.Status = Enum.GetValues<ThirdPartyAdministrativeStatus>()
            .First(x => x != ThirdPartyAdministrativeStatus.Active);
        await context.SaveChangesAsync();

        var policy = new Mock<IClearingHouseValidationPolicyService>(MockBehavior.Strict);
        var eligibility = new ThirdPartyAccountEligibilityService(context, policy.Object);
        var result = await eligibility.EvaluateAsync(new(account.Id, 1, TransactionNature.Debit, new DateTime(2026, 1, 15)));

        Assert.Equal(CustomerThirdPartyStatusEnum.Active, legacy.Status);
        Assert.Equal(ThirdPartyAccountEligibilityDecision.Blocked, result.Decision);
        Assert.Equal("MASTER_BLOCKED", result.Code);
        policy.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Eligibility_InactiveCanonicalThirdPartyMaster_BlocksBeforePolicyResolution()
    {
        await using var context = CreateContext();
        var account = SeedEligibilityAccount(context);
        var thirdParty = context.Set<ThirdParty>().Single(x => x.Id == account.ThirdPartyId);
        thirdParty.Status = Enum.GetValues<ThirdPartyAdministrativeStatus>()
            .First(x => x != ThirdPartyAdministrativeStatus.Active);
        await context.SaveChangesAsync();

        var policy = new Mock<IClearingHouseValidationPolicyService>(MockBehavior.Strict);
        var eligibility = new ThirdPartyAccountEligibilityService(context, policy.Object);
        var result = await eligibility.EvaluateAsync(new(account.Id, 1, TransactionNature.Debit, new DateTime(2026, 1, 15)));

        Assert.Equal(ThirdPartyAccountEligibilityDecision.Blocked, result.Decision);
        Assert.Equal("MASTER_BLOCKED", result.Code);
        policy.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Eligibility_ResolvedPolicyOutsideRequestedEffectiveContext_FailsClosedToTechnicalReview()
    {
        await using var context = CreateContext();
        var account = SeedEligibilityAccount(context);
        var rule = SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, null,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "ACH-DEBIT-FUTURE");
        rule.EffectiveFrom = new DateTime(2026, 2, 1);
        await context.SaveChangesAsync();

        var policy = new Mock<IClearingHouseValidationPolicyService>();
        policy.Setup(x => x.ResolveAsync(1, TransactionNature.Debit, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClearingHouseValidationPolicyResolution(rule, new DateTime(2026, 1, 15), TransactionNature.Debit, 1));
        var eligibility = new ThirdPartyAccountEligibilityService(context, policy.Object);

        var result = await eligibility.EvaluateAsync(new(account.Id, 1, TransactionNature.Debit, new DateTime(2026, 1, 15), "PPD"));

        Assert.Equal(ThirdPartyAccountEligibilityDecision.TechnicalReview, result.Decision);
        Assert.Equal("POLICY_CONTEXT_MISMATCH", result.Code);
    }

    [Theory]
    [InlineData(ThirdPartyAccountValidationStatus.PendingSend, ThirdPartyAccountEligibilityDecision.PrenotificationPending)]
    [InlineData(ThirdPartyAccountValidationStatus.PendingResponse, ThirdPartyAccountEligibilityDecision.PrenotificationPending)]
    [InlineData(ThirdPartyAccountValidationStatus.Rejected, ThirdPartyAccountEligibilityDecision.Rejected)]
    [InlineData(ThirdPartyAccountValidationStatus.RequiresCorrection, ThirdPartyAccountEligibilityDecision.RequiresRevalidation)]
    [InlineData(ThirdPartyAccountValidationStatus.TechnicalReview, ThirdPartyAccountEligibilityDecision.TechnicalReview)]
    [InlineData(ThirdPartyAccountValidationStatus.Expired, ThirdPartyAccountEligibilityDecision.ValidationExpired)]
    public async Task Eligibility_NonPositiveValidationStates_AreAlwaysNonAuthorizing(
        ThirdPartyAccountValidationStatus status,
        ThirdPartyAccountEligibilityDecision expected)
    {
        await using var context = CreateContext();
        var account = SeedEligibilityAccount(context);
        var rule = SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, null,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "ACH-DEBIT");
        await context.SaveChangesAsync();
        context.ThirdPartyAccountValidations.Add(new ThirdPartyAccountValidation
        {
            ThirdPartyAccountId = account.Id,
            ClearingHouseId = 1,
            TransactionNature = TransactionNature.Debit,
            ServiceType = "PPD",
            ValidationType = ThirdPartyAccountValidationType.Prenotification,
            Status = status,
            PolicyRuleId = rule.Id,
            PolicyEffectiveFrom = rule.EffectiveFrom,
            EffectiveDate = new DateTime(2026, 1, 12)
        });
        await context.SaveChangesAsync();

        var eligibility = new ThirdPartyAccountEligibilityService(context, CreatePolicyService(context));
        var result = await eligibility.EvaluateAsync(new(account.Id, 1, TransactionNature.Debit, new DateTime(2026, 1, 15), "ppd"));

        Assert.Equal(expected, result.Decision);
        Assert.NotEqual(ThirdPartyAccountEligibilityDecision.Allowed, result.Decision);
        Assert.NotEqual(ThirdPartyAccountEligibilityDecision.AllowedWithoutPrenotification, result.Decision);
    }

    [Fact]
    public async Task Eligibility_ValidationMustMatchChamberNatureServiceTypeAndRequestedDate()
    {
        await using var context = CreateContext();
        var account = SeedEligibilityAccount(context);
        var achDebit = SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, null,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "ACH-DEBIT");
        SeedRule(context, 2, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, null,
            PrenotificationResponseDeadlineMode.OperationalDayEnd, null, true, 13, "CENIT-DEBIT");
        SeedRule(context, 1, TransactionNature.Credit, PrenotificationRequirementMode.Optional, null,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "ACH-CREDIT");
        await context.SaveChangesAsync();

        var validation = new ThirdPartyAccountValidation
        {
            ThirdPartyAccountId = account.Id,
            ClearingHouseId = 1,
            TransactionNature = TransactionNature.Debit,
            ServiceType = "PPD",
            ValidationType = ThirdPartyAccountValidationType.Prenotification,
            Status = ThirdPartyAccountValidationStatus.Accepted,
            PolicyRuleId = achDebit.Id,
            PolicyEffectiveFrom = achDebit.EffectiveFrom,
            EffectiveDate = new DateTime(2026, 1, 20)
        };
        context.ThirdPartyAccountValidations.Add(validation);
        await context.SaveChangesAsync();
        context.PrenotificationValidationAttempts.Add(new PrenotificationValidationAttempt
        {
            ThirdPartyAccountValidationId = validation.Id,
            AttemptNumber = 1,
            IdempotencyKey = "eligibility-context-1",
            Status = PrenotificationValidationAttemptStatus.Accepted,
            Resolution = PrenotificationValidationResolution.Accepted,
            SentAtUtc = new DateTimeOffset(2026, 1, 20, 12, 0, 0, TimeSpan.Zero)
        });
        await context.SaveChangesAsync();

        var eligibility = new ThirdPartyAccountEligibilityService(context, CreatePolicyService(context));

        var tooEarly = await eligibility.EvaluateAsync(new(account.Id, 1, TransactionNature.Debit, new DateTime(2026, 1, 19), "ppd"));
        var wrongServiceType = await eligibility.EvaluateAsync(new(account.Id, 1, TransactionNature.Debit, new DateTime(2026, 1, 21), "CCD"));
        var wrongChamber = await eligibility.EvaluateAsync(new(account.Id, 2, TransactionNature.Debit, new DateTime(2026, 1, 21), "PPD"));
        var wrongNature = await eligibility.EvaluateAsync(new(account.Id, 1, TransactionNature.Credit, new DateTime(2026, 1, 21), "PPD"));
        var exactContext = await eligibility.EvaluateAsync(new(account.Id, 1, TransactionNature.Debit, new DateTime(2026, 1, 21), "PPD"));

        Assert.Equal(ThirdPartyAccountEligibilityDecision.PrenotificationPending, tooEarly.Decision);
        Assert.Equal("VALIDATION_NOT_EFFECTIVE_YET", tooEarly.Code);
        Assert.Equal(ThirdPartyAccountEligibilityDecision.RequiresPrenotification, wrongServiceType.Decision);
        Assert.Equal(ThirdPartyAccountEligibilityDecision.RequiresPrenotification, wrongChamber.Decision);
        Assert.Equal(ThirdPartyAccountEligibilityDecision.AllowedWithoutPrenotification, wrongNature.Decision);
        Assert.Equal(ThirdPartyAccountEligibilityDecision.Allowed, exactContext.Decision);
    }

    [Theory]
    [InlineData(ThirdPartyAccountValidationStatus.Accepted)]
    [InlineData(ThirdPartyAccountValidationStatus.TacitlyAccepted)]
    public async Task Eligibility_PositiveParentStateWithoutMatchingAppendOnlyEvidence_FailsClosedToTechnicalReview(
        ThirdPartyAccountValidationStatus status)
    {
        await using var context = CreateContext();
        var account = SeedEligibilityAccount(context);
        var rule = SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, null,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "ACH-DEBIT");
        await context.SaveChangesAsync();
        context.ThirdPartyAccountValidations.Add(new ThirdPartyAccountValidation
        {
            ThirdPartyAccountId = account.Id,
            ClearingHouseId = 1,
            TransactionNature = TransactionNature.Debit,
            ServiceType = "PPD",
            ValidationType = ThirdPartyAccountValidationType.Prenotification,
            Status = status,
            PolicyRuleId = rule.Id,
            PolicyEffectiveFrom = rule.EffectiveFrom,
            EffectiveDate = new DateTime(2026, 1, 12)
        });
        await context.SaveChangesAsync();

        var eligibility = new ThirdPartyAccountEligibilityService(context, CreatePolicyService(context));
        var result = await eligibility.EvaluateAsync(new(account.Id, 1, TransactionNature.Debit, new DateTime(2026, 1, 15), "PPD"));

        Assert.Equal(ThirdPartyAccountEligibilityDecision.TechnicalReview, result.Decision);
        Assert.Equal("VALIDATION_EVIDENCE_INCONSISTENT", result.Code);
    }

    [Fact]
    public async Task Eligibility_RepeatedEvaluation_IsDeterministicAndReadOnly()
    {
        await using var context = CreateContext();
        var account = SeedEligibilityAccount(context);
        var rule = SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, null,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "ACH-DEBIT");
        await context.SaveChangesAsync();
        var validation = new ThirdPartyAccountValidation
        {
            ThirdPartyAccountId = account.Id,
            ClearingHouseId = 1,
            TransactionNature = TransactionNature.Debit,
            ServiceType = "PPD",
            ValidationType = ThirdPartyAccountValidationType.Prenotification,
            Status = ThirdPartyAccountValidationStatus.Accepted,
            PolicyRuleId = rule.Id,
            PolicyEffectiveFrom = rule.EffectiveFrom,
            EffectiveDate = new DateTime(2026, 1, 12)
        };
        context.ThirdPartyAccountValidations.Add(validation);
        await context.SaveChangesAsync();
        var attempt = new PrenotificationValidationAttempt
        {
            ThirdPartyAccountValidationId = validation.Id,
            AttemptNumber = 1,
            IdempotencyKey = "eligibility-deterministic",
            Status = PrenotificationValidationAttemptStatus.Accepted,
            Resolution = PrenotificationValidationResolution.Accepted,
            SentAtUtc = new DateTimeOffset(2026, 1, 12, 12, 0, 0, TimeSpan.Zero)
        };
        context.PrenotificationValidationAttempts.Add(attempt);
        await context.SaveChangesAsync();

        var eligibility = new ThirdPartyAccountEligibilityService(context, CreatePolicyService(context));
        var request = new ThirdPartyAccountEligibilityRequest(account.Id, 1, TransactionNature.Debit, new DateTime(2026, 1, 15), "ppd");
        var first = await eligibility.EvaluateAsync(request);
        var second = await eligibility.EvaluateAsync(request);

        Assert.Equal(ThirdPartyAccountEligibilityDecision.Allowed, first.Decision);
        Assert.Equal(first.Decision, second.Decision);
        Assert.Equal(first.Code, second.Code);
        Assert.Equal(first.PolicyRule?.Id, second.PolicyRule?.Id);
        Assert.Equal(1, await context.ThirdPartyAccountValidations.CountAsync());
        Assert.Equal(1, await context.PrenotificationValidationAttempts.CountAsync());
    }

    [Fact]
    public async Task Eligibility_DuplicateContextRows_FailClosedRatherThanPickArbitraryValidation()
    {
        await using var context = CreateContext();
        var account = SeedEligibilityAccount(context);
        var rule = SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, null,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "ACH-DEBIT");
        await context.SaveChangesAsync();
        context.ThirdPartyAccountValidations.AddRange(
            new ThirdPartyAccountValidation
            {
                ThirdPartyAccountId = account.Id,
                ClearingHouseId = 1,
                TransactionNature = TransactionNature.Debit,
                ServiceType = "PPD",
                ValidationType = ThirdPartyAccountValidationType.Prenotification,
                Status = ThirdPartyAccountValidationStatus.Accepted,
                PolicyRuleId = rule.Id,
                PolicyEffectiveFrom = rule.EffectiveFrom,
                EffectiveDate = new DateTime(2026, 1, 12)
            },
            new ThirdPartyAccountValidation
            {
                ThirdPartyAccountId = account.Id,
                ClearingHouseId = 1,
                TransactionNature = TransactionNature.Debit,
                ServiceType = "PPD",
                ValidationType = ThirdPartyAccountValidationType.Prenotification,
                Status = ThirdPartyAccountValidationStatus.Rejected,
                PolicyRuleId = rule.Id,
                PolicyEffectiveFrom = rule.EffectiveFrom,
                EffectiveDate = new DateTime(2026, 1, 12)
            });
        await context.SaveChangesAsync();

        var eligibility = new ThirdPartyAccountEligibilityService(context, CreatePolicyService(context));

        await Assert.ThrowsAsync<InvalidOperationException>(() => eligibility.EvaluateAsync(
            new ThirdPartyAccountEligibilityRequest(account.Id, 1, TransactionNature.Debit, new DateTime(2026, 1, 15), "PPD")));
    }

    [Fact]
    public async Task Eligibility_PreCanceledEvaluation_PropagatesCancellationBeforeAnyReadOrPolicyDecision()
    {
        await using var context = CreateContext();
        var policy = new Mock<IClearingHouseValidationPolicyService>(MockBehavior.Strict);
        var eligibility = new ThirdPartyAccountEligibilityService(context, policy.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => eligibility.EvaluateAsync(
            new ThirdPartyAccountEligibilityRequest(9001, 1, TransactionNature.Debit, new DateTime(2026, 1, 15), "PPD"),
            cts.Token));

        policy.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Eligibility_PolicyReadFailure_PropagatesAndNeverTurnsIntoAllow()
    {
        await using var context = CreateContext();
        var account = SeedEligibilityAccount(context);
        await context.SaveChangesAsync();
        var policy = new Mock<IClearingHouseValidationPolicyService>();
        policy.Setup(x => x.ResolveAsync(1, TransactionNature.Debit, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Injected policy read failure."));
        var eligibility = new ThirdPartyAccountEligibilityService(context, policy.Object);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => eligibility.EvaluateAsync(
            new ThirdPartyAccountEligibilityRequest(account.Id, 1, TransactionNature.Debit, new DateTime(2026, 1, 15), "PPD")));

        Assert.Contains("policy read failure", error.Message.ToLowerInvariant());
    }

    [Fact]
    public async Task Eligibility_MasterReadFailure_PropagatesAndNeverTurnsIntoAllow()
    {
        var context = CreateContext();
        var policy = new Mock<IClearingHouseValidationPolicyService>(MockBehavior.Strict);
        var eligibility = new ThirdPartyAccountEligibilityService(context, policy.Object);
        await context.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => eligibility.EvaluateAsync(
            new ThirdPartyAccountEligibilityRequest(9001, 1, TransactionNature.Debit, new DateTime(2026, 1, 15), "PPD")));

        policy.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResponseDeadlines_AchUsesConfiguredCycleWindow_AndCenitUsesOperationalDayEnd()
    {
        await using var context = CreateContext();
        SeedClearingHouseWithCycles(context, 1, "ACH", new[]
        {
            ("ACH-1", new TimeSpan(7, 0, 0)),
            ("ACH-2", new TimeSpan(9, 0, 0)),
            ("ACH-3", new TimeSpan(11, 0, 0)),
            ("ACH-4", new TimeSpan(13, 0, 0)),
            ("ACH-5", new TimeSpan(15, 0, 0))
        });
        SeedClearingHouseWithCycles(context, 2, "CENIT", new[]
        {
            ("CENIT-1", new TimeSpan(10, 30, 0)),
            ("CENIT-2", new TimeSpan(13, 0, 0)),
            ("CENIT-5", new TimeSpan(18, 45, 0))
        });
        var achRule = SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, 3,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "ACH-DEBIT");
        var cenitRule = SeedRule(context, 2, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, null,
            PrenotificationResponseDeadlineMode.OperationalDayEnd, null, true, 13, "CENIT-DEBIT");
        await context.SaveChangesAsync();

        var service = CreatePolicyService(context);
        var achDeadline = await service.ResolvePrenotificationResponseDeadlineAsync(achRule, "ACH-1");
        var cenitDeadline = await service.ResolvePrenotificationResponseDeadlineAsync(cenitRule, "CENIT-1");

        Assert.Equal(new DateTimeOffset(2026, 1, 15, 20, 0, 0, TimeSpan.Zero), achDeadline); // 15:00 America/Bogota
        Assert.Equal(new DateTimeOffset(2026, 1, 15, 23, 45, 0, TimeSpan.Zero), cenitDeadline); // 18:45 America/Bogota
    }

    [Fact]
    public async Task ValidationLifecycle_IsIdempotent_StoresRejectionCause_AndAllowsReprenotification()
    {
        await using var context = CreateContext();
        var account = SeedCanonicalAccount(context);
        var rule = SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, 3,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "ACH-DEBIT");
        await context.SaveChangesAsync();

        var policy = new Mock<IClearingHouseValidationPolicyService>();
        policy.Setup(x => x.ResolveAsync(1, TransactionNature.Debit, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClearingHouseValidationPolicyResolution(rule, new DateTime(2026, 1, 12), TransactionNature.Debit, 1));
        policy.Setup(x => x.ResolvePrenotificationResponseDeadlineAsync(rule, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DateTimeOffset(2026, 1, 13, 17, 0, 0, TimeSpan.Zero));
        var service = new ThirdPartyAccountValidationService(context, policy.Object, Mock.Of<ILogger<ThirdPartyAccountValidationService>>());

        var firstRequest = new PrenotificationValidationStartRequest(account.Id, 100, 1, TransactionNature.Debit,
            new DateTime(2026, 1, 12), new DateTimeOffset(2026, 1, 12, 12, 0, 0, TimeSpan.Zero), "ACH-1", "prenote:100");
        var first = await service.RegisterPrenotificationAttemptAsync(firstRequest);
        var replay = await service.RegisterPrenotificationAttemptAsync(firstRequest);
        Assert.Equal(first.AttemptId, replay.AttemptId);
        Assert.True(replay.ReusedExistingAttempt);
        Assert.Equal(1, await context.PrenotificationValidationAttempts.CountAsync());
        Assert.Equal(first.AttemptId, (await context.ThirdPartyAccountValidations.SingleAsync()).CurrentAttemptId);

        var replayWithDifferentKey = await service.RegisterPrenotificationAttemptAsync(firstRequest with
        {
            IdempotencyKey = "prenote:100:retry"
        });
        Assert.Equal(first.AttemptId, replayWithDifferentKey.AttemptId);
        Assert.True(replayWithDifferentKey.ReusedExistingAttempt);
        Assert.Equal(1, await context.PrenotificationValidationAttempts.CountAsync());

        Assert.True(await service.ApplyTransactionResolutionAsync(new(
            100,
            AchTransferStateEnum.ReturnedByOperator,
            new DateTimeOffset(2026, 1, 12, 14, 0, 0, TimeSpan.Zero),
            "response:100",
            "corr:100",
            "R03",
            "Cuenta no localizada",
            "ACH-2")));

        var rejected = await context.PrenotificationValidationAttempts.SingleAsync(x => x.Id == first.AttemptId);
        Assert.Equal(PrenotificationValidationResolution.Rejected, rejected.Resolution);
        Assert.Equal("R03", rejected.ReturnCode);

        var second = await service.RegisterPrenotificationAttemptAsync(firstRequest with
        {
            AchTransactionId = 101,
            IdempotencyKey = "prenote:101",
            SentAtUtc = new DateTimeOffset(2026, 1, 13, 12, 0, 0, TimeSpan.Zero)
        });
        Assert.Equal(2, second.AttemptNumber);
        Assert.Equal(first.ValidationId, second.ValidationId);
        var currentValidation = await context.ThirdPartyAccountValidations.SingleAsync();
        Assert.Equal(ThirdPartyAccountValidationStatus.PendingResponse, currentValidation.Status);
        Assert.Equal(second.AttemptId, currentValidation.CurrentAttemptId);
        Assert.Equal(2, await context.PrenotificationValidationAttempts.CountAsync());
    }

    [Fact]
    public async Task ValidationLifecycle_IdempotencyKeyCollisionWithDifferentTransaction_FailsSafeWithoutDuplicate()
    {
        await using var context = CreateContext();
        var account = SeedCanonicalAccount(context);
        var rule = SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, 3,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "ACH-DEBIT");
        await context.SaveChangesAsync();

        var policy = new Mock<IClearingHouseValidationPolicyService>();
        policy.Setup(x => x.ResolveAsync(1, TransactionNature.Debit, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClearingHouseValidationPolicyResolution(rule, new DateTime(2026, 1, 12), TransactionNature.Debit, 1));
        policy.Setup(x => x.ResolvePrenotificationResponseDeadlineAsync(rule, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DateTimeOffset(2026, 1, 13, 17, 0, 0, TimeSpan.Zero));
        var service = new ThirdPartyAccountValidationService(context, policy.Object, Mock.Of<ILogger<ThirdPartyAccountValidationService>>());

        var first = new PrenotificationValidationStartRequest(account.Id, 110, 1, TransactionNature.Debit,
            new DateTime(2026, 1, 12), DateTimeOffset.UtcNow, "ACH-1", "prenote:collision");
        await service.RegisterPrenotificationAttemptAsync(first);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterPrenotificationAttemptAsync(first with
        {
            AchTransactionId = 111
        }));

        Assert.Equal(1, await context.PrenotificationValidationAttempts.CountAsync());
    }

    [Fact]
    public async Task ValidationLifecycle_IdempotencyReplay_RejectsEffectiveDateContextMismatch()
    {
        await using var context = CreateContext();
        var account = SeedCanonicalAccount(context);
        var rule = SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, 3,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "ACH-DEBIT");
        await context.SaveChangesAsync();

        var policy = new Mock<IClearingHouseValidationPolicyService>();
        policy.Setup(x => x.ResolveAsync(1, TransactionNature.Debit, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClearingHouseValidationPolicyResolution(rule, new DateTime(2026, 1, 12), TransactionNature.Debit, 1));
        policy.Setup(x => x.ResolvePrenotificationResponseDeadlineAsync(rule, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DateTimeOffset(2026, 1, 13, 17, 0, 0, TimeSpan.Zero));
        var service = new ThirdPartyAccountValidationService(context, policy.Object, Mock.Of<ILogger<ThirdPartyAccountValidationService>>());

        var first = new PrenotificationValidationStartRequest(account.Id, 115, 1, TransactionNature.Debit,
            new DateTime(2026, 1, 12), DateTimeOffset.UtcNow, "ACH-1", "prenote:date-bound", ServiceType: "PPD");
        await service.RegisterPrenotificationAttemptAsync(first);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterPrenotificationAttemptAsync(first with
        {
            EffectiveDate = new DateTime(2026, 1, 13)
        }));

        Assert.Equal(1, await context.PrenotificationValidationAttempts.CountAsync());
    }

    [Fact]
    public async Task ValidationLifecycle_TerminalAttempt_IsImmutableAgainstReplayOrConflictingLateResolution()
    {
        await using var context = CreateContext();
        var account = SeedCanonicalAccount(context);
        var rule = SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, 3,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "ACH-DEBIT");
        await context.SaveChangesAsync();

        var policy = new Mock<IClearingHouseValidationPolicyService>();
        policy.Setup(x => x.ResolveAsync(1, TransactionNature.Debit, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClearingHouseValidationPolicyResolution(rule, new DateTime(2026, 1, 12), TransactionNature.Debit, 1));
        policy.Setup(x => x.ResolvePrenotificationResponseDeadlineAsync(rule, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DateTimeOffset(2026, 1, 13, 17, 0, 0, TimeSpan.Zero));
        var service = new ThirdPartyAccountValidationService(context, policy.Object, Mock.Of<ILogger<ThirdPartyAccountValidationService>>());

        await service.RegisterPrenotificationAttemptAsync(new(account.Id, 120, 1, TransactionNature.Debit,
            new DateTime(2026, 1, 12), DateTimeOffset.UtcNow, "ACH-1", "prenote:120"));
        Assert.True(await service.ApplyTransactionResolutionAsync(new(
            120,
            AchTransferStateEnum.ReturnedByOperator,
            new DateTimeOffset(2026, 1, 12, 14, 0, 0, TimeSpan.Zero),
            "response:first",
            "corr:first",
            "R03",
            "Cuenta no localizada",
            "ACH-2")));
        await context.SaveChangesAsync();

        Assert.False(await service.ApplyTransactionResolutionAsync(new(
            120,
            AchTransferStateEnum.ReturnedByOperator,
            new DateTimeOffset(2026, 1, 12, 15, 0, 0, TimeSpan.Zero),
            "response:replay",
            "corr:replay",
            "R04",
            "No debe sobrescribir",
            "ACH-3")));
        Assert.False(await service.ApplyTransactionResolutionAsync(new(
            120,
            AchTransferStateEnum.Certified,
            new DateTimeOffset(2026, 1, 12, 16, 0, 0, TimeSpan.Zero),
            "response:conflict",
            "corr:conflict")));

        var attempt = await context.PrenotificationValidationAttempts.SingleAsync();
        var validation = await context.ThirdPartyAccountValidations.SingleAsync();
        Assert.Equal(PrenotificationValidationAttemptStatus.Rejected, attempt.Status);
        Assert.Equal(PrenotificationValidationResolution.Rejected, attempt.Resolution);
        Assert.Equal("R03", attempt.ReturnCode);
        Assert.Equal("Cuenta no localizada", attempt.ReturnDetail);
        Assert.Equal("response:first", attempt.EvidenceReference);
        Assert.Equal("corr:first", attempt.CorrelationReference);
        Assert.Equal("ACH-2", attempt.SourceCycleId);
        Assert.Equal(ThirdPartyAccountValidationStatus.Rejected, validation.Status);
    }

    [Fact]
    public async Task ValidationLifecycle_LateResolutionOfOlderAttempt_DoesNotCorruptCurrentReprenotificationState()
    {
        await using var context = CreateContext();
        var account = SeedCanonicalAccount(context);
        var rule = SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, 3,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "ACH-DEBIT");
        await context.SaveChangesAsync();

        var policy = new Mock<IClearingHouseValidationPolicyService>();
        policy.Setup(x => x.ResolveAsync(1, TransactionNature.Debit, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClearingHouseValidationPolicyResolution(rule, new DateTime(2026, 1, 12), TransactionNature.Debit, 1));
        policy.Setup(x => x.ResolvePrenotificationResponseDeadlineAsync(rule, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DateTimeOffset(2026, 1, 13, 17, 0, 0, TimeSpan.Zero));
        var service = new ThirdPartyAccountValidationService(context, policy.Object, Mock.Of<ILogger<ThirdPartyAccountValidationService>>());

        var first = await service.RegisterPrenotificationAttemptAsync(new(account.Id, 130, 1, TransactionNature.Debit,
            new DateTime(2026, 1, 12), DateTimeOffset.UtcNow, "ACH-1", "prenote:130"));
        var second = await service.RegisterPrenotificationAttemptAsync(new(account.Id, 131, 1, TransactionNature.Debit,
            new DateTime(2026, 1, 13), DateTimeOffset.UtcNow.AddHours(1), "ACH-2", "prenote:131"));

        Assert.True(await service.ApplyTransactionResolutionAsync(new(
            130,
            AchTransferStateEnum.ReturnedByOperator,
            DateTimeOffset.UtcNow.AddHours(2),
            "response:130",
            ReturnCode: "R03",
            ReturnDetail: "late-old-attempt",
            SourceCycleId: "ACH-3")));
        await context.SaveChangesAsync();

        var validation = await context.ThirdPartyAccountValidations.SingleAsync();
        var oldAttempt = await context.PrenotificationValidationAttempts.SingleAsync(x => x.Id == first.AttemptId);
        Assert.Equal(PrenotificationValidationAttemptStatus.Rejected, oldAttempt.Status);
        Assert.Equal(ThirdPartyAccountValidationStatus.PendingResponse, validation.Status);
        Assert.Equal(second.AttemptId, validation.CurrentAttemptId);
    }

    [Fact]
    public async Task ValidationLifecycle_MissingDeadlineContext_RoutesToTechnicalReviewEvenWhenTacitAcceptanceIsDisabled()
    {
        await using var context = CreateContext();
        var account = SeedCanonicalAccount(context);
        var rule = SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, 3,
            PrenotificationResponseDeadlineMode.CycleCount, 4, false, null, "ACH-DEBIT");
        await context.SaveChangesAsync();

        var policy = new Mock<IClearingHouseValidationPolicyService>();
        policy.Setup(x => x.ResolveAsync(1, TransactionNature.Debit, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClearingHouseValidationPolicyResolution(rule, new DateTime(2026, 1, 12), TransactionNature.Debit, 1));
        policy.Setup(x => x.ResolvePrenotificationResponseDeadlineAsync(rule, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTimeOffset?)null);
        var service = new ThirdPartyAccountValidationService(context, policy.Object, Mock.Of<ILogger<ThirdPartyAccountValidationService>>());

        var result = await service.RegisterPrenotificationAttemptAsync(new(account.Id, 140, 1, TransactionNature.Debit,
            new DateTime(2026, 1, 12), DateTimeOffset.UtcNow, "UNKNOWN-CYCLE", "prenote:140"));

        Assert.Equal(PrenotificationValidationAttemptStatus.TechnicalReview, result.AttemptStatus);
        Assert.Equal(ThirdPartyAccountValidationStatus.TechnicalReview, result.ValidationStatus);
        Assert.Null(result.ResponseDeadlineAtUtc);
    }

    [Fact]
    public async Task ValidationLifecycle_PreCanceledRegistration_DoesNotCreateValidationOrAttempt()
    {
        await using var context = CreateContext();
        var account = SeedCanonicalAccount(context);
        await context.SaveChangesAsync();
        var service = new ThirdPartyAccountValidationService(
            context,
            Mock.Of<IClearingHouseValidationPolicyService>(),
            Mock.Of<ILogger<ThirdPartyAccountValidationService>>());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.RegisterPrenotificationAttemptAsync(new(
            account.Id,
            150,
            1,
            TransactionNature.Debit,
            new DateTime(2026, 1, 12),
            DateTimeOffset.UtcNow,
            "ACH-1",
            "prenote:150"), cts.Token));

        Assert.Empty(await context.ThirdPartyAccountValidations.ToListAsync());
        Assert.Empty(await context.PrenotificationValidationAttempts.ToListAsync());
    }

    [Fact]
    public async Task ValidationLifecycle_WhenCurrentAttemptPointerPersistenceFails_RollsBackAttemptAndParentState()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var failSave = new FailOnNthSaveChangesInterceptor();
        await using var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(failSave)
            .Options);
        await context.Database.EnsureCreatedAsync();
        await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=OFF;");

        const int accountId = 9001;
        var rule = SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, 3,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "ACH-DEBIT");
        await context.SaveChangesAsync();
        var validation = new ThirdPartyAccountValidation
        {
            ThirdPartyAccountId = accountId,
            ClearingHouseId = 1,
            TransactionNature = TransactionNature.Debit,
            ServiceType = ThirdPartyAccountValidation.NormalizeServiceType(null),
            ValidationType = ThirdPartyAccountValidationType.Prenotification,
            Status = ThirdPartyAccountValidationStatus.NotStarted,
            PolicyRuleId = rule.Id,
            PolicyEffectiveFrom = rule.EffectiveFrom,
            EffectiveDate = new DateTime(2026, 1, 12)
        };
        context.ThirdPartyAccountValidations.Add(validation);
        await context.SaveChangesAsync();

        var policy = new Mock<IClearingHouseValidationPolicyService>();
        policy.Setup(x => x.ResolveAsync(1, TransactionNature.Debit, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClearingHouseValidationPolicyResolution(rule, new DateTime(2026, 1, 12), TransactionNature.Debit, 1));
        policy.Setup(x => x.ResolvePrenotificationResponseDeadlineAsync(rule, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DateTimeOffset(2026, 1, 13, 17, 0, 0, TimeSpan.Zero));
        var service = new ThirdPartyAccountValidationService(context, policy.Object, Mock.Of<ILogger<ThirdPartyAccountValidationService>>());
        failSave.Arm(2);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterPrenotificationAttemptAsync(new(
            accountId,
            155,
            1,
            TransactionNature.Debit,
            new DateTime(2026, 1, 12),
            DateTimeOffset.UtcNow,
            "ACH-1",
            "prenote:155")));

        context.ChangeTracker.Clear();
        var persistedValidation = await context.ThirdPartyAccountValidations.SingleAsync(x => x.Id == validation.Id);
        Assert.Equal(ThirdPartyAccountValidationStatus.NotStarted, persistedValidation.Status);
        Assert.Null(persistedValidation.CurrentAttemptId);
        Assert.Empty(await context.PrenotificationValidationAttempts.ToListAsync());
    }

    [Fact]
    public async Task ValidationLifecycle_PreCanceledResolution_DoesNotMutateAttemptOrCurrentState()
    {
        await using var context = CreateContext();
        var account = SeedCanonicalAccount(context);
        var rule = SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, 3,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "ACH-DEBIT");
        await context.SaveChangesAsync();

        var policy = new Mock<IClearingHouseValidationPolicyService>();
        policy.Setup(x => x.ResolveAsync(1, TransactionNature.Debit, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClearingHouseValidationPolicyResolution(rule, new DateTime(2026, 1, 12), TransactionNature.Debit, 1));
        policy.Setup(x => x.ResolvePrenotificationResponseDeadlineAsync(rule, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DateTimeOffset(2026, 1, 13, 17, 0, 0, TimeSpan.Zero));
        var service = new ThirdPartyAccountValidationService(context, policy.Object, Mock.Of<ILogger<ThirdPartyAccountValidationService>>());

        var started = await service.RegisterPrenotificationAttemptAsync(new(account.Id, 160, 1, TransactionNature.Debit,
            new DateTime(2026, 1, 12), DateTimeOffset.UtcNow, "ACH-1", "prenote:160"));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ApplyTransactionResolutionAsync(new(
            160,
            AchTransferStateEnum.ReturnedByOperator,
            DateTimeOffset.UtcNow,
            "response:160",
            ReturnCode: "R03"), cts.Token));

        var attempt = await context.PrenotificationValidationAttempts.SingleAsync(x => x.Id == started.AttemptId);
        var validation = await context.ThirdPartyAccountValidations.SingleAsync();
        Assert.Equal(PrenotificationValidationAttemptStatus.AwaitingResponse, attempt.Status);
        Assert.Equal(PrenotificationValidationResolution.None, attempt.Resolution);
        Assert.Equal(ThirdPartyAccountValidationStatus.PendingResponse, validation.Status);
        Assert.Equal(started.AttemptId, validation.CurrentAttemptId);
    }

    [Fact]
    public async Task ValidationLifecycle_MissingEffectivePolicy_RoutesToTechnicalReviewWithoutInventingOutcome()
    {
        await using var context = CreateContext();
        var account = SeedCanonicalAccount(context);
        await context.SaveChangesAsync();

        var policy = new Mock<IClearingHouseValidationPolicyService>();
        policy.Setup(x => x.ResolveAsync(99, TransactionNature.Debit, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClearingHouseValidationPolicyResolution(null, new DateTime(2026, 1, 12), TransactionNature.Debit, 99));
        var service = new ThirdPartyAccountValidationService(context, policy.Object, Mock.Of<ILogger<ThirdPartyAccountValidationService>>());

        var result = await service.RegisterPrenotificationAttemptAsync(new(account.Id, 199, 99, TransactionNature.Debit,
            new DateTime(2026, 1, 12), DateTimeOffset.UtcNow, "UNKNOWN-1", "prenote:199", ServiceType: "ppd"));

        Assert.Equal(ThirdPartyAccountValidationStatus.TechnicalReview, result.ValidationStatus);
        Assert.Equal(PrenotificationValidationAttemptStatus.TechnicalReview, result.AttemptStatus);
        Assert.Null(result.ResponseDeadlineAtUtc);
        var validation = await context.ThirdPartyAccountValidations.SingleAsync();
        Assert.Equal("PPD", validation.ServiceType);
    }

    [Fact]
    public async Task ValidationLifecycle_InvalidNatureOrEffectiveDate_IsRejectedBeforePolicyResolution()
    {
        await using var context = CreateContext();
        var account = SeedCanonicalAccount(context);
        await context.SaveChangesAsync();
        var policy = new Mock<IClearingHouseValidationPolicyService>(MockBehavior.Strict);
        var service = new ThirdPartyAccountValidationService(
            context,
            policy.Object,
            Mock.Of<ILogger<ThirdPartyAccountValidationService>>());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.RegisterPrenotificationAttemptAsync(new(
            account.Id,
            198,
            1,
            (TransactionNature)999,
            new DateTime(2026, 1, 12),
            DateTimeOffset.UtcNow,
            "ACH-1",
            "prenote:invalid-nature")));
        await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterPrenotificationAttemptAsync(new(
            account.Id,
            199,
            1,
            TransactionNature.Debit,
            default,
            DateTimeOffset.UtcNow,
            "ACH-1",
            "prenote:invalid-date")));

        policy.VerifyNoOtherCalls();
        Assert.Empty(await context.ThirdPartyAccountValidations.ToListAsync());
        Assert.Empty(await context.PrenotificationValidationAttempts.ToListAsync());
    }

    [Fact]
    public async Task ValidationLifecycle_ResolverContextMismatch_RoutesToTechnicalReviewAndSkipsDeadlineResolution()
    {
        await using var context = CreateContext();
        var account = SeedCanonicalAccount(context);
        var mismatchedRule = SeedRule(context, 2, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, null,
            PrenotificationResponseDeadlineMode.OperationalDayEnd, null, true, 13, "CENIT-DEBIT");
        await context.SaveChangesAsync();

        var policy = new Mock<IClearingHouseValidationPolicyService>(MockBehavior.Strict);
        policy.Setup(x => x.ResolveAsync(1, TransactionNature.Debit, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClearingHouseValidationPolicyResolution(
                mismatchedRule,
                new DateTime(2026, 1, 12),
                TransactionNature.Debit,
                1));
        var service = new ThirdPartyAccountValidationService(context, policy.Object, Mock.Of<ILogger<ThirdPartyAccountValidationService>>());

        var result = await service.RegisterPrenotificationAttemptAsync(new(
            account.Id,
            199,
            1,
            TransactionNature.Debit,
            new DateTime(2026, 1, 12),
            DateTimeOffset.UtcNow,
            "ACH-1",
            "prenote:resolver-mismatch",
            ServiceType: "PPD"));

        Assert.Equal(ThirdPartyAccountValidationStatus.TechnicalReview, result.ValidationStatus);
        Assert.Equal(PrenotificationValidationAttemptStatus.TechnicalReview, result.AttemptStatus);
        Assert.Null(result.ResponseDeadlineAtUtc);
        policy.Verify(x => x.ResolvePrenotificationResponseDeadlineAsync(
            It.IsAny<ClearingHouseTransactionRule>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidationLifecycle_NotApplicablePolicy_RoutesToTechnicalReview()
    {
        await using var context = CreateContext();
        var account = SeedCanonicalAccount(context);
        var rule = SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.NotApplicable, null,
            PrenotificationResponseDeadlineMode.NotApplicable, null, false, null, "ACH-NOT-APPLICABLE");
        await context.SaveChangesAsync();

        var policy = new Mock<IClearingHouseValidationPolicyService>(MockBehavior.Strict);
        policy.Setup(x => x.ResolveAsync(1, TransactionNature.Debit, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClearingHouseValidationPolicyResolution(rule, new DateTime(2026, 1, 12), TransactionNature.Debit, 1));
        var service = new ThirdPartyAccountValidationService(context, policy.Object, Mock.Of<ILogger<ThirdPartyAccountValidationService>>());

        var result = await service.RegisterPrenotificationAttemptAsync(new(
            account.Id,
            200,
            1,
            TransactionNature.Debit,
            new DateTime(2026, 1, 12),
            DateTimeOffset.UtcNow,
            "ACH-1",
            "prenote:not-applicable",
            ServiceType: "PPD"));

        Assert.Equal(ThirdPartyAccountValidationStatus.TechnicalReview, result.ValidationStatus);
        Assert.Equal(PrenotificationValidationAttemptStatus.TechnicalReview, result.AttemptStatus);
        Assert.Null(result.ResponseDeadlineAtUtc);
        policy.Verify(x => x.ResolvePrenotificationResponseDeadlineAsync(
            It.IsAny<ClearingHouseTransactionRule>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidationLifecycle_TechnicalReview_CannotBePromotedToAcceptedOrRejectedByResolutionCallback()
    {
        await using var context = CreateContext();
        var account = SeedCanonicalAccount(context);
        await context.SaveChangesAsync();

        var policy = new Mock<IClearingHouseValidationPolicyService>();
        policy.Setup(x => x.ResolveAsync(99, TransactionNature.Debit, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClearingHouseValidationPolicyResolution(null, new DateTime(2026, 1, 12), TransactionNature.Debit, 99));
        var logger = new Mock<ILogger<ThirdPartyAccountValidationService>>();
        var service = new ThirdPartyAccountValidationService(context, policy.Object, logger.Object);

        var started = await service.RegisterPrenotificationAttemptAsync(new(
            account.Id,
            201,
            99,
            TransactionNature.Debit,
            new DateTime(2026, 1, 12),
            DateTimeOffset.UtcNow,
            "UNKNOWN-1",
            "sensitive-idempotency-value",
            SourceFileReference: "sensitive-file-reference",
            ServiceType: "PPD"));

        Assert.False(await service.ApplyTransactionResolutionAsync(new(
            201,
            AchTransferStateEnum.Certified,
            DateTimeOffset.UtcNow,
            "sensitive-evidence-reference",
            "sensitive-correlation-reference")));
        Assert.False(await service.ApplyTransactionResolutionAsync(new(
            201,
            AchTransferStateEnum.ReturnedByOperator,
            DateTimeOffset.UtcNow,
            "sensitive-evidence-reference-2",
            "sensitive-correlation-reference-2",
            "R03",
            "sensitive-return-detail",
            "UNKNOWN-2")));

        var attempt = await context.PrenotificationValidationAttempts.SingleAsync(x => x.Id == started.AttemptId);
        var validation = await context.ThirdPartyAccountValidations.SingleAsync();
        Assert.Equal(PrenotificationValidationAttemptStatus.TechnicalReview, attempt.Status);
        Assert.Equal(PrenotificationValidationResolution.TechnicalReview, attempt.Resolution);
        Assert.Equal(ThirdPartyAccountValidationStatus.TechnicalReview, validation.Status);
        Assert.DoesNotContain("sensitive-evidence-reference", attempt.EvidenceReference, StringComparison.Ordinal);

        var renderedLogs = string.Join('|', logger.Invocations
            .Where(invocation => invocation.Method.Name == nameof(ILogger.Log))
            .Select(invocation => invocation.Arguments.Count > 2 ? invocation.Arguments[2]?.ToString() : null));
        Assert.DoesNotContain("sensitive-idempotency-value", renderedLogs, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-file-reference", renderedLogs, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-evidence-reference", renderedLogs, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-return-detail", renderedLogs, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidationLifecycle_TacitAcceptance_IsAutomaticState_NotManualApproval()
    {
        await using var context = CreateContext();
        var account = SeedCanonicalAccount(context);
        var rule = SeedRule(context, 2, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, null,
            PrenotificationResponseDeadlineMode.OperationalDayEnd, null, true, 13, "CENIT-DEBIT");
        await context.SaveChangesAsync();

        var policy = new Mock<IClearingHouseValidationPolicyService>();
        policy.Setup(x => x.ResolveAsync(2, TransactionNature.Debit, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClearingHouseValidationPolicyResolution(rule, new DateTime(2026, 1, 12), TransactionNature.Debit, 2));
        policy.Setup(x => x.ResolvePrenotificationResponseDeadlineAsync(rule, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DateTimeOffset(2026, 1, 12, 23, 0, 0, TimeSpan.Zero));
        var service = new ThirdPartyAccountValidationService(context, policy.Object, Mock.Of<ILogger<ThirdPartyAccountValidationService>>());

        await service.RegisterPrenotificationAttemptAsync(new(account.Id, 200, 2, TransactionNature.Debit,
            new DateTime(2026, 1, 12), DateTimeOffset.UtcNow, "CENIT-1", "prenote:200"));
        await service.ApplyTransactionResolutionAsync(new(200, AchTransferStateEnum.AppliedTacitly,
            DateTimeOffset.UtcNow, "job:tacit:200", SourceCycleId: "CENIT-5"));

        var attempt = await context.PrenotificationValidationAttempts.SingleAsync();
        var validation = await context.ThirdPartyAccountValidations.SingleAsync();
        Assert.Equal(PrenotificationValidationResolution.TacitlyAccepted, attempt.Resolution);
        Assert.Equal(ThirdPartyAccountValidationStatus.TacitlyAccepted, validation.Status);
    }

    [Fact]
    public async Task RecordMonetaryUseAsync_UpdatesOnlyTheContextualValidationForTheCanonicalAccount()
    {
        await using var context = CreateContext();
        var account = SeedCanonicalAccount(context);
        var rule = SeedRule(context, 2, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, null,
            PrenotificationResponseDeadlineMode.OperationalDayEnd, null, true, 13, "CENIT-DEBIT");
        await context.SaveChangesAsync();

        var validation = new ThirdPartyAccountValidation
        {
            ThirdPartyAccountId = account.Id,
            ClearingHouseId = 2,
            TransactionNature = TransactionNature.Debit,
            Status = ThirdPartyAccountValidationStatus.Accepted,
            PolicyRuleId = rule.Id,
            PolicyEffectiveFrom = rule.EffectiveFrom,
            EffectiveDate = new DateTime(2026, 1, 12)
        };
        context.ThirdPartyAccountValidations.Add(validation);
        await context.SaveChangesAsync();

        var service = new ThirdPartyAccountValidationService(
            context,
            Mock.Of<IClearingHouseValidationPolicyService>(),
            Mock.Of<ILogger<ThirdPartyAccountValidationService>>());
        var usedAt = new DateTimeOffset(2026, 2, 20, 14, 30, 0, TimeSpan.FromHours(-5));

        var updated = await service.RecordMonetaryUseAsync(new ThirdPartyAccountMonetaryUseRequest(
            account.Id,
            validation.Id,
            usedAt));
        await context.SaveChangesAsync();

        Assert.True(updated);
        Assert.Equal(usedAt.ToUniversalTime(),
            (await context.ThirdPartyAccountValidations.SingleAsync(x => x.Id == validation.Id)).LastMonetaryUseAtUtc);
        Assert.False(await service.RecordMonetaryUseAsync(new ThirdPartyAccountMonetaryUseRequest(
            account.Id,
            validation.Id,
            usedAt.AddHours(-1))));
        Assert.False(await service.RecordMonetaryUseAsync(new ThirdPartyAccountMonetaryUseRequest(
            account.Id + 999,
            validation.Id,
            usedAt.AddHours(1))));
    }

    [Fact]
    public async Task CanonicalReprenotification_DoesNotResetLegacyActiveStatus()
    {
        var legacy = new CustomerThirdParty();
        legacy.LinkPendingPrenotification(new AchTransaction { Id = 300 });
        legacy.ApplyAutomaticNachaResult(CustomerThirdPartyStatusEnum.Active, 300, "C1", DateTime.UtcNow,
            "automatic", "evidence");

        await using var context = CreateContext();
        var account = SeedCanonicalAccount(context);
        var rule = SeedRule(context, 1, TransactionNature.Debit, PrenotificationRequirementMode.Mandatory, 3,
            PrenotificationResponseDeadlineMode.CycleCount, 4, true, null, "ACH-DEBIT");
        await context.SaveChangesAsync();
        var policy = new Mock<IClearingHouseValidationPolicyService>();
        policy.Setup(x => x.ResolveAsync(1, TransactionNature.Debit, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClearingHouseValidationPolicyResolution(rule, DateTime.Today, TransactionNature.Debit, 1));
        policy.Setup(x => x.ResolvePrenotificationResponseDeadlineAsync(rule, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DateTimeOffset.UtcNow.AddHours(1));
        var service = new ThirdPartyAccountValidationService(context, policy.Object, Mock.Of<ILogger<ThirdPartyAccountValidationService>>());

        await service.RegisterPrenotificationAttemptAsync(new(account.Id, 301, 1, TransactionNature.Debit,
            DateTime.Today, DateTimeOffset.UtcNow, "ACH-1", "prenote:301"));

        Assert.Equal(CustomerThirdPartyStatusEnum.Active, legacy.Status);
        Assert.Equal(ThirdPartyAccountValidationStatus.PendingResponse,
            (await context.ThirdPartyAccountValidations.SingleAsync()).Status);
    }

    private sealed class FailOnNthSaveChangesInterceptor : SaveChangesInterceptor
    {
        private int _saveCall;
        private int _failOnCall;
        private bool _armed;

        public void Arm(int failOnCall)
        {
            _saveCall = 0;
            _failOnCall = failOnCall;
            _armed = true;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (_armed && ++_saveCall == _failOnCall)
            {
                _armed = false;
                throw new InvalidOperationException("Injected pointer persistence failure.");
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private static AchDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IClearingHouseValidationPolicyService CreatePolicyService(AchDbContext context)
    {
        var holidays = new Mock<IBankHoliday>();
        holidays.Setup(x => x.GetHolidays(It.IsAny<int>())).Returns(new List<BankHolidayModel>());
        return new ClearingHouseValidationPolicyService(context, holidays.Object, new OperationalCycleWindowResolver());
    }

    private static void SeedClearingHouseWithCycles(
        AchDbContext context,
        int clearingHouseId,
        string code,
        IEnumerable<(string Id, TimeSpan Cutoff)> cycles)
    {
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = clearingHouseId,
            ClearingHouseId = clearingHouseId,
            HolidayStrategy = "Colombian",
            TimeZoneId = "America/Bogota"
        });
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = clearingHouseId,
            ClearingHouseId = clearingHouseId,
            Code = code,
            Name = code,
            OriginCode = $"{clearingHouseId:00000000}"
        });
        foreach (var cycle in cycles)
        {
            context.AchCycles.Add(new AchCycle
            {
                Id = cycle.Id,
                ClearingHouseId = clearingHouseId,
                CycleName = cycle.Id,
                ProcessingDate = new DateTime(2026, 1, 15),
                StartTime = TimeSpan.Zero,
                EndTime = cycle.Cutoff,
                CutoffTime = cycle.Cutoff
            });
        }
    }

    private static ThirdPartyAccount SeedEligibilityAccount(AchDbContext context)
    {
        var thirdParty = Activator.CreateInstance<ThirdParty>()!;
        thirdParty.Id = 8001;
        thirdParty.Status = ThirdPartyAdministrativeStatus.Active;

        // Eligibility reads the canonical master relationship. Populate required string fields
        // generically so this fixture stays focused on administrative status rather than identity shape.
        var thirdPartyEntity = context.Model.FindEntityType(typeof(ThirdParty));
        if (thirdPartyEntity is not null)
        {
            foreach (var property in thirdPartyEntity.GetProperties()
                         .Where(x => !x.IsNullable && x.ClrType == typeof(string) && x.PropertyInfo?.CanWrite == true))
            {
                if (property.PropertyInfo!.GetValue(thirdParty) is null)
                {
                    property.PropertyInfo.SetValue(thirdParty, "X");
                }
            }
        }

        context.Set<ThirdParty>().Add(thirdParty);
        var account = new ThirdPartyAccount
        {
            Id = 9001,
            ThirdPartyId = thirdParty.Id,
            FinancialInstitutionId = 7001,
            AccountType = AccountTypeEnum.Savings,
            AccountNumberNormalized = "000123",
            Status = ThirdPartyAdministrativeStatus.Active
        };
        context.ThirdPartyAccounts.Add(account);
        return account;
    }

    private static ThirdPartyAccount SeedCanonicalAccount(AchDbContext context)
    {
        var account = new ThirdPartyAccount
        {
            Id = 9001,
            ThirdPartyId = 8001,
            FinancialInstitutionId = 7001,
            AccountType = AccountTypeEnum.Savings,
            AccountNumberNormalized = "000123",
            Status = ThirdPartyAdministrativeStatus.Active
        };
        context.ThirdPartyAccounts.Add(account);
        return account;
    }

    private static ClearingHouseTransactionRule SeedRule(
        AchDbContext context,
        int clearingHouseId,
        TransactionNature nature,
        PrenotificationRequirementMode mode,
        int? leadDays,
        PrenotificationResponseDeadlineMode deadlineMode,
        int? responseCycles,
        bool tacit,
        int? retentionMonths,
        string reference)
    {
        var rule = new ClearingHouseTransactionRule
        {
            ClearingHouseId = clearingHouseId,
            TransactionNature = nature,
            TransactionType = nature == TransactionNature.Debit ? TransactionTypeEnum.Debit : TransactionTypeEnum.Credit,
            RequiresPrenotification = mode == PrenotificationRequirementMode.Mandatory,
            PrenotificationMode = mode,
            PrenotificationLeadBusinessDays = leadDays,
            PrenotificationResponseDeadlineMode = deadlineMode,
            PrenotificationResponseWindowCycles = responseCycles,
            TacitAcceptanceEnabled = tacit,
            ValidationRetentionMonthsAfterLastMonetaryUse = retentionMonths,
            RequiresReceiverIdentificationValidation = nature == TransactionNature.Debit,
            ReceiverIdentificationValidationMode = nature == TransactionNature.Debit ? ValidationRequirementMode.Mandatory : ValidationRequirementMode.Optional,
            AppliesToNachaExport = true,
            AppliesToMonetaryTransactions = true,
            EffectiveFrom = new DateTime(2025, 1, 1),
            IsActive = true,
            NormativeSource = reference.StartsWith("ACH", StringComparison.Ordinal) ? "DDS-DIS-MAN-004 ACH Colombia V35" : "CENIT DSP-152 Anexo 2",
            NormativeReference = reference,
            Notes = "TPA3 test"
        };
        context.ClearingHouseTransactionRules.Add(rule);
        return rule;
    }
}
