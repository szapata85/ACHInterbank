using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ICenitNettingService
{
    Task<CenitNettingExecution> CalculateAsync(CenitCycleExecution execution, CancellationToken ct);
}
