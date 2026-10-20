using System.Reflection;
using System.Text.Json;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class ThirdPartyAccountReadShadowTests
{
    private const string SourceAccount = "123456789012";
    private const string ThirdPartyAccountNumber = "987654321098";
    private const string ThirdPartyDocument = "900123456";

    [Fact]
    public async Task CanonicalRead_RequiresPositiveCustomerScope()
    {
        await using var context = CreateContext();
        var service = new ThirdPartyAccountReadService(context, Mock.Of<IThirdPartyAccountEligibilityService>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetAsync(new ThirdPartyAccountReadQuery()));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetAsync(new ThirdPartyAccountReadQuery { CustomerId = 0 }));
    }

    [Fact]
    public async Task CanonicalRead_CustomerScopedQueryCannotReturnForeignAccount()
    {
        await using var context = CreateContext();
        var seeded = await SeedCanonicalGraphAsync(context);

        var foreignCustomer = new Customer
        {
            Id = 990,
            FirstName = "Otro",
            LastName = "Cliente",
            CompanyName = "Otro Cliente",
            PersonType = "PJ",
            DocumentType = "NIT",
            DocumentNumber = "900000990"
        };
        var foreignThirdParty = new ThirdParty
        {
            Id = 991,
            CustomerId = foreignCustomer.Id,
            Customer = foreignCustomer,
            PersonType = "PN",
            DocumentType = "CC",
            DocumentNumberNormalized = "1122334455",
            DisplayName = "Proveedor Ajeno",
            Status = ThirdPartyAdministrativeStatus.Active
        };
        var foreignAccount = new ThirdPartyAccount
        {
            Id = 992,
            ThirdPartyId = foreignThirdParty.Id,
            ThirdParty = foreignThirdParty,
            FinancialInstitutionId = seeded.Institution.Id,
            FinancialInstitution = seeded.Institution,
            AccountType = AccountTypeEnum.Checking,
            AccountNumberNormalized = "222233334444",
            Status = ThirdPartyAdministrativeStatus.Active
        };
        foreignThirdParty.Accounts.Add(foreignAccount);
        context.Customers.Add(foreignCustomer);
        context.ThirdParties.Add(foreignThirdParty);
        context.ThirdPartyAccounts.Add(foreignAccount);
        await context.SaveChangesAsync();

        var service = new ThirdPartyAccountReadService(context, Mock.Of<IThirdPartyAccountEligibilityService>());
        var result = await service.GetAsync(new ThirdPartyAccountReadQuery
        {
            CustomerId = seeded.Customer.Id,
            Page = 1,
            PageSize = 100
        });

        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, item => Assert.Equal(seeded.Customer.Id, item.CustomerId));
        Assert.DoesNotContain(result.Items, item => item.ThirdPartyAccountId == foreignAccount.Id);
    }

    [Fact]
    public async Task CanonicalReadController_UsesReadPolicy_RequiresCustomerScope_AndRejectsClientEligibilityContext()
    {
        var controllerType = typeof(ThirdPartyAccountsController);
        Assert.NotEmpty(controllerType.GetCustomAttributes<AuthorizeAttribute>(inherit: true));
        foreach (var methodName in new[] { nameof(ThirdPartyAccountsController.Get), nameof(ThirdPartyAccountsController.GetById) })
        {
            var method = controllerType.GetMethod(methodName) ?? throw new InvalidOperationException($"Missing {methodName}");
            Assert.Contains(method.GetCustomAttributes<AuthorizeAttribute>(inherit: true), attribute => attribute.Policy == "CanReadAch");
            Assert.Empty(method.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true));
        }

        var read = new Mock<IThirdPartyAccountReadService>(MockBehavior.Strict);
        var controller = new ThirdPartyAccountsController(read.Object);

        var missingCustomer = await controller.Get(
            search: ThirdPartyDocument,
            customerId: null,
            thirdPartyId: null,
            thirdPartyAccountId: null,
            financialInstitutionId: null,
            thirdPartyStatus: null,
            accountStatus: null,
            eligibilityClearingHouseId: null,
            eligibilityTransactionNature: null,
            eligibilityEffectiveDate: null,
            eligibilityServiceType: null,
            page: 1,
            pageSize: 20,
            ct: default);
        var missingCustomerError = Assert.IsType<BadRequestObjectResult>(missingCustomer);
        Assert.DoesNotContain(ThirdPartyDocument, JsonSerializer.Serialize(missingCustomerError.Value), StringComparison.Ordinal);

        var manipulatedContext = await controller.Get(
            search: null,
            customerId: 100,
            thirdPartyId: null,
            thirdPartyAccountId: null,
            financialInstitutionId: null,
            thirdPartyStatus: null,
            accountStatus: null,
            eligibilityClearingHouseId: 1,
            eligibilityTransactionNature: TransactionNature.Debit,
            eligibilityEffectiveDate: new DateTime(2026, 10, 12),
            eligibilityServiceType: ThirdPartyAccountNumber,
            page: 1,
            pageSize: 20,
            ct: default);
        var manipulatedContextError = Assert.IsType<BadRequestObjectResult>(manipulatedContext);
        Assert.DoesNotContain(ThirdPartyAccountNumber, JsonSerializer.Serialize(manipulatedContextError.Value), StringComparison.Ordinal);

        var missingCustomerById = await controller.GetById(
            thirdPartyAccountId: 401,
            customerId: null,
            eligibilityClearingHouseId: null,
            eligibilityTransactionNature: null,
            eligibilityEffectiveDate: null,
            eligibilityServiceType: null,
            ct: default);
        Assert.IsType<BadRequestObjectResult>(missingCustomerById);
        read.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanonicalReadController_GetByIdScopesLookupByCustomerAndReturnsPlainNotFound()
    {
        var read = new Mock<IThirdPartyAccountReadService>(MockBehavior.Strict);
        read.Setup(x => x.GetAsync(
                It.Is<ThirdPartyAccountReadQuery>(query =>
                    query.CustomerId == 100
                    && query.ThirdPartyAccountId == 401
                    && query.EligibilityContext == null
                    && query.Page == 1
                    && query.PageSize == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<ThirdPartyAccountReadDto>
            {
                Items = Array.Empty<ThirdPartyAccountReadDto>(),
                Total = 0,
                Page = 1,
                PageSize = 1
            });

        var controller = new ThirdPartyAccountsController(read.Object);
        var result = await controller.GetById(
            thirdPartyAccountId: 401,
            customerId: 100,
            eligibilityClearingHouseId: null,
            eligibilityTransactionNature: null,
            eligibilityEffectiveDate: null,
            eligibilityServiceType: null,
            ct: default);

        Assert.IsType<NotFoundResult>(result);
        read.VerifyAll();
    }

    [Fact]
    public async Task CanonicalRead_ReturnsCustomerThirdPartyAccountInstitutionAndMaskedIdentity()
    {
        await using var context = CreateContext();
        var seeded = await SeedCanonicalGraphAsync(context);
        var eligibility = new Mock<IThirdPartyAccountEligibilityService>();
        eligibility.Setup(x => x.EvaluateAsync(
                It.Is<ThirdPartyAccountEligibilityRequest>(r =>
                    r.ThirdPartyAccountId == seeded.Account.Id
                    && r.ClearingHouseId == 1
                    && r.TransactionNature == TransactionNature.Debit
                    && r.ServiceType == "PPD"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ThirdPartyAccountEligibilityResult(
                ThirdPartyAccountEligibilityDecision.Allowed,
                "ELIGIBLE",
                "eligible",
                null,
                ThirdPartyAccountValidationStatus.Accepted,
                seeded.AchValidation.Id,
                seeded.AchAttempt.Id));

        var service = new ThirdPartyAccountReadService(context, eligibility.Object);
        var result = await service.GetAsync(new ThirdPartyAccountReadQuery
        {
            CustomerId = seeded.Customer.Id,
            EligibilityContext = new ThirdPartyAccountEligibilityReadContext(
                1,
                TransactionNature.Debit,
                new DateTime(2026, 10, 11),
                "PPD")
        });

        var item = Assert.Single(result.Items);
        Assert.Equal(seeded.Customer.Id, item.CustomerId);
        Assert.Equal(seeded.ThirdParty.Id, item.ThirdPartyId);
        Assert.Equal(seeded.Account.Id, item.ThirdPartyAccountId);
        Assert.Equal(seeded.Institution.Id, item.FinancialInstitutionId);
        Assert.Equal(AccountTypeEnum.Savings, item.AccountType);
        Assert.Equal(ThirdPartyAdministrativeStatus.Active, item.ThirdPartyStatus);
        Assert.Equal(ThirdPartyAdministrativeStatus.Active, item.AccountStatus);
        Assert.Equal("****1098", item.AccountNumberMasked);
        Assert.Equal("****3456", item.DocumentNumberMasked);
        Assert.Equal(ThirdPartyAccountEligibilityDecision.Allowed, item.Eligibility?.Decision);

        var serialized = JsonSerializer.Serialize(item);
        Assert.DoesNotContain(ThirdPartyAccountNumber, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(ThirdPartyDocument, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-evidence-ref", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-correlation-ref", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-file-ref", serialized, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CanonicalRead_ReturnsMultipleValidationsByClearingHouse_WithLatestAttemptCauseAndTrace()
    {
        await using var context = CreateContext();
        var seeded = await SeedCanonicalGraphAsync(context);
        var eligibility = Mock.Of<IThirdPartyAccountEligibilityService>();
        var service = new ThirdPartyAccountReadService(context, eligibility);

        var item = await service.GetByAccountIdAsync(seeded.Account.Id);

        Assert.NotNull(item);
        Assert.Equal(2, item.Validations.Count);
        var ach = Assert.Single(item.Validations.Where(x => x.ClearingHouseId == 1));
        var cenit = Assert.Single(item.Validations.Where(x => x.ClearingHouseId == 2));
        Assert.Equal(TransactionNature.Debit, ach.TransactionNature);
        Assert.Equal(ThirdPartyAccountValidationStatus.Accepted, ach.Status);
        Assert.Equal(PrenotificationValidationResolution.Accepted, ach.LatestAttempt?.Resolution);
        Assert.Equal("ACH-C1", ach.LatestAttempt?.SourceCycleId);
        Assert.Equal(ThirdPartyAccountValidationStatus.Rejected, cenit.Status);
        Assert.Equal("R03", cenit.LatestAttempt?.ReturnCode);
        Assert.Equal(PrenotificationValidationResolution.Rejected, cenit.LatestAttempt?.Resolution);
    }

    [Fact]
    public async Task CanonicalRead_LoadsOnlyLatestAttemptPerValidation()
    {
        var materialization = new EntityMaterializationCounter();
        await using var context = CreateContext(materialization);
        var seeded = await SeedCanonicalGraphAsync(context);

        for (var attemptNumber = 2; attemptNumber <= 26; attemptNumber++)
        {
            context.PrenotificationValidationAttempts.Add(new PrenotificationValidationAttempt
            {
                Id = 600 + attemptNumber,
                ThirdPartyAccountValidationId = seeded.AchValidation.Id,
                ThirdPartyAccountValidation = seeded.AchValidation,
                AttemptNumber = attemptNumber,
                IdempotencyKey = $"read-ach-{attemptNumber}",
                Status = PrenotificationValidationAttemptStatus.Accepted,
                Resolution = PrenotificationValidationResolution.Accepted,
                SentAtUtc = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero).AddMinutes(attemptNumber),
                ResolvedAtUtc = new DateTimeOffset(2026, 10, 1, 13, 0, 0, TimeSpan.Zero).AddMinutes(attemptNumber),
                SourceCycleId = $"ACH-C{attemptNumber}"
            });
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = new ThirdPartyAccountReadService(context, Mock.Of<IThirdPartyAccountEligibilityService>());
        var item = await service.GetByAccountIdAsync(seeded.Account.Id);

        Assert.NotNull(item);
        var ach = Assert.Single(item.Validations.Where(x => x.ClearingHouseId == 1));
        Assert.Equal(26, ach.LatestAttempt?.AttemptNumber);
        Assert.Equal("ACH-C26", ach.LatestAttempt?.SourceCycleId);
        Assert.Equal(2, materialization.Count<PrenotificationValidationAttempt>());
    }

    [Fact]
    public async Task CanonicalRead_ClampsPageSizeAndPagesBeforeLoadingCanonicalGraphs()
    {
        await using var context = CreateContext();
        var seeded = await SeedCanonicalGraphAsync(context);
        var additional = Enumerable.Range(1, 104)
            .Select(index => new ThirdPartyAccount
            {
                Id = 400 + index,
                ThirdPartyId = seeded.ThirdParty.Id,
                ThirdParty = seeded.ThirdParty,
                FinancialInstitutionId = seeded.Institution.Id,
                FinancialInstitution = seeded.Institution,
                AccountType = AccountTypeEnum.Savings,
                AccountNumberNormalized = $"88{index:D10}",
                Status = ThirdPartyAdministrativeStatus.Active,
                OriginSource = ThirdPartyAccountOriginSource.SingleTransaction
            })
            .ToArray();
        context.ThirdPartyAccounts.AddRange(additional);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = new ThirdPartyAccountReadService(context, Mock.Of<IThirdPartyAccountEligibilityService>());
        var first = await service.GetAsync(new ThirdPartyAccountReadQuery
        {
            CustomerId = seeded.Customer.Id,
            Page = 1,
            PageSize = 1000
        });
        var second = await service.GetAsync(new ThirdPartyAccountReadQuery
        {
            CustomerId = seeded.Customer.Id,
            Page = 2,
            PageSize = 1000
        });

        Assert.Equal(105, first.Total);
        Assert.Equal(100, first.PageSize);
        Assert.Equal(100, first.Items.Count());
        Assert.Equal(100, second.PageSize);
        Assert.Equal(5, second.Items.Count());
        Assert.Empty(first.Items.Select(x => x.ThirdPartyAccountId).Intersect(second.Items.Select(x => x.ThirdPartyAccountId)));
    }

    [Theory]
    [MemberData(nameof(ShadowClassificationCases))]
    public void ShadowClassifier_ClassifiesExpectedDiscrepancy(
        ThirdPartyAccountShadowSnapshot snapshot,
        ThirdPartyAccountShadowDiscrepancyType expected)
    {
        Assert.Equal(expected, ThirdPartyAccountShadowClassifier.Classify(snapshot));
    }

    public static IEnumerable<object[]> ShadowClassificationCases()
    {
        yield return
        [
            new ThirdPartyAccountShadowSnapshot(true, true, true, true,
                CustomerThirdPartyStatusEnum.Active,
                ThirdPartyAccountValidationStatus.Accepted,
                ThirdPartyAccountEligibilityDecision.Allowed),
            ThirdPartyAccountShadowDiscrepancyType.Match
        ];
        yield return
        [
            new ThirdPartyAccountShadowSnapshot(true, false, true, false,
                CustomerThirdPartyStatusEnum.Pending, null, null),
            ThirdPartyAccountShadowDiscrepancyType.LegacyOnly
        ];
        yield return
        [
            new ThirdPartyAccountShadowSnapshot(false, true, true, false,
                null, null, null),
            ThirdPartyAccountShadowDiscrepancyType.CanonicalOnly
        ];
        yield return
        [
            new ThirdPartyAccountShadowSnapshot(true, true, true, true,
                CustomerThirdPartyStatusEnum.Active,
                ThirdPartyAccountValidationStatus.Rejected,
                ThirdPartyAccountEligibilityDecision.Rejected),
            ThirdPartyAccountShadowDiscrepancyType.StatusMismatch
        ];
        yield return
        [
            new ThirdPartyAccountShadowSnapshot(true, true, true, true,
                CustomerThirdPartyStatusEnum.Active,
                ThirdPartyAccountValidationStatus.Accepted,
                ThirdPartyAccountEligibilityDecision.RequiresRevalidation),
            ThirdPartyAccountShadowDiscrepancyType.EligibilityMismatch
        ];
        yield return
        [
            new ThirdPartyAccountShadowSnapshot(true, true, false, false,
                CustomerThirdPartyStatusEnum.Pending, null, null),
            ThirdPartyAccountShadowDiscrepancyType.IdentityIncomplete
        ];
        yield return
        [
            new ThirdPartyAccountShadowSnapshot(true, true, true, false,
                CustomerThirdPartyStatusEnum.Active,
                ThirdPartyAccountValidationStatus.Accepted,
                null),
            ThirdPartyAccountShadowDiscrepancyType.PolicyContextMissing
        ];
    }

    [Fact]
    public async Task ShadowComparison_ReturnsMatch_ForLinkedCanonicalValidationAndEligibility()
    {
        await using var context = CreateContext();
        var seeded = await SeedCanonicalGraphAsync(context);
        var legacy = await SeedLegacyActiveAsync(context, seeded, transactionId: 501);

        context.PrenotificationValidationAttempts.Add(new PrenotificationValidationAttempt
        {
            Id = 9901,
            ThirdPartyAccountValidationId = seeded.AchValidation.Id,
            AchTransactionId = 501,
            AttemptNumber = 2,
            IdempotencyKey = "shadow:501",
            Status = PrenotificationValidationAttemptStatus.Accepted,
            Resolution = PrenotificationValidationResolution.Accepted,
            SentAtUtc = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero),
            ResolvedAtUtc = new DateTimeOffset(2026, 10, 1, 13, 0, 0, TimeSpan.Zero)
        });
        await context.SaveChangesAsync();

        var eligibility = new Mock<IThirdPartyAccountEligibilityService>();
        eligibility.Setup(x => x.EvaluateAsync(It.IsAny<ThirdPartyAccountEligibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ThirdPartyAccountEligibilityResult(
                ThirdPartyAccountEligibilityDecision.Allowed,
                "ELIGIBLE",
                "eligible",
                null,
                ThirdPartyAccountValidationStatus.Accepted,
                seeded.AchValidation.Id,
                9901));
        var logger = new CaptureLogger<ThirdPartyAccountShadowComparisonService>();
        var service = new ThirdPartyAccountShadowComparisonService(context, eligibility.Object, logger);

        var summary = await service.CompareLegacyReadAsync(
            new CustomerThirdPartyQuery { Page = 1, PageSize = 20 },
            [legacy.Id],
            1);

        Assert.Equal(1, summary.Counts[ThirdPartyAccountShadowDiscrepancyType.Match]);
        Assert.Equal(0, summary.Counts[ThirdPartyAccountShadowDiscrepancyType.StatusMismatch]);
    }

    [Fact]
    public async Task ShadowComparison_ClassifiesLegacyOnlyAndCanonicalOnlyWithoutInventingIdentity()
    {
        await using var legacyOnlyContext = CreateContext();
        var legacySeed = await SeedLegacyOnlyAsync(legacyOnlyContext);
        var eligibility = Mock.Of<IThirdPartyAccountEligibilityService>();
        var legacyService = new ThirdPartyAccountShadowComparisonService(
            legacyOnlyContext,
            eligibility,
            new CaptureLogger<ThirdPartyAccountShadowComparisonService>());

        var legacySummary = await legacyService.CompareLegacyReadAsync(
            new CustomerThirdPartyQuery(),
            [legacySeed.Id],
            1);
        Assert.Equal(1, legacySummary.Counts[ThirdPartyAccountShadowDiscrepancyType.LegacyOnly]);

        await using var canonicalOnlyContext = CreateContext();
        await SeedCanonicalGraphAsync(canonicalOnlyContext);
        var canonicalService = new ThirdPartyAccountShadowComparisonService(
            canonicalOnlyContext,
            eligibility,
            new CaptureLogger<ThirdPartyAccountShadowComparisonService>());
        var canonicalSummary = await canonicalService.CompareLegacyReadAsync(
            new CustomerThirdPartyQuery
            {
                SourceAccountNumber = SourceAccount,
                Page = 1,
                PageSize = 20
            },
            [],
            0);

        Assert.Equal(1, canonicalSummary.Compared);
        Assert.Equal(1, canonicalSummary.Counts[ThirdPartyAccountShadowDiscrepancyType.CanonicalOnly]);
    }

    [Fact]
    public async Task ShadowObservability_DoesNotLogFullAccountDocumentOrNames()
    {
        await using var context = CreateContext();
        var seeded = await SeedCanonicalGraphAsync(context);
        var legacy = await SeedLegacyActiveAsync(context, seeded, transactionId: 601);
        var logger = new CaptureLogger<ThirdPartyAccountShadowComparisonService>();
        var eligibility = new Mock<IThirdPartyAccountEligibilityService>();
        eligibility.Setup(x => x.EvaluateAsync(It.IsAny<ThirdPartyAccountEligibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ThirdPartyAccountEligibilityResult(
                ThirdPartyAccountEligibilityDecision.Allowed,
                "ELIGIBLE",
                "eligible",
                null,
                ThirdPartyAccountValidationStatus.Accepted,
                seeded.AchValidation.Id,
                seeded.AchAttempt.Id));
        var service = new ThirdPartyAccountShadowComparisonService(context, eligibility.Object, logger);

        await service.CompareLegacyReadAsync(new CustomerThirdPartyQuery(), [legacy.Id], 1);

        var log = string.Join("\n", logger.Messages);
        Assert.DoesNotContain(ThirdPartyAccountNumber, log, StringComparison.Ordinal);
        Assert.DoesNotContain(ThirdPartyDocument, log, StringComparison.Ordinal);
        Assert.DoesNotContain("Proveedor Sensible", log, StringComparison.Ordinal);
        Assert.DoesNotContain("Cliente Sensible", log, StringComparison.Ordinal);
        Assert.Contains("without PII", log, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CutoverRead_UsesCanonicalValidationAsAuthority()
    {
        await using var context = CreateContext();
        var seeded = await SeedCanonicalGraphAsync(context);
        await SeedLegacyActiveAsync(context, seeded, transactionId: 701);
        await AddLinkedAttemptAsync(
            context,
            seeded.AchValidation,
            701,
            PrenotificationValidationAttemptStatus.Accepted,
            PrenotificationValidationResolution.Accepted);

        var service = new CustomerThirdPartyAppService(context);
        var result = await service.GetAsync(new CustomerThirdPartyQuery
        {
            SourceAccountNumber = SourceAccount,
            Page = 1,
            PageSize = 20
        });

        var item = Assert.Single(result.Items);
        Assert.Equal(seeded.Account.Id, item.Id);
        Assert.Equal(CustomerThirdPartyStatusEnum.Active, item.Status);
        Assert.Equal(ThirdPartyAccountNumber, item.DestinationAccountNumber);
        Assert.Equal(ThirdPartyDocument, item.RecipientIdNumber);
        Assert.Equal(701, item.PrenotificationTransactionId);
        Assert.Contains("canónica", item.StatusSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CutoverRead_LegacyActiveCannotOverrideCanonicalRejected()
    {
        await using var context = CreateContext();
        var seeded = await SeedCanonicalGraphAsync(context);
        await SeedLegacyActiveAsync(context, seeded, transactionId: 702);
        seeded.AchValidation.Status = ThirdPartyAccountValidationStatus.Rejected;
        await context.SaveChangesAsync();
        await AddLinkedAttemptAsync(
            context,
            seeded.AchValidation,
            702,
            PrenotificationValidationAttemptStatus.Rejected,
            PrenotificationValidationResolution.Rejected,
            "R03");

        var service = new CustomerThirdPartyAppService(context);
        var active = await service.GetAsync(new CustomerThirdPartyQuery
        {
            SourceAccountNumber = SourceAccount,
            Status = CustomerThirdPartyStatusEnum.Active
        });
        var rejected = await service.GetAsync(new CustomerThirdPartyQuery
        {
            SourceAccountNumber = SourceAccount,
            Status = CustomerThirdPartyStatusEnum.Rejected
        });

        Assert.Empty(active.Items);
        var item = Assert.Single(rejected.Items);
        Assert.Equal(CustomerThirdPartyStatusEnum.Rejected, item.Status);
        Assert.Equal("ACHCOL", item.ClearingHouseCode);
    }

    [Fact]
    public async Task CutoverRead_IsolatesCanonicalAccountsByResolvedCustomer()
    {
        await using var context = CreateContext();
        var seeded = await SeedCanonicalGraphAsync(context);
        await SeedLegacyActiveAsync(context, seeded, transactionId: 703);
        await AddLinkedAttemptAsync(
            context,
            seeded.AchValidation,
            703,
            PrenotificationValidationAttemptStatus.Accepted,
            PrenotificationValidationResolution.Accepted);

        var customer2 = new Customer
        {
            Id = 110,
            FirstName = "Otro",
            LastName = "Cliente",
            CompanyName = "Otro Cliente",
            PersonType = "PJ",
            DocumentType = seeded.Customer.DocumentType,
            DocumentNumber = seeded.Customer.DocumentNumber
        };
        var source2 = new CustomerAccount
        {
            Id = 111,
            CustomerId = customer2.Id,
            Customer = customer2,
            AccountNumber = "222222222222"
        };
        customer2.Accounts.Add(source2);
        var thirdParty2 = new ThirdParty
        {
            Id = 310,
            CustomerId = customer2.Id,
            Customer = customer2,
            PersonType = "PJ",
            DocumentType = "NIT",
            DocumentNumberNormalized = ThirdPartyDocument,
            DisplayName = "Proveedor Otro",
            Status = ThirdPartyAdministrativeStatus.Active
        };
        var account2 = new ThirdPartyAccount
        {
            Id = 410,
            ThirdPartyId = thirdParty2.Id,
            ThirdParty = thirdParty2,
            FinancialInstitutionId = seeded.Institution.Id,
            FinancialInstitution = seeded.Institution,
            AccountType = AccountTypeEnum.Savings,
            AccountNumberNormalized = ThirdPartyAccountNumber,
            Status = ThirdPartyAdministrativeStatus.Active
        };
        thirdParty2.Accounts.Add(account2);
        var validation2 = new ThirdPartyAccountValidation
        {
            Id = 520,
            ThirdPartyAccountId = account2.Id,
            ThirdPartyAccount = account2,
            ClearingHouseId = 1,
            ClearingHouse = await context.ClearingHouses.FirstAsync(x => x.Id == 1),
            TransactionNature = TransactionNature.Debit,
            ServiceType = "PPD",
            Status = ThirdPartyAccountValidationStatus.Accepted,
            EffectiveDate = new DateTime(2026, 10, 3)
        };
        account2.Validations.Add(validation2);
        context.Customers.Add(customer2);
        context.CustomerAccounts.Add(source2);
        context.ThirdParties.Add(thirdParty2);
        context.ThirdPartyAccounts.Add(account2);
        context.ThirdPartyAccountValidations.Add(validation2);
        await context.SaveChangesAsync();

        var service = new CustomerThirdPartyAppService(context);
        var result = await service.GetAsync(new CustomerThirdPartyQuery
        {
            SourceAccountNumber = SourceAccount
        });

        var item = Assert.Single(result.Items);
        Assert.Equal(seeded.Customer.Id, item.CustomerId);
        Assert.NotEqual(customer2.Id, item.CustomerId);
    }

    [Fact]
    public async Task CutoverRead_LegacyOnlyFallbackStaysVisibleButNeverBecomesActiveAuthority()
    {
        await using var context = CreateContext();
        var legacy = await SeedLegacyOnlyAsync(context);
        legacy.ApplyAutomaticNachaResult(
            CustomerThirdPartyStatusEnum.Active,
            null,
            "LEGACY-C1",
            new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc),
            "legacy accepted",
            "legacy-evidence");
        await context.SaveChangesAsync();

        var service = new CustomerThirdPartyAppService(context);
        var all = await service.GetAsync(new CustomerThirdPartyQuery
        {
            SourceAccountNumber = SourceAccount
        });
        var active = await service.GetAsync(new CustomerThirdPartyQuery
        {
            SourceAccountNumber = SourceAccount,
            Status = CustomerThirdPartyStatusEnum.Active
        });

        var fallback = Assert.Single(all.Items);
        Assert.Equal((long)legacy.Id, fallback.Id);
        Assert.Equal(CustomerThirdPartyStatusEnum.Pending, fallback.Status);
        Assert.Contains("Compatibilidad legacy", fallback.StatusSource, StringComparison.Ordinal);
        Assert.Empty(active.Items);
    }

    [Fact]
    public async Task CutoverRead_MultipleCanonicalValidationContextsDoNotInferOne()
    {
        await using var context = CreateContext();
        var seeded = await SeedCanonicalGraphAsync(context);
        var service = new CustomerThirdPartyAppService(context);

        var result = await service.GetAsync(new CustomerThirdPartyQuery
        {
            SourceAccountNumber = SourceAccount
        });

        var item = Assert.Single(result.Items);
        Assert.Equal(seeded.Account.Id, item.Id);
        Assert.Equal(CustomerThirdPartyStatusEnum.Pending, item.Status);
        Assert.Null(item.PrenotificationTransactionId);
        Assert.Null(item.ClearingHouseCode);
        Assert.Contains("múltiples contextos", item.StatusSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CutoverRead_PreservesRelevantLegacyFiltersOverCanonicalMaster()
    {
        await using var context = CreateContext();
        var seeded = await SeedCanonicalGraphAsync(context);
        await SeedLegacyActiveAsync(context, seeded, transactionId: 704);
        await AddLinkedAttemptAsync(
            context,
            seeded.AchValidation,
            704,
            PrenotificationValidationAttemptStatus.Accepted,
            PrenotificationValidationResolution.Accepted);
        var service = new CustomerThirdPartyAppService(context);

        var match = await service.GetAsync(new CustomerThirdPartyQuery
        {
            DestinationInstitutionId = seeded.Institution.Id,
            DestinationAccountNumber = ThirdPartyAccountNumber[^6..],
            RecipientIdNumber = ThirdPartyDocument[^6..],
            Page = 1,
            PageSize = 1
        });
        var miss = await service.GetAsync(new CustomerThirdPartyQuery
        {
            DestinationAccountNumber = "000000"
        });

        Assert.Single(match.Items);
        Assert.Equal(1, match.Total);
        Assert.Empty(miss.Items);
    }

    [Fact]
    public async Task CutoverRead_RemainsCanonicalAuthority_WhenShadowComparisonFails()
    {
        await using var context = CreateContext();
        var seeded = await SeedCanonicalGraphAsync(context);
        await SeedLegacyActiveAsync(context, seeded, transactionId: 705);
        await AddLinkedAttemptAsync(
            context,
            seeded.AchValidation,
            705,
            PrenotificationValidationAttemptStatus.Accepted,
            PrenotificationValidationResolution.Accepted);
        var shadow = new Mock<IThirdPartyAccountShadowComparisonService>();
        shadow.Setup(x => x.CompareLegacyReadAsync(
                It.IsAny<CustomerThirdPartyQuery>(),
                It.IsAny<IReadOnlyCollection<int>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("shadow unavailable"));
        var logger = new CaptureLogger<CustomerThirdPartyAppService>();
        var service = new CustomerThirdPartyAppService(context, shadow.Object, logger);

        var result = await service.GetAsync(new CustomerThirdPartyQuery
        {
            SourceAccountNumber = SourceAccount,
            Status = CustomerThirdPartyStatusEnum.Active,
            Page = 1,
            PageSize = 20
        });

        var item = Assert.Single(result.Items);
        Assert.Equal(seeded.Account.Id, item.Id);
        Assert.Equal(CustomerThirdPartyStatusEnum.Active, item.Status);
        shadow.Verify(x => x.CompareLegacyReadAsync(
            It.IsAny<CustomerThirdPartyQuery>(),
            It.IsAny<IReadOnlyCollection<int>>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains(logger.Messages, message => message.Contains("canonical", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CutoverRead_CanonicalFinalStateRequiresMatchingLatestAttemptEvidence()
    {
        await using var context = CreateContext();
        var seeded = await SeedCanonicalGraphAsync(context);
        await SeedLegacyActiveAsync(context, seeded, transactionId: 706);
        await AddLinkedAttemptAsync(
            context,
            seeded.AchValidation,
            706,
            PrenotificationValidationAttemptStatus.Rejected,
            PrenotificationValidationResolution.Rejected,
            "R03");

        var service = new CustomerThirdPartyAppService(context);
        var all = await service.GetAsync(new CustomerThirdPartyQuery
        {
            SourceAccountNumber = SourceAccount
        });
        var active = await service.GetAsync(new CustomerThirdPartyQuery
        {
            SourceAccountNumber = SourceAccount,
            Status = CustomerThirdPartyStatusEnum.Active
        });

        var item = Assert.Single(all.Items);
        Assert.Equal(seeded.Account.Id, item.Id);
        Assert.Equal(CustomerThirdPartyStatusEnum.Pending, item.Status);
        Assert.Empty(active.Items);
    }

    [Fact]
    public async Task CutoverRead_CanonicalProjectionFailureFallsBackToLegacyPendingOnly()
    {
        await using var context = CreateContext();
        var legacy = await SeedLegacyOnlyAsync(context);
        legacy.ApplyAutomaticNachaResult(
            CustomerThirdPartyStatusEnum.Active,
            null,
            "LEGACY-CANONICAL-FAILURE",
            new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc),
            "legacy accepted",
            "legacy-evidence");

        var brokenThirdParty = new ThirdParty
        {
            Id = 1300,
            CustomerId = legacy.CustomerId,
            Customer = legacy.Customer,
            PersonType = "PN",
            DocumentType = "CC",
            DocumentNumberNormalized = "777888999",
            DisplayName = "Broken canonical projection",
            Status = ThirdPartyAdministrativeStatus.Active
        };
        var brokenAccount = new ThirdPartyAccount
        {
            Id = 1400,
            ThirdPartyId = brokenThirdParty.Id,
            ThirdParty = brokenThirdParty,
            FinancialInstitutionId = 9999,
            AccountType = AccountTypeEnum.Savings,
            AccountNumberNormalized = "111122223333",
            Status = ThirdPartyAdministrativeStatus.Active
        };
        brokenThirdParty.Accounts.Add(brokenAccount);
        context.ThirdParties.Add(brokenThirdParty);
        context.ThirdPartyAccounts.Add(brokenAccount);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var logger = new CaptureLogger<CustomerThirdPartyAppService>();
        var service = new CustomerThirdPartyAppService(context, logger: logger);
        var all = await service.GetAsync(new CustomerThirdPartyQuery
        {
            SourceAccountNumber = SourceAccount
        });
        var active = await service.GetAsync(new CustomerThirdPartyQuery
        {
            SourceAccountNumber = SourceAccount,
            Status = CustomerThirdPartyStatusEnum.Active
        });

        var fallback = Assert.Single(all.Items);
        Assert.Equal((long)legacy.Id, fallback.Id);
        Assert.Equal(CustomerThirdPartyStatusEnum.Pending, fallback.Status);
        Assert.Contains("Compatibilidad legacy", fallback.StatusSource, StringComparison.Ordinal);
        Assert.Empty(active.Items);
        Assert.Contains(logger.Messages, message =>
            message.Contains("conservative legacy fallback", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CutoverRead_AmbiguousSourceDoesNotLetShadowInferOneCustomer()
    {
        await using var context = CreateContext();
        await SeedCanonicalGraphAsync(context);
        var otherCustomer = new Customer
        {
            Id = 901,
            FirstName = "Otro",
            LastName = "Cliente",
            PersonType = "PN",
            DocumentType = "CC",
            DocumentNumber = "123123123"
        };
        var duplicateSource = new CustomerAccount
        {
            Id = 902,
            CustomerId = otherCustomer.Id,
            Customer = otherCustomer,
            AccountNumber = SourceAccount
        };
        otherCustomer.Accounts.Add(duplicateSource);
        context.Customers.Add(otherCustomer);
        context.CustomerAccounts.Add(duplicateSource);
        await context.SaveChangesAsync();

        var shadow = new Mock<IThirdPartyAccountShadowComparisonService>();
        var service = new CustomerThirdPartyAppService(
            context,
            shadow.Object,
            new CaptureLogger<CustomerThirdPartyAppService>());

        var result = await service.GetAsync(new CustomerThirdPartyQuery
        {
            SourceAccountNumber = SourceAccount
        });

        Assert.Empty(result.Items);
        shadow.Verify(x => x.CompareLegacyReadAsync(
            It.IsAny<CustomerThirdPartyQuery>(),
            It.Is<IReadOnlyCollection<int>>(ids => ids.Count == 0),
            0,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CutoverRead_ShadowFailureDoesNotLogExceptionPii()
    {
        await using var context = CreateContext();
        var seeded = await SeedCanonicalGraphAsync(context);
        await SeedLegacyActiveAsync(context, seeded, transactionId: 707);
        await AddLinkedAttemptAsync(
            context,
            seeded.AchValidation,
            707,
            PrenotificationValidationAttemptStatus.Accepted,
            PrenotificationValidationResolution.Accepted);
        var shadow = new Mock<IThirdPartyAccountShadowComparisonService>();
        shadow.Setup(x => x.CompareLegacyReadAsync(
                It.IsAny<CustomerThirdPartyQuery>(),
                It.IsAny<IReadOnlyCollection<int>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                $"sensitive {ThirdPartyAccountNumber} {ThirdPartyDocument} Proveedor Sensible Cliente Sensible"));
        var logger = new CaptureLogger<CustomerThirdPartyAppService>();
        var service = new CustomerThirdPartyAppService(context, shadow.Object, logger);

        var result = await service.GetAsync(new CustomerThirdPartyQuery
        {
            SourceAccountNumber = SourceAccount,
            Status = CustomerThirdPartyStatusEnum.Active
        });

        Assert.Single(result.Items);
        var log = string.Join("\n", logger.Messages);
        Assert.DoesNotContain(ThirdPartyAccountNumber, log, StringComparison.Ordinal);
        Assert.DoesNotContain(ThirdPartyDocument, log, StringComparison.Ordinal);
        Assert.DoesNotContain("Proveedor Sensible", log, StringComparison.Ordinal);
        Assert.DoesNotContain("Cliente Sensible", log, StringComparison.Ordinal);
        Assert.Contains(nameof(InvalidOperationException), log, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CutoverRead_PreCanceledRequestPropagatesAndDoesNotRunShadow()
    {
        await using var context = CreateContext();
        await SeedCanonicalGraphAsync(context);
        var shadow = new Mock<IThirdPartyAccountShadowComparisonService>();
        var service = new CustomerThirdPartyAppService(context, shadow.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.GetAsync(new CustomerThirdPartyQuery { SourceAccountNumber = SourceAccount }, cts.Token));

        shadow.Verify(x => x.CompareLegacyReadAsync(
            It.IsAny<CustomerThirdPartyQuery>(),
            It.IsAny<IReadOnlyCollection<int>>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static async Task AddLinkedAttemptAsync(
        AchDbContext context,
        ThirdPartyAccountValidation validation,
        int transactionId,
        PrenotificationValidationAttemptStatus status,
        PrenotificationValidationResolution resolution,
        string? returnCode = null)
    {
        var nextAttempt = validation.Attempts.Count == 0
            ? 1
            : validation.Attempts.Max(x => x.AttemptNumber) + 1;
        var attempt = new PrenotificationValidationAttempt
        {
            ThirdPartyAccountValidationId = validation.Id,
            ThirdPartyAccountValidation = validation,
            AchTransactionId = transactionId,
            AttemptNumber = nextAttempt,
            IdempotencyKey = $"cutover:{validation.Id}:{transactionId}",
            Status = status,
            Resolution = resolution,
            SentAtUtc = new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero),
            ResolvedAtUtc = new DateTimeOffset(2026, 10, 5, 13, 0, 0, TimeSpan.Zero),
            ReturnCode = returnCode,
            SourceCycleId = $"CUTOVER-{transactionId}"
        };
        validation.Attempts.Add(attempt);
        context.PrenotificationValidationAttempts.Add(attempt);
        await context.SaveChangesAsync();
    }

    private static AchDbContext CreateContext(params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString());
        if (interceptors.Length > 0)
        {
            options.AddInterceptors(interceptors);
        }

        return new AchDbContext(options.Options);
    }

    private static async Task<CanonicalSeed> SeedCanonicalGraphAsync(AchDbContext context)
    {
        var customer = new Customer
        {
            Id = 100,
            FirstName = "Cliente",
            LastName = "Sensible",
            CompanyName = "Cliente Sensible",
            PersonType = "PJ",
            DocumentType = "NIT",
            DocumentNumber = "800555111"
        };
        var source = new CustomerAccount
        {
            Id = 101,
            CustomerId = customer.Id,
            Customer = customer,
            AccountNumber = SourceAccount
        };
        customer.Accounts.Add(source);

        var institution = new FinancialInstitution
        {
            Id = 200,
            Name = "Banco Destino",
            RoutingNumber = "12345678",
            TransitCode = "001"
        };
        var ach = new ClearingHouse
        {
            Id = 1,
            ClearingHouseId = 1,
            Code = "ACHCOL",
            Name = "ACH Colombia",
            OriginCode = "00000001"
        };
        var cenit = new ClearingHouse
        {
            Id = 2,
            ClearingHouseId = 2,
            Code = "CENIT",
            Name = "CENIT",
            OriginCode = "00000002"
        };
        var thirdParty = new ThirdParty
        {
            Id = 300,
            CustomerId = customer.Id,
            Customer = customer,
            PersonType = "PJ",
            DocumentType = "NIT",
            DocumentNumberNormalized = ThirdPartyDocument,
            DisplayName = "Proveedor Sensible",
            Status = ThirdPartyAdministrativeStatus.Active
        };
        var account = new ThirdPartyAccount
        {
            Id = 400,
            ThirdPartyId = thirdParty.Id,
            ThirdParty = thirdParty,
            FinancialInstitutionId = institution.Id,
            FinancialInstitution = institution,
            AccountType = AccountTypeEnum.Savings,
            AccountNumberNormalized = ThirdPartyAccountNumber,
            Status = ThirdPartyAdministrativeStatus.Active,
            OriginSource = ThirdPartyAccountOriginSource.SingleTransaction
        };
        thirdParty.Accounts.Add(account);

        var companyEntryDescription = new CompanyEntryDescriptionCatalog
        {
            Id = 600,
            Term = "PAGOS",
            Description = "PPD test",
            StandardEntryClassCode = "PPD",
            IsActive = true
        };

        var achValidation = new ThirdPartyAccountValidation
        {
            Id = 500,
            ThirdPartyAccountId = account.Id,
            ThirdPartyAccount = account,
            ClearingHouseId = ach.Id,
            ClearingHouse = ach,
            TransactionNature = TransactionNature.Debit,
            ServiceType = "PPD",
            Status = ThirdPartyAccountValidationStatus.Accepted,
            EffectiveDate = new DateTime(2026, 10, 1),
            PolicyRuleId = 700,
            PolicyEffectiveFrom = new DateTime(2026, 4, 1)
        };
        var achAttempt = new PrenotificationValidationAttempt
        {
            Id = 501,
            ThirdPartyAccountValidationId = achValidation.Id,
            ThirdPartyAccountValidation = achValidation,
            AttemptNumber = 1,
            IdempotencyKey = "read-ach-1",
            Status = PrenotificationValidationAttemptStatus.Accepted,
            Resolution = PrenotificationValidationResolution.Accepted,
            SentAtUtc = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero),
            ResolvedAtUtc = new DateTimeOffset(2026, 10, 1, 13, 0, 0, TimeSpan.Zero),
            SourceCycleId = "ACH-C1"
        };
        achValidation.Attempts.Add(achAttempt);

        var cenitValidation = new ThirdPartyAccountValidation
        {
            Id = 510,
            ThirdPartyAccountId = account.Id,
            ThirdPartyAccount = account,
            ClearingHouseId = cenit.Id,
            ClearingHouse = cenit,
            TransactionNature = TransactionNature.Credit,
            ServiceType = "PPD",
            Status = ThirdPartyAccountValidationStatus.Rejected,
            EffectiveDate = new DateTime(2026, 10, 2),
            PolicyRuleId = 710,
            PolicyEffectiveFrom = new DateTime(2025, 2, 27)
        };
        var cenitAttempt = new PrenotificationValidationAttempt
        {
            Id = 511,
            ThirdPartyAccountValidationId = cenitValidation.Id,
            ThirdPartyAccountValidation = cenitValidation,
            AttemptNumber = 1,
            IdempotencyKey = "read-cenit-1",
            Status = PrenotificationValidationAttemptStatus.Rejected,
            Resolution = PrenotificationValidationResolution.Rejected,
            SentAtUtc = new DateTimeOffset(2026, 10, 2, 12, 0, 0, TimeSpan.Zero),
            ResolvedAtUtc = new DateTimeOffset(2026, 10, 2, 13, 0, 0, TimeSpan.Zero),
            ReturnCode = "R03",
            ReturnDetail = "Cuenta inexistente",
            EvidenceReference = "sensitive-evidence-ref",
            CorrelationReference = "sensitive-correlation-ref",
            SourceFileReference = "sensitive-file-ref",
            SourceCycleId = "CENIT-C2"
        };
        cenitValidation.Attempts.Add(cenitAttempt);
        account.Validations.Add(achValidation);
        account.Validations.Add(cenitValidation);

        context.Customers.Add(customer);
        context.CustomerAccounts.Add(source);
        context.FinancialInstitutions.Add(institution);
        context.ClearingHouses.AddRange(ach, cenit);
        context.ThirdParties.Add(thirdParty);
        context.ThirdPartyAccounts.Add(account);
        context.CompanyEntryDescriptionCatalogs.Add(companyEntryDescription);
        context.ThirdPartyAccountValidations.AddRange(achValidation, cenitValidation);
        context.PrenotificationValidationAttempts.AddRange(achAttempt, cenitAttempt);
        await context.SaveChangesAsync();

        return new CanonicalSeed(customer, institution, thirdParty, account, achValidation, achAttempt);
    }

    private static async Task<CustomerThirdParty> SeedLegacyActiveAsync(
        AchDbContext context,
        CanonicalSeed seed,
        int transactionId)
    {
        var cycle = new AchCycle
        {
            Id = $"ACH-SHADOW-{transactionId}",
            CycleName = "Shadow",
            ProcessingDate = new DateTime(2026, 10, 1),
            CutoffTime = new TimeSpan(10, 0, 0),
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(10, 0, 0),
            ClearingHouseId = 1,
            ClearingHouse = await context.ClearingHouses.FirstAsync(x => x.Id == 1)
        };
        var transaction = new AchTransaction
        {
            Id = transactionId,
            Reference = $"shadow-{transactionId}",
            TransactionExternalId = $"shadow-{transactionId}",
            Type = TransactionTypeEnum.Debit,
            TransactionCode = "38",
            ServiceClassCode = "225",
            CompanyEntryDescriptionId = 600,
            IsPrenotification = true,
            EffectiveEntryDate = new DateTime(2026, 10, 1),
            SourceAccountNumber = SourceAccount,
            DestinationAccountNumber = ThirdPartyAccountNumber,
            SourceInstitutionId = 999,
            DestinationInstitutionId = seed.Institution.Id,
            AchCycleId = cycle.Id,
            AchCycle = cycle,
            State = AchTransferStateEnum.Certified,
            StateChangedAtUtc = new DateTime(2026, 10, 1, 13, 0, 0, DateTimeKind.Utc)
        };
        var legacy = new CustomerThirdParty
        {
            Id = transactionId + 1000,
            CustomerId = seed.Customer.Id,
            Customer = seed.Customer,
            DestinationInstitutionId = seed.Institution.Id,
            DestinationInstitution = seed.Institution,
            DestinationAccountNumber = ThirdPartyAccountNumber,
            RecipientIdNumber = ThirdPartyDocument
        };
        legacy.LinkPendingPrenotification(transaction);
        legacy.ApplyAutomaticNachaResult(
            CustomerThirdPartyStatusEnum.Active,
            transactionId,
            cycle.Id,
            transaction.StateChangedAtUtc,
            "automatic",
            "automatic-evidence");

        context.AchCycles.Add(cycle);
        context.AchTransactions.Add(transaction);
        context.CustomerThirdParties.Add(legacy);
        await context.SaveChangesAsync();
        return legacy;
    }

    private static async Task<CustomerThirdParty> SeedLegacyOnlyAsync(AchDbContext context)
    {
        var customer = new Customer
        {
            Id = 100,
            FirstName = "Cliente",
            LastName = "Sensible",
            CompanyName = "Cliente Sensible",
            PersonType = "PJ",
            DocumentType = "NIT",
            DocumentNumber = "800555111"
        };
        var source = new CustomerAccount
        {
            Id = 101,
            CustomerId = customer.Id,
            Customer = customer,
            AccountNumber = SourceAccount
        };
        customer.Accounts.Add(source);
        var institution = new FinancialInstitution
        {
            Id = 200,
            Name = "Banco Destino",
            RoutingNumber = "12345678",
            TransitCode = "001"
        };
        var legacy = new CustomerThirdParty
        {
            Id = 1200,
            CustomerId = customer.Id,
            Customer = customer,
            DestinationInstitutionId = institution.Id,
            DestinationInstitution = institution,
            DestinationAccountNumber = ThirdPartyAccountNumber,
            RecipientIdNumber = ThirdPartyDocument
        };
        context.Customers.Add(customer);
        context.CustomerAccounts.Add(source);
        context.FinancialInstitutions.Add(institution);
        context.CustomerThirdParties.Add(legacy);
        await context.SaveChangesAsync();
        return legacy;
    }

    private sealed class EntityMaterializationCounter : IMaterializationInterceptor
    {
        private readonly Dictionary<Type, int> _counts = [];

        public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
        {
            var type = entity.GetType();
            _counts[type] = _counts.TryGetValue(type, out var count) ? count + 1 : 1;
            return entity;
        }

        public int Count<T>() => _counts.TryGetValue(typeof(T), out var count) ? count : 0;
    }

    private sealed record CanonicalSeed(
        Customer Customer,
        FinancialInstitution Institution,
        ThirdParty ThirdParty,
        ThirdPartyAccount Account,
        ThirdPartyAccountValidation AchValidation,
        PrenotificationValidationAttempt AchAttempt);

    private sealed class CaptureLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Messages.Add(exception is null
                ? formatter(state, exception)
                : $"{formatter(state, exception)} | {exception.GetType().Name}: {exception.Message}");
    }
}
