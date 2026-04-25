using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record IncomingNachaAllowedActionsDto(
    IncomingNachaDispatchQueueStatus CurrentStatus,
    bool CanRetry,
    bool CanUnblock,
    bool CanRequeue,
    bool CanMarkFailedFinal,
    IReadOnlyList<string> AllowedActions);

public sealed record IncomingNachaDispatchTransitionDecision(
    IncomingNachaDispatchQueueStatus CurrentStatus,
    IncomingNachaDispatchEvent TransitionEvent,
    bool IsAllowed,
    IncomingNachaDispatchQueueStatus? NextStatus,
    string ResultCode,
    string Message);
