using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaSoapUatOrchestrator : INachaSoapUatOrchestrator
{
    private readonly INachaSoapPayloadMapper _payloadMapper;
    private readonly INachaSoapRequestMapper _requestMapper;
    private readonly INachaSoapUatReadinessChecker _readinessChecker;
    private readonly INachaSoapOperationalGate _operationalGate;
    private readonly INachaSoapResilientExecutor _resilientExecutor;
    private readonly INachaRealSoapClientAdapter _blockedRealSoapClientAdapter;

    public NachaSoapUatOrchestrator(
        INachaSoapPayloadMapper payloadMapper,
        INachaSoapRequestMapper requestMapper,
        INachaSoapUatReadinessChecker readinessChecker,
        INachaSoapOperationalGate operationalGate,
        INachaSoapResilientExecutor resilientExecutor,
        INachaRealSoapClientAdapter blockedRealSoapClientAdapter)
    {
        _payloadMapper = payloadMapper;
        _requestMapper = requestMapper;
        _readinessChecker = readinessChecker;
        _operationalGate = operationalGate;
        _resilientExecutor = resilientExecutor;
        _blockedRealSoapClientAdapter = blockedRealSoapClientAdapter;
    }

    public async Task<NachaSoapUatReadinessResult> ExecuteReadinessAsync(
        NachaSoapUatReadinessRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var audit = new List<NachaSoapUatAuditEvent>();
        var warnings = new List<string>();
        var errors = new List<string>();
        var blockReasons = new List<string>();
        var operation = request.Decision.SoapOperation;
        var executionRequest = BuildExecutionRequest(request);
        var _ = _blockedRealSoapClientAdapter;

        audit.Add(Event(request, "PayloadMappingStarted", "Inicia mapping de payload SOAP interno."));
        var payloadMapping = _payloadMapper.Map(request.Decision, request.ExecutionContext);
        audit.Add(Event(
            request,
            "PayloadMappingCompleted",
            payloadMapping.IsMapped ? "Payload SOAP interno mapeado." : "Payload SOAP interno no mapeado.",
            !payloadMapping.IsMapped,
            payloadMapping.SanitizedSummary));

        if (!payloadMapping.IsMapped || !payloadMapping.IsExecutable)
        {
            errors.AddRange(payloadMapping.Errors);
            warnings.AddRange(payloadMapping.Warnings);
            blockReasons.AddRange(payloadMapping.Errors.Count == 0 ? ["Payload mapping no ejecutable."] : payloadMapping.Errors);
            return BuildResult(
                request,
                payloadMapping,
                null,
                null,
                null,
                null,
                audit,
                warnings,
                errors,
                blockReasons,
                StatusFromDecision(request));
        }

        audit.Add(Event(request, "RequestMappingStarted", "Inicia mapping de request SOAP interno."));
        var mappedRequest = _requestMapper.Map(executionRequest);
        audit.Add(Event(
            request,
            "RequestMappingCompleted",
            mappedRequest.IsExecutable ? "Request SOAP interno mapeado." : "Request SOAP interno no ejecutable.",
            !mappedRequest.IsExecutable,
            mappedRequest.Trace));

        if (!mappedRequest.IsExecutable)
        {
            errors.AddRange(mappedRequest.Errors);
            blockReasons.AddRange(mappedRequest.Errors.Count == 0 ? ["Request SOAP interno no ejecutable."] : mappedRequest.Errors);
            return BuildResult(
                request,
                payloadMapping,
                mappedRequest,
                null,
                null,
                null,
                audit,
                warnings,
                errors,
                blockReasons,
                StatusFromDecision(request));
        }

        var gate = _operationalGate.Evaluate(executionRequest, request.UatControlOptions, request.EndpointDescriptors);
        audit.Add(Event(
            request,
            "OperationalGateEvaluated",
            gate.IsAllowed ? "Compuerta operacional permite continuar." : "Compuerta operacional bloquea.",
            gate.IsBlocked,
            gate.Audit));

        if (gate.IsBlocked)
        {
            blockReasons.Add(gate.BlockReason);
            errors.Add(gate.BlockReason);
            if (gate.BlockReason.Contains("NO-GO", StringComparison.OrdinalIgnoreCase)
                || gate.BlockReason.Contains("SOAP real", StringComparison.OrdinalIgnoreCase)
                || gate.BlockReason.Contains("ProductiveExecution", StringComparison.OrdinalIgnoreCase))
            {
                audit.Add(Event(request, "BlockedByNoGo", "Flujo bloqueado por NO-GO.", true));
            }

            return BuildResult(
                request,
                payloadMapping,
                mappedRequest,
                gate,
                null,
                null,
                audit,
                warnings,
                errors,
                blockReasons,
                StatusFromGate(gate));
        }

        var readiness = _readinessChecker.CheckReadiness(
            request.CorrelationId,
            request.UatControlOptions,
            request.EndpointDescriptors);
        audit.Add(Event(
            request,
            "ReadinessChecked",
            readiness.IsReady ? "Readiness UAT aprobado." : "Readiness UAT bloqueado.",
            readiness.IsBlocked,
            ReadinessSummary(readiness)));
        warnings.AddRange(readiness.Warnings);

        if (readiness.IsBlocked)
        {
            errors.AddRange(readiness.Errors);
            blockReasons.Add(readiness.BlockReason);
            return BuildResult(
                request,
                payloadMapping,
                mappedRequest,
                gate,
                readiness,
                null,
                audit,
                warnings,
                errors,
                blockReasons,
                StatusFromReadiness(readiness));
        }

        audit.Add(Event(request, "SimulationStarted", "Inicia simulacion resiliente SOAP/UAT."));
        var resilience = await _resilientExecutor.ExecuteAsync(
            executionRequest,
            request.ExecutionContext,
            request.RetryPolicy,
            cancellationToken);
        audit.Add(Event(
            request,
            "SimulationCompleted",
            resilience.IsSuccess ? "Simulacion resiliente completada." : "Simulacion resiliente no exitosa.",
            !resilience.IsSuccess,
            ResilienceSummary(resilience)));
        audit.Add(Event(request, "ResilienceEvaluated", "Resultado de resiliencia consolidado.", !resilience.IsSuccess, ResilienceSummary(resilience)));

        if (!resilience.IsSuccess)
        {
            errors.Add(resilience.FinalMessage);
            blockReasons.Add(resilience.FinalMessage);
        }

        audit.Add(Event(request, "Completed", "Readiness operacional UAT finalizado.", !resilience.IsSuccess));
        return BuildResult(
            request,
            payloadMapping,
            mappedRequest,
            gate,
            readiness,
            resilience,
            audit,
            warnings,
            errors,
            blockReasons,
            resilience.IsSuccess ? StatusFromMode(request.UatControlOptions.Mode) : NachaSoapUatReadinessStatus.Failed);
    }

    private static NachaSoapExecutionRequest BuildExecutionRequest(NachaSoapUatReadinessRequest request)
        => new()
        {
            Decision = request.Decision,
            CorrelationId = request.CorrelationId,
            ClearingHouseCode = request.ClearingHouseCode,
            ProfileCode = request.ProfileCode,
            RequestedBy = request.RequestedBy,
            IsEnabled = true,
            DryRun = true,
            PayloadContext = request.ExecutionContext,
            SimulationScenario = request.SimulationScenario,
            SimulationAttempts = request.SimulationAttempts,
            SimulationOptions = request.SimulationOptions,
            Metadata = request.Metadata
        };

    private static NachaSoapUatReadinessResult BuildResult(
        NachaSoapUatReadinessRequest request,
        NachaSoapPayloadMappingResult payloadMapping,
        NachaSoapMappedRequest? mappedRequest,
        NachaSoapOperationalGateResult? gate,
        NachaSoapReadinessCheckResult? readiness,
        NachaSoapResilienceExecutionResult? resilience,
        IReadOnlyList<NachaSoapUatAuditEvent> audit,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> errors,
        IReadOnlyList<string> blockReasons,
        NachaSoapUatReadinessStatus status)
    {
        var blocked = blockReasons.Any(x => !string.IsNullOrWhiteSpace(x));
        return new NachaSoapUatReadinessResult
        {
            CorrelationId = request.CorrelationId,
            OperationCandidate = request.Decision.SoapOperation,
            Status = status,
            IsReadyForUat = !blocked && resilience?.IsSuccess == true,
            IsBlocked = blocked,
            BlockReasons = blockReasons.Where(x => !string.IsNullOrWhiteSpace(x)).ToList(),
            PayloadMappingPassed = payloadMapping.IsMapped && payloadMapping.IsExecutable,
            RequestMappingPassed = mappedRequest?.IsExecutable == true,
            OperationalGatePassed = gate?.IsAllowed == true,
            ReadinessCheckPassed = readiness?.IsReady == true,
            SimulationPassed = resilience?.IsSuccess == true,
            ResiliencePassed = resilience?.IsSuccess == true,
            WouldInvokeRealSoap = false,
            ProductiveExecution = false,
            RequiresMonetaryMovement = payloadMapping.RequiresMonetaryMovement,
            PayloadSummary = payloadMapping.SanitizedSummary,
            RequestSummary = Sanitize(mappedRequest?.Trace ?? new Dictionary<string, string>()),
            ReadinessSummary = readiness is null ? new Dictionary<string, string>() : ReadinessSummary(readiness),
            SimulationSummary = resilience?.LastExecutionResult?.ResponseSummary is null
                ? new Dictionary<string, string>()
                : Sanitize(resilience.LastExecutionResult.ResponseSummary),
            ResilienceSummary = resilience is null ? new Dictionary<string, string>() : ResilienceSummary(resilience),
            AuditTrail = audit,
            Warnings = warnings,
            Errors = errors,
            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["RequestedBy"] = request.RequestedBy,
                ["SourceFileName"] = request.SourceFileName,
                ["Productivo"] = "NO-GO",
                ["WouldInvokeRealSoap"] = "false"
            }
        };
    }

    private static NachaSoapUatAuditEvent Event(
        NachaSoapUatReadinessRequest request,
        string eventType,
        string message,
        bool blocked = false,
        IReadOnlyDictionary<string, string>? details = null)
        => new()
        {
            EventId = $"{request.CorrelationId}-{eventType}",
            CorrelationId = request.CorrelationId,
            EventType = eventType,
            OperationCandidate = request.Decision.SoapOperation,
            Message = message,
            IsBlocked = blocked,
            Severity = blocked ? "Warning" : "Information",
            Timestamp = DateTime.UtcNow,
            ProductiveExecution = false,
            SanitizedDetails = Sanitize(details ?? new Dictionary<string, string>()),
            Metadata = new Dictionary<string, string> { ["Productivo"] = "NO-GO" }
        };

    private static Dictionary<string, string> ReadinessSummary(NachaSoapReadinessCheckResult readiness)
        => Sanitize(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["IsReady"] = readiness.IsReady.ToString(),
            ["IsBlocked"] = readiness.IsBlocked.ToString(),
            ["BlockReason"] = readiness.BlockReason,
            ["EnvironmentName"] = readiness.EnvironmentName,
            ["EndpointChecks"] = readiness.EndpointChecks.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["CertificateChecks"] = readiness.CertificateChecks.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["ProductiveExecution"] = "false",
            ["Phase"] = "6B.5"
        });

    private static Dictionary<string, string> ResilienceSummary(NachaSoapResilienceExecutionResult resilience)
        => Sanitize(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["IsSuccess"] = resilience.IsSuccess.ToString(),
            ["WasRetried"] = resilience.WasRetried.ToString(),
            ["AttemptCount"] = resilience.AttemptCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["FinalStatus"] = resilience.FinalStatus.ToString(),
            ["FinalMessage"] = resilience.FinalMessage,
            ["ProductiveExecution"] = "false",
            ["WasExecuted"] = resilience.WasExecuted.ToString(),
            ["Phase"] = "6B.5"
        });

    private static NachaSoapUatReadinessStatus StatusFromDecision(NachaSoapUatReadinessRequest request)
        => request.Decision.DecisionType == NachaIncomingDecisionType.ManualReviewRequired
           || request.Decision.SoapOperation == NachaSoapOperationCandidate.None
            ? NachaSoapUatReadinessStatus.ManualReviewRequired
            : NachaSoapUatReadinessStatus.BlockedByValidation;

    private static NachaSoapUatReadinessStatus StatusFromGate(NachaSoapOperationalGateResult gate)
    {
        if (gate.BlockReason.Contains("Endpoint", StringComparison.OrdinalIgnoreCase))
        {
            return NachaSoapUatReadinessStatus.BlockedByEndpoint;
        }

        if (gate.BlockReason.Contains("NO-GO", StringComparison.OrdinalIgnoreCase)
            || gate.BlockReason.Contains("SOAP real", StringComparison.OrdinalIgnoreCase)
            || gate.BlockReason.Contains("ProductiveExecution", StringComparison.OrdinalIgnoreCase))
        {
            return NachaSoapUatReadinessStatus.BlockedByNoGo;
        }

        return NachaSoapUatReadinessStatus.BlockedByConfiguration;
    }

    private static NachaSoapUatReadinessStatus StatusFromReadiness(NachaSoapReadinessCheckResult readiness)
    {
        if (readiness.EndpointChecks.Any(x => x.IsBlocked))
        {
            return NachaSoapUatReadinessStatus.BlockedByEndpoint;
        }

        if (readiness.CertificateChecks.Any(x => x.IsBlocked))
        {
            return NachaSoapUatReadinessStatus.BlockedByCertificate;
        }

        return NachaSoapUatReadinessStatus.BlockedByConfiguration;
    }

    private static NachaSoapUatReadinessStatus StatusFromMode(NachaSoapOperationalMode mode)
        => mode switch
        {
            NachaSoapOperationalMode.DryRun => NachaSoapUatReadinessStatus.ReadyForDryRun,
            NachaSoapOperationalMode.Simulated => NachaSoapUatReadinessStatus.ReadyForSimulation,
            _ => NachaSoapUatReadinessStatus.ReadyForUatControl
        };

    private static Dictionary<string, string> Sanitize(IReadOnlyDictionary<string, string> values)
        => NachaSoapEndpointSafetyValidator.SanitizeMetadata(values);
}
