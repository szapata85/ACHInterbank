using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>Acciones manuales habilitadas por la máquina de estados para un ítem de cola.</summary>
/// <param name="CurrentStatus">Estado de cola evaluado.</param>
/// <param name="CanRetry">Indica si ManualRetry está permitido.</param>
/// <param name="CanUnblock">Indica si ManualUnblock está permitido.</param>
/// <param name="CanRequeue">Indica si ManualRequeue está permitido.</param>
/// <param name="CanMarkFailedFinal">Indica si ManualMarkFailedFinal está permitido.</param>
/// <param name="AllowedActions">Nombres de las acciones permitidas para el estado.</param>
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
