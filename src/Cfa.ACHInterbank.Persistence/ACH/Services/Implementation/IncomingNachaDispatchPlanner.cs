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

    public IncomingNachaDispatchPlanner(
        AchDbContext context,
        IIncomingNachaDispatchEligibilityPolicy eligibility,
        IPaymentRailContextService? paymentRailContextService = null,
        IPaymentRailOperationalStrategyResolver? strategyResolver = null,
        IPaymentRailShadowCompareService? shadowCompareService = null,
        ILogger<IncomingNachaDispatchPlanner>? logger = null)
    {
        _context = context;
        _eligibility = eligibility;
        _paymentRailContextService = paymentRailContextService;
        _strategyResolver = strategyResolver;
        _shadowCompareService = shadowCompareService;
        _logger = logger ?? NullLogger<IncomingNachaDispatchPlanner>.Instance;
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

        var created = 0;
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
                nowLocal: DateTime.Now,
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

            CompareDispatchShadow(
                tx,
                ingestion,
                status,
                evaluation.IsEligible,
                evaluation.IsWaitingWindow,
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
                NextAttemptAtUtc = status == IncomingNachaDispatchQueueStatus.Queued ? DateTime.UtcNow : null,
                LastErrorCode = status == IncomingNachaDispatchQueueStatus.Blocked ? "POLICY_BLOCKED" : string.Empty,
                LastErrorMessage = evaluation.Reason
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
        IncomingNachaDispatchQueueStatus legacyStatus,
        bool legacyEligible,
        bool legacyWaitingWindow,
        int legacyPriority)
    {
        if (_paymentRailContextService is null || _strategyResolver is null || _shadowCompareService is null)
        {
            return;
        }

        var clearingHouseCode = transaction.AchCycle.ClearingHouse?.Code;
        var resolvedContext = _paymentRailContextService.ResolveContext(
            transaction.AchCycle.ClearingHouseId,
            clearingHouseCode,
            transaction.AchCycleId,
            ingestion.OperationalDate?.Date ?? transaction.AchCycle.ProcessingDate.Date);
        var strategy = _strategyResolver.ResolveStrategy(new PaymentRailResolveRequest(
            transaction.AchCycle.ClearingHouseId,
            clearingHouseCode,
            null));
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
