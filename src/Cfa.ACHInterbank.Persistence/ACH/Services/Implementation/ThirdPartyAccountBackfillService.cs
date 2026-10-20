using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class ThirdPartyAccountBackfillService : IThirdPartyAccountBackfillService
{
    private const int MaxBatchSize = 1000;
    private readonly AchDbContext _context;
    private readonly IThirdPartyOnboardingService _onboardingService;
    private readonly ILogger<ThirdPartyAccountBackfillService> _logger;

    public ThirdPartyAccountBackfillService(
        AchDbContext context,
        IThirdPartyOnboardingService onboardingService,
        ILogger<ThirdPartyAccountBackfillService> logger)
    {
        _context = context;
        _onboardingService = onboardingService;
        _logger = logger;
    }

    public async Task<ThirdPartyAccountBackfillSummary> RunAsync(
        ThirdPartyAccountBackfillRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.AfterLegacyThirdPartyId is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.AfterLegacyThirdPartyId));
        }
        if (request.BatchSize <= 0 || request.BatchSize > MaxBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(request.BatchSize), $"BatchSize debe estar entre 1 y {MaxBatchSize}.");
        }
        ct.ThrowIfCancellationRequested();

        if (request.AfterLegacyThirdPartyId is > 0)
        {
            var highestLegacyThirdPartyId = await _context.CustomerThirdParties
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(ct);

            if (highestLegacyThirdPartyId <= 0
                || request.AfterLegacyThirdPartyId.Value > highestLegacyThirdPartyId)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request.AfterLegacyThirdPartyId),
                    "Checkpoint fuera del rango disponible.");
            }
        }

        var query = _context.CustomerThirdParties
            .AsNoTracking()
            .Include(x => x.PrenotificationTransaction)
                .ThenInclude(x => x!.AchCycle)
            .Where(x => !request.AfterLegacyThirdPartyId.HasValue || x.Id > request.AfterLegacyThirdPartyId.Value)
            .OrderBy(x => x.Id);

        var rows = await query.Take(request.BatchSize + 1).ToListAsync(ct);
        var hasMore = rows.Count > request.BatchSize;
        if (hasMore)
        {
            rows.RemoveAt(rows.Count - 1);
        }
        var checkpoint = request.AfterLegacyThirdPartyId;
        var checkpointBlocked = false;

        var created = 0;
        var reused = 0;
        var skippedIncomplete = 0;
        var ambiguous = 0;
        var failed = 0;
        var historyCreated = 0;
        var historyReused = 0;
        var historySkippedIncomplete = 0;
        var historyAmbiguous = 0;
        var historyFailed = 0;
        var issues = new List<ThirdPartyAccountBackfillIssue>();

        foreach (var legacy in rows)
        {
            ct.ThrowIfCancellationRequested();

            MasterBackfillResult master;
            try
            {
                master = await BackfillMasterAsync(legacy, request.DryRun, ct);
            }
            catch (OperationCanceledException)
            {
                _context.ChangeTracker.Clear();
                throw;
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();
                failed++;
                checkpointBlocked = true;
                issues.Add(new ThirdPartyAccountBackfillIssue(legacy.Id, "FAILED", "MASTER_UNEXPECTED_ERROR"));
                _logger.LogError(
                    "TPA8A master backfill failed without PII. LegacyThirdPartyId={LegacyThirdPartyId}, ErrorType={ErrorType}.",
                    legacy.Id,
                    ex.GetType().Name);
                continue;
            }

            switch (master.Outcome)
            {
                case MasterOutcome.Created:
                    created++;
                    break;
                case MasterOutcome.Reused:
                    reused++;
                    break;
                case MasterOutcome.SkippedIncomplete:
                    skippedIncomplete++;
                    issues.Add(new ThirdPartyAccountBackfillIssue(legacy.Id, "SKIPPED_INCOMPLETE", master.Code));
                    historySkippedIncomplete++;
                    if (!checkpointBlocked)
                    {
                        checkpoint = legacy.Id;
                    }
                    continue;
                case MasterOutcome.Ambiguous:
                    ambiguous++;
                    issues.Add(new ThirdPartyAccountBackfillIssue(legacy.Id, "AMBIGUOUS", master.Code));
                    historyAmbiguous++;
                    if (!checkpointBlocked)
                    {
                        checkpoint = legacy.Id;
                    }
                    continue;
                default:
                    throw new InvalidOperationException("Resultado de backfill maestro no soportado.");
            }

            if (ct.IsCancellationRequested)
            {
                _context.ChangeTracker.Clear();
                ct.ThrowIfCancellationRequested();
            }

            HistoryOutcomeResult history;
            try
            {
                history = await BackfillValidationHistoryAsync(legacy, master, request.DryRun, ct);
            }
            catch (OperationCanceledException)
            {
                _context.ChangeTracker.Clear();
                throw;
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();
                history = new HistoryOutcomeResult(HistoryOutcome.Failed, "VALIDATION_HISTORY_UNEXPECTED_ERROR");
                _logger.LogError(
                    "TPA8A validation-history backfill failed without PII. LegacyThirdPartyId={LegacyThirdPartyId}, ErrorType={ErrorType}.",
                    legacy.Id,
                    ex.GetType().Name);
            }

            switch (history.Outcome)
            {
                case HistoryOutcome.Created:
                    historyCreated++;
                    break;
                case HistoryOutcome.Reused:
                    historyReused++;
                    break;
                case HistoryOutcome.SkippedIncomplete:
                    historySkippedIncomplete++;
                    issues.Add(new ThirdPartyAccountBackfillIssue(legacy.Id, "VALIDATION_HISTORY_SKIPPED_INCOMPLETE", history.Code));
                    break;
                case HistoryOutcome.Ambiguous:
                    historyAmbiguous++;
                    issues.Add(new ThirdPartyAccountBackfillIssue(legacy.Id, "VALIDATION_HISTORY_AMBIGUOUS", history.Code));
                    break;
                case HistoryOutcome.Failed:
                    historyFailed++;
                    checkpointBlocked = true;
                    issues.Add(new ThirdPartyAccountBackfillIssue(legacy.Id, "VALIDATION_HISTORY_FAILED", history.Code));
                    break;
            }

            if (!checkpointBlocked)
            {
                checkpoint = legacy.Id;
            }
        }

        var summary = new ThirdPartyAccountBackfillSummary(
            request.DryRun,
            request.AfterLegacyThirdPartyId,
            checkpoint,
            hasMore || checkpointBlocked,
            rows.Count,
            created,
            reused,
            skippedIncomplete,
            ambiguous,
            failed,
            historyCreated,
            historyReused,
            historySkippedIncomplete,
            historyAmbiguous,
            historyFailed,
            issues);

        _logger.LogInformation(
            "TPA8A backfill batch completed without PII. DryRun={DryRun}, Scanned={Scanned}, Created={Created}, Reused={Reused}, SkippedIncomplete={SkippedIncomplete}, Ambiguous={Ambiguous}, Failed={Failed}, ValidationHistoryCreated={ValidationHistoryCreated}, ValidationHistoryReused={ValidationHistoryReused}, ValidationHistorySkippedIncomplete={ValidationHistorySkippedIncomplete}, ValidationHistoryAmbiguous={ValidationHistoryAmbiguous}, ValidationHistoryFailed={ValidationHistoryFailed}, HasMore={HasMore}, LastProcessedLegacyThirdPartyId={LastProcessedLegacyThirdPartyId}, CheckpointBlocked={CheckpointBlocked}.",
            summary.DryRun,
            summary.Scanned,
            summary.Created,
            summary.Reused,
            summary.SkippedIncomplete,
            summary.Ambiguous,
            summary.Failed,
            summary.ValidationHistoryCreated,
            summary.ValidationHistoryReused,
            summary.ValidationHistorySkippedIncomplete,
            summary.ValidationHistoryAmbiguous,
            summary.ValidationHistoryFailed,
            summary.HasMore,
            summary.LastProcessedLegacyThirdPartyId,
            checkpointBlocked);

        return summary;
    }

    private async Task<MasterBackfillResult> BackfillMasterAsync(
        CustomerThirdParty legacy,
        bool dryRun,
        CancellationToken ct)
    {
        if (legacy.CustomerId <= 0
            || legacy.DestinationInstitutionId <= 0
            || string.IsNullOrWhiteSpace(legacy.DestinationAccountNumber)
            || string.IsNullOrWhiteSpace(legacy.RecipientIdNumber))
        {
            return MasterBackfillResult.Skipped("LEGACY_MASTER_IDENTITY_INCOMPLETE");
        }

        string accountNumber;
        string recipientDocumentNumber;
        try
        {
            accountNumber = ThirdPartyAccount.NormalizeAccountNumber(legacy.DestinationAccountNumber);
            recipientDocumentNumber = ThirdParty.NormalizeDocumentNumber(legacy.RecipientIdNumber);
        }
        catch (ArgumentException)
        {
            return MasterBackfillResult.Skipped("LEGACY_MASTER_IDENTITY_INCOMPLETE");
        }

        var transactionEvidence = ResolveTransactionEvidence(legacy, accountNumber, recipientDocumentNumber);
        var linkedAttempts = await LoadLinkedAttemptsAsync(legacy.PrenotificationTransactionId, includeAccountIdentity: true, ct: ct);
        if (linkedAttempts.Count > 1)
        {
            return MasterBackfillResult.Ambiguous("MULTIPLE_CANONICAL_ATTEMPTS_FOR_LEGACY_TRANSACTION");
        }
        if (linkedAttempts.Count == 1)
        {
            var linkedAccount = linkedAttempts[0].ThirdPartyAccountValidation.ThirdPartyAccount;
            if (!IdentityMatches(legacy, accountNumber, recipientDocumentNumber, linkedAccount))
            {
                return MasterBackfillResult.Ambiguous("CANONICAL_ATTEMPT_ACCOUNT_MISMATCH");
            }
            if (transactionEvidence.IsConflicting)
            {
                return MasterBackfillResult.Ambiguous(transactionEvidence.Code);
            }
            if (transactionEvidence.AccountType.HasValue
                && linkedAccount.AccountType != transactionEvidence.AccountType.Value)
            {
                return MasterBackfillResult.Ambiguous("CANONICAL_ATTEMPT_ACCOUNT_TYPE_CONFLICT");
            }

            return MasterBackfillResult.Reused(linkedAccount.Id, linkedAccount.ThirdPartyId, transactionEvidence);
        }

        if (transactionEvidence.IsConflicting)
        {
            return MasterBackfillResult.Ambiguous(transactionEvidence.Code);
        }

        var baseCandidateQuery = _context.ThirdPartyAccounts
            .AsNoTracking()
            .Where(x => x.ThirdParty.CustomerId == legacy.CustomerId
                        && x.FinancialInstitutionId == legacy.DestinationInstitutionId
                        && x.AccountNumberNormalized == accountNumber
                        && x.ThirdParty.DocumentNumberNormalized == recipientDocumentNumber);
        if (transactionEvidence.AccountType.HasValue)
        {
            baseCandidateQuery = baseCandidateQuery.Where(x => x.AccountType == transactionEvidence.AccountType.Value);
        }

        var baseCandidates = await baseCandidateQuery
            .Select(x => new CanonicalAccountCandidate(x.Id, x.ThirdPartyId))
            .Take(2)
            .ToListAsync(ct);

        if (baseCandidates.Count == 1)
        {
            return MasterBackfillResult.Reused(baseCandidates[0].Id, baseCandidates[0].ThirdPartyId, transactionEvidence);
        }
        if (baseCandidates.Count > 1)
        {
            return MasterBackfillResult.Ambiguous(
                transactionEvidence.AccountType.HasValue
                    ? "CANONICAL_ACCOUNT_IDENTITY_AMBIGUOUS"
                    : "CANONICAL_ACCOUNT_TYPE_AMBIGUOUS");
        }

        if (!transactionEvidence.AccountType.HasValue)
        {
            return MasterBackfillResult.Skipped("ACCOUNT_TYPE_EVIDENCE_MISSING");
        }

        var recipientCustomers = await _context.Customers
            .AsNoTracking()
            .Where(x => x.DocumentNumber.Trim().ToUpper() == recipientDocumentNumber)
            .Select(x => new RecipientIdentityEvidence(
                x.DocumentType,
                x.PersonType,
                x.CompanyName,
                x.FirstName,
                x.MiddleName,
                x.LastName,
                x.SecondLastName))
            .Take(2)
            .ToListAsync(ct);

        if (recipientCustomers.Count > 1)
        {
            return MasterBackfillResult.Ambiguous("RECIPIENT_IDENTITY_AMBIGUOUS");
        }

        ThirdPartyIdentity identity;
        if (recipientCustomers.Count == 1)
        {
            var recipient = recipientCustomers[0];
            if (string.IsNullOrWhiteSpace(recipient.DocumentType)
                || string.IsNullOrWhiteSpace(recipient.PersonType))
            {
                return MasterBackfillResult.Skipped("RECIPIENT_IDENTITY_INCOMPLETE");
            }

            var displayName = BuildDisplayName(recipient);
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return MasterBackfillResult.Skipped("RECIPIENT_DISPLAY_NAME_MISSING");
            }

            var documentType = ThirdParty.NormalizeCatalogCode(recipient.DocumentType, nameof(recipient.DocumentType));
            var personType = ThirdParty.NormalizeCatalogCode(recipient.PersonType, nameof(recipient.PersonType));
            var exactThirdParties = await _context.ThirdParties
                .AsNoTracking()
                .Where(x => x.CustomerId == legacy.CustomerId
                            && x.DocumentNumberNormalized == recipientDocumentNumber
                            && x.DocumentType == documentType)
                .Select(x => new ThirdPartyIdentity(
                    x.DocumentType,
                    x.DocumentNumberNormalized,
                    x.PersonType,
                    x.DisplayName,
                    x.Id))
                .Take(2)
                .ToListAsync(ct);
            if (exactThirdParties.Count > 1)
            {
                return MasterBackfillResult.Ambiguous("CANONICAL_THIRD_PARTY_IDENTITY_AMBIGUOUS");
            }

            identity = exactThirdParties.Count == 1
                ? exactThirdParties[0]
                : new ThirdPartyIdentity(documentType, recipientDocumentNumber, personType, displayName, null);
        }
        else
        {
            var thirdPartyCandidates = await _context.ThirdParties
                .AsNoTracking()
                .Where(x => x.CustomerId == legacy.CustomerId
                            && x.DocumentNumberNormalized == recipientDocumentNumber)
                .Select(x => new ThirdPartyIdentity(
                    x.DocumentType,
                    x.DocumentNumberNormalized,
                    x.PersonType,
                    x.DisplayName,
                    x.Id))
                .Take(2)
                .ToListAsync(ct);

            if (thirdPartyCandidates.Count == 1)
            {
                identity = thirdPartyCandidates[0];
            }
            else if (thirdPartyCandidates.Count > 1)
            {
                return MasterBackfillResult.Ambiguous("CANONICAL_THIRD_PARTY_DOCUMENT_TYPE_AMBIGUOUS");
            }
            else
            {
                return MasterBackfillResult.Skipped("RECIPIENT_IDENTITY_EVIDENCE_MISSING");
            }
        }

        var onboardingRequest = new ThirdPartyOnboardingRequest(
            legacy.CustomerId,
            identity.DocumentType,
            identity.DocumentNumber,
            identity.DisplayName,
            identity.PersonType,
            legacy.DestinationInstitutionId,
            transactionEvidence.AccountType.Value,
            accountNumber,
            ThirdPartyAccountOriginSource.Backfill);

        if (dryRun)
        {
            return MasterBackfillResult.Created(null, identity.ExistingThirdPartyId, transactionEvidence, onboardingRequest);
        }

        var ensured = await _onboardingService.EnsureAccountAsync(onboardingRequest, ct);
        return ensured.ThirdPartyCreated || ensured.ThirdPartyAccountCreated
            ? MasterBackfillResult.Created(ensured.ThirdPartyAccountId, ensured.ThirdPartyId, transactionEvidence, onboardingRequest)
            : MasterBackfillResult.Reused(ensured.ThirdPartyAccountId, ensured.ThirdPartyId, transactionEvidence);
    }

    private async Task<HistoryOutcomeResult> BackfillValidationHistoryAsync(
        CustomerThirdParty legacy,
        MasterBackfillResult master,
        bool dryRun,
        CancellationToken ct)
    {
        if (!legacy.PrenotificationTransactionId.HasValue || legacy.PrenotificationTransaction is null)
        {
            return HistoryOutcomeResult.Skipped("PRENOTIFICATION_TRANSACTION_LINK_MISSING");
        }

        var linkedAttempts = await LoadLinkedAttemptsAsync(legacy.PrenotificationTransactionId, includeAccountIdentity: false, ct: ct);
        if (linkedAttempts.Count > 1)
        {
            return HistoryOutcomeResult.Ambiguous("MULTIPLE_CANONICAL_ATTEMPTS_FOR_LEGACY_TRANSACTION");
        }
        if (linkedAttempts.Count == 1)
        {
            var linkedAttempt = linkedAttempts[0];
            var linkedValidation = linkedAttempt.ThirdPartyAccountValidation;
            var linkedTransaction = legacy.PrenotificationTransaction;
            if (master.ThirdPartyAccountId.HasValue
                && linkedValidation.ThirdPartyAccountId != master.ThirdPartyAccountId.Value)
            {
                return HistoryOutcomeResult.Ambiguous("CANONICAL_ATTEMPT_ACCOUNT_MISMATCH");
            }

            if (!master.TransactionEvidence.TransactionNature.HasValue
                || linkedTransaction.AchCycle is null
                || linkedTransaction.AchCycle.ClearingHouseId <= 0
                || linkedTransaction.EffectiveEntryDate == default)
            {
                return HistoryOutcomeResult.Skipped("PRENOTIFICATION_CONTEXT_EVIDENCE_MISSING");
            }

            var linkedServiceType = await _context.CompanyEntryDescriptionCatalogs
                .AsNoTracking()
                .Where(x => x.Id == linkedTransaction.CompanyEntryDescriptionId)
                .Select(x => x.StandardEntryClassCode)
                .FirstOrDefaultAsync(ct);
            if (string.IsNullOrWhiteSpace(linkedServiceType))
            {
                return HistoryOutcomeResult.Skipped("SERVICE_TYPE_EVIDENCE_MISSING");
            }
            linkedServiceType = ThirdPartyAccountValidation.NormalizeServiceType(linkedServiceType);

            if (linkedValidation.ClearingHouseId != linkedTransaction.AchCycle.ClearingHouseId
                || linkedValidation.TransactionNature != master.TransactionEvidence.TransactionNature.Value
                || !string.Equals(linkedValidation.ServiceType, linkedServiceType, StringComparison.Ordinal)
                || linkedValidation.ValidationType != ThirdPartyAccountValidationType.Prenotification
                || linkedValidation.EffectiveDate.Date != linkedTransaction.EffectiveEntryDate.Date)
            {
                return HistoryOutcomeResult.Ambiguous("CANONICAL_LINKED_HISTORY_CONTEXT_CONFLICT");
            }

            if (!TryResolveHistoricalStatus(
                    legacy.Status,
                    linkedTransaction.State,
                    out var expectedValidationStatus,
                    out var expectedAttemptStatus,
                    out var expectedResolution))
            {
                return HistoryOutcomeResult.Ambiguous("LEGACY_STATUS_TRANSACTION_STATE_CONFLICT");
            }

            if (linkedValidation.Status != expectedValidationStatus
                || linkedAttempt.Status != expectedAttemptStatus
                || linkedAttempt.Resolution != expectedResolution)
            {
                return HistoryOutcomeResult.Ambiguous("CANONICAL_LINKED_HISTORY_STATE_CONFLICT");
            }

            if (linkedValidation.CurrentAttemptId.HasValue
                && linkedValidation.CurrentAttemptId.Value != linkedAttempt.Id)
            {
                return HistoryOutcomeResult.Ambiguous("CANONICAL_CURRENT_ATTEMPT_CONFLICT");
            }

            if (!dryRun && !linkedValidation.CurrentAttemptId.HasValue)
            {
                var trackedValidation = await _context.ThirdPartyAccountValidations
                    .FirstAsync(x => x.Id == linkedAttempt.ThirdPartyAccountValidationId, ct);
                if (!trackedValidation.CurrentAttemptId.HasValue)
                {
                    trackedValidation.CurrentAttemptId = linkedAttempt.Id;
                    await _context.SaveChangesAsync(ct);
                }
                else if (trackedValidation.CurrentAttemptId.Value != linkedAttempt.Id)
                {
                    return HistoryOutcomeResult.Ambiguous("CANONICAL_CURRENT_ATTEMPT_CONFLICT");
                }
            }

            return HistoryOutcomeResult.Reused();
        }

        var transaction = legacy.PrenotificationTransaction;
        var accountNumber = ThirdPartyAccount.NormalizeAccountNumber(legacy.DestinationAccountNumber);
        var recipientDocumentNumber = ThirdParty.NormalizeDocumentNumber(legacy.RecipientIdNumber);
        var evidence = ResolveTransactionEvidence(legacy, accountNumber, recipientDocumentNumber);
        if (evidence.IsConflicting)
        {
            return HistoryOutcomeResult.Ambiguous(evidence.Code);
        }
        if (!evidence.AccountType.HasValue || !evidence.TransactionNature.HasValue)
        {
            return HistoryOutcomeResult.Skipped("PRENOTIFICATION_SEMANTIC_EVIDENCE_MISSING");
        }
        if (transaction.AchCycle is null
            || transaction.AchCycle.ClearingHouseId <= 0
            || string.IsNullOrWhiteSpace(transaction.AchCycleId)
            || transaction.EffectiveEntryDate == default
            || transaction.CreatedAt == default)
        {
            return HistoryOutcomeResult.Skipped("PRENOTIFICATION_CONTEXT_EVIDENCE_MISSING");
        }
        if (!string.IsNullOrWhiteSpace(legacy.ValidationCycleId)
            && !string.Equals(legacy.ValidationCycleId.Trim(), transaction.AchCycleId.Trim(), StringComparison.Ordinal))
        {
            return HistoryOutcomeResult.Ambiguous("LEGACY_VALIDATION_CYCLE_CONFLICT");
        }

        var serviceType = await _context.CompanyEntryDescriptionCatalogs
            .AsNoTracking()
            .Where(x => x.Id == transaction.CompanyEntryDescriptionId)
            .Select(x => x.StandardEntryClassCode)
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(serviceType))
        {
            return HistoryOutcomeResult.Skipped("SERVICE_TYPE_EVIDENCE_MISSING");
        }
        serviceType = ThirdPartyAccountValidation.NormalizeServiceType(serviceType);

        if (!TryResolveHistoricalStatus(
                legacy.Status,
                transaction.State,
                out var validationStatus,
                out var attemptStatus,
                out var resolution))
        {
            return HistoryOutcomeResult.Ambiguous("LEGACY_STATUS_TRANSACTION_STATE_CONFLICT");
        }

        DateTimeOffset? resolvedAtUtc = null;
        if (resolution != PrenotificationValidationResolution.None)
        {
            if (legacy.ValidationReceivedAt.HasValue)
            {
                resolvedAtUtc = new DateTimeOffset(DateTime.SpecifyKind(
                    legacy.ValidationReceivedAt.Value.ToUniversalTime(),
                    DateTimeKind.Utc));
            }
            else if (transaction.StateChangedAtUtc != default)
            {
                resolvedAtUtc = new DateTimeOffset(DateTime.SpecifyKind(
                    transaction.StateChangedAtUtc.ToUniversalTime(),
                    DateTimeKind.Utc));
            }
            else
            {
                return HistoryOutcomeResult.Skipped("VALIDATION_RESOLUTION_TIMESTAMP_MISSING");
            }
        }

        if (dryRun && !master.ThirdPartyAccountId.HasValue)
        {
            return HistoryOutcomeResult.Created();
        }
        if (!master.ThirdPartyAccountId.HasValue)
        {
            return HistoryOutcomeResult.Skipped("CANONICAL_ACCOUNT_NOT_PERSISTED");
        }

        var accountId = master.ThirdPartyAccountId.Value;
        var validationState = await _context.ThirdPartyAccountValidations
            .Where(x => x.ThirdPartyAccountId == accountId
                        && x.ClearingHouseId == transaction.AchCycle.ClearingHouseId
                        && x.TransactionNature == evidence.TransactionNature.Value
                        && x.ServiceType == serviceType
                        && x.ValidationType == ThirdPartyAccountValidationType.Prenotification)
            .Select(x => new
            {
                Validation = x,
                HasAttempts = x.Attempts.Any()
            })
            .FirstOrDefaultAsync(ct);
        var validation = validationState?.Validation;

        if (validationState?.HasAttempts == true)
        {
            return HistoryOutcomeResult.Ambiguous("EXISTING_VALIDATION_HISTORY_WITHOUT_TRANSACTION_LINK");
        }
        if (validation is not null && validation.Status != ThirdPartyAccountValidationStatus.NotStarted)
        {
            return HistoryOutcomeResult.Ambiguous("EXISTING_VALIDATION_STATE_WITHOUT_ATTEMPT");
        }

        if (dryRun)
        {
            return HistoryOutcomeResult.Created();
        }

        if (validation is null)
        {
            validation = new ThirdPartyAccountValidation
            {
                ThirdPartyAccountId = accountId,
                ClearingHouseId = transaction.AchCycle.ClearingHouseId,
                TransactionNature = evidence.TransactionNature.Value,
                ServiceType = serviceType,
                ValidationType = ThirdPartyAccountValidationType.Prenotification,
                // Persist the context first in a resumable neutral state. The historical
                // result is saved atomically with its attempt below, so an interruption
                // cannot leave a fabricated final status without evidence history.
                Status = ThirdPartyAccountValidationStatus.NotStarted,
                PolicyRuleId = null,
                PolicyEffectiveFrom = null,
                EffectiveDate = transaction.EffectiveEntryDate.Date,
                ExpiresAtUtc = null,
                LastMonetaryUseAtUtc = null
            };
            _context.ThirdPartyAccountValidations.Add(validation);
            var addedBeforeSave = SnapshotAddedEntities();
            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (RelationalDatabaseExceptionClassifier.IsUniqueViolation(ex))
            {
                DetachEntriesAddedDuringFailedSave(addedBeforeSave);
                if (_context.Entry(validation).State != EntityState.Detached)
                {
                    _context.Entry(validation).State = EntityState.Detached;
                }

                var concurrentValidationState = await _context.ThirdPartyAccountValidations
                    .Where(x => x.ThirdPartyAccountId == accountId
                                && x.ClearingHouseId == transaction.AchCycle.ClearingHouseId
                                && x.TransactionNature == evidence.TransactionNature.Value
                                && x.ServiceType == serviceType
                                && x.ValidationType == ThirdPartyAccountValidationType.Prenotification)
                    .Select(x => new
                    {
                        Validation = x,
                        HasAttempts = x.Attempts.Any()
                    })
                    .FirstOrDefaultAsync(ct);
                validation = concurrentValidationState?.Validation;
                if (validation is null)
                {
                    throw;
                }
                if (concurrentValidationState!.HasAttempts || validation.Status != ThirdPartyAccountValidationStatus.NotStarted)
                {
                    return HistoryOutcomeResult.Ambiguous("CONCURRENT_VALIDATION_HISTORY_CONFLICT");
                }
                validation.Status = validationStatus;
                validation.EffectiveDate = transaction.EffectiveEntryDate.Date;
            }
        }
        else
        {
            validation.EffectiveDate = transaction.EffectiveEntryDate.Date;
        }

        var idempotencyKey = $"tpa8a-backfill:ctp:{legacy.Id}:tx:{transaction.Id}";
        var existingAttempt = await _context.PrenotificationValidationAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey || x.AchTransactionId == transaction.Id, ct);
        if (existingAttempt is not null)
        {
            if (existingAttempt.ThirdPartyAccountValidationId != validation.Id)
            {
                return HistoryOutcomeResult.Ambiguous("CONCURRENT_ATTEMPT_CONTEXT_CONFLICT");
            }
            if (existingAttempt.Status != attemptStatus || existingAttempt.Resolution != resolution)
            {
                return HistoryOutcomeResult.Ambiguous("CONCURRENT_ATTEMPT_STATE_CONFLICT");
            }
            if (validation.Status != ThirdPartyAccountValidationStatus.NotStarted
                && validation.Status != validationStatus)
            {
                return HistoryOutcomeResult.Ambiguous("CONCURRENT_VALIDATION_STATE_CONFLICT");
            }

            if (validation.CurrentAttemptId.HasValue && validation.CurrentAttemptId.Value != existingAttempt.Id)
            {
                return HistoryOutcomeResult.Ambiguous("CANONICAL_CURRENT_ATTEMPT_CONFLICT");
            }

            validation.Status = validationStatus;
            validation.EffectiveDate = transaction.EffectiveEntryDate.Date;
            validation.CurrentAttemptId ??= existingAttempt.Id;
            await _context.SaveChangesAsync(ct);
            return HistoryOutcomeResult.Reused();
        }

        validation.Status = validationStatus;
        validation.EffectiveDate = transaction.EffectiveEntryDate.Date;

        var attempt = new PrenotificationValidationAttempt
        {
            ThirdPartyAccountValidationId = validation.Id,
            AttemptNumber = 1,
            AchTransactionId = transaction.Id,
            IdempotencyKey = idempotencyKey,
            Status = attemptStatus,
            SentAtUtc = transaction.CreatedAt.ToUniversalTime(),
            ResponseDeadlineAtUtc = null,
            ResolvedAtUtc = resolvedAtUtc,
            Resolution = resolution,
            ReturnCode = string.IsNullOrWhiteSpace(transaction.ReturnReasonCode) ? null : transaction.ReturnReasonCode.Trim(),
            ReturnDetail = string.IsNullOrWhiteSpace(legacy.ValidationMessage) ? null : legacy.ValidationMessage.Trim(),
            EvidenceReference = $"legacy-customer-third-party:{legacy.Id}:transaction:{transaction.Id}",
            CorrelationReference = $"legacy-customer-third-party:{legacy.Id}",
            SourceCycleId = transaction.AchCycleId.Trim(),
            SourceFileReference = null
        };

        _context.PrenotificationValidationAttempts.Add(attempt);
        var attemptAddedBeforeSave = SnapshotAddedEntities();
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (RelationalDatabaseExceptionClassifier.IsUniqueViolation(ex))
        {
            DetachEntriesAddedDuringFailedSave(attemptAddedBeforeSave);
            if (_context.Entry(attempt).State != EntityState.Detached)
            {
                _context.Entry(attempt).State = EntityState.Detached;
            }

            existingAttempt = await _context.PrenotificationValidationAttempts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey || x.AchTransactionId == transaction.Id, ct);
            if (existingAttempt is null || existingAttempt.ThirdPartyAccountValidationId != validation.Id)
            {
                await _context.Entry(validation).ReloadAsync(ct);
                return HistoryOutcomeResult.Ambiguous("CONCURRENT_ATTEMPT_CONTEXT_CONFLICT");
            }

            await _context.Entry(validation).ReloadAsync(ct);
            if (existingAttempt.Status != attemptStatus || existingAttempt.Resolution != resolution)
            {
                return HistoryOutcomeResult.Ambiguous("CONCURRENT_ATTEMPT_STATE_CONFLICT");
            }
            if (validation.Status != ThirdPartyAccountValidationStatus.NotStarted
                && validation.Status != validationStatus)
            {
                return HistoryOutcomeResult.Ambiguous("CONCURRENT_VALIDATION_STATE_CONFLICT");
            }
            if (validation.CurrentAttemptId.HasValue && validation.CurrentAttemptId.Value != existingAttempt.Id)
            {
                return HistoryOutcomeResult.Ambiguous("CANONICAL_CURRENT_ATTEMPT_CONFLICT");
            }

            validation.Status = validationStatus;
            validation.EffectiveDate = transaction.EffectiveEntryDate.Date;
            validation.CurrentAttemptId ??= existingAttempt.Id;
            await _context.SaveChangesAsync(ct);
            return HistoryOutcomeResult.Reused();
        }

        validation.CurrentAttemptId = attempt.Id;
        await _context.SaveChangesAsync(ct);
        return HistoryOutcomeResult.Created();
    }

    private async Task<List<PrenotificationValidationAttempt>> LoadLinkedAttemptsAsync(
        int? transactionId,
        bool includeAccountIdentity,
        CancellationToken ct)
    {
        if (!transactionId.HasValue || transactionId.Value <= 0)
        {
            return [];
        }

        IQueryable<PrenotificationValidationAttempt> query = _context.PrenotificationValidationAttempts
            .AsNoTracking()
            .Where(x => x.AchTransactionId == transactionId.Value);

        if (includeAccountIdentity)
        {
            query = query.Include(x => x.ThirdPartyAccountValidation)
                .ThenInclude(x => x.ThirdPartyAccount)
                    .ThenInclude(x => x.ThirdParty);
        }
        else
        {
            query = query.Include(x => x.ThirdPartyAccountValidation);
        }

        return await query
            .Take(2)
            .ToListAsync(ct);
    }

    private static TransactionEvidence ResolveTransactionEvidence(
        CustomerThirdParty legacy,
        string normalizedAccount,
        string normalizedRecipientDocument)
    {
        var transaction = legacy.PrenotificationTransaction;
        if (transaction is null)
        {
            return TransactionEvidence.Missing("PRENOTIFICATION_TRANSACTION_LINK_MISSING");
        }
        if (!transaction.IsPrenotification)
        {
            return TransactionEvidence.Conflict("LINKED_TRANSACTION_IS_NOT_PRENOTIFICATION");
        }
        string transactionAccount;
        string transactionRecipientDocument;
        try
        {
            transactionAccount = ThirdPartyAccount.NormalizeAccountNumber(transaction.DestinationAccountNumber);
            transactionRecipientDocument = ThirdParty.NormalizeDocumentNumber(transaction.RecipientIdNumber ?? string.Empty);
        }
        catch (ArgumentException)
        {
            return TransactionEvidence.Conflict("LINKED_TRANSACTION_IDENTITY_INVALID");
        }

        if (transaction.DestinationInstitutionId != legacy.DestinationInstitutionId
            || !string.Equals(transactionAccount, normalizedAccount, StringComparison.Ordinal)
            || !string.Equals(transactionRecipientDocument, normalizedRecipientDocument, StringComparison.Ordinal))
        {
            return TransactionEvidence.Conflict("LINKED_TRANSACTION_IDENTITY_CONFLICT");
        }

        var code = (transaction.TransactionCode ?? string.Empty).Trim();
        var semantic = NachaTransactionCodeTaxonomy.GetSupportedOrdinaryRules()
            .Where(x => x.IsPrenotification && string.Equals(x.TransactionCode, code, StringComparison.Ordinal))
            .ToArray();
        if (semantic.Length != 1)
        {
            return TransactionEvidence.Missing("ACCOUNT_TYPE_EVIDENCE_MISSING");
        }

        var transactionType = NachaTransactionCodeTaxonomy.ResolvePrenotificationDirection(code);
        var nature = transactionType.HasValue
            ? TransactionPrerequisitePolicyService.ResolveNature(transactionType.Value)
            : null;
        if (!nature.HasValue)
        {
            return TransactionEvidence.Missing("TRANSACTION_NATURE_EVIDENCE_MISSING");
        }

        return new TransactionEvidence(semantic[0].AccountType, nature.Value, false, "OK");
    }

    private static bool IdentityMatches(
        CustomerThirdParty legacy,
        string normalizedAccount,
        string normalizedRecipientDocument,
        ThirdPartyAccount account)
        => account.ThirdParty.CustomerId == legacy.CustomerId
           && account.FinancialInstitutionId == legacy.DestinationInstitutionId
           && string.Equals(account.AccountNumberNormalized, normalizedAccount, StringComparison.Ordinal)
           && string.Equals(account.ThirdParty.DocumentNumberNormalized, normalizedRecipientDocument, StringComparison.Ordinal);

    private static string? BuildDisplayName(RecipientIdentityEvidence recipient)
    {
        if (!string.IsNullOrWhiteSpace(recipient.CompanyName))
        {
            return recipient.CompanyName.Trim();
        }

        var parts = new[] { recipient.FirstName, recipient.MiddleName, recipient.LastName, recipient.SecondLastName }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToArray();
        return parts.Length == 0 ? null : string.Join(" ", parts);
    }

    private static bool TryResolveHistoricalStatus(
        CustomerThirdPartyStatusEnum legacyStatus,
        AchTransferStateEnum transactionState,
        out ThirdPartyAccountValidationStatus validationStatus,
        out PrenotificationValidationAttemptStatus attemptStatus,
        out PrenotificationValidationResolution resolution)
    {
        switch (legacyStatus, transactionState)
        {
            case (CustomerThirdPartyStatusEnum.Pending, AchTransferStateEnum.Pending):
                validationStatus = ThirdPartyAccountValidationStatus.PendingResponse;
                attemptStatus = PrenotificationValidationAttemptStatus.AwaitingResponse;
                resolution = PrenotificationValidationResolution.None;
                return true;
            case (CustomerThirdPartyStatusEnum.Active, AchTransferStateEnum.Certified):
                validationStatus = ThirdPartyAccountValidationStatus.Accepted;
                attemptStatus = PrenotificationValidationAttemptStatus.Accepted;
                resolution = PrenotificationValidationResolution.Accepted;
                return true;
            case (CustomerThirdPartyStatusEnum.Active, AchTransferStateEnum.AppliedTacitly):
                validationStatus = ThirdPartyAccountValidationStatus.TacitlyAccepted;
                attemptStatus = PrenotificationValidationAttemptStatus.TacitlyAccepted;
                resolution = PrenotificationValidationResolution.TacitlyAccepted;
                return true;
            case (CustomerThirdPartyStatusEnum.Rejected, AchTransferStateEnum.ReturnedByOperator):
            case (CustomerThirdPartyStatusEnum.Rejected, AchTransferStateEnum.ReturnedByEpr):
                validationStatus = ThirdPartyAccountValidationStatus.Rejected;
                attemptStatus = PrenotificationValidationAttemptStatus.Rejected;
                resolution = PrenotificationValidationResolution.Rejected;
                return true;
            default:
                validationStatus = ThirdPartyAccountValidationStatus.NotStarted;
                attemptStatus = PrenotificationValidationAttemptStatus.Created;
                resolution = PrenotificationValidationResolution.None;
                return false;
        }
    }

    private HashSet<object> SnapshotAddedEntities()
        => _context.ChangeTracker.Entries()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity)
            .ToHashSet(ReferenceEqualityComparer.Instance);

    private void DetachEntriesAddedDuringFailedSave(IReadOnlySet<object> entitiesTrackedBeforeSave)
    {
        foreach (var entry in _context.ChangeTracker.Entries().Where(entry => entry.State == EntityState.Added).ToList())
        {
            if (!entitiesTrackedBeforeSave.Contains(entry.Entity))
            {
                entry.State = EntityState.Detached;
            }
        }
    }

    private enum MasterOutcome
    {
        Created,
        Reused,
        SkippedIncomplete,
        Ambiguous
    }

    private enum HistoryOutcome
    {
        Created,
        Reused,
        SkippedIncomplete,
        Ambiguous,
        Failed
    }

    private sealed record TransactionEvidence(
        AccountTypeEnum? AccountType,
        TransactionNature? TransactionNature,
        bool IsConflicting,
        string Code)
    {
        public static TransactionEvidence Missing(string code) => new(null, null, false, code);
        public static TransactionEvidence Conflict(string code) => new(null, null, true, code);
    }

    private sealed record CanonicalAccountCandidate(long Id, long ThirdPartyId);

    private sealed record RecipientIdentityEvidence(
        string? DocumentType,
        string? PersonType,
        string? CompanyName,
        string? FirstName,
        string? MiddleName,
        string? LastName,
        string? SecondLastName);

    private sealed record ThirdPartyIdentity(
        string DocumentType,
        string DocumentNumber,
        string PersonType,
        string DisplayName,
        long? ExistingThirdPartyId);

    private sealed record MasterBackfillResult(
        MasterOutcome Outcome,
        string Code,
        long? ThirdPartyAccountId,
        long? ThirdPartyId,
        TransactionEvidence TransactionEvidence,
        ThirdPartyOnboardingRequest? OnboardingRequest)
    {
        public static MasterBackfillResult Created(
            long? accountId,
            long? thirdPartyId,
            TransactionEvidence transactionEvidence,
            ThirdPartyOnboardingRequest? request)
            => new(MasterOutcome.Created, "CREATED", accountId, thirdPartyId, transactionEvidence, request);

        public static MasterBackfillResult Reused(
            long accountId,
            long thirdPartyId,
            TransactionEvidence transactionEvidence)
            => new(MasterOutcome.Reused, "REUSED", accountId, thirdPartyId, transactionEvidence, null);

        public static MasterBackfillResult Skipped(string code)
            => new(MasterOutcome.SkippedIncomplete, code, null, null, TransactionEvidence.Missing(code), null);

        public static MasterBackfillResult Ambiguous(string code)
            => new(MasterOutcome.Ambiguous, code, null, null, TransactionEvidence.Conflict(code), null);
    }

    private sealed record HistoryOutcomeResult(HistoryOutcome Outcome, string Code)
    {
        public static HistoryOutcomeResult Created() => new(HistoryOutcome.Created, "CREATED");
        public static HistoryOutcomeResult Reused() => new(HistoryOutcome.Reused, "REUSED");
        public static HistoryOutcomeResult Skipped(string code) => new(HistoryOutcome.SkippedIncomplete, code);
        public static HistoryOutcomeResult Ambiguous(string code) => new(HistoryOutcome.Ambiguous, code);
    }
}
