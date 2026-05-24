namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record IncomingNachaDispatchEligibilityResult(
    bool IsEligible,
    bool IsWaitingWindow,
    bool IsBlocked,
    int Priority,
    string Reason,
    string EvidenceJson);

public sealed record IncomingNachaPostProcessingRunResult(
    int Planned,
    int Picked,
    int Confirmed,
    int RetryPending,
    int FailedFinal,
    int Blocked,
    int WaitingWindow,
    string Summary);

public sealed record ProcTransaccionesRequestContract(
    IReadOnlyDictionary<string, string> Parameters,
    IReadOnlyDictionary<string, string>? SourceValues = null);

public sealed record ProcTransaccionesRequestResolution(
    ProcTransaccionesRequestContract Contract,
    Guid MappingSetId,
    int MappingVersion,
    string MappingSnapshotHash);

public sealed record ProcTransaccionesParsedResponse(
    bool IsSuccess,
    bool IsPartialSuccess,
    bool IsFunctionalRejection,
    bool IsRetryable,
    string ResponseCode,
    string ResponseMessage,
    string RawResponse);
