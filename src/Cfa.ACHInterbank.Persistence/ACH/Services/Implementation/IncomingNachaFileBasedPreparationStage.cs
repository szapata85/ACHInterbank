using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// F4.2 preflight for the file-based NACHA queue. This stage deliberately cannot
/// dispatch SOAP or transition an item to Queued/RetryPending. F5 must explicitly
/// homologate the external contract before a separate release path is implemented.
/// </summary>
internal sealed class IncomingNachaFileBasedPreparationStage
{
    internal const string InitiallyBlockedCode = "ACHCOL_DISPATCH_NOT_ENABLED_F33";
    internal const string MappingPendingCode = "ACHCOL_MAPPING_NOT_READY_F42";
    internal const string RejectedCode = "ACHCOL_PREFLIGHT_REJECTED_F42";

    private readonly AchDbContext _context;
    private readonly IncomingNachaDispatchEligibilityPolicy _policy;
    private readonly IncomingNachaAuditEventsStage _audit;

    public IncomingNachaFileBasedPreparationStage(
        AchDbContext context,
        IncomingNachaDispatchEligibilityPolicy policy,
        IncomingNachaAuditEventsStage audit)
    {
        _context = context;
        _policy = policy;
        _audit = audit;
    }

    public async Task<IncomingNachaFileBasedPreparationResult> PrepareAsync(
        int chunkSize, bool featureEnabled, CancellationToken ct)
    {
        // OFF by default. Never process or mutate ACHCOL work without an explicit opt-in.
        if (!featureEnabled)
            return new IncomingNachaFileBasedPreparationResult(0, 0);

        // Only F3.3-originated blocked items; already prepared/rejected work is not
        // re-evaluated every scheduler tick, avoiding duplicate audit events.
        var candidates = await _context.IncomingNachaDispatchQueue
            .Where(q => q.SourceKind == IncomingNachaDispatchSourceKinds.AchColFileBased
                && q.QueueStatus == IncomingNachaDispatchQueueStatus.Blocked
                && q.LastErrorCode == InitiallyBlockedCode
                && q.AttemptCount == 0 && q.NextAttemptAtUtc == null)
            .OrderBy(q => q.Priority).ThenBy(q => q.Id)
            .Take(Math.Clamp(chunkSize, 1, 500))
            .ToListAsync(ct);
        if (candidates.Count == 0)
            return new IncomingNachaFileBasedPreparationResult(0, 0);

        var ingestionIds = candidates.Select(q => q.IncomingNachaFileIngestionId).Distinct().ToArray();
        var classificationIds = candidates.Select(q => q.IncomingNachaEntryClassificationId).Distinct().ToArray();
        var entryIds = candidates.Where(q => q.EntryDetailId.HasValue)
            .Select(q => q.EntryDetailId!.Value).Distinct().ToArray();
        var clearingIds = candidates.Select(q => q.ClearingHouseId).Distinct().ToArray();

        var ingestions = await _context.IncomingNachaFileIngestions.AsNoTracking()
            .Where(i => ingestionIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);
        var classifications = await _context.IncomingNachaEntryClassifications.AsNoTracking()
            .Where(c => classificationIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
        var entries = await _context.EntryDetails.AsNoTracking()
            .Where(e => entryIds.Contains(e.EntryDetailID)).ToDictionaryAsync(e => e.EntryDetailID, ct);
        var headerIds = entries.Values.Select(e => e.NachaID).Distinct().ToArray();
        var headers = await _context.NachaHeaders.AsNoTracking()
            .Where(h => headerIds.Contains(h.NachaID)).ToDictionaryAsync(h => h.NachaID, ct);
        var chambers = await _context.ClearingHouses.AsNoTracking()
            .Where(c => clearingIds.Contains(c.ClearingHouseId)).ToDictionaryAsync(c => c.ClearingHouseId, ct);

        var readyForMapping = 0;
        var rejected = 0;
        foreach (var queue in candidates)
        {
            ct.ThrowIfCancellationRequested();
            var reason = "ACHCOL_PREFLIGHT_CONTEXT_MISSING";
            var valid = false;
            if (queue.AchCycleId is null
                && queue.EntryDetailId is int entryId
                && ingestions.TryGetValue(queue.IncomingNachaFileIngestionId, out var ingestion)
                && classifications.TryGetValue(queue.IncomingNachaEntryClassificationId, out var classification)
                && entries.TryGetValue(entryId, out var entry)
                && headers.TryGetValue(entry.NachaID, out var header)
                && chambers.TryGetValue(queue.ClearingHouseId, out var chamber)
                && classification.IncomingNachaFileIngestionId == ingestion.Id
                && classification.EntryDetailId == entryId
                && ingestion.ResolvedClearingHouseId == queue.ClearingHouseId
                && ingestion.OperationalDate?.Date == queue.OperationalDate.Date)
            {
                var decision = await _policy.EvaluateFileBasedAsync(
                    ingestion, classification, header, entry, chamber, featureEnabled, ct);
                reason = decision.Reason;
                valid = decision.IsEligible && !decision.IsBlocked && !decision.IsWaitingWindow
                    && IncomingNachaFileBasedDispatchIdentity.TryBuild(
                        queue.ClearingHouseId, queue.OperationalDate, header, entry, out var expectedKey)
                    && string.Equals(queue.IdempotencyDispatchKey, expectedKey, StringComparison.Ordinal);
                if (decision.IsEligible && !valid)
                    reason = "ACHCOL_PREFLIGHT_IDEMPOTENCY_OR_QUEUE_MISMATCH";
            }

            // A positive preflight remains blocked: transport and retry are NOT
            // authorized here, even when ProcTransacciones operating mode is Live.
            queue.QueueStatus = IncomingNachaDispatchQueueStatus.Blocked;
            queue.NextAttemptAtUtc = null;
            if (valid)
            {
                queue.Priority = 100;
                queue.LastErrorCode = MappingPendingCode;
                queue.LastErrorMessage = "Contexto ACHCOL revalidado. Pendiente de homologacion SOAP F5; envio deshabilitado.";
                _audit.AddAutomaticEvent(queue, "AchColFileBasedPreflightReady", "Blocked",
                    "Identidad NACHA validada; no se autorizo transporte CFA.", MappingPendingCode);
                readyForMapping++;
            }
            else
            {
                queue.LastErrorCode = RejectedCode;
                queue.LastErrorMessage = reason;
                _audit.AddAutomaticEvent(queue, "AchColFileBasedPreflightRejected", "Blocked",
                    "La revalidacion NACHA no autorizo la entrada.", RejectedCode);
                rejected++;
            }
        }

        // Persist independent preflight/audit changes before the unchanged cycle-bound
        // worker. Only queue state and events are written; no integration executions.
        await _context.SaveChangesAsync(ct);
        return new IncomingNachaFileBasedPreparationResult(readyForMapping, rejected);
    }
}

internal sealed record IncomingNachaFileBasedPreparationResult(int ReadyForMapping, int Rejected)
{
    public int Processed => ReadyForMapping + Rejected;
}
