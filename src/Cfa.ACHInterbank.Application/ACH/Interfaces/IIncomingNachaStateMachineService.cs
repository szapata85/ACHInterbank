using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IIncomingNachaStateMachineService
{
    IncomingNachaAllowedActionsDto GetAllowedDispatchActions(IncomingNachaDispatchQueueStatus status);
    IncomingNachaDispatchTransitionDecision EvaluateDispatchTransition(IncomingNachaDispatchQueueStatus currentStatus, IncomingNachaDispatchEvent transitionEvent);
}
