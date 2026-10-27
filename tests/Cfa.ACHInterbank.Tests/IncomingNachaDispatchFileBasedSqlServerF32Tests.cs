using System.Reflection;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using PgQueueMigration = Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres.AchColFileBasedDispatchQueueF31;
using SqlQueueMigration = Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer.AchColFileBasedDispatchQueueF32;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

/// <summary>
/// F3.2: provider migration contract and safe downgrade; requires no live databases.
/// Runtime execution against SQL Server/PostgreSQL is deferred to F6.
/// </summary>
public sealed class IncomingNachaDispatchFileBasedSqlServerF32Tests
{
    [Fact]
    public void SqlServerAndPostgres_HaveEquivalentLogicalExpansion()
    {
        var pg = Operations(new PgQueueMigration(), "Npgsql.EntityFrameworkCore.PostgreSQL", "Up");
        var sql = Operations(new SqlQueueMigration(), "Microsoft.EntityFrameworkCore.SqlServer", "Up");

        foreach (var operations in new[] { pg, sql })
        {
            var additions = operations.OfType<AddColumnOperation>().ToArray();
            Assert.Equal(2, additions.Length);
            var source = Assert.Single(additions, x => x.Name == "SourceKind");
            Assert.Equal("IncomingNachaDispatchQueue", source.Table);
            Assert.False(source.IsNullable);
            Assert.Equal("CycleBound", source.DefaultValue);
            Assert.Equal(30, source.MaxLength);
            var entry = Assert.Single(additions, x => x.Name == "EntryDetailId");
            Assert.True(entry.IsNullable);
            Assert.Equal(typeof(int), entry.ClrType);

            var nullableLegacy = operations.OfType<AlterColumnOperation>().ToArray();
            Assert.Equal(2, nullableLegacy.Length);
            Assert.All(nullableLegacy, x => Assert.True(x.IsNullable));
            Assert.Equal(new[] { "AchCycleId", "AchTransactionId" },
                nullableLegacy.Select(x => x.Name).OrderBy(x => x).ToArray());
            Assert.All(nullableLegacy, x => Assert.False(x.OldColumn.IsNullable));

            var index = Assert.Single(operations.OfType<CreateIndexOperation>());
            Assert.Equal("IX_IncomingNachaDispatchQueue_EntryDetailId", index.Name);
            Assert.Equal("EntryDetailId", Assert.Single(index.Columns));
            Assert.False(index.IsUnique);

            var fk = Assert.Single(operations.OfType<AddForeignKeyOperation>());
            Assert.Equal("FK_IncomingNachaDispatchQueue_EntryDetails_EntryDetailId", fk.Name);
            Assert.Equal("EntryDetails", fk.PrincipalTable);
            Assert.Equal("EntryDetailID", Assert.Single(fk.PrincipalColumns));
            Assert.Equal(ReferentialAction.Restrict, fk.OnDelete);

            var check = Assert.Single(operations.OfType<AddCheckConstraintOperation>());
            Assert.Equal("CK_IncomingNachaDispatchQueue_SourceShape", check.Name);
            foreach (var term in new[] { "CycleBound", "AchColFileBased", "AchTransactionId", "AchCycleId", "EntryDetailId" })
                Assert.Contains(term, check.Sql);
            Assert.Empty(operations.OfType<DropIndexOperation>());
        }

        Assert.Equal("nvarchar(30)", sql.OfType<AddColumnOperation>().Single(x => x.Name == "SourceKind").ColumnType);
        Assert.Equal("nvarchar(max)", sql.OfType<AlterColumnOperation>().Single(x => x.Name == "AchCycleId").ColumnType);
        Assert.Equal("CycleBound", sql.OfType<AddColumnOperation>().Single(x => x.Name == "SourceKind").DefaultValue);
    }

    [Fact]
    public void SqlServer_Down_FirstGuardsAgainstLosingFinancialQueueRows()
    {
        var down = Operations(new SqlQueueMigration(), "Microsoft.EntityFrameworkCore.SqlServer", "Down");
        var guard = Assert.IsType<SqlOperation>(down[0]);
        Assert.Contains("THROW 51032", guard.Sql);
        Assert.Contains("[SourceKind] <> 'CycleBound'", guard.Sql);
        Assert.Contains("[EntryDetailId] IS NOT NULL", guard.Sql);
        Assert.Contains("[AchTransactionId] IS NULL", guard.Sql);
        Assert.Contains("[AchCycleId] IS NULL", guard.Sql);
        Assert.Contains(down, x => x is DropCheckConstraintOperation);
        Assert.Contains(down, x => x is DropForeignKeyOperation);
        Assert.Contains(down, x => x is DropIndexOperation);
        Assert.Equal(2, down.OfType<DropColumnOperation>().Count());
    }

    [Fact]
    public void SqlServerMigration_IdIsRegisteredAfterF23_AndDoesNotExecuteAnyDispatch()
    {
        var attrs = typeof(SqlQueueMigration).GetCustomAttributes(typeof(MigrationAttribute), false);
        var attr = Assert.IsType<MigrationAttribute>(Assert.Single(attrs));
        Assert.Equal("20261024224500_AchColFileBasedDispatchQueueF32", attr.Id);
        var operations = Operations(new SqlQueueMigration(), "Microsoft.EntityFrameworkCore.SqlServer", "Up");
        Assert.DoesNotContain(operations.OfType<SqlOperation>(), x =>
            x.Sql.Contains("Proc_Transacciones", StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<MigrationOperation> Operations(Migration migration, string provider, string methodName)
    {
        var builder = new MigrationBuilder(provider);
        var method = migration.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Migration.{methodName} missing");
        method.Invoke(migration, new object?[] { builder });
        return builder.Operations;
    }
}
