using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class CenitCycleExecutionService : ICenitCycleExecutionService
{
    private const string CenitCode = "CENIT";
    private readonly AchDbContext _context;
    private readonly ICenitNettingService _nettingService;
    private readonly ILiquidityOptimizationService _liquidityService;

    public CenitCycleExecutionService(
        AchDbContext context,
        ICenitNettingService nettingService,
        ILiquidityOptimizationService liquidityService)
    {
        _context = context;
        _nettingService = nettingService;
        _liquidityService = liquidityService;
    }

    public async Task<CenitCycleExecution> StartExecutionAsync(AchCycle cycle, CancellationToken ct)
    {
        var clearingHouse = await _context.ClearingHouses
            .AsNoTracking()
            .FirstAsync(x => x.Id == cycle.ClearingHouseId, ct);

        if (!string.Equals(clearingHouse.Code, CenitCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("La ejecución dedicada CENIT solo aplica para la cámara CENIT.");
        }

        var execution = await _context.CenitCycleExecutions
            .FirstOrDefaultAsync(x => x.AchCycleId == cycle.Id, ct);

        if (execution is null)
        {
            execution = new CenitCycleExecution
            {
                AchCycleId = cycle.Id,
                StartedAtUtc = DateTime.UtcNow,
                Status = "Running",
                Summary = "Execution started"
            };
            _context.CenitCycleExecutions.Add(execution);
            await _context.SaveChangesAsync(ct);
        }
        else if (string.Equals(execution.Status, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            return execution;
        }

        var queued = await _context.CenitCycleQueues
            .Where(x => x.TargetAchCycleId == cycle.Id && x.Status == "Queued")
            .ToListAsync(ct);

        foreach (var item in queued)
        {
            item.Status = "Consumed";
            item.DequeuedAtUtc = DateTime.UtcNow;
            item.CenitCycleExecutionId = execution.Id;
        }

        await _context.SaveChangesAsync(ct);

        await _nettingService.CalculateAsync(execution, ct);
        await _liquidityService.OptimizeCycleAsync(execution, ct);

        execution.Status = "Completed";
        execution.CompletedAtUtc = DateTime.UtcNow;
        execution.Summary = "Execution completed with netting and optimization.";
        await _context.SaveChangesAsync(ct);

        return execution;
    }
}
