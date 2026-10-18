using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class ThirdPartyAccountValidationService : IThirdPartyAccountValidationService
{
    private readonly AchDbContext _context;
    private readonly IClearingHouseValidationPolicyService _policyService;
    private readonly ILogger<ThirdPartyAccountValidationService> _logger;

    public ThirdPartyAccountValidationService(
        AchDbContext context,
        IClearingHouseValidationPolicyService policyService,
        ILogger<ThirdPartyAccountValidationService> logger)
    {
        _context = context;
        _policyService = policyService;
        _logger = logger;
    }

    public async Task<PrenotificationValidationStartResult> RegisterPrenotificationAttemptAsync(
        PrenotificationValidationStartRequest request,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ValidateStartRequest(request);
        var idempotencyKey = request.IdempotencyKey.Trim();
        var serviceType = ThirdPartyAccountValidation.NormalizeServiceType(request.ServiceType);
        var existingAttempt = await FindExistingReplayAttemptAsync(request, serviceType, idempotencyKey, ct);
        if (existingAttempt is not null)
        {
            return Map(existingAttempt, true);
        }

        var policy = await _policyService.ResolveAsync(
            request.ClearingHouseId,
            request.TransactionNature,
            request.EffectiveDate,
            ct);

        DateTimeOffset? responseDeadline = null;
        var resolvedPolicyContextCompatible = policy.Rule is not null
                                              && IsResolvedPolicyContextCompatible(policy.Rule, request);
        var technicalReview = policy.Rule is null
                              || !resolvedPolicyContextCompatible
                              || policy.Rule.PrenotificationMode == PrenotificationRequirementMode.NotApplicable;
        if (!technicalReview && policy.Rule is not null)
        {
            responseDeadline = await _policyService.ResolvePrenotificationResponseDeadlineAsync(
                policy.Rule,
                request.SourceCycleId,
                ct);
            technicalReview = policy.Rule.PrenotificationResponseDeadlineMode != PrenotificationResponseDeadlineMode.NotApplicable
                              && !responseDeadline.HasValue;
        }

        var validation = await FindValidationAsync(request, serviceType, ct);
        if (validation is null)
        {
            var candidate = new ThirdPartyAccountValidation
            {
                ThirdPartyAccountId = request.ThirdPartyAccountId,
                ClearingHouseId = request.ClearingHouseId,
                TransactionNature = request.TransactionNature,
                ServiceType = serviceType,
                ValidationType = ThirdPartyAccountValidationType.Prenotification,
                Status = ThirdPartyAccountValidationStatus.NotStarted,
                PolicyRuleId = policy.Rule?.Id,
                PolicyEffectiveFrom = policy.Rule?.EffectiveFrom,
                EffectiveDate = request.EffectiveDate.Date
            };

            _context.ThirdPartyAccountValidations.Add(candidate);
            var addedBeforeSave = SnapshotAddedEntities();
            try
            {
                ct.ThrowIfCancellationRequested();
                await _context.SaveChangesAsync(ct);
                validation = candidate;
            }
            catch (DbUpdateException ex) when (RelationalDatabaseExceptionClassifier.IsUniqueViolation(ex))
            {
                DetachEntriesAddedDuringFailedSave(addedBeforeSave);
                _context.Entry(candidate).State = EntityState.Detached;
                validation = await FindValidationAsync(request, serviceType, ct);
                if (validation is null)
                {
                    throw;
                }

                _logger.LogInformation(
                    "Third-party account validation recovered a concurrent context conflict. ThirdPartyAccountId={ThirdPartyAccountId}, ClearingHouseId={ClearingHouseId}, ValidationId={ValidationId}.",
                    request.ThirdPartyAccountId,
                    request.ClearingHouseId,
                    validation.Id);
            }
            catch
            {
                if (_context.Entry(candidate).State != EntityState.Detached)
                {
                    _context.Entry(candidate).State = EntityState.Detached;
                }
                throw;
            }
        }

        for (var retry = 0; retry < 2; retry++)
        {
            ct.ThrowIfCancellationRequested();
            existingAttempt = await FindExistingReplayAttemptAsync(request, serviceType, idempotencyKey, ct);
            if (existingAttempt is not null)
            {
                return Map(existingAttempt, true);
            }

            // Re-read the contextual validation after a concurrency retry so stale parent state cannot
            // overwrite the winner's CurrentAttemptId/status when the next append-only attempt is persisted.
            if (retry > 0)
            {
                validation = await FindValidationAsync(request, serviceType, ct)
                    ?? throw new InvalidOperationException("La validación contextual desapareció durante la recuperación de concurrencia.");
            }

            var nextAttemptNumber = (await _context.PrenotificationValidationAttempts
                .Where(x => x.ThirdPartyAccountValidationId == validation.Id)
                .MaxAsync(x => (int?)x.AttemptNumber, ct) ?? 0) + 1;

            var attempt = new PrenotificationValidationAttempt
            {
                ThirdPartyAccountValidationId = validation.Id,
                AttemptNumber = nextAttemptNumber,
                AchTransactionId = request.AchTransactionId,
                IdempotencyKey = idempotencyKey,
                Status = technicalReview
                    ? PrenotificationValidationAttemptStatus.TechnicalReview
                    : PrenotificationValidationAttemptStatus.AwaitingResponse,
                SentAtUtc = request.SentAtUtc.ToUniversalTime(),
                ResponseDeadlineAtUtc = responseDeadline,
                Resolution = technicalReview
                    ? PrenotificationValidationResolution.TechnicalReview
                    : PrenotificationValidationResolution.None,
                EvidenceReference = $"prenotification-created:{request.AchTransactionId}",
                CorrelationReference = idempotencyKey,
                SourceCycleId = request.SourceCycleId.Trim(),
                SourceFileReference = NormalizeOptional(request.SourceFileReference)
            };

            validation.PolicyRuleId = policy.Rule?.Id;
            validation.PolicyEffectiveFrom = policy.Rule?.EffectiveFrom;
            validation.EffectiveDate = request.EffectiveDate.Date;
            validation.Status = technicalReview
                ? ThirdPartyAccountValidationStatus.TechnicalReview
                : ThirdPartyAccountValidationStatus.PendingResponse;
            _context.PrenotificationValidationAttempts.Add(attempt);

            IDbContextTransaction? ownedTransaction = null;
            var ownsPersistenceTransaction = false;
            var transaction = _context.Database.CurrentTransaction;
            string? savepointName = null;
            var attemptPersisted = false;

            try
            {
                if (_context.Database.IsRelational())
                {
                    if (transaction is null)
                    {
                        ownedTransaction = await _context.Database.BeginTransactionAsync(ct);
                        ownsPersistenceTransaction = true;
                        transaction = ownedTransaction;
                    }
                    else if (transaction.SupportsSavepoints)
                    {
                        var candidateSavepointName = $"tpa9a3_{validation.Id}_{retry}_{Guid.NewGuid():N}";
                        await transaction.CreateSavepointAsync(candidateSavepointName, ct);
                        savepointName = candidateSavepointName;
                    }
                }

                ct.ThrowIfCancellationRequested();
                await _context.SaveChangesAsync(ct);
                attemptPersisted = true;

                validation.CurrentAttemptId = attempt.Id;
                ct.ThrowIfCancellationRequested();
                await _context.SaveChangesAsync(ct);

                if (ownedTransaction is not null)
                {
                    ct.ThrowIfCancellationRequested();
                    await ownedTransaction.CommitAsync(ct);
                }
                else if (savepointName is not null && transaction is not null)
                {
                    await transaction.ReleaseSavepointAsync(savepointName, ct);
                }

                _logger.LogInformation(
                    "Prenotification validation attempt registered. ThirdPartyAccountId={ThirdPartyAccountId}, ClearingHouseId={ClearingHouseId}, ValidationId={ValidationId}, AttemptId={AttemptId}, AttemptNumber={AttemptNumber}, TechnicalReview={TechnicalReview}.",
                    request.ThirdPartyAccountId,
                    request.ClearingHouseId,
                    validation.Id,
                    attempt.Id,
                    attempt.AttemptNumber,
                    technicalReview);
                return Map(attempt, false, validation.Status);
            }
            catch (DbUpdateException ex) when (RelationalDatabaseExceptionClassifier.IsUniqueViolation(ex))
            {
                await RollbackAttemptPersistenceScopeAsync(ownedTransaction, transaction, savepointName);
                if (ownedTransaction is not null)
                {
                    await ownedTransaction.DisposeAsync();
                    ownedTransaction = null;
                    transaction = null;
                }

                // If the attempt insert already succeeded inside a caller transaction, the failure happened
                // while advancing CurrentAttemptId. The service must not swallow that partial-stage failure.
                // A local savepoint has rolled the scope back; clearing tracked state prevents ghost entities.
                if (attemptPersisted && _context.Database.IsRelational() && !ownsPersistenceTransaction)
                {
                    if (savepointName is null && transaction is not null)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                    }
                    _context.ChangeTracker.Clear();
                    throw;
                }

                await ResetFailedAttemptStateAsync(validation, attempt);

                existingAttempt = await FindExistingReplayAttemptAsync(request, serviceType, idempotencyKey, ct);
                if (existingAttempt is not null)
                {
                    return Map(existingAttempt, true);
                }

                // A retry without an idempotent winner is safe only when the exact attempted sequence
                // was won concurrently. Any other unique violation is outside this lifecycle boundary.
                var concurrentSequenceWinnerExists = await _context.PrenotificationValidationAttempts
                    .AsNoTracking()
                    .AnyAsync(x => x.ThirdPartyAccountValidationId == validation.Id
                                   && x.AttemptNumber == nextAttemptNumber, ct);
                if (!concurrentSequenceWinnerExists || retry == 1)
                {
                    throw;
                }
            }
            catch
            {
                await RollbackAttemptPersistenceScopeAsync(ownedTransaction, transaction, savepointName);
                if (ownedTransaction is not null)
                {
                    await ownedTransaction.DisposeAsync();
                    ownedTransaction = null;
                    transaction = null;
                }

                if (attemptPersisted && _context.Database.IsRelational() && !ownsPersistenceTransaction)
                {
                    if (savepointName is null && transaction is not null)
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                    }
                    _context.ChangeTracker.Clear();
                }
                else
                {
                    await ResetFailedAttemptStateAsync(validation, attempt);
                }
                throw;
            }
            finally
            {
                if (ownedTransaction is not null)
                {
                    await ownedTransaction.DisposeAsync();
                }
            }
        }

        throw new InvalidOperationException("No fue posible registrar el intento de validación después de resolver concurrencia.");
    }

    public async Task<bool> ApplyTransactionResolutionAsync(
        PrenotificationValidationResolutionRequest request,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (request.AchTransactionId <= 0 || string.IsNullOrWhiteSpace(request.EvidenceReference))
        {
            throw new ArgumentException("La resolución requiere transacción persistida y evidencia técnica.", nameof(request));
        }

        var attempt = await _context.PrenotificationValidationAttempts
            .Include(x => x.ThirdPartyAccountValidation)
            .FirstOrDefaultAsync(x => x.AchTransactionId == request.AchTransactionId, ct);
        if (attempt is null)
        {
            return false;
        }

        var resolution = request.TransactionState switch
        {
            AchTransferStateEnum.Certified => PrenotificationValidationResolution.Accepted,
            AchTransferStateEnum.AppliedTacitly => PrenotificationValidationResolution.TacitlyAccepted,
            AchTransferStateEnum.ReturnedByOperator or AchTransferStateEnum.ReturnedByEpr => PrenotificationValidationResolution.Rejected,
            _ => PrenotificationValidationResolution.None
        };
        if (resolution == PrenotificationValidationResolution.None)
        {
            return false;
        }

        // Attempts are append-only evidence. Once a terminal result exists, neither a replay nor a
        // conflicting late event is allowed to rewrite the resolution, causal or evidence fields.
        if (IsTerminalAttempt(attempt))
        {
            return false;
        }

        // TechnicalReview is fail-closed: insufficient or inconsistent policy/deadline context may not
        // be promoted to an accepted/rejected business outcome by a later transaction-state callback.
        if (attempt.Status == PrenotificationValidationAttemptStatus.TechnicalReview
            || attempt.Resolution == PrenotificationValidationResolution.TechnicalReview)
        {
            return false;
        }

        var attemptStatus = resolution switch
        {
            PrenotificationValidationResolution.Accepted => PrenotificationValidationAttemptStatus.Accepted,
            PrenotificationValidationResolution.TacitlyAccepted => PrenotificationValidationAttemptStatus.TacitlyAccepted,
            _ => PrenotificationValidationAttemptStatus.Rejected
        };
        var validationStatus = resolution switch
        {
            PrenotificationValidationResolution.Accepted => ThirdPartyAccountValidationStatus.Accepted,
            PrenotificationValidationResolution.TacitlyAccepted => ThirdPartyAccountValidationStatus.TacitlyAccepted,
            _ => ThirdPartyAccountValidationStatus.Rejected
        };

        var normalizedReturnCode = NormalizeOptional(request.ReturnCode);
        var normalizedDetail = NormalizeOptional(request.ReturnDetail);
        var normalizedEvidence = request.EvidenceReference.Trim();
        var normalizedCorrelation = NormalizeOptional(request.CorrelationReference);
        var normalizedCycle = NormalizeOptional(request.SourceCycleId);
        var resolvedAt = request.ResolvedAtUtc.ToUniversalTime();

        var validation = attempt.ThirdPartyAccountValidation;
        var isCurrentAttempt = validation.CurrentAttemptId == attempt.Id;
        if (!validation.CurrentAttemptId.HasValue)
        {
            // Fail-safe recovery for a missing pointer: only the latest append-only attempt may repair it.
            isCurrentAttempt = !await _context.PrenotificationValidationAttempts
                .AsNoTracking()
                .AnyAsync(x => x.ThirdPartyAccountValidationId == validation.Id
                               && x.AttemptNumber > attempt.AttemptNumber, ct);
        }

        ct.ThrowIfCancellationRequested();
        attempt.Status = attemptStatus;
        attempt.Resolution = resolution;
        attempt.ResolvedAtUtc = resolvedAt;
        attempt.ReturnCode = normalizedReturnCode;
        attempt.ReturnDetail = normalizedDetail;
        attempt.EvidenceReference = normalizedEvidence;
        attempt.CorrelationReference = normalizedCorrelation ?? attempt.CorrelationReference;
        attempt.SourceCycleId = normalizedCycle ?? attempt.SourceCycleId;

        if (isCurrentAttempt)
        {
            validation.Status = validationStatus;
            validation.CurrentAttemptId = attempt.Id;
        }

        _logger.LogInformation(
            "Prenotification validation resolved automatically. ValidationId={ValidationId}, AttemptId={AttemptId}, AchTransactionId={AchTransactionId}, Resolution={Resolution}, CurrentAttempt={CurrentAttempt}.",
            validation.Id,
            attempt.Id,
            request.AchTransactionId,
            resolution,
            isCurrentAttempt);
        return true;
    }

    public async Task<bool> RecordMonetaryUseAsync(
        ThirdPartyAccountMonetaryUseRequest request,
        CancellationToken ct = default)
    {
        if (request.ThirdPartyAccountId <= 0 || request.ValidationId <= 0)
        {
            throw new ArgumentException("El uso monetario requiere cuenta canónica y validación contextual persistidas.", nameof(request));
        }

        var validation = await _context.ThirdPartyAccountValidations
            .FirstOrDefaultAsync(x => x.Id == request.ValidationId
                                      && x.ThirdPartyAccountId == request.ThirdPartyAccountId, ct);
        if (validation is null)
        {
            return false;
        }

        var usedAtUtc = request.UsedAtUtc.ToUniversalTime();
        if (validation.LastMonetaryUseAtUtc.HasValue
            && validation.LastMonetaryUseAtUtc.Value >= usedAtUtc)
        {
            return false;
        }

        validation.LastMonetaryUseAtUtc = usedAtUtc;
        _logger.LogInformation(
            "Third-party account monetary-use evidence updated. ThirdPartyAccountId={ThirdPartyAccountId}, ValidationId={ValidationId}, UsedAtUtc={UsedAtUtc}.",
            request.ThirdPartyAccountId,
            request.ValidationId,
            usedAtUtc);
        return true;
    }

    private async Task<PrenotificationValidationAttempt?> FindExistingReplayAttemptAsync(
        PrenotificationValidationStartRequest request,
        string serviceType,
        string idempotencyKey,
        CancellationToken ct)
    {
        var matches = await _context.PrenotificationValidationAttempts
            .AsNoTracking()
            .Include(x => x.ThirdPartyAccountValidation)
            .Where(x => x.IdempotencyKey == idempotencyKey || x.AchTransactionId == request.AchTransactionId)
            .Take(2)
            .ToListAsync(ct);

        if (matches.Count == 0)
        {
            return null;
        }

        if (matches.Count > 1 && matches.Select(x => x.Id).Distinct().Count() > 1)
        {
            throw new InvalidOperationException(
                "Se detectaron evidencias de intento incompatibles para la misma transacción/idempotency key; no es seguro reutilizar ni crear otro intento.");
        }

        var existing = matches[0];
        EnsureReplayMatchesRequest(existing, request, serviceType);
        return existing;
    }

    private static void EnsureReplayMatchesRequest(
        PrenotificationValidationAttempt attempt,
        PrenotificationValidationStartRequest request,
        string serviceType)
    {
        var validation = attempt.ThirdPartyAccountValidation;
        var sameContext = attempt.AchTransactionId == request.AchTransactionId
                          && validation.ThirdPartyAccountId == request.ThirdPartyAccountId
                          && validation.ClearingHouseId == request.ClearingHouseId
                          && validation.TransactionNature == request.TransactionNature
                          && validation.ValidationType == ThirdPartyAccountValidationType.Prenotification
                          && validation.EffectiveDate.Date == request.EffectiveDate.Date
                          && string.Equals(
                              ThirdPartyAccountValidation.NormalizeServiceType(validation.ServiceType),
                              serviceType,
                              StringComparison.Ordinal);
        if (!sameContext)
        {
            throw new InvalidOperationException(
                "La idempotency key o la transacción ya pertenece a otro contexto de validación; la recuperación no es segura.");
        }
    }

    private static bool IsResolvedPolicyContextCompatible(
        ClearingHouseTransactionRule rule,
        PrenotificationValidationStartRequest request)
    {
        var requestedDate = request.EffectiveDate.Date;
        return rule.IsActive
               && rule.ClearingHouseId == request.ClearingHouseId
               && rule.TransactionNature == request.TransactionNature
               && rule.EffectiveFrom.Date <= requestedDate
               && (!rule.EffectiveTo.HasValue || rule.EffectiveTo.Value.Date >= requestedDate);
    }

    private static bool IsTerminalAttempt(PrenotificationValidationAttempt attempt)
        => (attempt.Status is PrenotificationValidationAttemptStatus.Accepted
                or PrenotificationValidationAttemptStatus.TacitlyAccepted
                or PrenotificationValidationAttemptStatus.Rejected)
           || (attempt.Resolution is PrenotificationValidationResolution.Accepted
                or PrenotificationValidationResolution.TacitlyAccepted
                or PrenotificationValidationResolution.Rejected);

    private async Task RollbackAttemptPersistenceScopeAsync(
        IDbContextTransaction? ownedTransaction,
        IDbContextTransaction? transaction,
        string? savepointName)
    {
        if (ownedTransaction is not null)
        {
            await ownedTransaction.RollbackAsync(CancellationToken.None);
            return;
        }

        if (transaction is not null && savepointName is not null)
        {
            await transaction.RollbackToSavepointAsync(savepointName, CancellationToken.None);
            await transaction.ReleaseSavepointAsync(savepointName, CancellationToken.None);
        }
    }

    private async Task ResetFailedAttemptStateAsync(
        ThirdPartyAccountValidation validation,
        PrenotificationValidationAttempt attempt)
    {
        var attemptEntry = _context.Entry(attempt);
        if (attemptEntry.State != EntityState.Detached)
        {
            attemptEntry.State = EntityState.Detached;
        }

        var validationEntry = _context.Entry(validation);
        if (validationEntry.State != EntityState.Detached)
        {
            await validationEntry.ReloadAsync(CancellationToken.None);
        }
    }

    private Task<ThirdPartyAccountValidation?> FindValidationAsync(
        PrenotificationValidationStartRequest request,
        string serviceType,
        CancellationToken ct)
        => _context.ThirdPartyAccountValidations.FirstOrDefaultAsync(x =>
            x.ThirdPartyAccountId == request.ThirdPartyAccountId
            && x.ClearingHouseId == request.ClearingHouseId
            && x.TransactionNature == request.TransactionNature
            && x.ServiceType == serviceType
            && x.ValidationType == ThirdPartyAccountValidationType.Prenotification,
            ct);

    private static PrenotificationValidationStartResult Map(
        PrenotificationValidationAttempt attempt,
        bool reused,
        ThirdPartyAccountValidationStatus? validationStatus = null)
        => new(
            attempt.ThirdPartyAccountValidationId,
            attempt.Id,
            attempt.AttemptNumber,
            attempt.AchTransactionId,
            validationStatus ?? attempt.ThirdPartyAccountValidation.Status,
            attempt.Status,
            attempt.ResponseDeadlineAtUtc,
            reused);

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

    private static void ValidateStartRequest(PrenotificationValidationStartRequest request)
    {
        if (request.ThirdPartyAccountId <= 0 || request.AchTransactionId <= 0 || request.ClearingHouseId <= 0)
        {
            throw new ArgumentException("La validación requiere cuenta canónica, transacción y cámara persistidas.", nameof(request));
        }
        if (!Enum.IsDefined(request.TransactionNature))
        {
            throw new ArgumentOutOfRangeException(nameof(request.TransactionNature));
        }
        if (request.EffectiveDate == default)
        {
            throw new ArgumentException("La validación requiere una fecha efectiva.", nameof(request));
        }
        if (string.IsNullOrWhiteSpace(request.SourceCycleId) || string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new ArgumentException("La validación requiere ciclo e idempotency key.", nameof(request));
        }
        if (request.IdempotencyKey.Trim().Length > 160)
        {
            throw new ArgumentException("La idempotency key de validación no puede superar 160 caracteres.", nameof(request));
        }
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
