using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaSoapUatOrchestrationTests
{
    [Fact]
    public async Task UatOrchestrator_ShouldCompleteReadinessForProcTransaccionesSimulation()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request());

        Assert.True(result.IsReadyForUat);
        Assert.True(result.PayloadMappingPassed);
        Assert.True(result.SimulationPassed);
    }

    [Fact]
    public async Task UatOrchestrator_ShouldCompleteReadinessForProcContrapartidasSimulation()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request(
            operation: NachaSoapOperationCandidate.ProcContrapartidas,
            decisionType: NachaIncomingDecisionType.ApplyDebitMovement));

        Assert.True(result.IsReadyForUat);
        Assert.True(result.RequiresMonetaryMovement);
    }

    [Fact]
    public async Task UatOrchestrator_ShouldCompleteReadinessForRegistrarRespuestaSimulation()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request(
            operation: NachaSoapOperationCandidate.RegistrarRespuestaTransaccion,
            decisionType: NachaIncomingDecisionType.RegisterDifferentialResponse,
            monetary: false,
            amount: 0));

        Assert.True(result.IsReadyForUat);
        Assert.False(result.RequiresMonetaryMovement);
    }

    [Fact]
    public async Task UatOrchestrator_ShouldSetPhase6B5()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request());

        Assert.Equal("6B.5", result.Phase);
        Assert.All(result.AuditTrail, x => Assert.Equal("6B.5", x.Phase));
    }

    [Fact]
    public async Task UatOrchestrator_ShouldKeepProductiveExecutionFalse()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request());

        Assert.False(result.ProductiveExecution);
        Assert.All(result.AuditTrail, x => Assert.False(x.ProductiveExecution));
    }

    [Fact]
    public async Task UatOrchestrator_ShouldKeepWouldInvokeRealSoapFalse()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request());

        Assert.False(result.WouldInvokeRealSoap);
        Assert.Equal("false", result.Metadata["WouldInvokeRealSoap"]);
    }

    [Fact]
    public async Task UatOrchestrator_ShouldBlockWhenOperationalGateBlocks()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request(options: Options(allowReal: true)));

        Assert.True(result.IsBlocked);
        Assert.Equal(NachaSoapUatReadinessStatus.BlockedByNoGo, result.Status);
    }

    [Fact]
    public async Task UatOrchestrator_ShouldBlockWhenReadinessFails()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request(endpoints: [Endpoint(cert: false)]));

        Assert.True(result.IsBlocked);
        Assert.False(result.ReadinessCheckPassed);
        Assert.Equal(NachaSoapUatReadinessStatus.BlockedByCertificate, result.Status);
    }

    [Fact]
    public async Task UatOrchestrator_ShouldBlockProductiveExecutionTrue()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request(options: Options(productive: true)));

        Assert.True(result.IsBlocked);
        Assert.Equal(NachaSoapUatReadinessStatus.BlockedByNoGo, result.Status);
    }

    [Fact]
    public async Task UatOrchestrator_ShouldBlockRealSoapInvocation()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request(options: Options(allowReal: true)));

        Assert.True(result.IsBlocked);
        Assert.Contains(result.BlockReasons, x => x.Contains("SOAP real", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UatOrchestrator_ShouldBlockProductionEndpoint()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request(endpoints: [Endpoint(production: true, url: "https://prod.example.invalid/soap")]));

        Assert.True(result.IsBlocked);
        Assert.Equal(NachaSoapUatReadinessStatus.BlockedByEndpoint, result.Status);
    }

    [Fact]
    public async Task UatOrchestrator_ShouldSkipNoneOperation()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request(
            operation: NachaSoapOperationCandidate.None,
            decisionType: NachaIncomingDecisionType.ManualReviewRequired,
            monetary: false));

        Assert.True(result.IsBlocked);
        Assert.Equal(NachaSoapUatReadinessStatus.ManualReviewRequired, result.Status);
    }

    [Fact]
    public async Task UatOrchestrator_ShouldSkipManualReviewRequired()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request(decisionType: NachaIncomingDecisionType.ManualReviewRequired));

        Assert.True(result.IsBlocked);
        Assert.Equal(NachaSoapUatReadinessStatus.ManualReviewRequired, result.Status);
    }

    [Fact]
    public async Task UatOrchestrator_ShouldStopWhenPayloadMappingFails()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request(amount: 0));

        Assert.False(result.PayloadMappingPassed);
        Assert.False(result.RequestMappingPassed);
        Assert.DoesNotContain(result.AuditTrail, x => x.EventType == "OperationalGateEvaluated");
    }

    [Fact]
    public async Task UatOrchestrator_ShouldStopWhenRequestMappingFails()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request(clearingHouse: ""));

        Assert.True(result.PayloadMappingPassed);
        Assert.False(result.RequestMappingPassed);
        Assert.DoesNotContain(result.AuditTrail, x => x.EventType == "OperationalGateEvaluated");
    }

    [Fact]
    public async Task UatOrchestrator_ShouldNotRunSimulationWhenGateFails()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request(options: Options(allowReal: true)));

        Assert.False(result.SimulationPassed);
        Assert.DoesNotContain(result.AuditTrail, x => x.EventType == "SimulationStarted");
    }

    [Fact]
    public async Task UatOrchestrator_ShouldNotRunResilienceWhenReadinessFails()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request(endpoints: [Endpoint(cert: false)]));

        Assert.False(result.ResiliencePassed);
        Assert.DoesNotContain(result.AuditTrail, x => x.EventType == "ResilienceEvaluated");
    }

    [Fact]
    public async Task UatOrchestrator_ShouldRunResilienceWhenReadinessPasses()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request());

        Assert.True(result.ResiliencePassed);
        Assert.Contains(result.AuditTrail, x => x.EventType == "ResilienceEvaluated");
    }

    [Fact]
    public async Task UatOrchestrator_ShouldWriteAuditTrail()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request());

        Assert.NotEmpty(result.AuditTrail);
        Assert.Contains(result.AuditTrail, x => x.EventType == "Completed");
    }

    [Fact]
    public async Task UatOrchestrator_AuditTrail_ShouldIncludePayloadMapping()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request());

        Assert.Contains(result.AuditTrail, x => x.EventType == "PayloadMappingStarted");
        Assert.Contains(result.AuditTrail, x => x.EventType == "PayloadMappingCompleted");
    }

    [Fact]
    public async Task UatOrchestrator_AuditTrail_ShouldIncludeOperationalGate()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request());

        Assert.Contains(result.AuditTrail, x => x.EventType == "OperationalGateEvaluated");
    }

    [Fact]
    public async Task UatOrchestrator_AuditTrail_ShouldIncludeReadiness()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request());

        Assert.Contains(result.AuditTrail, x => x.EventType == "ReadinessChecked");
    }

    [Fact]
    public async Task UatOrchestrator_AuditTrail_ShouldIncludeSimulationResult()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request());

        Assert.Contains(result.AuditTrail, x => x.EventType == "SimulationCompleted");
    }

    [Fact]
    public async Task UatOrchestrator_AuditTrail_ShouldNotExposeSensitiveData()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request(metadata: new Dictionary<string, string> { ["accountHint"] = "1234567890123456" }));
        var joined = string.Join("|", result.AuditTrail.SelectMany(x => x.SanitizedDetails.Values));

        Assert.DoesNotContain("1234567890123456", joined);
        Assert.DoesNotContain("password", joined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", joined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UatOrchestrator_ShouldReturnRetrySummaryWhenSimulationRetries()
    {
        var request = Request(
            attempts: [Scenario(timeout: true), Scenario(reference: "SIM-OK")],
            retryPolicy: new NachaSoapRetryPolicy { MaxAttempts = 2, RetryOnTimeout = true });

        var result = await Orchestrator().ExecuteReadinessAsync(request);

        Assert.True(result.ResiliencePassed);
        Assert.Equal("True", result.ResilienceSummary["WasRetried"]);
        Assert.Equal("2", result.ResilienceSummary["AttemptCount"]);
    }

    [Fact]
    public async Task UatOrchestrator_ShouldReturnDuplicateSummaryWhenIdempotencyDetectsDuplicate()
    {
        var store = new NachaSoapInMemoryIdempotencyStore();
        var orchestrator = Orchestrator(store);

        await orchestrator.ExecuteReadinessAsync(Request());
        var duplicate = await orchestrator.ExecuteReadinessAsync(Request());

        Assert.False(duplicate.ResiliencePassed);
        Assert.Equal("SkippedDuplicate", duplicate.ResilienceSummary["FinalStatus"]);
    }

    [Fact]
    public async Task UatOrchestrator_ShouldReturnSoapFaultSummaryWhenSimulationFaults()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request(scenario: Scenario(fault: true, faultCode: "SOAP-NONRETRY")));

        Assert.False(result.ResiliencePassed);
        Assert.Contains("SOAP", result.SimulationSummary["ResponseCode"]);
    }

    [Fact]
    public async Task UatOrchestrator_ShouldReturnTimeoutSummaryWhenSimulationTimesOut()
    {
        var result = await Orchestrator().ExecuteReadinessAsync(Request(scenario: Scenario(timeout: true)));

        Assert.False(result.ResiliencePassed);
        Assert.Equal("TIMEOUT", result.SimulationSummary["ResponseCode"]);
    }

    [Fact]
    public void ExistingSoapPayloadMappingTests_ShouldRemainPassing()
    {
        var mapped = new NachaSoapPayloadMapper().Map(Request().Decision, Context());

        Assert.True(mapped.IsMapped);
    }

    [Fact]
    public async Task ExistingSoapSimulatedGatewayTests_ShouldRemainPassing()
    {
        var result = await Gateway().ExecuteAsync(ExecutionRequest(Request()), Context());

        Assert.Equal(NachaSoapExecutionStatus.SimulatedSuccess, result.Status);
    }

    [Fact]
    public async Task ExistingSoapResilienceTests_ShouldRemainPassing()
    {
        var executor = ResilientExecutor();

        var result = await executor.ExecuteAsync(ExecutionRequest(Request()), Context(), new NachaSoapRetryPolicy());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ExistingUatOperationalControlTests_ShouldRemainPassing()
    {
        var readiness = Readiness().CheckReadiness("compat", Options(), [Endpoint()]);

        Assert.True(readiness.IsReady);
    }

    private static NachaSoapUatOrchestrator Orchestrator(NachaSoapInMemoryIdempotencyStore? store = null)
        => new(
            new NachaSoapPayloadMapper(),
            new NachaSoapRequestMapper(new NachaSoapPayloadMapper()),
            Readiness(),
            new NachaSoapOperationalGate(new NachaSoapEndpointSafetyValidator()),
            ResilientExecutor(store),
            new NachaBlockedRealSoapClientAdapter());

    private static NachaSoapResilientExecutor ResilientExecutor(NachaSoapInMemoryIdempotencyStore? store = null)
        => new(
            Gateway(),
            store ?? new NachaSoapInMemoryIdempotencyStore(),
            new NachaSoapInMemoryAttemptAuditor(),
            new NachaSoapResiliencePolicyEvaluator());

    private static NachaSoapSimulatedGateway Gateway()
        => new([new NachaSoapMockOperationAdapter()], new NachaSoapRequestMapper(new NachaSoapPayloadMapper()));

    private static NachaSoapUatReadinessChecker Readiness()
        => new(new NachaSoapEndpointSafetyValidator(), new NachaSoapCertificateReadinessValidator());

    private static NachaSoapUatReadinessRequest Request(
        NachaSoapOperationCandidate operation = NachaSoapOperationCandidate.ProcTransacciones,
        NachaIncomingDecisionType decisionType = NachaIncomingDecisionType.ApplyCreditMovement,
        bool monetary = true,
        long amount = 150000,
        string clearingHouse = "ACH",
        NachaSoapUatControlOptions? options = null,
        IReadOnlyList<NachaSoapEndpointDescriptor>? endpoints = null,
        NachaSoapSimulationScenario? scenario = null,
        IReadOnlyList<NachaSoapSimulationScenario>? attempts = null,
        NachaSoapRetryPolicy? retryPolicy = null,
        IReadOnlyDictionary<string, string>? metadata = null)
        => new()
        {
            CorrelationId = "phase-6b5-uat-orch",
            SourceFileName = "ACH_COL_IN_001.ach",
            ClearingHouseCode = clearingHouse,
            ProfileCode = "OFFICIAL_ACH_ENTRADA_ORIGINAL_V1_0",
            Decision = Decision(operation, decisionType, monetary),
            ExecutionContext = Context(amount: amount),
            UatControlOptions = options ?? Options(),
            SimulationOptions = new NachaSoapSimulationOptions(),
            SimulationScenario = scenario ?? Scenario(),
            SimulationAttempts = attempts ?? [],
            RetryPolicy = retryPolicy ?? new NachaSoapRetryPolicy(),
            EndpointDescriptors = endpoints ?? [Endpoint()],
            RequestedBy = "test",
            RequestedAt = new DateTime(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc),
            Metadata = metadata ?? new Dictionary<string, string> { ["case"] = "orchestration" }
        };

    private static NachaIncomingDecision Decision(
        NachaSoapOperationCandidate operation,
        NachaIncomingDecisionType decisionType,
        bool monetary)
        => new()
        {
            EntryTraceNumber = "123456780000001",
            OriginalTraceNumber = "123456780000000",
            TransactionId = decisionType == NachaIncomingDecisionType.ManualReviewRequired ? null : 123,
            PrenotificationId = decisionType is NachaIncomingDecisionType.ApprovePrenotification or NachaIncomingDecisionType.RejectPrenotification ? 44 : null,
            DecisionType = decisionType,
            RequiresMonetaryMovement = monetary,
            SoapOperation = operation,
            ReasonCode = "R01",
            ReasonDescription = "UAT",
            NewInternalStatus = "Accepted"
        };

    private static NachaSoapExecutionContext Context(long amount = 150000)
        => new()
        {
            CorrelationId = "phase-6b5-uat-orch",
            SourceFileName = "ACH_COL_IN_001.ach",
            ClearingHouseCode = "ACH",
            ProfileCode = "OFFICIAL_ACH_ENTRADA_ORIGINAL_V1_0",
            AmountInCents = amount,
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

    private static NachaSoapExecutionRequest ExecutionRequest(NachaSoapUatReadinessRequest request)
        => new()
        {
            Decision = request.Decision,
            CorrelationId = request.CorrelationId,
            ClearingHouseCode = request.ClearingHouseCode,
            ProfileCode = request.ProfileCode,
            IsEnabled = true,
            DryRun = true,
            PayloadContext = request.ExecutionContext,
            SimulationScenario = request.SimulationScenario,
            SimulationOptions = request.SimulationOptions,
            Metadata = request.Metadata
        };

    private static NachaSoapUatControlOptions Options(
        bool productive = false,
        bool allowReal = false,
        NachaSoapOperationalMode mode = NachaSoapOperationalMode.Simulated)
        => new()
        {
            Enabled = true,
            EnvironmentName = "UAT",
            ProductiveExecution = productive,
            AllowRealSoapInvocation = allowReal,
            AllowUatEndpoints = true,
            RequireCertificateValidation = true,
            Mode = mode
        };

    private static NachaSoapEndpointDescriptor Endpoint(
        bool production = false,
        bool cert = true,
        string url = "https://uat-ach-gateway.example.invalid/soap")
        => new()
        {
            OperationCandidate = NachaSoapOperationCandidate.ProcTransacciones,
            EnvironmentName = production ? "Production" : "UAT",
            EndpointName = "ACH-UAT-SOAP",
            EndpointUrl = url,
            IsProduction = production,
            IsUat = !production,
            IsEnabled = true,
            RequiresClientCertificate = cert,
            CertificateThumbprint = cert ? "AABBCCDDEEFF00112233445566778899" : string.Empty,
            CertificateStoreName = cert ? "My" : string.Empty,
            CertificateStoreLocation = cert ? "CurrentUser" : string.Empty
        };

    private static NachaSoapSimulationScenario Scenario(
        bool timeout = false,
        bool fault = false,
        string faultCode = "",
        string reference = "SIM-ORCH-001")
        => new()
        {
            ScenarioId = "uat-orchestration",
            ShouldSucceed = !timeout && !fault,
            ShouldTimeout = timeout,
            ShouldReturnSoapFault = fault,
            SoapFaultCode = faultCode,
            SoapFaultMessage = fault ? "SOAP fault simulado." : string.Empty,
            SimulatedExternalReference = reference,
            SimulatedResponseCode = "00",
            SimulatedResponseMessage = "SIMULATED_OK"
        };
}
