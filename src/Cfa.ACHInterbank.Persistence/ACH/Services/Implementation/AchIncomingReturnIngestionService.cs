using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public class AchIncomingReturnIngestionService(
    AchDbContext context,
    IAchRegulatoryCatalogService regulatoryCatalogService,
    IAchCauseCodePolicy? causeCodePolicy = null,
    IAchStateTransitionService? stateTransitionService = null) : IAchIncomingReturnIngestionService
{
    public async Task<AchIncomingReturnIngestionResult> IngestAsync(AchIncomingReturnIngestionRequest request, CancellationToken cancellationToken)
    {
        var failures = new List<AchIncomingReturnIngestionFailure>();
        var items = new List<AchIncomingReturnItem>();
        var auditRecords = new List<AchIncomingReturnAuditRecord>();
        var seenDuplicateKeys = new HashSet<string>(StringComparer.Ordinal);
        var duplicatedKeys = new HashSet<string>(StringComparer.Ordinal);
        var contentSha256 = ComputeSha256(request.RawContent ?? string.Empty);

        if (string.IsNullOrWhiteSpace(request.RawContent))
        {
            failures.Add(new("FILE_EMPTY", "El archivo entrante está vacío.", nameof(request.RawContent)));
            return await BuildResultAsync(request, 0, items, failures, auditRecords, duplicatedKeys, contentSha256, cancellationToken);
        }

        var records = ChunkRecords(request.RawContent);
        for (int i = 0; i < records.Count; i++)
        {
            var record = records[i];
            if (!(record.Length >= 30 && record.StartsWith("7") && record.Substring(1, 2) == "99"))
            {
                continue;
            }

            var recordIndex = i + 1;
            var reason = record.Substring(3, 5).Trim();
            var normalizedReason = reason.Trim().ToUpperInvariant();
            var originalTrace = record.Substring(8, 15).Trim();
            var trace = record.Length >= 106 ? record.Substring(91, 15).Trim() : null;

            if (string.IsNullOrWhiteSpace(normalizedReason))
            {
                failures.Add(new("RETURN_REASON_MISSING", "No se encontró causal de devolución.", nameof(reason), trace));
            }

            if (string.IsNullOrWhiteSpace(originalTrace))
            {
                failures.Add(new("ORIGINAL_TRACE_MISSING", "No se encontró traza original para vincular la devolución.", nameof(originalTrace), trace));
                items.Add(new(trace, null, normalizedReason, null, null, null, null, false, record));
                auditRecords.Add(BuildAuditRecord(recordIndex, record, trace, null, normalizedReason, null, null, false));
                continue;
            }

            var originalCandidates = await context.AchTransactions
                .AsNoTracking()
                .Include(t => t.AchCycle)
                .Where(t => t.TraceNumber == originalTrace || t.OriginalTraceRef == originalTrace)
                .OrderBy(t => t.Id)
                .Take(2)
                .ToListAsync(cancellationToken);

            if (originalCandidates.Count > 1)
            {
                failures.Add(new("ORIGINAL_TRANSACTION_AMBIGUOUS", "La traza original coincide con varias transacciones y requiere revisión.", nameof(originalTrace), trace));
                items.Add(new(trace, originalTrace, normalizedReason, null, null, null, null, false, record));
                auditRecords.Add(BuildAuditRecord(recordIndex, record, trace, originalTrace, normalizedReason, null, null, false));
                continue;
            }

            var originalTx = originalCandidates.SingleOrDefault();

            if (originalTx is null)
            {
                failures.Add(new("ORIGINAL_TRANSACTION_NOT_FOUND", "No se encontró la transacción original de la devolución.", nameof(originalTrace), trace));
                RegisterDuplicateFailure(seenDuplicateKeys, duplicatedKeys, failures, trace, null, null, originalTrace, normalizedReason);
                items.Add(new(trace, originalTrace, normalizedReason, null, null, null, null, false, record));
                auditRecords.Add(BuildAuditRecord(recordIndex, record, trace, originalTrace, normalizedReason, null, null, false));
                continue;
            }

            var clearingHouseId = originalTx.AchCycle?.ClearingHouseId;
            RegisterDuplicateFailure(seenDuplicateKeys, duplicatedKeys, failures, trace, originalTx.Id, clearingHouseId, originalTrace, normalizedReason);
            if (!clearingHouseId.HasValue || clearingHouseId.Value <= 0)
            {
                failures.Add(new("CLEARING_HOUSE_MISSING", "No se pudo resolver la cámara de la transacción original.", "ClearingHouseId", trace));
            }
            else if (!string.IsNullOrWhiteSpace(normalizedReason))
            {
                if (causeCodePolicy is not null)
                {
                    var clearingHouseCode = originalTx.AchCycle?.ClearingHouse?.Code;
                    if (string.IsNullOrWhiteSpace(clearingHouseCode))
                    {
                        clearingHouseCode = await context.ClearingHouses
                            .AsNoTracking()
                            .Where(x => x.Id == clearingHouseId.Value)
                            .Select(x => x.Code)
                            .FirstOrDefaultAsync(cancellationToken);
                    }

                    var causePolicyResult = await causeCodePolicy.EvaluateAsync(
                        new AchCauseCodePolicyRequest(
                            normalizedReason,
                            AchCauseCodeFlow.IncomingReturn,
                            clearingHouseId.Value,
                            clearingHouseCode,
                            originalTx.Type.ToString(),
                            originalTx.EffectiveEntryDate,
                            Source: nameof(AchIncomingReturnIngestionService)),
                        cancellationToken);

                    if (!causePolicyResult.IsAllowed || causePolicyResult.Issues.Any(x => x.Severity == AchCauseCodePolicySeverity.Error))
                    {
                        var detail = string.Join(" | ", causePolicyResult.Issues.Where(x => x.Severity == AchCauseCodePolicySeverity.Error).Select(x => $"{x.Code}: {x.Message}"));
                        failures.Add(new(
                            "INCOMING_RETURN_CAUSE_POLICY_REJECTED",
                            string.IsNullOrWhiteSpace(detail)
                                ? "La causal entrante no cumple la política rail-flow para devoluciones entrantes."
                                : $"La causal entrante no cumple la política rail-flow para devoluciones entrantes. {detail}",
                            "ReturnReasonCode",
                            trace));
                        items.Add(new(trace, originalTrace, normalizedReason, originalTx.Id, clearingHouseId, originalTx.Type.ToString(), originalTx.State.ToString(), true, record));
                        auditRecords.Add(BuildAuditRecord(recordIndex, record, trace, originalTrace, normalizedReason, originalTx.Id, clearingHouseId, true));
                        continue;
                    }
                }

                var returnCodeValidation = await regulatoryCatalogService.ValidateReturnCodeAsync(
                    clearingHouseId.Value,
                    normalizedReason,
                    originalTx.Type,
                    originalTx.EffectiveEntryDate,
                    request.ReceivedAtUtc.Date,
                    cancellationToken);

                if (!returnCodeValidation.IsAllowed)
                {
                    failures.Add(new(
                        "INCOMING_RETURN_CODE_REJECTED",
                        returnCodeValidation.Reason ?? $"La causal de devolución entrante {normalizedReason} no está permitida para la cámara de la transacción original.",
                        "ReturnReasonCode",
                        trace));
                }
                else
                {
                    var policyValidation = await regulatoryCatalogService.ValidateReturnPolicyAsync(
                        clearingHouseId.Value,
                        originalTx.Type,
                        normalizedReason,
                        originalTx.EffectiveEntryDate,
                        request.ReceivedAtUtc.Date,
                        hasAddenda: true,
                        originalTx.State.ToString(),
                        cancellationToken);

                    if (!policyValidation.IsAllowed)
                    {
                        failures.Add(new(
                            "INCOMING_RETURN_POLICY_REJECTED",
                            policyValidation.Reason ?? "La política regulatoria no permite la devolución entrante para la transacción original.",
                            "ReturnReasonCode",
                            trace));
                    }
                }
            }

            items.Add(new(trace, originalTrace, normalizedReason, originalTx.Id, clearingHouseId, originalTx.Type.ToString(), originalTx.State.ToString(), true, record));
            auditRecords.Add(BuildAuditRecord(recordIndex, record, trace, originalTrace, normalizedReason, originalTx.Id, clearingHouseId, true));
        }

        return await BuildResultAsync(request, records.Count, items, failures, auditRecords, duplicatedKeys, contentSha256, cancellationToken);
    }

    private async Task<AchIncomingReturnIngestionResult> BuildResultAsync(
        AchIncomingReturnIngestionRequest request,
        int totalRecords,
        List<AchIncomingReturnItem> items,
        List<AchIncomingReturnIngestionFailure> failures,
        List<AchIncomingReturnAuditRecord> auditRecords,
        HashSet<string> duplicatedKeys,
        string contentSha256,
        CancellationToken cancellationToken)
    {
        var parsed = items.Count;
        var linked = items.Count(x => x.IsLinked);
        var unlinked = parsed - linked;
        var decision = DetermineDecision(parsed, linked, failures);
        var isRejectedTotal = string.Equals(decision, AchIncomingReturnIngestionDecision.RejectedTotal, StringComparison.Ordinal);
        var isRejectedPartial = string.Equals(decision, AchIncomingReturnIngestionDecision.RejectedPartial, StringComparison.Ordinal);
        var updatedTransactionIds = new List<int>();
        if (!isRejectedTotal)
        {
            var failedTraces = failures.Where(x => !string.IsNullOrWhiteSpace(x.TraceNumber)).Select(x => x.TraceNumber!).ToHashSet(StringComparer.Ordinal);
            var failedOriginalTraces = failures.Where(x => x.Field == nameof(AchIncomingReturnItem.OriginalTraceNumber) || x.Field == "OriginalTraceNumber")
                .Select(x => x.TraceNumber).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).ToHashSet(StringComparer.Ordinal);
            var eligibleIds = items
                .Where(x => x.IsLinked && x.OriginalTransactionId.HasValue)
                .Where(x => x.ClearingHouseId.HasValue
                    && !string.IsNullOrWhiteSpace(x.ReturnReasonCode)
                    && !string.IsNullOrWhiteSpace(x.OriginalTraceNumber))
                .Where(x => !duplicatedKeys.Contains(BuildDuplicateKey(x.OriginalTransactionId, x.ClearingHouseId, x.OriginalTraceNumber, x.ReturnReasonCode)))
                .Where(x => (x.TraceNumber is null || !failedTraces.Contains(x.TraceNumber)) && (x.OriginalTraceNumber is null || !failedOriginalTraces.Contains(x.OriginalTraceNumber)))
                .Select(x => x.OriginalTransactionId!.Value)
                .Distinct()
                .ToList();

            if (eligibleIds.Count > 0)
            {
                var transitionPolicy = stateTransitionService ?? new AchStateTransitionService(context);
                foreach (var transactionId in eligibleIds)
                {
                    var matchedItem = items.FirstOrDefault(x => x.OriginalTransactionId == transactionId)
                        ?? throw new InvalidOperationException("No se encontró la evidencia de devolución seleccionada para aplicar.");
                    var eventClearingHouseId = matchedItem.ClearingHouseId
                        ?? throw new InvalidOperationException("La devolución elegible no conserva una cámara compensadora determinística.");
                    var originalTraceNumber = matchedItem.OriginalTraceNumber
                        ?? throw new InvalidOperationException("La devolución elegible no conserva la traza original.");
                    var returnReasonCode = matchedItem.ReturnReasonCode
                        ?? throw new InvalidOperationException("La devolución elegible no conserva el código de causal.");
                    var transactionSnapshot = await context.AchTransactions.AsNoTracking()
                        .Where(x => x.Id == transactionId)
                        .Select(x => new { x.State, x.EffectiveEntryDate })
                        .SingleAsync(cancellationToken);
                    var payload = $$"""
{
  "schemaVersion": 1,
  "eventType": "IncomingReturnApplied",
  "source": "AchIncomingReturnIngestionService.IngestAsync",
  "generationMode": "incoming-return",
  "achTransactionId": {{transactionId}},
  "previousState": "{{transactionSnapshot.State}}",
  "newState": "{{AchTransferStateEnum.ReturnedByEpr}}",
  "returnReasonCode": "{{matchedItem?.ReturnReasonCode ?? string.Empty}}",
  "originalTraceNumber": "{{matchedItem?.OriginalTraceNumber ?? string.Empty}}",
  "fileName": "{{request.FileName}}",
  "effectiveEntryDate": "{{transactionSnapshot.EffectiveEntryDate:yyyy-MM-dd}}",
  "appliedAtUtc": "{{request.ReceivedAtUtc:O}}",
  "stateChanged": {{(transactionSnapshot.State != AchTransferStateEnum.ReturnedByEpr).ToString().ToLowerInvariant()}}
}
""";
                    var semanticKey = AchIncomingEventIdentityPolicy.BuildReturnKey(
                        eventClearingHouseId,
                        transactionId,
                        originalTraceNumber,
                        returnReasonCode);
                    var transition = await transitionPolicy.TransitionAsync(new AchStateTransitionRequest(
                        transactionId,
                        AchTransferStateEnum.ReturnedByEpr,
                        AchStateEventSourceEnum.Epr,
                        returnReasonCode,
                        payload,
                        originalTraceNumber,
                        request.ReceivedAtUtc,
                        semanticKey,
                        eventClearingHouseId), cancellationToken);
                    if (transition.Applied)
                    {
                        updatedTransactionIds.Add(transactionId);
                    }
                }
            }
        }

        var audit = new AchIncomingReturnIngestionAudit(
            request.FileName,
            request.ReceivedAtUtc,
            request.Source,
            request.UploadedBy,
            request.RawContent?.Length ?? 0,
            totalRecords,
            parsed,
            linked,
            unlinked,
            failures.Count,
            updatedTransactionIds.Count,
            decision,
            contentSha256,
            auditRecords,
            failures.Select(x => new AchIncomingReturnAuditFailure(x.Code, x.Message, x.Field, x.TraceNumber, null)).ToList());

        return new(failures.Count == 0, decision, isRejectedTotal, isRejectedPartial, totalRecords, parsed, linked, unlinked, updatedTransactionIds.Count, updatedTransactionIds, items, failures, audit);
    }


    private static string DetermineDecision(int parsedReturnCount, int linkedReturnCount, IReadOnlyCollection<AchIncomingReturnIngestionFailure> failures)
    {
        if (failures.Count == 0)
        {
            return AchIncomingReturnIngestionDecision.Accepted;
        }

        if (failures.Any(x => x.Code == "FILE_EMPTY"))
        {
            return AchIncomingReturnIngestionDecision.RejectedTotal;
        }

        if (parsedReturnCount == 0)
        {
            return AchIncomingReturnIngestionDecision.RejectedTotal;
        }

        if (linkedReturnCount == 0)
        {
            return AchIncomingReturnIngestionDecision.RejectedTotal;
        }

        var linkedRegulatoryFailures = failures.Count(x => x.Code is "INCOMING_RETURN_CODE_REJECTED" or "INCOMING_RETURN_POLICY_REJECTED");
        if (linkedRegulatoryFailures >= linkedReturnCount)
        {
            return AchIncomingReturnIngestionDecision.RejectedTotal;
        }

        return AchIncomingReturnIngestionDecision.RejectedPartial;
    }

    private static AchIncomingReturnAuditRecord BuildAuditRecord(int recordIndex, string rawRecord, string? trace, string? originalTrace, string? reason, int? originalTransactionId, int? clearingHouseId, bool isLinked)
    {
        return new(
            recordIndex,
            rawRecord.Length > 0 ? rawRecord[0].ToString() : string.Empty,
            trace,
            originalTrace,
            reason,
            originalTransactionId,
            clearingHouseId,
            isLinked,
            ComputeSha256(rawRecord),
            BuildPreview(rawRecord));
    }

    private static string BuildPreview(string rawRecord)
    {
        if (string.IsNullOrEmpty(rawRecord)) return "***";
        if (rawRecord.Length < 20) return "***";
        return $"{rawRecord[..8]}...{rawRecord[^8..]}";
    }

    private static List<string> ChunkRecords(string rawContent)
    {
        var clean = rawContent.Replace("\r", string.Empty).Replace("\n", string.Empty);
        var records = new List<string>();
        for (int i = 0; i + 106 <= clean.Length; i += 106)
        {
            records.Add(clean.Substring(i, 106));
        }
        return records;
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static void RegisterDuplicateFailure(
        HashSet<string> seenDuplicateKeys,
        HashSet<string> duplicatedKeys,
        List<AchIncomingReturnIngestionFailure> failures,
        string? trace,
        int? originalTransactionId,
        int? clearingHouseId,
        string originalTrace,
        string normalizedReason)
    {
        if (string.IsNullOrWhiteSpace(normalizedReason))
        {
            return;
        }

        var duplicateKey = BuildDuplicateKey(originalTransactionId, clearingHouseId, originalTrace, normalizedReason);

        if (!seenDuplicateKeys.Add(duplicateKey))
        {
            duplicatedKeys.Add(duplicateKey);
            failures.Add(new(
                "INCOMING_RETURN_DUPLICATE_IN_FILE",
                "La devolución entrante está duplicada dentro del mismo archivo.",
                "OriginalTraceNumber",
                trace));
        }
    }

    private static string BuildDuplicateKey(int? originalTransactionId, int? clearingHouseId, string? originalTrace, string? normalizedReason)
    {
        return originalTransactionId.HasValue
            ? $"{clearingHouseId?.ToString() ?? "null"}|tx:{originalTransactionId.Value}|rr:{normalizedReason}"
            : $"{clearingHouseId?.ToString() ?? "null"}|ot:{originalTrace}|rr:{normalizedReason}";
    }
}
