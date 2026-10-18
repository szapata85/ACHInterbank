using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// R5D internal stage orchestrator. It consumes dispatch work that was already parsed,
/// validated, persisted and correlated by the existing inbound pipeline. It does not own
/// physical parsing, validation, parsed-graph persistence or the ingestion transaction.
/// </summary>
internal sealed class IncomingNachaPostProcessingPipeline
{
    private readonly AchDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly IContrapartidaDispatchJobService? _contrapartidaDispatchJobService;
    private readonly IncomingNachaResolveContextStage _resolveContext;
    private readonly IncomingNachaEligibilityStage _eligibility;
    private readonly IncomingNachaLinkCorrelationStage _linkCorrelation;
    private readonly IncomingNachaStateTransitionStage _stateTransition;
    private readonly IncomingNachaDispatchPlanningStage _dispatchPlanning;
    private readonly IncomingNachaExternalDispatchStage _externalDispatch;
    private readonly IncomingNachaAuditEventsStage _auditEvents;

    public IncomingNachaPostProcessingPipeline(
        AchDbContext context,
        IProcTransaccionesRequestMapper mapper,
        IProcTransaccionesResponseParser parser,
        IWscfaachSoapClient soapClient,
        IncomingNachaDispatchResilienceOptions resilienceOptions,
        ProcTransaccionesDispatchOptions dispatchOptions,
        ITransactionIntegrationOperationResolver? operationResolver,
        IIntegrationMappingReadinessService? mappingReadinessService,
        IIntegrationMappingTraceWriter? mappingTraceWriter,
        IContrapartidaDispatchJobService? contrapartidaDispatchJobService,
        ISoapIntegrationSettingsService? soapIntegrationSettingsService,
        IIncomingNachaLocalLivePreparationService? localLivePreparationService,
        IIntegrationResponseCatalogResolver? responseCatalogResolver,
        TimeProvider timeProvider,
        IIncomingNachaAchResultResolver? achResultResolver,
        ICycleCalendarGuard calendarGuard,
        IOperationalCycleWindowResolver windowResolver,
        ICycleTransactionPolicy? cycleTransactionPolicy)
    {
        _context = context;
        _timeProvider = timeProvider;
        _contrapartidaDispatchJobService = contrapartidaDispatchJobService;
        _resolveContext = new IncomingNachaResolveContextStage(context, dispatchOptions, soapIntegrationSettingsService);
        _eligibility = new IncomingNachaEligibilityStage(context, cycleTransactionPolicy, calendarGuard, windowResolver, timeProvider);
        _linkCorrelation = new IncomingNachaLinkCorrelationStage(context);
        _stateTransition = new IncomingNachaStateTransitionStage(context, resilienceOptions, timeProvider);
        _dispatchPlanning = new IncomingNachaDispatchPlanningStage(operationResolver);
        _externalDispatch = new IncomingNachaExternalDispatchStage(
            context,
            mapper,
            parser,
            soapClient,
            operationResolver,
            mappingReadinessService,
            localLivePreparationService,
            responseCatalogResolver,
            timeProvider);
        _auditEvents = new IncomingNachaAuditEventsStage(context, mappingTraceWriter, achResultResolver, timeProvider);
    }

    public async Task<IncomingNachaPostProcessingRunResult> ExecuteAsync(
        int chunkSize,
        string triggeredBy,
        CancellationToken ct)
    {
        var safeChunk = Math.Clamp(chunkSize, 10, 500);
        var nowUtcOffset = _timeProvider.GetUtcNow();
        var nowUtc = nowUtcOffset.UtcDateTime;
        var nowLocal = TimeZoneInfo.ConvertTime(nowUtcOffset, _timeProvider.LocalTimeZone).DateTime;

        var integrationCodePolicies = await _resolveContext.LoadIntegrationCodePoliciesAsync(ct);

        var waitingEvaluations = await _eligibility.EvaluateWaitingWindowAsync(nowLocal, ct);
        var waitingReleased = 0;
        var waitingBlocked = 0;
        var waitingChanged = false;
        foreach (var evaluation in waitingEvaluations)
        {
            if (!evaluation.Changed)
            {
                continue;
            }

            var transition = _stateTransition.ApplyWaitingWindowEvaluation(evaluation, nowUtc);
            _auditEvents.AddAutomaticEvent(
                evaluation.Queue,
                transition.EventType,
                transition.EventStatus,
                transition.EventMessage,
                transition.EventCode);
            waitingChanged = true;
            if (transition.CountAsReleased)
            {
                waitingReleased++;
            }
            if (transition.CountAsBlocked)
            {
                waitingBlocked++;
            }
        }

        if (waitingChanged)
        {
            await _context.SaveChangesAsync(ct);
        }

        var resolved = await _resolveContext.ResolveQueueContextAsync(safeChunk, nowUtc, ct);
        if (resolved.Ids.Count == 0)
        {
            var waitingForWindow = await _resolveContext.CountWaitingWindowAsync(ct);
            return new IncomingNachaPostProcessingRunResult(
                0,
                0,
                0,
                0,
                0,
                waitingBlocked,
                waitingReleased,
                waitingForWindow > 0
                    ? "La tarea fue evaluada, pero no puede realizar despachos en este momento porque el ciclo no se encuentra dentro de su ventana operativa."
                    : "La tarea fue evaluada y no encontró registros elegibles.");
        }

        var confirmed = 0;
        var retryPending = 0;
        var failedFinal = 0;
        var blocked = waitingBlocked;
        var waitingWindow = waitingReleased;
        var contrapartidaDispatchTargets = new HashSet<(string CycleId, int ClearingHouseId)>();
        var calendarDecisions = new Dictionary<string, CycleCalendarGuardResult>(StringComparer.Ordinal);
        var procTransaccionesRuntime = await _resolveContext.ResolveProcTransaccionesRuntimeAsync(ct);

        foreach (var queue in resolved.Queues)
        {
            var correlation = await _linkCorrelation.ResolveAsync(
                queue,
                resolved.Ingestions,
                resolved.Classifications,
                ct);

            if (correlation.PriorSuccessfulExecution)
            {
                IncomingNachaStateTransitionStage.MarkAlreadyConfirmed(queue);
                _auditEvents.AddAutomaticEvent(
                    queue,
                    "DispatchAlreadyConfirmed",
                    "Skipped",
                    "La operación ya tiene una respuesta funcional exitosa; no se reenvió.");
                confirmed++;
                continue;
            }

            var eligibility = await _eligibility.EvaluateAsync(
                queue,
                nowUtcOffset,
                calendarDecisions,
                ct);
            if (!eligibility.CanProceed)
            {
                _stateTransition.ApplyEligibilityDecision(queue, eligibility);
                _auditEvents.AddAutomaticEvent(
                    queue,
                    eligibility.EventType,
                    eligibility.EventStatus,
                    eligibility.EventMessage,
                    eligibility.ErrorCode);
                if (eligibility.CountAsWaitingWindow)
                {
                    waitingWindow++;
                }
                else
                {
                    blocked++;
                }
                continue;
            }

            IncomingNachaStateTransitionStage.MarkDispatching(queue, nowUtc);
            _auditEvents.AddAutomaticEvent(queue, "DispatchStarted", "Applied", "Dispatch automático iniciado.");

            var correlationId = $"in-nacha-{queue.Id:N}-{queue.AttemptCount}";
            var plan = await _dispatchPlanning.PlanAsync(queue, ct);
            if (plan.DelegateToContrapartidas)
            {
                _stateTransition.MarkContrapartidasDelegated(queue);
                _auditEvents.AddAutomaticEvent(
                    queue,
                    "DispatchDelegatedToContrapartidas",
                    "Applied",
                    "Proc_Contrapartidas delegado al job de contrapartida en dry-run.",
                    IntegrationGuaranteeConstants.ProcContrapartidas);
                contrapartidaDispatchTargets.Add((queue.AchTransaction.AchCycleId, queue.AchTransaction.AchCycle.ClearingHouseId));
                confirmed++;
                continue;
            }

            var execution = _auditEvents.CreateExecution(
                queue,
                correlation.Classification,
                procTransaccionesRuntime.OperatingMode,
                correlationId);

            try
            {
                if (!correlation.HasContext)
                {
                    IncomingNachaStateTransitionStage.BlockMissingContext(queue);
                    _auditEvents.AddAutomaticEvent(
                        queue,
                        "IntegrationContextMissing",
                        "Blocked",
                        queue.LastErrorMessage,
                        queue.LastErrorCode);
                    blocked++;
                    continue;
                }

                var preparation = await _externalDispatch.PrepareAsync(
                    queue,
                    correlation.Ingestion!,
                    correlation.Classification!,
                    ct);
                await _auditEvents.RecordPreparedRequestAsync(
                    execution,
                    preparation,
                    queue,
                    correlationId,
                    procTransaccionesRuntime.Endpoint,
                    procTransaccionesRuntime.OperatingMode,
                    ct);

                var sendResult = await _externalDispatch.SendAsync(
                    preparation.RequestXml,
                    queue,
                    procTransaccionesRuntime.OperatingMode,
                    ct);
                if (sendResult.GuardrailEvent is not null)
                {
                    _auditEvents.AddAutomaticEvent(
                        queue,
                        sendResult.GuardrailEvent.EventType,
                        sendResult.GuardrailEvent.EventStatus,
                        sendResult.GuardrailEvent.Message,
                        sendResult.GuardrailEvent.Code);
                }

                var dispatchResult = await _externalDispatch.InterpretAsync(
                    sendResult.ResponseXml,
                    procTransaccionesRuntime.OperatingMode,
                    ct);
                await _auditEvents.RecordResponseAsync(
                    execution,
                    queue,
                    dispatchResult,
                    procTransaccionesRuntime.OperatingMode,
                    ct);

                var outcome = _stateTransition.ApplyDispatchOutcome(
                    queue,
                    dispatchResult,
                    integrationCodePolicies,
                    nowUtc);
                _auditEvents.AddAutomaticEvent(
                    queue,
                    outcome.EventType,
                    outcome.EventStatus,
                    outcome.EventMessage,
                    outcome.EventCode);

                switch (outcome.Counter)
                {
                    case IncomingNachaPostProcessingCounter.Confirmed:
                        confirmed++;
                        break;
                    case IncomingNachaPostProcessingCounter.RetryPending:
                        retryPending++;
                        break;
                    case IncomingNachaPostProcessingCounter.FailedFinal:
                        failedFinal++;
                        break;
                    case IncomingNachaPostProcessingCounter.Blocked:
                        blocked++;
                        break;
                }
            }
            catch (InvalidOperationException ex)
            {
                var errorCode = IncomingNachaAuditEventsStage.ResolveControlledErrorCode(ex);
                IncomingNachaStateTransitionStage.BlockControlledFailure(queue, errorCode, ex.Message);
                _auditEvents.RecordControlledFailure(execution, queue, ex);
                _auditEvents.AddAutomaticEvent(queue, "DispatchBlockedByMapping", "Blocked", ex.Message, queue.LastErrorCode);
                blocked++;
            }
            catch (Exception ex)
            {
                var errorCode = IncomingNachaStateTransitionStage.NormalizeTechnicalIntegrationCode(
                    "TECHNICAL_ERROR",
                    ex.Message,
                    integrationCodePolicies);
                IncomingNachaStateTransitionStage.BlockUncertainFailure(queue, errorCode, ex.Message);
                _auditEvents.RecordUncertainFailure(execution, queue, ex);
                _auditEvents.AddAutomaticEvent(
                    queue,
                    "IntegrationOutcomeUncertain",
                    "Blocked",
                    "La respuesta del servicio es incierta y requiere conciliación antes de reenviar.",
                    queue.LastErrorCode);
                blocked++;
            }
        }

        if (_contrapartidaDispatchJobService is not null && contrapartidaDispatchTargets.Count > 0)
        {
            foreach (var target in contrapartidaDispatchTargets)
            {
                await _contrapartidaDispatchJobService.ProcessCycleAsync(
                    target.CycleId,
                    target.ClearingHouseId,
                    triggeredBy,
                    safeChunk,
                    ct);
            }
        }

        // Preserve the existing post-processing persistence boundary. This SaveChanges
        // writes queue/state/audit results only; the parsed NACHA graph remains owned by R5C.
        await _context.SaveChangesAsync(ct);
        var summary = $"Procesadas={resolved.Queues.Count}, Confirmadas={confirmed}, Retry={retryPending}, FailedFinal={failedFinal}, Blocked={blocked}, WaitingWindow={waitingWindow}.";
        return new IncomingNachaPostProcessingRunResult(
            Planned: resolved.Ids.Count,
            Picked: resolved.Queues.Count,
            Confirmed: confirmed,
            RetryPending: retryPending,
            FailedFinal: failedFinal,
            Blocked: blocked,
            WaitingWindow: waitingWindow,
            Summary: summary);
    }
}

internal sealed record IncomingNachaResolvedQueueContext(
    IReadOnlyList<Guid> Ids,
    IReadOnlyList<IncomingNachaDispatchQueue> Queues,
    IReadOnlyDictionary<Guid, IncomingNachaFileIngestion> Ingestions,
    IReadOnlyDictionary<Guid, IncomingNachaEntryClassification> Classifications);

internal sealed record IncomingNachaProcTransaccionesRuntime(string Endpoint, string OperatingMode);

/// <summary>Stage 1: resolve runtime and persisted queue context.</summary>
internal sealed class IncomingNachaResolveContextStage
{
    private const string SoapMethodName = IntegrationGuaranteeConstants.ProcTransacciones;
    private readonly AchDbContext _context;
    private readonly ProcTransaccionesDispatchOptions _dispatchOptions;
    private readonly ISoapIntegrationSettingsService? _soapIntegrationSettingsService;

    public IncomingNachaResolveContextStage(
        AchDbContext context,
        ProcTransaccionesDispatchOptions dispatchOptions,
        ISoapIntegrationSettingsService? soapIntegrationSettingsService)
    {
        _context = context;
        _dispatchOptions = dispatchOptions;
        _soapIntegrationSettingsService = soapIntegrationSettingsService;
    }

    public async Task<IReadOnlyDictionary<string, bool>> LoadIntegrationCodePoliciesAsync(CancellationToken ct)
        => await _context.AchFileRejectionCodes
            .AsNoTracking()
            .Where(x => x.IsActive && x.AppliesToStage == "Integration")
            .ToDictionaryAsync(x => x.Code.ToUpper(), x => x.IsRetryable, ct);

    public async Task<IncomingNachaResolvedQueueContext> ResolveQueueContextAsync(
        int safeChunk,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var ids = await _context.IncomingNachaDispatchQueue
            .AsNoTracking()
            .Where(x => x.QueueStatus == IncomingNachaDispatchQueueStatus.Queued || x.QueueStatus == IncomingNachaDispatchQueueStatus.RetryPending)
            .Where(x => !x.NextAttemptAtUtc.HasValue || x.NextAttemptAtUtc <= nowUtc)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Id)
            .Select(x => x.Id)
            .Take(safeChunk)
            .ToListAsync(ct);

        if (ids.Count == 0)
        {
            return new IncomingNachaResolvedQueueContext(
                ids,
                [],
                new Dictionary<Guid, IncomingNachaFileIngestion>(),
                new Dictionary<Guid, IncomingNachaEntryClassification>());
        }

        var queues = await _context.IncomingNachaDispatchQueue
            .Include(x => x.AchTransaction).ThenInclude(x => x.AchCycle)
                .ThenInclude(x => x.ClearingHouse)
                    .ThenInclude(x => x!.ClearingHouseConfig)
            .Include(x => x.AchTransaction).ThenInclude(x => x.AchCycle)
                .ThenInclude(x => x.ClearingHouseCycleConfig)
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(ct);
        var ingestions = await _context.IncomingNachaFileIngestions
            .Where(x => queues.Select(q => q.IncomingNachaFileIngestionId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);
        var classifications = await _context.IncomingNachaEntryClassifications
            .Where(x => queues.Select(q => q.IncomingNachaEntryClassificationId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        return new IncomingNachaResolvedQueueContext(ids, queues, ingestions, classifications);
    }

    public Task<int> CountWaitingWindowAsync(CancellationToken ct)
        => _context.IncomingNachaDispatchQueue.AsNoTracking()
            .CountAsync(x => x.QueueStatus == IncomingNachaDispatchQueueStatus.WaitingWindow, ct);

    public async Task<IncomingNachaProcTransaccionesRuntime> ResolveProcTransaccionesRuntimeAsync(CancellationToken ct)
    {
        if (_soapIntegrationSettingsService is null)
        {
            return new IncomingNachaProcTransaccionesRuntime(string.Empty, _dispatchOptions.NormalizedMode);
        }

        var settings = await _soapIntegrationSettingsService.GetAsync(ct);
        var mapping = settings.WscfaachMappings
            .FirstOrDefault(x => string.Equals(x.MethodName, SoapMethodName, StringComparison.OrdinalIgnoreCase));
        var mode = mapping?.Enabled == false
            ? "Disabled"
            : IncomingNachaPostProcessingMode.Normalize(mapping?.OperatingMode, _dispatchOptions.NormalizedMode);
        return new IncomingNachaProcTransaccionesRuntime(mapping?.Endpoint?.Trim() ?? string.Empty, mode);
    }
}

internal sealed record IncomingNachaWaitingWindowEvaluation(
    IncomingNachaDispatchQueue Queue,
    IncomingNachaDispatchWindowPosition Position,
    DateTime? NextEligibleAtUtc,
    bool Changed);

internal sealed record IncomingNachaEligibilityDecision(
    bool CanProceed,
    IncomingNachaDispatchQueueStatus? QueueStatus,
    DateTime? NextAttemptAtUtc,
    string ErrorCode,
    string ErrorMessage,
    string EventType,
    string EventStatus,
    string EventMessage,
    bool CountAsWaitingWindow);

/// <summary>Stage 2: evaluate cycle, calendar and operational-window eligibility.</summary>
internal sealed class IncomingNachaEligibilityStage
{
    private readonly AchDbContext _context;
    private readonly ICycleTransactionPolicy? _cycleTransactionPolicy;
    private readonly ICycleCalendarGuard _calendarGuard;
    private readonly IOperationalCycleWindowResolver _windowResolver;
    private readonly TimeProvider _timeProvider;

    public IncomingNachaEligibilityStage(
        AchDbContext context,
        ICycleTransactionPolicy? cycleTransactionPolicy,
        ICycleCalendarGuard calendarGuard,
        IOperationalCycleWindowResolver windowResolver,
        TimeProvider timeProvider)
    {
        _context = context;
        _cycleTransactionPolicy = cycleTransactionPolicy;
        _calendarGuard = calendarGuard;
        _windowResolver = windowResolver;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<IncomingNachaWaitingWindowEvaluation>> EvaluateWaitingWindowAsync(
        DateTime nowLocal,
        CancellationToken ct)
    {
        var waitingQueues = await _context.IncomingNachaDispatchQueue
            .Include(x => x.AchTransaction)
            .ThenInclude(x => x.AchCycle)
            .Where(x => x.QueueStatus == IncomingNachaDispatchQueueStatus.WaitingWindow)
            .ToListAsync(ct);

        var evaluations = new List<IncomingNachaWaitingWindowEvaluation>(waitingQueues.Count);
        foreach (var queue in waitingQueues)
        {
            var cycle = queue.AchTransaction?.AchCycle;
            var window = cycle is null
                ? new IncomingNachaDispatchWindowEvaluation(IncomingNachaDispatchWindowPosition.Invalid, null)
                : IncomingNachaDispatchWindowCalculator.Evaluate(cycle, nowLocal, _timeProvider.LocalTimeZone);
            var changed = window.Position switch
            {
                IncomingNachaDispatchWindowPosition.Before => queue.NextAttemptAtUtc != window.NextEligibleAtUtc
                    || !string.IsNullOrEmpty(queue.LastErrorCode),
                _ => true
            };
            evaluations.Add(new IncomingNachaWaitingWindowEvaluation(
                queue,
                window.Position,
                window.NextEligibleAtUtc,
                changed));
        }

        return evaluations;
    }

    public async Task<IncomingNachaEligibilityDecision> EvaluateAsync(
        IncomingNachaDispatchQueue queue,
        DateTimeOffset now,
        IDictionary<string, CycleCalendarGuardResult> calendarDecisions,
        CancellationToken ct)
    {
        if (_cycleTransactionPolicy is not null)
        {
            var transaction = queue.AchTransaction;
            var cycle = transaction.AchCycle;
            var cycleDecision = await _cycleTransactionPolicy.EvaluateAsync(new CycleTransactionPolicyRequest(
                cycle.ClearingHouseId,
                cycle.ClearingHouseCycleConfigId,
                cycle.ProcessingDate,
                cycle.ClearingHouse?.Code,
                cycle.ClearingHouse?.ClearingHouseConfig?.PaymentRailCode,
                cycle.CycleName,
                transaction.Type,
                transaction.IsPrenotification,
                transaction.ReturnReasonCode,
                transaction.OriginalTraceRef,
                TransactionCode: transaction.TransactionCode), ct);
            if (!cycleDecision.IsAllowed)
            {
                return new IncomingNachaEligibilityDecision(
                    false,
                    IncomingNachaDispatchQueueStatus.Blocked,
                    null,
                    cycleDecision.ReasonCode,
                    cycleDecision.Message,
                    "DispatchBlockedByCyclePolicy",
                    "Blocked",
                    cycleDecision.Message,
                    false);
            }
        }

        var operationalDecision = EvaluateOperationalWindow(queue.AchTransaction.AchCycle, now);
        if (!operationalDecision.CanDispatch)
        {
            return new IncomingNachaEligibilityDecision(
                false,
                operationalDecision.WaitForWindow
                    ? IncomingNachaDispatchQueueStatus.WaitingWindow
                    : IncomingNachaDispatchQueueStatus.Blocked,
                operationalDecision.NextWindowUtc?.UtcDateTime,
                operationalDecision.Code,
                operationalDecision.Reason,
                "DispatchWindowGuard",
                operationalDecision.WaitForWindow ? "Deferred" : "Blocked",
                operationalDecision.Reason,
                operationalDecision.WaitForWindow);
        }

        if (!calendarDecisions.TryGetValue(queue.AchTransaction.AchCycleId, out var calendarDecision))
        {
            calendarDecision = await _calendarGuard.EnsureExecutableAsync(queue.AchTransaction.AchCycle, ct);
            calendarDecisions.Add(queue.AchTransaction.AchCycleId, calendarDecision);
        }
        if (!calendarDecision.CanExecute)
        {
            const string errorCode = "DEFERRED_BY_OPERATIONAL_CALENDAR";
            return new IncomingNachaEligibilityDecision(
                false,
                IncomingNachaDispatchQueueStatus.WaitingWindow,
                null,
                errorCode,
                calendarDecision.Reason,
                "DeferredByOperationalCalendar",
                "Deferred",
                $"Despacho diferido hasta {calendarDecision.RescheduledDate:yyyy-MM-dd}; SOAP no invocado.",
                true);
        }

        return new IncomingNachaEligibilityDecision(
            true,
            null,
            null,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            false);
    }

    private OperationalDispatchDecision EvaluateOperationalWindow(AchCycle cycle, DateTimeOffset now)
    {
        if (cycle.ClearingHouse is null || !cycle.ClearingHouse.IsActive)
        {
            return new(false, false, null, "CLEARING_HOUSE_INACTIVE", "La cámara compensadora está inactiva.");
        }

        if (cycle.OperationalStatus != AchCycleOperationalStatus.Open)
        {
            return new(false, false, null, "CYCLE_NOT_OPEN", "El ciclo no se encuentra abierto.");
        }

        if (cycle.ClearingHouseCycleConfig is { } config)
        {
            if (!config.IsActive)
            {
                return new(false, false, null, "CYCLE_CONFIG_INACTIVE", "La configuración del ciclo está inactiva.");
            }
            if (cycle.ProcessingDate.Date < config.EffectiveFrom.Date
                || config.EffectiveTo.HasValue && cycle.ProcessingDate.Date > config.EffectiveTo.Value.Date)
            {
                return new(false, false, null, "CYCLE_CONFIG_NOT_EFFECTIVE", "La configuración del ciclo no está vigente para la fecha operativa.");
            }
        }

        var window = _windowResolver.Resolve(
            cycle.ProcessingDate,
            cycle.StartTime,
            cycle.EndTime,
            ClearingHouseOperationalTimeZone.Resolve(cycle),
            now);
        return window.Status switch
        {
            OperationalCycleWindowStatus.Inside => new(true, false, null, string.Empty, string.Empty),
            OperationalCycleWindowStatus.Before => new(false, true, window.StartInstant, "WAITING_OPERATIONAL_WINDOW",
                $"El ciclo aún no se encuentra dentro de su ventana operativa. Próxima ventana: {window.LocalStart:yyyy-MM-dd HH:mm} a {window.LocalEnd:HH:mm} ({window.TimeZoneId})."),
            _ => new(false, false, null, "OPERATIONAL_WINDOW_CLOSED",
                "La tarea fue evaluada, pero no puede realizar despachos en este momento porque el ciclo no se encuentra dentro de su ventana operativa.")
        };
    }

    private sealed record OperationalDispatchDecision(
        bool CanDispatch,
        bool WaitForWindow,
        DateTimeOffset? NextWindowUtc,
        string Code,
        string Reason);
}

internal sealed record IncomingNachaCorrelationResolution(
    bool PriorSuccessfulExecution,
    bool HasContext,
    IncomingNachaFileIngestion? Ingestion,
    IncomingNachaEntryClassification? Classification);

/// <summary>Stage 3: consume the already-created ingestion/classification/link correlation context.</summary>
internal sealed class IncomingNachaLinkCorrelationStage
{
    private readonly AchDbContext _context;

    public IncomingNachaLinkCorrelationStage(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IncomingNachaCorrelationResolution> ResolveAsync(
        IncomingNachaDispatchQueue queue,
        IReadOnlyDictionary<Guid, IncomingNachaFileIngestion> ingestions,
        IReadOnlyDictionary<Guid, IncomingNachaEntryClassification> classifications,
        CancellationToken ct)
    {
        var priorSuccess = await _context.IncomingNachaIntegrationExecution.AsNoTracking()
            .AnyAsync(x => x.DispatchQueueId == queue.Id && x.IsSuccessful, ct);

        ingestions.TryGetValue(queue.IncomingNachaFileIngestionId, out var ingestion);
        classifications.TryGetValue(queue.IncomingNachaEntryClassificationId, out var classification);
        return new IncomingNachaCorrelationResolution(
            priorSuccess,
            ingestion is not null && classification is not null,
            ingestion,
            classification);
    }
}

internal enum IncomingNachaPostProcessingCounter
{
    None = 0,
    Confirmed = 1,
    RetryPending = 2,
    FailedFinal = 3,
    Blocked = 4
}

internal sealed record IncomingNachaStateTransitionOutcome(
    IncomingNachaPostProcessingCounter Counter,
    string EventType,
    string EventStatus,
    string EventMessage,
    string? EventCode = null);

internal sealed record IncomingNachaWaitingWindowTransition(
    string EventType,
    string EventStatus,
    string EventMessage,
    string? EventCode,
    bool CountAsReleased,
    bool CountAsBlocked);

/// <summary>Stage 4: own in-memory queue/transaction state transitions; no transaction boundary is opened here.</summary>
internal sealed class IncomingNachaStateTransitionStage
{
    private readonly AchDbContext _context;
    private readonly IncomingNachaDispatchResilienceOptions _resilienceOptions;
    private readonly TimeProvider _timeProvider;

    public IncomingNachaStateTransitionStage(
        AchDbContext context,
        IncomingNachaDispatchResilienceOptions resilienceOptions,
        TimeProvider timeProvider)
    {
        _context = context;
        _resilienceOptions = resilienceOptions;
        _timeProvider = timeProvider;
    }

    public IncomingNachaWaitingWindowTransition ApplyWaitingWindowEvaluation(
        IncomingNachaWaitingWindowEvaluation evaluation,
        DateTime nowUtc)
    {
        var queue = evaluation.Queue;
        switch (evaluation.Position)
        {
            case IncomingNachaDispatchWindowPosition.Open:
                queue.QueueStatus = IncomingNachaDispatchQueueStatus.Queued;
                queue.NextAttemptAtUtc = nowUtc;
                queue.LastErrorCode = string.Empty;
                queue.LastErrorMessage = string.Empty;
                return new IncomingNachaWaitingWindowTransition(
                    "DispatchWindowOpened",
                    "Applied",
                    "La ventana operativa fue reevaluada y está abierta; el despacho quedó en cola.",
                    null,
                    true,
                    false);

            case IncomingNachaDispatchWindowPosition.Before:
                queue.NextAttemptAtUtc = evaluation.NextEligibleAtUtc;
                queue.LastErrorCode = string.Empty;
                queue.LastErrorMessage = "En espera del inicio de la ventana operativa del ciclo.";
                return new IncomingNachaWaitingWindowTransition(
                    "DispatchWindowScheduled",
                    "Applied",
                    "Se calculó de forma determinística el próximo inicio de ventana operativa.",
                    null,
                    false,
                    false);

            case IncomingNachaDispatchWindowPosition.Expired:
                queue.QueueStatus = IncomingNachaDispatchQueueStatus.Blocked;
                queue.NextAttemptAtUtc = null;
                queue.LastErrorCode = "WINDOW_EXPIRED";
                queue.LastErrorMessage = "La ventana operativa del ciclo expiró; no se permite despacho automático.";
                return new IncomingNachaWaitingWindowTransition(
                    "DispatchWindowExpired",
                    "Blocked",
                    queue.LastErrorMessage,
                    queue.LastErrorCode,
                    false,
                    true);

            default:
                queue.QueueStatus = IncomingNachaDispatchQueueStatus.Blocked;
                queue.NextAttemptAtUtc = null;
                queue.LastErrorCode = "WINDOW_SCHEDULE_INVALID";
                queue.LastErrorMessage = "No fue posible reevaluar de forma determinística la ventana operativa.";
                return new IncomingNachaWaitingWindowTransition(
                    "DispatchWindowInvalid",
                    "Blocked",
                    queue.LastErrorMessage,
                    queue.LastErrorCode,
                    false,
                    true);
        }
    }

    public void ApplyEligibilityDecision(IncomingNachaDispatchQueue queue, IncomingNachaEligibilityDecision decision)
    {
        queue.QueueStatus = decision.QueueStatus ?? queue.QueueStatus;
        queue.NextAttemptAtUtc = decision.NextAttemptAtUtc;
        queue.LastErrorCode = decision.ErrorCode;
        queue.LastErrorMessage = decision.ErrorMessage;
    }

    public static void MarkAlreadyConfirmed(IncomingNachaDispatchQueue queue)
    {
        queue.QueueStatus = IncomingNachaDispatchQueueStatus.Confirmed;
        queue.NextAttemptAtUtc = null;
    }

    public static void MarkDispatching(IncomingNachaDispatchQueue queue, DateTime nowUtc)
    {
        queue.QueueStatus = IncomingNachaDispatchQueueStatus.Dispatching;
        queue.LastAttemptAtUtc = nowUtc;
        queue.AttemptCount += 1;
    }

    public void MarkContrapartidasDelegated(IncomingNachaDispatchQueue queue)
    {
        queue.QueueStatus = IncomingNachaDispatchQueueStatus.Dispatched;
        queue.ConfirmedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        queue.NextAttemptAtUtc = null;
        queue.LastResponseCode = string.Empty;
        queue.LastErrorCode = string.Empty;
        queue.LastErrorMessage = string.Empty;
    }

    public static void BlockMissingContext(IncomingNachaDispatchQueue queue)
    {
        queue.QueueStatus = IncomingNachaDispatchQueueStatus.Blocked;
        queue.LastErrorCode = "MISSING_CONTEXT";
        queue.LastErrorMessage = "No se encontró contexto de ingesta/clasificación.";
        queue.NextAttemptAtUtc = null;
    }

    public IncomingNachaStateTransitionOutcome ApplyDispatchOutcome(
        IncomingNachaDispatchQueue queue,
        IncomingNachaExternalDispatchResult result,
        IReadOnlyDictionary<string, bool> integrationCodePolicies,
        DateTime nowUtc)
    {
        queue.LastResponseCode = result.Parsed.ResponseCode;
        queue.LastErrorCode = result.BusinessSuccess ? string.Empty : result.Parsed.ResponseCode;
        queue.LastErrorMessage = result.BusinessSuccess
            ? string.Empty
            : result.CatalogResult?.Description ?? result.Parsed.ResponseMessage;

        if (result.BusinessSuccess)
        {
            if (result.CatalogResult is not null)
            {
                ApplySuccessfulTransactionState(queue.AchTransaction, result.CatalogResult, result.FinishedAtUtc);
            }
            queue.QueueStatus = IncomingNachaDispatchQueueStatus.Confirmed;
            queue.ConfirmedAtUtc = result.FinishedAtUtc;
            queue.NextAttemptAtUtc = null;
            return new IncomingNachaStateTransitionOutcome(
                IncomingNachaPostProcessingCounter.Confirmed,
                "IntegrationSucceeded",
                "Applied",
                "Integración confirmada exitosamente.");
        }

        if (result.TransportStatus == IntegrationTransportStatus.Succeeded
            && result.CatalogResult is { RequiresManualReview: true })
        {
            queue.QueueStatus = IncomingNachaDispatchQueueStatus.Blocked;
            queue.NextAttemptAtUtc = null;
            return new IncomingNachaStateTransitionOutcome(
                IncomingNachaPostProcessingCounter.Blocked,
                "IntegrationResponsePendingCatalog",
                "Blocked",
                "La respuesta del core requiere revisión manual o parametrización de catálogo.",
                result.Parsed.ResponseCode);
        }

        if (result.RetryAllowed)
        {
            var normalizedCode = NormalizeTechnicalIntegrationCode(
                result.Parsed.ResponseCode,
                result.Parsed.ResponseMessage,
                integrationCodePolicies);
            queue.LastErrorCode = normalizedCode;
            queue.LastErrorMessage = result.Parsed.ResponseMessage;

            if (queue.AttemptCount < _resilienceOptions.MaxAttempts)
            {
                queue.QueueStatus = IncomingNachaDispatchQueueStatus.RetryPending;
                queue.NextAttemptAtUtc = ComputeNextAttemptUtc(nowUtc, queue.AttemptCount);
                return new IncomingNachaStateTransitionOutcome(
                    IncomingNachaPostProcessingCounter.RetryPending,
                    "IntegrationRetryableFailed",
                    "RetryPending",
                    $"Falla técnica retryable. Reintento programado para {queue.NextAttemptAtUtc:O}.",
                    normalizedCode);
            }

            queue.QueueStatus = IncomingNachaDispatchQueueStatus.FailedFinal;
            queue.NextAttemptAtUtc = null;
            return new IncomingNachaStateTransitionOutcome(
                IncomingNachaPostProcessingCounter.FailedFinal,
                "MaxAttemptsExceeded",
                "FailedFinal",
                "Se agotó la política de reintentos para error técnico.",
                normalizedCode);
        }

        queue.QueueStatus = IncomingNachaDispatchQueueStatus.FailedFinal;
        queue.NextAttemptAtUtc = null;
        queue.LastErrorCode = "IFUNC";
        return new IncomingNachaStateTransitionOutcome(
            IncomingNachaPostProcessingCounter.FailedFinal,
            "IntegrationNonRetryableFailed",
            "FailedFinal",
            "Rechazo funcional no retryable.",
            queue.LastErrorCode);
    }

    public static void BlockControlledFailure(IncomingNachaDispatchQueue queue, string errorCode, string message)
    {
        queue.QueueStatus = IncomingNachaDispatchQueueStatus.Blocked;
        queue.LastErrorCode = errorCode;
        queue.LastErrorMessage = message;
        queue.NextAttemptAtUtc = null;
    }

    public static void BlockUncertainFailure(IncomingNachaDispatchQueue queue, string errorCode, string message)
    {
        queue.LastErrorCode = errorCode;
        queue.LastErrorMessage = message;
        queue.QueueStatus = IncomingNachaDispatchQueueStatus.Blocked;
        queue.NextAttemptAtUtc = null;
    }

    public static string NormalizeTechnicalIntegrationCode(
        string responseCode,
        string message,
        IReadOnlyDictionary<string, bool> integrationCodePolicies)
    {
        var normalizedCode = (responseCode ?? string.Empty).Trim().ToUpperInvariant();
        var normalizedMessage = (message ?? string.Empty).ToUpperInvariant();

        var candidate = normalizedCode.Contains("503") ? "I503"
            : normalizedCode.Contains("500") ? "I500"
            : normalizedCode.Contains("TIMEOUT") || normalizedMessage.Contains("TIMEOUT") ? "ITIMEOUT"
            : normalizedCode.Contains("SOAP") || normalizedMessage.Contains("SOAP") ? "ISOAP"
            : "I500";

        if (integrationCodePolicies.Count == 0)
        {
            return candidate;
        }

        return integrationCodePolicies.ContainsKey(candidate) ? candidate : "I500";
    }

    private DateTime ComputeNextAttemptUtc(DateTime nowUtc, int attemptCount)
    {
        var safeInitial = Math.Max(1, _resilienceOptions.InitialBackoffSeconds);
        var safeMultiplier = _resilienceOptions.BackoffMultiplier <= 1 ? 2d : _resilienceOptions.BackoffMultiplier;
        var exp = Math.Pow(safeMultiplier, Math.Max(0, attemptCount - 1));
        var seconds = Math.Min(_resilienceOptions.MaxBackoffSeconds, (int)Math.Round(safeInitial * exp, MidpointRounding.AwayFromZero));

        if (_resilienceOptions.EnableJitter)
        {
            seconds += Random.Shared.Next(0, Math.Max(1, _resilienceOptions.JitterMaxSeconds + 1));
        }

        return nowUtc.AddSeconds(seconds);
    }

    private void ApplySuccessfulTransactionState(
        AchTransaction transaction,
        IntegrationResponseCatalogResult catalog,
        DateTime processedAtUtc)
    {
        if (!Enum.TryParse<AchTransferStateEnum>(catalog.TargetTransactionState, true, out var targetState)
            || transaction.State == targetState)
        {
            return;
        }

        if (transaction.State != AchTransferStateEnum.Pending
            || targetState != AchTransferStateEnum.AppliedTacitly)
        {
            throw new InvalidOperationException(
                $"CORE_RESPONSE_TARGET_STATE_INVALID: no se permite aplicar {catalog.TargetTransactionState} desde {transaction.State}.");
        }

        var previous = transaction.State;
        transaction.State = targetState;
        transaction.StateChangedAtUtc = processedAtUtc;
        _context.AchTransactionStateEvents.Add(new AchTransactionStateEvent
        {
            AchTransactionId = transaction.Id,
            FromState = previous,
            ToState = targetState,
            Source = AchStateEventSourceEnum.System,
            PayloadJson = JsonSerializer.Serialize(new
            {
                responseCatalogId = catalog.CatalogId,
                category = catalog.Category,
                method = catalog.Method,
                businessStatus = catalog.BusinessStatus.ToString()
            })
        });
    }
}

internal sealed record IncomingNachaDispatchPlan(
    TransactionIntegrationOperationResult? Operation,
    bool DelegateToContrapartidas);

/// <summary>Stage 5: resolve which downstream operation, if any, is planned for the queued transaction.</summary>
internal sealed class IncomingNachaDispatchPlanningStage
{
    private readonly ITransactionIntegrationOperationResolver? _operationResolver;

    public IncomingNachaDispatchPlanningStage(ITransactionIntegrationOperationResolver? operationResolver)
    {
        _operationResolver = operationResolver;
    }

    public async Task<IncomingNachaDispatchPlan> PlanAsync(IncomingNachaDispatchQueue queue, CancellationToken ct)
    {
        var operation = _operationResolver is null
            ? null
            : await _operationResolver.ResolveAsync(queue.AchTransaction, ct);
        var delegateToContrapartidas = operation is not null
            && operation.IsSupported
            && operation.OperationKey == IntegrationGuaranteeConstants.ProcContrapartidas;
        return new IncomingNachaDispatchPlan(operation, delegateToContrapartidas);
    }
}

internal sealed record IncomingNachaPreparedDispatch(
    TransactionIntegrationOperationResult Operation,
    IntegrationMappingReadinessResult Readiness,
    ProcTransaccionesRequestResolution Resolution,
    string RequestXml);

internal sealed record IncomingNachaGuardrailEvent(
    string EventType,
    string EventStatus,
    string Message,
    string Code);

internal sealed record IncomingNachaSendResult(string ResponseXml, IncomingNachaGuardrailEvent? GuardrailEvent);

internal sealed record IncomingNachaExternalDispatchResult(
    ProcTransaccionesParsedResponse Parsed,
    string ResponseXml,
    DateTime FinishedAtUtc,
    IntegrationTransportStatus TransportStatus,
    IntegrationResponseCatalogResult? CatalogResult,
    bool BusinessSuccess,
    bool RetryAllowed);

/// <summary>Stage 6: prepare and execute the existing Proc_Transacciones external dispatch without adding retries.</summary>
internal sealed class IncomingNachaExternalDispatchStage
{
    private const string SoapMethodName = IntegrationGuaranteeConstants.ProcTransacciones;
    private readonly AchDbContext _context;
    private readonly IProcTransaccionesRequestMapper _mapper;
    private readonly IProcTransaccionesResponseParser _parser;
    private readonly IWscfaachSoapClient _soapClient;
    private readonly ITransactionIntegrationOperationResolver? _operationResolver;
    private readonly IIntegrationMappingReadinessService? _mappingReadinessService;
    private readonly IIncomingNachaLocalLivePreparationService? _localLivePreparationService;
    private readonly IIntegrationResponseCatalogResolver? _responseCatalogResolver;
    private readonly TimeProvider _timeProvider;

    public IncomingNachaExternalDispatchStage(
        AchDbContext context,
        IProcTransaccionesRequestMapper mapper,
        IProcTransaccionesResponseParser parser,
        IWscfaachSoapClient soapClient,
        ITransactionIntegrationOperationResolver? operationResolver,
        IIntegrationMappingReadinessService? mappingReadinessService,
        IIncomingNachaLocalLivePreparationService? localLivePreparationService,
        IIntegrationResponseCatalogResolver? responseCatalogResolver,
        TimeProvider timeProvider)
    {
        _context = context;
        _mapper = mapper;
        _parser = parser;
        _soapClient = soapClient;
        _operationResolver = operationResolver;
        _mappingReadinessService = mappingReadinessService;
        _localLivePreparationService = localLivePreparationService;
        _responseCatalogResolver = responseCatalogResolver;
        _timeProvider = timeProvider;
    }

    public async Task<IncomingNachaPreparedDispatch> PrepareAsync(
        IncomingNachaDispatchQueue queue,
        IncomingNachaFileIngestion ingestion,
        IncomingNachaEntryClassification classification,
        CancellationToken ct)
    {
        if (_localLivePreparationService is not null)
        {
            var entry = await _context.EntryDetails
                .SingleAsync(x => x.EntryDetailID == classification.EntryDetailId, ct);
            await _localLivePreparationService.EnsureAsync(ingestion, entry, classification.FunctionalClass, ct);
        }

        var readinessContext = await EnsureProcTransaccionesReadinessAsync(queue.AchTransaction, ct);
        var resolution = await _mapper.ResolveAsync(
            queue,
            ingestion,
            classification,
            queue.AchTransaction,
            queue.AchTransaction.AchCycle,
            _timeProvider.GetLocalNow().DateTime,
            ct);
        EnsureSnapshotConsistency(readinessContext.Readiness, resolution);
        var requestXml = _mapper.BuildSoapBody(resolution.Contract);
        return new IncomingNachaPreparedDispatch(
            readinessContext.Operation,
            readinessContext.Readiness,
            resolution,
            requestXml);
    }

    public async Task<IncomingNachaSendResult> SendAsync(
        string requestXml,
        IncomingNachaDispatchQueue queue,
        string operatingMode,
        CancellationToken ct)
    {
        if (IncomingNachaPostProcessingMode.IsLive(operatingMode))
        {
            return new IncomingNachaSendResult(await _soapClient.ProcTransaccionesAsync(requestXml, ct), null);
        }

        if (IncomingNachaPostProcessingMode.IsDisabled(operatingMode))
        {
            throw new InvalidOperationException("PROC_TRANSACCIONES_DISABLED: Proc_Transacciones disabled; no se transmite externamente.");
        }

        const string code = "PROC_TRANSACCIONES_DRY_RUN";
        const string message = "Proc_Transacciones dry-run: payload generado, no transmitido.";
        var response = $"""
            <Envelope>
              <Body>
                <Proc_TransaccionesResponse>
                  <RTAACH>{code}</RTAACH>
                  <RTALOC>{message}</RTALOC>
                </Proc_TransaccionesResponse>
              </Body>
            </Envelope>
            """;
        return new IncomingNachaSendResult(
            response,
            new IncomingNachaGuardrailEvent("ProcTransaccionesDryRunGuardrail", "Blocked", message, code));
    }

    public async Task<IncomingNachaExternalDispatchResult> InterpretAsync(
        string responseXml,
        string operatingMode,
        CancellationToken ct)
    {
        var parsed = _parser.Parse(responseXml);
        var finishedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var transportStatus = ResolveTransportStatus(parsed, operatingMode);
        var catalogResult = transportStatus == IntegrationTransportStatus.Succeeded
            ? await ResolveCoreResponseAsync(parsed.ResponseCode, finishedAtUtc, ct)
            : null;
        var businessSuccess = transportStatus == IntegrationTransportStatus.Succeeded
            && catalogResult is { IsKnownCode: true, BusinessStatus: IntegrationResponseBusinessStatus.Success };
        var retryAllowed = catalogResult is null ? parsed.IsRetryable : catalogResult.RetryAllowed;
        return new IncomingNachaExternalDispatchResult(
            parsed,
            responseXml,
            finishedAtUtc,
            transportStatus,
            catalogResult,
            businessSuccess,
            retryAllowed);
    }

    private async Task<(TransactionIntegrationOperationResult Operation, IntegrationMappingReadinessResult Readiness)> EnsureProcTransaccionesReadinessAsync(
        AchTransaction transaction,
        CancellationToken ct)
    {
        if (_operationResolver is null || _mappingReadinessService is null)
        {
            throw new InvalidOperationException(
                "PROC_TRANSACCIONES_READINESS_SERVICE_UNAVAILABLE: no se puede evaluar readiness de Proc_Transacciones sin resolver de operación o servicio de readiness.");
        }

        var operation = await _operationResolver.ResolveAsync(transaction, ct);
        if (operation is null)
        {
            throw new InvalidOperationException(
                "PROC_TRANSACCIONES_READINESS_SERVICE_UNAVAILABLE: el resolvedor de operación devolvió null para Proc_Transacciones.");
        }
        if (!operation.IsSupported
            || operation.OperationKey != IntegrationGuaranteeConstants.ProcTransacciones
            || operation.MappingPurpose != IntegrationGuaranteeConstants.MonetaryCreditRequest)
        {
            throw new InvalidOperationException(
                $"INTEGRATION_OPERATION_MISMATCH: la transaccion {transaction.Id} no corresponde a Proc_Transacciones/MonetaryCreditRequest.");
        }

        var readiness = await _mappingReadinessService.EvaluateAsync(operation, ct);
        if (readiness is null)
        {
            throw new InvalidOperationException(
                "PROC_TRANSACCIONES_READINESS_SERVICE_UNAVAILABLE: el servicio de readiness devolvió null para Proc_Transacciones.");
        }
        if (readiness.Status == "Failed" || !readiness.IsReady)
        {
            throw new InvalidOperationException(
                $"{readiness.Code}: no se puede construir payload Proc_Transacciones para transaccion {transaction.Id}; faltan mappings requeridos.");
        }
        if (!readiness.CanBuildPayload)
        {
            throw new InvalidOperationException(
                $"{readiness.Code}: readiness de Proc_Transacciones no permite construir payload para transaccion {transaction.Id}.");
        }
        if (readiness.UsesFallback)
        {
            throw new InvalidOperationException(
                "PROC_TRANSACCIONES_REQUIRED_FIELD_USES_FALLBACK: Proc_Transacciones no puede marcar readiness Ok ni construir payload con fallback requerido.");
        }

        return (operation, readiness);
    }

    private static void EnsureSnapshotConsistency(
        IntegrationMappingReadinessResult readiness,
        ProcTransaccionesRequestResolution resolution)
    {
        if (!readiness.MappingSetId.HasValue
            || !readiness.MappingVersion.HasValue
            || string.IsNullOrWhiteSpace(readiness.MappingSnapshotHash))
        {
            throw new InvalidOperationException("MAPPING_SNAPSHOT_CHANGED: readiness no expuso identidad completa del mapping.");
        }

        if (readiness.MappingSetId.Value != resolution.MappingSetId
            || readiness.MappingVersion.Value != resolution.MappingVersion
            || !string.Equals(readiness.MappingSnapshotHash, resolution.MappingSnapshotHash, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("MAPPING_SNAPSHOT_CHANGED: el snapshot del mapping cambió entre readiness y dispatch.");
        }
    }

    private async Task<IntegrationResponseCatalogResult> ResolveCoreResponseAsync(
        string? responseCode,
        DateTime processedAtUtc,
        CancellationToken ct)
    {
        if (_responseCatalogResolver is not null)
        {
            return await _responseCatalogResolver.ResolveAsync(
                IntegrationResponseCategory.CoreSoapResponse,
                SoapMethodName,
                responseCode,
                processedAtUtc,
                ct);
        }

        return new IntegrationResponseCatalogResult(
            null,
            (responseCode ?? string.Empty).Trim().ToUpperInvariant(),
            "Código pendiente de parametrización",
            IntegrationResponseCategory.CoreSoapResponse,
            IntegrationResponseCategory.CoreSoapResponse,
            SoapMethodName,
            IntegrationResponseBusinessStatus.PendingCatalog,
            false,
            true,
            false,
            string.Empty,
            false);
    }

    private static IntegrationTransportStatus ResolveTransportStatus(
        ProcTransaccionesParsedResponse parsed,
        string operatingMode)
    {
        if (IncomingNachaPostProcessingMode.IsDisabled(operatingMode)
            || IncomingNachaPostProcessingMode.IsDryRunLike(operatingMode))
        {
            return IntegrationTransportStatus.NotExecuted;
        }

        if (parsed.ResponseCode.Contains("TIMEOUT", StringComparison.OrdinalIgnoreCase)
            || parsed.ResponseMessage.Contains("TIMEOUT", StringComparison.OrdinalIgnoreCase))
        {
            return IntegrationTransportStatus.TimedOut;
        }

        if (parsed.ResponseCode.Equals("SOAP_FAULT", StringComparison.OrdinalIgnoreCase)
            || parsed.ResponseCode.Equals("PARSER_ERROR", StringComparison.OrdinalIgnoreCase)
            || parsed.ResponseCode.Equals("EMPTY", StringComparison.OrdinalIgnoreCase)
            || parsed.RawResponse.Contains("<Fault", StringComparison.OrdinalIgnoreCase)
            || parsed.RawResponse.Contains(":Fault", StringComparison.OrdinalIgnoreCase))
        {
            return IntegrationTransportStatus.Failed;
        }

        return IntegrationTransportStatus.Succeeded;
    }
}

/// <summary>Stage 7: integration audit, result projection and processing events.</summary>
internal sealed class IncomingNachaAuditEventsStage
{
    private const string SoapMethodName = IntegrationGuaranteeConstants.ProcTransacciones;
    private const string TechnicalStatusSucceeded = "Succeeded";
    private const string TechnicalStatusFunctionalRejection = "FunctionalRejection";
    private const string TechnicalStatusSoapFault = "SoapFault";
    private const string TechnicalStatusRetryableFailure = "RetryableFailure";
    private const string TechnicalStatusParserError = "ParserError";
    private const string TechnicalStatusTechnicalException = "TechnicalException";
    private const string TechnicalStatusDryRun = "DryRun";
    private const string TechnicalStatusDisabled = "Disabled";
    private const string TechnicalStatusUnknownFailure = "UnknownFailure";

    private readonly AchDbContext _context;
    private readonly IIntegrationMappingTraceWriter? _mappingTraceWriter;
    private readonly IIncomingNachaAchResultResolver? _achResultResolver;
    private readonly TimeProvider _timeProvider;

    public IncomingNachaAuditEventsStage(
        AchDbContext context,
        IIntegrationMappingTraceWriter? mappingTraceWriter,
        IIncomingNachaAchResultResolver? achResultResolver,
        TimeProvider timeProvider)
    {
        _context = context;
        _mappingTraceWriter = mappingTraceWriter;
        _achResultResolver = achResultResolver;
        _timeProvider = timeProvider;
    }

    public IncomingNachaIntegrationExecution CreateExecution(
        IncomingNachaDispatchQueue queue,
        IncomingNachaEntryClassification? classification,
        string operatingMode,
        string correlationId)
    {
        var execution = new IncomingNachaIntegrationExecution
        {
            DispatchQueueId = queue.Id,
            EntryDetailId = classification?.EntryDetailId,
            AttemptNumber = queue.AttemptCount,
            ClearingHouseId = queue.ClearingHouseId,
            MethodName = SoapMethodName,
            SoapMethodName = SoapMethodName,
            ExecutionMode = NormalizeAuditValue(operatingMode, 20),
            CorrelationId = correlationId,
            StartedAtUtc = _timeProvider.GetUtcNow().UtcDateTime
        };
        _context.IncomingNachaIntegrationExecution.Add(execution);
        return execution;
    }

    public void AddAutomaticEvent(
        IncomingNachaDispatchQueue queue,
        string eventType,
        string eventStatus,
        string message,
        string? code = null)
    {
        _context.IncomingNachaProcessingEvents.Add(new IncomingNachaProcessingEvent
        {
            IncomingNachaFileIngestionId = queue.IncomingNachaFileIngestionId,
            AchTransactionId = queue.AchTransactionId,
            EventType = eventType,
            EventStatus = eventStatus,
            Message = string.IsNullOrWhiteSpace(code) ? message : $"{code}: {message}",
            EvidenceJson = JsonSerializer.Serialize(new
            {
                queueId = queue.Id,
                status = queue.QueueStatus,
                queue.AttemptCount,
                code = code ?? string.Empty
            }),
            OccurredAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            RaisedBy = "inbound.dispatch.orchestrator"
        });
    }

    public async Task RecordPreparedRequestAsync(
        IncomingNachaIntegrationExecution execution,
        IncomingNachaPreparedDispatch preparation,
        IncomingNachaDispatchQueue queue,
        string correlationId,
        string soapEndpoint,
        string operatingMode,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        execution.MappingSetId = preparation.Resolution.MappingSetId;
        execution.MappingVersion = preparation.Resolution.MappingVersion;
        execution.MappingSnapshotHash = preparation.Resolution.MappingSnapshotHash;
        execution.RequestPayloadXml = preparation.RequestXml;
        execution.RequestHash = Hash(preparation.RequestXml);
        execution.SoapEndpoint = NormalizeAuditValue(soapEndpoint, 500);

        if (_mappingTraceWriter is null)
        {
            return;
        }

        await _mappingTraceWriter.WriteAsync(
            preparation.Operation,
            preparation.Resolution.Contract,
            queue.AchTransactionId,
            queue.AchTransaction.Reference,
            correlationId,
            dryRun: !IncomingNachaPostProcessingMode.IsLive(operatingMode),
            externalTransmission: IncomingNachaPostProcessingMode.IsLive(operatingMode),
            ct);
    }

    public async Task RecordResponseAsync(
        IncomingNachaIntegrationExecution execution,
        IncomingNachaDispatchQueue queue,
        IncomingNachaExternalDispatchResult result,
        string operatingMode,
        CancellationToken ct)
    {
        ApplyResponseAudit(execution, result, operatingMode);
        await ApplyAchResultAsync(
            execution,
            queue,
            result.Parsed.ResponseCode,
            result.FinishedAtUtc,
            result.TransportStatus,
            ct);
    }

    public static string ResolveControlledErrorCode(InvalidOperationException ex)
        => ex.Message.StartsWith("PROC_TRANSACCIONES_DISABLED:", StringComparison.OrdinalIgnoreCase)
            ? "PROC_TRANSACCIONES_DISABLED"
            : ex.Message.StartsWith("PROC_TRANSACCIONES_READINESS_SERVICE_UNAVAILABLE:", StringComparison.OrdinalIgnoreCase)
                ? "PROC_TRANSACCIONES_READINESS_SERVICE_UNAVAILABLE"
                : ex.Message.StartsWith("MAPPING_SNAPSHOT_CHANGED:", StringComparison.OrdinalIgnoreCase)
                    ? "MAPPING_SNAPSHOT_CHANGED"
                    : "MAPPING_INVALID";

    public void RecordControlledFailure(
        IncomingNachaIntegrationExecution execution,
        IncomingNachaDispatchQueue queue,
        InvalidOperationException ex)
    {
        execution.ResponseCode = queue.LastErrorCode;
        execution.ResponseMessage = ex.Message;
        execution.SoapResponseCode = queue.LastErrorCode;
        execution.SoapResponseDescription = ex.Message;
        execution.SoapTechnicalStatus = queue.LastErrorCode.Equals("PROC_TRANSACCIONES_DISABLED", StringComparison.OrdinalIgnoreCase)
            ? TechnicalStatusDisabled
            : TechnicalStatusTechnicalException;
        execution.IsSuccessful = false;
        execution.IsFunctionalRejection = false;
        execution.IsTechnicalFailure = true;
        execution.ProcessingStatus = IncomingNachaIndividualProcessingStatus.TechnicalFailed;
        execution.BusinessOutcome = IncomingNachaBusinessOutcome.NotProcessed;
        execution.ResultCode = string.Empty;
        execution.ResultDescription = string.Empty;
        execution.TechnicalErrorCode = queue.LastErrorCode;
        execution.TechnicalErrorMessage = ex.Message;
        execution.TransportStatus = IntegrationTransportStatus.Failed;
        execution.BusinessStatus = IntegrationResponseBusinessStatus.Unknown;
        execution.RetryAllowed = false;
        execution.RequiresManualReview = false;
        execution.ProcessedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        execution.TechnicalException = BuildTechnicalException(ex);
        execution.RequestHash = execution.RequestHash == string.Empty ? Hash(ex.Message) : execution.RequestHash;
        execution.FinishedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        execution.DurationMs = CalculateDurationMs(execution.StartedAtUtc, execution.FinishedAtUtc.Value);
    }

    public void RecordUncertainFailure(
        IncomingNachaIntegrationExecution execution,
        IncomingNachaDispatchQueue queue,
        Exception ex)
    {
        execution.ResponseCode = queue.LastErrorCode;
        execution.ResponseMessage = ex.Message;
        execution.FinishedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        execution.IsSuccess = false;
        execution.IsRetryable = false;
        execution.SoapResponseCode = queue.LastErrorCode;
        execution.SoapResponseDescription = ex.Message;
        execution.SoapTechnicalStatus = TechnicalStatusTechnicalException;
        execution.IsSuccessful = false;
        execution.IsFunctionalRejection = false;
        execution.IsTechnicalFailure = true;
        execution.ProcessingStatus = IncomingNachaIndividualProcessingStatus.TechnicalFailed;
        execution.BusinessOutcome = IncomingNachaBusinessOutcome.NotProcessed;
        execution.ResultCode = string.Empty;
        execution.ResultDescription = string.Empty;
        execution.TechnicalErrorCode = queue.LastErrorCode;
        execution.TechnicalErrorMessage = ex.Message;
        execution.TransportStatus = IntegrationTransportStatus.Failed;
        execution.BusinessStatus = IntegrationResponseBusinessStatus.Unknown;
        execution.RetryAllowed = false;
        execution.RequiresManualReview = true;
        execution.ProcessedAtUtc = execution.FinishedAtUtc;
        execution.TechnicalException = BuildTechnicalException(ex);
        execution.DurationMs = CalculateDurationMs(execution.StartedAtUtc, execution.FinishedAtUtc.Value);
    }

    private void ApplyResponseAudit(
        IncomingNachaIntegrationExecution execution,
        IncomingNachaExternalDispatchResult result,
        string operatingMode)
    {
        var parsed = result.Parsed;
        var soapCode = NormalizeAuditValue(parsed.ResponseCode, 80);
        var soapDescription = NormalizeAuditValue(result.CatalogResult?.Description ?? parsed.ResponseMessage, 4000);
        var businessStatus = result.CatalogResult?.BusinessStatus ?? IntegrationResponseBusinessStatus.Unknown;
        var isFunctionalRejection = businessStatus == IntegrationResponseBusinessStatus.Rejected;
        var isTechnicalFailure = result.TransportStatus is IntegrationTransportStatus.Failed or IntegrationTransportStatus.TimedOut;

        execution.ResponsePayloadXml = result.ResponseXml;
        execution.ResponseHash = Hash(result.ResponseXml);
        execution.SoapResponseCode = soapCode;
        execution.SoapResponseDescription = soapDescription;
        execution.SoapTechnicalStatus = result.TransportStatus == IntegrationTransportStatus.Succeeded
            ? TechnicalStatusSucceeded
            : ResolveSoapTechnicalStatus(parsed, result.BusinessSuccess, isFunctionalRejection, operatingMode);
        execution.ResponseCatalogId = result.CatalogResult?.CatalogId;
        execution.TransportStatus = result.TransportStatus;
        execution.BusinessStatus = businessStatus;
        execution.RetryAllowed = result.RetryAllowed;
        execution.RequiresManualReview = result.CatalogResult?.RequiresManualReview ?? false;
        execution.ProcessedAtUtc = result.FinishedAtUtc;
        execution.IsSuccessful = result.BusinessSuccess;
        execution.IsFunctionalRejection = isFunctionalRejection;
        execution.IsTechnicalFailure = isTechnicalFailure;
        execution.ResponseCode = soapCode;
        execution.ResponseMessage = soapDescription;
        execution.IsSuccess = result.BusinessSuccess;
        execution.IsRetryable = result.RetryAllowed;
        execution.FinishedAtUtc = result.FinishedAtUtc;
        execution.DurationMs = CalculateDurationMs(execution.StartedAtUtc, result.FinishedAtUtc);
    }

    private async Task ApplyAchResultAsync(
        IncomingNachaIntegrationExecution execution,
        IncomingNachaDispatchQueue queue,
        string responseCode,
        DateTime processedAtUtc,
        IntegrationTransportStatus transportStatus,
        CancellationToken ct)
    {
        if (transportStatus != IntegrationTransportStatus.Succeeded)
        {
            execution.ProcessingStatus = IncomingNachaIndividualProcessingStatus.TechnicalFailed;
            execution.BusinessOutcome = IncomingNachaBusinessOutcome.NotProcessed;
            execution.ResultCode = string.Empty;
            execution.ResultDescription = string.Empty;
            return;
        }

        var resolution = _achResultResolver is null
            ? null
            : await _achResultResolver.ResolveAsync(new IncomingNachaAchResultRequest(
                queue.ClearingHouseId,
                responseCode,
                queue.AchTransaction.Type == TransactionTypeEnum.Return ? AchReturnFlowType.Return : AchReturnFlowType.Any,
                IsDebit: queue.AchTransaction.Type == TransactionTypeEnum.Debit,
                IsCredit: queue.AchTransaction.Type == TransactionTypeEnum.Credit,
                IsPrenotification: queue.AchTransaction.Type == TransactionTypeEnum.Prenotification,
                IsReturn: queue.AchTransaction.Type == TransactionTypeEnum.Return,
                processedAtUtc), ct);

        execution.ProcessingStatus = IncomingNachaIndividualProcessingStatus.Completed;
        execution.AchReturnCodeId = resolution?.AchReturnCodeId;
        execution.ResultCode = NormalizeAuditValue(responseCode, 20);
        execution.ResultDescription = NormalizeAuditValue(
            resolution is { IsResolved: true }
                ? resolution.ResultDescription
                : execution.SoapResponseDescription,
            500);
        execution.BusinessOutcome = resolution is { IsResolved: true }
            ? resolution.BusinessOutcome
            : execution.BusinessStatus switch
            {
                IntegrationResponseBusinessStatus.Success => IncomingNachaBusinessOutcome.Successful,
                IntegrationResponseBusinessStatus.Rejected => IncomingNachaBusinessOutcome.Rejected,
                _ => IncomingNachaBusinessOutcome.PendingResponse
            };
    }

    private static string ResolveSoapTechnicalStatus(
        ProcTransaccionesParsedResponse parsed,
        bool isSuccessful,
        bool isFunctionalRejection,
        string operatingMode)
    {
        if (IncomingNachaPostProcessingMode.IsDisabled(operatingMode))
        {
            return TechnicalStatusDisabled;
        }
        if (IncomingNachaPostProcessingMode.IsDryRunLike(operatingMode))
        {
            return TechnicalStatusDryRun;
        }
        if (isSuccessful)
        {
            return TechnicalStatusSucceeded;
        }
        if (parsed.ResponseCode.Equals("SOAP_FAULT", StringComparison.OrdinalIgnoreCase)
            || parsed.RawResponse.Contains("<Fault", StringComparison.OrdinalIgnoreCase)
            || parsed.RawResponse.Contains(":Fault", StringComparison.OrdinalIgnoreCase))
        {
            return TechnicalStatusSoapFault;
        }
        if (parsed.ResponseCode.Equals("PARSER_ERROR", StringComparison.OrdinalIgnoreCase))
        {
            return TechnicalStatusParserError;
        }
        if (isFunctionalRejection)
        {
            return TechnicalStatusFunctionalRejection;
        }
        if (parsed.IsRetryable)
        {
            return TechnicalStatusRetryableFailure;
        }
        return TechnicalStatusUnknownFailure;
    }

    private static string Hash(string payload)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload ?? string.Empty));
        return Convert.ToHexString(bytes);
    }

    private static long CalculateDurationMs(DateTime startedAtUtc, DateTime finishedAtUtc)
        => Math.Max(0, (long)(finishedAtUtc - startedAtUtc).TotalMilliseconds);

    private static string BuildTechnicalException(Exception ex)
        => NormalizeAuditValue($"{ex.GetType().Name}: {ex.Message}", 4000);

    internal static string NormalizeAuditValue(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}

internal static class IncomingNachaPostProcessingMode
{
    public static string Normalize(string? mode, string fallback)
    {
        var candidate = string.IsNullOrWhiteSpace(mode) ? fallback : mode.Trim();
        if (string.Equals(candidate, "Live", StringComparison.OrdinalIgnoreCase))
        {
            return "Live";
        }
        if (string.Equals(candidate, "Disabled", StringComparison.OrdinalIgnoreCase))
        {
            return "Disabled";
        }
        return "DryRun";
    }

    public static bool IsLive(string mode)
        => string.Equals(mode, "Live", StringComparison.OrdinalIgnoreCase);

    public static bool IsDisabled(string mode)
        => string.Equals(mode, "Disabled", StringComparison.OrdinalIgnoreCase);

    public static bool IsDryRunLike(string mode)
        => !IsLive(mode) && !IsDisabled(mode);
}
