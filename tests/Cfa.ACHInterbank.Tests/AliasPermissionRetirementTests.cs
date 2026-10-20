using System.Reflection;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using PgMigration = Cfa.ACHInterbank.Persistence.DataBase.Migrations.Postgres.RetireAliasPermissionSeed;
using SqlMigration = Cfa.ACHInterbank.Persistence.Migrations.SqlServer.DataBase.Migrations.SqlServer.RetireAliasPermissionSeed;

namespace Cfa.ACHInterbank.Tests;

public sealed class AliasPermissionRetirementTests
{
    private static readonly Guid HistoricalAliasPermissionId = Guid.Parse("0ee0a30f-48b6-4b8c-9afd-0dfb3d4770e7");

    [Fact]
    public async Task CurrentSeeds_ShouldNotContainFunctionalAliasPermissionOrRoleAssignments()
    {
        await using var context = CreateContext();

        Assert.False(await context.Permissions.AsNoTracking().AnyAsync(x => x.Name == "CanReadAliases"));
        Assert.False(await context.Permissions.AsNoTracking().AnyAsync(x => x.Name == "CanManageAliases"));
        Assert.False(await context.RolePermissions.AsNoTracking().AnyAsync(x => x.PermissionId == HistoricalAliasPermissionId));
        Assert.False(await context.MenuItems.AsNoTracking().AnyAsync(x => x.Route == "/aliases"));

        var activePermissionNames = await context.Permissions
            .AsNoTracking()
            .Where(x => x.Name == "CanReadAch"
                || x.Name == "CanManageAch"
                || x.Name == "CanReadCatalogs"
                || x.Name == "CanManageUsers")
            .Select(x => x.Name)
            .OrderBy(x => x)
            .ToListAsync();

        Assert.Equal(
            new[] { "CanManageAch", "CanManageUsers", "CanReadAch", "CanReadCatalogs" },
            activePermissionNames);

        var adminPermissionNames = await PermissionNamesForRoleAsync(context, RoleConfiguration.AdminRoleId);
        Assert.Contains("CanManageAch", adminPermissionNames);
        Assert.Contains("CanReadAch", adminPermissionNames);
        Assert.Contains("CanReadCatalogs", adminPermissionNames);
        Assert.Contains("CanManageUsers", adminPermissionNames);

        var operatorPermissionNames = await PermissionNamesForRoleAsync(context, RoleConfiguration.OperatorRoleId);
        Assert.Contains("CanManageAch", operatorPermissionNames);
        Assert.Contains("CanReadAch", operatorPermissionNames);
        Assert.Contains("CanReadCatalogs", operatorPermissionNames);
    }

    [Fact]
    public void CurrentConfiguration_ShouldNotExposeRetiredAliasConstants()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;

        Assert.Null(typeof(PermissionConfiguration).GetField("ReadAliasesPermissionId", flags));
        Assert.Null(typeof(MenuItemConfiguration).GetField("AliasesId", flags));
    }

    [Fact]
    public void ProviderMigrations_Up_ShouldOnlyDeleteAliasRoleLinksAndPermission()
    {
        AssertAliasRetirementUp(Operations(new PgMigration(), "Npgsql.EntityFrameworkCore.PostgreSQL", "Up"));
        AssertAliasRetirementUp(Operations(new SqlMigration(), "Microsoft.EntityFrameworkCore.SqlServer", "Up"));
    }

    [Fact]
    public void ProviderMigrations_Down_ShouldOnlyRestoreAliasPermissionAndOriginalRoleLinks()
    {
        AssertAliasRetirementDown(Operations(new PgMigration(), "Npgsql.EntityFrameworkCore.PostgreSQL", "Down"));
        AssertAliasRetirementDown(Operations(new SqlMigration(), "Microsoft.EntityFrameworkCore.SqlServer", "Down"));
    }

    private static async Task<string[]> PermissionNamesForRoleAsync(AchDbContext context, Guid roleId)
        => await context.RolePermissions
            .AsNoTracking()
            .Where(x => x.RoleId == roleId)
            .Select(x => x.Permission.Name)
            .OrderBy(x => x)
            .ToArrayAsync();

    private static void AssertAliasRetirementUp(IReadOnlyList<MigrationOperation> operations)
    {
        Assert.Equal(3, operations.Count);
        var deletes = operations.Cast<DeleteDataOperation>().ToArray();

        var roleDeletes = deletes.Where(x => x.Table == "RolePermissions").ToArray();
        Assert.Equal(2, roleDeletes.Length);
        Assert.All(roleDeletes, x => Assert.Equal(new[] { "PermissionId", "RoleId" }, x.KeyColumns));
        Assert.All(roleDeletes, x => Assert.Equal(HistoricalAliasPermissionId, (Guid)x.KeyValues[0, 0]));
        Assert.Equal(
            new[] { RoleConfiguration.AdminRoleId, RoleConfiguration.OperatorRoleId }.OrderBy(x => x),
            roleDeletes.Select(x => (Guid)x.KeyValues[0, 1]).OrderBy(x => x));

        var permissionDelete = Assert.Single(deletes.Where(x => x.Table == "Permissions"));
        Assert.Equal(new[] { "Id" }, permissionDelete.KeyColumns);
        Assert.Equal(HistoricalAliasPermissionId, (Guid)permissionDelete.KeyValues[0, 0]);
    }

    private static void AssertAliasRetirementDown(IReadOnlyList<MigrationOperation> operations)
    {
        Assert.Equal(2, operations.Count);
        var inserts = operations.Cast<InsertDataOperation>().ToArray();

        var permissionInsert = Assert.Single(inserts.Where(x => x.Table == "Permissions"));
        Assert.Equal(new[] { "Id", "Description", "Name" }, permissionInsert.Columns);
        Assert.Equal(HistoricalAliasPermissionId, (Guid)permissionInsert.Values[0, 0]);
        Assert.Equal("Consulta de alias", permissionInsert.Values[0, 1]);
        Assert.Equal("CanReadAliases", permissionInsert.Values[0, 2]);

        var roleInsert = Assert.Single(inserts.Where(x => x.Table == "RolePermissions"));
        Assert.Equal(new[] { "PermissionId", "RoleId" }, roleInsert.Columns);
        Assert.Equal(2, roleInsert.Values.GetLength(0));
        Assert.Equal(HistoricalAliasPermissionId, (Guid)roleInsert.Values[0, 0]);
        Assert.Equal(HistoricalAliasPermissionId, (Guid)roleInsert.Values[1, 0]);
        Assert.Equal(
            new[] { RoleConfiguration.AdminRoleId, RoleConfiguration.OperatorRoleId }.OrderBy(x => x),
            new[] { (Guid)roleInsert.Values[0, 1], (Guid)roleInsert.Values[1, 1] }.OrderBy(x => x));
    }

    private static IReadOnlyList<MigrationOperation> Operations(Migration migration, string provider, string methodName)
    {
        var builder = new MigrationBuilder(provider);
        var method = migration.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Migration {methodName} method not found.");
        method.Invoke(migration, new object?[] { builder });
        return builder.Operations;
    }

    private static AchDbContext CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
