using Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cfa.ACHInterbank.Tests;

public sealed class QuartzSqlServerMigrationTests
{
    [Fact]
    public void QuartzSchemaMigration_ShouldBeEmbeddedAndSplitIntoExecutableBatches()
    {
        var migration = new QuartzSqlServer471Schema();
        var resourceNames = typeof(QuartzSqlServer471Schema).Assembly.GetManifestResourceNames();
        Assert.Contains("ACHInterbank.Migrations.QuartzSqlServer471.sql", resourceNames);

        var operations = migration.UpOperations.OfType<SqlOperation>().ToList();
        Assert.True(operations.Count > 20, "El SQL de Quartz debe separarse por lotes GO.");
        Assert.Contains(operations, operation => operation.Sql.Contains("QRTZ_PAUSED_JOB_GRPS", StringComparison.Ordinal));
        Assert.Contains(operations, operation => operation.Sql.Contains("THROW 51005", StringComparison.Ordinal));
        Assert.DoesNotContain(operations, operation =>
            operation.Sql.Split('\n').Any(line =>
                string.Equals(line.Trim(), "GO", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void QuartzSchemaRollback_ShouldPreserveJobsAndTriggers()
    {
        var migration = new QuartzSqlServer471Schema();
        Assert.Empty(migration.DownOperations);
    }
}
