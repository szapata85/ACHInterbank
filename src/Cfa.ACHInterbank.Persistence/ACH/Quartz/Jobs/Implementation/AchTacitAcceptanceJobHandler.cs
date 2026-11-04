using System.Data;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;

[DisallowConcurrentExecution]
public class AchTacitAcceptanceJobHandler : ITaskHandler
{
    private readonly AchDbContext _db;
    private readonly IAchStateTransitionService _stateTransitionService;
    private readonly IClearingHouseValidationPolicyService _validationPolicyService;
    private readonly ILogger<AchTacitAcceptanceJobHandler> _log;
    private readonly TimeProvider _timeProvider;

    public string Code => "AchTacitAcceptanceJob";

    public AchTacitAcceptanceJobHandler(
        AchDbContext db,
        IAchStateTransitionService stateTransitionService,
        IClearingHouseValidationPolicyService validationPolicyService,
        ILogger<AchTacitAcceptanceJobHandler> log,
        TimeProvider? timeProvider = null)
    {
        _db = db;
        _stateTransitionService = stateTransitionService;
        _validationPolicyService = validationPolicyService;
        _log = log;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var batchSize = ParsePositiveInt(task, "BatchSize", 500);
        // APN-I12: rotate a durable, strictly increasing keyset cursor across runs.
        // This keeps permanently blocked early rows from starving later candidates.
        // It also reexamines attempts whose persisted deadline was null while the
        // future operational calendar was incomplete (the safety gate recomputes it).
        var defaultScanLimit = (int)Math.Min(20_000L, Math.Max((long)batchSize, (long)batchSize * 4));
        var scanLimit = Math.Max(batchSize, ParsePositiveInt(task, "MaxCandidatesPerRun", defaultScanLimit));
        var pageSize = Math.Min(250, scanLimit);
        var cursor = 0;
        if (task.Id > 0)
        {
            var rawCursor = await _db.TaskParameters.AsNoTracking()
                .Where(x => x.TaskDefinitionId == task.Id
                            && x.Key == PrenotificationCandidateScan.CursorParameterKey)
                .Select(x => x.Value)
                .FirstOrDefaultAsync(cancellationToken);
            cursor = PrenotificationCandidateScan.ParseCursor(rawCursor);
        }
        else
        {
            _log.LogWarning("APN-I12: ejecución sin TaskDefinition.Id persistido; cursor no durable.");
        }

        var applied = 0;
        var skipped = 0;
        var errors = 0;
        var examined = 0;
        var lastExamined = cursor;
        var wrapped = false;

        // Two disjoint ranges, (cursor, MAX] and [1, cursor], prevent permanent
        // starvation without an unbounded sweep. The cursor only controls who is
        // examined next; it NEVER authorizes a tacit acceptance.
        for (var pass = 0; pass < (cursor > 0 ? 2 : 1)
                && applied < batchSize && examined < scanLimit; pass++)
        {
            var afterId = pass == 0 ? cursor : 0;
            var maxId = pass == 0 ? int.MaxValue : cursor;
            if (pass == 1) wrapped = true;

            while (applied < batchSize && examined < scanLimit)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var page = await PrenotificationCandidateScan.LoadPageAsync(
                    _db, now, afterId, maxId,
                    Math.Min(pageSize, scanLimit - examined), cancellationToken);
                if (page.Count == 0) break;

                foreach (var transactionId in page)
                {
                    if (applied >= batchSize || examined >= scanLimit) break;
                    cancellationToken.ThrowIfCancellationRequested();
                    afterId = transactionId;
                    lastExamined = transactionId;
                    examined++;
                    try
                    {
                        if (await ApplyIfSafeAsync(transactionId, now, cancellationToken))
                            applied++;
                        else
                            skipped++;
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        skipped++;
                        errors++;
                        _db.ChangeTracker.Clear();
                        _log.LogWarning(ex,
                            "APN-I12: prenotificación {TransactionId} retenida; no se aplicó aceptación tácita.",
                            transactionId);
                    }
                }
            }
        }

        // Existing TaskParameters table and unique (TaskDefinitionId, Key) index:
        // no migration and no new domain state. A failed cursor write only delays
        // fair scanning; it must not undo/reclassify successful state transitions.
        if (task.Id > 0)
        {
            try
            {
                var parameter = await _db.TaskParameters
                    .SingleOrDefaultAsync(x => x.TaskDefinitionId == task.Id
                        && x.Key == PrenotificationCandidateScan.CursorParameterKey, cancellationToken);
                if (parameter is null)
                    _db.TaskParameters.Add(new Cfa.ACHInterbank.Domain.Entities.SchedulerTask.TaskParameter
                    {
                        TaskDefinitionId = task.Id,
                        Key = PrenotificationCandidateScan.CursorParameterKey,
                        Value = lastExamined.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    });
                else
                    parameter.Value = lastExamined.ToString(System.Globalization.CultureInfo.InvariantCulture);
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _db.ChangeTracker.Clear();
                _log.LogWarning(ex, "APN-I12: no se pudo persistir cursor de exploración; próximo ciclo reintentará.");
            }
        }

        if (examined == scanLimit)
            _log.LogWarning(
                "APN-I12: límite de exploración alcanzado ({Examined}/{ScanLimit}). " +
                "Si existen más candidatos, se evaluarán en ejecuciones posteriores. Cursor={Cursor}.",
                examined, scanLimit, lastExamined);
        _log.LogInformation(
            "APN-I12: Evaluadas={Examined}, Aplicadas={Applied}, Omitidas={Skipped}, " +
            "Errores={Errors}, CursorInicial={InitialCursor}, CursorFinal={FinalCursor}, " +
            "SegundaPasada={Wrapped}, UTC={Now}",
            examined, applied, skipped, errors, cursor, lastExamined, wrapped, now);

        return $"Tacit acceptance APN-I12. Evaluadas: {examined}. Aplicadas: {applied}. " +
            $"Omitidas: {skipped}. Errores: {errors}. Cursor: {lastExamined}. UtcNow: {now:O}.";
    }

    private async Task<bool> ApplyIfSafeAsync(int transactionId, DateTimeOffset now, CancellationToken ct)
    {
        // The state transition and the final guard share one persistence transaction.
        // Serializable is required to prevent a concurrent inbound return from being
        // silently overwritten by a previously evaluated candidate. Relational providers
        // enforce the isolation; non-relational tests still exercise the guard logic.
        await using var transaction = _db.Database.IsRelational()
            ? await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;
        try
        {
            var original = await _db.AchTransactions.AsNoTracking()
                .Include(x => x.AchCycle)
                .FirstOrDefaultAsync(x => x.Id == transactionId, ct);
            if (original is null || !original.IsPrenotification
                || original.State != AchTransferStateEnum.Pending
                || original.AchCycle is null)
                return false;

            var houseId = original.AchCycle.ClearingHouseId;
            var attempts = await _db.PrenotificationValidationAttempts.AsNoTracking()
                .Include(x => x.ThirdPartyAccountValidation)
                    .ThenInclude(x => x.PolicyRule)
                .Where(x => x.AchTransactionId == transactionId)
                .Take(2)
                .ToListAsync(ct);

            ClearingHouseTransactionRule? rule;
            DateTimeOffset? storedDeadline = null;
            string sourceCycleId;
            var canonicalValid = true;

            if (attempts.Count != 0)
            {
                // A conflicting or noncurrent attempt must be examined manually.
                if (attempts.Count != 1) return false;
                var attempt = attempts[0];
                var validation = attempt.ThirdPartyAccountValidation;
                canonicalValid = attempt.Status == PrenotificationValidationAttemptStatus.AwaitingResponse
                    && attempt.Resolution == PrenotificationValidationResolution.None
                    && validation.Status == ThirdPartyAccountValidationStatus.PendingResponse
                    && validation.CurrentAttemptId == attempt.Id
                    && validation.ClearingHouseId == houseId
                    && validation.PolicyRuleId.HasValue
                    && validation.PolicyRuleId == validation.PolicyRule?.Id
                    && attempt.SourceCycleId == original.AchCycleId;
                if (!canonicalValid) return false;
                rule = validation.PolicyRule;
                storedDeadline = attempt.ResponseDeadlineAtUtc;
                sourceCycleId = attempt.SourceCycleId!;
            }
            else
            {
                var prenotificationType = NachaTransactionCodeTaxonomy.ResolvePrenotificationDirection(original.TransactionCode);
                var nature = prenotificationType.HasValue
                    ? TransactionPrerequisitePolicyService.ResolveNature(prenotificationType.Value)
                    : null;
                if (!nature.HasValue) return false;
                var policy = await _validationPolicyService.ResolveAsync(
                    houseId, nature.Value, original.EffectiveEntryDate, ct);
                rule = policy.Rule;
                sourceCycleId = original.AchCycleId;
            }

            if (rule is null || !rule.TacitAcceptanceEnabled
                || rule.ClearingHouseId != houseId)
                return false;
            var recalculatedDeadline = await _validationPolicyService
                .ResolvePrenotificationResponseDeadlineAsync(rule, sourceCycleId, ct);
            if (!recalculatedDeadline.HasValue) return false;

            // An exported file is the minimum positive proof of transmission. Creation
            // of a transaction/attempt alone does NOT prove delivery to the chamber.
            var transmitted = await _db.AchFileExportTransactions.AsNoTracking()
                .AnyAsync(x => x.AchTransactionId == transactionId
                               && x.AchCycleId == original.AchCycleId
                               && x.TraceNumber == original.TraceNumber
                               && x.AchFileExport.ClearingHouseId == houseId
                               && x.AchFileExport.AchCycleId == original.AchCycleId
                               && x.AchFileExport.TransmittedAtUtc.HasValue
                               && (x.AchFileExport.LifecycleStatus == AchFileExportLifecycleStatus.Transmitted
                                   || x.AchFileExport.LifecycleStatus == AchFileExportLifecycleStatus.Acknowledged
                                   || x.AchFileExport.LifecycleStatus == AchFileExportLifecycleStatus.Accepted), ct);
            if (!transmitted) return false;

            // A response may already be received but not yet homologated/transitioned.
            // It is never safe to infer a positive result from that ambiguity.
            var trace = original.TraceNumber;
            var externalId = original.TransactionExternalId;
            var reference = original.Reference;
            var hasResponse = await _db.AchResponses.AsNoTracking()
                .AnyAsync(x => (x.ClearingHouseId == houseId || !x.ClearingHouseId.HasValue)
                               && (x.AchTransactionId == transactionId
                                   || (!string.IsNullOrEmpty(trace) && x.IdTransaccion == trace)
                                   || (!string.IsNullOrEmpty(externalId) && x.IdTransaccion == externalId)
                                   || (!string.IsNullOrEmpty(reference) && x.IdTransaccion == reference)), ct);
            if (hasResponse) return false;

            // A correlated inbound link can precede the final state transition.
            var linkedInboundResponse = await _db.IncomingNachaTransactionLinks.AsNoTracking()
                .AnyAsync(x => x.AchTransactionId == transactionId, ct);
            if (linkedInboundResponse) return false;

            var hasGeneratedReturn = await _db.NachaInboundSimulationEntries.AsNoTracking()
                .AnyAsync(x => x.TransactionId == transactionId
                               && x.Simulation.ScenarioType == NachaInboundSimulationType.IncomingPrenotificationReturn
                               && x.Simulation.Status != NachaInboundSimulationStatus.Failed
                               && x.Simulation.Status != NachaInboundSimulationStatus.Blocked, ct);
            if (hasGeneratedReturn) return false;

            // A chamber-specific inbound ingestion may contain the return. Do not
            // approve while that file is still arriving, parsing or being linked.
            // APN-I11: operational dates are in the chamber's Colombia-local calendar;
            // UploadedAtUtc and CENIT ReceivedAtUtc are actual UTC instants. A UTC
            // midnight cutoff is not the end of the Colombia-local operational day.
            var inboundWindow = PrenotificationReceiptWindow.Create(
                original.AchCycle.ProcessingDate,
                recalculatedDeadline.Value,
                ClearingHouseOperationalTimeZone.Resolve(original.AchCycle));
            var inboundPending = await _db.IncomingNachaFileIngestions.AsNoTracking()
                // APN-I10: an unclassified receipt cannot yet be assigned safely to
                // another chamber. Treat it as potentially relevant until resolved.
                .AnyAsync(x => (x.ResolvedClearingHouseId == houseId
                                || (!x.ResolvedClearingHouseId.HasValue
                                    && (x.DetectedClearingHouseId == houseId
                                        || !x.DetectedClearingHouseId.HasValue)))
                               && ((x.OperationalDate.HasValue
                                    && x.OperationalDate.Value >= inboundWindow.OperationalStartDate
                                    && x.OperationalDate.Value < inboundWindow.OperationalEndExclusive)
                                   || (!x.OperationalDate.HasValue
                                       // No date in an unclassified file: use its
                                       // actual upload window, not a timeless global lock.
                                       && x.UploadedAtUtc >= inboundWindow.UtcUnclassifiedStartInclusive
                                       && x.UploadedAtUtc < inboundWindow.UtcEndExclusive))
                               && (x.IngestionStatus == IncomingNachaIngestionStatus.Recibido
                                   || x.IngestionStatus == IncomingNachaIngestionStatus.EnValidacion
                                   || x.IngestionStatus == IncomingNachaIngestionStatus.PendienteResolucion
                                   || x.IngestionStatus == IncomingNachaIngestionStatus.ListoParaParseo
                                   || x.IngestionStatus == IncomingNachaIngestionStatus.Parseado
                                   // Blocked/failed ingestions have not been reconciled as
                                   // absence of a return; they require operational review.
                                   || x.IngestionStatus == IncomingNachaIngestionStatus.Bloqueado
                                   || x.IngestionStatus == IncomingNachaIngestionStatus.Fallido
                                   || x.ProcessingResults.Any(r => r.OutcomeStatus == IncomingNachaProcessingOutcomeStatus.EnProceso)), ct);

            // APN-I10: MFT can claim the physical ACH Colombia file before an
            // IncomingNachaFileIngestion row has been created. The previous guard
            // could not see that interval. A failed/uncertain inbound transfer is
            // also not positive proof that the camera returned nothing.
            var mftInboundOutstanding = await _db.AchManagedFileTransfers.AsNoTracking()
                .AnyAsync(x => x.ClearingHouseId == houseId
                               && x.Direction == AchManagedFileDirection.Inbound
                               && x.OperationalDate >= inboundWindow.OperationalStartDate
                               && x.OperationalDate < inboundWindow.OperationalEndExclusive
                               && (x.Status == AchManagedFileTransferStatus.Received
                                   || x.Status == AchManagedFileTransferStatus.InProgress
                                   || x.Status == AchManagedFileTransferStatus.RetryPending
                                   || x.Status == AchManagedFileTransferStatus.Uncertain
                                   || x.Status == AchManagedFileTransferStatus.Failed
                                   || x.Status == AchManagedFileTransferStatus.Rejected), ct);

            // CENIT gateway responses use a different ledger from NACHA uploads.
            // A related individual rejection or unresolved response is never the
            // equivalent of an empty completed session. ACK/NoActivity are technical
            // messages, NOT proof that all participant returns were collected.
            var cenitResponseOutstanding = await _db.CenitChamberResponses.AsNoTracking()
                .AnyAsync(x => x.ClearingHouseId == houseId
                               && ((x.AchTransactionId == transactionId
                                    && x.ResponseType == CenitChamberResponseType.OperatorRejected)
                                   // A file-level NACK rejects the exported file even
                                   // when no individual transaction trace is supplied.
                                   || (x.ResponseType == CenitChamberResponseType.Nack
                                       && x.AchFileExportId.HasValue
                                       && x.AchFileExport!.Transactions.Any(t => t.AchTransactionId == transactionId))
                                   || (x.ResponseType == CenitChamberResponseType.OperatorRejected
                                       && !string.IsNullOrEmpty(trace)
                                       && x.TransactionTraceNumber == trace)
                                   || ((x.ResponseType == CenitChamberResponseType.Unknown
                                        || x.ResponseType == CenitChamberResponseType.Nack
                                        || x.ResponseType == CenitChamberResponseType.OperatorRejected)
                                       && (x.CorrelationOutcome == CenitChamberCorrelationOutcome.Pending
                                           || x.CorrelationOutcome == CenitChamberCorrelationOutcome.NotFound
                                           || x.CorrelationOutcome == CenitChamberCorrelationOutcome.Ambiguous
                                           || x.CorrelationOutcome == CenitChamberCorrelationOutcome.TransactionNotFound
                                           || x.CorrelationOutcome == CenitChamberCorrelationOutcome.TransactionAmbiguous
                                           || x.CorrelationOutcome == CenitChamberCorrelationOutcome.Invalid)
                                       // APN-I11: a still-unresolved negative CENIT response
                                       // may belong to a later *return* session, not the
                                       // original sending cycle. Do not prematurely exclude it.
                                       && x.ReceivedAtUtc >= inboundWindow.UtcOperationalStartInclusive
                                       && x.ReceivedAtUtc < inboundWindow.UtcEndExclusive)), ct);

            if (!PrenotificationTacitAcceptanceDecision.IsOperationallySafe(
                    storedDeadline, recalculatedDeadline, now, canonicalValid,
                    transmitted, inboundPending || mftInboundOutstanding || cenitResponseOutstanding,
                    hasResponse || hasGeneratedReturn || linkedInboundResponse))
                return false;

            var result = await _stateTransitionService.TransitionAsync(new AchStateTransitionRequest(
                transactionId,
                AchTransferStateEnum.AppliedTacitly,
                AchStateEventSourceEnum.System,
                PayloadJson: "{\"origin\":\"quartz-ach-tacit-acceptance\",\"deadlineAuthority\":\"recalculated-clearing-house-policy\",\"gate\":\"APN-I12\"}",
                ChangedAtUtc: now.UtcDateTime,
                IdempotencyKey: $"apn-tacit:{transactionId}",
                ClearingHouseId: houseId), ct);

            if (transaction is not null) await transaction.CommitAsync(ct);
            return result.Applied;
        }
        finally
        {
            // Never carry tracked transaction/validation entities between candidates.
            _db.ChangeTracker.Clear();
        }
    }

    private static int ParsePositiveInt(TaskDefinition task, string key, int defaultValue)
    {
        var raw = task.Parameters.FirstOrDefault(p => p.Key == key)?.Value;
        return int.TryParse(raw, out var value) && value > 0 ? value : defaultValue;
    }
}
