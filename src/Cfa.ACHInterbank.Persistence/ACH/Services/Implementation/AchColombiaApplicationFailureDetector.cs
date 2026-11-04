using System.Text.Json;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// I02-ACH: evidencia pasiva de un rechazo funcional de aplicación confirmado por el core.
/// No elige una devolución, no crea AchReturnGenerated y no invoca transporte NACHA-M.
/// Las fallas de prevalidación, transporte y de ciclo NO son fallas de aplicación del receptor.
/// </summary>
internal sealed class AchColombiaApplicationFailureDetector(AchDbContext context, TimeProvider timeProvider)
{
    internal const string DetectedEvent = "AchColApplicationFailureDetected";
    internal const string ReviewEvent = "AchColApplicationFailureReview";

    internal static bool IsConfirmedApplicationFailure(
        IncomingNachaDispatchQueue queue,
        IncomingNachaIntegrationExecution execution,
        string operatingMode)
    {
        var tx = queue.AchTransaction;
        var cycle = tx?.AchCycle;
        return IncomingNachaPostProcessingMode.IsLive(operatingMode)
            && IncomingNachaPostProcessingMode.IsLive(execution.ExecutionMode)
            && queue.SourceKind == IncomingNachaDispatchSourceKinds.CycleBound
            && queue.AchTransactionId is > 0
            && queue.IncomingNachaTransactionLinkId.HasValue
            && !string.IsNullOrWhiteSpace(queue.AchCycleId)
            && tx is not null && tx.Id == queue.AchTransactionId
            && string.Equals(tx.AchCycleId, queue.AchCycleId, StringComparison.Ordinal)
            && cycle is not null && cycle.Id == queue.AchCycleId
            && cycle.ClearingHouseId == queue.ClearingHouseId
            && string.Equals(cycle.ClearingHouse?.Code, "ACHCOL", StringComparison.OrdinalIgnoreCase)
            && (tx.Type is TransactionTypeEnum.Credit or TransactionTypeEnum.Debit or TransactionTypeEnum.Prenotification)
            && execution.DispatchQueueId == queue.Id
            && string.Equals(execution.SoapMethodName, "Proc_Transacciones", StringComparison.Ordinal)
            && execution.ClearingHouseId == queue.ClearingHouseId
            && execution.ProcessedAtUtc.HasValue
            && execution.TransportStatus == IntegrationTransportStatus.Succeeded
            && execution.ResponseCatalogId.HasValue
            && execution.BusinessStatus == IntegrationResponseBusinessStatus.Rejected
            && execution.IsFunctionalRejection
            && !execution.IsTechnicalFailure
            && !execution.IsSuccessful
            && !execution.RetryAllowed
            && !execution.RequiresManualReview
            && execution.ProcessingStatus == IncomingNachaIndividualProcessingStatus.Completed;
    }

    internal async Task<bool> RecordIfConfirmedAsync(
        IncomingNachaDispatchQueue queue,
        IncomingNachaIntegrationExecution execution,
        string operatingMode,
        CancellationToken ct)
    {
        if (!IsConfirmedApplicationFailure(queue, execution, operatingMode))
            return false;

        // Se registra una sola incidencia operativa por transacción en el flujo normal.
        // I03 incorporará idempotencia con garantía de BD para devoluciones preparadas.
        var transactionId = queue.AchTransactionId!.Value;
        var alreadyTracked = context.IncomingNachaProcessingEvents.Local.Any(e =>
            e.AchTransactionId == transactionId && (e.EventType == DetectedEvent || e.EventType == ReviewEvent));
        if (alreadyTracked || await context.IncomingNachaProcessingEvents.AsNoTracking().AnyAsync(e =>
                e.AchTransactionId == transactionId && (e.EventType == DetectedEvent || e.EventType == ReviewEvent), ct))
            return false;

        // Un código del core solo se propone como causal si coincide literalmente con Rxx
        // y consta tanto en el catálogo ACH de respuestas para ESTA cámara/tipo como en
        // el catálogo de devoluciones. No se infieren causas a partir de descripciones SOAP.
        var responseCode = execution.ResultCode.Trim().ToUpperInvariant();
        var isPossibleReason = responseCode.Length == 3 && responseCode[0] == 'R'
            && char.IsAsciiDigit(responseCode[1]) && char.IsAsciiDigit(responseCode[2]);
        var reasonKnown = isPossibleReason && execution.AchReturnCodeId.HasValue
            && await context.ReturnReasons.AsNoTracking().AnyAsync(
                r => r.Code == responseCode && r.IsForReturn, ct);
        var suggestedReason = reasonKnown ? responseCode : string.Empty;

        context.IncomingNachaProcessingEvents.Add(new IncomingNachaProcessingEvent
        {
            IncomingNachaFileIngestionId = queue.IncomingNachaFileIngestionId,
            EntryDetailId = execution.EntryDetailId,
            AchTransactionId = transactionId,
            EventType = reasonKnown ? DetectedEvent : ReviewEvent,
            EventStatus = reasonKnown ? "Detectada" : "RevisionCausal",
            Message = reasonKnown
                ? "Se detectó un rechazo funcional de aplicación ACH Colombia. La causal sugerida requiere validación antes de devolver."
                : "Se detectó un rechazo funcional de aplicación ACH Colombia, sin causal de devolución verificada. Requiere revisión.",
            EvidenceJson = JsonSerializer.Serialize(new
            {
                camera = "ACHCOL",
                queueId = queue.Id,
                executionId = execution.Id,
                transactionId,
                transactionType = queue.AchTransaction!.IsPrenotification ? "Prenotificacion" : queue.AchTransaction.Type.ToString(),
                responseCode,
                suggestedReturnReason = suggestedReason,
                causeVerified = reasonKnown,
                origin = "CORE_FUNCIONAL",
                returnGenerated = false,
                returnSent = false
            }),
            OccurredAtUtc = timeProvider.GetUtcNow().UtcDateTime,
            RaisedBy = "achcol.application-failure.detector"
        });
        return true;
    }
}
