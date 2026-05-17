using System.IO;

namespace Cfa.ACHInterbank.Tests;

public class SeedMappingTransactionMigrationGuardTests
{
    [Fact]
    public void PostgresMigrations_ShouldNotContainBroadSeedMappingTransactionDelete()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var migrationsPath = Path.Combine(repoRoot, "src", "Cfa.ACHInterbank.Persistence", "DataBase", "Migrations", "Postgres");

        var migrationFiles = Directory.GetFiles(migrationsPath, "*.cs", SearchOption.TopDirectoryOnly);

        Assert.Contains(migrationFiles, x => Path.GetFileName(x).Contains("InitialPostgresSchemaBaseline"));
        Assert.DoesNotContain(migrationFiles, x => Path.GetFileName(x).Contains("RemoveSeedMappingTransactionFromAchTransactions"));

        var allContent = string.Join("\n", migrationFiles.Select(File.ReadAllText));

        Assert.DoesNotContain("DELETE FROM \"AchTransactions\" t\n                WHERE t.\"Reference\" IS NOT NULL", allContent);
        Assert.DoesNotContain("DELETE FROM \"AchTransactions\"", allContent);
    }
}
