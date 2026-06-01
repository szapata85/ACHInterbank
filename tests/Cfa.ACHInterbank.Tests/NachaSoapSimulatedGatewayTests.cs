using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaSoapSimulatedGatewayTests
{
    [Fact]
    public async Task SimulatedGateway_ShouldExecuteProcContrapartidasAdapterWithoutRealSoap()
    {
        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.ProcContrapartidas, NachaIncomingDecisionType.ApplyDebitMovement, true), Context());

        Assert.Equal(NachaSoapExecutionStatus.SimulatedSuccess, result.Status);
        Assert.False(result.SoapWasInvoked);
        Assert.Equal(NachaSoapOperationCandidate.ProcContrapartidas.ToString(), result.Trace["Operation"]);
    }

    [Fact]
    public async Task SimulatedGateway_ShouldExecuteProcTransaccionesAdapterWithoutRealSoap()
    {
        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true), Context());

        Assert.Equal(NachaSoapExecutionStatus.SimulatedSuccess, result.Status);
        Assert.False(result.SoapWasInvoked);
        Assert.Equal("SIM-001", result.ExternalReference);
    }

    [Fact]
    public async Task SimulatedGateway_ShouldExecuteRegistrarRespuestaAdapterWithoutRealSoap()
    {
        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.RegistrarRespuestaTransaccion, NachaIncomingDecisionType.RegisterDifferentialResponse, false), Context());

        Assert.Equal(NachaSoapExecutionStatus.SimulatedSuccess, result.Status);
        Assert.False(result.SoapWasInvoked);
        Assert.False(result.MappedRequest!.RequiresMonetaryMovement);
    }

    [Fact]
    public async Task SimulatedGateway_ShouldSetPhase6B5()
    {
        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true), Context());

        Assert.Equal("6B.5", result.Phase);
        Assert.Equal("6B.5", result.Trace["Phase"]);
    }

    [Fact]
    public async Task SimulatedGateway_ShouldSetProductiveExecutionFalse()
    {
        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true), Context());

        Assert.False(result.ProductiveExecution);
        Assert.Equal("false", result.Trace["ProductiveExecution"]);
    }

    [Fact]
    public async Task SimulatedGateway_ShouldSetWasExecutedFalse()
    {
        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true), Context());

        Assert.False(result.WasExecuted);
        Assert.Equal("false", result.Trace["WasExecuted"]);
    }

    [Fact]
    public async Task SimulatedGateway_ShouldMarkSimulatedExecution()
    {
        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true), Context());

        Assert.True(result.SimulatedExecution);
        Assert.Equal("true", result.Trace["SimulatedExecution"]);
    }

    [Fact]
    public async Task SimulatedGateway_ShouldSkipNoneOperation()
    {
        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.None, NachaIncomingDecisionType.ManualReviewRequired, false), Context());

        Assert.Equal(NachaSoapExecutionStatus.Skipped, result.Status);
        Assert.False(result.SoapWasInvoked);
    }

    [Fact]
    public async Task SimulatedGateway_ShouldSkipManualReviewRequired()
    {
        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ManualReviewRequired, true), Context());

        Assert.Equal(NachaSoapExecutionStatus.Skipped, result.Status);
        Assert.Contains("ManualReviewRequired", result.Message);
    }

    [Fact]
    public async Task SimulatedGateway_ShouldFailWhenNoAdapterCanHandleOperation()
    {
        var gateway = new NachaSoapSimulatedGateway([], new NachaSoapRequestMapper(new NachaSoapPayloadMapper()));

        var result = await gateway.ExecuteAsync(Request(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true), Context());

        Assert.Equal(NachaSoapExecutionStatus.Rejected, result.Status);
        Assert.Contains("No existe adapter", result.Message);
    }

    [Fact]
    public async Task SimulatedGateway_ShouldBlockProductiveExecutionTrue()
    {
        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true, options: Options(productive: true)), Context());

        Assert.Equal(NachaSoapExecutionStatus.BlockedByNoGo, result.Status);
        Assert.False(result.SoapWasInvoked);
    }

    [Fact]
    public async Task SimulatedGateway_ShouldBlockAllowExternalSoapInvocationTrue()
    {
        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true, options: Options(allowExternal: true)), Context());

        Assert.Equal(NachaSoapExecutionStatus.BlockedByNoGo, result.Status);
        Assert.Contains("AllowExternalSoapInvocation", result.Message);
    }

    [Fact]
    public async Task SimulatedGateway_ShouldRejectCredentialsInMetadata()
    {
        var request = Request(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true, metadata: new Dictionary<string, string> { ["password"] = "x" });

        var result = await Gateway().ExecuteAsync(request, Context());

        Assert.Equal(NachaSoapExecutionStatus.Rejected, result.Status);
        Assert.Contains("sensibles", result.Message);
    }

    [Fact]
    public async Task SimulatedGateway_ShouldNotExposeSensitiveDataInSummaries()
    {
        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true), Context());
        var joined = string.Join("|", result.RequestSummary.Values.Concat(result.ResponseSummary.Values));

        Assert.DoesNotContain("1234567890123456", joined);
        Assert.DoesNotContain("password", joined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", joined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcContrapartidasAdapter_ShouldRequireMonetaryMovement()
    {
        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.ProcContrapartidas, NachaIncomingDecisionType.ApplyDebitMovement, false), Context());

        Assert.Equal(NachaSoapExecutionStatus.Rejected, result.Status);
    }

    [Fact]
    public async Task ProcTransaccionesAdapter_ShouldRequireMonetaryMovement()
    {
        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, false), Context());

        Assert.Equal(NachaSoapExecutionStatus.Rejected, result.Status);
    }

    [Fact]
    public async Task RegistrarRespuestaAdapter_ShouldRejectMonetaryMovement()
    {
        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.RegistrarRespuestaTransaccion, NachaIncomingDecisionType.RegisterDifferentialResponse, true), Context());

        Assert.Equal(NachaSoapExecutionStatus.Rejected, result.Status);
    }

    [Fact]
    public async Task RegistrarRespuestaAdapter_ShouldNeverRequireMonetaryMovement()
    {
        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.RegistrarRespuestaTransaccion, NachaIncomingDecisionType.RegisterDifferentialResponse, false), Context());

        Assert.False(result.MappedRequest!.RequiresMonetaryMovement);
        Assert.False(result.MappedRequest.PayloadMapping!.RequiresMonetaryMovement);
    }

    [Fact]
    public async Task SimulatedGateway_ShouldReturnSoapFaultWhenScenarioRequestsFault()
    {
        var scenario = Scenario(fault: true, faultCode: "SOAP-500", faultMessage: "Fault UAT");

        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true, scenario: scenario), Context());

        Assert.Equal(NachaSoapExecutionStatus.SimulatedSoapFault, result.Status);
        Assert.True(result.IsSoapFault);
        Assert.Equal("SOAP-500", result.SoapFaultCode);
    }

    [Fact]
    public async Task SimulatedGateway_ShouldReturnTimeoutWhenScenarioRequestsTimeout()
    {
        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true, scenario: Scenario(timeout: true)), Context());

        Assert.Equal(NachaSoapExecutionStatus.SimulatedTimeout, result.Status);
        Assert.True(result.IsTimeout);
        Assert.False(result.SoapWasInvoked);
    }

    [Fact]
    public async Task SimulatedGateway_ShouldReturnControlledFailureForInvalidRequest()
    {
        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyDebitMovement, true), Context());

        Assert.Equal(NachaSoapExecutionStatus.Rejected, result.Status);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task SimulatedGateway_ShouldReturnSimulatedExternalReferenceOnSuccess()
    {
        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true, scenario: Scenario(reference: "EXT-SIM-42")), Context());

        Assert.Equal("EXT-SIM-42", result.ExternalReference);
        Assert.Equal("EXT-SIM-42", result.ResponseSummary["ExternalReference"]);
    }

    [Fact]
    public async Task PayloadMappedProcContrapartidas_ShouldExecuteThroughSimulatedGateway()
    {
        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.ProcContrapartidas, NachaIncomingDecisionType.ApplyDebitMovement, true), Context());

        Assert.Equal("NachaSoapProcContrapartidasPayload", result.MappedRequest!.PayloadMapping!.PayloadType);
        Assert.Equal(NachaSoapExecutionStatus.SimulatedSuccess, result.Status);
    }

    [Fact]
    public async Task PayloadMappedProcTransacciones_ShouldExecuteThroughSimulatedGateway()
    {
        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true), Context());

        Assert.Equal("NachaSoapProcTransaccionesPayload", result.MappedRequest!.PayloadMapping!.PayloadType);
    }

    [Fact]
    public async Task PayloadMappedRegistrarRespuesta_ShouldExecuteThroughSimulatedGateway()
    {
        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.RegistrarRespuestaTransaccion, NachaIncomingDecisionType.RegisterDifferentialResponse, false), Context());

        Assert.Equal("NachaSoapRegistrarRespuestaTransaccionPayload", result.MappedRequest!.PayloadMapping!.PayloadType);
    }

    [Fact]
    public async Task PayloadMappedRegistrarRespuesta_ShouldNotRequireMonetaryMovementThroughGateway()
    {
        var result = await Gateway().ExecuteAsync(Request(NachaSoapOperationCandidate.RegistrarRespuestaTransaccion, NachaIncomingDecisionType.RegisterDifferentialResponse, false), Context());

        Assert.False(result.MappedRequest!.PayloadMapping!.RequiresMonetaryMovement);
    }

    [Fact]
    public async Task DryRunExecutor_ShouldRemainCompatibleWithSimulatedGatewayModels()
    {
        var request = Request(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true);

        var result = await new NachaSoapDryRunOperationExecutor(new NachaSoapRequestMapper(new NachaSoapPayloadMapper())).ExecuteAsync(request);

        Assert.Equal(NachaSoapExecutionStatus.DryRunCompleted, result.Status);
        Assert.False(result.SoapWasInvoked);
    }

    [Fact]
    public void ExistingNachaSoapOperationGatewayTests_ShouldContinuePassing()
    {
        var mapped = new NachaSoapRequestMapper(new NachaSoapPayloadMapper()).Map(Request(NachaSoapOperationCandidate.ProcTransacciones, NachaIncomingDecisionType.ApplyCreditMovement, true));

        Assert.True(mapped.IsExecutable);
        Assert.Equal("Proc_Transacciones", mapped.MethodName);
    }

    private static INachaSoapSimulatedGateway Gateway()
        => new NachaSoapSimulatedGateway(
            [new NachaSoapMockOperationAdapter()],
            new NachaSoapRequestMapper(new NachaSoapPayloadMapper()));

    private static NachaSoapExecutionRequest Request(
        NachaSoapOperationCandidate operation,
        NachaIncomingDecisionType decisionType,
        bool requiresMonetaryMovement,
        NachaSoapSimulationScenario? scenario = null,
        NachaSoapSimulationOptions? options = null,
        IReadOnlyDictionary<string, string>? metadata = null)
        => new()
        {
            Decision = new NachaIncomingDecision
            {
                EntryTraceNumber = "123456780000001",
                OriginalTraceNumber = "123456780000000",
                TransactionId = decisionType == NachaIncomingDecisionType.ManualReviewRequired ? null : 123,
                PrenotificationId = decisionType is NachaIncomingDecisionType.ApprovePrenotification or NachaIncomingDecisionType.RejectPrenotification ? 44 : null,
                DecisionType = decisionType,
                RequiresMonetaryMovement = requiresMonetaryMovement,
                SoapOperation = operation,
                ReasonCode = "R01",
                ReasonDescription = "UAT",
                NewInternalStatus = "Accepted"
            },
            CorrelationId = "phase-6b5-sim",
            ClearingHouseCode = "ACH",
            ProfileCode = "OFFICIAL_ACH_ENTRADA_ORIGINAL_V1_0",
            RequestedBy = "test",
            IsEnabled = true,
            DryRun = true,
            PayloadContext = Context(),
            SimulationScenario = scenario ?? Scenario(),
            SimulationOptions = options ?? Options(),
            Metadata = metadata ?? new Dictionary<string, string> { ["case"] = "sim" }
        };

    private static NachaSoapExecutionContext Context()
        => new()
        {
            CorrelationId = "phase-6b5-sim",
            SourceFileName = "ACH_COL_IN_001.ach",
            ClearingHouseCode = "ACH",
            ProfileCode = "OFFICIAL_ACH_ENTRADA_ORIGINAL_V1_0",
            AmountInCents = 150000,
            Currency = "COP",
            SourceAccountReference = "1234567890123456",
            DestinationAccountReference = "6543210009874321",
            SourceFinancialInstitutionCode = "76543210",
            DestinationFinancialInstitutionCode = "12345678",
            ExternalOriginatorInstitutionCode = "76543210",
            CfaReceiverInstitutionCode = "12345678",
            CreatedAt = new DateTime(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc),
            Metadata = new Dictionary<string, string> { ["accountHint"] = "1234567890123456" }
        };

    private static NachaSoapSimulationScenario Scenario(
        bool fault = false,
        bool timeout = false,
        string faultCode = "",
        string faultMessage = "",
        string reference = "SIM-001")
        => new()
        {
            ScenarioId = "uat-sim",
            ShouldSucceed = !fault && !timeout,
            ShouldReturnSoapFault = fault,
            ShouldTimeout = timeout,
            SoapFaultCode = faultCode,
            SoapFaultMessage = faultMessage,
            SimulatedExternalReference = reference,
            SimulatedResponseCode = "00",
            SimulatedResponseMessage = "SIMULATED_OK",
            Metadata = new Dictionary<string, string> { ["scenario"] = "uat" }
        };

    private static NachaSoapSimulationOptions Options(bool productive = false, bool allowExternal = false)
        => new()
        {
            Enabled = true,
            AllowFaultSimulation = true,
            AllowTimeoutSimulation = true,
            ProductiveExecution = productive,
            AllowExternalSoapInvocation = allowExternal,
            EnvironmentName = "UAT-Sim"
        };
}
