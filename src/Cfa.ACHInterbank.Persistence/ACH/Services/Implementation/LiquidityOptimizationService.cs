using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;
using System.Text.Json;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class LiquidityOptimizationService : ILiquidityOptimizationService
{
    private readonly AchDbContext _context;
    private readonly ITransactionPriorityPolicy _priorityPolicy;
    private readonly IPaymentRailContextService? _paymentRailContextService;
    private readonly IPaymentRailOperationalStrategyResolver? _strategyResolver;
    private readonly IPaymentRailShadowCompareService? _shadowCompareService;
    private readonly ILogger<LiquidityOptimizationService> _logger;

    public LiquidityOptimizationService(
        AchDbContext context,
        ITransactionPriorityPolicy priorityPolicy,
        IPaymentRailContextService? paymentRailContextService = null,
        IPaymentRailOperationalStrategyResolver? strategyResolver = null,
        IPaymentRailShadowCompareService? shadowCompareService = null,
        ILogger<LiquidityOptimizationService>? logger = null)
    {
        _context = context;
        _priorityPolicy = priorityPolicy;
        _paymentRailContextService = paymentRailContextService;
        _strategyResolver = strategyResolver;
        _shadowCompareService = shadowCompareService;
        _logger = logger ?? NullLogger<LiquidityOptimizationService>.Instance;
    }

    public async Task<IReadOnlyCollection<LiquidityOptimizationDecision>> OptimizeCycleAsync(CenitCycleExecution execution, CancellationToken ct)
    {
        if (!_context.Database.IsRelational())
        {
            return await OptimizeCycleCoreAsync(execution, ct);
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        IReadOnlyCollection<LiquidityOptimizationDecision>? result = null;
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            result = await OptimizeCycleCoreAsync(execution, ct);
            await transaction.CommitAsync(ct);
        });
        return result ?? [];
    }

    private async Task<IReadOnlyCollection<LiquidityOptimizationDecision>> OptimizeCycleCoreAsync(CenitCycleExecution execution, CancellationToken ct)
    {
        var existing = await _context.LiquidityOptimizationDecisions
            .AsNoTracking()
            .Where(x => x.CenitCycleExecutionId == execution.Id)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);
        if (existing.Count > 0)
        {
            return existing;
        }

        var cycle = await _context.AchCycles
            .AsNoTracking()
            .Include(x => x.ClearingHouse)
                .ThenInclude(x => x!.ClearingHouseConfig)
            .FirstAsync(x => x.Id == execution.AchCycleId, ct);
        var cycleIndex = ExtractCycleIndex(cycle.CycleName);
        var transactions = await _context.AchTransactions
            .Include(x => x.AchBatch)
            .Include(x => x.ContrapartidaDispatchItem)
            .Where(x => x.AchCycleId == execution.AchCycleId)
            .OrderByDescending(x => x.Amount)
            .ToListAsync(ct);
        var transactionIds = transactions.Select(x => x.Id).ToArray();
        var exactFileMemberships = await _context.AchFileExportTransactions
            .AsNoTracking()
            .Where(x => transactionIds.Contains(x.AchTransactionId))
            .Select(x => new
            {
                x.AchTransactionId,
                x.AchFileExport.FileName,
                x.AchFileExport.Version,
                x.AchFileExport.GeneratedAtUtc
            })
            .ToListAsync(ct);
        var sourceFileByTransaction = exactFileMemberships
            .GroupBy(x => x.AchTransactionId)
            .ToDictionary(
                x => x.Key,
                x => x.OrderByDescending(y => y.Version)
                    .ThenByDescending(y => y.GeneratedAtUtc)
                    .Select(y => y.FileName)
                    .First());

        var balances = await _context.CenitNetPositions
            .Where(x => x.CenitNettingExecution.CenitCycleExecutionId == execution.Id)
            .ToDictionaryAsync(x => x.FinancialInstitutionId, x => x.AvailableLiquidity, ct);

        var decisions = new List<LiquidityOptimizationDecision>(transactions.Count);

        var priorityMap = new Dictionary<int, int>();
        foreach (var transaction in transactions)
        {
            priorityMap[transaction.Id] = await _priorityPolicy.ResolvePriorityAsync(transaction, ct);
        }

        foreach (var tx in transactions.OrderByDescending(t => priorityMap[t.Id]))
        {
            balances.TryGetValue(tx.SourceInstitutionId, out var liquidity);
            var hasLiquidity = liquidity >= tx.Amount;

            string decision;
            string reason;
            string? nextCycleId = null;
            var previousState = tx.State;
            var previousBatchId = tx.AchBatchId;
            var sourceFileReference = sourceFileByTransaction.GetValueOrDefault(tx.Id) ?? string.Empty;

            if (hasLiquidity)
            {
                decision = "Processed";
                reason = "LiquidityAvailable";
                balances[tx.SourceInstitutionId] = liquidity - tx.Amount;
            }
            else if (cycleIndex <= 3)
            {
                decision = "Deferred";
                reason = "TransferredToNextCycleByRule123";
                nextCycleId = await ResolveNextCycleIdAsync(cycle, ct);
                if (!string.IsNullOrWhiteSpace(nextCycleId))
                {
                    var targetBatch = await ResolveTargetBatchAsync(tx, nextCycleId, ct);
                    tx.AchCycleId = nextCycleId;
                    tx.AchBatch = targetBatch;
                    tx.AchBatchId = targetBatch.Id;
                    tx.StateChangedAtUtc = DateTime.UtcNow;
                    if (tx.ContrapartidaDispatchItem is not null)
                    {
                        tx.ContrapartidaDispatchItem.AchCycleId = nextCycleId;
                        tx.ContrapartidaDispatchItem.AchBatchId = targetBatch.Id;
                        tx.ContrapartidaDispatchItem.ClearingHouseId = cycle.ClearingHouseId;
                    }

                    var alreadyQueued = await _context.CenitCycleQueues.AnyAsync(x =>
                        x.AchTransactionId == tx.Id
                        && x.TargetAchCycleId == nextCycleId
                        && x.Status == "Queued", ct);
                    if (!alreadyQueued)
                    {
                        _context.CenitCycleQueues.Add(new CenitCycleQueue
                        {
                            AchTransactionId = tx.Id,
                            OriginalAchCycleId = cycle.Id,
                            TargetAchCycleId = nextCycleId,
                            QueueReason = "LiquidityDeferredByRule123",
                            Status = "Queued",
                            EnqueuedAtUtc = DateTime.UtcNow,
                            CenitCycleExecutionId = execution.Id
                        });
                    }
                }
            }
            else if (cycleIndex is 4 or 5)
            {
                decision = "Rejected";
                reason = "InsufficientFundsAfterOptimizationRule45";
                tx.State = AchTransferStateEnum.ReturnedByOperator;
                tx.StateChangedAtUtc = DateTime.UtcNow;
                tx.ReturnReasonCode = "DXX-LIQ";
            }
            else
            {
                decision = "Rejected";
                reason = "UnknownCycle";
            }

            decisions.Add(new LiquidityOptimizationDecision
            {
                CenitCycleExecutionId = execution.Id,
                AchTransactionId = tx.Id,
                AchBatchId = tx.AchBatchId,
                ValueDate = tx.EffectiveEntryDate.Date,
                ClearingHouseId = cycle.ClearingHouseId,
                ClearingHouseCode = cycle.ClearingHouse?.Code ?? string.Empty,
                SourceFileReference = sourceFileReference,
                DecisionType = decision,
                DecisionReason = reason,
                LiquidityModelUsed = "Simulated",
                Priority = priorityMap[tx.Id],
                FromCycleId = cycle.Id,
                ToCycleId = nextCycleId,
                DecidedAtUtc = DateTime.UtcNow
            });

            if (previousState != tx.State || !string.IsNullOrWhiteSpace(nextCycleId))
            {
                _context.AchTransactionStateEvents.Add(new AchTransactionStateEvent
                {
                    AchTransactionId = tx.Id,
                    FromState = previousState,
                    ToState = tx.State,
                    Source = AchStateEventSourceEnum.System,
                    ReasonCode = decision,
                    PayloadJson = JsonSerializer.Serialize(new
                    {
                        decision,
                        reason,
                        fromCycle = cycle.Id,
                        toCycle = nextCycleId,
                        fromBatchId = previousBatchId,
                        toBatchId = tx.AchBatchId
                    }),
                    OccurredAtUtc = tx.StateChangedAtUtc
                });
            }
        }

        _context.LiquidityOptimizationDecisions.AddRange(decisions);
        await _context.SaveChangesAsync(ct);
        CompareLiquidityShadow(cycle, execution, decisions);
        return decisions;
    }

    private async Task<AchBatch> ResolveTargetBatchAsync(AchTransaction transaction, string targetCycleId, CancellationToken ct)
    {
        var sourceBatch = transaction.AchBatch
            ?? throw new InvalidOperationException("La transacción no tiene lote de origen para realizar la reasignación.");
        var targetCycle = await _context.AchCycles.AsNoTracking()
            .FirstAsync(x => x.Id == targetCycleId, ct);

        var tracked = _context.AchBatches.Local.FirstOrDefault(x =>
            x.AchCycleId == targetCycleId
            && x.CompanyName == sourceBatch.CompanyName
            && x.CompanyIdentification == sourceBatch.CompanyIdentification
            && x.CompanyEntryDescription == sourceBatch.CompanyEntryDescription
            && x.EffectiveEntryDate == targetCycle.ProcessingDate.Date);
        if (tracked is not null)
        {
            return tracked;
        }

        var existing = await _context.AchBatches.FirstOrDefaultAsync(x =>
            x.AchCycleId == targetCycleId
            && x.CompanyName == sourceBatch.CompanyName
            && x.CompanyIdentification == sourceBatch.CompanyIdentification
            && x.CompanyEntryDescription == sourceBatch.CompanyEntryDescription
            && x.EffectiveEntryDate == targetCycle.ProcessingDate.Date, ct);
        if (existing is not null)
        {
            return existing;
        }

        var targetBatch = new AchBatch
        {
            AchCycleId = targetCycleId,
            ServiceClassCode = sourceBatch.ServiceClassCode,
            CompanyName = sourceBatch.CompanyName,
            CompanyIdentification = sourceBatch.CompanyIdentification,
            CompanyEntryDescription = sourceBatch.CompanyEntryDescription,
            CompanyEntryDescriptionId = sourceBatch.CompanyEntryDescriptionId,
            OriginOrOdfi = sourceBatch.OriginOrOdfi,
            EffectiveEntryDate = targetCycle.ProcessingDate.Date,
            BatchSequenceNumber = 0
        };
        _context.AchBatches.Add(targetBatch);
        await _context.SaveChangesAsync(ct);
        return targetBatch;
    }

    private void CompareLiquidityShadow(AchCycle cycle, CenitCycleExecution execution, IReadOnlyCollection<LiquidityOptimizationDecision> decisions)
    {
        if (_paymentRailContextService is null || _strategyResolver is null || _shadowCompareService is null)
        {
            return;
        }

        try
        {
            var processed = decisions.Count(x => string.Equals(x.DecisionType, "Processed", StringComparison.OrdinalIgnoreCase));
            var deferred = decisions.Count(x => string.Equals(x.DecisionType, "Deferred", StringComparison.OrdinalIgnoreCase));
            var rejected = decisions.Count(x => string.Equals(x.DecisionType, "Rejected", StringComparison.OrdinalIgnoreCase));

            var context = _paymentRailContextService.ResolveContext(
                cycle.ClearingHouseId,
                cycle.ClearingHouse?.Code,
                execution.AchCycleId,
                cycle.ProcessingDate.Date,
                cycle.ClearingHouse?.ClearingHouseConfig?.PaymentRailCode);
            var strategy = _strategyResolver.ResolveStrategy(new PaymentRailResolveRequest(
                cycle.ClearingHouseId,
                cycle.ClearingHouse?.Code,
                cycle.ClearingHouse?.ClearingHouseConfig?.PaymentRailCode));
            const string legacyDecisionCode = "CENIT_LIQUIDITY_OPTIMIZED";
            var wrapperResult = strategy.EvaluateCapabilityWrapper(new PaymentRailWrapperCallRequest(
                context.OperationalContext,
                PaymentRailCapabilityKind.Liquidity,
                legacyDecisionCode));
            var shadowResult = _shadowCompareService.CompareLiquidityOperation(
                context,
                wrapperResult,
                legacyDecisionCode,
                processed,
                deferred,
                rejected);

            _logger.LogInformation(
                "PAYMENT_RAIL_SHADOW_COMPARE_LIQUIDITY|RailCode={RailCode}|LegacyDecision={LegacyDecision}|WrapperDecision={WrapperDecision}|Equivalent={Equivalent}|Code={Code}",
                shadowResult.RailCode,
                shadowResult.LegacyDecisionCode,
                shadowResult.WrapperDecisionCode,
                shadowResult.IsEquivalent,
                shadowResult.ComparisonCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PAYMENT_RAIL_SHADOW_COMPARE_LIQUIDITY_FAILED");
        }
    }

    private async Task<string?> ResolveNextCycleIdAsync(AchCycle currentCycle, CancellationToken ct)
    {
        var candidateCycles = await _context.AchCycles
            .Where(x => x.ClearingHouseId == currentCycle.ClearingHouseId)
            .Select(x => new { x.Id, x.ProcessingDate, x.CutoffTime })
            .ToListAsync(ct);

        return candidateCycles
            .Where(x => x.ProcessingDate > currentCycle.ProcessingDate
                        || (x.ProcessingDate == currentCycle.ProcessingDate && x.CutoffTime > currentCycle.CutoffTime))
            .OrderBy(x => x.ProcessingDate)
            .ThenBy(x => x.CutoffTime)
            .Select(x => x.Id)
            .FirstOrDefault();
    }

    private static int ExtractCycleIndex(string cycleName)
    {
        var digits = new string(cycleName.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var cycleIndex) ? cycleIndex : -1;
    }
}
