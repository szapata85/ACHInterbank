using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaSoapResilienceAndIdempotencyTests
{
    [Fact]
    public void IdempotencyKey_ShouldBeDeterministicForSameRequest()
    {
        var first = NachaSoapResilientExecutor.BuildIdempotencyKey(Request(), Context());
        var second = NachaSoapResilientExecutor.BuildIdempotencyKey(Request(), Context());

        Assert.Equal(first, second);
    }

    [Fact]
    public void IdempotencyKey_ShouldChangeForDifferentTraceNumber()
    {
        var first = NachaSoapResilientExecutor.BuildIdempotencyKey(Request(), Context());
        var second = NachaSoapResilientExecutor.BuildIdempotencyKey(Request(entryTrace: "123456780000999"), Context());

        Assert.NotEqual(first, second);
    }

    [Fact]
    public async Task IdempotencyStore_ShouldDetectDuplicateCompletedRequest()
    {
        var store = new NachaSoapInMemoryIdempotencyStore();
        var record = Record("IDEMP-001");

        var begin = await store.TryBeginAsync(record);
        await store.CompleteAsync(record.IdempotencyKey, Result(record.IdempotencyKey, NachaSoapIdempotencyStatus.Completed));
        var duplicate = await store.TryBeginAsync(Record("IDEMP-001"));

        Assert.True(begin.CanExecute);
        Assert.False(duplicate.CanExecute);
        Assert.Equal(NachaSoapIdempotencyStatus.Completed, duplicate.Record.Status);
    }

    [Fact]
    public async Task ResilientExecutor_ShouldSkipDuplicateWithoutReexecutingGateway()
    {
        var auditor = new NachaSoapInMemoryAttemptAuditor();
        var executor = Executor(auditor: auditor);

        var first = await executor.ExecuteAsync(Request(), Context(), Policy());
        var second = await executor.ExecuteAsync(Request(), Context(), Policy());

        Assert.True(first.IsSuccess);
        Assert.True(second.WasSkipped);
        Assert.Equal(NachaSoapIdempotencyStatus.SkippedDuplicate, second.FinalStatus);
        Assert.False(second.WasExecuted);
    }

    [Fact]
    public async Task ResilientExecutor_ShouldRecordDuplicateAuditEvent()
    {
        var auditor = new NachaSoapInMemoryAttemptAuditor();
        var executor = Executor(auditor: auditor);
        var key = NachaSoapResilientExecutor.BuildIdempotencyKey(Request(), Context());

        await executor.ExecuteAsync(Request(), Context(), Policy());
        await executor.ExecuteAsync(Request(), Context(), Policy());
        var attempts = await auditor.GetAttemptsAsync(key);

        Assert.Contains(attempts, x => x.Classification == NachaSoapFailureClassification.DuplicateBlocked);
    }

    [Fact]
    public async Task AttemptAuditor_ShouldRecordSuccessfulAttempt()
    {
        var auditor = new NachaSoapInMemoryAttemptAuditor();
        var executor = Executor(auditor: auditor);

        var result = await executor.ExecuteAsync(Request(), Context(), Policy());

        Assert.Single(result.Attempts);
        Assert.True(result.Attempts[0].IsSuccess);
    }

    [Fact]
    public async Task AttemptAuditor_ShouldRecordSoapFaultAttempt()
    {
        var result = await Executor().ExecuteAsync(
            Request(scenario: Scenario(fault: true, faultCode: "SOAP-NONRETRY")),
            Context(),
            Policy(retrySoapFault: false));

        Assert.True(result.Attempts[0].IsSoapFault);
        Assert.Equal(NachaSoapFailureClassification.SoapFault, result.Attempts[0].Classification);
    }

    [Fact]
    public async Task AttemptAuditor_ShouldRecordTimeoutAttempt()
    {
        var result = await Executor().ExecuteAsync(Request(scenario: Scenario(timeout: true)), Context(), Policy(retryTimeout: false));

        Assert.True(result.Attempts[0].IsTimeout);
        Assert.Equal(NachaSoapFailureClassification.Timeout, result.Attempts[0].Classification);
    }

    [Fact]
    public async Task AttemptAudit_ShouldIncludePhase6B5()
    {
        var result = await Executor().ExecuteAsync(Request(), Context(), Policy());

        Assert.Equal("6B.5", result.Attempts[0].Phase);
    }

    [Fact]
    public async Task AttemptAudit_ShouldKeepProductiveExecutionFalse()
    {
        var result = await Executor().ExecuteAsync(Request(), Context(), Policy());

        Assert.False(result.ProductiveExecution);
        Assert.False(result.Attempts[0].ProductiveExecution);
    }

    [Fact]
    public async Task AttemptAudit_ShouldNotExposeSensitiveData()
    {
        var result = await Executor().ExecuteAsync(Request(), Context(), Policy());
        var joined = string.Join("|", result.Attempts[0].SanitizedRequestSummary.Values.Concat(result.Attempts[0].SanitizedResponseSummary.Values));

        Assert.DoesNotContain("1234567890123456", joined);
        Assert.DoesNotContain("password", joined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", joined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Policy_ShouldRetryTimeoutWhenEnabled()
    {
        var evaluator = new NachaSoapResiliencePolicyEvaluator();

        Assert.True(evaluator.ShouldRetry(Execution(NachaSoapExecutionStatus.SimulatedTimeout, timeout: true), Policy(maxAttempts: 2, retryTimeout: true), 1));
    }

    [Fact]
    public void Policy_ShouldNotRetryTimeoutWhenDisabled()
    {
        var evaluator = new NachaSoapResiliencePolicyEvaluator();

        Assert.False(evaluator.ShouldRetry(Execution(NachaSoapExecutionStatus.SimulatedTimeout, timeout: true), Policy(maxAttempts: 2, retryTimeout: false), 1));
    }

    [Fact]
    public void Policy_ShouldRetryTransientSoapFaultWhenEnabled()
    {
        var evaluator = new NachaSoapResiliencePolicyEvaluator();

        Assert.True(evaluator.ShouldRetry(Execution(NachaSoapExecutionStatus.SimulatedSoapFault, fault: true, faultCode: "SOAP-TRANSIENT"), Policy(maxAttempts: 2, retrySoapFault: true), 1));
    }

    [Fact]
    public void Policy_ShouldNotRetryNonRetryableFault()
    {
        var evaluator = new NachaSoapResiliencePolicyEvaluator();

        Assert.False(evaluator.ShouldRetry(Execution(NachaSoapExecutionStatus.SimulatedSoapFault, fault: true, faultCode: "SOAP-BUSINESS"), Policy(maxAttempts: 2, retrySoapFault: true, nonRetryable: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SOAP-BUSINESS" }), 1));
    }

    [Fact]
    public void Policy_ShouldNotRetryValidationFailure()
    {
        var evaluator = new NachaSoapResiliencePolicyEvaluator();

        Assert.False(evaluator.ShouldRetry(Execution(NachaSoapExecutionStatus.Rejected), Policy(maxAttempts: 2, retryTransient: true), 1));
    }

    [Fact]
    public void Policy_ShouldCalculateBackoffWithoutSleeping()
    {
        var evaluator = new NachaSoapResiliencePolicyEvaluator();

        var delay = evaluator.CalculateDelayMs(Policy(maxAttempts: 3, baseDelay: 100, maxDelay: 150), 2);

        Assert.Equal(150, delay);
    }

    [Fact]
    public async Task ResilientExecutor_ShouldSucceedOnFirstAttempt()
    {
        var result = await Executor().ExecuteAsync(Request(), Context(), Policy());

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.AttemptCount);
        Assert.Equal(NachaSoapIdempotencyStatus.Completed, result.FinalStatus);
    }

    [Fact]
    public async Task ResilientExecutor_ShouldRetryTimeoutAndThenSucceed()
    {
        var request = Request(attempts: [Scenario(timeout: true), Scenario(reference: "SIM-OK")]);

        var result = await Executor().ExecuteAsync(request, Context(), Policy(maxAttempts: 2, retryTimeout: true));

        Assert.True(result.IsSuccess);
        Assert.True(result.WasRetried);
        Assert.Equal(2, result.AttemptCount);
    }

    [Fact]
    public async Task ResilientExecutor_ShouldRetrySoapFaultAndThenSucceed()
    {
        var request = Request(attempts: [Scenario(fault: true, faultCode: "SOAP-TRANSIENT"), Scenario(reference: "SIM-OK")]);

        var result = await Executor().ExecuteAsync(request, Context(), Policy(maxAttempts: 2, retrySoapFault: true));

        Assert.True(result.IsSuccess);
        Assert.True(result.WasRetried);
    }

    [Fact]
    public async Task ResilientExecutor_ShouldStopAfterMaxAttempts()
    {
        var request = Request(attempts: [Scenario(timeout: true), Scenario(timeout: true), Scenario(reference: "SIM-LATE")]);

        var result = await Executor().ExecuteAsync(request, Context(), Policy(maxAttempts: 2, retryTimeout: true));

        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.AttemptCount);
    }

    [Fact]
    public async Task ResilientExecutor_ShouldReturnFailureAfterRetriesExhausted()
    {
        var result = await Executor().ExecuteAsync(Request(scenario: Scenario(timeout: true)), Context(), Policy(maxAttempts: 2, retryTimeout: true));

        Assert.Equal(NachaSoapIdempotencyStatus.Failed, result.FinalStatus);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ResilientExecutor_ShouldBlockProductiveExecutionTrue()
    {
        var result = await Executor().ExecuteAsync(Request(options: Options(productive: true)), Context(), Policy(maxAttempts: 2, retryTransient: true));

        Assert.Equal(NachaSoapIdempotencyStatus.BlockedByNoGo, result.FinalStatus);
        Assert.Single(result.Attempts);
    }

    [Fact]
    public async Task ResilientExecutor_ShouldBlockExternalSoapInvocationTrue()
    {
        var result = await Executor().ExecuteAsync(Request(options: Options(allowExternal: true)), Context(), Policy(maxAttempts: 2, retryTransient: true));

        Assert.Equal(NachaSoapIdempotencyStatus.BlockedByNoGo, result.FinalStatus);
        Assert.False(result.WasExecuted);
    }

    [Fact]
    public async Task ResilientExecutor_ShouldSkipNoneOperation()
    {
        var result = await Executor().ExecuteAsync(Request(operation: NachaSoapOperationCandidate.None, decisionType: NachaIncomingDecisionType.ManualReviewRequired, monetary: false), Context(), Policy());

        Assert.True(result.WasSkipped);
        Assert.Equal(0, result.AttemptCount);
    }

    [Fact]
    public async Task ResilientExecutor_ShouldSkipManualReviewRequired()
    {
        var result = await Executor().ExecuteAsync(Request(decisionType: NachaIncomingDecisionType.ManualReviewRequired), Context(), Policy());

        Assert.True(result.WasSkipped);
        Assert.Equal(NachaSoapIdempotencyStatus.ManualReviewRequired, result.FinalStatus);
    }

    [Fact]
    public async Task ResilientExecutor_ShouldKeepWasExecutedFalse()
    {
        var result = await Executor().ExecuteAsync(Request(), Context(), Policy());

        Assert.False(result.WasExecuted);
        Assert.False(result.LastExecutionResult!.WasExecuted);
    }

    [Fact]
    public async Task ResilientExecutor_ShouldKeepProductiveExecutionFalse()
    {
        var result = await Executor().ExecuteAsync(Request(), Context(), Policy());

        Assert.False(result.ProductiveExecution);
        Assert.False(result.LastExecutionResult!.ProductiveExecution);
    }

    [Fact]
    public async Task ResilientExecutor_ShouldReturnAttemptCount()
    {
        var result = await Executor().ExecuteAsync(Request(attempts: [Scenario(timeout: true), Scenario(reference: "SIM-OK")]), Context(), Policy(maxAttempts: 2, retryTimeout: true));

        Assert.Equal(2, result.AttemptCount);
    }

    [Fact]
    public async Task PayloadMappedProcTransacciones_ShouldExecuteWithResilience()
    {
        var result = await Executor().ExecuteAsync(Request(), Context(), Policy());

        Assert.Equal("NachaSoapProcTransaccionesPayload", result.LastExecutionResult!.MappedRequest!.PayloadMapping!.PayloadType);
    }

    [Fact]
    public async Task PayloadMappedRegistrarRespuesta_ShouldExecuteWithResilienceWithoutMonetaryMovement()
    {
        var request = Request(
            operation: NachaSoapOperationCandidate.RegistrarRespuestaTransaccion,
            decisionType: NachaIncomingDecisionType.RegisterDifferentialResponse,
            monetary: false);

        var result = await Executor().ExecuteAsync(request, Context(), Policy());

        Assert.True(result.IsSuccess);
        Assert.False(result.LastExecutionResult!.MappedRequest!.PayloadMapping!.RequiresMonetaryMovement);
    }

    [Fact]
    public async Task SimulatedGatewayExistingTests_ShouldRemainCompatible()
    {
        var gateway = Gateway();

        var result = await gateway.ExecuteAsync(Request(), Context());

        Assert.Equal(NachaSoapExecutionStatus.SimulatedSuccess, result.Status);
        Assert.False(result.SoapWasInvoked);
    }

    private static INachaSoapResilientExecutor Executor(
        INachaSoapIdempotencyStore? store = null,
        INachaSoapAttemptAuditor? auditor = null)
        => new NachaSoapResilientExecutor(
            Gateway(),
            store ?? new NachaSoapInMemoryIdempotencyStore(),
            auditor ?? new NachaSoapInMemoryAttemptAuditor(),
            new NachaSoapResiliencePolicyEvaluator());

    private static INachaSoapSimulatedGateway Gateway()
        => new NachaSoapSimulatedGateway(
            [new NachaSoapMockOperationAdapter()],
            new NachaSoapRequestMapper(new NachaSoapPayloadMapper()));

    private static NachaSoapExecutionRequest Request(
        NachaSoapOperationCandidate operation = NachaSoapOperationCandidate.ProcTransacciones,
        NachaIncomingDecisionType decisionType = NachaIncomingDecisionType.ApplyCreditMovement,
        bool monetary = true,
        string entryTrace = "123456780000001",
        NachaSoapSimulationScenario? scenario = null,
        IReadOnlyList<NachaSoapSimulationScenario>? attempts = null,
        NachaSoapSimulationOptions? options = null)
        => new()
        {
            Decision = new NachaIncomingDecision
            {
                EntryTraceNumber = entryTrace,
                OriginalTraceNumber = "123456780000000",
                TransactionId = decisionType == NachaIncomingDecisionType.ManualReviewRequired ? null : 123,
                PrenotificationId = decisionType is NachaIncomingDecisionType.ApprovePrenotification or NachaIncomingDecisionType.RejectPrenotification ? 44 : null,
                DecisionType = decisionType,
                RequiresMonetaryMovement = monetary,
                SoapOperation = operation,
                ReasonCode = "R01",
                ReasonDescription = "UAT",
                NewInternalStatus = "Accepted"
            },
            CorrelationId = "phase-6b5-resilience",
            ClearingHouseCode = "ACH",
            ProfileCode = "OFFICIAL_ACH_ENTRADA_ORIGINAL_V1_0",
            RequestedBy = "test",
            IsEnabled = true,
            DryRun = true,
            PayloadContext = Context(),
            SimulationScenario = scenario ?? Scenario(),
            SimulationAttempts = attempts ?? [],
            SimulationOptions = options ?? Options(),
            Metadata = new Dictionary<string, string> { ["case"] = "resilience" }
        };

    private static NachaSoapExecutionContext Context()
        => new()
        {
            CorrelationId = "phase-6b5-resilience",
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
        bool timeout = false,
        bool fault = false,
        string faultCode = "",
        string reference = "SIM-RES-001")
        => new()
        {
            ScenarioId = "resilience-sim",
            ShouldSucceed = !timeout && !fault,
            ShouldTimeout = timeout,
            ShouldReturnSoapFault = fault,
            SoapFaultCode = faultCode,
            SoapFaultMessage = fault ? "SOAP fault simulado." : string.Empty,
            SimulatedExternalReference = reference,
            SimulatedResponseCode = "00",
            SimulatedResponseMessage = "SIMULATED_OK",
            Metadata = new Dictionary<string, string> { ["scenario"] = "resilience" }
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

    private static NachaSoapRetryPolicy Policy(
        int maxAttempts = 1,
        int baseDelay = 0,
        int maxDelay = 0,
        bool retryTimeout = false,
        bool retrySoapFault = false,
        bool retryTransient = false,
        IReadOnlySet<string>? nonRetryable = null)
        => new()
        {
            MaxAttempts = maxAttempts,
            BaseDelayMs = baseDelay,
            MaxDelayMs = maxDelay,
            RetryOnTimeout = retryTimeout,
            RetryOnSoapFault = retrySoapFault,
            RetryOnTransientFailure = retryTransient,
            NonRetryableErrorCodes = nonRetryable ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        };

    private static NachaSoapExecutionResult Execution(
        NachaSoapExecutionStatus status,
        bool timeout = false,
        bool fault = false,
        string faultCode = "")
        => new()
        {
            Status = status,
            IsTimeout = timeout,
            IsSoapFault = fault,
            SoapFaultCode = faultCode,
            ResponseCode = status.ToString(),
            ResponseMessage = status.ToString()
        };

    private static NachaSoapIdempotencyRecord Record(string idempotencyKey)
        => new()
        {
            IdempotencyKey = idempotencyKey,
            CorrelationId = "corr",
            OperationCandidate = NachaSoapOperationCandidate.ProcTransacciones,
            EntryTraceNumber = "trace",
            RequestHash = "hash",
            FirstSeenAt = new DateTime(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc),
            LastAttemptAt = new DateTime(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc)
        };

    private static NachaSoapResilienceExecutionResult Result(
        string idempotencyKey,
        NachaSoapIdempotencyStatus status)
        => new()
        {
            IdempotencyKey = idempotencyKey,
            FinalStatus = status,
            AttemptCount = 1,
            FinalMessage = status.ToString()
        };
}
