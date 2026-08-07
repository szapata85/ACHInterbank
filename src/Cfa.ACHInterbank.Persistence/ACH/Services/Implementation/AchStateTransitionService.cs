using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Nodes;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class AchStateTransitionService : IAchStateTransitionService
{
    private readonly AchDbContext _context;

    public AchStateTransitionService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<AchTransaction> TransitionAsync(
        int transactionId,
        AchTransferStateEnum toState,
        AchStateEventSourceEnum source,
        string? reasonCode = null,
        string? payloadJson = null,
        string? originalTraceRef = null,
        DateTime? changedAtUtc = null,
        CancellationToken ct = default)
    {
        var result = await TransitionAsync(new AchStateTransitionRequest(
            transactionId,
            toState,
            source,
            reasonCode,
            payloadJson,
            originalTraceRef,
            changedAtUtc), ct);
        return result.Transaction;
    }

    public async Task<AchStateTransitionResult> TransitionAsync(
        AchStateTransitionRequest request,
        CancellationToken ct = default)
    {
        var normalizedIdempotencyKey = NormalizeIdempotencyKey(request.IdempotencyKey);
        if (normalizedIdempotencyKey is not null)
        {
            var existingEvent = await _context.AchTransactionStateEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdempotencyKey == normalizedIdempotencyKey, ct);
            if (existingEvent is not null)
            {
                if (!RepresentsSameEvent(existingEvent, request))
                {
                    throw new InvalidOperationException("La identidad funcional recibida ya pertenece a un evento diferente y requiere revisión.");
                }

                var existingTransaction = await _context.AchTransactions
                    .FirstAsync(x => x.Id == request.TransactionId, ct);
                return new AchStateTransitionResult(existingTransaction, false, true);
            }
        }

        var transaction = await _context.AchTransactions
            .FirstOrDefaultAsync(x => x.Id == request.TransactionId, ct)
            ?? throw new KeyNotFoundException($"No existe la transacción ACH con id {request.TransactionId}.");

        var fromState = transaction.State;
        ValidateTransition(fromState, request.ToState);
        ValidateSourceForTargetState(request.ToState, request.Source);

        var normalizedReasonCode = NormalizeReasonCode(request.ReasonCode);
        var normalizedOriginalTraceRef = NormalizeOriginalTraceRef(request.OriginalTraceRef);
        ValidateTransitionPayload(request.ToState, normalizedReasonCode, normalizedOriginalTraceRef);

        transaction.State = request.ToState;
        transaction.StateChangedAtUtc = request.ChangedAtUtc?.ToUniversalTime() ?? DateTime.UtcNow;

        if (request.ToState is AchTransferStateEnum.ReturnedByOperator or AchTransferStateEnum.ReturnedByEpr)
        {
            EnforceReturnSla(transaction, request.ToState);

            transaction.ReturnReasonCode = normalizedReasonCode!;
            if (!string.IsNullOrWhiteSpace(normalizedOriginalTraceRef))
            {
                transaction.OriginalTraceRef = normalizedOriginalTraceRef;
            }
        }

        var stateEvent = new AchTransactionStateEvent
        {
            AchTransactionId = transaction.Id,
            FromState = fromState,
            ToState = request.ToState,
            Source = request.Source,
            ReasonCode = normalizedReasonCode,
            PayloadJson = BuildAuditPayloadJson(request.ToState, request.PayloadJson, normalizedReasonCode),
            IdempotencyKey = normalizedIdempotencyKey,
            ClearingHouseId = request.ClearingHouseId,
            AchReturnCodeId = request.AchReturnCodeId,
            ResolvedReasonDescription = NormalizeDescription(request.ResolvedReasonDescription),
            OccurredAtUtc = transaction.StateChangedAtUtc
        };

        await SynchronizePrenotificationThirdPartyAsync(
            transaction,
            request.ToState,
            normalizedReasonCode,
            transaction.StateChangedAtUtc,
            ct);

        await _context.AchTransactionStateEvents.AddAsync(stateEvent, ct);
        try
        {
            await _context.SaveChangesAsync(ct);
            return new AchStateTransitionResult(transaction, true, false);
        }
        catch (DbUpdateException ex) when (normalizedIdempotencyKey is not null && IsUniqueViolation(ex))
        {
            _context.ChangeTracker.Clear();

            var existingEvent = await _context.AchTransactionStateEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdempotencyKey == normalizedIdempotencyKey, ct);
            if (existingEvent is null)
            {
                throw;
            }
            if (!RepresentsSameEvent(existingEvent, request))
            {
                throw new InvalidOperationException("La identidad funcional recibida ya pertenece a un evento diferente y requiere revisión.");
            }

            var existingTransaction = await _context.AchTransactions
                .FirstAsync(x => x.Id == request.TransactionId, ct);
            return new AchStateTransitionResult(existingTransaction, false, true);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            var message = current.Message;
            if (message.Contains("unique", StringComparison.OrdinalIgnoreCase)
                || message.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                || message.Contains("2601", StringComparison.OrdinalIgnoreCase)
                || message.Contains("2627", StringComparison.OrdinalIgnoreCase)
                || message.Contains("23505", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool RepresentsSameEvent(AchTransactionStateEvent existing, AchStateTransitionRequest request)
        => existing.AchTransactionId == request.TransactionId
            && existing.ToState == request.ToState
            && string.Equals(existing.ReasonCode, NormalizeReasonCode(request.ReasonCode), StringComparison.OrdinalIgnoreCase)
            && existing.ClearingHouseId == request.ClearingHouseId
            && existing.AchReturnCodeId == request.AchReturnCodeId;

    private async Task SynchronizePrenotificationThirdPartyAsync(
        AchTransaction transaction,
        AchTransferStateEnum toState,
        string? reasonCode,
        DateTime changedAtUtc,
        CancellationToken ct)
    {
        if (!transaction.IsPrenotification)
        {
            return;
        }

        var result = toState switch
        {
            AchTransferStateEnum.AppliedTacitly or AchTransferStateEnum.Certified
                => CustomerThirdPartyStatusEnum.Active,
            AchTransferStateEnum.ReturnedByOperator or AchTransferStateEnum.ReturnedByEpr
                => CustomerThirdPartyStatusEnum.Rejected,
            _ => (CustomerThirdPartyStatusEnum?)null
        };

        if (!result.HasValue)
        {
            return;
        }

        var thirdParty = await _context.CustomerThirdParties
            .FirstOrDefaultAsync(x => x.PrenotificationTransactionId == transaction.Id, ct);
        if (thirdParty is null)
        {
            return;
        }

        var message = result == CustomerThirdPartyStatusEnum.Active
            ? toState == AchTransferStateEnum.AppliedTacitly
                ? "Prenotificación aprobada automáticamente por vencimiento del plazo normativo sin devolución."
                : "Prenotificación aprobada automáticamente mediante respuesta NACHA-M."
            : $"Prenotificación rechazada automáticamente mediante respuesta NACHA-M. Causal: {reasonCode}.";

        thirdParty.ApplyAutomaticNachaResult(
            result.Value,
            transaction.Id,
            transaction.AchCycleId,
            changedAtUtc,
            message,
            $"ach-state-transition:{transaction.Id}:{toState}");
    }

    private static void ValidateTransition(AchTransferStateEnum fromState, AchTransferStateEnum toState)
    {
        var isAllowed = (fromState, toState) switch
        {
            (AchTransferStateEnum.Pending, AchTransferStateEnum.ReturnedByOperator) => true,
            (AchTransferStateEnum.Pending, AchTransferStateEnum.ReturnedByEpr) => true,
            (AchTransferStateEnum.Pending, AchTransferStateEnum.AppliedTacitly) => true,
            (AchTransferStateEnum.Pending, AchTransferStateEnum.Certified) => true,
            (AchTransferStateEnum.ReturnedByEpr, AchTransferStateEnum.Certified) => true,
            (AchTransferStateEnum.AppliedTacitly, AchTransferStateEnum.Certified) => true,
            (AchTransferStateEnum.AppliedTacitly, AchTransferStateEnum.ReturnedByOperator) => true,
            (AchTransferStateEnum.AppliedTacitly, AchTransferStateEnum.ReturnedByEpr) => true,
            (AchTransferStateEnum.Certified, AchTransferStateEnum.ReturnedByOperator) => true,
            (AchTransferStateEnum.Certified, AchTransferStateEnum.ReturnedByEpr) => true,
            _ => false
        };

        if (!isAllowed)
        {
            throw new InvalidOperationException(
                $"Transición ACH no permitida: {fromState} -> {toState}.");
        }
    }

    private static void ValidateSourceForTargetState(AchTransferStateEnum toState, AchStateEventSourceEnum source)
    {
        var sourceIsValid = toState switch
        {
            AchTransferStateEnum.ReturnedByOperator => source == AchStateEventSourceEnum.Operator,
            AchTransferStateEnum.ReturnedByEpr => source == AchStateEventSourceEnum.Epr,
            AchTransferStateEnum.AppliedTacitly => source == AchStateEventSourceEnum.System,
            AchTransferStateEnum.Certified => source is AchStateEventSourceEnum.Claims or AchStateEventSourceEnum.System,
            _ => false
        };

        if (!sourceIsValid)
        {
            throw new InvalidOperationException(
                $"La fuente {source} no es válida para mover a estado {toState}.");
        }
    }

    private static void ValidateTransitionPayload(
        AchTransferStateEnum toState,
        string? reasonCode,
        string? originalTraceRef)
    {
        if (toState is AchTransferStateEnum.ReturnedByOperator or AchTransferStateEnum.ReturnedByEpr)
        {
            if (string.IsNullOrWhiteSpace(reasonCode))
            {
                throw new InvalidOperationException(
                    $"La transición a {toState} requiere causal de devolución/rechazo (código Dxx o Rxx).");
            }

            if (toState == AchTransferStateEnum.ReturnedByEpr && string.IsNullOrWhiteSpace(originalTraceRef))
            {
                throw new InvalidOperationException(
                    "La transición a ReturnedByEpr requiere OriginalTraceRef para trazabilidad con adenda 99.");
            }
        }
    }

    private static string? NormalizeReasonCode(string? reasonCode)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            return null;
        }

        var normalized = reasonCode.Trim().ToUpperInvariant();
        return normalized.Length <= 20 ? normalized : normalized[..20];
    }

    private static string? NormalizeOriginalTraceRef(string? originalTraceRef)
    {
        if (string.IsNullOrWhiteSpace(originalTraceRef))
        {
            return null;
        }

        var normalized = originalTraceRef.Trim();
        return normalized.Length <= 20 ? normalized : normalized[..20];
    }

    private static string? NormalizeIdempotencyKey(string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return null;
        }

        var normalized = idempotencyKey.Trim().ToLowerInvariant();
        if (normalized.Length > 128)
        {
            throw new InvalidOperationException("La identidad funcional del evento excede la longitud permitida.");
        }

        return normalized;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var normalized = description.Trim();
        return normalized.Length <= 300 ? normalized : normalized[..300];
    }

    private static string? BuildAuditPayloadJson(
        AchTransferStateEnum toState,
        string? payloadJson,
        string? reasonCode)
    {
        var marker = GetMirrorStatusLabel(toState);
        if (marker is null)
        {
            return string.IsNullOrWhiteSpace(payloadJson) ? null : payloadJson.Trim();
        }

        JsonObject payload;
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            payload = new JsonObject();
        }
        else
        {
            try
            {
                payload = JsonNode.Parse(payloadJson)?.AsObject() ?? new JsonObject
                {
                    ["payload"] = payloadJson.Trim()
                };
            }
            catch (Exception)
            {
                payload = new JsonObject
                {
                    ["payload"] = payloadJson.Trim()
                };
            }
        }

        payload["mirrorStatusLabel"] = marker;
        if (!string.IsNullOrWhiteSpace(reasonCode))
        {
            payload["returnReasonCode"] = reasonCode;
        }

        return payload.ToJsonString();
    }

    private static string? GetMirrorStatusLabel(AchTransferStateEnum toState)
    {
        return toState switch
        {
            AchTransferStateEnum.ReturnedByEpr or AchTransferStateEnum.ReturnedByOperator => "TRANSACCIÓN FALLIDA",
            AchTransferStateEnum.AppliedTacitly or AchTransferStateEnum.Certified => "TRANSACCIÓN APLICADA EXITOSAMENTE",
            _ => null
        };
    }

    private static void EnforceReturnSla(AchTransaction transaction, AchTransferStateEnum toState)
    {
        if (toState is not (AchTransferStateEnum.ReturnedByOperator or AchTransferStateEnum.ReturnedByEpr))
        {
            return;
        }

        if (!transaction.SlaDeadlineAtUtc.HasValue)
        {
            return;
        }

        var nowUtc = DateTime.UtcNow;
        if (nowUtc <= transaction.SlaDeadlineAtUtc.Value)
        {
            return;
        }

        if (transaction.Type == TransactionTypeEnum.Debit)
        {
            throw new InvalidOperationException(
                $"No se permite compensar devoluciones débito fuera del plazo de 4 ciclos. SLA vencido: {transaction.SlaDeadlineAtUtc:O}.");
        }
    }
}
