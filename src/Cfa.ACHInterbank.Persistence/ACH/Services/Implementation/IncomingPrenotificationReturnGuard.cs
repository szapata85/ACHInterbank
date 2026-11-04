using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// I04: guarda de correlacion para Return In de prenotificaciones. No aplica estados ni
/// persiste resultados: la unica ruta de aplicacion sigue siendo AchStateTransitionService.
/// Los archivos del simulador se reconocen exclusivamente por SHA-256 de bytes recibidos.
/// </summary>
public static class IncomingPrenotificationReturnGuard
{
    public sealed record Result(bool Allowed, bool Applies, string Code, string Message)
    {
        public static Result NotApplicable { get; } = new(true, false, "NOT_PRENOTIFICATION_RETURN", "La entrada no pertenece al flujo de prenotificaciones.");
        public static Result Ok { get; } = new(true, true, "PRENOTIFICATION_RETURN_CORRELATED", "Devolucion de prenotificacion correlacionada; continua por el pipeline oficial.");
        public static Result Block(string code, string message) => new(false, true, code, message);
    }

    public static Result Evaluate(
        IncomingNachaFileIngestion ingestion,
        EntryDetail entry,
        AddendaRecord? addenda,
        IncomingNachaEntryClassification classification,
        IncomingNachaLinkingResult link,
        AchTransaction? original,
        PrenotificationValidationAttempt? attempt,
        NachaInboundSimulation? matchedSimulation,
        bool ambiguousSimulationHash = false)
    {
        var isPrenote = original?.IsPrenotification == true || original?.Type == TransactionTypeEnum.Prenotification;
        if (!isPrenote && matchedSimulation is null && !ambiguousSimulationHash)
            return Result.NotApplicable;

        if (ambiguousSimulationHash)
            return Result.Block("SIMULATION_FILE_HASH_AMBIGUOUS", "El archivo corresponde a mas de una simulacion; requiere revision manual.");
        if (original is null || !isPrenote)
            return Result.Block("PRENOTE_ORIGINAL_REQUIRED", "El archivo simulado no corresponde a una prenotificacion original.");
        if (!link.IsFinal || link.AchTransactionId != original.Id ||
            link.LinkType is not (IncomingNachaLinkType.ExactOriginalTraceRef or IncomingNachaLinkType.Manual))
            return Result.Block("PRENOTE_LINK_NOT_EXACT", "La devolucion exige correlacion por trace original inequívoco o vinculo manual final.");
        if (addenda is null || !string.Equals(addenda.CodeTypeAddendumRecord?.Trim(), "99", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(addenda.OriginalTraceNumber)
            || !string.Equals(addenda.OriginalTraceNumber.Trim(), original.TraceNumber?.Trim(), StringComparison.Ordinal)
            || !string.Equals(classification.OriginalTraceRef?.Trim(), original.TraceNumber?.Trim(), StringComparison.Ordinal))
            return Result.Block("PRENOTE_ORIGINAL_TRACE_MISMATCH", "La adenda 99 no conserva el trace de la prenotificacion original.");
        if (!entry.Amount.HasValue || entry.Amount.Value != 0m || original.Amount != 0m)
            return Result.Block("PRENOTE_RETURN_NONZERO_AMOUNT", "La devolucion de prenotificacion debe tener importe cero.");
        if (string.IsNullOrWhiteSpace(entry.SequenceNumber) ||
            string.Equals(entry.SequenceNumber.Trim(), original.TraceNumber?.Trim(), StringComparison.Ordinal))
            return Result.Block("PRENOTE_RETURN_TRACE_INVALID", "El trace del retorno debe ser distinto del original.");
        if (!ingestion.ResolvedClearingHouseId.HasValue || original.AchCycle is null ||
            original.AchCycle.ClearingHouseId != ingestion.ResolvedClearingHouseId.Value)
            return Result.Block("PRENOTE_CLEARING_HOUSE_MISMATCH", "La camara de origen no coincide con la del archivo recibido.");
        // APN-I10: a late return after a terminal outcome is an operational
        // exception. Do not mutate the immutable attempt or re-activate/reject
        // the third party by a different pipeline.
        if (original.State is AchTransferStateEnum.AppliedTacitly or AchTransferStateEnum.Certified)
            return Result.Block("PRENOTE_LATE_RETURN_REQUIRES_MANUAL_REVIEW",
                "Devolución recibida después del resultado terminal de prenotificación; revisión manual obligatoria.");
        if (attempt is not null)
        {
            if (attempt.AchTransactionId != original.Id ||
                attempt.ThirdPartyAccountValidation?.ClearingHouseId != ingestion.ResolvedClearingHouseId.Value ||
                (attempt.ThirdPartyAccountValidation.CurrentAttemptId.HasValue &&
                 attempt.ThirdPartyAccountValidation.CurrentAttemptId != attempt.Id))
                return Result.Block("PRENOTE_ATTEMPT_CONTEXT_MISMATCH", "El intento vigente no pertenece a la operacion y camara recibidas.");
            var terminal = attempt.Resolution is PrenotificationValidationResolution.Accepted
                or PrenotificationValidationResolution.TacitlyAccepted
                or PrenotificationValidationResolution.Rejected;
            if (terminal || attempt.Status is PrenotificationValidationAttemptStatus.Accepted
                or PrenotificationValidationAttemptStatus.TacitlyAccepted
                or PrenotificationValidationAttemptStatus.Rejected)
            {
                // Permitimos el mismo reupload para que el pipeline de eventos marque WasDuplicate.
                var isExactReplay = attempt.Resolution == PrenotificationValidationResolution.Rejected
                    && (original.State is AchTransferStateEnum.ReturnedByOperator or AchTransferStateEnum.ReturnedByEpr)
                    && string.Equals(attempt.ReturnCode?.Trim(), classification.ReturnReasonCode?.Trim(), StringComparison.OrdinalIgnoreCase);
                if (!isExactReplay)
                    return Result.Block("PRENOTE_ALREADY_RESOLVED", "La prenotificacion ya tiene un resultado terminal diferente.");
            }
            else if (attempt.Status is not (PrenotificationValidationAttemptStatus.Sent
                or PrenotificationValidationAttemptStatus.AwaitingResponse))
                return Result.Block("PRENOTE_ATTEMPT_NOT_AWAITING", "El intento no esta en espera de respuesta.");
        }
        if (matchedSimulation is not null)
        {
            if (matchedSimulation.ScenarioType != NachaInboundSimulationType.IncomingPrenotificationReturn ||
                matchedSimulation.Status is not (NachaInboundSimulationStatus.Generated or NachaInboundSimulationStatus.Downloaded) ||
                matchedSimulation.ClearingHouseId != ingestion.ResolvedClearingHouseId ||
                matchedSimulation.OriginFinancialInstitutionId != original.DestinationInstitutionId ||
                matchedSimulation.DestinationFinancialInstitutionId != original.SourceInstitutionId ||
                !string.Equals(matchedSimulation.ReasonCode?.Trim(), classification.ReturnReasonCode?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                matchedSimulation.Entries.Count(x => x.TransactionId == original.Id &&
                    string.Equals(x.PrenotificationReference?.Trim(), original.TraceNumber?.Trim(), StringComparison.Ordinal)) != 1)
                return Result.Block("SIMULATION_PROVENANCE_MISMATCH", "El archivo no coincide con la camara, entidad, causal y operaciones reservadas por el simulador.");
            if (attempt is null)
                return Result.Block("SIMULATION_PRENOTE_ATTEMPT_REQUIRED", "No se encontro intento de validacion asociado a la prenotificacion simulada.");
        }
        return Result.Ok;
    }
}
