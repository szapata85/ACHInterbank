using System.Reflection;
using System.Text.Json;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class ThirdPartyAdministrationApiTests
{
    private const string ThirdPartyDocument = "900123456";
    private const string ThirdPartyAccountNumber = "987654321098";
    private const string SourceAccountNumber = "123456789012";

    [Fact]
    public async Task ThirdParty_CreateUpdateDeactivate_IsIdempotent_AndDuplicateMetadataConflicts()
    {
        await using var context = CreateContext();
        var seed = await SeedReferencesAsync(context);
        var service = CreateAdministrationService(context);

        var created = await service.CreateThirdPartyAsync(seed.Customer1.Id,
            new CreateThirdPartyCommand("NIT", ThirdPartyDocument, "Proveedor Uno", "PJ"));
        var repeated = await service.CreateThirdPartyAsync(seed.Customer1.Id,
            new CreateThirdPartyCommand("NIT", ThirdPartyDocument, "Proveedor Uno", "PJ"));

        Assert.True(created.Created);
        Assert.False(repeated.Created);
        Assert.True(repeated.ReusedExisting);
        Assert.Equal(created.Value.ThirdPartyId, repeated.Value.ThirdPartyId);
        Assert.Equal("****3456", created.Value.DocumentNumberMasked);
        Assert.DoesNotContain(ThirdPartyDocument, JsonSerializer.Serialize(created.Value), StringComparison.Ordinal);

        var duplicateConflict = await Assert.ThrowsAsync<ThirdPartyAdministrationConflictException>(() =>
            service.CreateThirdPartyAsync(seed.Customer1.Id,
                new CreateThirdPartyCommand("NIT", ThirdPartyDocument, "Otro Nombre", "PJ")));
        Assert.Equal("DUPLICATE_IDENTITY_CONFLICT", duplicateConflict.Code);

        var identityMetadataConflict = await Assert.ThrowsAsync<ThirdPartyAdministrationConflictException>(() =>
            service.CreateThirdPartyAsync(seed.Customer1.Id,
                new CreateThirdPartyCommand("NIT", ThirdPartyDocument, "Proveedor Uno", "PN")));
        Assert.Equal("IDENTITY_METADATA_CONFLICT", identityMetadataConflict.Code);

        var updated = await service.UpdateThirdPartyAsync(
            seed.Customer1.Id,
            created.Value.ThirdPartyId,
            new UpdateThirdPartyCommand("Proveedor Actualizado"));
        Assert.NotNull(updated);
        Assert.Equal("Proveedor Actualizado", updated.DisplayName);
        Assert.Equal([nameof(UpdateThirdPartyCommand.DisplayName)],
            typeof(UpdateThirdPartyCommand).GetProperties().Select(x => x.Name).ToArray());

        var deactivated = await service.SetThirdPartyStatusAsync(
            seed.Customer1.Id,
            created.Value.ThirdPartyId,
            new SetThirdPartyAdministrativeStatusCommand(ThirdPartyAdministrativeStatus.Inactive));
        Assert.Equal(ThirdPartyAdministrativeStatus.Inactive, deactivated?.Status);
    }

    [Fact]
    public async Task Account_CreateUpdateDeactivate_ReusesCanonicalNaturalKey_AndKeepsBankIdentityImmutable()
    {
        await using var context = CreateContext();
        var seed = await SeedReferencesAsync(context);
        var service = CreateAdministrationService(context);
        var thirdParty = await service.CreateThirdPartyAsync(seed.Customer1.Id,
            new CreateThirdPartyCommand("NIT", ThirdPartyDocument, "Proveedor", "PJ"));

        var first = await service.CreateAccountAsync(seed.Customer1.Id, thirdParty.Value.ThirdPartyId,
            new CreateThirdPartyAccountCommand(seed.Institution.Id, AccountTypeEnum.Savings, ThirdPartyAccountNumber));
        var second = await service.CreateAccountAsync(seed.Customer1.Id, thirdParty.Value.ThirdPartyId,
            new CreateThirdPartyAccountCommand(seed.Institution.Id, AccountTypeEnum.Savings, ThirdPartyAccountNumber));

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.True(first.Created);
        Assert.False(second.Created);
        Assert.Equal(first.Value.ThirdPartyAccountId, second.Value.ThirdPartyAccountId);
        Assert.Equal("****1098", first.Value.AccountNumberMasked);
        Assert.Equal(ThirdPartyAccountOriginSource.Manual, first.Value.OriginSource);

        var updateProperties = typeof(UpdateThirdPartyAccountCommand).GetProperties().Select(x => x.Name).ToArray();
        Assert.Equal([nameof(UpdateThirdPartyAccountCommand.Status)], updateProperties);

        var updated = await service.UpdateAccountAsync(
            seed.Customer1.Id,
            thirdParty.Value.ThirdPartyId,
            first.Value.ThirdPartyAccountId,
            new UpdateThirdPartyAccountCommand(ThirdPartyAdministrativeStatus.Inactive));
        Assert.Equal(ThirdPartyAdministrativeStatus.Inactive, updated?.AccountStatus);

        var reactivated = await service.SetAccountStatusAsync(
            seed.Customer1.Id,
            thirdParty.Value.ThirdPartyId,
            first.Value.ThirdPartyAccountId,
            new SetThirdPartyAdministrativeStatusCommand(ThirdPartyAdministrativeStatus.Active));
        Assert.Equal(ThirdPartyAdministrativeStatus.Active, reactivated?.AccountStatus);
    }

    [Fact]
    public async Task ReadsAndCommands_AreIsolatedByCustomerThirdPartyAndAccount()
    {
        await using var context = CreateContext();
        var seed = await SeedReferencesAsync(context);
        var service = CreateAdministrationService(context);
        var thirdParty = await service.CreateThirdPartyAsync(seed.Customer1.Id,
            new CreateThirdPartyCommand("NIT", ThirdPartyDocument, "Proveedor", "PJ"));
        var account = await service.CreateAccountAsync(seed.Customer1.Id, thirdParty.Value.ThirdPartyId,
            new CreateThirdPartyAccountCommand(seed.Institution.Id, AccountTypeEnum.Savings, ThirdPartyAccountNumber));
        Assert.NotNull(account);

        var foreignList = await service.GetThirdPartiesAsync(
            seed.Customer2.Id,
            new ThirdPartyAdministrationQuery(null, null, 1, 20));
        Assert.Empty(foreignList.Items);
        Assert.Null(await service.GetThirdPartyAsync(seed.Customer2.Id, thirdParty.Value.ThirdPartyId));
        Assert.Null(await service.UpdateThirdPartyAsync(
            seed.Customer2.Id,
            thirdParty.Value.ThirdPartyId,
            new UpdateThirdPartyCommand("Intruso")));
        Assert.Null(await service.SetThirdPartyStatusAsync(
            seed.Customer2.Id,
            thirdParty.Value.ThirdPartyId,
            new SetThirdPartyAdministrativeStatusCommand(ThirdPartyAdministrativeStatus.Inactive)));
        Assert.Null(await service.CreateAccountAsync(
            seed.Customer2.Id,
            thirdParty.Value.ThirdPartyId,
            new CreateThirdPartyAccountCommand(seed.Institution.Id, AccountTypeEnum.Savings, "111122223333")));
        Assert.Null(await service.UpdateAccountAsync(
            seed.Customer2.Id,
            thirdParty.Value.ThirdPartyId,
            account.Value.ThirdPartyAccountId,
            new UpdateThirdPartyAccountCommand(ThirdPartyAdministrativeStatus.Inactive)));
        Assert.Null(await service.SetAccountStatusAsync(
            seed.Customer2.Id,
            thirdParty.Value.ThirdPartyId,
            account.Value.ThirdPartyAccountId,
            new SetThirdPartyAdministrativeStatusCommand(ThirdPartyAdministrativeStatus.Inactive)));
        Assert.Null(await service.SetAccountStatusAsync(
            seed.Customer1.Id,
            thirdParty.Value.ThirdPartyId + 999,
            account.Value.ThirdPartyAccountId,
            new SetThirdPartyAdministrativeStatusCommand(ThirdPartyAdministrativeStatus.Inactive)));

        var persistedAccount = await context.ThirdPartyAccounts.AsNoTracking()
            .SingleAsync(x => x.Id == account.Value.ThirdPartyAccountId);
        Assert.Equal(ThirdPartyAdministrativeStatus.Active, persistedAccount.Status);
        Assert.Equal("Proveedor", (await context.ThirdParties.AsNoTracking()
            .SingleAsync(x => x.Id == thirdParty.Value.ThirdPartyId)).DisplayName);
    }

    [Fact]
    public async Task AdministrativeRead_DoesNotTrackCanonicalMasterGraph()
    {
        await using var context = CreateContext();
        var seed = await SeedCanonicalAccountAsync(context);
        context.ChangeTracker.Clear();
        var service = CreateAdministrationService(context);

        var result = await service.GetThirdPartyAsync(seed.Customer.Id, seed.ThirdParty.Id);

        Assert.NotNull(result);
        Assert.Equal(1, result.AccountCount);
        Assert.Empty(context.ChangeTracker.Entries<ThirdParty>());
        Assert.Empty(context.ChangeTracker.Entries<ThirdPartyAccount>());
    }

    [Fact]
    public async Task AccountStatusCommand_ValidatesOwnershipWithoutMaterializingThirdPartyRelationship()
    {
        await using var context = CreateContext();
        var seed = await SeedCanonicalAccountAsync(context);
        context.ChangeTracker.Clear();
        var service = CreateAdministrationService(context);

        var result = await service.SetAccountStatusAsync(
            seed.Customer.Id,
            seed.ThirdParty.Id,
            seed.Account.Id,
            new SetThirdPartyAdministrativeStatusCommand(ThirdPartyAdministrativeStatus.Inactive));

        Assert.NotNull(result);
        Assert.Equal(ThirdPartyAdministrativeStatus.Inactive, result.AccountStatus);
        Assert.Empty(context.ChangeTracker.Entries<ThirdParty>());
        Assert.Single(context.ChangeTracker.Entries<ThirdPartyAccount>());
    }

    [Fact]
    public async Task UpdateThirdParty_NoOpPreservesAdministrativeResultWithoutAdditionalPersistenceWork()
    {
        await using var context = CreateContext();
        var seed = await SeedReferencesAsync(context);
        var service = CreateAdministrationService(context);
        var created = await service.CreateThirdPartyAsync(seed.Customer1.Id,
            new CreateThirdPartyCommand("NIT", ThirdPartyDocument, "Proveedor Uno", "PJ"));
        var auditCountBefore = await context.AuditLogs.CountAsync();
        context.ChangeTracker.Clear();

        var result = await service.UpdateThirdPartyAsync(
            seed.Customer1.Id,
            created.Value.ThirdPartyId,
            new UpdateThirdPartyCommand("Proveedor Uno"));

        Assert.NotNull(result);
        Assert.Equal("Proveedor Uno", result.DisplayName);
        Assert.Equal(auditCountBefore, await context.AuditLogs.CountAsync());
    }

    [Fact]
    public async Task ManualAndSilentOnboarding_ConvergeOnSameMaster()
    {
        await using var context = CreateContext();
        var seed = await SeedReferencesAsync(context);
        var onboarding = new ThirdPartyOnboardingService(context, NullLogger<ThirdPartyOnboardingService>.Instance);
        var service = CreateAdministrationService(context, onboarding);

        var manual = await service.CreateThirdPartyAsync(seed.Customer1.Id,
            new CreateThirdPartyCommand("NIT", ThirdPartyDocument, "Proveedor", "PJ"));
        var silent = await onboarding.EnsureAccountAsync(new ThirdPartyOnboardingRequest(
            seed.Customer1.Id,
            "NIT",
            ThirdPartyDocument,
            "Proveedor",
            "PJ",
            seed.Institution.Id,
            AccountTypeEnum.Savings,
            ThirdPartyAccountNumber,
            ThirdPartyAccountOriginSource.SingleTransaction));

        Assert.Equal(manual.Value.ThirdPartyId, silent.ThirdPartyId);
        Assert.False(silent.ThirdPartyCreated);
        Assert.Single(await context.ThirdParties.Where(x => x.CustomerId == seed.Customer1.Id).ToListAsync());
    }

    [Fact]
    public async Task AuditAndAdministrativeReads_DoNotLeakFullDocumentOrAccountNumbers()
    {
        await using var context = CreateContext();
        context.AuditEnabled = false;
        var seed = await SeedReferencesAsync(context);
        context.ChangeTracker.Clear();
        context.AuditEnabled = true;
        var service = CreateAdministrationService(context);

        var thirdParty = await service.CreateThirdPartyAsync(seed.Customer1.Id,
            new CreateThirdPartyCommand("NIT", ThirdPartyDocument, "Nombre Sensible", "PJ"));
        var account = await service.CreateAccountAsync(seed.Customer1.Id, thirdParty.Value.ThirdPartyId,
            new CreateThirdPartyAccountCommand(seed.Institution.Id, AccountTypeEnum.Savings, ThirdPartyAccountNumber));

        var auditPayload = string.Join('|', await context.AuditLogs.AsNoTracking()
            .Select(x => (x.BeforeJson ?? string.Empty) + (x.AfterJson ?? string.Empty) + (x.ChangedFields ?? string.Empty))
            .ToListAsync());
        Assert.DoesNotContain(ThirdPartyDocument, auditPayload, StringComparison.Ordinal);
        Assert.DoesNotContain(ThirdPartyAccountNumber, auditPayload, StringComparison.Ordinal);
        Assert.DoesNotContain("Nombre Sensible", auditPayload, StringComparison.Ordinal);

        Assert.NotNull(account);
        Assert.Equal("****3456", thirdParty.Value.DocumentNumberMasked);
        Assert.Equal("****1098", account.Value.AccountNumberMasked);
        Assert.DoesNotContain(ThirdPartyDocument, JsonSerializer.Serialize(thirdParty.Value), StringComparison.Ordinal);
        Assert.DoesNotContain(ThirdPartyAccountNumber, JsonSerializer.Serialize(account.Value), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ValidationHistory_ReturnsAllAttempts_WithoutEvidenceCorrelationOrDetail()
    {
        await using var context = CreateContext();
        var seed = await SeedCanonicalAccountAsync(context);
        var read = new ThirdPartyAccountReadService(context, Mock.Of<IThirdPartyAccountEligibilityService>());
        var validation = new ThirdPartyAccountValidation
        {
            Id = 700,
            ThirdPartyAccountId = seed.Account.Id,
            ClearingHouseId = seed.ClearingHouse.Id,
            ClearingHouse = seed.ClearingHouse,
            TransactionNature = TransactionNature.Debit,
            ServiceType = "PPD",
            Status = ThirdPartyAccountValidationStatus.Rejected,
            EffectiveDate = new DateTime(2026, 10, 11)
        };
        validation.Attempts.Add(new PrenotificationValidationAttempt
        {
            Id = 701,
            ThirdPartyAccountValidationId = validation.Id,
            AttemptNumber = 1,
            IdempotencyKey = "sensitive-key-1",
            Status = PrenotificationValidationAttemptStatus.Rejected,
            Resolution = PrenotificationValidationResolution.Rejected,
            SentAtUtc = DateTimeOffset.UtcNow.AddHours(-2),
            ReturnCode = "R03",
            ReturnDetail = "sensitive detail",
            EvidenceReference = "sensitive evidence",
            CorrelationReference = "sensitive correlation",
            SourceFileReference = "sensitive file",
            SourceCycleId = "C1"
        });
        validation.Attempts.Add(new PrenotificationValidationAttempt
        {
            Id = 702,
            ThirdPartyAccountValidationId = validation.Id,
            AttemptNumber = 2,
            IdempotencyKey = "sensitive-key-2",
            Status = PrenotificationValidationAttemptStatus.AwaitingResponse,
            Resolution = PrenotificationValidationResolution.None,
            SentAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10),
            SourceCycleId = "C2"
        });
        context.ThirdPartyAccountValidations.Add(validation);
        context.PrenotificationValidationAttempts.AddRange(validation.Attempts);
        await context.SaveChangesAsync();

        var history = await read.GetValidationHistoryAsync(seed.Account.Id, seed.Customer.Id, seed.ThirdParty.Id);

        var item = Assert.Single(history!);
        Assert.Equal(2, item.Attempts.Count);
        Assert.Equal(2, item.Attempts[0].AttemptNumber);
        var json = JsonSerializer.Serialize(item);
        Assert.DoesNotContain("sensitive detail", json, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive evidence", json, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive correlation", json, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive file", json, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-key", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Revalidation_UsesPolicyResolver_IsIdempotent_AndCannotTargetAnotherCustomer()
    {
        await using var context = CreateContext();
        var seed = await SeedCanonicalAccountAsync(context);
        context.CompanyEntryDescriptionCatalogs.AddRange(
            new CompanyEntryDescriptionCatalog
            {
                Id = 900,
                Term = "PAGOS",
                Description = "PPD",
                StandardEntryClassCode = "PPD",
                IsActive = true
            },
            new CompanyEntryDescriptionCatalog
            {
                Id = 901,
                Term = "PAGOS-CCD",
                Description = "CCD",
                StandardEntryClassCode = "CCD",
                IsActive = true
            });
        var secondAccount = new ThirdPartyAccount
        {
            Id = 61,
            ThirdPartyId = seed.ThirdParty.Id,
            ThirdParty = seed.ThirdParty,
            FinancialInstitutionId = seed.Institution.Id,
            FinancialInstitution = seed.Institution,
            AccountType = AccountTypeEnum.Savings,
            AccountNumberNormalized = "987654321099",
            Status = ThirdPartyAdministrativeStatus.Active,
            OriginSource = ThirdPartyAccountOriginSource.Manual
        };
        seed.ThirdParty.Accounts.Add(secondAccount);
        context.ThirdPartyAccounts.Add(secondAccount);
        await context.SaveChangesAsync();

        var policy = new Mock<IClearingHouseValidationPolicyService>();
        policy.Setup(x => x.ResolveAsync(
                seed.ClearingHouse.Id,
                TransactionNature.Debit,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClearingHouseValidationPolicyResolution(
                new ClearingHouseTransactionRule
                {
                    Id = 88,
                    ClearingHouseId = seed.ClearingHouse.Id,
                    TransactionNature = TransactionNature.Debit,
                    TransactionType = TransactionTypeEnum.Debit,
                    PrenotificationMode = PrenotificationRequirementMode.Mandatory,
                    EffectiveFrom = new DateTime(2026, 1, 1),
                    IsActive = true
                },
                new DateTime(2026, 10, 11),
                TransactionNature.Debit,
                seed.ClearingHouse.Id));

        var tx = new Mock<IAchTransactionService>();
        tx.Setup(x => x.RegisterCanonicalPrenotificationAsync(
                It.IsAny<CanonicalPrenotificationTransactionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CanonicalPrenotificationTransactionRequest request, CancellationToken _) =>
            {
                var validation = new ThirdPartyAccountValidation
                {
                    Id = 801,
                    ThirdPartyAccountId = request.ThirdPartyAccountId,
                    ClearingHouseId = request.ExpectedClearingHouseId,
                    TransactionNature = request.TransactionNature,
                    ServiceType = "PPD",
                    ValidationType = ThirdPartyAccountValidationType.Prenotification,
                    Status = ThirdPartyAccountValidationStatus.PendingResponse,
                    EffectiveDate = request.ExpectedEffectiveDate
                };
                var attempt = new PrenotificationValidationAttempt
                {
                    Id = 802,
                    ThirdPartyAccountValidationId = validation.Id,
                    ThirdPartyAccountValidation = validation,
                    AchTransactionId = 803,
                    AttemptNumber = 1,
                    IdempotencyKey = request.ValidationIdempotencyKey,
                    Status = PrenotificationValidationAttemptStatus.AwaitingResponse,
                    Resolution = PrenotificationValidationResolution.None,
                    SentAtUtc = DateTimeOffset.UtcNow
                };
                validation.Attempts.Add(attempt);
                context.ThirdPartyAccountValidations.Add(validation);
                context.PrenotificationValidationAttempts.Add(attempt);
                context.SaveChanges();
                return new AchTransaction { Id = 803 };
            });

        var logger = new Mock<ILogger<ThirdPartyAccountRevalidationService>>();
        var service = new ThirdPartyAccountRevalidationService(
            context,
            tx.Object,
            policy.Object,
            logger.Object);
        var command = new RequestThirdPartyAccountRevalidationCommand(
            seed.ClearingHouse.Id,
            TransactionNature.Debit,
            new DateTime(2026, 10, 11),
            "PPD",
            900,
            SourceAccountNumber,
            "tpa5-revalidate-1");

        var first = await service.RequestAsync(seed.Customer.Id, seed.ThirdParty.Id, seed.Account.Id, command);
        var second = await service.RequestAsync(seed.Customer.Id, seed.ThirdParty.Id, seed.Account.Id, command);
        var wrongCustomer = await service.RequestAsync(seed.Customer.Id + 999, seed.ThirdParty.Id, seed.Account.Id, command);
        var wrongParent = await service.RequestAsync(seed.Customer.Id, seed.ThirdParty.Id + 999, seed.Account.Id, command);
        var wrongAccount = await service.RequestAsync(seed.Customer.Id, seed.ThirdParty.Id, seed.Account.Id + 999, command);
        var crossAccountConflict = await Assert.ThrowsAsync<ThirdPartyAdministrationConflictException>(() =>
            service.RequestAsync(seed.Customer.Id, seed.ThirdParty.Id, secondAccount.Id, command));
        var effectiveDateConflict = await Assert.ThrowsAsync<ThirdPartyAdministrationConflictException>(() =>
            service.RequestAsync(
                seed.Customer.Id,
                seed.ThirdParty.Id,
                seed.Account.Id,
                new RequestThirdPartyAccountRevalidationCommand(
                    seed.ClearingHouse.Id,
                    TransactionNature.Debit,
                    command.EffectiveDate.AddDays(1),
                    "PPD",
                    900,
                    SourceAccountNumber,
                    command.IdempotencyKey)));
        var serviceTypeConflict = await Assert.ThrowsAsync<ThirdPartyAdministrationConflictException>(() =>
            service.RequestAsync(
                seed.Customer.Id,
                seed.ThirdParty.Id,
                seed.Account.Id,
                new RequestThirdPartyAccountRevalidationCommand(
                    seed.ClearingHouse.Id,
                    TransactionNature.Debit,
                    command.EffectiveDate,
                    "CCD",
                    901,
                    SourceAccountNumber,
                    command.IdempotencyKey)));
        var clearingHouseConflict = await Assert.ThrowsAsync<ThirdPartyAdministrationConflictException>(() =>
            service.RequestAsync(
                seed.Customer.Id,
                seed.ThirdParty.Id,
                seed.Account.Id,
                new RequestThirdPartyAccountRevalidationCommand(
                    seed.ClearingHouse.Id + 1,
                    TransactionNature.Debit,
                    command.EffectiveDate,
                    "PPD",
                    900,
                    SourceAccountNumber,
                    command.IdempotencyKey)));
        var natureConflict = await Assert.ThrowsAsync<ThirdPartyAdministrationConflictException>(() =>
            service.RequestAsync(
                seed.Customer.Id,
                seed.ThirdParty.Id,
                seed.Account.Id,
                new RequestThirdPartyAccountRevalidationCommand(
                    seed.ClearingHouse.Id,
                    TransactionNature.Credit,
                    command.EffectiveDate,
                    "PPD",
                    900,
                    SourceAccountNumber,
                    command.IdempotencyKey)));
        var sourceAccountConflict = await Assert.ThrowsAsync<ThirdPartyAdministrationConflictException>(() =>
            service.RequestAsync(
                seed.Customer.Id,
                seed.ThirdParty.Id,
                seed.Account.Id,
                new RequestThirdPartyAccountRevalidationCommand(
                    seed.ClearingHouse.Id,
                    TransactionNature.Debit,
                    command.EffectiveDate,
                    "PPD",
                    900,
                    "000000000000",
                    command.IdempotencyKey)));

        Assert.NotNull(first);
        Assert.False(first.ReusedExistingAttempt);
        Assert.True(second!.ReusedExistingAttempt);
        Assert.Null(wrongCustomer);
        Assert.Null(wrongParent);
        Assert.Null(wrongAccount);
        Assert.Equal("IDEMPOTENCY_KEY_CONFLICT", crossAccountConflict.Code);
        Assert.Equal("IDEMPOTENCY_KEY_CONFLICT", effectiveDateConflict.Code);
        Assert.Equal("IDEMPOTENCY_KEY_CONFLICT", serviceTypeConflict.Code);
        Assert.Equal("IDEMPOTENCY_KEY_CONFLICT", clearingHouseConflict.Code);
        Assert.Equal("IDEMPOTENCY_KEY_CONFLICT", natureConflict.Code);
        Assert.Equal("SOURCE_ACCOUNT_NOT_OWNED", sourceAccountConflict.Code);
        tx.Verify(x => x.RegisterCanonicalPrenotificationAsync(
            It.Is<CanonicalPrenotificationTransactionRequest>(r =>
                r.ThirdPartyAccountId == seed.Account.Id
                && r.ExpectedClearingHouseId == seed.ClearingHouse.Id
                && r.TransactionNature == TransactionNature.Debit
                && r.ValidationIdempotencyKey == "tpa5-revalidate-1"),
            It.IsAny<CancellationToken>()), Times.Once);
        var renderedLogs = string.Join('|', logger.Invocations
            .Where(invocation => invocation.Method.Name == nameof(ILogger.Log))
            .Select(invocation => invocation.Arguments.Count > 2 ? invocation.Arguments[2]?.ToString() : null));
        Assert.DoesNotContain(ThirdPartyDocument, renderedLogs, StringComparison.Ordinal);
        Assert.DoesNotContain(ThirdPartyAccountNumber, renderedLogs, StringComparison.Ordinal);
        Assert.DoesNotContain(SourceAccountNumber, renderedLogs, StringComparison.Ordinal);
        Assert.DoesNotContain(command.IdempotencyKey, renderedLogs, StringComparison.Ordinal);
        policy.VerifyAll();
    }

    [Fact]
    public async Task Revalidation_RejectsResolverContextMismatch_AndDoesNotTrustClientContext()
    {
        await using var context = CreateContext();
        var seed = await SeedCanonicalAccountAsync(context);
        context.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog
        {
            Id = 910,
            Term = "PAGOS",
            Description = "PPD",
            StandardEntryClassCode = "PPD",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var mismatchedRule = new ClearingHouseTransactionRule
        {
            Id = 911,
            ClearingHouseId = seed.ClearingHouse.Id + 1,
            TransactionNature = TransactionNature.Debit,
            TransactionType = TransactionTypeEnum.Debit,
            PrenotificationMode = PrenotificationRequirementMode.Mandatory,
            EffectiveFrom = new DateTime(2026, 1, 1),
            IsActive = true
        };
        var policy = new Mock<IClearingHouseValidationPolicyService>();
        policy.Setup(x => x.ResolveAsync(
                seed.ClearingHouse.Id,
                TransactionNature.Debit,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClearingHouseValidationPolicyResolution(
                mismatchedRule,
                new DateTime(2026, 10, 11),
                TransactionNature.Debit,
                seed.ClearingHouse.Id));
        var transactions = new Mock<IAchTransactionService>(MockBehavior.Strict);
        var service = new ThirdPartyAccountRevalidationService(
            context,
            transactions.Object,
            policy.Object,
            NullLogger<ThirdPartyAccountRevalidationService>.Instance);

        var conflict = await Assert.ThrowsAsync<ThirdPartyAdministrationConflictException>(() => service.RequestAsync(
            seed.Customer.Id,
            seed.ThirdParty.Id,
            seed.Account.Id,
            new RequestThirdPartyAccountRevalidationCommand(
                seed.ClearingHouse.Id,
                TransactionNature.Debit,
                new DateTime(2026, 10, 11),
                "PPD",
                910,
                SourceAccountNumber,
                "tpa9b32-policy-context")));

        Assert.Equal("POLICY_CONTEXT_MISMATCH", conflict.Code);
        transactions.VerifyNoOtherCalls();
    }

    [Fact]
    public void Api_UsesExistingReadManagePolicies_AndExposesNoManualApproveRejectCommand()
    {
        var controllerAuthorization = typeof(ThirdPartiesController).GetCustomAttributes<AuthorizeAttribute>().Single();
        Assert.Null(controllerAuthorization.Policy);

        var methods = typeof(ThirdPartiesController).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.DoesNotContain(methods, method => method.GetCustomAttributes<AllowAnonymousAttribute>().Any());

        var mutating = methods.Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any(a =>
            a.HttpMethods.Any(v => v is "POST" or "PUT" or "PATCH" or "DELETE"))).ToArray();
        Assert.NotEmpty(mutating);
        Assert.All(mutating, method =>
        {
            var authorization = method.GetCustomAttributes<AuthorizeAttribute>().Single();
            Assert.Equal("CanManageAch", authorization.Policy);
        });

        var reads = methods.Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any(a => a.HttpMethods.Contains("GET"))).ToArray();
        Assert.All(reads, method => Assert.Equal("CanReadAch", method.GetCustomAttributes<AuthorizeAttribute>().Single().Policy));

        var revalidation = methods.Single(m => m.Name == nameof(ThirdPartiesController.RequestRevalidation));
        Assert.Equal("CanManageAch", revalidation.GetCustomAttributes<AuthorizeAttribute>().Single().Policy);
        var revalidationRoute = revalidation.GetCustomAttributes<HttpMethodAttribute>().Single().Template ?? string.Empty;
        Assert.Contains("revalidations", revalidationRoute, StringComparison.Ordinal);

        var routeText = string.Join('|', methods.SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>())
            .Select(a => a.Template ?? string.Empty));
        Assert.DoesNotContain("approve", routeText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("reject", routeText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(methods, m => m.Name.Contains("Approve", StringComparison.OrdinalIgnoreCase)
                                         || m.Name.Contains("Reject", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ForeignAndMissingThirdPartyCommands_ReturnIndistinguishableNotFound()
    {
        await using var context = CreateContext();
        var seed = await SeedReferencesAsync(context);
        var service = CreateAdministrationService(context);
        var thirdParty = await service.CreateThirdPartyAsync(seed.Customer1.Id,
            new CreateThirdPartyCommand("NIT", ThirdPartyDocument, "Proveedor", "PJ"));
        var controller = CreateController(service, "safe-correlation");

        var foreign = await controller.UpdateThirdParty(
            seed.Customer2.Id,
            thirdParty.Value.ThirdPartyId,
            new UpdateThirdPartyCommand("Intruso"));
        var missing = await controller.UpdateThirdParty(
            seed.Customer1.Id,
            thirdParty.Value.ThirdPartyId + 999,
            new UpdateThirdPartyCommand("Intruso"));

        Assert.IsType<NotFoundResult>(foreign);
        Assert.IsType<NotFoundResult>(missing);
    }


    [Fact]
    public async Task RevalidationEndpoint_UsesGenericErrors_NotFoundAndServerOwnedAuditScope()
    {
        var sensitiveMessage = $"document={ThirdPartyDocument};account={ThirdPartyAccountNumber};evidence=secret";
        var admin = new Mock<IThirdPartyAdministrationService>(MockBehavior.Strict);
        var revalidation = new Mock<IThirdPartyAccountRevalidationService>(MockBehavior.Strict);
        var command = new RequestThirdPartyAccountRevalidationCommand(
            40,
            TransactionNature.Debit,
            new DateTime(2026, 10, 11),
            "PPD",
            900,
            SourceAccountNumber,
            "sensitive-idempotency-key");

        revalidation.Setup(x => x.RequestAsync(10, 50, 60, command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ThirdPartyAdministrationConflictException($"CONFLICT_{ThirdPartyAccountNumber}", sensitiveMessage));
        var conflictController = CreateController(admin.Object, sensitiveMessage, revalidation.Object);

        var conflict = Assert.IsType<ConflictObjectResult>(await conflictController.RequestRevalidation(10, 50, 60, command));
        var conflictJson = JsonSerializer.Serialize(conflict.Value);
        Assert.DoesNotContain(ThirdPartyDocument, conflictJson, StringComparison.Ordinal);
        Assert.DoesNotContain(ThirdPartyAccountNumber, conflictJson, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", conflictJson, StringComparison.Ordinal);
        Assert.Contains("CONFLICT", conflictJson, StringComparison.Ordinal);
        Assert.Equal("ThirdPartyAccount.Revalidation.Request", conflictController.HttpContext.Items[AchDbContext.AuditActionItemKey]);
        Assert.Equal("server-trace-tpa9b31", conflictController.HttpContext.Items[AchDbContext.AuditCorrelationItemKey]);
        Assert.NotEqual(sensitiveMessage, conflictController.HttpContext.Items[AchDbContext.AuditCorrelationItemKey]);

        var missing = new Mock<IThirdPartyAccountRevalidationService>(MockBehavior.Strict);
        missing.Setup(x => x.RequestAsync(10, 50, 60, command, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ThirdPartyAccountRevalidationResult?)null);
        var missingController = CreateController(admin.Object, sensitiveMessage, missing.Object);
        Assert.IsType<NotFoundResult>(await missingController.RequestRevalidation(10, 50, 60, command));
    }

    [Fact]
    public async Task CommandErrors_DoNotReflectSensitiveExceptionMessages_AndAuditCorrelationIsServerOwned()
    {
        var sensitiveMessage = $"document={ThirdPartyDocument};account={ThirdPartyAccountNumber}";

        var invalidRequestService = new Mock<IThirdPartyAdministrationService>(MockBehavior.Strict);
        invalidRequestService.Setup(x => x.CreateThirdPartyAsync(10, It.IsAny<CreateThirdPartyCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException(sensitiveMessage));
        var invalidRequestController = CreateController(invalidRequestService.Object, sensitiveMessage);

        var invalidRequest = Assert.IsType<BadRequestObjectResult>(await invalidRequestController.CreateThirdParty(
            10,
            new CreateThirdPartyCommand("NIT", ThirdPartyDocument, "Proveedor", "PJ")));
        var invalidRequestJson = JsonSerializer.Serialize(invalidRequest.Value);
        Assert.DoesNotContain(ThirdPartyDocument, invalidRequestJson, StringComparison.Ordinal);
        Assert.DoesNotContain(ThirdPartyAccountNumber, invalidRequestJson, StringComparison.Ordinal);
        Assert.Equal("server-trace-tpa9b31", invalidRequestController.HttpContext.Items[AchDbContext.AuditCorrelationItemKey]);
        Assert.NotEqual(sensitiveMessage, invalidRequestController.HttpContext.Items[AchDbContext.AuditCorrelationItemKey]);

        var notFoundService = new Mock<IThirdPartyAdministrationService>(MockBehavior.Strict);
        notFoundService.Setup(x => x.CreateThirdPartyAsync(10, It.IsAny<CreateThirdPartyCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ThirdPartyAdministrationConflictException($"{ThirdPartyDocument}_NOT_FOUND", sensitiveMessage));
        var notFoundController = CreateController(notFoundService.Object, sensitiveMessage);

        var notFound = Assert.IsType<NotFoundObjectResult>(await notFoundController.CreateThirdParty(
            10,
            new CreateThirdPartyCommand("NIT", ThirdPartyDocument, "Proveedor", "PJ")));
        var notFoundJson = JsonSerializer.Serialize(notFound.Value);
        Assert.DoesNotContain(ThirdPartyDocument, notFoundJson, StringComparison.Ordinal);
        Assert.DoesNotContain(ThirdPartyAccountNumber, notFoundJson, StringComparison.Ordinal);
        Assert.Contains("NOT_FOUND", notFoundJson, StringComparison.Ordinal);

        var conflictService = new Mock<IThirdPartyAdministrationService>(MockBehavior.Strict);
        conflictService.Setup(x => x.CreateThirdPartyAsync(10, It.IsAny<CreateThirdPartyCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ThirdPartyAdministrationConflictException($"CONFLICT_{ThirdPartyAccountNumber}", sensitiveMessage));
        var conflictController = CreateController(conflictService.Object, sensitiveMessage);

        var conflict = Assert.IsType<ConflictObjectResult>(await conflictController.CreateThirdParty(
            10,
            new CreateThirdPartyCommand("NIT", ThirdPartyDocument, "Proveedor", "PJ")));
        var conflictJson = JsonSerializer.Serialize(conflict.Value);
        Assert.DoesNotContain(ThirdPartyDocument, conflictJson, StringComparison.Ordinal);
        Assert.DoesNotContain(ThirdPartyAccountNumber, conflictJson, StringComparison.Ordinal);
        Assert.Contains("CONFLICT", conflictJson, StringComparison.Ordinal);
    }

    [Fact]
    public void AdministrativeDtos_DoNotExposeRawOrTechnicalSensitiveFields()
    {
        var forbiddenThirdPartyFields = new[]
        {
            "DocumentNumber",
            "DocumentNumberNormalized",
            "RowVersion",
            "ConcurrencyToken"
        };
        var forbiddenAccountFields = new[]
        {
            "AccountNumber",
            "AccountNumberNormalized",
            "DocumentNumber",
            "DocumentNumberNormalized",
            "IdempotencyKey",
            "EvidenceReference",
            "CorrelationReference",
            "SourceFileReference",
            "RowVersion",
            "ConcurrencyToken"
        };

        Assert.DoesNotContain(typeof(ThirdPartyAdministrationDto).GetProperties(),
            property => forbiddenThirdPartyFields.Contains(property.Name, StringComparer.Ordinal));
        Assert.DoesNotContain(typeof(ThirdPartyAccountReadDto).GetProperties(),
            property => forbiddenAccountFields.Contains(property.Name, StringComparer.Ordinal));
    }

    [Fact]
    public void MasterCreateContracts_HaveNoClearingHouseDimension()
    {
        Assert.DoesNotContain(typeof(CreateThirdPartyCommand).GetProperties(), p => p.Name.Contains("ClearingHouse", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(CreateThirdPartyAccountCommand).GetProperties(), p => p.Name.Contains("ClearingHouse", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(ThirdParty).GetProperties(), p => p.Name.Contains("ClearingHouse", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(typeof(ThirdPartyAccount).GetProperties(), p => p.Name.Contains("ClearingHouse", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(typeof(RequestThirdPartyAccountRevalidationCommand).GetProperties(), p => p.Name == nameof(RequestThirdPartyAccountRevalidationCommand.ClearingHouseId));
    }

    [Fact]
    public async Task ParentAdministrativeDeactivation_BlocksContextualEligibility_WithoutChangingValidationResult()
    {
        await using var context = CreateContext();
        var seed = await SeedCanonicalAccountAsync(context);
        seed.ThirdParty.Status = ThirdPartyAdministrativeStatus.Inactive;
        var validation = new ThirdPartyAccountValidation
        {
            Id = 920,
            ThirdPartyAccountId = seed.Account.Id,
            ClearingHouseId = seed.ClearingHouse.Id,
            TransactionNature = TransactionNature.Credit,
            ServiceType = "PPD",
            Status = ThirdPartyAccountValidationStatus.Accepted,
            EffectiveDate = new DateTime(2026, 10, 11)
        };
        context.ThirdPartyAccountValidations.Add(validation);
        await context.SaveChangesAsync();
        var policy = new Mock<IClearingHouseValidationPolicyService>(MockBehavior.Strict);
        var eligibility = new ThirdPartyAccountEligibilityService(context, policy.Object);

        var result = await eligibility.EvaluateAsync(new ThirdPartyAccountEligibilityRequest(
            seed.Account.Id,
            seed.ClearingHouse.Id,
            TransactionNature.Credit,
            new DateTime(2026, 10, 11),
            "PPD"));

        Assert.Equal(ThirdPartyAccountEligibilityDecision.Blocked, result.Decision);
        Assert.Equal(ThirdPartyAccountValidationStatus.Accepted, (await context.ThirdPartyAccountValidations.FindAsync(validation.Id))!.Status);
        policy.VerifyNoOtherCalls();
    }

    private static ThirdPartiesController CreateController(
        IThirdPartyAdministrationService administrationService,
        string callerCorrelation,
        IThirdPartyAccountRevalidationService? revalidationService = null)
    {
        var controller = new ThirdPartiesController(
            administrationService,
            Mock.Of<IThirdPartyAccountReadService>(),
            revalidationService ?? Mock.Of<IThirdPartyAccountRevalidationService>());
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "server-trace-tpa9b31"
        };
        httpContext.Request.Headers["X-Correlation-ID"] = callerCorrelation;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    private static ThirdPartyAdministrationService CreateAdministrationService(
        AchDbContext context,
        IThirdPartyOnboardingService? onboarding = null)
    {
        onboarding ??= new ThirdPartyOnboardingService(context, NullLogger<ThirdPartyOnboardingService>.Instance);
        var read = new ThirdPartyAccountReadService(context, Mock.Of<IThirdPartyAccountEligibilityService>());
        return new ThirdPartyAdministrationService(
            context,
            onboarding,
            read,
            NullLogger<ThirdPartyAdministrationService>.Instance);
    }

    private static AchDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<ReferenceSeed> SeedReferencesAsync(AchDbContext context)
    {
        var customer1 = new Customer
        {
            Id = 10,
            FirstName = "Cliente",
            LastName = "Uno",
            CompanyName = "CLIENTE UNO",
            PersonType = "PJ",
            DocumentType = "NIT",
            DocumentNumber = "800123456"
        };
        customer1.Accounts.Add(new CustomerAccount
        {
            Id = 11,
            CustomerId = customer1.Id,
            Customer = customer1,
            AccountNumber = SourceAccountNumber
        });
        var customer2 = new Customer
        {
            Id = 20,
            FirstName = "Cliente",
            LastName = "Dos",
            CompanyName = "CLIENTE DOS",
            PersonType = "PJ",
            DocumentType = "NIT",
            DocumentNumber = "800654321"
        };
        var institution = new FinancialInstitution
        {
            Id = 30,
            Name = "Banco Destino",
            RoutingNumber = "12345",
            TransitCode = "678",
            Status = FinancialInstitutionStatus.Active
        };
        context.DocumentTypes.AddRange(
            new DocumentTypeCatalog { Code = "NIT", Name = "NIT" },
            new DocumentTypeCatalog { Code = "CC", Name = "CC" });
        context.PersonTypes.AddRange(
            new PersonTypeCatalog { Code = "PJ", Name = "Jurídica" },
            new PersonTypeCatalog { Code = "PN", Name = "Natural" });
        context.Customers.AddRange(customer1, customer2);
        context.FinancialInstitutions.Add(institution);
        await context.SaveChangesAsync();
        return new ReferenceSeed(customer1, customer2, institution);
    }

    private static async Task<CanonicalSeed> SeedCanonicalAccountAsync(AchDbContext context)
    {
        var refs = await SeedReferencesAsync(context);
        var clearingHouse = new ClearingHouse
        {
            Id = 40,
            ClearingHouseId = 40,
            Name = "ACH Test",
            Code = "ACHCOL",
            OriginCode = "00000001"
        };
        var thirdParty = new ThirdParty
        {
            Id = 50,
            CustomerId = refs.Customer1.Id,
            Customer = refs.Customer1,
            PersonType = "PJ",
            DocumentType = "NIT",
            DocumentNumberNormalized = ThirdPartyDocument,
            DisplayName = "Proveedor",
            Status = ThirdPartyAdministrativeStatus.Active
        };
        var account = new ThirdPartyAccount
        {
            Id = 60,
            ThirdPartyId = thirdParty.Id,
            ThirdParty = thirdParty,
            FinancialInstitutionId = refs.Institution.Id,
            FinancialInstitution = refs.Institution,
            AccountType = AccountTypeEnum.Savings,
            AccountNumberNormalized = ThirdPartyAccountNumber,
            Status = ThirdPartyAdministrativeStatus.Active,
            OriginSource = ThirdPartyAccountOriginSource.Manual
        };
        thirdParty.Accounts.Add(account);
        context.ClearingHouses.Add(clearingHouse);
        context.ThirdParties.Add(thirdParty);
        context.ThirdPartyAccounts.Add(account);
        await context.SaveChangesAsync();
        return new CanonicalSeed(refs.Customer1, refs.Institution, clearingHouse, thirdParty, account);
    }

    private sealed record ReferenceSeed(Customer Customer1, Customer Customer2, FinancialInstitution Institution);
    private sealed record CanonicalSeed(Customer Customer, FinancialInstitution Institution, ClearingHouse ClearingHouse, ThirdParty ThirdParty, ThirdPartyAccount Account);
}
