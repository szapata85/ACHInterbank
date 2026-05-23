namespace Cfa.ACHInterbank.Application.ACH.Responses.Processing.Models;

public sealed record DifferentialPrenotificationResponseProcessResult(
    bool Processed,
    bool StateChanged,
    bool StateEventCreated,
    bool TracePersisted,
    bool MonetaryMovementCreated,
    bool BalancesAffected,
    bool Duplicate,
    int? PrenotificationTransactionId,
    Guid? TraceId,
    string? TargetState,
    string? ErrorCode,
    string? Message)
{
    public bool Success => Processed && ErrorCode is null;

    public static DifferentialPrenotificationResponseProcessResult Skipped(string message)
        => new(false, false, false, false, false, false, false, null, null, null, null, message);

    public static DifferentialPrenotificationResponseProcessResult Failed(string code, string message, Guid? traceId = null, int? prenotificationTransactionId = null)
        => new(false, false, false, traceId.HasValue, false, false, false, prenotificationTransactionId, traceId, null, code, message);
}
