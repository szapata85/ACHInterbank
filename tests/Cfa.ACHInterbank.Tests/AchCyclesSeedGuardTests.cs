using System.IO;

namespace Cfa.ACHInterbank.Tests;

public class AchCyclesSeedGuardTests
{
    [Fact]
    public void IntegrationMappingScenarioSeeder_ShouldNotContainSeedCycleOrSeedMappingTransaction()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var seederPath = Path.Combine(repoRoot, "src", "Cfa.ACHInterbank.Persistence", "ACH", "Services", "Implementation", "Seeders", "IntegrationMappingScenarioSeeder.cs");

        var content = File.ReadAllText(seederPath);

        Assert.DoesNotContain("SEED-CYCLE", content);
        Assert.DoesNotContain("SEED-MAPPING-001", content);
        Assert.DoesNotContain("SEED COMPANY", content);
        Assert.DoesNotContain("SEED INFO", content);
    }
}
