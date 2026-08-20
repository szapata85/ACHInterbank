using System.Globalization;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Models;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class DifferentialPrenotificationResponseProcessor : IDifferentialPrenotificationResponseProcessor
{
    private readonly AchDbContext _context;
    private readonly ITransactionIntegrationOperationResolver _operationResolver;
    private readonly IIntegrationMappingReadinessService _readinessService;
    private readonly IIntegrationMappingTraceWriter _traceWriter;
    private readonly IAchStateTransitionService _stateTransitionService;

    public DifferentialPrenotificationResponseProcessor(
        AchDbContext context,
        ITransactionIntegrationOperationResolver operationResolver,
        IIntegrationMappingReadinessService readinessService,
        IIntegrationMappingTraceWriter traceWriter,
        IAchStateTransitionService stateTransitionService)
    {
        _context = context;
        _operationResolver = operationResolver;
        _readinessService = readinessService;
        _traceWriter = traceWriter;
        _stateTransitionService = stateTransitionService;
    }

    public async Task<DifferentialPrenotificationResponseProcessResult> ProcessAsync(
        ProcesarRespuestaAchCommand command,
        AchResponse response,
        HomologarRespuestaAchResult homologation,
        CancellationToken cancellationToken = default)
    {
        if (command.TipoRespuesta != Domain.Models.ACH.Enums.TipoRespuestaAch.Prenota)
        {
            return DifferentialPrenotificationResponseProcessResult.Skipped("La respuesta no corresponde a prenotificacion.");
        }

        var match = await ResolvePrenotificationMatchAsync(command, cancellationToken);
        if (match.CorrelationErrorCode is not null)
        {
            var ambiguousTrace = await WriteTraceAsync(command, response, homologation, match, null, cancellationToken);
            return DifferentialPrenotificationResponseProcessResult.Failed(
                match.CorrelationErrorCode,
                match.CorrelationErrorMessage!,
                ambiguousTrace.TraceId);
        }

        if (match.Prenotification is null)
        {
            var unmatchedTrace = await WriteTraceAsync(command, response, homologation, match, null, cancellationToken);
            return DifferentialPrenotificationResponseProcessResult.Failed(
                "DIFFERENTIAL_RESPONSE_PRENOTIFICATION_NOT_FOUND",
                "No se encontro prenotificacion CFA pendiente relacionada con la respuesta diferencial.",
                unmatchedTrace.TraceId);
        }

        var prenotification = match.Prenotification;
        response.AchTransactionId = prenotification.Id;
        response.CorrelationStatus = AchResponseCorrelationStatus.Matched;
        response.CorrelationCriterion = "VinculoPrenotificacionNacha";
        if (prenotification.State != AchTransferStateEnum.Pending)
        {
            var duplicateTrace = await WriteTraceAsync(command, response, homologation, match, prenotification, cancellationToken);
            return new DifferentialPrenotificationResponseProcessResult(
                Processed: false,
                StateChanged: false,
                StateEventCreated: false,
                TracePersisted: true,
                MonetaryMovementCreated: false,
                BalancesAffected: false,
                Duplicate: true,
                PrenotificationTransactionId: prenotification.Id,
                TraceId: duplicateTrace.TraceId,
                TargetState: prenotification.State.ToString(),
                ErrorCode: "DIFFERENTIAL_RESPONSE_ALREADY_PROCESSED",
                Message: "La prenotificacion relacionada ya no esta pendiente.");
        }

        if (match.EntryDetail is not null && match.NachaHeader is null)
        {
            var unmatchedTrace = await WriteTraceAsync(command, response, homologation, match, prenotification, cancellationToken);
            return DifferentialPrenotificationResponseProcessResult.Failed(
                "DIFFERENTIAL_RESPONSE_UNMATCHED",
                "La entrada NACHA-M correlacionada no tiene cabecera de archivo asociada.",
                unmatchedTrace.TraceId,
                prenotification.Id);
        }

        var responseClearingHouseId = response.ClearingHouseId;
        var prenotificationClearingHouseId = prenotification.AchCycle?.ClearingHouseId;
        var nachaClearingHouseId = match.NachaHeader?.ClearingHouseId;
        if (!responseClearingHouseId.HasValue
            || !prenotificationClearingHouseId.HasValue
            || responseClearingHouseId.Value != prenotificationClearingHouseId.Value
            || (nachaClearingHouseId.HasValue
                && responseClearingHouseId.Value != nachaClearingHouseId.Value))
        {
            var mismatchTrace = await WriteTraceAsync(command, response, homologation, match, prenotification, cancellationToken);
            return DifferentialPrenotificationResponseProcessResult.Failed(
                "DIFFERENTIAL_RESPONSE_CLEARING_HOUSE_MISMATCH",
                "La cámara de la respuesta, el archivo NACHA-M y la prenotificación no coinciden.",
                mismatchTrace.TraceId,
                prenotification.Id);
        }

        if (match.EntryDetail is not null
            && (!string.Equals(
                    Normalize(match.EntryDetail.AccountNumber),
                    Normalize(prenotification.DestinationAccountNumber),
                    StringComparison.Ordinal)
                || (!string.IsNullOrWhiteSpace(match.EntryDetail.RecipIdNumber)
                    && !string.Equals(
                        Normalize(match.EntryDetail.RecipIdNumber),
                        Normalize(prenotification.RecipientIdNumber),
                        StringComparison.OrdinalIgnoreCase))))
        {
            var inconsistentTrace = await WriteTraceAsync(command, response, homologation, match, prenotification, cancellationToken);
            return DifferentialPrenotificationResponseProcessResult.Failed(
                "DIFFERENTIAL_RESPONSE_IDENTITY_MISMATCH",
                "La cuenta o identificación de la respuesta no coincide con la prenotificación.",
                inconsistentTrace.TraceId,
                prenotification.Id);
        }

        var operation = _operationResolver.ResolveDifferentialResponse(command.IdTransaccion, prenotification.Id);
        if (operation.MovesMoney)
        {
            return DifferentialPrenotificationResponseProcessResult.Failed(
                "DIFFERENTIAL_RESPONSE_NON_MONETARY_GUARDRAIL_FAILED",
                "RegistrarRespuestaTransaccion fue clasificada como monetaria.",
                prenotificationTransactionId: prenotification.Id);
        }

        var readiness = await _readinessService.EvaluateAsync(operation, cancellationToken);
        if (!readiness.IsReady || readiness.Status == "Failed" || !readiness.CanBuildPayload)
        {
            var detail = readiness.MissingRequiredMappings.Count > 0
                ? string.Join(", ", readiness.MissingRequiredMappings)
                : string.Join("; ", readiness.Errors);
            return DifferentialPrenotificationResponseProcessResult.Failed(
                readiness.Code == "OK" ? "DIFFERENTIAL_RESPONSE_MAPPING_REQUIRED" : readiness.Code,
                $"No se puede procesar respuesta diferencial de prenotificacion sin mappings requeridos activos. {detail}".Trim(),
                prenotificationTransactionId: prenotification.Id);
        }

        var trace = await WriteTraceAsync(command, response, homologation, match, prenotification, cancellationToken);
        if (trace.MissingRequiredFields.Count > 0)
        {
            return DifferentialPrenotificationResponseProcessResult.Failed(
                "DIFFERENTIAL_RESPONSE_REQUIRED_FIELD_MISSING",
                $"No se puede procesar respuesta diferencial: faltan campos requeridos en mapping trace: {string.Join(", ", trace.MissingRequiredFields)}.",
                trace.TraceId,
                prenotificationTransactionId: prenotification.Id);
        }

        var route = ResolveStateRoute(command, homologation, match);
        if (!route.Success)
        {
            return DifferentialPrenotificationResponseProcessResult.Failed(
                route.ErrorCode ?? "PRENOTIFICATION_RESPONSE_STATE_TRANSITION_INVALID",
                route.Message,
                trace.TraceId,
                prenotification.Id);
        }

        var transition = await _stateTransitionService.TransitionAsync(
            new AchStateTransitionRequest(
                prenotification.Id,
                route.TargetState!.Value,
                route.Source,
                route.ReasonCode,
                JsonSerializer.Serialize(new
                {
                    responseId = response.Id,
                    traceId = trace.TraceId,
                    command.IdTransaccion,
                    command.CodigoEstadoExterno,
                    command.CodigoCausalExterna,
                    homologation.EstadoInternoNombre,
                    nachaHeaderId = match.NachaHeader?.NachaID,
                    entryDetailId = match.EntryDetail?.EntryDetailID,
                    monetaryMovementCreated = false,
                    balancesAffected = false
                }),
                route.OriginalTraceRef,
                response.FechaRecepcion,
                $"ach-response:{response.Id:D}",
                response.ClearingHouseId,
                ResolvedReasonDescription: homologation.DescripcionCausalNormalizada),
            cancellationToken);
        if (transition.WasDuplicate)
        {
            return new DifferentialPrenotificationResponseProcessResult(
                Processed: false,
                StateChanged: false,
                StateEventCreated: false,
                TracePersisted: true,
                MonetaryMovementCreated: false,
                BalancesAffected: false,
                Duplicate: true,
                PrenotificationTransactionId: prenotification.Id,
                TraceId: trace.TraceId,
                TargetState: transition.Transaction.State.ToString(),
                ErrorCode: "DIFFERENTIAL_RESPONSE_ALREADY_PROCESSED",
                Message: "La respuesta diferencial ya había sido procesada.");
        }

        return new DifferentialPrenotificationResponseProcessResult(
            Processed: true,
            StateChanged: true,
            StateEventCreated: true,
            TracePersisted: true,
            MonetaryMovementCreated: false,
            BalancesAffected: false,
            Duplicate: false,
            PrenotificationTransactionId: prenotification.Id,
            TraceId: trace.TraceId,
            TargetState: route.TargetState.Value.ToString(),
            ErrorCode: null,
            Message: "Respuesta diferencial de prenotificacion procesada sin movimiento monetario.");
    }

    private async Task<IntegrationMappingTraceWriteResult> WriteTraceAsync(
        ProcesarRespuestaAchCommand command,
        AchResponse response,
        HomologarRespuestaAchResult homologation,
        PrenotificationNachaMatch match,
        AchTransaction? prenotification,
        CancellationToken cancellationToken)
    {
        var operation = _operationResolver.ResolveDifferentialResponse(command.IdTransaccion, prenotification?.Id);
        var payload = BuildTracePayload(command, response, homologation, match, prenotification);
        return await _traceWriter.WriteAsync(
            operation,
            payload,
            prenotification?.Id,
            command.IdTransaccion,
            command.CorrelationId ?? response.CorrelationId ?? string.Empty,
            dryRun: true,
            externalTransmission: false,
            ct: cancellationToken);
    }

    private async Task<PrenotificationNachaMatch> ResolvePrenotificationMatchAsync(ProcesarRespuestaAchCommand command, CancellationToken cancellationToken)
    {
        var reference = Normalize(command.IdTransaccion);

        var entries = await _context.EntryDetails.AsNoTracking()
            .Where(x => x.SequenceNumber != null && x.SequenceNumber.Trim() == reference)
            .OrderByDescending(x => x.EntryDetailID)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (entries.Count > 1)
        {
            return PrenotificationNachaMatch.Ambiguous(
                "La referencia diferencial coincide con mas de una entrada NACHA-M.");
        }

        var entry = entries.SingleOrDefault();

        AddendaRecord? addenda = null;
        IncomingNachaTransactionLink? link = null;
        if (entry is not null)
        {
            addenda = await _context.AddendaRecords.AsNoTracking()
                .Where(x => x.NachaID == entry.NachaID)
                .Where(x => x.EntryDetailSequenceNumber == entry.SequenceNumber
                    || x.OriginalTraceNumber == entry.SequenceNumber
                    || x.NewTraceNumber == entry.SequenceNumber)
                .OrderByDescending(x => x.AddendaID)
                .FirstOrDefaultAsync(cancellationToken);

            var links = await _context.IncomingNachaTransactionLinks.AsNoTracking()
                .Where(x => x.EntryDetailId == entry.EntryDetailID && x.IsFinal)
                .OrderByDescending(x => x.LinkedAtUtc)
                .Take(2)
                .ToListAsync(cancellationToken);

            if (links.Count > 1)
            {
                return PrenotificationNachaMatch.Ambiguous(
                    "La entrada NACHA-M tiene mas de un vinculo transaccional final.");
            }

            link = links.SingleOrDefault();
        }

        var resolution = await ResolvePrenotificationAsync(reference, link?.AchTransactionId, cancellationToken);
        if (resolution.Ambiguous)
        {
            return PrenotificationNachaMatch.Ambiguous(
                "La referencia diferencial coincide con mas de una prenotificacion original.");
        }

        var prenotification = resolution.Prenotification;
        var nachaId = entry?.NachaID;

        var header = string.IsNullOrWhiteSpace(nachaId)
            ? null
            : await _context.NachaHeaders.AsNoTracking().FirstOrDefaultAsync(x => x.NachaID == nachaId, cancellationToken);
        var batchHeader = string.IsNullOrWhiteSpace(nachaId)
            ? null
            : await _context.BatchHeaders.AsNoTracking().Where(x => x.NachaID == nachaId).OrderBy(x => x.BatchID).FirstOrDefaultAsync(cancellationToken);
        var batchControl = string.IsNullOrWhiteSpace(nachaId)
            ? null
            : await _context.BatchControls.AsNoTracking().Where(x => x.NachaID == nachaId).OrderBy(x => x.BatchControlID).FirstOrDefaultAsync(cancellationToken);
        var fileControl = string.IsNullOrWhiteSpace(nachaId)
            ? null
            : await _context.FileControls.AsNoTracking().Where(x => x.NachaID == nachaId).OrderBy(x => x.FileControlID).FirstOrDefaultAsync(cancellationToken);

        return new PrenotificationNachaMatch(prenotification, header, batchHeader, entry, addenda, batchControl, fileControl);
    }

    private async Task<PrenotificationResolution> ResolvePrenotificationAsync(string reference, int? linkedTransactionId, CancellationToken cancellationToken)
    {
        var query = _context.AchTransactions
            .Include(x => x.SourceInstitution)
            .Include(x => x.AchCycle)
            .Where(x => x.IsPrenotification);

        if (linkedTransactionId.HasValue)
        {
            var linked = await query.FirstOrDefaultAsync(x => x.Id == linkedTransactionId.Value, cancellationToken);
            if (linked is not null)
            {
                return new PrenotificationResolution(linked, false);
            }
        }

        var candidates = await query
            .Where(x => x.Direction == AchTransactionDirection.Outgoing
                && x.Origin == AchTransactionOrigin.Cfa
                && x.ClassificationStatus == AchTransactionClassificationStatus.Determined)
            .Where(x => x.Reference == reference
                || x.TransactionExternalId == reference
                || x.TraceNumber == reference)
            .OrderByDescending(x => x.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        return candidates.Count switch
        {
            0 => new PrenotificationResolution(null, false),
            1 => new PrenotificationResolution(candidates[0], false),
            _ => new PrenotificationResolution(null, true)
        };
    }

    private static DifferentialResponseMappingPayload BuildTracePayload(
        ProcesarRespuestaAchCommand command,
        AchResponse response,
        HomologarRespuestaAchResult homologation,
        PrenotificationNachaMatch match,
        AchTransaction? prenotification)
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["idCanal"] = command.IdCanal.ToString(CultureInfo.InvariantCulture),
            ["nombreCanal"] = command.NombreCanal,
            ["idTransaccion"] = command.IdTransaccion,
            ["idEstado"] = (homologation.IdEstadoServicioExterno ?? 0).ToString(CultureInfo.InvariantCulture),
            ["causal"] = command.CodigoCausalExterna ?? string.Empty,
            ["idTransaccionAxon"] = command.IdTransaccionServicioExterno.ToString(CultureInfo.InvariantCulture),
            ["descripcionCausal"] = command.DescripcionCausalExterna ?? homologation.DescripcionCausalNormalizada ?? string.Empty
        };

        var sourceValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["differentialResponse.idTransaccion"] = command.IdTransaccion,
            ["differentialResponse.idCanal"] = command.IdCanal.ToString(CultureInfo.InvariantCulture),
            ["differentialResponse.nombreCanal"] = command.NombreCanal,
            ["differentialResponse.idEstado"] = (homologation.IdEstadoServicioExterno ?? 0).ToString(CultureInfo.InvariantCulture),
            ["differentialResponse.codigoEstadoExterno"] = command.CodigoEstadoExterno,
            ["differentialResponse.codigoCausalExterna"] = command.CodigoCausalExterna ?? string.Empty,
            ["differentialResponse.idTransaccionServicioExterno"] = command.IdTransaccionServicioExterno.ToString(CultureInfo.InvariantCulture),
            ["differentialResponse.descripcionCausalExterna"] = command.DescripcionCausalExterna ?? homologation.DescripcionCausalNormalizada ?? string.Empty,
            ["differentialResponse.estadoInternoNombre"] = homologation.EstadoInternoNombre ?? string.Empty,
            ["prenotification.reference"] = prenotification?.Reference ?? string.Empty,
            ["prenotification.state"] = prenotification?.State.ToString() ?? string.Empty,
            ["transaction.id"] = prenotification?.Id.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["transaction.reference"] = prenotification?.Reference ?? string.Empty,
            ["transaction.transactionExternalId"] = prenotification?.TransactionExternalId ?? string.Empty,
            ["transaction.traceNumber"] = prenotification?.TraceNumber ?? string.Empty,
            ["nachaHeaders.nachaId"] = match.NachaHeader?.NachaID ?? string.Empty,
            ["nachaHeaders.immediateOrigin"] = match.NachaHeader?.ImmediateOrigin ?? string.Empty,
            ["nachaHeaders.immediateDestination"] = match.NachaHeader?.ImmediateDestination ?? string.Empty,
            ["nachaHeaders.fileIdModifier"] = match.NachaHeader?.FileIdModifier ?? string.Empty,
            ["nachaHeaders.referenceCode"] = match.NachaHeader?.ReferenceCode ?? string.Empty,
            ["batchHeaders.companyId"] = match.BatchHeader?.CompanyId ?? string.Empty,
            ["batchHeaders.companyName"] = match.BatchHeader?.CompanyName ?? string.Empty,
            ["batchHeaders.standardEntryClassCode"] = match.BatchHeader?.StandardEntryClassCode ?? string.Empty,
            ["batchHeaders.companyEntryDescription"] = match.BatchHeader?.CompanyEntryDescription ?? string.Empty,
            ["batchHeaders.effectiveEntryDate"] = match.BatchHeader?.EffectiveEntryDate ?? string.Empty,
            ["batchHeaders.originParticipantEntityCode"] = match.BatchHeader?.OriginParticipantEntityCode ?? string.Empty,
            ["batchHeaders.batchNumber"] = match.BatchHeader?.BatchNumber.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["entryDetails.transactionCode"] = match.EntryDetail?.TransactionCode ?? string.Empty,
            ["entryDetails.receivingParticipantEntityCode"] = match.EntryDetail?.ReceivingParticipantEntityCode ?? string.Empty,
            ["entryDetails.accountNumber"] = match.EntryDetail?.AccountNumber ?? string.Empty,
            ["entryDetails.amount"] = match.EntryDetail?.Amount?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["entryDetails.recipIdNumber"] = match.EntryDetail?.RecipIdNumber ?? string.Empty,
            ["entryDetails.recipUserName"] = match.EntryDetail?.RecipUserName ?? string.Empty,
            ["entryDetails.sequenceNumber"] = match.EntryDetail?.SequenceNumber ?? string.Empty,
            ["addendaRecords.infofromOriginator"] = match.AddendaRecord?.InfofromOriginator ?? string.Empty,
            ["addendaRecords.invoiceOrAccountNumber"] = match.AddendaRecord?.InvoiceOrAccountNumber ?? string.Empty,
            ["addendaRecords.returnReasonCode"] = match.AddendaRecord?.ReturnReasonCode ?? string.Empty,
            ["addendaRecords.originalTraceNumber"] = match.AddendaRecord?.OriginalTraceNumber ?? string.Empty,
            ["batchControls.entryAddendaCount"] = match.BatchControl?.EntryAddendaCount?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["batchControls.entryHash"] = match.BatchControl?.EntryHash?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["batchControls.totalDebitAmount"] = match.BatchControl?.TotalDebitAmount.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["batchControls.totalCreditAmount"] = match.BatchControl?.TotalCreditAmount.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["fileControls.batchCount"] = match.FileControl?.BatchCount.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["fileControls.blockCount"] = match.FileControl?.BlockCount.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["fileControls.entryAddendaCount"] = match.FileControl?.EntryAddendaCount.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["fileControls.entryHash"] = match.FileControl?.EntryHash.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["fileControls.totalDebitAmount"] = match.FileControl?.TotalDebitAmount.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            ["fileControls.totalCreditAmount"] = match.FileControl?.TotalCreditAmount.ToString(CultureInfo.InvariantCulture) ?? string.Empty
        };

        return new DifferentialResponseMappingPayload(parameters, sourceValues);
    }

    private static PrenotificationStateRoute ResolveStateRoute(
        ProcesarRespuestaAchCommand command,
        HomologarRespuestaAchResult homologation,
        PrenotificationNachaMatch match)
    {
        var status = $"{homologation.EstadoInternoNombre} {command.CodigoEstadoExterno}".Trim().ToUpperInvariant();
        if (status.Contains("APROB", StringComparison.Ordinal)
            || status.Contains("EXIT", StringComparison.Ordinal)
            || status is "00" or "0" or "OK")
        {
            return PrenotificationStateRoute.Ok(AchTransferStateEnum.Certified, AchStateEventSourceEnum.System, null, match.EntryDetail?.SequenceNumber);
        }

        var reason = (homologation.CausalNormalizada ?? command.CodigoCausalExterna ?? match.AddendaRecord?.ReturnReasonCode ?? string.Empty)
            .Trim()
            .ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(reason))
        {
            return PrenotificationStateRoute.Fail("DIFFERENTIAL_RESPONSE_REQUIRED_FIELD_MISSING", "La respuesta rechazada requiere causal homologada o externa.");
        }

        var source = reason.StartsWith("R", StringComparison.OrdinalIgnoreCase)
            ? AchStateEventSourceEnum.Epr
            : AchStateEventSourceEnum.Operator;
        var targetState = source == AchStateEventSourceEnum.Epr
            ? AchTransferStateEnum.ReturnedByEpr
            : AchTransferStateEnum.ReturnedByOperator;
        var originalTrace = match.AddendaRecord?.OriginalTraceNumber ?? match.EntryDetail?.SequenceNumber ?? command.IdTransaccion;
        return PrenotificationStateRoute.Ok(targetState, source, reason, originalTrace);
    }

    private static string Normalize(string? value)
        => (value ?? string.Empty).Trim();

    private sealed record PrenotificationNachaMatch(
        AchTransaction? Prenotification,
        NachaHeader? NachaHeader,
        BatchHeader? BatchHeader,
        EntryDetail? EntryDetail,
        AddendaRecord? AddendaRecord,
        BatchControl? BatchControl,
        FileControl? FileControl,
        string? CorrelationErrorCode = null,
        string? CorrelationErrorMessage = null)
    {
        public static PrenotificationNachaMatch Ambiguous(string message)
            => new(
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                "DIFFERENTIAL_RESPONSE_CORRELATION_AMBIGUOUS",
                message);
    }

    private sealed record PrenotificationResolution(AchTransaction? Prenotification, bool Ambiguous);

    private sealed record DifferentialResponseMappingPayload(
        IReadOnlyDictionary<string, string> Parameters,
        IReadOnlyDictionary<string, string> SourceValues);

    private sealed record PrenotificationStateRoute(
        bool Success,
        AchTransferStateEnum? TargetState,
        AchStateEventSourceEnum Source,
        string? ReasonCode,
        string? OriginalTraceRef,
        string? ErrorCode,
        string Message)
    {
        public static PrenotificationStateRoute Ok(AchTransferStateEnum targetState, AchStateEventSourceEnum source, string? reasonCode, string? originalTraceRef)
            => new(true, targetState, source, reasonCode, originalTraceRef, null, "Transicion de prenotificacion diferencial resuelta.");

        public static PrenotificationStateRoute Fail(string code, string message)
            => new(false, null, AchStateEventSourceEnum.System, null, null, code, message);
    }
}
