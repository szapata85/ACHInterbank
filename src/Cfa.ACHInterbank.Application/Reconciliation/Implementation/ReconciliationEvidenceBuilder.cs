using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Application.Reconciliation.Interfaces;
using Cfa.ACHInterbank.Application.Reconciliation.Models;

namespace Cfa.ACHInterbank.Application.Reconciliation.Implementation;

public sealed class ReconciliationEvidenceBuilder : IReconciliationEvidenceBuilder
{
    public ReconciliationEvidenceResult Build(
        ReconciliationEvidenceRequest request,
        IEnumerable<ReconciliationEvidenceItem> items,
        IEnumerable<ReconciliationEvidenceAttachment> attachments,
        IEnumerable<ReconciliationEvidenceDifferenceLink> differenceLinks,
        IEnumerable<ReconciliationEvidenceReview> reviews)
    {
        var itemsList = items.ToList();
        var attachmentsList = attachments.ToList();
        var diffsList = differenceLinks.ToList();
        var reviewsList = reviews.ToList();

        var scope = new ReconciliationEvidenceScope
        {
            DateFrom = request.DateFrom,
            DateTo = request.DateTo,
            ClearingHouseId = request.ClearingHouseId,
            ClearingHouseCode = request.ClearingHouseCode,
            CycleId = request.CycleId,
            CycleName = request.CycleName,
            FileId = request.FileId,
            FileName = request.FileName,
            FileHash = request.FileHash,
            TransactionId = request.TransactionId,
            Status = request.Status,
            CauseCode = request.CauseCode,
            RequestedBy = request.RequestedBy,
            CorrelationId = request.CorrelationId
        };

        var generatedAt = DateTimeOffset.UtcNow;
        var idem = BuildIdempotencyKey(scope, request.EvidenceType, itemsList, generatedAt);

        return new ReconciliationEvidenceResult
        {
            EvidenceSetId = Guid.NewGuid(),
            GeneratedAt = generatedAt,
            GeneratedBy = string.IsNullOrWhiteSpace(request.RequestedBy) ? "system" : request.RequestedBy,
            Scope = scope,
            Items = itemsList,
            Attachments = attachmentsList,
            DifferenceLinks = diffsList,
            Reviews = reviewsList,
            BoundaryFlags = ReconciliationEvidenceBoundaryFlags.Default,
            IdempotencyKey = idem,
            Warnings = BuildWarnings(request, itemsList, attachmentsList, diffsList)
        };
    }

    private static ReconciliationEvidenceIdempotencyKey BuildIdempotencyKey(ReconciliationEvidenceScope scope, ReconciliationEvidenceType? evidenceType, IReadOnlyList<ReconciliationEvidenceItem> items, DateTimeOffset generatedAt)
    {
        var sourceRef = string.Join("|", items.OrderBy(x => x.EvidenceItemId).Select(x => $"{x.EvidenceItemId}:{x.TransactionId}:{x.FileHash}:{x.ThirdPartyReference}"));
        var scopeRaw = $"{scope.DateFrom:O}|{scope.DateTo:O}|{scope.ClearingHouseId}|{scope.ClearingHouseCode}|{scope.CycleId}|{scope.FileId}|{scope.FileHash}|{scope.TransactionId}|{scope.Status}|{scope.CauseCode}|{scope.CorrelationId}";

        var scopeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(scopeRaw)));
        var fullRaw = $"{scopeHash}|{evidenceType?.ToString() ?? "Any"}|{sourceRef}";
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fullRaw)));

        return new ReconciliationEvidenceIdempotencyKey
        {
            Key = key,
            ScopeHash = scopeHash,
            EvidenceType = evidenceType?.ToString() ?? "Any",
            SourceReference = sourceRef,
            GeneratedAt = generatedAt
        };
    }

    private static IReadOnlyList<string> BuildWarnings(ReconciliationEvidenceRequest request, IReadOnlyList<ReconciliationEvidenceItem> items, IReadOnlyList<ReconciliationEvidenceAttachment> attachments, IReadOnlyList<ReconciliationEvidenceDifferenceLink> diffs)
    {
        var warnings = new List<string>();
        if (!request.IncludeCudEvidence) warnings.Add("CUD evidence excluded by request.");
        if (items.Any(x => x.IsCudEvidence && !attachments.Any(a => a.EvidenceItemId == x.EvidenceItemId))) warnings.Add("Some CUD evidence items have no attachment metadata.");
        if (diffs.Any(x => x.Severity == ReconciliationEvidenceSeverity.Critical)) warnings.Add("Critical reconciliation differences detected.");
        if (items.Any(x => !attachments.Any(a => a.EvidenceItemId == x.EvidenceItemId))) warnings.Add("Some evidence items have no attachments.");
        if (items.Any(x => x.IsManualAuditOnly)) warnings.Add("Manual audit-only evidence present.");
        if (items.Any(x => x.IsOrphan)) warnings.Add("Orphan evidence present.");
        return warnings;
    }
}
