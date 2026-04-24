using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

namespace Cfa.ACHInterbank.Tests;

public class IncomingNachaStateMachineServiceTests
{
    private readonly IncomingNachaStateMachineService _sut = new();

    [Theory]
    [InlineData(IncomingNachaDispatchQueueStatus.Blocked, IncomingNachaDispatchEvent.ManualUnblock, IncomingNachaDispatchQueueStatus.Queued)]
    [InlineData(IncomingNachaDispatchQueueStatus.RetryPending, IncomingNachaDispatchEvent.ManualRetry, IncomingNachaDispatchQueueStatus.Queued)]
    [InlineData(IncomingNachaDispatchQueueStatus.Queued, IncomingNachaDispatchEvent.ManualMarkFailedFinal, IncomingNachaDispatchQueueStatus.FailedFinal)]
    [InlineData(IncomingNachaDispatchQueueStatus.Blocked, IncomingNachaDispatchEvent.ManualMarkFailedFinal, IncomingNachaDispatchQueueStatus.FailedFinal)]
    [InlineData(IncomingNachaDispatchQueueStatus.WaitingWindow, IncomingNachaDispatchEvent.ManualRequeue, IncomingNachaDispatchQueueStatus.Queued)]
    public void EvaluateDispatchTransition_ShouldAllowConfiguredTransitions(
        IncomingNachaDispatchQueueStatus currentStatus,
        IncomingNachaDispatchEvent transitionEvent,
        IncomingNachaDispatchQueueStatus expectedStatus)
    {
        var decision = _sut.EvaluateDispatchTransition(currentStatus, transitionEvent);

        Assert.True(decision.IsAllowed);
        Assert.Equal(expectedStatus, decision.NextStatus);
        Assert.StartsWith("INCOMING_NACHA_STATE_MACHINE_OK_", decision.ResultCode);
    }

    [Theory]
    [InlineData(IncomingNachaDispatchQueueStatus.Confirmed, IncomingNachaDispatchEvent.ManualRetry, "MANUAL_RETRY")]
    [InlineData(IncomingNachaDispatchQueueStatus.FailedFinal, IncomingNachaDispatchEvent.ManualRequeue, "MANUAL_REQUEUE")]
    [InlineData(IncomingNachaDispatchQueueStatus.FailedFinal, IncomingNachaDispatchEvent.ManualMarkFailedFinal, "MANUAL_MARK_FAILED_FINAL")]
    [InlineData(IncomingNachaDispatchQueueStatus.Blocked, IncomingNachaDispatchEvent.ManualRetry, "MANUAL_RETRY")]
    [InlineData(IncomingNachaDispatchQueueStatus.Dispatching, IncomingNachaDispatchEvent.ManualRequeue, "MANUAL_REQUEUE")]
    [InlineData(IncomingNachaDispatchQueueStatus.WaitingWindow, IncomingNachaDispatchEvent.ManualRetry, "MANUAL_RETRY")]
    [InlineData(IncomingNachaDispatchQueueStatus.RetryPending, IncomingNachaDispatchEvent.ManualUnblock, "MANUAL_UNBLOCK")]
    [InlineData(IncomingNachaDispatchQueueStatus.Confirmed, IncomingNachaDispatchEvent.ManualRequeue, "MANUAL_REQUEUE")]
    [InlineData(IncomingNachaDispatchQueueStatus.Confirmed, IncomingNachaDispatchEvent.ManualMarkFailedFinal, "MANUAL_MARK_FAILED_FINAL")]
    public void EvaluateDispatchTransition_ShouldRejectGuardedTransitions(
        IncomingNachaDispatchQueueStatus currentStatus,
        IncomingNachaDispatchEvent transitionEvent,
        string expectedGuardCode)
    {
        var decision = _sut.EvaluateDispatchTransition(currentStatus, transitionEvent);

        Assert.False(decision.IsAllowed);
        Assert.Null(decision.NextStatus);
        Assert.Contains(expectedGuardCode, decision.ResultCode);
    }

    [Fact]
    public void GetAllowedDispatchActions_ShouldExposeActionMatrixForFutureSpa()
    {
        var blocked = _sut.GetAllowedDispatchActions(IncomingNachaDispatchQueueStatus.Blocked);
        var confirmed = _sut.GetAllowedDispatchActions(IncomingNachaDispatchQueueStatus.Confirmed);
        var failedFinal = _sut.GetAllowedDispatchActions(IncomingNachaDispatchQueueStatus.FailedFinal);
        var dispatching = _sut.GetAllowedDispatchActions(IncomingNachaDispatchQueueStatus.Dispatching);
        var waitingWindow = _sut.GetAllowedDispatchActions(IncomingNachaDispatchQueueStatus.WaitingWindow);
        var retryPending = _sut.GetAllowedDispatchActions(IncomingNachaDispatchQueueStatus.RetryPending);

        Assert.False(blocked.CanRetry);
        Assert.True(blocked.CanUnblock);
        Assert.Contains("mark-failed-final", blocked.AllowedActions);

        Assert.False(confirmed.CanRetry);
        Assert.False(confirmed.CanUnblock);
        Assert.Empty(confirmed.AllowedActions);

        Assert.False(failedFinal.CanRequeue);
        Assert.False(failedFinal.CanMarkFailedFinal);
        Assert.Empty(failedFinal.AllowedActions);

        Assert.False(dispatching.CanRetry);
        Assert.False(dispatching.CanUnblock);
        Assert.False(dispatching.CanRequeue);
        Assert.False(dispatching.CanMarkFailedFinal);

        Assert.False(waitingWindow.CanRetry);
        Assert.True(waitingWindow.CanRequeue);
        Assert.True(waitingWindow.CanMarkFailedFinal);

        Assert.True(retryPending.CanRetry);
        Assert.True(retryPending.CanRequeue);
        Assert.True(retryPending.CanMarkFailedFinal);
    }
}
