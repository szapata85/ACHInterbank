using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaSoapResilientExecutor : INachaSoapResilientExecutor
{
    private readonly INachaSoapSimulatedGateway _gateway;
    private readonly INachaSoapIdempotencyStore _idempotencyStore;
    private readonly INachaSoapAttemptAuditor _attemptAuditor;
    private readonly INachaSoapResiliencePolicyEvaluator _policyEvaluator;

    public NachaSoapResilientExecutor(
        INachaSoapSimulatedGateway gateway,
        INachaSoapIdempotencyStore idempotencyStore,
        INachaSoapAttemptAuditor attemptAuditor,
        INachaSoapResiliencePolicyEvaluator policyEvaluator)
    {
        _gateway = gateway;
        _idempotencyStore = idempotencyStore;
        _attemptAuditor = attemptAuditor;
        _policyEvaluator = policyEvaluator;
    }

    public async Task<NachaSoapResilienceExecutionResult> ExecuteAsync(
        NachaSoapExecutionRequest request,
        NachaSoapExecutionContext context,
        NachaSoapRetryPolicy policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(policy);

        var maxAttempts = Math.Max(1, policy.MaxAttempts);
        var operation = request.Decision.SoapOperation;
        var idempotencyKey = BuildIdempotencyKey(request, context);
        var requestHash = BuildRequestHash(request, context);
        var record = BuildRecord(request, context, idempotencyKey, requestHash);
        var begin = await _idempotencyStore.TryBeginAsync(record, cancellationToken);

        if (!begin.CanExecute)
        {
            var duplicateAudit = BuildDuplicateAudit(request, context, begin.Record, maxAttempts);
            await _attemptAuditor.RecordAttemptAsync(duplicateAudit, cancellationToken);

            return new NachaSoapResilienceExecutionResult
            {
                CorrelationId = request.CorrelationId,
                IdempotencyKey = idempotencyKey,
                OperationCandidate = operation,
                WasSkipped = true,
                WasExecuted = false,
                AttemptCount = begin.Record.AttemptCount,
                FinalStatus = NachaSoapIdempotencyStatus.SkippedDuplicate,
                FinalMessage = "Solicitud SOAP omitida por idempotencia: duplicado detectado.",
                Attempts = [duplicateAudit],
                ProductiveExecution = false,
                Metadata = new Dictionary<string, string> { ["RequestHash"] = requestHash }
            };
        }

        if (operation == NachaSoapOperationCandidate.None
            || request.Decision.DecisionType is NachaIncomingDecisionType.ManualReviewRequired or NachaIncomingDecisionType.IgnoreDuplicate)
        {
            var skipped = BuildSkippedResult(request, idempotencyKey, requestHash, operation);
            await _idempotencyStore.FailAsync(idempotencyKey, skipped, cancellationToken);
            return skipped;
        }

        var attempts = new List<NachaSoapAttemptAudit>();
        NachaSoapExecutionResult? lastResult = null;

        for (var attemptNumber = 1; attemptNumber <= maxAttempts; attemptNumber++)
        {
            var attemptRequest = WithAttemptScenario(request, attemptNumber);
            var startedAt = DateTime.UtcNow;
            lastResult = await _gateway.ExecuteAsync(attemptRequest, context, cancellationToken);
            var finishedAt = DateTime.UtcNow;
            var shouldRetry = _policyEvaluator.ShouldRetry(lastResult, policy, attemptNumber);
            var classification = _policyEvaluator.ClassifyFailure(lastResult, policy);
            var audit = BuildAttemptAudit(
                request,
                idempotencyKey,
                attemptNumber,
                maxAttempts,
                startedAt,
                finishedAt,
                lastResult,
                classification,
                shouldRetry,
                _policyEvaluator.CalculateDelayMs(policy, attemptNumber));
            attempts.Add(audit);
            await _attemptAuditor.RecordAttemptAsync(audit, cancellationToken);

            if (lastResult.Status == NachaSoapExecutionStatus.SimulatedSuccess)
            {
                var success = BuildFinalResult(request, idempotencyKey, requestHash, attempts, lastResult, NachaSoapIdempotencyStatus.Completed);
                await _idempotencyStore.CompleteAsync(idempotencyKey, success, cancellationToken);
                return success;
            }

            if (!shouldRetry)
            {
                break;
            }
        }

        var status = lastResult?.Status == NachaSoapExecutionStatus.BlockedByNoGo
            ? NachaSoapIdempotencyStatus.BlockedByNoGo
            : NachaSoapIdempotencyStatus.Failed;
        var failure = BuildFinalResult(request, idempotencyKey, requestHash, attempts, lastResult, status);
        await _idempotencyStore.FailAsync(idempotencyKey, failure, cancellationToken);
        return failure;
    }

    public static string BuildIdempotencyKey(NachaSoapExecutionRequest request, NachaSoapExecutionContext context)
        => Sha256(BuildStableMaterial(request, context));

    public static string BuildRequestHash(NachaSoapExecutionRequest request, NachaSoapExecutionContext context)
        => Sha256($"{BuildStableMaterial(request, context)}|{context.Currency}|{context.ClearingHouseCode}|{request.ProfileCode}");

    private static string BuildStableMaterial(NachaSoapExecutionRequest request, NachaSoapExecutionContext context)
        => string.Join("|",
            request.Decision.SoapOperation,
            request.CorrelationId,
            request.Decision.TransactionId?.ToString() ?? string.Empty,
            request.Decision.PrenotificationId?.ToString() ?? string.Empty,
            request.Decision.EntryTraceNumber,
            request.Decision.OriginalTraceNumber ?? string.Empty,
            context.SourceFileName,
            context.AmountInCents.ToString(System.Globalization.CultureInfo.InvariantCulture),
            request.Decision.ReasonCode ?? string.Empty);

    private static string Sha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private static NachaSoapIdempotencyRecord BuildRecord(
        NachaSoapExecutionRequest request,
        NachaSoapExecutionContext context,
        string idempotencyKey,
        string requestHash)
        => new()
        {
            IdempotencyKey = idempotencyKey,
            CorrelationId = request.CorrelationId,
            OperationCandidate = request.Decision.SoapOperation,
            TransactionId = request.Decision.TransactionId,
            PrenotificationId = request.Decision.PrenotificationId,
            EntryTraceNumber = request.Decision.EntryTraceNumber,
            OriginalTraceNumber = request.Decision.OriginalTraceNumber ?? string.Empty,
            SourceFileName = context.SourceFileName,
            RequestHash = requestHash,
            FirstSeenAt = DateTime.UtcNow,
            LastAttemptAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, string> { ["Phase"] = "6B.5" }
        };

    private static NachaSoapExecutionRequest WithAttemptScenario(
        NachaSoapExecutionRequest request,
        int attemptNumber)
        => new()
        {
            Decision = request.Decision,
            CorrelationId = request.CorrelationId,
            ClearingHouseCode = request.ClearingHouseCode,
            ProfileCode = request.ProfileCode,
            RequestedBy = request.RequestedBy,
            IsEnabled = request.IsEnabled,
            DryRun = request.DryRun,
            PayloadContext = request.PayloadContext,
            SimulationScenario = request.SimulationAttempts.Count >= attemptNumber
                ? request.SimulationAttempts[attemptNumber - 1]
                : request.SimulationScenario,
            SimulationAttempts = request.SimulationAttempts,
            SimulationOptions = request.SimulationOptions,
            Metadata = request.Metadata
        };

    private static NachaSoapAttemptAudit BuildAttemptAudit(
        NachaSoapExecutionRequest request,
        string idempotencyKey,
        int attemptNumber,
        int maxAttempts,
        DateTime startedAt,
        DateTime finishedAt,
        NachaSoapExecutionResult result,
        NachaSoapFailureClassification classification,
        bool shouldRetry,
        int nextDelayMs)
        => new()
        {
            AttemptId = $"{idempotencyKey}-{attemptNumber}",
            CorrelationId = request.CorrelationId,
            IdempotencyKey = idempotencyKey,
            OperationCandidate = request.Decision.SoapOperation,
            AttemptNumber = attemptNumber,
            MaxAttempts = maxAttempts,
            StartedAt = startedAt,
            FinishedAt = finishedAt,
            DurationMs = Math.Max(0, (long)(finishedAt - startedAt).TotalMilliseconds),
            IsSuccess = result.Status == NachaSoapExecutionStatus.SimulatedSuccess,
            IsTimeout = result.IsTimeout,
            IsSoapFault = result.IsSoapFault,
            IsTransient = classification is NachaSoapFailureClassification.Timeout
                or NachaSoapFailureClassification.SoapFault
                or NachaSoapFailureClassification.TransientFailure,
            IsRetryable = shouldRetry,
            Classification = classification,
            ErrorCode = result.SoapFaultCode.Length > 0 ? result.SoapFaultCode : result.ResponseCode,
            ErrorDescription = result.SoapFaultMessage.Length > 0 ? result.SoapFaultMessage : result.ResponseMessage,
            SanitizedRequestSummary = Sanitize(result.RequestSummary),
            SanitizedResponseSummary = Sanitize(result.ResponseSummary),
            ProductiveExecution = false,
            Metadata = new Dictionary<string, string>
            {
                ["NextDelayMs"] = nextDelayMs.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["SoapWasInvoked"] = "false",
                ["WasExecuted"] = "false"
            }
        };

    private static NachaSoapAttemptAudit BuildDuplicateAudit(
        NachaSoapExecutionRequest request,
        NachaSoapExecutionContext context,
        NachaSoapIdempotencyRecord record,
        int maxAttempts)
    {
        var now = DateTime.UtcNow;
        return new NachaSoapAttemptAudit
        {
            AttemptId = $"{record.IdempotencyKey}-duplicate",
            CorrelationId = request.CorrelationId,
            IdempotencyKey = record.IdempotencyKey,
            OperationCandidate = request.Decision.SoapOperation,
            AttemptNumber = record.AttemptCount + 1,
            MaxAttempts = maxAttempts,
            StartedAt = now,
            FinishedAt = now,
            Classification = NachaSoapFailureClassification.DuplicateBlocked,
            ErrorCode = "DUPLICATE",
            ErrorDescription = "Solicitud duplicada bloqueada por idempotencia.",
            SanitizedRequestSummary = Sanitize(new Dictionary<string, string>
            {
                ["Phase"] = "6B.5",
                ["OperationCandidate"] = request.Decision.SoapOperation.ToString(),
                ["CorrelationId"] = request.CorrelationId,
                ["SourceFileName"] = context.SourceFileName
            }),
            SanitizedResponseSummary = Sanitize(new Dictionary<string, string>
            {
                ["Phase"] = "6B.5",
                ["ResponseCode"] = "DUPLICATE",
                ["ResponseMessage"] = "Solicitud duplicada bloqueada por idempotencia."
            }),
            ProductiveExecution = false
        };
    }

    private static NachaSoapResilienceExecutionResult BuildSkippedResult(
        NachaSoapExecutionRequest request,
        string idempotencyKey,
        string requestHash,
        NachaSoapOperationCandidate operation)
        => new()
        {
            CorrelationId = request.CorrelationId,
            IdempotencyKey = idempotencyKey,
            OperationCandidate = operation,
            WasSkipped = true,
            WasExecuted = false,
            AttemptCount = 0,
            FinalStatus = operation == NachaSoapOperationCandidate.None
                ? NachaSoapIdempotencyStatus.SkippedDuplicate
                : NachaSoapIdempotencyStatus.ManualReviewRequired,
            FinalMessage = operation == NachaSoapOperationCandidate.None
                ? "Operacion None omitida por executor resiliente."
                : "ManualReviewRequired/IgnoreDuplicate omitido por executor resiliente.",
            ProductiveExecution = false,
            Metadata = new Dictionary<string, string> { ["RequestHash"] = requestHash }
        };

    private static NachaSoapResilienceExecutionResult BuildFinalResult(
        NachaSoapExecutionRequest request,
        string idempotencyKey,
        string requestHash,
        IReadOnlyList<NachaSoapAttemptAudit> attempts,
        NachaSoapExecutionResult? lastResult,
        NachaSoapIdempotencyStatus finalStatus)
        => new()
        {
            CorrelationId = request.CorrelationId,
            IdempotencyKey = idempotencyKey,
            OperationCandidate = request.Decision.SoapOperation,
            IsSuccess = finalStatus == NachaSoapIdempotencyStatus.Completed,
            WasSkipped = false,
            WasExecuted = false,
            WasRetried = attempts.Count > 1,
            AttemptCount = attempts.Count,
            FinalStatus = finalStatus,
            FinalMessage = lastResult?.Message ?? "Sin resultado de gateway simulado.",
            Attempts = attempts,
            LastExecutionResult = lastResult,
            ProductiveExecution = false,
            Metadata = new Dictionary<string, string>
            {
                ["RequestHash"] = requestHash,
                ["SoapWasInvoked"] = "false",
                ["WasExecuted"] = "false"
            }
        };

    private static Dictionary<string, string> Sanitize(IReadOnlyDictionary<string, string> values)
        => NachaSoapMockOperationAdapter.SanitizeMetadata(values);
}
