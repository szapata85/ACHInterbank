using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class IncomingNachaPostParseProcessor : IIncomingNachaPostParseProcessor
{
    private readonly AchDbContext _context;
    private readonly IIncomingNachaFunctionalClassifier _classifier;
    private readonly IIncomingNachaTransactionLinker _linker;
    private readonly IIncomingNachaPrenotificationResolver _prenotificationResolver;
    private readonly IIncomingNachaDispatchPlanner _dispatchPlanner;
    private readonly IAchRegulatoryCatalogService _regulatoryCatalogService;
    private readonly IAchStateTransitionService _stateTransitionService;
    private readonly IIncomingNachaLocalLivePreparationService? _localLivePreparationService;

    public IncomingNachaPostParseProcessor(
        AchDbContext context,
        IIncomingNachaFunctionalClassifier classifier,
        IIncomingNachaTransactionLinker linker,
        IIncomingNachaPrenotificationResolver prenotificationResolver,
        IIncomingNachaDispatchPlanner dispatchPlanner,
        IAchRegulatoryCatalogService regulatoryCatalogService,
        IAchStateTransitionService stateTransitionService,
        IIncomingNachaLocalLivePreparationService? localLivePreparationService = null)
    {
        _context = context;
        _classifier = classifier;
        _linker = linker;
        _prenotificationResolver = prenotificationResolver;
        _dispatchPlanner = dispatchPlanner;
        _regulatoryCatalogService = regulatoryCatalogService;
        _stateTransitionService = stateTransitionService;
        _localLivePreparationService = localLivePreparationService;
    }

    public async Task ProcessAsync(Guid ingestionId, string executedBy, CancellationToken ct = default)
    {
        var headers = await _context.NachaHeaders
            .Where(h => h.IncomingNachaFileIngestionId == ingestionId)
            .Select(h => h.NachaID)
            .ToListAsync(ct);

        if (headers.Count == 0)
        {
            await AddEventAsync(ingestionId, null, null, null, "ClasificacionBloqueada", "Bloqueado", "No se encontraron headers asociados a la ingesta para clasificación funcional.", new { ingestionId }, executedBy, ct);
            return;
        }

        var entries = await _context.EntryDetails
            .Where(e => e.NachaID != null && headers.Contains(e.NachaID))
            .OrderBy(e => e.EntryDetailID)
            .ToListAsync(ct);

        var addendas = await _context.AddendaRecords
            .Where(a => a.NachaID != null && headers.Contains(a.NachaID))
            .OrderBy(a => a.AddendaID)
            .ToListAsync(ct);

        foreach (var entry in entries)
        {
            var relatedAddenda = addendas.FirstOrDefault(a => IsAddendaForEntry(entry, a));
            var classification = _classifier.Classify(entry, relatedAddenda);

            var classificationRow = await _context.IncomingNachaEntryClassifications
                .FirstOrDefaultAsync(x => x.IncomingNachaFileIngestionId == ingestionId
                                          && x.EntryDetailId == entry.EntryDetailID
                                          && x.AddendaRecordId == (relatedAddenda != null ? relatedAddenda.AddendaID : null), ct);

            if (classificationRow is null)
            {
                classificationRow = new IncomingNachaEntryClassification
                {
                    IncomingNachaFileIngestionId = ingestionId,
                    EntryDetailId = entry.EntryDetailID,
                    AddendaRecordId = relatedAddenda?.AddendaID
                };
                _context.IncomingNachaEntryClassifications.Add(classificationRow);
            }

            classificationRow.FunctionalClass = classification.FunctionalClass;
            classificationRow.EligibilityStatus = classification.EligibilityStatus;
            classificationRow.RequiresLink = classification.RequiresLink;
            classificationRow.RequiresManualResolution = classification.RequiresManualResolution;
            classificationRow.OriginalTraceRef = classification.OriginalTraceRef;
            classificationRow.ReturnReasonCode = classification.ReturnReasonCode;
            classificationRow.PrenoteStatus = classification.PrenoteStatus;
            classificationRow.BusinessMeaning = classification.BusinessMeaning;
            classificationRow.ClassifierVersion = classification.ClassifierVersion;
            classificationRow.ClassificationEvidenceJson = classification.ClassificationEvidenceJson;

            await AddEventAsync(ingestionId, entry.EntryDetailID, relatedAddenda?.AddendaID, null,
                "ClasificacionGenerada", "Ok", classification.BusinessMeaning,
                new { classification.FunctionalClass, classification.EligibilityStatus, classification.ClassifierVersion }, executedBy, ct);

            if (!classification.RequiresLink)
            {
                continue;
            }

            var ingestion = await _context.IncomingNachaFileIngestions
                .AsNoTracking()
                .FirstAsync(x => x.Id == ingestionId, ct);

            if (_localLivePreparationService is not null)
            {
                await _localLivePreparationService.EnsureAsync(ingestion, entry, classification.FunctionalClass, ct);
            }

            var linkResult = await _linker.LinkAsync(
                entry,
                relatedAddenda,
                new IncomingNachaLinkingContext
                {
                    IncomingNachaFileIngestionId = ingestionId,
                    FunctionalClass = classification.FunctionalClass,
                    OperationalDate = ingestion.OperationalDate,
                    ResolvedAchCycleId = ingestion.ResolvedAchCycleId,
                    ResolvedClearingHouseId = ingestion.ResolvedClearingHouseId
                },
                ct);
            var link = await _context.IncomingNachaTransactionLinks
                .FirstOrDefaultAsync(x => x.IncomingNachaFileIngestionId == ingestionId
                                          && x.EntryDetailId == entry.EntryDetailID
                                          && x.AddendaRecordId == (relatedAddenda != null ? relatedAddenda.AddendaID : null), ct);

            if (link is null)
            {
                link = new IncomingNachaTransactionLink
                {
                    IncomingNachaFileIngestionId = ingestionId,
                    EntryDetailId = entry.EntryDetailID,
                    AddendaRecordId = relatedAddenda?.AddendaID,
                    LinkedBy = executedBy,
                    LinkedAtUtc = DateTime.UtcNow
                };
                _context.IncomingNachaTransactionLinks.Add(link);
            }

            link.LinkType = linkResult.LinkType;
            link.AchTransactionId = linkResult.AchTransactionId;
            link.ConfidenceScore = linkResult.ConfidenceScore;
            link.EvidenceJson = linkResult.EvidenceJson;
            link.IsFinal = linkResult.IsFinal;
            link.LinkedAtUtc = DateTime.UtcNow;
            link.LinkedBy = executedBy;

            if (linkResult.IsAmbiguous || linkResult.IsNotFound)
            {
                var unresolvedEvidence = BuildUnresolvedIncomingReturnEvidence(
                    ingestion,
                    entry,
                    relatedAddenda,
                    classification,
                    linkResult,
                    executedBy);
                classificationRow.EligibilityStatus = IncomingNachaEligibilityStatus.Bloqueada;
                classificationRow.RequiresManualResolution = true;
                link.EvidenceJson = JsonSerializer.Serialize(unresolvedEvidence);
                var addendaId = relatedAddenda != null ? relatedAddenda.AddendaID : (int?)null;
                var alreadyExists = await _context.IncomingNachaProcessingEvents.AnyAsync(x =>
                    x.IncomingNachaFileIngestionId == ingestionId
                    && x.EntryDetailId == entry.EntryDetailID
                    && x.AddendaRecordId == addendaId
                    && x.EventType == "LinkingBloqueado"
                    && x.EventStatus == (linkResult.IsAmbiguous ? "Ambiguo" : "NoEncontrado"), ct);
                if (!alreadyExists)
                {
                    await AddEventAsync(ingestionId, entry.EntryDetailID, relatedAddenda?.AddendaID, null,
                        "LinkingBloqueado", linkResult.IsAmbiguous ? "Ambiguo" : "NoEncontrado",
                        "Linking no determinístico. Se bloquea avance automático.",
                        unresolvedEvidence, executedBy, ct);
                }
                continue;
            }

            await AddEventAsync(ingestionId, entry.EntryDetailID, relatedAddenda?.AddendaID, linkResult.AchTransactionId,
                "LinkingExitoso", "Ok", "Linking determinístico completado.",
                new { linkResult.LinkType, linkResult.ConfidenceScore }, executedBy, ct);

            await ApplyBusinessEffectsAsync(ingestion, entry, relatedAddenda, classificationRow, linkResult, executedBy, ct);
        }

        await _context.SaveChangesAsync(ct);
        await _dispatchPlanner.PlanForIngestionAsync(ingestionId, executedBy, ct);
    }

    private static object BuildUnresolvedIncomingReturnEvidence(
        IncomingNachaFileIngestion ingestion,
        EntryDetail entry,
        AddendaRecord? addenda,
        IncomingNachaClassificationResult classification,
        IncomingNachaLinkingResult linkResult,
        string executedBy)
    {
        var candidateIds = ExtractCandidateIds(linkResult.EvidenceJson);
        var isAmbiguous = linkResult.IsAmbiguous || linkResult.LinkType == IncomingNachaLinkType.Ambiguous;
        var resolutionReason = isAmbiguous ? "Ambiguous" : "NotFound";
        var originalTrace = (classification.OriginalTraceRef ?? addenda?.OriginalTraceNumber ?? string.Empty).Trim();
        return new
        {
            schemaVersion = 1,
            eventType = "IncomingReturnUnresolved",
            resolutionStatus = "Unresolved",
            resolutionReason,
            manualReviewRequired = true,
            source = "IncomingNachaPostParseProcessor.ProcessAsync",
            linkSource = "IncomingNachaTransactionLinker.LinkAsync",
            incomingFileId = ingestion.Id,
            fileName = ingestion.FileName,
            fileHashSha256 = ingestion.FileHashSha256,
            fileSize = ingestion.FileSize,
            uploadedBy = ingestion.UploadedBy,
            receivedAtUtc = ingestion.ReceivedAtUtc,
            clearingHouseId = ingestion.ResolvedClearingHouseId,
            achCycleId = ingestion.ResolvedAchCycleId,
            operationalDate = ingestion.OperationalDate?.ToString("yyyy-MM-dd"),
            functionalClass = classification.FunctionalClass.ToString(),
            returnReasonCode = classification.ReturnReasonCode,
            originalTraceNumber = originalTrace,
            entrySequenceNumber = entry.SequenceNumber,
            entryDetailId = entry.EntryDetailID,
            addendaId = addenda?.AddendaID,
            addendaType = addenda?.CodeTypeAddendumRecord,
            linkType = linkResult.LinkType.ToString(),
            candidateCount = candidateIds.Count,
            candidateTransactionIds = candidateIds,
            stateChanged = false,
            applied = false,
            requestedBy = string.IsNullOrWhiteSpace(executedBy) ? "sistema" : executedBy,
            createdAtUtc = DateTime.UtcNow
        };
    }

    private static List<int> ExtractCandidateIds(string? evidenceJson)
    {
        if (string.IsNullOrWhiteSpace(evidenceJson))
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(evidenceJson);
            if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return candidates.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.Number && x.TryGetInt32(out _))
                .Select(x => x.GetInt32())
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private async Task ApplyBusinessEffectsAsync(
        IncomingNachaFileIngestion ingestion,
        EntryDetail entry,
        AddendaRecord? addenda,
        IncomingNachaEntryClassification classification,
        IncomingNachaLinkingResult link,
        string executedBy,
        CancellationToken ct)
    {
        if (!link.AchTransactionId.HasValue)
        {
            return;
        }

        if (classification.FunctionalClass == IncomingNachaFunctionalClass.Prenotificacion)
        {
            var prenoteResolution = await _prenotificationResolver.ResolveAsync(
                ingestion.Id,
                entry,
                link.AchTransactionId,
                ingestion.ResolvedClearingHouseId,
                ingestion.OperationalDate,
                executedBy,
                ct);

            classification.PrenoteStatus = prenoteResolution.PrenoteStatus;
            classification.RequiresManualResolution = prenoteResolution.RequiresManualReview;
            classification.EligibilityStatus = prenoteResolution.RequiresManualReview
                ? IncomingNachaEligibilityStatus.RevisionManual
                : classification.EligibilityStatus;

            await AddEventAsync(ingestion.Id, entry.EntryDetailID, addenda?.AddendaID, link.AchTransactionId,
                prenoteResolution.Applied ? "PrenotificacionAplicada" : "PrenotificacionRequiereRevision",
                prenoteResolution.RequiresManualReview ? "RevisionManual" : "Ok",
                prenoteResolution.Message,
                new { prenoteResolution.EvidenceJson }, executedBy, ct);
        }

        if (classification.FunctionalClass is IncomingNachaFunctionalClass.Devolucion or IncomingNachaFunctionalClass.RechazadaOperador or IncomingNachaFunctionalClass.RetornoEpr)
        {
            var route = await ResolveReturnRouteAsync(
                ingestion.ResolvedClearingHouseId ?? 0,
                classification.ReturnReasonCode,
                classification.FunctionalClass,
                ct);
            if (!route.IsTransitionAllowed)
            {
                classification.EligibilityStatus = IncomingNachaEligibilityStatus.RevisionManual;
                classification.RequiresManualResolution = true;
                await AddEventAsync(ingestion.Id, entry.EntryDetailID, addenda?.AddendaID, link.AchTransactionId,
                    "TransicionBloqueada", "Bloqueado", route.Reason,
                    new { classification.ReturnReasonCode, route.Reason, route.Source }, executedBy, ct);
                return;
            }

            await _stateTransitionService.TransitionAsync(
                link.AchTransactionId.Value,
                route.TargetState,
                route.Source,
                classification.ReturnReasonCode,
                classification.ClassificationEvidenceJson,
                classification.OriginalTraceRef,
                DateTime.UtcNow,
                ct);

            await AddEventAsync(ingestion.Id, entry.EntryDetailID, addenda?.AddendaID, link.AchTransactionId,
                "TransicionDisparada", "Ok", "Transición de estado aplicada para devolución entrante.",
                new { route.TargetState, route.Source, classification.ReturnReasonCode, classification.OriginalTraceRef, route.Reason }, executedBy, ct);
        }
    }

    private static bool IsAddendaForEntry(EntryDetail entry, AddendaRecord addenda)
    {
        if (!string.Equals(entry.NachaID, addenda.NachaID, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var entrySuffix = GetEntrySequenceSuffix(entry.SequenceNumber);
        var addendaSuffix = GetEntrySequenceSuffix(addenda.EntryDetailSequenceNumber);
        return !string.IsNullOrWhiteSpace(entrySuffix)
               && !string.IsNullOrWhiteSpace(addendaSuffix)
               && string.Equals(entrySuffix, addendaSuffix, StringComparison.Ordinal);
    }

    private static string? GetEntrySequenceSuffix(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Trim().Where(char.IsDigit).ToArray());
        if (digits.Length < 7)
        {
            return null;
        }

        return digits[^7..];
    }

    private async Task AddEventAsync(
        Guid ingestionId,
        int? entryDetailId,
        int? addendaId,
        int? achTransactionId,
        string eventType,
        string status,
        string message,
        object evidence,
        string raisedBy,
        CancellationToken ct)
    {
        _context.IncomingNachaProcessingEvents.Add(new IncomingNachaProcessingEvent
        {
            IncomingNachaFileIngestionId = ingestionId,
            EntryDetailId = entryDetailId,
            AddendaRecordId = addendaId,
            AchTransactionId = achTransactionId,
            EventType = eventType,
            EventStatus = status,
            Message = message,
            EvidenceJson = JsonSerializer.Serialize(evidence),
            OccurredAtUtc = DateTime.UtcNow,
            RaisedBy = string.IsNullOrWhiteSpace(raisedBy) ? "sistema" : raisedBy
        });

        await Task.CompletedTask;
    }

    private async Task<(bool IsTransitionAllowed, AchTransferStateEnum TargetState, AchStateEventSourceEnum Source, string Reason)> ResolveReturnRouteAsync(
        int clearingHouseId,
        string? returnReasonCode,
        IncomingNachaFunctionalClass functionalClass,
        CancellationToken ct)
    {
        var code = (returnReasonCode ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
        {
            return (false, AchTransferStateEnum.Pending, AchStateEventSourceEnum.System, "Causal vacía: requiere revisión manual.");
        }

        var returnCodes = await _regulatoryCatalogService.GetReturnCodesAsync(clearingHouseId, ct);
        var model = returnCodes.FirstOrDefault(x =>
            x.IsActive
            && string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
        if (model is null)
        {
            return (false, AchTransferStateEnum.Pending, AchStateEventSourceEnum.System,
                clearingHouseId <= 0
                    ? $"No fue posible resolver la cámara para validar la causal {code}."
                    : $"Causal {code} no existe en el catálogo regulatorio de la cámara {clearingHouseId}.");
        }

        var regulatorySource = (model.RegulatorySource ?? string.Empty).Trim().ToUpperInvariant();
        var isOperator = code.StartsWith("DEV", StringComparison.OrdinalIgnoreCase)
                         || regulatorySource.Contains("OPER", StringComparison.OrdinalIgnoreCase)
                         || regulatorySource.Contains("ACH", StringComparison.OrdinalIgnoreCase);
        var isEpr = code.StartsWith("R", StringComparison.OrdinalIgnoreCase)
                    || regulatorySource.Contains("EPR", StringComparison.OrdinalIgnoreCase)
                    || regulatorySource.Contains("CENIT", StringComparison.OrdinalIgnoreCase);

        if (!model.AppliesToReturn)
        {
            return (false, AchTransferStateEnum.Pending, AchStateEventSourceEnum.System, $"Causal {code} no aplica a devolución entrante.");
        }

        if (isOperator && isEpr)
        {
            return (false, AchTransferStateEnum.Pending, AchStateEventSourceEnum.System, $"Causal {code} ambigua entre operador y EPR.");
        }

        if (isOperator)
        {
            if (functionalClass == IncomingNachaFunctionalClass.RetornoEpr)
            {
                return (false, AchTransferStateEnum.Pending, AchStateEventSourceEnum.System, $"Causal {code} clasificada como operador, pero la clasificación funcional viene como RetornoEpr.");
            }
            return (true, AchTransferStateEnum.ReturnedByOperator, AchStateEventSourceEnum.Operator, $"Causal {code} mapeada como rechazo operador.");
        }

        if (isEpr)
        {
            if (functionalClass == IncomingNachaFunctionalClass.RechazadaOperador)
            {
                return (false, AchTransferStateEnum.Pending, AchStateEventSourceEnum.System, $"Causal {code} clasificada como EPR, pero la clasificación funcional viene como RechazadaOperador.");
            }
            return (true, AchTransferStateEnum.ReturnedByEpr, AchStateEventSourceEnum.Epr, $"Causal {code} mapeada como retorno EPR.");
        }

        return (false, AchTransferStateEnum.Pending, AchStateEventSourceEnum.System, $"Causal {code} no determinable con reglas actuales.");
    }
}
