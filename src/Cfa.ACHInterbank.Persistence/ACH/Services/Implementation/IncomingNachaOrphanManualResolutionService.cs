using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class IncomingNachaOrphanManualResolutionService : IIncomingNachaOrphanManualResolutionService
{
    private readonly AchDbContext _context;

    public IncomingNachaOrphanManualResolutionService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IncomingNachaOrphanManualResolutionResult> ResolveAsync(IncomingNachaOrphanManualResolutionRequest request, CancellationToken ct = default)
    {
        var resolvedBy = request.ResolvedBy?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(resolvedBy))
        {
            return new(false, "Invalid", null, null, "ResolvedBy es requerido.");
        }

        var link = await FindLinkAsync(request, ct);
        if (link is null)
        {
            return new(false, "NotFound", null, null, "No se encontró link huérfano/no resuelto.");
        }

        if (link.LinkType is not (IncomingNachaLinkType.NotFound or IncomingNachaLinkType.Ambiguous or IncomingNachaLinkType.NoResuelto))
        {
            return new(false, "InvalidState", null, null, "El link no está en estado de resolución manual.");
        }

        var alreadyResolved = await _context.IncomingNachaProcessingEvents.AsNoTracking().AnyAsync(x =>
            x.IncomingNachaFileIngestionId == link.IncomingNachaFileIngestionId
            && x.EntryDetailId == link.EntryDetailId
            && x.AddendaRecordId == link.AddendaRecordId
            && x.EventType == "OrphanManualResolution"
            && (x.EventStatus == "Resolved" || x.EventStatus == "Ignored" || x.EventStatus == "Linked"), ct);

        if (alreadyResolved)
        {
            return new(false, "AlreadyResolved", null, null, "La huérfana ya fue resuelta manualmente.");
        }

        var classification = await _context.IncomingNachaEntryClassifications.FirstOrDefaultAsync(x =>
            x.IncomingNachaFileIngestionId == link.IncomingNachaFileIngestionId
            && x.EntryDetailId == link.EntryDetailId
            && x.AddendaRecordId == link.AddendaRecordId, ct);

        if (classification is not null)
        {
            classification.RequiresManualResolution = false;
            classification.EligibilityStatus = IncomingNachaEligibilityStatus.RevisionManual;
        }

        var ingestion = await _context.IncomingNachaFileIngestions.AsNoTracking().FirstAsync(x => x.Id == link.IncomingNachaFileIngestionId, ct);
        var previousEvidence = ParseJson(link.EvidenceJson);
        var previousReason = previousEvidence.TryGetProperty("resolutionReason", out var rr) ? rr.GetString() : link.LinkType.ToString();
        var candidateIds = ExtractCandidateIds(previousEvidence);
        var now = DateTime.UtcNow;
        var action = request.ResolutionAction.ToString();
        var eventStatus = request.ResolutionAction switch
        {
            IncomingNachaOrphanResolutionAction.MarkAsIgnored => "Ignored",
            IncomingNachaOrphanResolutionAction.MarkAsRejected => "Resolved",
            IncomingNachaOrphanResolutionAction.LinkToTransaction => "Linked",
            _ => "Resolved"
        };

        var payload = new
        {
            schemaVersion = 1,
            eventType = "IncomingReturnManualResolved",
            source = "IncomingNachaOrphanManualResolutionService.ResolveAsync",
            resolutionStatus = "ResolvedOperationally",
            resolutionAction = action,
            manualReviewRequired = false,
            incomingNachaTransactionLinkId = link.Id,
            incomingFileId = link.IncomingNachaFileIngestionId,
            ingestion.FileName,
            ingestion.FileHashSha256,
            clearingHouseId = ingestion.ResolvedClearingHouseId,
            achCycleId = ingestion.ResolvedAchCycleId,
            entryDetailId = link.EntryDetailId,
            addendaId = link.AddendaRecordId,
            returnReasonCode = previousEvidence.TryGetProperty("returnReasonCode", out var rc) ? rc.GetString() : null,
            originalTraceNumber = previousEvidence.TryGetProperty("originalTraceNumber", out var ot) ? ot.GetString() : null,
            previousResolutionReason = previousReason,
            candidateTransactionIds = candidateIds,
            resolvedAchTransactionId = request.ResolvedAchTransactionId,
            resolvedBy,
            resolvedAtUtc = now,
            comment = request.Comment,
            resolutionReason = request.ResolutionReason,
            correlationId = request.CorrelationId,
            stateChanged = false,
            applied = false,
            achTransactionStateEventCreated = false
        };

        link.EvidenceJson = JsonSerializer.Serialize(payload);
        link.LinkedBy = resolvedBy;
        link.LinkedAtUtc = now;

        var ev = new IncomingNachaProcessingEvent
        {
            IncomingNachaFileIngestionId = link.IncomingNachaFileIngestionId,
            EntryDetailId = link.EntryDetailId,
            AddendaRecordId = link.AddendaRecordId,
            AchTransactionId = request.ResolvedAchTransactionId,
            EventType = "OrphanManualResolution",
            EventStatus = eventStatus,
            Message = $"Resolución manual incoming/orphan action={action}",
            EvidenceJson = JsonSerializer.Serialize(payload),
            OccurredAtUtc = now,
            RaisedBy = resolvedBy
        };

        _context.IncomingNachaProcessingEvents.Add(ev);
        await _context.SaveChangesAsync(ct);

        return new(true, eventStatus, ev.Id, null, "Resolución manual registrada.");
    }

    private async Task<IncomingNachaTransactionLink?> FindLinkAsync(IncomingNachaOrphanManualResolutionRequest request, CancellationToken ct)
    {
        if (request.IncomingNachaTransactionLinkId.HasValue)
        {
            return await _context.IncomingNachaTransactionLinks
                .FirstOrDefaultAsync(x => x.Id == request.IncomingNachaTransactionLinkId.Value, ct);
        }

        if (request.IncomingNachaFileIngestionId.HasValue && request.EntryDetailId.HasValue)
        {
            return await _context.IncomingNachaTransactionLinks
                .FirstOrDefaultAsync(x => x.IncomingNachaFileIngestionId == request.IncomingNachaFileIngestionId.Value
                    && x.EntryDetailId == request.EntryDetailId
                    && x.AddendaRecordId == request.AddendaRecordId, ct);
        }

        return null;
    }

    private static JsonElement ParseJson(string? raw)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
        return doc.RootElement.Clone();
    }

    private static int[] ExtractCandidateIds(JsonElement root)
    {
        if (!root.TryGetProperty("candidateTransactionIds", out var ids) || ids.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return ids.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Number && x.TryGetInt32(out _)).Select(x => x.GetInt32()).Distinct().ToArray();
    }
}
