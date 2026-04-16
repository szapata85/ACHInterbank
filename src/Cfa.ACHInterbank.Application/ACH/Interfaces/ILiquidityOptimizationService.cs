using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ILiquidityOptimizationService
{
    Task<IReadOnlyCollection<LiquidityOptimizationDecision>> OptimizeCycleAsync(CenitCycleExecution execution, CancellationToken ct);
}
