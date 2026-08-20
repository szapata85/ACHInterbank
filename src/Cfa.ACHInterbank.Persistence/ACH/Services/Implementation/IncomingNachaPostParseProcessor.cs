using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
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
    private readonly IIncomingNachaAchResultResolver _resultResolver;
    private readonly ICenitIncomingReturnPolicy _cenitReturnPolicy;
    private readonly ICycleNumberResolver _cycleNumberResolver;
    private readonly ICenitReturnOfReturnService? _cenitReturnOfReturnService;

    public IncomingNachaPostParseProcessor(
        AchDbContext context,
        IIncomingNachaFunctionalClassifier classifier,
        IIncomingNachaTransactionLinker linker,
        IIncomingNachaPrenotificationResolver prenotificationResolver,
        IIncomingNachaDispatchPlanner dispatchPlanner,
        IAchRegulatoryCatalogService regulatoryCatalogService,
        IAchStateTransitionService stateTransitionService,
        IIncomingNachaLocalLivePreparationService? localLivePreparationService = null,
        IIncomingNachaAchResultResolver? resultResolver = null,
        ICenitIncomingReturnPolicy? cenitReturnPolicy = null,
        ICycleNumberResolver? cycleNumberResolver = null,
        ICenitReturnOfReturnService? cenitReturnOfReturnService = null)
    {
        _context = context;
        _classifier = classifier;
        _linker = linker;
        _prenotificationResolver = prenotificationResolver;
        _dispatchPlanner = dispatchPlanner;
        _regulatoryCatalogService = regulatoryCatalogService;
        _stateTransitionService = stateTransitionService;
        _localLivePreparationService = localLivePreparationService;
        _resultResolver = resultResolver ?? new IncomingNachaAchResultResolver(context);
        _cenitReturnPolicy = cenitReturnPolicy ?? new CenitIncomingReturnPolicy();
        _cycleNumberResolver = cycleNumberResolver ?? new CycleNumberResolver();
        _cenitReturnOfReturnService = cenitReturnOfReturnService;
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

    public async Task<IncomingNachaLinkedReturnApplicationResult> ApplyLinkedReturnAsync(
        Guid incomingNachaTransactionLinkId,
        string executedBy,
        CancellationToken ct = default)
    {
        var link = await _context.IncomingNachaTransactionLinks
            .FirstOrDefaultAsync(x => x.Id == incomingNachaTransactionLinkId, ct)
            ?? throw new KeyNotFoundException("No existe la relación de devolución indicada.");

        if (link.LinkType != IncomingNachaLinkType.Manual || !link.IsFinal || !link.AchTransactionId.HasValue)
        {
            throw new InvalidOperationException("La devolución no tiene una relación manual final válida.");
        }

        var classification = await _context.IncomingNachaEntryClassifications
            .FirstOrDefaultAsync(x => x.IncomingNachaFileIngestionId == link.IncomingNachaFileIngestionId
                && x.EntryDetailId == link.EntryDetailId
                && x.AddendaRecordId == link.AddendaRecordId, ct)
            ?? throw new InvalidOperationException("No existe clasificación para la devolución indicada.");

        if (classification.FunctionalClass is not (IncomingNachaFunctionalClass.Devolucion
            or IncomingNachaFunctionalClass.RechazadaOperador
            or IncomingNachaFunctionalClass.RetornoEpr))
        {
            throw new InvalidOperationException("La entrada seleccionada no corresponde a una devolución entrante aplicable.");
        }

        var ingestion = await _context.IncomingNachaFileIngestions
            .FirstAsync(x => x.Id == link.IncomingNachaFileIngestionId, ct);
        var entry = await _context.EntryDetails
            .FirstAsync(x => x.EntryDetailID == link.EntryDetailId, ct);
        var addenda = link.AddendaRecordId.HasValue
            ? await _context.AddendaRecords.FirstOrDefaultAsync(x => x.AddendaID == link.AddendaRecordId.Value, ct)
            : null;
        var manualLink = new IncomingNachaLinkingResult
        {
            LinkType = IncomingNachaLinkType.Manual,
            AchTransactionId = link.AchTransactionId,
            IsFinal = true,
            ConfidenceScore = 1m,
            EvidenceJson = link.EvidenceJson
        };

        await AddEventAsync(ingestion.Id, entry.EntryDetailID, addenda?.AddendaID, link.AchTransactionId,
            "LinkingManualExitoso", "Ok", "Relación manual confirmada; la devolución continúa por el pipeline oficial.",
            new { incomingNachaTransactionLinkId, link.AchTransactionId }, executedBy, ct);

        return await ApplyBusinessEffectsAsync(ingestion, entry, addenda, classification, manualLink, executedBy, ct);
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

    private async Task<IncomingNachaLinkedReturnApplicationResult> ApplyBusinessEffectsAsync(
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
            return new(false, false, true, null, "MissingTransaction", "La devolución no tiene una transacción relacionada.");
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

        if (classification.FunctionalClass == IncomingNachaFunctionalClass.DevolucionDevolucion)
        {
            if (_cenitReturnOfReturnService is null || addenda is null)
                return new(false, false, true, null, "CENIT_ROR_SERVICE_REQUIRED", "El servicio CENIT ROR no está disponible.");

            var sourceReturnTrace = addenda.NewTraceNumber?.Trim() ?? string.Empty;
            var parent = await _context.Set<AchReturnGenerated>()
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.NewSequenceNumber == sourceReturnTrace
                                           && x.OriginalTransactionId == link.AchTransactionId.Value, ct);
            if (parent is null)
                return new(false, false, true, null, "CENIT_ROR_PARENT_RETURN_NOT_FOUND", "No se encontró el Return Out padre del ROR.");

            var idempotencyKey = $"cenit-ror-in:{ingestion.ResolvedClearingHouseId}:{parent.Id}:{entry.SequenceNumber}:{classification.ReturnReasonCode}";
            var rorBusinessDate = ingestion.EffectiveDate
                                  ?? ingestion.OperationalDate
                                  ?? ingestion.ReceivedAtUtc
                                  ?? DateTime.UtcNow;
            var result = await _cenitReturnOfReturnService.IngestIncomingAsync(new(
                parent.Id,
                link.AchTransactionId.Value,
                ingestion.ResolvedAchCycleId ?? string.Empty,
                classification.ReturnReasonCode ?? string.Empty,
                entry.TransactionCode?.Trim() ?? string.Empty,
                entry.SequenceNumber?.Trim() ?? string.Empty,
                addenda.OriginalTraceNumber?.Trim() ?? string.Empty,
                addenda.IdUserOrig?.Trim() ?? string.Empty,
                sourceReturnTrace,
                addenda.PurposeOfTransaction?.Trim() ?? string.Empty,
                addenda.InvoiceOrAccountNumber?.Trim() ?? string.Empty,
                entry.Amount ?? 0m,
                rorBusinessDate,
                idempotencyKey), ct);

            await AddEventAsync(ingestion.Id, entry.EntryDetailID, addenda.AddendaID, link.AchTransactionId,
                result.WasDuplicate ? "CenitRorDuplicadoIgnorado" : result.IsSuccessful ? "CenitRorAplicado" : "CenitRorBloqueado",
                result.WasDuplicate ? "Duplicado" : result.IsSuccessful ? "Ok" : "Bloqueado",
                result.Message,
                new { result.Code, result.FlowId, result.ReturnOfReturnTransactionId, sourceReturnTrace }, executedBy, ct);
            return new(result.IsSuccessful && !result.WasDuplicate, result.WasDuplicate, !result.IsSuccessful, null, result.Code, result.Message);
        }

        if (classification.FunctionalClass is IncomingNachaFunctionalClass.Devolucion or IncomingNachaFunctionalClass.RechazadaOperador or IncomingNachaFunctionalClass.RetornoEpr)
        {
            var route = await ResolveReturnRouteAsync(
                ingestion.ResolvedClearingHouseId ?? 0,
                classification.ReturnReasonCode,
                classification.FunctionalClass,
                link.AchTransactionId.Value,
                ingestion.OperationalDate ?? ingestion.ReceivedAtUtc ?? DateTime.UtcNow,
                ct);
            if (!route.IsTransitionAllowed)
            {
                classification.EligibilityStatus = IncomingNachaEligibilityStatus.RevisionManual;
                classification.RequiresManualResolution = true;
                await AddEventAsync(ingestion.Id, entry.EntryDetailID, addenda?.AddendaID, link.AchTransactionId,
                    "TransicionBloqueada", "Bloqueado", route.Reason,
                    new { classification.ReturnReasonCode, route.Reason, route.Source }, executedBy, ct);
                return new(false, false, true, null, "TransitionBlocked", route.Reason);
            }

            var cenitPolicy = await EvaluateCenitReturnPolicyAsync(ingestion, entry, classification, link.AchTransactionId.Value, ct);
            if (cenitPolicy is not null && !cenitPolicy.IsAllowed)
            {
                classification.EligibilityStatus = cenitPolicy.RequiresManualReview
                    ? IncomingNachaEligibilityStatus.RevisionManual
                    : IncomingNachaEligibilityStatus.Bloqueada;
                classification.RequiresManualResolution = cenitPolicy.RequiresManualReview;
                await AddEventAsync(ingestion.Id, entry.EntryDetailID, addenda?.AddendaID, link.AchTransactionId,
                    "PoliticaCenitReturnInBloqueada",
                    cenitPolicy.RequiresManualReview ? "RevisionManual" : "Bloqueado",
                    cenitPolicy.Message,
                    new { cenitPolicy.Code, cenitPolicy.Status, classification.ReturnReasonCode }, executedBy, ct);
                return new(false, false, cenitPolicy.RequiresManualReview, null, cenitPolicy.Code, cenitPolicy.Message);
            }

            var idempotencyKey = AchIncomingEventIdentityPolicy.BuildReturnKey(
                ingestion.ResolvedClearingHouseId!.Value,
                link.AchTransactionId.Value,
                classification.OriginalTraceRef,
                classification.ReturnReasonCode!);

            var transition = await _stateTransitionService.TransitionAsync(
                new AchStateTransitionRequest(
                    link.AchTransactionId.Value,
                    route.TargetState,
                    route.Source,
                    classification.ReturnReasonCode,
                    classification.ClassificationEvidenceJson,
                    classification.OriginalTraceRef,
                    DateTime.UtcNow,
                    idempotencyKey,
                    ingestion.ResolvedClearingHouseId,
                    route.AchReturnCodeId,
                    route.Description),
                ct);

            if (transition.WasDuplicate)
            {
                classification.EligibilityStatus = IncomingNachaEligibilityStatus.Bloqueada;
                classification.RequiresManualResolution = false;
                await AddEventAsync(ingestion.Id, entry.EntryDetailID, addenda?.AddendaID, link.AchTransactionId,
                    "EventoDuplicadoIgnorado", "Duplicado", "La novedad ya había sido aplicada y no produjo efectos adicionales.",
                    new { idempotencyKey, classification.ReturnReasonCode, classification.OriginalTraceRef }, executedBy, ct);
                var existingEventId = await _context.AchTransactionStateEvents.AsNoTracking()
                    .Where(x => x.IdempotencyKey == idempotencyKey)
                    .Select(x => (long?)x.Id)
                    .SingleOrDefaultAsync(ct);
                return new(false, true, false, existingEventId, "AlreadyApplied", "La devolución ya había sido aplicada.");
            }

            await AddEventAsync(ingestion.Id, entry.EntryDetailID, addenda?.AddendaID, link.AchTransactionId,
                "TransicionDisparada", "Ok", "Transición de estado aplicada para devolución entrante.",
                new { route.TargetState, route.Source, classification.ReturnReasonCode, classification.OriginalTraceRef, route.Reason }, executedBy, ct);

            var stateEventId = await _context.AchTransactionStateEvents.AsNoTracking()
                .Where(x => x.IdempotencyKey == idempotencyKey)
                .Select(x => (long?)x.Id)
                .SingleOrDefaultAsync(ct);
            return new(true, false, false, stateEventId, "Applied", "La devolución fue aplicada por el pipeline oficial.");
        }

        return new(false, false, classification.RequiresManualResolution, null, "NoReturnEffect", "La clasificación no requiere aplicación de devolución.");
    }

    private async Task<CenitIncomingReturnPolicyResult?> EvaluateCenitReturnPolicyAsync(
        IncomingNachaFileIngestion ingestion,
        EntryDetail entry,
        IncomingNachaEntryClassification classification,
        int achTransactionId,
        CancellationToken ct)
    {
        if (!ingestion.ResolvedClearingHouseId.HasValue)
        {
            return null;
        }

        var clearingHouseCode = await _context.ClearingHouses.AsNoTracking()
            .Where(x => x.Id == ingestion.ResolvedClearingHouseId.Value)
            .Select(x => x.Code)
            .SingleOrDefaultAsync(ct);
        if (!string.Equals(clearingHouseCode, "CENIT", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var transaction = await _context.AchTransactions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == achTransactionId, ct);
        if (transaction is null)
        {
            return new CenitIncomingReturnPolicyResult(
                CenitIncomingReturnPolicyStatus.ManualReviewRequired,
                "CENIT_ORIGINAL_TRANSACTION_REQUIRED",
                "No existe la transaccion original requerida por la politica CENIT Return In.");
        }

        var returnValueDate = ingestion.EffectiveDate ?? ingestion.OperationalDate;
        if (!returnValueDate.HasValue)
        {
            return new CenitIncomingReturnPolicyResult(
                CenitIncomingReturnPolicyStatus.ManualReviewRequired,
                "CENIT_RETURN_VALUE_DATE_REQUIRED",
                "No existe Fecha Valor normalizada para validar la devolucion CENIT.");
        }

        if (!entry.Amount.HasValue)
        {
            return new CenitIncomingReturnPolicyResult(
                CenitIncomingReturnPolicyStatus.ManualReviewRequired,
                "CENIT_RETURN_AMOUNT_REQUIRED",
                "No existe valor normalizado para validar la devolucion CENIT.");
        }

        var returnCycleName = !string.IsNullOrWhiteSpace(ingestion.ResolvedAchCycleId)
            ? await _context.AchCycles.AsNoTracking()
                .Where(x => x.Id == ingestion.ResolvedAchCycleId)
                .Select(x => x.CycleName)
                .SingleOrDefaultAsync(ct)
            : null;
        var originalCycleName = !string.IsNullOrWhiteSpace(transaction.AchCycleId)
            ? await _context.AchCycles.AsNoTracking()
                .Where(x => x.Id == transaction.AchCycleId)
                .Select(x => x.CycleName)
                .SingleOrDefaultAsync(ct)
            : null;
        var returnCycleNumber = ingestion.DetectedCycleNumber ?? _cycleNumberResolver.Resolve(returnCycleName);
        var cycleNames = await _context.AchCycles.AsNoTracking()
            .Where(x => x.ClearingHouseId == ingestion.ResolvedClearingHouseId.Value
                && x.ProcessingDate.Date == returnValueDate.Value.Date)
            .Select(x => x.CycleName)
            .ToListAsync(ct);
        var lastReturnCycleNumber = cycleNames
            .Select(_cycleNumberResolver.Resolve)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .DefaultIfEmpty()
            .Max();

        var evidence = ParseCenitOperationalEvidence(classification.ClassificationEvidenceJson);
        return _cenitReturnPolicy.Evaluate(new CenitIncomingReturnPolicyRequest(
            transaction.Type,
            classification.ReturnReasonCode ?? string.Empty,
            transaction.EffectiveEntryDate.Date,
            returnValueDate.Value.Date,
            _cycleNumberResolver.Resolve(originalCycleName),
            returnCycleNumber,
            lastReturnCycleNumber > 0 ? lastReturnCycleNumber : null,
            transaction.Amount,
            entry.Amount.Value,
            evidence.PrenotificationDirection,
            evidence.ReturnRequestDate,
            evidence.ImmediateReturnCycleConfirmed,
            evidence.FundsAvailabilityRequired,
            evidence.FundsAvailabilityConfirmed,
            evidence.ConfirmationToOriginatorRecorded,
            evidence.ReceiverRejectionDeadlineDate));
    }

    private static CenitIncomingReturnOperationalEvidence ParseCenitOperationalEvidence(string? evidenceJson)
    {
        if (string.IsNullOrWhiteSpace(evidenceJson))
        {
            return new();
        }

        try
        {
            using var document = JsonDocument.Parse(evidenceJson);
            if (!document.RootElement.TryGetProperty("cenitReturnOperationalEvidence", out var evidence))
            {
                return new();
            }

            return evidence.Deserialize<CenitIncomingReturnOperationalEvidence>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new();
        }
        catch (JsonException)
        {
            return new();
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

    private async Task<(bool IsTransitionAllowed, AchTransferStateEnum TargetState, AchStateEventSourceEnum Source, string Reason, int? AchReturnCodeId, string? Description)> ResolveReturnRouteAsync(
        int clearingHouseId,
        string? returnReasonCode,
        IncomingNachaFunctionalClass functionalClass,
        int achTransactionId,
        DateTime effectiveDate,
        CancellationToken ct)
    {
        var code = (returnReasonCode ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
        {
            return (false, AchTransferStateEnum.Pending, AchStateEventSourceEnum.System, "Causal vacía: requiere revisión manual.", null, null);
        }

        if (clearingHouseId <= 0)
        {
            return (false, AchTransferStateEnum.Pending, AchStateEventSourceEnum.System,
                $"No fue posible resolver la cámara para validar la causal {code}.", null, null);
        }

        var transaction = await _context.AchTransactions.AsNoTracking()
            .Where(x => x.Id == achTransactionId)
            .Select(x => new { x.Type })
            .FirstOrDefaultAsync(ct);
        if (transaction is null)
        {
            return (false, AchTransferStateEnum.Pending, AchStateEventSourceEnum.System,
                "La transacción original no existe y la novedad requiere revisión.", null, null);
        }

        var resolution = await _resultResolver.ResolveAsync(new IncomingNachaAchResultRequest(
            clearingHouseId,
            code,
            AchReturnFlowType.Return,
            transaction.Type == TransactionTypeEnum.Debit,
            transaction.Type == TransactionTypeEnum.Credit,
            transaction.Type == TransactionTypeEnum.Prenotification,
            transaction.Type is TransactionTypeEnum.Return or TransactionTypeEnum.Reversal,
            effectiveDate), ct);
        if (!resolution.IsResolved)
        {
            return (false, AchTransferStateEnum.Pending, AchStateEventSourceEnum.System,
                $"La causal {code} no pudo resolverse de forma única para la cámara, el flujo y la vigencia ({resolution.ResolutionCode}).",
                null,
                null);
        }

        if (resolution.BusinessOutcome is not (IncomingNachaBusinessOutcome.Returned or IncomingNachaBusinessOutcome.Rejected))
        {
            return (false, AchTransferStateEnum.Pending, AchStateEventSourceEnum.System,
                $"La causal {code} tiene resultado {resolution.BusinessOutcome} y no autoriza una devolución.",
                resolution.AchReturnCodeId,
                resolution.ResultDescription);
        }

        var chamberCode = await _context.ClearingHouses.AsNoTracking()
            .Where(x => x.Id == clearingHouseId)
            .Select(x => x.Code)
            .SingleOrDefaultAsync(ct);
        if (string.Equals(chamberCode, "ACH", StringComparison.OrdinalIgnoreCase)
            || string.Equals(chamberCode, "ACHCOL", StringComparison.OrdinalIgnoreCase)
            || string.Equals(chamberCode, "ACHCOLOMBIA", StringComparison.OrdinalIgnoreCase))
        {
            if (functionalClass is IncomingNachaFunctionalClass.RechazadaOperador or IncomingNachaFunctionalClass.Devolucion)
            {
                return (true, AchTransferStateEnum.ReturnedByOperator, AchStateEventSourceEnum.Operator,
                    $"Causal {code} resuelta para ACH Colombia.", resolution.AchReturnCodeId, resolution.ResultDescription);
            }

            return (false, AchTransferStateEnum.Pending, AchStateEventSourceEnum.System,
                "La clasificación funcional no corresponde a un rechazo individual de ACH Colombia.",
                resolution.AchReturnCodeId,
                resolution.ResultDescription);
        }

        if (string.Equals(chamberCode, "CENIT", StringComparison.OrdinalIgnoreCase))
        {
            if (functionalClass is IncomingNachaFunctionalClass.Devolucion or IncomingNachaFunctionalClass.RetornoEpr)
            {
                return (true, AchTransferStateEnum.ReturnedByEpr, AchStateEventSourceEnum.Epr,
                    $"Causal {code} resuelta para CENIT.", resolution.AchReturnCodeId, resolution.ResultDescription);
            }

            return (false, AchTransferStateEnum.Pending, AchStateEventSourceEnum.System,
                "La clasificación funcional no corresponde a una devolución de CENIT.",
                resolution.AchReturnCodeId,
                resolution.ResultDescription);
        }

        return (false, AchTransferStateEnum.Pending, AchStateEventSourceEnum.System,
            $"La cámara asociada a la causal {code} no tiene una estrategia de devolución configurada.",
            resolution.AchReturnCodeId,
            resolution.ResultDescription);
    }

}
