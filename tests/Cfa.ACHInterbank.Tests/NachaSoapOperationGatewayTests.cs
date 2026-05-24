using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaSoapOperationGatewayTests
{
    [Fact]
    public void Map_ProcTransacciones_ShouldRequireMonetaryMovement()
    {
        var mapped = new NachaSoapRequestMapper().Map(BuildRequest(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true));

        Assert.True(mapped.IsExecutable);
        Assert.True(mapped.RequiresMonetaryMovement);
        Assert.Equal("Proc_Transacciones", mapped.MethodName);
    }

    [Fact]
    public void Map_ProcContrapartidas_ShouldRequireMonetaryMovement()
    {
        var mapped = new NachaSoapRequestMapper().Map(BuildRequest(NachaSoapOperationCandidate.ProcContrapartidas, NachaIncomingDecisionType.ApplyDebitMovement, true));

        Assert.True(mapped.IsExecutable);
        Assert.True(mapped.RequiresMonetaryMovement);
        Assert.Equal("Proc_Contrapartidas", mapped.MethodName);
    }

    [Fact]
    public void Map_RegistrarRespuestaTransaccion_ShouldNotRequireMonetaryMovement()
    {
        var mapped = new NachaSoapRequestMapper().Map(BuildRequest(NachaSoapOperationCandidate.RegistrarRespuestaTransaccion, NachaIncomingDecisionType.RegisterDifferentialResponse, false));

        Assert.True(mapped.IsExecutable);
        Assert.False(mapped.RequiresMonetaryMovement);
        Assert.Equal("RegistrarRespuestaTransaccion", mapped.MethodName);
    }

    [Fact]
    public void Map_None_ShouldNotBeExecutable()
    {
        var mapped = new NachaSoapRequestMapper().Map(BuildRequest(NachaSoapOperationCandidate.None, NachaIncomingDecisionType.ManualReviewRequired, false));

        Assert.False(mapped.IsExecutable);
        Assert.False(mapped.WouldInvokeSoap);
        Assert.Contains(mapped.Errors, x => x.Contains("None", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Map_ManualReview_ShouldBeNonMonetaryAndNotExecutable()
    {
        var mapped = new NachaSoapRequestMapper().Map(BuildRequest(NachaSoapOperationCandidate.None, NachaIncomingDecisionType.ManualReviewRequired, false));

        Assert.False(mapped.RequiresMonetaryMovement);
        Assert.False(mapped.IsExecutable);
        Assert.Contains(mapped.Errors, x => x.Contains("ManualReviewRequired", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DryRun_ShouldNotInvokeRealSoap()
    {
        var executor = new NachaSoapDryRunOperationExecutor(new NachaSoapRequestMapper());

        var result = await executor.ExecuteAsync(BuildRequest(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true, isEnabled: true));

        Assert.Equal(NachaSoapExecutionStatus.DryRunCompleted, result.Status);
        Assert.False(result.SoapWasInvoked);
        Assert.False(result.ProductiveExecution);
    }

    [Fact]
    public async Task Disabled_ShouldReturnSkipped()
    {
        var executor = new NachaSoapDryRunOperationExecutor(new NachaSoapRequestMapper());

        var result = await executor.ExecuteAsync(BuildRequest(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true, isEnabled: false));

        Assert.Equal(NachaSoapExecutionStatus.Skipped, result.Status);
        Assert.False(result.SoapWasInvoked);
    }

    [Fact]
    public async Task Result_ShouldIncludePhase6B5()
    {
        var executor = new NachaSoapDryRunOperationExecutor(new NachaSoapRequestMapper());

        var result = await executor.ExecuteAsync(BuildRequest(NachaSoapOperationCandidate.RegistrarRespuestaTransaccion, NachaIncomingDecisionType.RegisterDifferentialResponse, false, isEnabled: true));

        Assert.Equal("6B.5", result.Phase);
        Assert.Equal("6B.5", result.Trace["Phase"]);
        Assert.Equal("6B.5", result.MappedRequest!.Phase);
    }

    private static NachaSoapExecutionRequest BuildRequest(
        NachaSoapOperationCandidate operation,
        NachaIncomingDecisionType decisionType,
        bool requiresMonetaryMovement,
        bool isEnabled = true)
        => new()
        {
            CorrelationId = "phase-6b5-test",
            ClearingHouseCode = "ACH",
            ProfileCode = "OFFICIAL_ACH_ENTRADA_ORIGINAL_V1_0",
            IsEnabled = isEnabled,
            DryRun = true,
            RequestedBy = "test",
            Decision = new NachaIncomingDecision
            {
                EntryTraceNumber = "123456780000001",
                OriginalTraceNumber = "123456780000000",
                TransactionId = 100,
                DecisionType = decisionType,
                RequiresMonetaryMovement = requiresMonetaryMovement,
                SoapOperation = operation,
                ReasonCode = operation == NachaSoapOperationCandidate.RegistrarRespuestaTransaccion ? "R01" : null,
                AuditMessage = "UAT 6B.5"
            }
        };
}
