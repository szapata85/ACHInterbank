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
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav34MMftAdministrationAuthorizationTests
{
    [Fact]
    public async Task ConfigPermissions_AreExistingFineCapabilities_MaterializedIdempotently_WithoutGrants()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;
        var grantsBefore = await RoleGrantsAsync(context);

        Assert.Contains(FineGrainedPermissions.Config.Read, FineGrainedPermissions.AllPermissions);
        Assert.Contains(FineGrainedPermissions.Config.Manage, FineGrainedPermissions.AllPermissions);
        Assert.False(await context.Permissions.AsNoTracking().AnyAsync(x => x.Name == FineGrainedPermissions.Config.Read));
        Assert.False(await context.Permissions.AsNoTracking().AnyAsync(x => x.Name == FineGrainedPermissions.Config.Manage));

        var seeder = new ConfigurationPermissionSeeder(context);
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var read = await context.Permissions.AsNoTracking().SingleAsync(x => x.Name == FineGrainedPermissions.Config.Read);
        var manage = await context.Permissions.AsNoTracking().SingleAsync(x => x.Name == FineGrainedPermissions.Config.Manage);
        Assert.Equal(ConfigurationPermissionSeeder.ReadPermissionId, read.Id);
        Assert.Equal(ConfigurationPermissionSeeder.ManagePermissionId, manage.Id);
        Assert.Equal("Consultar configuración de la plataforma", read.Description);
        Assert.Equal("Administrar configuración de la plataforma", manage.Description);
        Assert.Empty(await context.RolePermissions.AsNoTracking()
            .Where(x => x.PermissionId == read.Id || x.PermissionId == manage.Id)
            .ToArrayAsync());
        Assert.Equal(grantsBefore, await RoleGrantsAsync(context));
    }

    [Fact]
    public async Task MftMenu_KeepsCanonicalIdentity_UsesConfigReadFallback_AndPreservesLegacyRoleBecauseParentStillBlocksSafeRemoval()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;
        await SeedFunctionalNavigationAsync(context);
        await new NavigationInformationArchitectureSeeder(context).SeedAsync();
        await new ConfigurationPermissionSeeder(context).SeedAsync();
        context.ChangeTracker.Clear();

        var parentBefore = await ParentAuthorizationAsync(context);
        var grantsBefore = await RoleGrantsAsync(context);
        var seeder = new MftAdministrationMenuAuthorizationSeeder(context);
        Assert.True(BulkIngestionUploadMenuAuthorizationSeeder.AuthorizationReconciliationOrder < seeder.Order);

        await seeder.SeedAsync();
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var parent = await context.MenuItems.AsNoTracking().SingleAsync(x => x.Route == "/integraciones");
        var item = await context.MenuItems.AsNoTracking().SingleAsync(x => x.Route == "/administracion/mft-ach");
        Assert.Equal("Conexiones", parent.Label);
        Assert.Equal("Configuración del intercambio de archivos", item.Label);
        Assert.Equal(parent.Id, item.ParentId);
        Assert.Equal(40, item.Order);
        Assert.True(item.IsActive);

        var permissions = await context.MenuItemPermissions.AsNoTracking()
            .Where(x => x.MenuItemId == item.Id)
            .Select(x => x.PermissionId)
            .OrderBy(x => x)
            .ToArrayAsync();
        Assert.Equal(new[]
        {
            ConfigurationPermissionSeeder.ReadPermissionId,
            PermissionConfiguration.ReadAchPermissionId
        }.OrderBy(x => x).ToArray(), permissions);

        var roles = await context.MenuItemRoles.AsNoTracking()
            .Where(x => x.MenuItemId == item.Id)
            .Select(x => x.RoleId)
            .ToArrayAsync();
        Assert.Equal(new[] { RoleConfiguration.AdminRoleId }, roles);
        Assert.Equal(parentBefore, await ParentAuthorizationAsync(context));
        Assert.Equal(grantsBefore, await RoleGrantsAsync(context));
    }

    [Fact]
    public async Task ConfigPolicies_SeparateReadFromManagement_AndKeepLegacyFallbacks()
    {
        using var provider = Provider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        Assert.True((await authorization.AuthorizeAsync(User(FineGrainedPermissions.Config.Read), null, P1Policies.ConfigRead)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(User("CanReadAch"), null, P1Policies.ConfigRead)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(User(FineGrainedPermissions.Config.Manage), null, P1Policies.ConfigRead)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(User("CanManageAch"), null, P1Policies.ConfigRead)).Succeeded);

        Assert.True((await authorization.AuthorizeAsync(User(FineGrainedPermissions.Config.Manage), null, P1Policies.ConfigManage)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(User("CanManageAch"), null, P1Policies.ConfigManage)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(User(FineGrainedPermissions.Config.Read), null, P1Policies.ConfigManage)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(User("CanReadAch"), null, P1Policies.ConfigManage)).Succeeded);
    }

    [Fact]
    public void ConsumedApiEndpoints_UseConfigReadForReads_AndConfigManageForAllMutationsAndConnectionTest()
    {
        AssertAction<AchColombiaFileExchangeController>(nameof(AchColombiaFileExchangeController.GetAdministration), typeof(HttpGetAttribute), P1Policies.ConfigRead);
        AssertAction<AchColombiaFileExchangeController>(nameof(AchColombiaFileExchangeController.UpdateAdministration), typeof(HttpPutAttribute), P1Policies.ConfigManage);
        AssertAction<AchColombiaFileExchangeController>(nameof(AchColombiaFileExchangeController.SetCredential), typeof(HttpPutAttribute), P1Policies.ConfigManage);
        AssertAction<AchColombiaFileExchangeController>(nameof(AchColombiaFileExchangeController.TestConnection), typeof(HttpPostAttribute), P1Policies.ConfigManage);
        AssertAction<AchColombiaCycleOperationalSchedulesController>(nameof(AchColombiaCycleOperationalSchedulesController.GetAdministration), typeof(HttpGetAttribute), P1Policies.ConfigRead);
        AssertAction<AchColombiaCycleOperationalSchedulesController>(nameof(AchColombiaCycleOperationalSchedulesController.Create), typeof(HttpPostAttribute), P1Policies.ConfigManage);
        AssertAction<AchColombiaCycleOperationalSchedulesController>(nameof(AchColombiaCycleOperationalSchedulesController.Update), typeof(HttpPutAttribute), P1Policies.ConfigManage);
    }

    [Fact]
    public async Task SpaEntry_IsReadCapabilityBased_AndEveryMutableActionUsesConfigManageOrLegacyManageFailClosed()
    {
        var root = RepositoryRoot();
        var routing = await Read(root, "web/ach-interbank-ui/src/app/app-routing.module.ts");
        var component = await Read(root, "web/ach-interbank-ui/src/app/features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.ts");
        var html = await Read(root, "web/ach-interbank-ui/src/app/features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.html");

        var route = Slice(routing, "path: 'administracion/mft-ach'", "path: 'cenit'");
        Assert.Contains("canActivate: [permissionGuard]", route);
        Assert.Contains("permissions: ['Config.Read', 'CanReadAch']", route);
        Assert.DoesNotContain("roles:", route);
        Assert.DoesNotContain("roleGuard", route);

        Assert.Contains("this.auth.hasPermission(['Config.Manage', 'CanManageAch'])", component);
        Assert.Contains("if (!this.model || !this.canManage || this.savingConnection) return;", component);
        Assert.Contains("if (!this.canManage || this.testingConnection) return;", component);
        Assert.Contains("if (!this.model || !this.canManage || this.savingSchedules) return;", component);
        Assert.Contains("*ngIf=\"canManage\"", html);
        Assert.Contains("[disabled]=\"!canManage", html);
        Assert.DoesNotContain("hasRole", component);
        Assert.DoesNotContain("ACH.Operator", component);
        Assert.DoesNotContain("Admin", component);
    }

    [Fact]
    public async Task Surface_OnlyConfiguresMftAndSchedules_SecretsStayBackend_AndOnlyConnectionTestHasImmediateExternalIo()
    {
        var root = RepositoryRoot();
        var component = await Read(root, "web/ach-interbank-ui/src/app/features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.ts");
        var fileService = await Read(root, "web/ach-interbank-ui/src/app/core/services/ach-colombia-file-exchange.service.ts");
        var scheduleService = await Read(root, "web/ach-interbank-ui/src/app/core/services/ach-colombia-operational-schedules.service.ts");
        var admin = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedMftAdministrationComponent.cs");
        var credential = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedMftCredentialComponent.cs");
        var tester = await Read(root, "src/Cfa.ACHInterbank.External/Connections/AchColombiaManagedMftConnectionTester.cs");
        var scheduleAdmin = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaCycleOperationalScheduleAdminService.cs");

        Assert.Contains("this.api.administration()", component);
        Assert.Contains("this.schedulesApi.administration()", component);
        Assert.Contains("this.api.updateAdministration", component);
        Assert.Contains("this.api.setCredential", component);
        Assert.Contains("this.api.testConnection()", component);
        Assert.Contains("this.schedulesApi.create", component);
        Assert.Contains("this.schedulesApi.update", component);
        Assert.DoesNotContain("executeOutbound", component);
        Assert.DoesNotContain("executeInbound", component);
        Assert.DoesNotContain("retry(", component);
        Assert.DoesNotContain("reprocess(", component);
        Assert.DoesNotContain("archive(", component);
        Assert.DoesNotContain("retire(", component);
        Assert.DoesNotContain("download(", component);

        Assert.Contains("`${this.base}/administration`", fileService);
        Assert.Contains("`${this.base}/administration/credential`", fileService);
        Assert.Contains("`${this.base}/administration/test-connection`", fileService);
        Assert.Contains("`${this.base}/administration`", scheduleService);

        Assert.Contains("!string.IsNullOrWhiteSpace(x.ProtectedCredential)", admin);
        Assert.DoesNotContain("x.ProtectedCredential,", admin);
        Assert.Contains("entity.ProtectedCredential = encryption.Encrypt(value.Secret);", credential);
        Assert.Contains("_encryption.Decrypt(configuration.ProtectedCredential!)", tester);
        Assert.Contains("_sessionFactory.Connect(configuration, password)", tester);
        Assert.Contains("session.List(path)", tester);
        Assert.DoesNotContain("Upload", tester);
        Assert.DoesNotContain("Download", tester);

        Assert.Contains("await context.SaveChangesAsync(ct);", scheduleAdmin);
        Assert.Contains("await scheduleSynchronizer.SynchronizeOutboundAsync(ct);", scheduleAdmin);
    }

    [Fact]
    public async Task Regression_ThreeExcludedConnectionSurfaces_ParentStructure_TestTools_AndPreviousNav34RemainUntouched()
    {
        var root = RepositoryRoot();
        var routing = await Read(root, "web/ach-interbank-ui/src/app/app-routing.module.ts");
        var integrationsRoutes = await Read(root, "web/ach-interbank-ui/src/app/features/integrations/integrations.routes.ts");
        var navigationService = await Read(root, "web/ach-interbank-ui/src/app/core/services/navigation.service.ts");
        var ia = await Read(root, "src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs");
        var testTools = await Read(root, "src/Cfa.ACHInterbank.Persistence/Security/Seeders/TestToolsMenuAuthorizationSeeder.cs");
        var bulkUpload = await Read(root, "src/Cfa.ACHInterbank.Persistence/Security/Seeders/BulkIngestionUploadMenuAuthorizationSeeder.cs");

        Assert.Contains("path: 'integraciones'", routing);
        Assert.Contains("roles: ['Admin', 'ACH.Operator']", Slice(routing, "path: 'integraciones'", "path: 'navigation'"));
        Assert.Contains("path: 'contrato'", integrationsRoutes);
        Assert.Contains("path: 'relacion-informacion'", integrationsRoutes);
        Assert.Contains("path: 'conexion'", integrationsRoutes);
        Assert.DoesNotContain("normalizeIntegrationNavigation", navigationService);
        Assert.DoesNotContain("Centro de integración CFA", navigationService);
        Assert.DoesNotContain("children: []", navigationService);
        Assert.Contains("\"Contrato del servicio\"", ia);
        Assert.Contains("\"Relación de información\"", ia);
        Assert.Contains("\"Conexión entidad participante\"", ia);
        Assert.Contains("FineGrainedPermissions.TestTools.Access", testTools);
        Assert.Equal(112, BulkIngestionUploadMenuAuthorizationSeeder.AuthorizationReconciliationOrder);
        Assert.DoesNotContain("/integraciones/contrato", await Read(root, "src/Cfa.ACHInterbank.Persistence/Security/Seeders/MftAdministrationMenuAuthorizationSeeder.cs"));
        Assert.DoesNotContain("/integraciones/relacion-informacion", await Read(root, "src/Cfa.ACHInterbank.Persistence/Security/Seeders/MftAdministrationMenuAuthorizationSeeder.cs"));
        Assert.DoesNotContain("/integraciones/conexion", await Read(root, "src/Cfa.ACHInterbank.Persistence/Security/Seeders/MftAdministrationMenuAuthorizationSeeder.cs"));
    }

    private static void AssertAction<TController>(string actionName, Type httpAttributeType, string expectedPolicy)
    {
        var method = typeof(TController).GetMethod(actionName);
        Assert.NotNull(method);
        Assert.Contains(method!.GetCustomAttributes(true), attribute => httpAttributeType.IsInstanceOfType(attribute));
        Assert.Contains(method.GetCustomAttributes<AuthorizeAttribute>(true), x => x.Policy == expectedPolicy);
    }

    private static async Task<string[]> RoleGrantsAsync(AchDbContext context)
        => (await context.RolePermissions.AsNoTracking()
            .OrderBy(x => x.RoleId)
            .ThenBy(x => x.PermissionId)
            .Select(x => new { x.RoleId, x.PermissionId })
            .ToArrayAsync())
            .Select(x => $"{x.RoleId}|{x.PermissionId}")
            .ToArray();

    private static async Task<string[]> ParentAuthorizationAsync(AchDbContext context)
    {
        var id = await context.MenuItems.AsNoTracking().Where(x => x.Route == "/integraciones").Select(x => x.Id).SingleAsync();
        var permissions = await context.MenuItemPermissions.AsNoTracking().Where(x => x.MenuItemId == id).Select(x => $"P:{x.PermissionId}").ToArrayAsync();
        var roles = await context.MenuItemRoles.AsNoTracking().Where(x => x.MenuItemId == id).Select(x => $"R:{x.RoleId}").ToArrayAsync();
        return permissions.Concat(roles).OrderBy(x => x).ToArray();
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
        {
            await seeder.SeedAsync();
        }
    }

    private static Fixture CreateFixture()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options);
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
