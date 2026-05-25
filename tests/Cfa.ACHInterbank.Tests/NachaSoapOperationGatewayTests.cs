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

    [Fact]
    public void Map_ProcTransacciones_WithNonCreditDecision_ShouldReject()
    {
        var mapped = new NachaSoapRequestMapper().Map(BuildRequest(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyDebitMovement, true));

        Assert.False(mapped.IsExecutable);
        Assert.Contains(mapped.Errors, x => x.Contains("ApplyCreditMovement", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Map_ProcContrapartidas_WithNonDebitDecision_ShouldReject()
    {
        var mapped = new NachaSoapRequestMapper().Map(BuildRequest(NachaSoapOperationCandidate.ProcContrapartidas, NachaIncomingDecisionType.ApplyCreditMovement, true));

        Assert.False(mapped.IsExecutable);
        Assert.Contains(mapped.Errors, x => x.Contains("ApplyDebitMovement", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Map_RegistrarRespuestaTransaccion_WithMonetaryFlag_ShouldReject()
    {
        var mapped = new NachaSoapRequestMapper().Map(BuildRequest(NachaSoapOperationCandidate.RegistrarRespuestaTransaccion, NachaIncomingDecisionType.RegisterDifferentialResponse, true));

        Assert.False(mapped.IsExecutable);
        Assert.False(mapped.RequiresMonetaryMovement);
        Assert.Contains(mapped.Errors, x => x.Contains("no debe mover dinero", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Map_MissingAuditContext_ShouldReject()
    {
        var mapped = new NachaSoapRequestMapper().Map(BuildRequest(
            NachaSoapOperationCandidate.ProcTransacciones,
            NachaIncomingDecisionType.ApplyCreditMovement,
            true,
            correlationId: string.Empty,
            clearingHouseCode: string.Empty,
            profileCode: string.Empty));

        Assert.False(mapped.IsExecutable);
        Assert.Contains(mapped.Errors, x => x.Contains("CorrelationId", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(mapped.Errors, x => x.Contains("ClearingHouseCode", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(mapped.Errors, x => x.Contains("ProfileCode", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RealExecutionRequest_ShouldBeBlockedByNoGo()
    {
        var executor = new NachaSoapDryRunOperationExecutor(new NachaSoapRequestMapper());

        var result = await executor.ExecuteAsync(BuildRequest(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true, dryRun: false));

        Assert.Equal(NachaSoapExecutionStatus.BlockedByNoGo, result.Status);
        Assert.False(result.SoapWasInvoked);
        Assert.False(result.ProductiveExecution);
        Assert.Contains("NO-GO", result.Message);
    }

    [Fact]
    public void Map_ShouldCarryAuditPayloadWithoutCredentials()
    {
        var mapped = new NachaSoapRequestMapper().Map(BuildRequest(NachaSoapOperationCandidate.RegistrarRespuestaTransaccion, NachaIncomingDecisionType.RegisterDifferentialResponse, false));

        Assert.Equal("false", mapped.Payload["ProductiveExecution"]);
        Assert.Equal("6B.5", mapped.Trace["Phase"]);
        Assert.DoesNotContain(mapped.Payload.Keys, x => x.Contains("Password", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(mapped.Payload.Keys, x => x.Contains("Credential", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(mapped.Payload.Keys, x => x.Contains("Token", StringComparison.OrdinalIgnoreCase));
    }

    private static NachaSoapExecutionRequest BuildRequest(
        NachaSoapOperationCandidate operation,
        NachaIncomingDecisionType decisionType,
        bool requiresMonetaryMovement,
        bool isEnabled = true,
        bool dryRun = true,
        string correlationId = "phase-6b5-test",
        string clearingHouseCode = "ACH",
        string profileCode = "OFFICIAL_ACH_ENTRADA_ORIGINAL_V1_0")
        => new()
        {
            CorrelationId = correlationId,
            ClearingHouseCode = clearingHouseCode,
            ProfileCode = profileCode,
            IsEnabled = isEnabled,
            DryRun = dryRun,
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
