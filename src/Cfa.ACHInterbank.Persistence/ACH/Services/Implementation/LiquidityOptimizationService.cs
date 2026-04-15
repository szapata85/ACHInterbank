using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class LiquidityOptimizationService : ILiquidityOptimizationService
{
    private readonly AchDbContext _context;
    private readonly ITransactionPriorityPolicy _priorityPolicy;

    public LiquidityOptimizationService(AchDbContext context, ITransactionPriorityPolicy priorityPolicy)
    {
        _context = context;
        _priorityPolicy = priorityPolicy;
    }

    public async Task<IReadOnlyCollection<LiquidityOptimizationDecision>> OptimizeCycleAsync(CenitCycleExecution execution, CancellationToken ct)
    {
        var cycle = await _context.AchCycles.AsNoTracking().FirstAsync(x => x.Id == execution.AchCycleId, ct);
        var cycleIndex = ExtractCycleIndex(cycle.CycleName);

        var transactions = await _context.AchTransactions
            .Where(x => x.AchCycleId == execution.AchCycleId)
            .OrderByDescending(x => x.Amount)
            .ToListAsync(ct);

        var balances = await _context.CenitNetPositions
            .Where(x => x.CenitNettingExecution.CenitCycleExecutionId == execution.Id)
            .ToDictionaryAsync(x => x.FinancialInstitutionId, x => x.AvailableLiquidity, ct);

        var decisions = new List<LiquidityOptimizationDecision>(transactions.Count);

        foreach (var tx in transactions.OrderByDescending(_priorityPolicy.ResolvePriority))
        {
            balances.TryGetValue(tx.SourceInstitutionId, out var liquidity);
            var hasLiquidity = liquidity >= tx.Amount;

            string decision;
            string reason;
            string? nextCycleId = null;

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
            }
            else if (cycleIndex is 4 or 5)
            {
                decision = "Rejected";
                reason = "InsufficientFundsAfterOptimizationRule45";
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
                DecisionType = decision,
                DecisionReason = reason,
                Priority = _priorityPolicy.ResolvePriority(tx),
                FromCycleId = cycle.Id,
                ToCycleId = nextCycleId,
                DecidedAtUtc = DateTime.UtcNow
            });
        }

        _context.LiquidityOptimizationDecisions.AddRange(decisions);
        await _context.SaveChangesAsync(ct);
        return decisions;
    }

    private async Task<string?> ResolveNextCycleIdAsync(AchCycle currentCycle, CancellationToken ct)
    {
        return await _context.AchCycles
            .Where(x => x.ClearingHouseId == currentCycle.ClearingHouseId
                        && (x.ProcessingDate > currentCycle.ProcessingDate
                            || (x.ProcessingDate == currentCycle.ProcessingDate && x.CutoffTime > currentCycle.CutoffTime)))
            .OrderBy(x => x.ProcessingDate)
            .ThenBy(x => x.CutoffTime)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }

    private static int ExtractCycleIndex(string cycleName)
    {
        var digits = new string(cycleName.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var cycleIndex) ? cycleIndex : -1;
    }
}
