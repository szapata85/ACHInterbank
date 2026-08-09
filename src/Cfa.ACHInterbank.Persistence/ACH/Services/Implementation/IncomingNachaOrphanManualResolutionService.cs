using System.Data;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class IncomingNachaOrphanManualResolutionService : IIncomingNachaOrphanManualResolutionService
{
    private readonly AchDbContext _context;
    private readonly IIncomingNachaPostParseProcessor? _postParseProcessor;
    private readonly TimeProvider _timeProvider;

    public IncomingNachaOrphanManualResolutionService(
        AchDbContext context,
        IIncomingNachaPostParseProcessor? postParseProcessor = null,
        TimeProvider? timeProvider = null)
    {
        _context = context;
        _postParseProcessor = postParseProcessor;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<IncomingNachaOrphanManualResolutionResult> ResolveAsync(
        IncomingNachaOrphanManualResolutionRequest request,
        CancellationToken ct = default)
    {
        var validation = ValidateRequest(request);
        if (validation is not null)
        {
            return validation;
        }

        if (!_context.Database.IsRelational())
        {
            return await ResolveCoreAsync(request, ct);
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async cancellationToken =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
            var result = await ResolveCoreAsync(request, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }, ct);
    }

    private async Task<IncomingNachaOrphanManualResolutionResult> ResolveCoreAsync(
        IncomingNachaOrphanManualResolutionRequest request,
        CancellationToken ct)
    {
        var resolvedBy = request.ResolvedBy.Trim();
        var link = await FindLinkAsync(request, ct);
        if (link is null)
        {
            return new(false, "NotFound", null, null, "No se encontró la devolución huérfana indicada.");
        }

        if (link.IsFinal || link.LinkType == IncomingNachaLinkType.Manual)
        {
            return await ResolveReplayAsync(link, request, ct);
        }

        if (link.LinkType is not (IncomingNachaLinkType.NotFound or IncomingNachaLinkType.Ambiguous or IncomingNachaLinkType.NoResuelto))
        {
            return new(false, "InvalidState", null, null, "La devolución no está pendiente de resolución manual.");
        }

        if (request.ResolutionAction == IncomingNachaOrphanResolutionAction.KeepPending)
        {
            return new(false, "Pending", null, null, "La devolución permanece pendiente y no fue aplicada.");
        }

        var classification = await _context.IncomingNachaEntryClassifications
            .FirstOrDefaultAsync(x => x.IncomingNachaFileIngestionId == link.IncomingNachaFileIngestionId
                && x.EntryDetailId == link.EntryDetailId
                && x.AddendaRecordId == link.AddendaRecordId, ct);
        var ingestion = await _context.IncomingNachaFileIngestions
            .FirstAsync(x => x.Id == link.IncomingNachaFileIngestionId, ct);
        var entry = link.EntryDetailId.HasValue
            ? await _context.EntryDetails.FirstOrDefaultAsync(x => x.EntryDetailID == link.EntryDetailId.Value, ct)
            : null;
        var previousEvidence = ParseJson(link.EvidenceJson);
        var candidateIds = ExtractCandidateIds(previousEvidence);
        var previousReason = previousEvidence.TryGetProperty("resolutionReason", out var reasonElement)
            ? reasonElement.GetString()
            : link.LinkType.ToString();
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        IncomingNachaLinkedReturnApplicationResult? application = null;
        if (request.ResolutionAction == IncomingNachaOrphanResolutionAction.LinkToTransaction)
        {
            if (classification is null || entry is null)
            {
                return new(false, "Invalid", null, null, "La devolución no conserva clasificación y detalle suficientes para aplicarla.");
            }

            var transaction = await _context.AchTransactions
                .AsNoTracking()
                .Include(x => x.AchCycle)
                .FirstOrDefaultAsync(x => x.Id == request.ResolvedAchTransactionId, ct);
            if (transaction is null)
            {
                return new(false, "TransactionNotFound", null, null, "No existe la transacción seleccionada.");
            }

            var incompatibilities = IncomingNachaOrphanCompatibilityPolicy.Evaluate(
                ingestion, entry, classification, transaction, candidateIds);
            if (incompatibilities.Count > 0)
            {
                return new(false, "IncompatibleTransaction", null, null, string.Join(" ", incompatibilities));
            }

            var claimed = await ClaimAsync(link, request.ResolvedAchTransactionId!.Value, resolvedBy, now, ct);
            if (!claimed)
            {
                _context.ChangeTracker.Clear();
                var current = await FindLinkAsync(request, ct);
                return current is null
                    ? new(false, "NotFound", null, null, "La devolución dejó de estar disponible.")
                    : await ResolveReplayAsync(current, request, ct);
            }

            _context.ChangeTracker.Clear();
            link = await _context.IncomingNachaTransactionLinks
                .FirstAsync(x => x.Id == link.Id, ct);
            classification = await _context.IncomingNachaEntryClassifications
                .FirstAsync(x => x.IncomingNachaFileIngestionId == link.IncomingNachaFileIngestionId
                    && x.EntryDetailId == link.EntryDetailId
                    && x.AddendaRecordId == link.AddendaRecordId, ct);
            classification.RequiresManualResolution = false;
            classification.EligibilityStatus = IncomingNachaEligibilityStatus.RevisionManual;

            if (_postParseProcessor is null)
            {
                throw new InvalidOperationException("El pipeline oficial de devoluciones no está disponible.");
            }

            application = await _postParseProcessor.ApplyLinkedReturnAsync(link.Id, resolvedBy, ct);
            if (application.RequiresManualResolution)
            {
                throw new InvalidOperationException(application.Message);
            }

            classification.RequiresManualResolution = false;
            classification.EligibilityStatus = application.WasDuplicate
                ? IncomingNachaEligibilityStatus.Bloqueada
                : IncomingNachaEligibilityStatus.Elegible;
        }
        else
        {
            link.LinkType = IncomingNachaLinkType.Manual;
            link.IsFinal = true;
            link.LinkedBy = resolvedBy;
            link.LinkedAtUtc = now;
            if (classification is not null)
            {
                classification.RequiresManualResolution = false;
                classification.EligibilityStatus = IncomingNachaEligibilityStatus.RevisionManual;
            }
        }

        var eventStatus = request.ResolutionAction switch
        {
            IncomingNachaOrphanResolutionAction.MarkAsIgnored => "Ignored",
            IncomingNachaOrphanResolutionAction.MarkAsRejected => "Rejected",
            IncomingNachaOrphanResolutionAction.LinkToTransaction when application?.WasDuplicate == true => "AlreadyApplied",
            IncomingNachaOrphanResolutionAction.LinkToTransaction => "Applied",
            _ => "Resolved"
        };
        var payload = BuildEvidence(
            link,
            ingestion,
            request,
            candidateIds,
            previousReason,
            resolvedBy,
            now,
            application);
        link.EvidenceJson = JsonSerializer.Serialize(payload);

        var processingEvent = new IncomingNachaProcessingEvent
        {
            IncomingNachaFileIngestionId = link.IncomingNachaFileIngestionId,
            EntryDetailId = link.EntryDetailId,
            AddendaRecordId = link.AddendaRecordId,
            AchTransactionId = request.ResolvedAchTransactionId,
            EventType = "OrphanManualResolution",
            EventStatus = eventStatus,
            Message = request.ResolutionAction == IncomingNachaOrphanResolutionAction.LinkToTransaction
                ? "Devolución relacionada y aplicada por el pipeline oficial."
                : $"Resolución manual de devolución: {request.ResolutionAction}.",
            EvidenceJson = link.EvidenceJson,
            OccurredAtUtc = now,
            RaisedBy = resolvedBy
        };
        _context.IncomingNachaProcessingEvents.Add(processingEvent);
        await _context.SaveChangesAsync(ct);

        return new(true, eventStatus, processingEvent.Id, application?.AchTransactionStateEventId,
            application?.Message ?? "Resolución manual registrada.");
    }

    private IncomingNachaOrphanManualResolutionResult? ValidateRequest(IncomingNachaOrphanManualResolutionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ResolvedBy))
        {
            return new(false, "Invalid", null, null, "La identidad del operador es obligatoria.");
        }

        if (request.ResolutionAction == IncomingNachaOrphanResolutionAction.LinkToTransaction)
        {
            if (!request.ResolvedAchTransactionId.HasValue)
            {
                return new(false, "Invalid", null, null, "Debe seleccionar la transacción correcta.");
            }

            if (string.IsNullOrWhiteSpace(request.ResolutionReason) || request.ResolutionReason.Trim().Length < 8)
            {
                return new(false, "Invalid", null, null, "La justificación debe tener al menos 8 caracteres.");
            }
        }

        return null;
    }

    private async Task<bool> ClaimAsync(
        IncomingNachaTransactionLink link,
        int achTransactionId,
        string resolvedBy,
        DateTime now,
        CancellationToken ct)
    {
        if (!_context.Database.IsRelational())
        {
            if (link.IsFinal || link.LinkType == IncomingNachaLinkType.Manual)
            {
                return false;
            }

            link.AchTransactionId = achTransactionId;
            link.LinkType = IncomingNachaLinkType.Manual;
            link.ConfidenceScore = 1m;
            link.IsFinal = true;
            link.LinkedBy = resolvedBy;
            link.LinkedAtUtc = now;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        var affected = await _context.IncomingNachaTransactionLinks
            .Where(x => x.Id == link.Id
                && !x.IsFinal
                && (x.LinkType == IncomingNachaLinkType.NotFound
                    || x.LinkType == IncomingNachaLinkType.Ambiguous
                    || x.LinkType == IncomingNachaLinkType.NoResuelto))
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => x.AchTransactionId, achTransactionId)
                .SetProperty(x => x.LinkType, IncomingNachaLinkType.Manual)
                .SetProperty(x => x.ConfidenceScore, 1m)
                .SetProperty(x => x.IsFinal, true)
                .SetProperty(x => x.LinkedBy, resolvedBy)
                .SetProperty(x => x.LinkedAtUtc, now), ct);
        return affected == 1;
    }

    private async Task<IncomingNachaOrphanManualResolutionResult> ResolveReplayAsync(
        IncomingNachaTransactionLink link,
        IncomingNachaOrphanManualResolutionRequest request,
        CancellationToken ct)
    {
        var sameTarget = request.ResolutionAction == IncomingNachaOrphanResolutionAction.LinkToTransaction
            && link.LinkType == IncomingNachaLinkType.Manual
            && link.IsFinal
            && link.AchTransactionId == request.ResolvedAchTransactionId;
        if (!sameTarget)
        {
            return new(false, "AlreadyResolved", null, null, "La devolución ya fue resuelta con otra decisión.");
        }

        var resolutionEvent = await _context.IncomingNachaProcessingEvents.AsNoTracking()
            .Where(x => x.IncomingNachaFileIngestionId == link.IncomingNachaFileIngestionId
                && x.EntryDetailId == link.EntryDetailId
                && x.AddendaRecordId == link.AddendaRecordId
                && x.EventType == "OrphanManualResolution")
            .OrderByDescending(x => x.OccurredAtUtc)
            .FirstOrDefaultAsync(ct);
        var stateEventId = ExtractLong(ParseJson(resolutionEvent?.EvidenceJson), "achTransactionStateEventId");
        return new(true, "AlreadyApplied", resolutionEvent?.Id, stateEventId,
            "La misma relación ya había sido aplicada; no se generaron efectos adicionales.")
        {
            IsIdempotentReplay = true
        };
    }

    private async Task<IncomingNachaTransactionLink?> FindLinkAsync(
        IncomingNachaOrphanManualResolutionRequest request,
        CancellationToken ct)
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

    private static object BuildEvidence(
        IncomingNachaTransactionLink link,
        IncomingNachaFileIngestion ingestion,
        IncomingNachaOrphanManualResolutionRequest request,
        IReadOnlyCollection<int> candidateIds,
        string? previousReason,
        string resolvedBy,
        DateTime resolvedAtUtc,
        IncomingNachaLinkedReturnApplicationResult? application)
        => new
        {
            schemaVersion = 2,
            eventType = "IncomingReturnManualResolved",
            source = "IncomingNachaOrphanManualResolutionService.ResolveAsync",
            resolutionStatus = application?.WasDuplicate == true ? "AlreadyApplied" : "Resolved",
            resolutionAction = request.ResolutionAction.ToString(),
            manualReviewRequired = application?.RequiresManualResolution ?? false,
            incomingNachaTransactionLinkId = link.Id,
            incomingFileId = link.IncomingNachaFileIngestionId,
            ingestion.FileName,
            ingestion.FileHashSha256,
            clearingHouseId = ingestion.ResolvedClearingHouseId,
            achCycleId = ingestion.ResolvedAchCycleId,
            entryDetailId = link.EntryDetailId,
            addendaId = link.AddendaRecordId,
            previousResolutionReason = previousReason,
            candidateTransactionIds = candidateIds,
            resolvedAchTransactionId = request.ResolvedAchTransactionId,
            resolvedBy,
            resolvedAtUtc,
            request.Comment,
            request.ResolutionReason,
            request.CorrelationId,
            stateChanged = application?.Applied == true,
            applied = application?.Applied == true || application?.WasDuplicate == true,
            achTransactionStateEventCreated = application?.Applied == true,
            achTransactionStateEventId = application?.AchTransactionStateEventId,
            applicationStatus = application?.Status
        };

    private static JsonElement ParseJson(string? raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            using var doc = JsonDocument.Parse("{}");
            return doc.RootElement.Clone();
        }
    }

    private static int[] ExtractCandidateIds(JsonElement root)
    {
        if (!root.TryGetProperty("candidateTransactionIds", out var ids) || ids.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return ids.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.Number && x.TryGetInt32(out _))
            .Select(x => x.GetInt32())
            .Distinct()
            .ToArray();
    }

    private static long? ExtractLong(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt64(out var parsed)
                ? parsed
                : null;
}
