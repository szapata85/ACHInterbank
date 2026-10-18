using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using PgMigration = Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres.AddAchColombiaCycleOperationalSchedule;
using SqlMigration = Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer.AddAchColombiaCycleOperationalSchedule;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColombiaCycleOperationalScheduleMigrationTests
{
    [Fact]
    public void PostgreSqlAndSqlServerMigrations_HaveEquivalentLogicalShape()
    {
        var postgres = Operations(new PgMigration(), "Npgsql.EntityFrameworkCore.PostgreSQL");
        var sqlServer = Operations(new SqlMigration(), "Microsoft.EntityFrameworkCore.SqlServer");

        var postgresTable = Assert.Single(postgres.OfType<CreateTableOperation>());
        var sqlServerTable = Assert.Single(sqlServer.OfType<CreateTableOperation>());
        Assert.Equal("AchColombiaCycleOperationalSchedules", postgresTable.Name);
        Assert.Equal(postgresTable.Name, sqlServerTable.Name);

        var expectedColumns = new[]
        {
            "Id",
            "ClearingHouseId",
            "ClearingHouseCycleConfigId",
            "AutomaticOutboundEnabled",
            "AutomaticOutboundStartTime",
            "AutomaticOutboundLatestStartTime",
            "OutboundExecutionStrategy",
            "EffectiveTimeZoneId",
            "IsActive",
            "CreatedAt",
            "UpdatedAt",
            "RowVersion"
        };

        Assert.Equal(expectedColumns.OrderBy(x => x), postgresTable.Columns.Select(x => x.Name).OrderBy(x => x));
        Assert.Equal(expectedColumns.OrderBy(x => x), sqlServerTable.Columns.Select(x => x.Name).OrderBy(x => x));
        Assert.Equal(2, postgresTable.ForeignKeys.Count);
        Assert.Equal(2, sqlServerTable.ForeignKeys.Count);
        Assert.Equal(3, postgresTable.CheckConstraints.Count);
        Assert.Equal(3, sqlServerTable.CheckConstraints.Count);

        AssertIndexSet(postgres);
        AssertIndexSet(sqlServer);
    }

    private static void AssertIndexSet(IReadOnlyList<MigrationOperation> operations)
    {
        var indexes = operations.OfType<CreateIndexOperation>().ToArray();
        Assert.Contains(indexes, x => x.Name == "IX_AchColombiaCycleOperationalSchedules_ClearingHouseCycleConfigId" && !x.IsUnique);
        Assert.Contains(indexes, x => x.Name == "IX_AchColombiaCycleOperationalSchedules_House_Active" && !x.IsUnique);
        Assert.Contains(indexes, x => x.Name == "UX_AchColombiaCycleOperationalSchedules_House_Cycle" && x.IsUnique);
    }

    private static IReadOnlyList<MigrationOperation> Operations(Migration migration, string provider)
    {
        var builder = new MigrationBuilder(provider);
        var method = migration.GetType().GetMethod(
            "Up",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Migration Up method not found.");
        method.Invoke(migration, new object?[] { builder });
        return builder.Operations;
    }
}
