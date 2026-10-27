using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class IncomingNachaCommandCenterOperationsService : IIncomingNachaCommandCenterOperationsService
{
    private readonly AchDbContext _context;
    private readonly IIncomingNachaStateMachineService _stateMachineService;
    private readonly TimeProvider _timeProvider;

    public IncomingNachaCommandCenterOperationsService(
        AchDbContext context,
        IIncomingNachaStateMachineService stateMachineService,
        TimeProvider? timeProvider = null)
    {
        _context = context;
        _stateMachineService = stateMachineService;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task<IncomingNachaManualActionResultDto> RetryManualAsync(Guid queueId, IncomingNachaManualActionRequest request, string performedBy, CancellationToken ct = default)
        => ApplyManualActionAsync(queueId, request, performedBy, IncomingNachaDispatchEvent.ManualRetry, (q, req) =>
        {
            q.NextAttemptAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            q.LastErrorCode = string.Empty;
            q.LastErrorMessage = string.Empty;
            if (req.Priority.HasValue)
            {
                q.Priority = Math.Clamp(req.Priority.Value, 1, 999);
            }

            return "Reintento manual aplicado.";
        }, ct);

    public Task<IncomingNachaManualActionResultDto> UnblockManualAsync(Guid queueId, IncomingNachaManualActionRequest request, string performedBy, CancellationToken ct = default)
        => ApplyManualActionAsync(queueId, request, performedBy, IncomingNachaDispatchEvent.ManualUnblock, (q, req) =>
        {
            q.NextAttemptAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            q.LastErrorCode = string.Empty;
            q.LastErrorMessage = string.Empty;
            if (req.Priority.HasValue)
            {
                q.Priority = Math.Clamp(req.Priority.Value, 1, 999);
            }

            return "Desbloqueo manual aplicado.";
        }, ct);

    public Task<IncomingNachaManualActionResultDto> RequeueManualAsync(Guid queueId, IncomingNachaManualActionRequest request, string performedBy, CancellationToken ct = default)
        => ApplyManualActionAsync(queueId, request, performedBy, IncomingNachaDispatchEvent.ManualRequeue, (q, req) =>
        {
            q.NextAttemptAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            if (req.Priority.HasValue)
            {
                q.Priority = Math.Clamp(req.Priority.Value, 1, 999);
            }

            return "Nueva programación manual aplicada.";
        }, ct);

    public Task<IncomingNachaManualActionResultDto> MarkFailedFinalManualAsync(Guid queueId, IncomingNachaManualActionRequest request, string performedBy, CancellationToken ct = default)
        => ApplyManualActionAsync(queueId, request, performedBy, IncomingNachaDispatchEvent.ManualMarkFailedFinal, (q, _) =>
        {
            q.NextAttemptAtUtc = null;
            return "Marcado manual como FailedFinal.";
        }, ct);

    private async Task<IncomingNachaManualActionResultDto> ApplyManualActionAsync(
        Guid queueId,
        IncomingNachaManualActionRequest request,
        string performedBy,
        IncomingNachaDispatchEvent transitionEvent,
        Func<IncomingNachaDispatchQueue, IncomingNachaManualActionRequest, string> apply,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Justification) || request.Justification.Trim().Length < 8)
        {
            throw new InvalidOperationException("La justificación es obligatoria y debe tener al menos 8 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new InvalidOperationException("IdempotencyKey es obligatorio para acciones manuales.");
        }

        var normalizedKey = request.IdempotencyKey.Trim();
        var normalizedBy = string.IsNullOrWhiteSpace(performedBy) ? "ops.command-center" : performedBy;

        var queue = await _context.IncomingNachaDispatchQueue.FirstOrDefaultAsync(x => x.Id == queueId, ct)
                    ?? throw new InvalidOperationException("No existe item de cola indicado.");

        // F4.3: ACK/timeout CFA and replays must never be resolved by a blind
        // Command Center retry/unblock. The F5 release contract is not implemented.
        // Audit the rejected attempt using the existing transition event model.
        if (queue.SourceKind == IncomingNachaDispatchSourceKinds.AchColFileBased)
        {
            var canCloseWithoutSending = transitionEvent == IncomingNachaDispatchEvent.ManualMarkFailedFinal
                && queue.AttemptCount == 0
                && !await _context.IncomingNachaIntegrationExecution.AsNoTracking()
                    .AnyAsync(x => x.DispatchQueueId == queue.Id, ct);
            if (!canCloseWithoutSending)
            {
                const string code = "ACHCOL_FILEBASED_MANUAL_RELEASE_FORBIDDEN_F43";
                var reason = "Entrada ACHCOL retenida: no reintentar, desbloquear ni reencolar antes de F5; "
                    + "si existe un intento CFA, primero conciliar su resultado.";
                var rejected = new IncomingNachaDispatchTransitionDecision(
                    queue.QueueStatus, transitionEvent, false, null, code, reason);
                await RegisterTransitionEventAsync(
                    queue, normalizedBy, transitionEvent, "DispatchTransition",
                    $"Event:{transitionEvent};IdempotencyKey:{normalizedKey}",
                    "Rejected", queue.QueueStatus, queue.QueueStatus, request, rejected, ct);
                throw new InvalidOperationException($"[{code}] {reason}");
            }
        }

        var idempotentEventType = "DispatchTransition";
        var idempotentMessage = $"Event:{transitionEvent};IdempotencyKey:{normalizedKey}";
        var replayed = await _context.IncomingNachaProcessingEvents.AsNoTracking().AnyAsync(x =>
            x.IncomingNachaFileIngestionId == queue.IncomingNachaFileIngestionId
            && x.EventType == idempotentEventType
            && x.Message == idempotentMessage
            && x.AchTransactionId == queue.AchTransactionId, ct);

        if (replayed)
        {
            return new IncomingNachaManualActionResultDto(queue.Id, ToActionLabel(transitionEvent), queue.QueueStatus, queue.QueueStatus, true, "Solicitud idempotente ya aplicada previamente.")
            {
                ActionText = ToActionText(transitionEvent)
            };
        }

        var previousStatus = queue.QueueStatus;
        var transition = _stateMachineService.EvaluateDispatchTransition(previousStatus, transitionEvent);
        if (!transition.IsAllowed || !transition.NextStatus.HasValue)
        {
            await RegisterTransitionEventAsync(
                queue,
                normalizedBy,
                transitionEvent,
                idempotentEventType,
                idempotentMessage,
                "Rejected",
                previousStatus,
                previousStatus,
                request,
                transition,
                ct);

            throw new InvalidOperationException($"[{transition.ResultCode}] {transition.Message}");
        }

        queue.QueueStatus = transition.NextStatus.Value;
        var appliedMessage = apply(queue, request);

        await RegisterTransitionEventAsync(
            queue,
            normalizedBy,
            transitionEvent,
            idempotentEventType,
            idempotentMessage,
            "Applied",
            previousStatus,
            queue.QueueStatus,
            request,
            transition,
            ct);

        return new IncomingNachaManualActionResultDto(queue.Id, ToActionLabel(transitionEvent), previousStatus, queue.QueueStatus, false, $"{appliedMessage} [{transition.ResultCode}]")
        {
            ActionText = ToActionText(transitionEvent)
        };
    }

    private static string ToActionLabel(IncomingNachaDispatchEvent transitionEvent)
        => transitionEvent switch
        {
            IncomingNachaDispatchEvent.ManualRetry => "Retry",
            IncomingNachaDispatchEvent.ManualUnblock => "Unblock",
            IncomingNachaDispatchEvent.ManualRequeue => "Requeue",
            IncomingNachaDispatchEvent.ManualMarkFailedFinal => "MarkFailedFinal",
            _ => transitionEvent.ToString()
        };

    private static string ToActionText(IncomingNachaDispatchEvent transitionEvent)
        => transitionEvent switch
        {
            IncomingNachaDispatchEvent.ManualRetry => "Reintentar",
            IncomingNachaDispatchEvent.ManualUnblock => "Desbloquear",
            IncomingNachaDispatchEvent.ManualRequeue => "Volver a programar",
            IncomingNachaDispatchEvent.ManualMarkFailedFinal => "Marcar como falla final",
            _ => "Acción operativa"
        };

    private async Task RegisterTransitionEventAsync(
        IncomingNachaDispatchQueue queue,
        string normalizedBy,
        IncomingNachaDispatchEvent transitionEvent,
        string eventType,
        string eventMessage,
        string eventStatus,
        IncomingNachaDispatchQueueStatus previousStatus,
        IncomingNachaDispatchQueueStatus currentStatus,
        IncomingNachaManualActionRequest request,
        IncomingNachaDispatchTransitionDecision transition,
        CancellationToken ct)
    {
        _context.IncomingNachaProcessingEvents.Add(new IncomingNachaProcessingEvent
        {
            IncomingNachaFileIngestionId = queue.IncomingNachaFileIngestionId,
            AchTransactionId = queue.AchTransactionId,
            EventType = eventType,
            EventStatus = eventStatus,
            Message = eventMessage,
            EvidenceJson = JsonSerializer.Serialize(new
            {
                queueId = queue.Id,
                action = ToActionLabel(transitionEvent),
                transitionEvent,
                transition.ResultCode,
                transition.Message,
                previousStatus,
                currentStatus,
                request.Justification,
                request.Priority,
                request.IdempotencyKey,
                performedBy = normalizedBy
            }),
            OccurredAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            RaisedBy = normalizedBy
        });

        await _context.SaveChangesAsync(ct);
    }

}
