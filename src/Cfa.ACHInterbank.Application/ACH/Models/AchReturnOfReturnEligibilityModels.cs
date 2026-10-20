namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record AchReturnOfReturnEligibilityRequest(
    int SourceReturnTransactionId,
    string NewReturnReasonCode,
    DateTime RequestedAtUtc,
    string? RequestedBy = null,
    string? Source = null);

/// <summary>Falla funcional reportada por la evaluación de elegibilidad de una devolución de devolución.</summary>
/// <param name="Code">Código interno de la falla de elegibilidad.</param>
/// <param name="Message">Mensaje descriptivo producido por la evaluación.</param>
/// <param name="Field">Campo relacionado con la falla cuando la evaluación lo identifica.</param>
public sealed record AchReturnOfReturnEligibilityFailure(
    string Code,
    string Message,
    string? Field = null);

/// <summary>Resultado de elegibilidad de una devolución de devolución.</summary>
/// <param name="IsEligible">Indica si la evaluación funcional considera elegible la solicitud.</param>
/// <param name="ClearingHouseId">Identificador de la cámara resuelta cuando pudo determinarse.</param>
/// <param name="SourceReturnTransactionId">Identificador de la transacción de devolución origen cuando pudo resolverse.</param>
/// <param name="OriginalReturnReasonCode">Causal de devolución original cuando está disponible.</param>
/// <param name="NewReturnReasonCode">Nueva causal evaluada cuando está disponible.</param>
/// <param name="IsUniquePerTransaction">Indica el resultado de la comprobación de unicidad por transacción realizada por el servicio.</param>
/// <param name="Failures">Fallas funcionales detectadas; puede estar vacío cuando la solicitud es elegible.</param>
public sealed record AchReturnOfReturnEligibilityResult(
    bool IsEligible,
    int? ClearingHouseId,
    int? SourceReturnTransactionId,
    string? OriginalReturnReasonCode,
    string? NewReturnReasonCode,
    bool IsUniquePerTransaction,
    IReadOnlyCollection<AchReturnOfReturnEligibilityFailure> Failures);
