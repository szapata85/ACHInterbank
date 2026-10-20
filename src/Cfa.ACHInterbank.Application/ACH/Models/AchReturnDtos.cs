namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>
/// Vista de una transacción del ciclo consultado para el flujo de devoluciones.
/// </summary>
/// <param name="Id">Identificador interno de la transacción.</param>
/// <param name="TraceNumber">Número de trazabilidad persistido de la transacción.</param>
/// <param name="Amount">Monto persistido de la transacción.</param>
/// <param name="TransactionCode">Código transaccional persistido.</param>
/// <param name="Reference">Referencia persistida de la transacción.</param>
/// <param name="SourceAccountNumber">Número de cuenta origen persistido.</param>
/// <param name="DestinationAccountNumber">Número de cuenta destino persistido.</param>
/// <param name="OriginatingDfi">Código DFI originador persistido.</param>
/// <param name="ReceivingDfi">Código DFI receptor persistido.</param>
/// <param name="AchCycleId">Identificador del ciclo al que pertenece la transacción.</param>
/// <param name="EffectiveEntryDate">Fecha efectiva persistida de la transacción.</param>
/// <param name="IsPrenotification">Indica si la transacción está marcada como prenotificación.</param>
/// <param name="IsEligible">Indicador calculado por la consulta. Actualmente es false cuando ya existe una devolución generada para la transacción.</param>
/// <param name="ValidationMessage">Motivo asociado al indicador de elegibilidad cuando la consulta detecta una devolución previamente generada.</param>
public sealed record ReturnEligibleTransactionDto(
    int Id,
    string TraceNumber,
    decimal Amount,
    string TransactionCode,
    string Reference,
    string SourceAccountNumber,
    string DestinationAccountNumber,
    string OriginatingDfi,
    string ReceivingDfi,
    string AchCycleId,
    DateTime EffectiveEntryDate,
    bool IsPrenotification,
    bool IsEligible,
    string? ValidationMessage);

/// <summary>
/// Selección de una transacción y causal para generar una devolución saliente.
/// </summary>
/// <param name="TransactionId">Identificador interno de la transacción original que se desea devolver.</param>
/// <param name="ReturnReasonCode">Código de causal que el servicio valida para la cámara, flujo y transacción seleccionados.</param>
/// <param name="CenitOperationalEvidence">Evidencia operativa opcional consumida por la política CENIT cuando la devolución se procesa en esa cámara.</param>
/// <param name="AchColombiaR10Basis">Base opcional utilizada por la validación R10 cuando la devolución corresponde a ACH Colombia.</param>
public sealed record ReturnSelectionItemDto(
    int TransactionId,
    string ReturnReasonCode,
    CenitIncomingReturnOperationalEvidence? CenitOperationalEvidence = null,
    AchColombiaR10ReturnBasis? AchColombiaR10Basis = null);

/// <summary>
/// Base operativa que puede acompañar la validación R10 de una devolución ACH Colombia.
/// </summary>
public enum AchColombiaR10ReturnBasis
{
    /// <summary>La solicitud se sustenta en ausencia de prenotificación; el servicio contrasta esa condición con las transacciones persistidas.</summary>
    NoPrenotification = 1,

    /// <summary>La solicitud declara ausencia de autorización o acuerdo del receptor; el servicio acepta esta base explícita dentro de la validación R10.</summary>
    NoReceiverAuthorizationOrAgreement = 2
}

/// <summary>
/// Solicitud para generar un archivo de devoluciones a partir de una selección de transacciones.
/// </summary>
/// <param name="CycleId">Identificador del ciclo original usado para resolver las transacciones y la cámara.</param>
/// <param name="Items">Transacciones seleccionadas con la causal de devolución correspondiente. El servicio requiere al menos un ítem y no admite TransactionId duplicados.</param>
/// <param name="ReturnCycleId">Identificador opcional del ciclo de devolución. Cuando está vacío o no se informa, el servicio utiliza CycleId.</param>
public sealed record GenerateReturnsFileRequest(
    string CycleId,
    IReadOnlyList<ReturnSelectionItemDto> Items,
    string? ReturnCycleId = null);

public sealed record GenerateReturnsFileResponse(string FileName, string ContentType, byte[] Content, int TotalRecords, int TotalReturns);
