using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ICenitCycleExecutionService
{
    Task<CenitCycleExecution> StartExecutionAsync(AchCycle cycle, CancellationToken ct);
}
