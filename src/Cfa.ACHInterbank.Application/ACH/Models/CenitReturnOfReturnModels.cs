namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record CenitReturnOfReturnOutRequest(
    long ParentIncomingReturnStateEventId,
    string ReasonCode,
    string ReturnCycleId,
    DateTime RequestedAtUtc,
    string? RequestedBy = null,
    string? Source = null);

public sealed record CenitReturnOfReturnInRequest(
    int ParentOutgoingReturnGeneratedId,
    int OriginalTransactionId,
    string ReceivedCycleId,
    string ReasonCode,
    string TransactionCode,
    string TraceNumber,
    string OriginalTraceNumber,
    string OriginalReceivingDfi,
    string SourceReturnTraceNumber,
    string SourceReturnSettlementDate,
    string SourceReturnReasonCode,
    decimal Amount,
    DateTime ReceivedAtUtc,
    string IdempotencyKey);

/// <summary>Resultado del servicio CENIT de devolución de devolución expuesto por la creación saliente.</summary>
/// <param name="IsSuccessful">Indica si el servicio completó exitosamente la creación solicitada.</param>
/// <param name="WasDuplicate">Indica si el servicio identificó la solicitud como duplicada.</param>
/// <param name="FlowId">Identificador del flujo de devolución de devolución cuando existe.</param>
/// <param name="ReturnOfReturnTransactionId">Identificador de la transacción de devolución de devolución cuando existe.</param>
/// <param name="Code">Código de resultado producido por el servicio.</param>
/// <param name="Message">Mensaje de resultado producido por el servicio.</param>
public sealed record CenitReturnOfReturnResult(
    bool IsSuccessful,
    bool WasDuplicate,
    long? FlowId,
    int? ReturnOfReturnTransactionId,
    string Code,
    string Message);
