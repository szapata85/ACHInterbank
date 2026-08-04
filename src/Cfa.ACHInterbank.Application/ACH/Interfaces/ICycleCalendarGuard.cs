using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ICycleCalendarGuard
{
    Task<CycleCalendarGuardResult> EnsureExecutableAsync(AchCycle cycle, CancellationToken ct = default);
}
