using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Services;

/// <summary>
/// I02: reglas puras sobre evidencia persistida. NO reserva idempotencia ni ejecuta
/// devoluciones. I03 debe reevaluar dentro de la transaccion de generacion.
/// </summary>
public static class PrenotificationSimulationEligibilityPolicy
{
    public sealed record Evidence(
        bool IsPrenotification,
        bool IsOriginDefault,
        bool IsDestinationExternal,
        AchTransactionDirection Direction,
        AchTransactionOrigin Origin,
        AchTransferStateEnum TransactionState,
        string TraceNumber,
        string TransactionExternalId,
        string OriginalCycleId,
        string ClearingHouseCode,
        int RespondingFinancialInstitutionId,
        bool TransmittedOrAcknowledged,
        int ActiveAttemptCount,
        bool CurrentAttemptMatches,
        PrenotificationValidationAttemptStatus? AttemptStatus,
        ThirdPartyAccountValidationStatus? ValidationStatus,
        PrenotificationValidationResolution? Resolution,
        DateTimeOffset? ResponseDeadlineAtUtc,
        bool HasPriorResponse);

    public static IReadOnlyList<PrenotificationSimulationContractViolation> CheckCommon(Evidence evidence)
    {
        var issues = new List<PrenotificationSimulationContractViolation>();
        void Error(string code, string field, string detail) => issues.Add(new(code, field, detail));
        if (!evidence.IsPrenotification)
            Error("NOT_A_PRENOTIFICATION", "OriginalPrenotificationIds", "El registro no es una prenotificacion.");
        if (!evidence.IsOriginDefault || !evidence.IsDestinationExternal
            || evidence.Direction == AchTransactionDirection.Incoming
            || evidence.Origin == AchTransactionOrigin.ExternalInstitution)
            Error("NOT_ORIGINATED_HERE", "OriginalPrenotificationIds", "La operacion no fue originada por ACH Interbank hacia una entidad externa.");
        if (evidence.TransactionState != AchTransferStateEnum.Pending)
            Error("PRENOTIFICATION_NOT_PENDING", "OriginalPrenotificationIds", "La transaccion original ya no esta pendiente.");
        if (string.IsNullOrWhiteSpace(evidence.TraceNumber) || string.IsNullOrWhiteSpace(evidence.TransactionExternalId))
            Error("PRENOTIFICATION_CORRELATION_INCOMPLETE", "OriginalPrenotificationIds", "Falta trace o identificador externo canonico.");
        if (!evidence.TransmittedOrAcknowledged)
            Error("PRENOTIFICATION_NOT_TRANSMITTED", "OriginalPrenotificationIds", "No hay exportacion transmitida o reconocida asociada a la transaccion.");
        if (evidence.ActiveAttemptCount != 1 || !evidence.CurrentAttemptMatches
            || evidence.AttemptStatus != PrenotificationValidationAttemptStatus.AwaitingResponse
            || evidence.ValidationStatus != ThirdPartyAccountValidationStatus.PendingResponse
            || evidence.Resolution != PrenotificationValidationResolution.None)
            Error("PRENOTIFICATION_ATTEMPT_NOT_ACTIVE", "OriginalPrenotificationIds", "Falta un intento de validacion actual, unico y pendiente.");
        if (evidence.HasPriorResponse)
            Error("PRENOTIFICATION_ALREADY_RESPONDED", "OriginalPrenotificationIds", "Ya existe una respuesta asociada a la transaccion.");
        if (evidence.ResponseDeadlineAtUtc is null)
            Error("PRENOTIFICATION_DEADLINE_UNKNOWN", "OriginalPrenotificationIds", "No existe vencimiento reglamentario resuelto.");
        return issues;
    }

    public static IReadOnlyList<PrenotificationSimulationContractViolation> CheckOutcome(
        Evidence evidence, PrenotificationSimulationOutcome outcome, DateTimeOffset nowUtc)
    {
        var errors = CheckCommon(evidence).ToList();
        if (evidence.ResponseDeadlineAtUtc is not { } deadline) return errors;
        if (outcome == PrenotificationSimulationOutcome.Returned && nowUtc >= deadline)
            errors.Add(new("RETURN_DEADLINE_EXPIRED", "BusinessDate", "No se puede simular una devolucion dentro del plazo cuando este ya vencio."));
        if (outcome == PrenotificationSimulationOutcome.NoReturnByDeadline && nowUtc < deadline)
            errors.Add(new("PRENOTIFICATION_DEADLINE_NOT_REACHED", "BusinessDate", "La aceptacion tacita aun no puede evaluarse."));
        return errors;
    }
}
