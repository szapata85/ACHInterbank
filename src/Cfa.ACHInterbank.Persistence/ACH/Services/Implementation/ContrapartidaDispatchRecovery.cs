using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>Recovery for orphaned claims; independent of ACH operational hours.</summary>
public static class ContrapartidaDispatchRecovery
{
    public const string Idle = "Idle";
    public const string Preparing = "Preparing";
    public const string InFlight = "InFlight";
    public static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(15);
    private const string UnknownCode = "E2E02_OUTCOME_UNKNOWN";
    public const string UnsentCycleCode = "RES_H02_CYCLE_CLOSED_PRE_SEND";
    internal const int MaxSoapAttempts = 5;
    private const string ExhaustedCode = "H01_RETRY_LIMIT_RECONCILIATION";

    public static async Task<int> RecoverStaleAsync(AchDbContext db, DateTime nowUtc,
        ILogger? logger = null, CancellationToken ct = default)
    {
        // Include legacy Reporting/Retrying records with no lease: they may already have transmitted.
        var candidates = await db.ContrapartidaDispatchItems.AsNoTracking()
            .Where(x => x.State == ContrapartidaDispatchItemStateEnum.ReportingContrapartida
                     || x.State == ContrapartidaDispatchItemStateEnum.Retrying)
            .Where(x => !x.DispatchLeaseExpiresAtUtc.HasValue || x.DispatchLeaseExpiresAtUtc <= nowUtc)
            .OrderBy(x => x.Id).Take(100)
            .Select(x => new { x.Id, x.State, x.DispatchLeaseToken, x.DispatchLeasePhase,
                x.DispatchLeaseExpiresAtUtc, x.AttemptCount, x.RequiresReconciliation })
            .ToListAsync(ct);
        // Also clean up legacy RetryPending records left at the limit by older recovery code.
        // This scanner runs from Quartz even without an active ACH cycle.
        var recovered = await FinalizeExhaustedRetryPendingAsync(db, nowUtc, logger, ct);
        foreach (var candidate in candidates)
        {
            // If an earlier result was already declared uncertain, never restore retry eligibility.
            var safeToRetry = candidate.DispatchLeasePhase == Preparing
                && !candidate.RequiresReconciliation && candidate.AttemptCount < MaxSoapAttempts;
            var recoveryToken = $"recovery:{Guid.NewGuid():N}";
            Guid? batchId = null;
            var tokenSegments = candidate.DispatchLeaseToken.Split(':');
            if (tokenSegments.Length == 2
                && (tokenSegments[0] == "scheduled" || tokenSegments[0] == "manual-retry")
                && Guid.TryParseExact(tokenSegments[1], "N", out var parsedBatchId))
                batchId = parsedBatchId;
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var claimed = await db.ContrapartidaDispatchItems
                .Where(x => x.Id == candidate.Id && x.State == candidate.State
                    && x.DispatchLeaseToken == candidate.DispatchLeaseToken
                    // AUD-E2E-02: phase, expiry and attempt are part of the SAME database CAS.
                    // A worker moving Preparing -> InFlight cannot be recovered as NotSent.
                    && x.DispatchLeasePhase == candidate.DispatchLeasePhase
                    && x.DispatchLeaseExpiresAtUtc == candidate.DispatchLeaseExpiresAtUtc
                    && x.AttemptCount == candidate.AttemptCount
                    && x.RequiresReconciliation == candidate.RequiresReconciliation
                    && (!x.DispatchLeaseExpiresAtUtc.HasValue || x.DispatchLeaseExpiresAtUtc <= nowUtc))
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(x => x.State, safeToRetry
                        ? ContrapartidaDispatchItemStateEnum.RetryPending
                        : ContrapartidaDispatchItemStateEnum.ContrapartidaReportFailed)
                    .SetProperty(x => x.DispatchLeaseToken, recoveryToken)
                    .SetProperty(x => x.DispatchLeaseExpiresAtUtc, (DateTime?)null)
                    .SetProperty(x => x.DispatchLeasePhase, Idle)
                    .SetProperty(x => x.RequiresReconciliation, !safeToRetry)
                    // Do not consume a SOAP attempt for an internal lease recovery.
                    // AttemptCount counts completed dispatch executions, never watchdog passes.
                    .SetProperty(x => x.LastCorrelationId, recoveryToken)
                    .SetProperty(x => x.LastDispatchedBy, "recovery:e2e02")
                    .SetProperty(x => x.LastResponseCode, safeToRetry ? "NOT_SENT" : "UNKNOWN")
                    .SetProperty(x => x.LastErrorCode, safeToRetry ? "E2E02_PRE_SEND_CRASH" : UnknownCode)
                    .SetProperty(x => x.LastErrorMessage, safeToRetry
                        ? "Interrupción durante preparación: no se habilitó el transporte SOAP."
                        : "Posible envío SOAP previo a interrupción; conciliación obligatoria, sin retransmisión.")
                    .SetProperty(x => x.NextAttemptAtUtc, safeToRetry ? (DateTime?)nowUtc : null), ct);
            if (claimed == 0)
            {
                await tx.RollbackAsync(ct);
                continue;
            }

            db.ContrapartidaDispatchAttempts.Add(new ContrapartidaDispatchAttempt
            {
                DispatchItemId = candidate.Id,
                DispatchBatchId = batchId,
                // Negative sequence numbers are recovery-only audit events. They cannot
                // collide with SOAP attempt numbers (unique item/attempt index).
                AttemptNumber = await NextRecoveryAuditNumberAsync(db, candidate.Id, ct),
                StartedAtUtc = nowUtc,
                FinishedAtUtc = nowUtc,
                Result = ContrapartidaDispatchAttemptResultEnum.Failed,
                CorrelationId = recoveryToken,
                TriggeredBy = "recovery:e2e02",
                RetryEligible = safeToRetry,
                RetryAllowed = safeToRetry,
                RequiresManualReview = !safeToRetry,
                TransportStatus = safeToRetry ? IntegrationTransportStatus.NotExecuted : IntegrationTransportStatus.Failed,
                BusinessStatus = IntegrationResponseBusinessStatus.Unknown,
                IsTechnicalFailure = !safeToRetry,
                ErrorCode = safeToRetry ? "E2E02_PRE_SEND_CRASH" : UnknownCode,
                ErrorMessage = safeToRetry
                    ? "Lease de preparación vencida; SOAP aún no habilitado."
                    : "Resultado SOAP desconocido tras caída; consultar conciliación.",
                ExternalResponseCode = "RECOVERY",
                SoapMethodName = "Proc_Contrapartidas",
                SoapTechnicalStatus = safeToRetry ? "NotSent" : "Unknown",
                ProcessedAtUtc = nowUtc,
                TechnicalException = "Interrupción detectada por recuperación E2E-02"
            });
            if (batchId.HasValue)
            {
                await db.ContrapartidaDispatchBatches
                    .Where(b => b.Id == batchId.Value
                        && b.Status == ContrapartidaDispatchBatchStatusEnum.Processing)
                    .ExecuteUpdateAsync(updates => updates
                        .SetProperty(b => b.Status, ContrapartidaDispatchBatchStatusEnum.Failed)
                        .SetProperty(b => b.FinishedAtUtc, (DateTime?)nowUtc)
                        .SetProperty(b => b.SummaryMessage,
                            "Interrupción detectada E2E-02: verificar intentos recuperados."), ct);
            }
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            recovered++;
            logger?.LogWarning("E2E02 recovered orphan item {ItemId}: phase={Phase} safeToRetry={Safe}",
                candidate.Id, candidate.DispatchLeasePhase, safeToRetry);
        }
        // The scanner is bounded. The next Quartz invocation handles any remaining orphans.
        return recovered;
    }

    private static async Task<int> NextRecoveryAuditNumberAsync(
        AchDbContext db, long itemId, CancellationToken ct)
    {
        var lowest = await db.ContrapartidaDispatchAttempts.AsNoTracking()
            .Where(x => x.DispatchItemId == itemId && x.AttemptNumber < 0)
            .Select(x => (int?)x.AttemptNumber).MinAsync(ct);
        return lowest.HasValue ? checked(lowest.Value - 1) : -1;
    }

    /// <summary>
    /// Existing (legacy) RetryPending rows with an exhausted counter must not be
    /// silently ignored forever. Fail closed rather than adjusting historical counts
    /// or implicitly authorising another monetary debit.
    /// </summary>
    private static async Task<int> FinalizeExhaustedRetryPendingAsync(
        AchDbContext db, DateTime nowUtc, ILogger? logger, CancellationToken ct)
    {
        var candidates = await db.ContrapartidaDispatchItems.AsNoTracking()
            .Where(x => x.State == ContrapartidaDispatchItemStateEnum.RetryPending
                && x.AttemptCount >= MaxSoapAttempts)
            .OrderBy(x => x.Id).Take(100)
            .Select(x => new { x.Id, x.AttemptCount, x.DispatchLeaseToken,
                x.DispatchLeasePhase, x.RequiresReconciliation })
            .ToListAsync(ct);
        var finalized = 0;
        foreach (var item in candidates)
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            var token = $"recovery:exhausted:{Guid.NewGuid():N}";
            var updated = await db.ContrapartidaDispatchItems
                .Where(x => x.Id == item.Id
                    && x.State == ContrapartidaDispatchItemStateEnum.RetryPending
                    && x.AttemptCount == item.AttemptCount
                    && x.DispatchLeaseToken == item.DispatchLeaseToken
                    && x.DispatchLeasePhase == item.DispatchLeasePhase
                    && x.RequiresReconciliation == item.RequiresReconciliation)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.State, ContrapartidaDispatchItemStateEnum.ContrapartidaReportFailed)
                    .SetProperty(x => x.RequiresReconciliation, true)
                    .SetProperty(x => x.NextAttemptAtUtc, (DateTime?)null)
                    .SetProperty(x => x.DispatchLeaseToken, token)
                    .SetProperty(x => x.DispatchLeaseExpiresAtUtc, (DateTime?)null)
                    .SetProperty(x => x.DispatchLeasePhase, Idle)
                    .SetProperty(x => x.LastCorrelationId, token)
                    .SetProperty(x => x.LastDispatchedBy, "recovery:h01")
                    .SetProperty(x => x.LastErrorCode, ExhaustedCode)
                    .SetProperty(x => x.LastErrorMessage,
                        "Límite de reintentos alcanzado; requiere revisión y conciliación, sin retransmisión automática."), ct);
            if (updated == 0)
            {
                await tx.RollbackAsync(ct);
                continue;
            }
            db.ContrapartidaDispatchAttempts.Add(new ContrapartidaDispatchAttempt
            {
                DispatchItemId = item.Id,
                AttemptNumber = await NextRecoveryAuditNumberAsync(db, item.Id, ct),
                StartedAtUtc = nowUtc,
                FinishedAtUtc = nowUtc,
                Result = ContrapartidaDispatchAttemptResultEnum.Failed,
                CorrelationId = token,
                TriggeredBy = "recovery:h01",
                RetryEligible = false,
                RetryAllowed = false,
                RequiresManualReview = true,
                TransportStatus = IntegrationTransportStatus.NotExecuted,
                BusinessStatus = IntegrationResponseBusinessStatus.Unknown,
                ErrorCode = ExhaustedCode,
                ErrorMessage = "Contador agotado; conciliación manual, sin SOAP.",
                SoapMethodName = "Proc_Contrapartidas",
                SoapTechnicalStatus = "NotSent",
                ProcessedAtUtc = nowUtc
            });
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            finalized++;
            logger?.LogWarning("H01 moved exhausted RetryPending item {ItemId} to reconciliation", item.Id);
        }
        return finalized;
    }

    /// <summary>Advance the fence before network I/O (legacy tests/clients that don't supply evidence).</summary>
    public static async Task MarkBeforeTransportAsync(AchDbContext db,
        IReadOnlyList<long> itemIds, string token, DateTime nowUtc, CancellationToken ct)
    {
        var updated = await TransitionToInFlightAsync(db, itemIds, token, nowUtc, ct);
        if (updated != itemIds.Count)
            throw new InvalidOperationException("E2E02_LEASE_LOST: reclamo vencido o recuperado. SOAP no invocado.");
    }

    /// <summary>
    /// AUD-E2E-04: an immutable SOAP intent and the InFlight fence commit together.
    /// No network call is permitted until both writes have committed.
    /// </summary>
    public static async Task PersistIntentBeforeTransportAsync(AchDbContext db,
        IReadOnlyList<long> itemIds, string token, Guid batchId, DateTime nowUtc,
        string payload, string payloadHash, Guid? mappingSetId, int? mappingVersion,
        string mappingSnapshotHash, string soapEndpoint, string executionMode, CancellationToken ct)
    {
        if (itemIds.Count != 1 || string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(payloadHash))
            throw new InvalidOperationException("E2E04_INTENT_INVALID: SOAP sin payload/hash o cardinalidad distinta de uno.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var totalItems = await db.ContrapartidaDispatchBatches.AsNoTracking()
            .Where(x => x.Id == batchId && x.Status == ContrapartidaDispatchBatchStatusEnum.Processing)
            .Select(x => (int?)x.TotalItems).SingleOrDefaultAsync(ct);
        if (!totalItems.HasValue)
            throw new InvalidOperationException("E2E04_INTENT_CONFLICT: lote no disponible para SOAP. No enviar.");

        // Atomic per-item evidence + phase fence; survives a crash in a multi-item manual retry.
        var advanced = await db.ContrapartidaDispatchItems
            .Where(x => itemIds.Contains(x.Id)
                && x.DispatchLeaseToken == token && !x.RequiresReconciliation
                && (x.State == ContrapartidaDispatchItemStateEnum.ReportingContrapartida
                    || x.State == ContrapartidaDispatchItemStateEnum.Retrying)
                && x.DispatchLeasePhase == Preparing && x.DispatchLeaseExpiresAtUtc > nowUtc)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.DispatchLeasePhase, InFlight)
                .SetProperty(x => x.DispatchIntentCorrelationId, token)
                .SetProperty(x => x.DispatchIntentPayloadHash, payloadHash)
                .SetProperty(x => x.DispatchIntentPayloadXml, payload)
                .SetProperty(x => x.DispatchIntentMappingSetId, mappingSetId)
                .SetProperty(x => x.DispatchIntentMappingVersion, mappingVersion)
                .SetProperty(x => x.DispatchIntentMappingSnapshotHash, mappingSnapshotHash)
                .SetProperty(x => x.DispatchIntentSoapEndpoint, soapEndpoint)
                .SetProperty(x => x.DispatchIntentExecutionMode, executionMode)
                .SetProperty(x => x.DispatchIntentAtUtc, (DateTime?)nowUtc), ct);
        if (advanced != itemIds.Count)
            throw new InvalidOperationException("E2E02_LEASE_LOST: no se puede registrar intención de un reclamo vencido.");

        // For single-request batches retain the existing batch-level read-model as well.
        // For manual multi-request batches, never overwrite another item's SOAP intent.
        if (totalItems.Value <= 1)
        {
            var persisted = await db.ContrapartidaDispatchBatches
                .Where(x => x.Id == batchId && x.Status == ContrapartidaDispatchBatchStatusEnum.Processing
                    && x.DispatchIntentAtUtc == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.DispatchCorrelationId, token)
                    .SetProperty(x => x.RequestPayloadHash, payloadHash)
                    .SetProperty(x => x.RequestPayloadXml, payload)
                    .SetProperty(x => x.MappingSetId, mappingSetId)
                    .SetProperty(x => x.MappingVersion, mappingVersion)
                    .SetProperty(x => x.MappingSnapshotHash, mappingSnapshotHash)
                    .SetProperty(x => x.DispatchSoapEndpoint, soapEndpoint)
                    .SetProperty(x => x.DispatchExecutionMode, executionMode)
                    .SetProperty(x => x.DispatchIntentAtUtc, (DateTime?)nowUtc), ct);
            if (persisted != 1)
                throw new InvalidOperationException("E2E04_INTENT_CONFLICT: no se confirmó la intención SOAP. No enviar.");
        }
        await tx.CommitAsync(ct);
    }

    /// <summary>
    /// Cancel an owned, definitely-unsent claim (Preparing or InFlight) atomically.
    /// If the process dies after the intent commit but before this commit, the
    /// recovery scanner conservatively requires reconciliation: never blind retry.
    /// </summary>
    public static async Task AbortUnsentCycleClaimAsync(
        AchDbContext db, IReadOnlyList<long> itemIds, string token, Guid batchId,
        string cycleId, string expectedPhase, DateTime nowUtc, string payload,
        string endpoint, string executionMode, CancellationToken ct)
    {
        if (itemIds.Count != 1 || (expectedPhase != Preparing && expectedPhase != InFlight))
            throw new InvalidOperationException("RES_H02_ABORT_INVALID: only a single owned pre-send claim can be aborted.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var itemId = itemIds[0];
        var affected = await db.ContrapartidaDispatchItems
            .Where(x => x.Id == itemId && x.AchCycleId == cycleId
                && x.DispatchLeaseToken == token && x.DispatchLeasePhase == expectedPhase
                && !x.RequiresReconciliation
                && (x.State == ContrapartidaDispatchItemStateEnum.ReportingContrapartida
                    || x.State == ContrapartidaDispatchItemStateEnum.Retrying))
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => x.State, ContrapartidaDispatchItemStateEnum.RetryPending)
                .SetProperty(x => x.DispatchLeasePhase, Idle)
                .SetProperty(x => x.DispatchLeaseToken, string.Empty)
                .SetProperty(x => x.DispatchLeaseExpiresAtUtc, (DateTime?)null)
                .SetProperty(x => x.NextAttemptAtUtc, (DateTime?)nowUtc)
                .SetProperty(x => x.LastDispatchedBy, "preflight:cycle-closed")
                .SetProperty(x => x.LastErrorCode, UnsentCycleCode)
                .SetProperty(x => x.LastResponseCode, "NOT_SENT")
                .SetProperty(x => x.LastErrorMessage,
                    "Ciclo o claim dejó de ser ejecutable antes del transporte SOAP. No se efectuó débito.")
                .SetProperty(x => x.UpdatedAt, new DateTimeOffset(nowUtc, TimeSpan.Zero)), ct);
        if (affected != 1)
            throw new InvalidOperationException("RES_H02_ABORT_LOST: lease/cycle moved; SOAP must NOT be sent.");

        db.ContrapartidaDispatchAttempts.Add(new ContrapartidaDispatchAttempt
        {
            DispatchItemId = itemId,
            DispatchBatchId = batchId,
            AttemptNumber = await NextRecoveryAuditNumberAsync(db, itemId, ct),
            StartedAtUtc = nowUtc, FinishedAtUtc = nowUtc,
            Result = ContrapartidaDispatchAttemptResultEnum.Failed,
            CorrelationId = token, TriggeredBy = "preflight:cycle-closed",
            RetryEligible = true, RetryAllowed = true,
            RequiresManualReview = false,
            TransportStatus = IntegrationTransportStatus.NotExecuted,
            BusinessStatus = IntegrationResponseBusinessStatus.Unknown,
            ErrorCode = UnsentCycleCode, ExternalResponseCode = "NOT_SENT",
            ErrorMessage = "Ventana, calendario, estado de ciclo o lease cambió antes del SOAP.",
            RequestPayloadXml = payload,
            SoapMethodName = "Proc_Contrapartidas", SoapEndpoint = endpoint,
            ExecutionMode = executionMode, SoapTechnicalStatus = "NotSent",
            ProcessedAtUtc = nowUtc
        });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private static Task<int> TransitionToInFlightAsync(AchDbContext db,
        IReadOnlyList<long> itemIds, string token, DateTime nowUtc, CancellationToken ct)
        => db.ContrapartidaDispatchItems
            .Where(x => itemIds.Contains(x.Id)
                && x.DispatchLeaseToken == token
                && !x.RequiresReconciliation
                && (x.State == ContrapartidaDispatchItemStateEnum.ReportingContrapartida
                    || x.State == ContrapartidaDispatchItemStateEnum.Retrying)
                && x.DispatchLeasePhase == Preparing
                && x.DispatchLeaseExpiresAtUtc > nowUtc)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(x => x.DispatchLeasePhase, InFlight), ct);
}
