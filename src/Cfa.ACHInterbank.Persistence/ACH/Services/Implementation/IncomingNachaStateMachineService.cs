using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class IncomingNachaStateMachineService : IIncomingNachaStateMachineService
{
    // Fase actual (Prompt 4B hardening):
    // WaitingWindow NO permite retry manual salvo política explícita.
    private const bool AllowRetryFromWaitingWindowByPolicy = false;

    private static readonly IReadOnlyDictionary<IncomingNachaDispatchEvent, TransitionRule> RuleByEvent =
        new Dictionary<IncomingNachaDispatchEvent, TransitionRule>
        {
            [IncomingNachaDispatchEvent.ManualRetry] = new(
                IncomingNachaDispatchEvent.ManualRetry,
                "MANUAL_RETRY",
                IncomingNachaDispatchQueueStatus.Queued,
                "Reintento manual aplicado.",
                AllowedSources: new HashSet<IncomingNachaDispatchQueueStatus>
                {
                    IncomingNachaDispatchQueueStatus.Queued,
                    IncomingNachaDispatchQueueStatus.Dispatched,
                    IncomingNachaDispatchQueueStatus.RetryPending
                }),
            [IncomingNachaDispatchEvent.ManualUnblock] = new(
                IncomingNachaDispatchEvent.ManualUnblock,
                "MANUAL_UNBLOCK",
                IncomingNachaDispatchQueueStatus.Queued,
                "Desbloqueo manual aplicado.",
                AllowedSources: new HashSet<IncomingNachaDispatchQueueStatus> { IncomingNachaDispatchQueueStatus.Blocked }),
            [IncomingNachaDispatchEvent.ManualRequeue] = new(
                IncomingNachaDispatchEvent.ManualRequeue,
                "MANUAL_REQUEUE",
                IncomingNachaDispatchQueueStatus.Queued,
                "Re-encolado manual aplicado.",
                AllowedSources: new HashSet<IncomingNachaDispatchQueueStatus>
                {
                    IncomingNachaDispatchQueueStatus.Queued,
                    IncomingNachaDispatchQueueStatus.Dispatched,
                    IncomingNachaDispatchQueueStatus.RetryPending,
                    IncomingNachaDispatchQueueStatus.Blocked,
                    IncomingNachaDispatchQueueStatus.WaitingWindow
                }),
            [IncomingNachaDispatchEvent.ManualMarkFailedFinal] = new(
                IncomingNachaDispatchEvent.ManualMarkFailedFinal,
                "MANUAL_MARK_FAILED_FINAL",
                IncomingNachaDispatchQueueStatus.FailedFinal,
                "Marcado manual a FailedFinal aplicado.",
                AllowedSources: new HashSet<IncomingNachaDispatchQueueStatus>
                {
                    IncomingNachaDispatchQueueStatus.Queued,
                    IncomingNachaDispatchQueueStatus.Dispatched,
                    IncomingNachaDispatchQueueStatus.RetryPending,
                    IncomingNachaDispatchQueueStatus.Blocked,
                    IncomingNachaDispatchQueueStatus.WaitingWindow
                })
        };

    public IncomingNachaAllowedActionsDto GetAllowedDispatchActions(IncomingNachaDispatchQueueStatus status)
    {
        var canRetry = IsTransitionAllowed(status, IncomingNachaDispatchEvent.ManualRetry);
        var canUnblock = IsTransitionAllowed(status, IncomingNachaDispatchEvent.ManualUnblock);
        var canRequeue = IsTransitionAllowed(status, IncomingNachaDispatchEvent.ManualRequeue);
        var canMarkFailedFinal = IsTransitionAllowed(status, IncomingNachaDispatchEvent.ManualMarkFailedFinal);

        var actions = new List<string>(4);
        if (canRetry) actions.Add("retry");
        if (canUnblock) actions.Add("unblock");
        if (canRequeue) actions.Add("requeue");
        if (canMarkFailedFinal) actions.Add("mark-failed-final");

        return new IncomingNachaAllowedActionsDto(
            status,
            canRetry,
            canUnblock,
            canRequeue,
            canMarkFailedFinal,
            actions);
    }

    public IncomingNachaDispatchTransitionDecision EvaluateDispatchTransition(
        IncomingNachaDispatchQueueStatus currentStatus,
        IncomingNachaDispatchEvent transitionEvent)
    {
        if (!RuleByEvent.TryGetValue(transitionEvent, out var rule))
        {
            return new IncomingNachaDispatchTransitionDecision(
                currentStatus,
                transitionEvent,
                false,
                null,
                "INCOMING_NACHA_STATE_MACHINE_EVENT_NOT_SUPPORTED",
                $"Evento de transición no soportado: {transitionEvent}.");
        }

        if (!IsTransitionAllowed(currentStatus, transitionEvent))
        {
            return new IncomingNachaDispatchTransitionDecision(
                currentStatus,
                transitionEvent,
                false,
                null,
                $"INCOMING_NACHA_STATE_MACHINE_GUARD_{rule.GuardCode}",
                $"La transición manual '{rule.GuardCode}' no está permitida desde estado '{currentStatus}'.");
        }

        return new IncomingNachaDispatchTransitionDecision(
            currentStatus,
            transitionEvent,
            true,
            rule.TargetStatus,
            $"INCOMING_NACHA_STATE_MACHINE_OK_{rule.GuardCode}",
            rule.SuccessMessage);
    }

    private static bool IsTransitionAllowed(IncomingNachaDispatchQueueStatus status, IncomingNachaDispatchEvent transitionEvent)
    {
        if (!RuleByEvent.TryGetValue(transitionEvent, out var rule))
        {
            return false;
        }

        if (status == IncomingNachaDispatchQueueStatus.WaitingWindow
            && transitionEvent == IncomingNachaDispatchEvent.ManualRetry
            && !AllowRetryFromWaitingWindowByPolicy)
        {
            return false;
        }

        return rule.AllowedSources.Contains(status);
    }

    private sealed record TransitionRule(
        IncomingNachaDispatchEvent Event,
        string GuardCode,
        IncomingNachaDispatchQueueStatus TargetStatus,
        string SuccessMessage,
        IReadOnlySet<IncomingNachaDispatchQueueStatus> AllowedSources);
}
