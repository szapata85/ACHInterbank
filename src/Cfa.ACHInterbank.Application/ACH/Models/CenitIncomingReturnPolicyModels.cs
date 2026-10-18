using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>
/// Dirección de prenotificación que puede aportarse como evidencia operativa para la evaluación CENIT.
/// </summary>
public enum CenitPrenotificationDirection
{
    /// <summary>No se aporta una dirección explícita; el servicio intenta resolverla desde el código transaccional.</summary>
    Unknown = 0,

    /// <summary>Prenotificación de débito.</summary>
    Debit = 1,

    /// <summary>Prenotificación de crédito.</summary>
    Credit = 2
}

public enum CenitIncomingReturnPolicyStatus
{
    Allowed = 1,
    Rejected = 2,
    ManualReviewRequired = 3
}

public sealed record CenitReturnCauseDefinition(
    string Code,
    string Description,
    bool AppliesToDebitPrenotification,
    bool AppliesToDebitMonetary,
    bool AppliesToCreditPrenotification,
    bool AppliesToCreditMonetary,
    int? MaxCalendarDays);

public sealed record CenitIncomingReturnPolicyRequest(
    TransactionTypeEnum OriginalTransactionType,
    string ReturnReasonCode,
    DateTime OriginalValueDate,
    DateTime ReturnValueDate,
    int? OriginalCycleNumber,
    int? ReturnCycleNumber,
    int? LastReturnCycleNumber,
    decimal OriginalAmount,
    decimal ReturnedAmount,
    CenitPrenotificationDirection PrenotificationDirection = CenitPrenotificationDirection.Unknown,
    DateTime? ReturnRequestDate = null,
    bool? ImmediateReturnCycleConfirmed = null,
    bool? FundsAvailabilityRequired = null,
    bool? FundsAvailabilityConfirmed = null,
    bool? ConfirmationToOriginatorRecorded = null,
    DateTime? ReceiverRejectionDeadlineDate = null);

/// <summary>
/// Evidencia operativa opcional que la generación de devoluciones entrega a la política CENIT cuando corresponde.
/// </summary>
/// <param name="PrenotificationDirection">Dirección de prenotificación informada. Unknown permite que el servicio intente resolverla desde el código transaccional.</param>
/// <param name="ReturnRequestDate">Fecha de la solicitud de devolución cuando se dispone de ella.</param>
/// <param name="ImmediateReturnCycleConfirmed">Confirmación opcional de que se está usando el ciclo inmediato de devolución.</param>
/// <param name="FundsAvailabilityRequired">Indica, cuando se informa, si la evaluación requiere comprobar disponibilidad de fondos.</param>
/// <param name="FundsAvailabilityConfirmed">Resultado opcional de la comprobación de disponibilidad de fondos.</param>
/// <param name="ConfirmationToOriginatorRecorded">Indica, cuando se informa, si quedó registrada la confirmación al originador.</param>
/// <param name="ReceiverRejectionDeadlineDate">Fecha límite de rechazo del receptor cuando forma parte de la evidencia disponible.</param>
public sealed record CenitIncomingReturnOperationalEvidence(
    CenitPrenotificationDirection PrenotificationDirection = CenitPrenotificationDirection.Unknown,
    DateTime? ReturnRequestDate = null,
    bool? ImmediateReturnCycleConfirmed = null,
    bool? FundsAvailabilityRequired = null,
    bool? FundsAvailabilityConfirmed = null,
    bool? ConfirmationToOriginatorRecorded = null,
    DateTime? ReceiverRejectionDeadlineDate = null);

public sealed record CenitIncomingReturnPolicyResult(
    CenitIncomingReturnPolicyStatus Status,
    string Code,
    string Message)
{
    public bool IsAllowed => Status == CenitIncomingReturnPolicyStatus.Allowed;
    public bool RequiresManualReview => Status == CenitIncomingReturnPolicyStatus.ManualReviewRequired;
}
