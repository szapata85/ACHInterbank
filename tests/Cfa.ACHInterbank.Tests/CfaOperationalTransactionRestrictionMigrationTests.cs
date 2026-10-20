using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using PgMigration = Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres.AddCfaOperationalTransactionRestrictions;
using SqlMigration = Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer.AddCfaOperationalTransactionRestrictions;

namespace Cfa.ACHInterbank.Tests;

public sealed class CfaOperationalTransactionRestrictionMigrationTests
{
    [Fact]
    public void PostgreSqlAndSqlServerMigrations_HaveEquivalentOperationalRestrictionShape()
    {
        var postgres = Operations(new PgMigration(), "Npgsql.EntityFrameworkCore.PostgreSQL");
        var sqlServer = Operations(new SqlMigration(), "Microsoft.EntityFrameworkCore.SqlServer");

        var postgresTable = Assert.Single(postgres.OfType<CreateTableOperation>());
        var sqlServerTable = Assert.Single(sqlServer.OfType<CreateTableOperation>());

        Assert.Equal("CfaOperationalTransactionRestrictions", postgresTable.Name);
        Assert.Equal(postgresTable.Name, sqlServerTable.Name);

        var expectedColumns = new[]
        {
            "Id",
            "ClearingHouseId",
            "ClearingHouseCycleConfigId",
            "TransactionType",
            "IsPrenotification",
            "MaxAmountPerTransaction",
            "MaxAmountPerCycle",
            "MaxTransactionsPerCycle",
            "AllowedAccountTypes",
            "EffectiveFrom",
            "EffectiveTo",
            "IsActive",
            "CreatedAt",
            "UpdatedAt",
            "RowVersion"
        };

        Assert.Equal(expectedColumns.OrderBy(x => x), postgresTable.Columns.Select(x => x.Name).OrderBy(x => x));
        Assert.Equal(expectedColumns.OrderBy(x => x), sqlServerTable.Columns.Select(x => x.Name).OrderBy(x => x));
        Assert.Equal(2, postgresTable.ForeignKeys.Count);
        Assert.Equal(2, sqlServerTable.ForeignKeys.Count);
        Assert.Equal(5, postgresTable.CheckConstraints.Count);
        Assert.Equal(5, sqlServerTable.CheckConstraints.Count);

        Assert.Contains(postgres.OfType<CreateIndexOperation>(), x => x.Name == "IX_CfaOperationalRestrictions_Resolve");
        Assert.Contains(sqlServer.OfType<CreateIndexOperation>(), x => x.Name == "IX_CfaOperationalRestrictions_Resolve");
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
