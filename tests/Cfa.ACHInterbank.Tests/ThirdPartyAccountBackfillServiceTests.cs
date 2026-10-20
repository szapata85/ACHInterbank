using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class ThirdPartyAccountBackfillServiceTests
{
    [Fact]
    public async Task RunAsync_DryRun_ReportsWouldCreateWithoutWritingCanonicalRows()
    {
        await using var context = CreateContext();
        await SeedLegacyAsync(context, legacyId: 1, recipientId: "10101", accountNumber: "000111", transactionId: 101);
        var onboarding = new Mock<IThirdPartyOnboardingService>(MockBehavior.Strict);
        var service = new ThirdPartyAccountBackfillService(
            context,
            onboarding.Object,
            Mock.Of<ILogger<ThirdPartyAccountBackfillService>>());

        var result = await service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: true));

        Assert.Equal(1, result.Scanned);
        Assert.Equal(1, result.Created);
        Assert.Equal(1, result.ValidationHistoryCreated);
        Assert.Empty(await context.ThirdParties.ToListAsync());
        Assert.Empty(await context.ThirdPartyAccounts.ToListAsync());
        Assert.Empty(await context.ThirdPartyAccountValidations.ToListAsync());
        Assert.Empty(await context.PrenotificationValidationAttempts.ToListAsync());
        onboarding.Verify(x => x.EnsureAccountAsync(
            It.IsAny<ThirdPartyOnboardingRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public async Task RunAsync_RejectsBatchSizeOutsideSecurityBounds(int batchSize)
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: true, BatchSize: batchSize)));
    }

    [Fact]
    public async Task RunAsync_RejectsCheckpointBeyondLegacyHighWaterBeforeCanonicalWrites()
    {
        await using var context = CreateContext();
        await SeedLegacyAsync(context, legacyId: 1, recipientId: "11101", accountNumber: "000112", transactionId: 111);
        var onboarding = new Mock<IThirdPartyOnboardingService>(MockBehavior.Strict);
        var service = new ThirdPartyAccountBackfillService(
            context,
            onboarding.Object,
            Mock.Of<ILogger<ThirdPartyAccountBackfillService>>());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.RunAsync(new ThirdPartyAccountBackfillRequest(
                DryRun: false,
                AfterLegacyThirdPartyId: 2,
                BatchSize: 1)));

        Assert.Empty(await context.ThirdParties.ToListAsync());
        Assert.Empty(await context.ThirdPartyAccounts.ToListAsync());
        Assert.Empty(await context.ThirdPartyAccountValidations.ToListAsync());
        Assert.Empty(await context.PrenotificationValidationAttempts.ToListAsync());
        onboarding.Verify(x => x.EnsureAccountAsync(
            It.IsAny<ThirdPartyOnboardingRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_DryRun_DoesNotRepairExistingCanonicalHistory()
    {
        await using var context = CreateContext();
        await SeedLegacyAsync(context, legacyId: 1, recipientId: "12121", accountNumber: "000121", transactionId: 121);
        var service = CreateService(context);

        var applied = await service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false));
        Assert.Equal(1, applied.ValidationHistoryCreated);

        var validation = await context.ThirdPartyAccountValidations.SingleAsync();
        var attempt = await context.PrenotificationValidationAttempts.SingleAsync();
        validation.CurrentAttemptId = null;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var dryRun = await service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: true));
        var persistedValidation = await context.ThirdPartyAccountValidations.AsNoTracking().SingleAsync();

        Assert.Equal(1, dryRun.ValidationHistoryReused);
        Assert.Null(persistedValidation.CurrentAttemptId);
        Assert.Equal(1, await context.PrenotificationValidationAttempts.CountAsync());
        Assert.Equal(attempt.Id, (await context.PrenotificationValidationAttempts.AsNoTracking().SingleAsync()).Id);
    }

    [Fact]
    public async Task RunAsync_IsIdempotentAcrossRepeatedApplyRuns()
    {
        await using var context = CreateContext();
        await SeedLegacyAsync(context, legacyId: 1, recipientId: "20202", accountNumber: "000222", transactionId: 202);
        var service = CreateService(context);

        var first = await service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false));
        var second = await service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false));

        Assert.Equal(1, first.Created);
        Assert.Equal(1, first.ValidationHistoryCreated);
        Assert.Equal(1, second.Reused);
        Assert.Equal(1, second.ValidationHistoryReused);
        Assert.Equal(1, await context.ThirdParties.CountAsync());
        Assert.Equal(1, await context.ThirdPartyAccounts.CountAsync());
        Assert.Equal(1, await context.ThirdPartyAccountValidations.CountAsync());
        Assert.Equal(1, await context.PrenotificationValidationAttempts.CountAsync());
        var historicalValidation = await context.ThirdPartyAccountValidations.SingleAsync();
        var historicalAttempt = await context.PrenotificationValidationAttempts.SingleAsync();
        Assert.Null(historicalValidation.PolicyRuleId);
        Assert.Null(historicalValidation.PolicyEffectiveFrom);
        Assert.Null(historicalValidation.ExpiresAtUtc);
        Assert.Null(historicalAttempt.ResponseDeadlineAtUtc);
    }

    [Fact]
    public async Task RunAsync_ReusesExistingCanonicalAccountWithoutInventingMissingAccountType()
    {
        await using var context = CreateContext();
        var source = AddSourceCustomer(context, 10);
        var thirdParty = new ThirdParty
        {
            Id = 700,
            CustomerId = source.Id,
            DocumentType = "CC",
            DocumentNumberNormalized = "30303",
            DisplayName = "Tercero Existente",
            PersonType = "PN"
        };
        context.ThirdParties.Add(thirdParty);
        context.ThirdPartyAccounts.Add(new ThirdPartyAccount
        {
            Id = 701,
            ThirdPartyId = thirdParty.Id,
            ThirdParty = thirdParty,
            FinancialInstitutionId = 2,
            AccountType = AccountTypeEnum.Checking,
            AccountNumberNormalized = "000333"
        });
        context.CustomerThirdParties.Add(new CustomerThirdParty
        {
            Id = 1,
            CustomerId = source.Id,
            DestinationInstitutionId = 2,
            DestinationAccountNumber = "000333",
            RecipientIdNumber = "30303"
        });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false));

        Assert.Equal(1, result.Reused);
        Assert.Equal(0, result.Created);
        Assert.Equal(1, result.ValidationHistorySkippedIncomplete);
        Assert.Equal(1, await context.ThirdPartyAccounts.CountAsync());
    }

    [Fact]
    public async Task RunAsync_SkipsIncompleteLegacyDataInsteadOfGuessingIdentityOrAccountType()
    {
        await using var context = CreateContext();
        var source = AddSourceCustomer(context, 10);
        context.CustomerThirdParties.Add(new CustomerThirdParty
        {
            Id = 1,
            CustomerId = source.Id,
            DestinationInstitutionId = 2,
            DestinationAccountNumber = "000444",
            RecipientIdNumber = "40404"
        });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false));

        Assert.Equal(1, result.SkippedIncomplete);
        Assert.Contains(result.Issues, x => x.LegacyThirdPartyId == 1 && x.Category == "SKIPPED_INCOMPLETE");
        Assert.Empty(await context.ThirdParties.ToListAsync());
        Assert.Empty(await context.ThirdPartyAccounts.ToListAsync());
    }

    [Fact]
    public async Task RunAsync_ReportsAmbiguousRecipientIdentityWithoutCreatingMasterData()
    {
        await using var context = CreateContext();
        await SeedLegacyAsync(
            context,
            legacyId: 1,
            recipientId: "50505",
            accountNumber: "000555",
            transactionId: 505,
            addRecipientCustomer: false);
        context.Customers.AddRange(
            RecipientCustomer(20, "CC", "50505", "Receptor Uno"),
            RecipientCustomer(21, "CE", "50505", "Receptor Dos"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false));

        Assert.Equal(1, result.Ambiguous);
        Assert.Contains(result.Issues, x => x.Code == "RECIPIENT_IDENTITY_AMBIGUOUS");
        Assert.Empty(await context.ThirdParties.ToListAsync());
        Assert.Empty(await context.ThirdPartyAccounts.ToListAsync());
    }

    [Fact]
    public async Task RunAsync_DoesNotDuplicateCanonicalMasterWhenLegacyRowsNormalizeToSameIdentity()
    {
        await using var context = CreateContext();
        await SeedLegacyAsync(context, legacyId: 1, recipientId: "60606", accountNumber: "000666", transactionId: 601);
        await SeedLegacyAsync(
            context,
            legacyId: 2,
            recipientId: " 60606 ",
            accountNumber: " 000666 ",
            transactionId: 602,
            addSourceCustomer: false,
            addRecipientCustomer: false);
        var service = CreateService(context);

        var result = await service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false, BatchSize: 10));

        Assert.Equal(2, result.Scanned);
        Assert.Equal(1, result.Created);
        Assert.Equal(1, result.Reused);
        Assert.Equal(1, await context.ThirdParties.CountAsync());
        Assert.Equal(1, await context.ThirdPartyAccounts.CountAsync());
    }

    [Fact]
    public async Task RunAsync_DoesNotInventValidationHistoryWhenServiceContextIsMissing()
    {
        await using var context = CreateContext();
        await SeedLegacyAsync(
            context,
            legacyId: 1,
            recipientId: "70707",
            accountNumber: "000777",
            transactionId: 707,
            addServiceCatalog: false);
        var service = CreateService(context);

        var result = await service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false));

        Assert.Equal(1, result.Created);
        Assert.Equal(1, result.ValidationHistorySkippedIncomplete);
        Assert.Contains(result.Issues, x => x.Code == "SERVICE_TYPE_EVIDENCE_MISSING");
        Assert.Equal(1, await context.ThirdPartyAccounts.CountAsync());
        Assert.Empty(await context.ThirdPartyAccountValidations.ToListAsync());
        Assert.Empty(await context.PrenotificationValidationAttempts.ToListAsync());
    }

    [Fact]
    public async Task RunAsync_ResumesFromPersistedNeutralValidationContext()
    {
        await using var context = CreateContext();
        await SeedLegacyAsync(context, legacyId: 1, recipientId: "81818", accountNumber: "000818", transactionId: 818);
        var service = CreateService(context);

        var initial = await service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false));
        var account = await context.ThirdPartyAccounts.SingleAsync();
        var existingAttempt = await context.PrenotificationValidationAttempts.SingleAsync();
        var validation = await context.ThirdPartyAccountValidations.SingleAsync();

        context.PrenotificationValidationAttempts.Remove(existingAttempt);
        validation.CurrentAttemptId = null;
        validation.Status = ThirdPartyAccountValidationStatus.NotStarted;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var resumed = await service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false));
        var resumedValidation = await context.ThirdPartyAccountValidations.SingleAsync();
        var resumedAttempt = await context.PrenotificationValidationAttempts.SingleAsync();

        Assert.Equal(1, initial.ValidationHistoryCreated);
        Assert.Equal(1, resumed.Reused);
        Assert.Equal(1, resumed.ValidationHistoryCreated);
        Assert.Equal(account.Id, resumedValidation.ThirdPartyAccountId);
        Assert.Equal(ThirdPartyAccountValidationStatus.PendingResponse, resumedValidation.Status);
        Assert.Equal(resumedAttempt.Id, resumedValidation.CurrentAttemptId);
    }

    [Fact]
    public async Task RunAsync_ResumesSafelyFromLegacyIdCheckpoint()
    {
        await using var context = CreateContext();
        await SeedLegacyAsync(context, legacyId: 1, recipientId: "80801", accountNumber: "000801", transactionId: 801);
        await SeedLegacyAsync(context, legacyId: 2, recipientId: "80802", accountNumber: "000802", transactionId: 802, addSourceCustomer: false);
        var service = CreateService(context);

        var first = await service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false, BatchSize: 1));
        var second = await service.RunAsync(new ThirdPartyAccountBackfillRequest(
            DryRun: false,
            AfterLegacyThirdPartyId: first.LastProcessedLegacyThirdPartyId,
            BatchSize: 1));
        var replaySecondPage = await service.RunAsync(new ThirdPartyAccountBackfillRequest(
            DryRun: false,
            AfterLegacyThirdPartyId: first.LastProcessedLegacyThirdPartyId,
            BatchSize: 1));

        Assert.True(first.HasMore);
        Assert.Equal(1, first.LastProcessedLegacyThirdPartyId);
        Assert.False(second.HasMore);
        Assert.Equal(2, second.LastProcessedLegacyThirdPartyId);
        Assert.Equal(1, replaySecondPage.Reused);
        Assert.Equal(2, await context.ThirdParties.CountAsync());
        Assert.Equal(2, await context.ThirdPartyAccounts.CountAsync());
        Assert.Equal(2, await context.PrenotificationValidationAttempts.CountAsync());
    }

    [Fact]
    public async Task RunAsync_BatchLookahead_DoesNotProcessBeyondRequestedPage_AndResumesFromCheckpoint()
    {
        await using var context = CreateContext();
        await SeedLegacyAsync(context, legacyId: 1, recipientId: "83801", accountNumber: "000831", transactionId: 831);
        await SeedLegacyAsync(context, legacyId: 2, recipientId: "83802", accountNumber: "000832", transactionId: 832, addSourceCustomer: false);
        await SeedLegacyAsync(context, legacyId: 3, recipientId: "83803", accountNumber: "000833", transactionId: 833, addSourceCustomer: false);
        var service = CreateService(context);

        var first = await service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false, BatchSize: 2));

        Assert.Equal(2, first.Scanned);
        Assert.True(first.HasMore);
        Assert.Equal(2, first.LastProcessedLegacyThirdPartyId);
        Assert.Equal(2, await context.ThirdPartyAccounts.CountAsync());
        Assert.Equal(2, await context.PrenotificationValidationAttempts.CountAsync());

        var second = await service.RunAsync(new ThirdPartyAccountBackfillRequest(
            DryRun: false,
            AfterLegacyThirdPartyId: first.LastProcessedLegacyThirdPartyId,
            BatchSize: 2));

        Assert.Equal(1, second.Scanned);
        Assert.False(second.HasMore);
        Assert.Equal(3, second.LastProcessedLegacyThirdPartyId);
        Assert.Equal(3, await context.ThirdPartyAccounts.CountAsync());
        Assert.Equal(3, await context.PrenotificationValidationAttempts.CountAsync());
    }

    [Fact]
    public async Task RunAsync_IncompleteIdentity_DoesNotInvokeOnboarding()
    {
        await using var context = CreateContext();
        var source = AddSourceCustomer(context, 10);
        context.CustomerThirdParties.Add(new CustomerThirdParty
        {
            Id = 1,
            CustomerId = source.Id,
            DestinationInstitutionId = 2,
            DestinationAccountNumber = " ",
            RecipientIdNumber = "84848"
        });
        await context.SaveChangesAsync();

        var onboarding = new Mock<IThirdPartyOnboardingService>(MockBehavior.Strict);
        var service = new ThirdPartyAccountBackfillService(
            context,
            onboarding.Object,
            Mock.Of<ILogger<ThirdPartyAccountBackfillService>>());

        var result = await service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false));

        Assert.Equal(1, result.SkippedIncomplete);
        Assert.Equal(1, result.ValidationHistorySkippedIncomplete);
        onboarding.Verify(x => x.EnsureAccountAsync(
            It.IsAny<ThirdPartyOnboardingRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_ExistingValidationHistory_ChecksExistenceWithoutMaterializingAttempts()
    {
        await using var context = CreateContext();
        await SeedLegacyAsync(context, legacyId: 1, recipientId: "85858", accountNumber: "000858", transactionId: 858);
        var service = CreateService(context);
        var initial = await service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false));
        Assert.Equal(1, initial.ValidationHistoryCreated);

        var validation = await context.ThirdPartyAccountValidations.SingleAsync();
        var originalAttempt = await context.PrenotificationValidationAttempts.SingleAsync();
        validation.CurrentAttemptId = null;
        validation.Status = ThirdPartyAccountValidationStatus.NotStarted;
        context.PrenotificationValidationAttempts.Remove(originalAttempt);
        await context.SaveChangesAsync();

        for (var i = 1; i <= 25; i++)
        {
            context.PrenotificationValidationAttempts.Add(new PrenotificationValidationAttempt
            {
                ThirdPartyAccountValidationId = validation.Id,
                AttemptNumber = i,
                AchTransactionId = 5000 + i,
                IdempotencyKey = $"unrelated-{i}",
                Status = PrenotificationValidationAttemptStatus.Created,
                SentAtUtc = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc).AddMinutes(i),
                Resolution = PrenotificationValidationResolution.None,
                EvidenceReference = $"unrelated-evidence-{i}",
                CorrelationReference = $"unrelated-correlation-{i}",
                SourceCycleId = $"UX-{i}"
            });
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false));

        Assert.Equal(1, result.Reused);
        Assert.Equal(1, result.ValidationHistoryAmbiguous);
        Assert.Contains(result.Issues, x => x.Code == "EXISTING_VALIDATION_HISTORY_WITHOUT_TRANSACTION_LINK");
        Assert.Empty(context.ChangeTracker.Entries<PrenotificationValidationAttempt>());
    }

    [Fact]
    public async Task RunAsync_CancellationAfterMasterPersistence_SkipsHistoryAndClearsTracker()
    {
        await using var context = CreateContext();
        await SeedLegacyAsync(context, legacyId: 1, recipientId: "86868", accountNumber: "000868", transactionId: 868);

        using var cts = new CancellationTokenSource();
        var realOnboarding = new ThirdPartyOnboardingService(
            context,
            Mock.Of<ILogger<ThirdPartyOnboardingService>>());
        var onboarding = new Mock<IThirdPartyOnboardingService>();
        onboarding
            .Setup(x => x.EnsureAccountAsync(
                It.IsAny<ThirdPartyOnboardingRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns<ThirdPartyOnboardingRequest, CancellationToken>(async (request, token) =>
            {
                var result = await realOnboarding.EnsureAccountAsync(request, token);
                cts.Cancel();
                return result;
            });
        var service = new ThirdPartyAccountBackfillService(
            context,
            onboarding.Object,
            Mock.Of<ILogger<ThirdPartyAccountBackfillService>>());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false), cts.Token));

        Assert.Equal(1, await context.ThirdPartyAccounts.CountAsync());
        Assert.Empty(await context.ThirdPartyAccountValidations.ToListAsync());
        Assert.Empty(await context.PrenotificationValidationAttempts.ToListAsync());
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task RunAsync_UnexpectedRecordFailure_DoesNotAdvanceCheckpoint_AndReplayCanRecover()
    {
        await using var context = CreateContext();
        const string accountNumber = "000909";
        const string recipientDocument = "90909";
        await SeedLegacyAsync(context, legacyId: 1, recipientId: recipientDocument, accountNumber: accountNumber, transactionId: 909);

        var onboarding = new Mock<IThirdPartyOnboardingService>();
        onboarding.Setup(x => x.EnsureAccountAsync(
                It.IsAny<ThirdPartyOnboardingRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => context.ThirdParties.Add(new ThirdParty
            {
                CustomerId = 10,
                DocumentType = "CC",
                DocumentNumberNormalized = recipientDocument,
                DisplayName = "ghost tracked recipient",
                PersonType = "PN"
            }))
            .ThrowsAsync(new InvalidOperationException($"sensitive {accountNumber} {recipientDocument}"));
        var logger = new CaptureLogger<ThirdPartyAccountBackfillService>();
        var failingService = new ThirdPartyAccountBackfillService(context, onboarding.Object, logger);

        var failed = await failingService.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false));

        Assert.Equal(1, failed.Failed);
        Assert.Null(failed.LastProcessedLegacyThirdPartyId);
        Assert.True(failed.HasMore);
        Assert.Empty(await context.ThirdPartyAccounts.ToListAsync());
        Assert.Empty(await context.ThirdParties.ToListAsync());
        Assert.Empty(context.ChangeTracker.Entries());
        var log = string.Join("\n", logger.Messages);
        Assert.DoesNotContain(accountNumber, log, StringComparison.Ordinal);
        Assert.DoesNotContain(recipientDocument, log, StringComparison.Ordinal);
        Assert.Contains(nameof(InvalidOperationException), log, StringComparison.Ordinal);

        var replay = await CreateService(context).RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false));

        Assert.Equal(1, replay.Created);
        Assert.Equal(1, replay.ValidationHistoryCreated);
        Assert.Equal(1, replay.LastProcessedLegacyThirdPartyId);
        Assert.False(replay.HasMore);
        Assert.Equal(1, await context.ThirdPartyAccounts.CountAsync());
        Assert.Equal(1, await context.PrenotificationValidationAttempts.CountAsync());
    }

    [Fact]
    public async Task RunAsync_ReplayWithMismatchedCanonicalContext_DoesNotRepairOrRebindHistory()
    {
        await using var context = CreateContext();
        await SeedLegacyAsync(context, legacyId: 1, recipientId: "90919", accountNumber: "000919", transactionId: 919);
        var service = CreateService(context);

        var first = await service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false));
        Assert.Equal(1, first.ValidationHistoryCreated);

        var validation = await context.ThirdPartyAccountValidations.SingleAsync();
        validation.ServiceType = "CCD";
        validation.CurrentAttemptId = null;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var replay = await service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false));
        var persistedValidation = await context.ThirdPartyAccountValidations.AsNoTracking().SingleAsync();

        Assert.Equal(1, replay.Reused);
        Assert.Equal(1, replay.ValidationHistoryAmbiguous);
        Assert.Contains(replay.Issues, x => x.Code == "CANONICAL_LINKED_HISTORY_CONTEXT_CONFLICT");
        Assert.Equal("CCD", persistedValidation.ServiceType);
        Assert.Null(persistedValidation.CurrentAttemptId);
        Assert.Equal(1, await context.PrenotificationValidationAttempts.CountAsync());
    }

    [Fact]
    public async Task RunAsync_ReplayWithConflictingLegacyOutcome_DoesNotOverwriteCanonicalHistory()
    {
        await using var context = CreateContext();
        await SeedLegacyAsync(context, legacyId: 1, recipientId: "91991", accountNumber: "000991", transactionId: 991);
        var service = CreateService(context);

        var first = await service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false));
        Assert.Equal(1, first.ValidationHistoryCreated);

        var legacy = await context.CustomerThirdParties
            .Include(x => x.PrenotificationTransaction)
            .SingleAsync(x => x.Id == 1);
        var transaction = legacy.PrenotificationTransaction!;
        transaction.State = AchTransferStateEnum.Certified;
        transaction.StateChangedAtUtc = new DateTime(2026, 10, 10, 14, 0, 0, DateTimeKind.Utc);
        legacy.ApplyAutomaticNachaResult(
            CustomerThirdPartyStatusEnum.Active,
            transaction.Id,
            transaction.AchCycleId,
            transaction.StateChangedAtUtc,
            "later legacy outcome",
            "later legacy evidence");
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var replay = await service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false));
        var canonicalValidation = await context.ThirdPartyAccountValidations.SingleAsync();
        var canonicalAttempt = await context.PrenotificationValidationAttempts.SingleAsync();

        Assert.Equal(1, replay.Reused);
        Assert.Equal(1, replay.ValidationHistoryAmbiguous);
        Assert.Contains(replay.Issues, x => x.Code == "CANONICAL_LINKED_HISTORY_STATE_CONFLICT");
        Assert.Equal(ThirdPartyAccountValidationStatus.PendingResponse, canonicalValidation.Status);
        Assert.Equal(PrenotificationValidationAttemptStatus.AwaitingResponse, canonicalAttempt.Status);
        Assert.Equal(PrenotificationValidationResolution.None, canonicalAttempt.Resolution);
        Assert.Equal(canonicalAttempt.Id, canonicalValidation.CurrentAttemptId);
    }

    [Fact]
    public async Task RunAsync_PreCanceledBatch_DoesNotCreateCanonicalData()
    {
        await using var context = CreateContext();
        await SeedLegacyAsync(context, legacyId: 1, recipientId: "92992", accountNumber: "000992", transactionId: 992);
        var service = CreateService(context);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false), cts.Token));

        Assert.Empty(await context.ThirdParties.ToListAsync());
        Assert.Empty(await context.ThirdPartyAccounts.ToListAsync());
        Assert.Empty(await context.ThirdPartyAccountValidations.ToListAsync());
        Assert.Empty(await context.PrenotificationValidationAttempts.ToListAsync());
    }

    [Fact]
    public async Task Tpa8Closure_BackfilledCanonicalHistoryFeedsCutoverCompatibilityRead()
    {
        await using var context = CreateContext();
        await SeedLegacyAsync(context, legacyId: 1, recipientId: "91919", accountNumber: "000919", transactionId: 919);
        await EnsureReadContextAsync(context);

        var legacy = await context.CustomerThirdParties
            .Include(x => x.PrenotificationTransaction)
            .SingleAsync(x => x.Id == 1);
        var transaction = legacy.PrenotificationTransaction!;
        transaction.State = AchTransferStateEnum.Certified;
        transaction.StateChangedAtUtc = new DateTime(2026, 10, 10, 13, 0, 0, DateTimeKind.Utc);
        legacy.ApplyAutomaticNachaResult(
            CustomerThirdPartyStatusEnum.Active,
            transaction.Id,
            transaction.AchCycleId,
            transaction.StateChangedAtUtc,
            "automatic",
            "automatic-evidence");
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var backfill = CreateService(context);
        var backfillResult = await backfill.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false));

        Assert.Equal(1, backfillResult.Created);
        Assert.Equal(1, backfillResult.ValidationHistoryCreated);
        var validation = await context.ThirdPartyAccountValidations.SingleAsync();
        Assert.Equal(ThirdPartyAccountValidationStatus.Accepted, validation.Status);
        Assert.Null(validation.PolicyRuleId);
        Assert.Null(validation.PolicyEffectiveFrom);

        context.ChangeTracker.Clear();
        var read = new CustomerThirdPartyAppService(context);
        var active = await read.GetAsync(new CustomerThirdPartyQuery
        {
            SourceAccountNumber = "SRC-001",
            Status = CustomerThirdPartyStatusEnum.Active
        });

        var item = Assert.Single(active.Items);
        Assert.Equal(validation.ThirdPartyAccountId, item.Id);
        Assert.Equal(CustomerThirdPartyStatusEnum.Active, item.Status);
        Assert.Contains("canónica", item.StatusSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Tpa8Closure_IncompleteBackfillRemainsConservativeLegacyFallback()
    {
        await using var context = CreateContext();
        var source = AddSourceCustomer(context, 10);
        var legacy = new CustomerThirdParty
        {
            Id = 1,
            CustomerId = source.Id,
            DestinationInstitutionId = 2,
            DestinationAccountNumber = "000929",
            RecipientIdNumber = "92929"
        };
        legacy.ApplyAutomaticNachaResult(
            CustomerThirdPartyStatusEnum.Active,
            null,
            "LEGACY-C1",
            new DateTime(2026, 10, 10, 13, 0, 0, DateTimeKind.Utc),
            "legacy accepted",
            "legacy-evidence");
        context.CustomerThirdParties.Add(legacy);
        await context.SaveChangesAsync();
        await EnsureReadContextAsync(context);

        var backfill = CreateService(context);
        var backfillResult = await backfill.RunAsync(new ThirdPartyAccountBackfillRequest(DryRun: false));

        Assert.Equal(1, backfillResult.SkippedIncomplete);
        Assert.Empty(await context.ThirdPartyAccounts.ToListAsync());

        context.ChangeTracker.Clear();
        var read = new CustomerThirdPartyAppService(context);
        var all = await read.GetAsync(new CustomerThirdPartyQuery { SourceAccountNumber = "SRC-001" });
        var active = await read.GetAsync(new CustomerThirdPartyQuery
        {
            SourceAccountNumber = "SRC-001",
            Status = CustomerThirdPartyStatusEnum.Active
        });

        var fallback = Assert.Single(all.Items);
        Assert.Equal(CustomerThirdPartyStatusEnum.Pending, fallback.Status);
        Assert.Contains("Compatibilidad legacy", fallback.StatusSource, StringComparison.Ordinal);
        Assert.Empty(active.Items);
    }

    private static async Task EnsureReadContextAsync(AchDbContext context)
    {
        var source = await context.Customers
            .Include(x => x.Accounts)
            .SingleAsync(x => x.Id == 10);
        if (!source.Accounts.Any(x => x.AccountNumber == "SRC-001"))
        {
            var sourceAccount = new CustomerAccount
            {
                Id = 9001,
                CustomerId = source.Id,
                Customer = source,
                AccountNumber = "SRC-001"
            };
            source.Accounts.Add(sourceAccount);
            context.CustomerAccounts.Add(sourceAccount);
        }

        if (!await context.FinancialInstitutions.AnyAsync(x => x.Id == 2))
        {
            context.FinancialInstitutions.Add(new FinancialInstitution
            {
                Id = 2,
                Name = "Banco Destino",
                RoutingNumber = "00000002",
                TransitCode = "002"
            });
        }

        if (!await context.ClearingHouses.AnyAsync(x => x.Id == 1))
        {
            context.ClearingHouses.Add(new ClearingHouse
            {
                Id = 1,
                ClearingHouseId = 1,
                Code = "ACHCOL",
                Name = "ACH Colombia",
                OriginCode = "00000001"
            });
        }

        await context.SaveChangesAsync();
    }

    private static ThirdPartyAccountBackfillService CreateService(AchDbContext context)
    {
        var onboarding = new ThirdPartyOnboardingService(
            context,
            Mock.Of<ILogger<ThirdPartyOnboardingService>>());
        return new ThirdPartyAccountBackfillService(
            context,
            onboarding,
            Mock.Of<ILogger<ThirdPartyAccountBackfillService>>());
    }

    private static AchDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase($"tpa8a-{Guid.NewGuid():N}")
            .Options;
        return new AchDbContext(options);
    }

    private static async Task SeedLegacyAsync(
        AchDbContext context,
        int legacyId,
        string recipientId,
        string accountNumber,
        int transactionId,
        bool addSourceCustomer = true,
        bool addRecipientCustomer = true,
        bool addServiceCatalog = true)
    {
        if (addSourceCustomer && !await context.Customers.AnyAsync(x => x.Id == 10))
        {
            AddSourceCustomer(context, 10);
        }
        if (addRecipientCustomer && !await context.Customers.AnyAsync(x => x.DocumentNumber == recipientId.Trim()))
        {
            context.Customers.Add(RecipientCustomer(100 + legacyId, "CC", recipientId.Trim(), $"Receptor {legacyId}"));
        }
        if (addServiceCatalog && !await context.CompanyEntryDescriptionCatalogs.AnyAsync(x => x.Id == 1))
        {
            context.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog
            {
                Id = 1,
                Term = "PAGOS",
                Description = "Pagos",
                StandardEntryClassCode = "PPD",
                IsActive = true
            });
        }

        var cycleId = $"C{transactionId}";
        var cycle = new AchCycle
        {
            Id = cycleId,
            CycleName = cycleId,
            ProcessingDate = new DateTime(2026, 10, 10),
            ClearingHouseId = 1,
            StartTime = TimeSpan.Zero,
            EndTime = TimeSpan.FromHours(1),
            CutoffTime = TimeSpan.FromHours(1),
            OutputReleaseTime = TimeSpan.FromHours(1)
        };
        context.AchCycles.Add(cycle);

        var transaction = new AchTransaction
        {
            Id = transactionId,
            Type = TransactionTypeEnum.Credit,
            IsPrenotification = true,
            TransactionCode = "33",
            TransactionExternalId = $"TX-{transactionId}",
            Reference = $"REF-{transactionId}",
            TraceNumber = transactionId.ToString("000000000000000"),
            EffectiveEntryDate = new DateTime(2026, 10, 10),
            State = AchTransferStateEnum.Pending,
            StateChangedAtUtc = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc),
            RecipientIdNumber = recipientId.Trim(),
            SourceAccountNumber = "SRC-001",
            DestinationAccountNumber = accountNumber.Trim(),
            SourceInstitutionId = 1,
            DestinationInstitutionId = 2,
            AchCycleId = cycleId,
            AchCycle = cycle,
            CompanyEntryDescriptionId = addServiceCatalog ? 1 : 999,
            CompanyName = "Originador",
            CompanyIdentification = "900000001",
            OriginatingDFI = "00000001",
            ReceivingDFI = "00000002",
            ServiceClassCode = "200"
        };
        context.AchTransactions.Add(transaction);

        context.CustomerThirdParties.Add(new CustomerThirdParty
        {
            Id = legacyId,
            CustomerId = 10,
            DestinationInstitutionId = 2,
            DestinationAccountNumber = accountNumber,
            RecipientIdNumber = recipientId,
            PrenotificationTransactionId = transactionId,
            PrenotificationTransaction = transaction
        });
        await context.SaveChangesAsync();
    }

    private static Customer AddSourceCustomer(AchDbContext context, int id)
    {
        var customer = new Customer
        {
            Id = id,
            FirstName = "Empresa",
            LastName = "Origen",
            CompanyName = "Empresa Origen",
            PersonType = "PJ",
            DocumentType = "NIT",
            DocumentNumber = "900000001"
        };
        context.Customers.Add(customer);
        return customer;
    }

    private static Customer RecipientCustomer(int id, string documentType, string documentNumber, string name)
        => new()
        {
            Id = id,
            FirstName = name,
            LastName = "Prueba",
            PersonType = "PN",
            DocumentType = documentType,
            DocumentNumber = documentNumber
        };

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
