using Cfa.ACHInterbank.Application.ACH.Interfaces;
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
        var transaction = await _context.AchTransactions
            .FirstOrDefaultAsync(x => x.Id == transactionId, ct)
            ?? throw new KeyNotFoundException($"No existe la transacción ACH con id {transactionId}.");

        var fromState = transaction.State;
        ValidateTransition(fromState, toState);
        ValidateSourceForTargetState(toState, source);

        var normalizedReasonCode = NormalizeReasonCode(reasonCode);
        var normalizedOriginalTraceRef = NormalizeOriginalTraceRef(originalTraceRef);
        ValidateTransitionPayload(toState, normalizedReasonCode, normalizedOriginalTraceRef);

        transaction.State = toState;
        transaction.StateChangedAtUtc = changedAtUtc?.ToUniversalTime() ?? DateTime.UtcNow;

        if (toState is AchTransferStateEnum.ReturnedByOperator or AchTransferStateEnum.ReturnedByEpr)
        {
            EnforceReturnSla(transaction, toState);

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
            ToState = toState,
            Source = source,
            ReasonCode = normalizedReasonCode,
            PayloadJson = BuildAuditPayloadJson(toState, payloadJson, normalizedReasonCode)
        };

        await _context.AchTransactionStateEvents.AddAsync(stateEvent, ct);
        await _context.SaveChangesAsync(ct);

        return transaction;
    }

    private static void ValidateTransition(AchTransferStateEnum fromState, AchTransferStateEnum toState)
    {
        var isAllowed = (fromState, toState) switch
        {
            (AchTransferStateEnum.Pending, AchTransferStateEnum.ReturnedByOperator) => true,
            (AchTransferStateEnum.Pending, AchTransferStateEnum.ReturnedByEpr) => true,
            (AchTransferStateEnum.Pending, AchTransferStateEnum.AppliedTacitly) => true,
            (AchTransferStateEnum.ReturnedByEpr, AchTransferStateEnum.Certified) => true,
            (AchTransferStateEnum.AppliedTacitly, AchTransferStateEnum.Certified) => true,
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
