using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;

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
        var transactions = await _context.AchTransactions
            .AsNoTracking()
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
            .ToDictionaryAsync(x => x.FinancialInstitutionId, x => x.SimulatedLiquidity, ct);

        var decisions = new List<LiquidityOptimizationDecision>(transactions.Count);

        var priorityMap = new Dictionary<int, int>();
        foreach (var transaction in transactions)
        {
            priorityMap[transaction.Id] = await _priorityPolicy.ResolvePriorityAsync(transaction, ct);
        }

        // Análisis informativo: no modifica AchTransaction, AchBatch, dispatch,
        // colas, eventos ni liquida fondos. Los resultados oficiales llegan por
        // CenitChamberResponseService, no por este simulador.
        foreach (var tx in transactions.OrderBy(t => priorityMap[t.Id]).ThenByDescending(t => t.Amount))
        {
            balances.TryGetValue(tx.SourceInstitutionId, out var liquidity);
            var hasLiquidity = liquidity >= tx.Amount;
            if (hasLiquidity)
            {
                balances[tx.SourceInstitutionId] = liquidity - tx.Amount;
            }

            decisions.Add(new LiquidityOptimizationDecision
            {
                CenitCycleExecutionId = execution.Id,
                AchTransactionId = tx.Id,
                AchBatchId = tx.AchBatchId,
                ValueDate = tx.EffectiveEntryDate.Date,
                ClearingHouseId = cycle.ClearingHouseId,
                ClearingHouseCode = cycle.ClearingHouse?.Code ?? string.Empty,
                SourceFileReference = sourceFileByTransaction.GetValueOrDefault(tx.Id) ?? string.Empty,
                DecisionType = hasLiquidity ? "SimulatedCandidate" : "SimulatedInsufficientLiquidity",
                DecisionReason = hasLiquidity ? "InternalEstimateOnly" : "UnverifiedInternalLiquidity_NoOperatorRejection",
                LiquidityModelUsed = "Simulated",
                Priority = priorityMap[tx.Id],
                FromCycleId = cycle.Id,
                ToCycleId = null,
                DecidedAtUtc = DateTime.UtcNow
            });
        }

        _context.LiquidityOptimizationDecisions.AddRange(decisions);
        await _context.SaveChangesAsync(ct);
        CompareLiquidityShadow(cycle, execution, decisions);
        return decisions;
    }

    private void CompareLiquidityShadow(AchCycle cycle, CenitCycleExecution execution, IReadOnlyCollection<LiquidityOptimizationDecision> decisions)
    {
        if (_paymentRailContextService is null || _strategyResolver is null || _shadowCompareService is null)
        {
            return;
        }

        try
        {
            var processed = decisions.Count(x => string.Equals(x.DecisionType, "SimulatedCandidate", StringComparison.OrdinalIgnoreCase));
            var deferred = decisions.Count(x => string.Equals(x.DecisionType, "SimulatedInsufficientLiquidity", StringComparison.OrdinalIgnoreCase));
            var rejected = 0;

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

}
