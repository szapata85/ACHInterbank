using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav210BCatalogsCoverageTests
{
    private static readonly (string Route, string Label, int Order)[] ExpectedCatalogChildren =
    [
        ("/catalogs/financial-institutions", "Entidades financieras", 10),
        ("/catalogs/company-entry-descriptions", "Conceptos de lote", 20),
        ("/catalogs/document-types", "Tipos de documento", 30),
        ("/catalogs/person-types", "Tipos de persona", 40),
        ("/catalogs/gender-types", "Tipos de género", 50),
        ("/catalogs/phone-types", "Tipos de teléfono", 60),
        ("/catalogs/email-types", "Tipos de correo", 70),
        ("/catalogs/address-types", "Tipos de dirección", 80),
        ("/catalogs/transaction-codes", "Códigos de transacción", 90)
    ];

    [Fact]
    public async Task CanonicalSeeder_CompletesCatalogsWithExactRoutesLabelsAndOrders()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var parameters = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == NavigationInformationArchitectureSeeder.ParametersAdministrationRoute);
        var catalogs = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.IsActive && item.Route == "/catalogs");

        Assert.Equal(parameters.Id, catalogs.ParentId);
        Assert.Equal("Catálogos", catalogs.Label);
        Assert.Equal(70, catalogs.Order);

        var children = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.ParentId == catalogs.Id)
            .OrderBy(item => item.Order)
            .Select(item => new { item.Route, item.Label, item.Order })
            .ToArrayAsync();

        Assert.Equal(
            ExpectedCatalogChildren,
            children.Select(item => (item.Route, item.Label, item.Order)).ToArray());
        Assert.Equal(children.Length, children.Select(item => item.Order).Distinct().Count());

        foreach (var expected in ExpectedCatalogChildren)
        {
            Assert.Equal(1, await fixture.Context.MenuItems.CountAsync(item =>
                item.IsActive && item.Route == expected.Route));
        }

        var legacySpecialDates = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == "/catalogs/clearing-house-special-dates");
        Assert.False(legacySpecialDates.IsActive);
    }

    [Fact]
    public async Task CanonicalSeeder_PreservesExistingFinancialInstitutionsAuthorizationAndUsesCurrentManageAchEnvelopeForNewCatalogEntries()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        fixture.Context.ChangeTracker.Clear();

        var existingCatalogItemsBefore = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.Id == MenuItemConfiguration.CatalogsId
                || item.Id == MenuItemConfiguration.FinancialInstitutionsId
                || item.Route == "/catalogs/clearing-house-special-dates")
            .OrderBy(item => item.Id)
            .ToArrayAsync();
        var existingIds = existingCatalogItemsBefore.Select(item => item.Id).ToArray();
        var existingPermissionsBefore = await ReadPermissionLinksAsync(fixture.Context, existingIds);
        var existingRolesBefore = await ReadRoleLinksAsync(fixture.Context, existingIds);
        var permissionDefinitionsBefore = await ReadPermissionDefinitionsAsync(fixture.Context);
        var roleDefinitionsBefore = await ReadRoleDefinitionsAsync(fixture.Context);
        var roleGrantsBefore = await ReadRoleGrantsAsync(fixture.Context);

        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        Assert.Equal(existingPermissionsBefore, await ReadPermissionLinksAsync(fixture.Context, existingIds));
        Assert.Equal(existingRolesBefore, await ReadRoleLinksAsync(fixture.Context, existingIds));

        var newRoutes = ExpectedCatalogChildren.Skip(1).Select(item => item.Route).ToArray();
        var newItems = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => newRoutes.Contains(item.Route))
            .OrderBy(item => item.Order)
            .ToArrayAsync();
        Assert.Equal(8, newItems.Length);

        foreach (var item in newItems)
        {
            var permissions = await fixture.Context.MenuItemPermissions
                .AsNoTracking()
                .Where(link => link.MenuItemId == item.Id)
                .Select(link => link.PermissionId)
                .OrderBy(id => id)
                .ToArrayAsync();
            Assert.Equal(new[] { PermissionConfiguration.ManageAchPermissionId }, permissions);

            var roles = await fixture.Context.MenuItemRoles
                .AsNoTracking()
                .Where(link => link.MenuItemId == item.Id)
                .Select(link => link.RoleId)
                .OrderBy(id => id)
                .ToArrayAsync();
            Assert.Equal(
                new[] { RoleConfiguration.AdminRoleId, RoleConfiguration.OperatorRoleId }.OrderBy(id => id).ToArray(),
                roles);
        }

        Assert.Equal(permissionDefinitionsBefore, await ReadPermissionDefinitionsAsync(fixture.Context));
        Assert.Equal(roleDefinitionsBefore, await ReadRoleDefinitionsAsync(fixture.Context));
        Assert.Equal(roleGrantsBefore, await ReadRoleGrantsAsync(fixture.Context));
    }

    [Fact]
    public async Task CanonicalSeeder_PreservesNineRootsAchCyclesAndPendingServiceTestsState()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        await new NavigationInformationArchitectureSeeder(fixture.Context).SeedAsync();
        fixture.Context.ChangeTracker.Clear();

        var roots = await fixture.Context.MenuItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.ParentId == null)
            .OrderBy(item => item.Order)
            .Select(item => new { item.Route, item.Label, item.Order })
            .ToArrayAsync();

        Assert.Equal(
            new[]
            {
                (Route: "/dashboard", Label: "Inicio", Order: 10),
                (Route: "/transactions", Label: "Operación", Order: 20),
                (Route: "/customers", Label: "Clientes", Order: 30),
                (Route: "/seguimiento-control", Label: "Seguimiento y control", Order: 40),
                (Route: "/administracion-parametros", Label: "Administración de parámetros", Order: 50),
                (Route: "/nacha-security", Label: "Seguridad de archivos", Order: 60),
                (Route: "/reports", Label: "Reportes", Order: 70),
                (Route: "/administracion", Label: "Administración", Order: 80),
                (Route: "/uat", Label: "Herramientas de prueba", Order: 90)
            },
            roots.Select(item => (item.Route, item.Label, item.Order)).ToArray());

        Assert.Equal(1, await fixture.Context.MenuItems.CountAsync(item => item.IsActive && item.Route == "/ach-cycles"));

        var serviceTests = await fixture.Context.MenuItems
            .AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.ServiceTestsRoute);
        Assert.False(serviceTests.IsActive);
    }

    [Fact]
    public async Task CanonicalSeeder_IsIdempotentForNav210BCatalogsState()
    {
        await using var fixture = CreateFixture();
        await SeedFunctionalNavigationAsync(fixture.Context);
        var seeder = new NavigationInformationArchitectureSeeder(fixture.Context);

        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        var first = await ReadCatalogSnapshotAsync(fixture.Context);

        await seeder.SeedAsync();
        fixture.Context.ChangeTracker.Clear();
        var second = await ReadCatalogSnapshotAsync(fixture.Context);

        Assert.Equal(first, second);
    }

    private static async Task<string[]> ReadCatalogSnapshotAsync(AchDbContext context)
    {
        var catalogRoutes = ExpectedCatalogChildren.Select(item => item.Route)
            .Append("/catalogs")
            .Append("/catalogs/clearing-house-special-dates")
            .ToArray();

        var items = await context.MenuItems
            .AsNoTracking()
            .Where(item => catalogRoutes.Contains(item.Route))
            .OrderBy(item => item.Id)
            .Select(item => new
            {
                item.Id,
                item.Route,
                item.Label,
                item.ParentId,
                item.Order,
                item.IsActive,
                item.Exact
            })
            .ToArrayAsync();

        var ids = items.Select(item => item.Id).ToArray();
        var permissions = await ReadPermissionLinksAsync(context, ids);
        var roles = await ReadRoleLinksAsync(context, ids);

        return items.Select(item =>
                $"ITEM|{item.Id}|{item.Route}|{item.Label}|{item.ParentId}|{item.Order}|{item.IsActive}|{item.Exact}")
            .Concat(permissions.Select(value => $"PERM|{value}"))
            .Concat(roles.Select(value => $"ROLE|{value}"))
            .ToArray();
    }

    private static async Task<string[]> ReadPermissionLinksAsync(AchDbContext context, int[] ids)
    {
        var rows = await context.MenuItemPermissions
            .AsNoTracking()
            .Where(link => ids.Contains(link.MenuItemId))
            .OrderBy(link => link.MenuItemId)
            .ThenBy(link => link.PermissionId)
            .Select(link => new { link.MenuItemId, link.PermissionId })
            .ToArrayAsync();
        return rows.Select(link => $"{link.MenuItemId}|{link.PermissionId}").ToArray();
    }

    private static async Task<string[]> ReadRoleLinksAsync(AchDbContext context, int[] ids)
    {
        var rows = await context.MenuItemRoles
            .AsNoTracking()
            .Where(link => ids.Contains(link.MenuItemId))
            .OrderBy(link => link.MenuItemId)
            .ThenBy(link => link.RoleId)
            .Select(link => new { link.MenuItemId, link.RoleId })
            .ToArrayAsync();
        return rows.Select(link => $"{link.MenuItemId}|{link.RoleId}").ToArray();
    }

    private static async Task<string[]> ReadPermissionDefinitionsAsync(AchDbContext context)
    {
        var rows = await context.Permissions
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.Name })
            .ToArrayAsync();
        return rows.Select(item => $"{item.Id}|{item.Name}").ToArray();
    }

    private static async Task<string[]> ReadRoleDefinitionsAsync(AchDbContext context)
    {
        var rows = await context.Roles
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.Name })
            .ToArrayAsync();
        return rows.Select(item => $"{item.Id}|{item.Name}").ToArray();
    }

    private static async Task<string[]> ReadRoleGrantsAsync(AchDbContext context)
    {
        var rows = await context.RolePermissions
            .AsNoTracking()
            .OrderBy(item => item.RoleId)
            .ThenBy(item => item.PermissionId)
            .Select(item => new { item.RoleId, item.PermissionId })
            .ToArrayAsync();
        return rows.Select(item => $"{item.RoleId}|{item.PermissionId}").ToArray();
    }

    private static async Task SeedFunctionalNavigationAsync(AchDbContext context)
    {
        IDbSeeder[] seeders =
        [
            new ClaimsPermissionSeeder(context),
            new CfaOperationalTransactionRestrictionsPermissionSeeder(context),
            new NachaSecurityMenuSeeder(context),
            new NachaConfigMenuSeeder(context),
            new OperationalRestrictionsMenuSeeder(context),
            new UsersMenuSeeder(context),
            new CyclesMenuSeeder(context),
            new IncomingNachaCommandCenterMenuSeeder(context),
            new AchClaimsMenuSeeder(context),
            new AchColombiaFileExchangeMenuSeeder(context),
            new AchColombiaMftAdministrationMenuSeeder(context),
            new OutgoingTransactionMonitoringSeeder(context),
            new ReportsMenuSeeder(context)
        ];

        foreach (var seeder in DbSeederExecutionOrder.Apply(seeders))
            await seeder.SeedAsync();
    }

    private static Fixture CreateFixture()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options);
        context.Database.EnsureCreated();

        return new Fixture(context, connection);
    }

    private sealed class Fixture(AchDbContext context, SqliteConnection connection) : IAsyncDisposable
    {
        public AchDbContext Context { get; } = context;

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
