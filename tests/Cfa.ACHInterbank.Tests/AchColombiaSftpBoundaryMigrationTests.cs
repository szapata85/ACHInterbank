using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using PgMigration = Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres.AddAchColombiaSftpBoundaryConfiguration;
using SqlMigration = Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer.AddAchColombiaSftpBoundaryConfiguration;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColombiaSftpBoundaryMigrationTests
{
    [Fact]
    public void PostgreSqlAndSqlServerMigrations_ShouldAddEquivalentSftpBoundaryFields()
    {
        var postgres = Operations(new PgMigration(), "Npgsql.EntityFrameworkCore.PostgreSQL");
        var sqlServer = Operations(new SqlMigration(), "Microsoft.EntityFrameworkCore.SqlServer");

        AssertEquivalent(postgres);
        AssertEquivalent(sqlServer);
    }

    private static void AssertEquivalent(IReadOnlyList<MigrationOperation> operations)
    {
        var adds = operations.OfType<AddColumnOperation>().ToArray();
        Assert.Equal(2, adds.Length);
        var fingerprint = Assert.Single(adds.Where(x => x.Name == "SftpHostKeyFingerprint"));
        Assert.Equal("AchManagedFileTransferConfigurations", fingerprint.Table);
        Assert.True(fingerprint.IsNullable);
        Assert.Equal(200, fingerprint.MaxLength);

        var timeout = Assert.Single(adds.Where(x => x.Name == "SftpTimeoutSeconds"));
        Assert.Equal("AchManagedFileTransferConfigurations", timeout.Table);
        Assert.False(timeout.IsNullable);
        Assert.Equal(30, timeout.DefaultValue);
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
