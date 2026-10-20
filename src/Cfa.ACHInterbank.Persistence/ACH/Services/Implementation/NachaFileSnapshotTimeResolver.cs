using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

internal static class NachaFileSnapshotTimeResolver
{
    internal static DateTime Resolve(AchCycle cycle)
    {
        var snapshot = cycle.ProcessingDate.Date.Add(cycle.CutoffTime);
        return DateTime.SpecifyKind(snapshot, cycle.ProcessingDate.Kind);
    }
}
