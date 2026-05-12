using System.IO;

namespace Cfa.ACHInterbank.Tests;

public class SeedMappingTransactionMigrationGuardTests
{
    [Fact]
    public void Migration_ShouldTargetOnlySeedMappingTransaction()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var migrationPath = Path.Combine(repoRoot, "src", "Cfa.ACHInterbank.Persistence", "DataBase", "Migrations", "Postgres", "20260511230542_RemoveSeedMappingTransactionFromAchTransactions.cs");

        var content = File.ReadAllText(migrationPath);

        Assert.Contains("SEED-MAPPING-001", content);
        Assert.Contains("SEED COMPANY", content);
        Assert.DoesNotContain("DELETE FROM \"AchTransactions\" t\n                WHERE t.\"Reference\" IS NOT NULL", content);
    }
}
