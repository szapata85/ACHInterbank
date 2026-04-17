using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
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
    private readonly IAchStateTransitionService _stateTransitionService;

    public IncomingNachaPostParseProcessor(
        AchDbContext context,
        IIncomingNachaFunctionalClassifier classifier,
        IIncomingNachaTransactionLinker linker,
        IAchStateTransitionService stateTransitionService)
    {
        _context = context;
        _classifier = classifier;
        _linker = linker;
        _stateTransitionService = stateTransitionService;
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

            var linkResult = await _linker.LinkAsync(entry, relatedAddenda, ct);
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
                classificationRow.EligibilityStatus = IncomingNachaEligibilityStatus.Bloqueada;
                classificationRow.RequiresManualResolution = true;
                await AddEventAsync(ingestionId, entry.EntryDetailID, relatedAddenda?.AddendaID, null,
                    "LinkingBloqueado", linkResult.IsAmbiguous ? "Ambiguo" : "NoEncontrado",
                    "Linking no determinístico. Se bloquea avance automático.",
                    new { linkResult.LinkType, linkResult.EvidenceJson }, executedBy, ct);
                continue;
            }

            await AddEventAsync(ingestionId, entry.EntryDetailID, relatedAddenda?.AddendaID, linkResult.AchTransactionId,
                "LinkingExitoso", "Ok", "Linking determinístico completado.",
                new { linkResult.LinkType, linkResult.ConfidenceScore }, executedBy, ct);

            await ApplyBusinessEffectsAsync(ingestionId, entry, relatedAddenda, classificationRow, linkResult, executedBy, ct);
        }

        await _context.SaveChangesAsync(ct);
    }

    private async Task ApplyBusinessEffectsAsync(
        Guid ingestionId,
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
            var thirdParties = await _context.CustomerThirdParties
                .Where(x => x.PrenotificationTransactionId == link.AchTransactionId.Value)
                .ToListAsync(ct);

            foreach (var tp in thirdParties)
            {
                tp.Status = CustomerThirdPartyStatusEnum.Active;
                tp.ValidationReceivedAt = DateTime.UtcNow;
                tp.ValidationMessage = "Prenotificación entrante confirmada por archivo NACHA-M.";
            }

            classification.PrenoteStatus = thirdParties.Count > 0
                ? IncomingNachaPrenoteStatus.ActivaTercero
                : IncomingNachaPrenoteStatus.RequiereRevision;

            await AddEventAsync(ingestionId, entry.EntryDetailID, addenda?.AddendaID, link.AchTransactionId,
                thirdParties.Count > 0 ? "PrenotificacionAplicada" : "PrenotificacionRequiereRevision",
                "Ok",
                thirdParties.Count > 0 ? "Prenotificación aplicada sobre CustomerThirdParty." : "No se encontró CustomerThirdParty para aplicar prenotificación.",
                new { thirdPartyCount = thirdParties.Count }, executedBy, ct);
        }

        if (classification.FunctionalClass is IncomingNachaFunctionalClass.Devolucion or IncomingNachaFunctionalClass.RechazadaOperador or IncomingNachaFunctionalClass.RetornoEpr)
        {
            var source = classification.FunctionalClass == IncomingNachaFunctionalClass.RechazadaOperador
                ? AchStateEventSourceEnum.Operator
                : AchStateEventSourceEnum.Epr;

            var toState = classification.FunctionalClass == IncomingNachaFunctionalClass.RechazadaOperador
                ? AchTransferStateEnum.ReturnedByOperator
                : AchTransferStateEnum.ReturnedByEpr;

            await _stateTransitionService.TransitionAsync(
                link.AchTransactionId.Value,
                toState,
                source,
                classification.ReturnReasonCode,
                classification.ClassificationEvidenceJson,
                classification.OriginalTraceRef,
                DateTime.UtcNow,
                ct);

            await AddEventAsync(ingestionId, entry.EntryDetailID, addenda?.AddendaID, link.AchTransactionId,
                "TransicionDisparada", "Ok", "Transición de estado aplicada para devolución entrante.",
                new { toState, source, classification.ReturnReasonCode, classification.OriginalTraceRef }, executedBy, ct);
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
}
