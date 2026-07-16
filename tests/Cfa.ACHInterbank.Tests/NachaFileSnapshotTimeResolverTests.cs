using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

namespace Cfa.ACHInterbank.Tests;

public class NachaFileSnapshotTimeResolverTests
{
    [Fact]
    public void Resolve_UsesOperationalDateAndCycleCutoff_Deterministically()
    {
        var cycle = new AchCycle
        {
            Id = "cycle-1",
            CycleName = "Ciclo 1",
            ProcessingDate = new DateTime(2026, 7, 14, 0, 0, 0, DateTimeKind.Utc),
            CutoffTime = new TimeSpan(8, 0, 0)
        };

        var first = NachaFileSnapshotTimeResolver.Resolve(cycle);
        var second = NachaFileSnapshotTimeResolver.Resolve(cycle);

        Assert.Equal(new DateTime(2026, 7, 14, 8, 0, 0, DateTimeKind.Utc), first);
        Assert.Equal(first, second);
    }
}
