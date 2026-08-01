using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class IncomingNachaDispatchPlanner : IIncomingNachaDispatchPlanner
{
    private readonly AchDbContext _context;
    private readonly IIncomingNachaDispatchEligibilityPolicy _eligibility;
    private readonly IPaymentRailContextService? _paymentRailContextService;
    private readonly IPaymentRailOperationalStrategyResolver? _strategyResolver;
    private readonly IPaymentRailShadowCompareService? _shadowCompareService;
    private readonly ILogger<IncomingNachaDispatchPlanner> _logger;
    private readonly TimeProvider _timeProvider;

    public IncomingNachaDispatchPlanner(
        AchDbContext context,
        IIncomingNachaDispatchEligibilityPolicy eligibility,
        IPaymentRailContextService? paymentRailContextService = null,
        IPaymentRailOperationalStrategyResolver? strategyResolver = null,
        IPaymentRailShadowCompareService? shadowCompareService = null,
        ILogger<IncomingNachaDispatchPlanner>? logger = null,
        TimeProvider? timeProvider = null)
    {
        _context = context;
        _eligibility = eligibility;
        _paymentRailContextService = paymentRailContextService;
        _strategyResolver = strategyResolver;
        _shadowCompareService = shadowCompareService;
        _logger = logger ?? NullLogger<IncomingNachaDispatchPlanner>.Instance;
        _timeProvider = timeProvider ?? OperationalTimeProvider.SystemBogota;
    }

    public async Task<int> PlanForIngestionAsync(Guid ingestionId, string plannedBy, CancellationToken ct = default)
    {
        var ingestion = await _context.IncomingNachaFileIngestions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == ingestionId, ct)
            ?? throw new InvalidOperationException($"No existe ingesta {ingestionId}.");

        var candidates = await (
            from c in _context.IncomingNachaEntryClassifications
            join l in _context.IncomingNachaTransactionLinks on
                new { c.IncomingNachaFileIngestionId, EntryId = (int?)c.EntryDetailId, c.AddendaRecordId }
                equals new { l.IncomingNachaFileIngestionId, EntryId = l.EntryDetailId, l.AddendaRecordId }
            where c.IncomingNachaFileIngestionId == ingestionId && l.AchTransactionId != null
            select new { Classification = c, Link = l, AchTransactionId = l.AchTransactionId!.Value }
        ).ToListAsync(ct);

        var transactionIds = candidates.Select(x => x.AchTransactionId).Distinct().ToArray();
        var txMap = await _context.AchTransactions
            .Include(x => x.AchCycle)
            .Where(x => transactionIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);
        var clearingHouseIds = txMap.Values
            .Select(x => x.AchCycle.ClearingHouseId)
            .Distinct()
            .ToArray();
        var clearingHouseCodes = await _context.ClearingHouses
            .AsNoTracking()
            .Where(clearingHouse => clearingHouseIds.Contains(clearingHouse.ClearingHouseId))
            .ToDictionaryAsync(clearingHouse => clearingHouse.ClearingHouseId, clearingHouse => clearingHouse.Code, ct);
        var paymentRailCodes = await _context.ClearingHouseConfigs
            .AsNoTracking()
            .Where(config => clearingHouseIds.Contains(config.ClearingHouseId))
            .ToDictionaryAsync(config => config.ClearingHouseId, config => config.PaymentRailCode, ct);

        var created = 0;
        var nowUtcOffset = _timeProvider.GetUtcNow();
        var nowLocal = _timeProvider.GetLocalNow().LocalDateTime;
        foreach (var candidate in candidates)
        {
            if (!txMap.TryGetValue(candidate.AchTransactionId, out var tx))
            {
                continue;
            }

            var evaluation = await _eligibility.EvaluateAsync(
                ingestion,
                candidate.Classification,
                candidate.Link,
                tx,
                nowLocal: nowLocal,
                ct);

            var dispatchKey = BuildIdempotencyKey(ingestion.Id, tx.Id, tx.AchCycleId, candidate.Classification.Id);
            var existing = await _context.IncomingNachaDispatchQueue
                .FirstOrDefaultAsync(x => x.IdempotencyDispatchKey == dispatchKey, ct);
            if (existing is not null)
            {
                continue;
            }

            var status = evaluation.IsEligible
                ? IncomingNachaDispatchQueueStatus.Queued
                : evaluation.IsWaitingWindow
                    ? IncomingNachaDispatchQueueStatus.WaitingWindow
                    : IncomingNachaDispatchQueueStatus.Blocked;

            var nextAttemptAtUtc = status == IncomingNachaDispatchQueueStatus.Queued
                ? nowUtcOffset.UtcDateTime
                : (DateTime?)null;
            var lastErrorCode = status == IncomingNachaDispatchQueueStatus.Blocked
                ? "POLICY_BLOCKED"
                : string.Empty;
            var queueReason = evaluation.Reason;

            if (status == IncomingNachaDispatchQueueStatus.WaitingWindow)
            {
                var window = IncomingNachaDispatchWindowCalculator.Evaluate(
                    tx.AchCycle,
                    nowLocal,
                    _timeProvider.LocalTimeZone);

                switch (window.Position)
                {
                    case IncomingNachaDispatchWindowPosition.Before:
                        nextAttemptAtUtc = window.NextEligibleAtUtc;
                        break;
                    case IncomingNachaDispatchWindowPosition.Expired:
                        status = IncomingNachaDispatchQueueStatus.Blocked;
                        lastErrorCode = "WINDOW_EXPIRED";
                        queueReason = "La ventana operativa del ciclo ya expiró; no se permite despacho automático.";
                        break;
                    case IncomingNachaDispatchWindowPosition.Invalid:
                        status = IncomingNachaDispatchQueueStatus.Blocked;
                        lastErrorCode = "WINDOW_SCHEDULE_INVALID";
                        queueReason = "No fue posible calcular de forma determinística la próxima ventana operativa.";
                        break;
                    default:
                        status = IncomingNachaDispatchQueueStatus.Blocked;
                        lastErrorCode = "WINDOW_POLICY_INCONSISTENT";
                        queueReason = "La evaluación de ventana operativa resultó inconsistente; se bloqueó de forma segura.";
                        break;
                }
            }

            CompareDispatchShadow(
                tx,
                ingestion,
                clearingHouseCodes.GetValueOrDefault(tx.AchCycle.ClearingHouseId),
                paymentRailCodes.GetValueOrDefault(tx.AchCycle.ClearingHouseId),
                status,
                status == IncomingNachaDispatchQueueStatus.Queued,
                status == IncomingNachaDispatchQueueStatus.WaitingWindow,
                evaluation.Priority);

            _context.IncomingNachaDispatchQueue.Add(new IncomingNachaDispatchQueue
            {
                IncomingNachaFileIngestionId = ingestion.Id,
                IncomingNachaEntryClassificationId = candidate.Classification.Id,
                IncomingNachaTransactionLinkId = candidate.Link.Id,
                AchTransactionId = tx.Id,
                AchCycleId = tx.AchCycleId,
                ClearingHouseId = tx.AchCycle.ClearingHouseId,
                OperationalDate = ingestion.OperationalDate?.Date ?? tx.AchCycle.ProcessingDate.Date,
                QueueStatus = status,
                Priority = evaluation.Priority,
                IdempotencyDispatchKey = dispatchKey,
                NextAttemptAtUtc = nextAttemptAtUtc,
                LastErrorCode = lastErrorCode,
                LastErrorMessage = queueReason
            });
            created++;
        }

        if (created > 0)
        {
            await _context.SaveChangesAsync(ct);
        }

        return created;
    }

    private void CompareDispatchShadow(
        AchTransaction transaction,
        IncomingNachaFileIngestion ingestion,
        string? clearingHouseCode,
        string? paymentRailCode,
        IncomingNachaDispatchQueueStatus legacyStatus,
        bool legacyEligible,
        bool legacyWaitingWindow,
        int legacyPriority)
    {
        if (_paymentRailContextService is null || _strategyResolver is null || _shadowCompareService is null)
        {
            return;
        }

        var resolvedContext = _paymentRailContextService.ResolveContext(
            transaction.AchCycle.ClearingHouseId,
            clearingHouseCode,
            transaction.AchCycleId,
            ingestion.OperationalDate?.Date ?? transaction.AchCycle.ProcessingDate.Date,
            paymentRailCode);
        var strategy = _strategyResolver.ResolveStrategy(new PaymentRailResolveRequest(
            transaction.AchCycle.ClearingHouseId,
            clearingHouseCode,
            paymentRailCode));
        var wrapperResult = strategy.EvaluateCapabilityWrapper(new PaymentRailWrapperCallRequest(
            resolvedContext.OperationalContext,
            PaymentRailCapabilityKind.Dispatch,
            legacyStatus.ToString()));
        var shadowResult = _shadowCompareService.CompareDispatchPlanning(
            resolvedContext,
            wrapperResult,
            legacyStatus.ToString(),
            legacyEligible,
            legacyWaitingWindow,
            legacyPriority);

        _logger.LogInformation(
            "PAYMENT_RAIL_SHADOW_COMPARE_DISPATCH|TxId={TxId}|RailCode={RailCode}|LegacyDecision={LegacyDecision}|WrapperDecision={WrapperDecision}|Equivalent={Equivalent}|Code={Code}",
            transaction.Id,
            shadowResult.RailCode,
            shadowResult.LegacyDecisionCode,
            shadowResult.WrapperDecisionCode,
            shadowResult.IsEquivalent,
            shadowResult.ComparisonCode);
    }

    private static string BuildIdempotencyKey(Guid ingestionId, int transactionId, string cycleId, Guid classificationId)
    {
        var raw = $"{ingestionId:N}|{transactionId}|{cycleId}|{classificationId:N}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }
}

internal enum IncomingNachaDispatchWindowPosition
{
    Before,
    Open,
    Expired,
    Invalid
}

internal readonly record struct IncomingNachaDispatchWindowEvaluation(
    IncomingNachaDispatchWindowPosition Position,
    DateTime? NextEligibleAtUtc);

internal static class IncomingNachaDispatchWindowCalculator
{
    public static IncomingNachaDispatchWindowEvaluation Evaluate(
        AchCycle cycle,
        DateTime nowLocal,
        TimeZoneInfo localTimeZone)
    {
        var processingDate = cycle.ProcessingDate.Date;
        var startLocal = cycle.StartTime <= cycle.EndTime
            ? processingDate + cycle.StartTime
            : processingDate.AddDays(-1) + cycle.StartTime;
        var endLocal = processingDate + cycle.EndTime;
        var comparableNow = DateTime.SpecifyKind(nowLocal, DateTimeKind.Unspecified);
        startLocal = DateTime.SpecifyKind(startLocal, DateTimeKind.Unspecified);
        endLocal = DateTime.SpecifyKind(endLocal, DateTimeKind.Unspecified);

        if (comparableNow > endLocal)
        {
            return new IncomingNachaDispatchWindowEvaluation(
                IncomingNachaDispatchWindowPosition.Expired,
                null);
        }

        if (comparableNow >= startLocal)
        {
            return new IncomingNachaDispatchWindowEvaluation(
                IncomingNachaDispatchWindowPosition.Open,
                null);
        }

        if (localTimeZone.IsInvalidTime(startLocal) || localTimeZone.IsAmbiguousTime(startLocal))
        {
            return new IncomingNachaDispatchWindowEvaluation(
                IncomingNachaDispatchWindowPosition.Invalid,
                null);
        }

        try
        {
            return new IncomingNachaDispatchWindowEvaluation(
                IncomingNachaDispatchWindowPosition.Before,
                TimeZoneInfo.ConvertTimeToUtc(startLocal, localTimeZone));
        }
        catch (ArgumentException)
        {
            return new IncomingNachaDispatchWindowEvaluation(
                IncomingNachaDispatchWindowPosition.Invalid,
                null);
        }
    }
}
