using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
        var cycle = await _context.AchCycles
            .AsNoTracking()
            .Include(x => x.ClearingHouse)
            .FirstAsync(x => x.Id == execution.AchCycleId, ct);
        var cycleIndex = ExtractCycleIndex(cycle.CycleName);
        var sourceFileReference = await _context.AchFileExports
            .AsNoTracking()
            .Where(x => x.AchCycleId == execution.AchCycleId)
            .OrderByDescending(x => x.GeneratedAtUtc)
            .Select(x => x.FileName)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        var transactions = await _context.AchTransactions
            .Where(x => x.AchCycleId == execution.AchCycleId)
            .OrderByDescending(x => x.Amount)
            .ToListAsync(ct);

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
                    tx.AchCycleId = nextCycleId;
                    tx.StateChangedAtUtc = DateTime.UtcNow;
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
                        toCycle = nextCycleId
                    })
                });
            }
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
