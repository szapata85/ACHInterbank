using System.Reflection;
using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.External;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav34IPlatformCapabilitiesAuthorizationTests
{
    [Fact]
    public async Task CanonicalReadPermission_IsPersistedIdempotently_WithoutGrants()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;
        var grantsBefore = await RoleGrantsAsync(context);

        Assert.Contains(FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry, FineGrainedPermissions.AllPermissions);
        Assert.False(await context.Permissions.AsNoTracking()
            .AnyAsync(item => item.Name == FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry));

        var seeder = new PaymentRailCapabilityRegistryPermissionSeeder(context);
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var permission = await context.Permissions.AsNoTracking()
            .SingleAsync(item => item.Name == FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry);
        Assert.Equal(PaymentRailCapabilityRegistryPermissionSeeder.PermissionId, permission.Id);
        Assert.Equal("Consultar capacidades de la plataforma", permission.Description);
        Assert.Empty(await context.RolePermissions.AsNoTracking()
            .Where(link => link.PermissionId == permission.Id)
            .ToArrayAsync());
        Assert.Equal(grantsBefore, await RoleGrantsAsync(context));
    }

    [Fact]
    public async Task CurrentSeededAudience_HasLegacyFallbacksOnlyOnAdminAndOperator_AndNoDedicatedGrant()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;
        await new PaymentRailCapabilityRegistryPermissionSeeder(context).SeedAsync();

        var manageRoles = await context.RolePermissions.AsNoTracking()
            .Where(link => link.PermissionId == PermissionConfiguration.ManageAchPermissionId)
            .OrderBy(link => link.RoleId)
            .Select(link => link.RoleId)
            .ToArrayAsync();
        var readRoles = await context.RolePermissions.AsNoTracking()
            .Where(link => link.PermissionId == PermissionConfiguration.ReadAchPermissionId)
            .OrderBy(link => link.RoleId)
            .Select(link => link.RoleId)
            .ToArrayAsync();
        var dedicatedRoles = await context.RolePermissions.AsNoTracking()
            .Where(link => link.PermissionId == PaymentRailCapabilityRegistryPermissionSeeder.PermissionId)
            .Select(link => link.RoleId)
            .ToArrayAsync();

        var expected = new[] { RoleConfiguration.AdminRoleId, RoleConfiguration.OperatorRoleId }.OrderBy(id => id).ToArray();
        Assert.Equal(expected, manageRoles.OrderBy(id => id).ToArray());
        Assert.Equal(expected, readRoles.OrderBy(id => id).ToArray());
        Assert.Empty(dedicatedRoles);
        Assert.Equal(2, await context.Roles.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task MenuAuthorization_MirrorsEffectivePolicyAlternatives_WithoutRoleAndBoundary_AndIsIdempotent()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;
        await SeedFunctionalNavigationAsync(context);
        await new NavigationInformationArchitectureSeeder(context).SeedAsync();
        await new PaymentRailCapabilityRegistryPermissionSeeder(context).SeedAsync();
        context.ChangeTracker.Clear();

        var grantsBefore = await RoleGrantsAsync(context);
        var seeder = new PaymentRailCapabilityRegistryMenuAuthorizationSeeder(context);
        Assert.True(NavigationInformationArchitectureSeeder.FinalReconciliationOrder < seeder.Order);
        Assert.True(NavigationMenuAuthorizationSeeder.AuthorizationReconciliationOrder < seeder.Order);

        await seeder.SeedAsync();
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var administration = await context.MenuItems.AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.AdministrationRoute);
        var item = await context.MenuItems.AsNoTracking()
            .SingleAsync(value => value.Route == NavigationInformationArchitectureSeeder.PlatformCapabilitiesRoute);
        Assert.Equal(NavigationInformationArchitectureSeeder.PlatformCapabilitiesLabel, item.Label);
        Assert.Equal(administration.Id, item.ParentId);
        Assert.Equal(50, item.Order);
        Assert.True(item.IsActive);

        var permissions = await context.MenuItemPermissions.AsNoTracking()
            .Where(link => link.MenuItemId == item.Id)
            .OrderBy(link => link.PermissionId)
            .Select(link => link.PermissionId)
            .ToArrayAsync();
        Assert.Equal(
            new[]
            {
                PaymentRailCapabilityRegistryPermissionSeeder.PermissionId,
                PermissionConfiguration.ManageAchPermissionId,
                PermissionConfiguration.ReadAchPermissionId
            }.OrderBy(id => id).ToArray(),
            permissions);
        Assert.Empty(await context.MenuItemRoles.AsNoTracking()
            .Where(link => link.MenuItemId == item.Id)
            .ToArrayAsync());
        Assert.Equal(grantsBefore, await RoleGrantsAsync(context));
    }

    [Fact]
    public async Task ExistingBackendPolicy_AcceptsDedicatedAndLegacyFallbacks_ButNotUnrelatedPermission()
    {
        using var provider = Provider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        Assert.True((await authorization.AuthorizeAsync(User(FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry), null, FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(User("CanManageAch"), null, FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(User("CanReadAch"), null, FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(User("CanReadCatalogs"), null, FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), null, FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)).Succeeded);
    }

    [Fact]
    public void ApiSurface_RemainsGetOnly_AndEveryEndpointUsesCanonicalReadPolicy()
    {
        var methods = typeof(PaymentRailCapabilityRegistryController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .ToArray();

        Assert.Equal(3, methods.Length);
        foreach (var method in methods)
        {
            var httpMethods = method.GetCustomAttributes<HttpMethodAttribute>(true).ToArray();
            Assert.Single(httpMethods);
            Assert.IsType<HttpGetAttribute>(httpMethods[0]);
            Assert.Contains(method.GetCustomAttributes<AuthorizeAttribute>(true),
                attribute => attribute.Policy == FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry
                             && string.IsNullOrWhiteSpace(attribute.Roles));
        }

        Assert.DoesNotContain(methods, method => method.GetCustomAttributes(true)
            .Any(attribute => attribute is HttpPostAttribute or HttpPutAttribute or HttpPatchAttribute or HttpDeleteAttribute));
    }

    [Fact]
    public async Task SpaSurface_RemainsReadOnly_AndDirectRouteUsesOnlyEffectivePermissionAlternatives()
    {
        var root = RepositoryRoot();
        var appRouting = await Read(root, "web/ach-interbank-ui/src/app/app-routing.module.ts");
        var featureRouting = await Read(root, "web/ach-interbank-ui/src/app/features/payment-rail-capability-registry/payment-rail-capability-registry-routing.module.ts");
        var component = await Read(root, "web/ach-interbank-ui/src/app/features/payment-rail-capability-registry/pages/payment-rail-capability-registry-page.component.ts");
        var html = await Read(root, "web/ach-interbank-ui/src/app/features/payment-rail-capability-registry/pages/payment-rail-capability-registry-page.component.html");
        var service = await Read(root, "web/ach-interbank-ui/src/app/features/payment-rail-capability-registry/services/payment-rail-capability-registry-api.service.ts");

        var route = Slice(appRouting, "path: 'payment-rail-capability-registry'", "path: 'unauthorized'");
        Assert.Contains("canActivate: [permissionGuard]", route);
        Assert.Contains("permissions: ['CanViewPaymentRailCapabilityRegistry', 'CanManageAch', 'CanReadAch']", route);
        Assert.DoesNotContain("roles:", route);
        Assert.DoesNotContain("roleGuard", route);
        Assert.Contains("path: '', component: PaymentRailCapabilityRegistryPageComponent", featureRouting);

        Assert.Contains("getRails()", service);
        Assert.Contains("getCapabilitiesByRail", service);
        Assert.Contains("getCapabilityByRail", service);
        Assert.Equal(3, Count(service, "this.api.get<"));
        Assert.DoesNotContain("this.api.post<", service);
        Assert.DoesNotContain("this.api.put<", service);
        Assert.DoesNotContain("this.api.patch<", service);
        Assert.DoesNotContain("this.api.delete<", service);
        Assert.DoesNotContain("cutover", component, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Modo', valor: 'Solo lectura'", component);
        Assert.Contains("Esta pantalla no permite edición", html);
        Assert.Contains("sin cutover", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistryServiceHasInternalUpsert_ButItIsNotExposedByThisUiOrController()
    {
        var root = RepositoryRoot();
        var contract = await Read(root, "src/Cfa.ACHInterbank.Application/ACH/Interfaces/PaymentRails/IPaymentRailCapabilityRegistryService.cs");
        var implementation = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/PaymentRailCapabilityRegistryService.cs");
        var controller = await Read(root, "src/Cfa.ACHInterbank.Api/Controllers/PaymentRailCapabilityRegistryController.cs");
        var angular = await Read(root, "web/ach-interbank-ui/src/app/features/payment-rail-capability-registry/services/payment-rail-capability-registry-api.service.ts");

        Assert.Contains("UpsertCapabilityAsync", contract);
        Assert.Contains("UpsertCapabilityAsync", implementation);
        Assert.Contains("SaveChangesAsync", implementation);
        Assert.DoesNotContain("UpsertCapabilityAsync", controller);
        Assert.DoesNotContain("[HttpPost", controller);
        Assert.DoesNotContain("[HttpPut", controller);
        Assert.DoesNotContain("[HttpPatch", controller);
        Assert.DoesNotContain("[HttpDelete", controller);
        Assert.DoesNotContain("this.api.post<", angular);
        Assert.DoesNotContain("this.api.put<", angular);
        Assert.DoesNotContain("this.api.patch<", angular);
        Assert.DoesNotContain("this.api.delete<", angular);
    }

    [Fact]
    public async Task Regression_PreviousNav34SecuritySurfacesAndAdministrationStructureRemainIntact()
    {
        var root = RepositoryRoot();
        var ia = await Read(root, "src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs");
        var appRouting = await Read(root, "web/ach-interbank-ui/src/app/app-routing.module.ts");
        var handler = await Read(root, "src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs");
        var navigationMenuSeeder = await Read(root, "src/Cfa.ACHInterbank.Persistence/Security/Seeders/NavigationMenuAuthorizationSeeder.cs");
        var systemRecordsSeeder = await Read(root, "src/Cfa.ACHInterbank.Persistence/Security/Seeders/SystemRecordsMenuAuthorizationSeeder.cs");

        Assert.Contains("Reconcile(menuOrganization, MenuOrganizationLabel, administration.Id, order: 40);", ia);
        Assert.Contains("order: 50", Slice(ia, "PlatformCapabilitiesRoute", "ReconcileTestToolsAsync"));
        Assert.Contains("permissions: ['NavigationMenu.Manage', 'CanManageUsers']", appRouting);
        Assert.Contains("permissions: ['NavigationLogs.Read', 'CanReadAch']", appRouting);
        Assert.Contains("NavigationMenuPermissionSeeder.PermissionId", navigationMenuSeeder);
        Assert.Contains("SystemRecordPermissionsSeeder.NavigationReadPermissionId", systemRecordsSeeder);
        Assert.Contains("return hasRole && hasPermission;", handler);
    }

    private static int Count(string source, string marker)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(marker, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += marker.Length;
        }
        return count;
    }

    private static async Task<string[]> RoleGrantsAsync(AchDbContext context)
        => (await context.RolePermissions.AsNoTracking()
            .OrderBy(link => link.RoleId)
            .ThenBy(link => link.PermissionId)
            .Select(link => new { link.RoleId, link.PermissionId })
            .ToArrayAsync())
            .Select(link => $"{link.RoleId}|{link.PermissionId}")
            .ToArray();

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
        {
            await seeder.SeedAsync();
        }
    }

    private static Fixture CreateFixture()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options);
        context.Database.EnsureCreated();
        return new Fixture(context, connection);
    }

    private static ServiceProvider Provider()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["appSettings:tokenManager:issuerJwt"] = "issuer",
                ["appSettings:tokenManager:audienceJwt"] = "audience",
                ["appSettings:tokenManager:secretKetJwt"] = "this-is-a-test-secret-key-with-32-bytes"
            })
            .Build();
        services.AddExternal(configuration);
        return services.BuildServiceProvider();
    }

    private static ClaimsPrincipal User(string permission)
        => new(new ClaimsIdentity([new Claim("permission", permission)], "test"));

    private static async Task<string> Read(string root, string relativePath)
        => await File.ReadAllTextAsync(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string Slice(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Start marker not found: {startMarker}");
        var end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        Assert.True(end > start, $"End marker not found after start: {endMarker}");
        return source[start..end];
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ACHInterbank.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory!.FullName;
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
