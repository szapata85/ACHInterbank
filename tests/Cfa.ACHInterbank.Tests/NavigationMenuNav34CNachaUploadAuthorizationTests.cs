using System.Reflection;
using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.DataBase.Queries.Navigation;
using Cfa.ACHInterbank.Application.Navigation;
using Cfa.ACHInterbank.Application.Navigation.Queries;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.External;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav34CNachaUploadAuthorizationTests
{
    [Fact]
    public async Task NachaUploadMenu_UsesUploadCapabilityWithoutReadFallback_AndPreservesIdentity()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;

        var before = await context.MenuItems.AsNoTracking()
            .SingleAsync(item => item.Id == MenuItemConfiguration.NachaUploadId);
        var createLinksBefore = await context.MenuItemPermissions.AsNoTracking()
            .Where(link => link.MenuItemId == MenuItemConfiguration.TransactionsCreateId)
            .OrderBy(link => link.PermissionId)
            .Select(link => link.PermissionId)
            .ToArrayAsync();
        var returnsLinksBefore = await context.MenuItemPermissions.AsNoTracking()
            .Where(link => link.MenuItemId == MenuItemConfiguration.TransactionsReturnsId)
            .OrderBy(link => link.PermissionId)
            .Select(link => link.PermissionId)
            .ToArrayAsync();
        var rolesBefore = await context.MenuItemRoles.AsNoTracking()
            .Where(link => link.MenuItemId == MenuItemConfiguration.NachaUploadId)
            .OrderBy(link => link.RoleId)
            .Select(link => link.RoleId)
            .ToArrayAsync();

        var permissionSeeder = new NachaUploadPermissionSeeder(context);
        var authorizationSeeder = new NachaUploadMenuAuthorizationSeeder(context);
        Assert.True(permissionSeeder.Order < authorizationSeeder.Order);
        Assert.True(TransactionsCreateMenuAuthorizationSeeder.AuthorizationReconciliationOrder < authorizationSeeder.Order);

        await permissionSeeder.SeedAsync();
        await authorizationSeeder.SeedAsync();
        await permissionSeeder.SeedAsync();
        await authorizationSeeder.SeedAsync();
        context.ChangeTracker.Clear();

        var after = await context.MenuItems.AsNoTracking()
            .SingleAsync(item => item.Id == MenuItemConfiguration.NachaUploadId);
        Assert.Equal(before.Label, after.Label);
        Assert.Equal(before.Route, after.Route);
        Assert.Equal(before.ParentId, after.ParentId);
        Assert.Equal(before.Order, after.Order);
        Assert.Equal(before.Exact, after.Exact);
        Assert.Equal(before.IsActive, after.IsActive);
        Assert.Equal("/transactions/nacha-upload", after.Route);

        var permissions = await context.MenuItemPermissions.AsNoTracking()
            .Where(link => link.MenuItemId == MenuItemConfiguration.NachaUploadId)
            .OrderBy(link => link.PermissionId)
            .Select(link => link.PermissionId)
            .ToArrayAsync();

        Assert.Equal(2, permissions.Length);
        Assert.Contains(PermissionConfiguration.ManageAchPermissionId, permissions);
        Assert.Contains(NachaUploadPermissionSeeder.PermissionId, permissions);
        Assert.DoesNotContain(PermissionConfiguration.ReadAchPermissionId, permissions);

        var rolesAfter = await context.MenuItemRoles.AsNoTracking()
            .Where(link => link.MenuItemId == MenuItemConfiguration.NachaUploadId)
            .OrderBy(link => link.RoleId)
            .Select(link => link.RoleId)
            .ToArrayAsync();
        Assert.Equal(rolesBefore, rolesAfter);

        Assert.Equal(createLinksBefore, await context.MenuItemPermissions.AsNoTracking()
            .Where(link => link.MenuItemId == MenuItemConfiguration.TransactionsCreateId)
            .OrderBy(link => link.PermissionId)
            .Select(link => link.PermissionId)
            .ToArrayAsync());
        Assert.Equal(returnsLinksBefore, await context.MenuItemPermissions.AsNoTracking()
            .Where(link => link.MenuItemId == MenuItemConfiguration.TransactionsReturnsId)
            .OrderBy(link => link.PermissionId)
            .Select(link => link.PermissionId)
            .ToArrayAsync());
    }

    [Fact]
    public async Task NachaUploadMenuHandler_ReadOnlyDoesNotExposeUpload_ButUploadAlternativesDo()
    {
        var fine = new Permission { Name = FineGrainedPermissions.Nacha.Upload };
        var manage = new Permission { Name = "CanManageAch" };
        var item = new MenuItem
        {
            Id = MenuItemConfiguration.NachaUploadId,
            Label = "Cargar archivo NACHA-M",
            Route = "/transactions/nacha-upload",
            Order = 50,
            Exact = true,
            IsActive = true
        };
        item.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = item.Id, Permission = fine });
        item.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = item.Id, Permission = manage });

        var repository = new StaticMenuRepository([item]);

        var readOnly = await new GetMenuForCurrentUserHandler(
                repository,
                new TestCurrentUserContext([new Claim("permission", "CanReadAch")]))
            .Handle(new GetMenuForCurrentUserQuery(), CancellationToken.None);
        Assert.DoesNotContain(readOnly, menu => menu.Route == "/transactions/nacha-upload");

        var fineGrained = await new GetMenuForCurrentUserHandler(
                repository,
                new TestCurrentUserContext([new Claim("permission", FineGrainedPermissions.Nacha.Upload)]))
            .Handle(new GetMenuForCurrentUserQuery(), CancellationToken.None);
        Assert.Contains(fineGrained, menu => menu.Route == "/transactions/nacha-upload");

        var legacyManage = await new GetMenuForCurrentUserHandler(
                repository,
                new TestCurrentUserContext([new Claim("permission", "CanManageAch")]))
            .Handle(new GetMenuForCurrentUserQuery(), CancellationToken.None);
        Assert.Contains(legacyManage, menu => menu.Route == "/transactions/nacha-upload");
    }

    [Fact]
    public async Task NachaUploadPermission_IsExistingFineGrainedContract_AndSeederAddsNoRoleGrant()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;

        Assert.Contains(FineGrainedPermissions.Nacha.Upload, FineGrainedPermissions.AllPermissions);

        var seeder = new NachaUploadPermissionSeeder(context);
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var permission = await context.Permissions.AsNoTracking()
            .SingleAsync(item => item.Id == NachaUploadPermissionSeeder.PermissionId);
        Assert.Equal(FineGrainedPermissions.Nacha.Upload, permission.Name);

        var grants = await context.RolePermissions.AsNoTracking()
            .Where(link => link.PermissionId == NachaUploadPermissionSeeder.PermissionId)
            .ToArrayAsync();
        Assert.Empty(grants);
    }

    [Fact]
    public async Task NachaUploadSpa_UsesSameUploadAlternatives_WhileParentAndCreateRemainUnchanged()
    {
        var root = RepositoryRoot();
        var routing = await File.ReadAllTextAsync(Path.Combine(
            root, "web", "ach-interbank-ui", "src", "app", "features", "transactions", "transactions-routing.module.ts"));
        var appRouting = await File.ReadAllTextAsync(Path.Combine(
            root, "web", "ach-interbank-ui", "src", "app", "app-routing.module.ts"));

        var uploadRoute = Slice(routing, "path: 'nacha-upload'", "path: 'cycle-configs'");
        Assert.Contains("canActivate: [permissionGuard]", uploadRoute);
        Assert.Contains("permissions: ['Nacha.Upload', 'CanManageAch']", uploadRoute);
        Assert.DoesNotContain("CanReadAch", uploadRoute);
        Assert.DoesNotContain("roles:", uploadRoute);

        var createRoute = Slice(routing, "path: 'create'", "path: 'bulk-create'");
        Assert.Contains("permissions: ['Transactions.Create', 'CanManageAch']", createRoute);

        var parentRoute = Slice(appRouting, "path: 'transactions'", "path: 'integraciones'");
        Assert.Contains("roles: ['Admin', 'ACH.Operator']", parentRoute);
        Assert.Contains("permissions: ['CanManageAch', 'CanReadAch']", parentRoute);
    }

    [Fact]
    public async Task NachaUploadApi_UsesFineOrLegacyManagement_WhileRecordsRemainReadOnly()
    {
        AssertActionPolicy(nameof(NachaUploadController.UploadNachaFile), P1Policies.NachaUpload);
        AssertActionPolicy(nameof(NachaUploadController.GetUploadedRecords), P1Policies.NachaRead);

        using var provider = BuildExternalProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        Assert.True((await authorization.AuthorizeAsync(
            Principal(FineGrainedPermissions.Nacha.Upload), null, P1Policies.NachaUpload)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(
            Principal("CanManageAch"), null, P1Policies.NachaUpload)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            Principal("CanReadAch"), null, P1Policies.NachaUpload)).Succeeded);

        Assert.True((await authorization.AuthorizeAsync(
            Principal(FineGrainedPermissions.Nacha.Read), null, P1Policies.NachaRead)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(
            Principal("CanReadAch"), null, P1Policies.NachaRead)).Succeeded);
    }

    [Fact]
    public async Task NachaUploadFlow_PersistsParsedGraphAndPlansAsyncDispatch_WithoutDirectSoapDispatch()
    {
        var root = RepositoryRoot();
        var component = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/components/nacha-upload/nacha-upload.component.ts");
        var spaService = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/services/nacha-upload.service.ts");
        var controller = await Read(root, "src/Cfa.ACHInterbank.Api/Controllers/NachaUploadController.cs");
        var ingestion = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaIngestionAppService.cs");
        var postParse = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostParseProcessor.cs");
        var planner = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaDispatchPlanner.cs");
        var localLive = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaLocalLivePreparationService.cs");

        Assert.Contains("this.nachaUpload.upload(file, clearingHouseId", component);
        Assert.Contains("this.nachaUpload.listRecords(filters)", component);
        Assert.Contains("this.submitUpload(true, this.lastUploadResult.ingestionId)", component);
        Assert.Contains("this.http.post(this.api.resolveUrl('NachaUpload/upload'), form)", spaService);
        Assert.Contains("this.http.get<NachaUploadRecord[]>(this.api.resolveUrl('NachaUpload/records')", spaService);

        Assert.Contains("_ingestionService.IngestAsync", controller);
        Assert.Contains("ParseAndSaveDetailedAsync", ingestion);
        Assert.Contains("_postParseProcessor.ProcessAsync", ingestion);
        Assert.Contains("_dispatchPlanner.PlanForIngestionAsync", postParse);
        Assert.Contains("_context.IncomingNachaDispatchQueue.Add", planner);

        Assert.DoesNotContain("IWscfaachSoapClient", controller);
        Assert.DoesNotContain("IContrapartidaDispatchJobService", controller);
        Assert.DoesNotContain("IWscfaachSoapClient", ingestion);
        Assert.DoesNotContain("IContrapartidaDispatchJobService", ingestion);
        Assert.DoesNotContain("IWscfaachSoapClient", planner);
        Assert.DoesNotContain("IContrapartidaDispatchJobService", planner);

        Assert.Contains("_environment.IsDevelopment()", localLive);
        Assert.Contains("RUN_LOCAL_SOAP_PROC_TRANSACCIONES_E2E", localLive);
        Assert.Contains("ALLOW_LOCAL_MONETARY_SOAP_E2E", localLive);
        Assert.Contains("ProcTransacciones:Mode", localLive);
        Assert.Contains("new AchTransaction", localLive);
    }

    [Fact]
    public async Task Nav34C_DoesNotRegressManualReviewOrTestToolsSecurity()
    {
        var root = RepositoryRoot();
        var manualReview = await Read(root, "web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-manual-review-page.component.ts");
        var testTools = await Read(root, "src/Cfa.ACHInterbank.Persistence/Security/Seeders/TestToolsMenuAuthorizationSeeder.cs");
        var securityModel = await Read(root, "src/Cfa.ACHInterbank.Persistence/Security/Seeders/TransactionsCreateMenuAuthorizationSeeder.cs");

        Assert.Contains("Nacha.Generate", manualReview);
        Assert.Contains("CanManageAch", manualReview);
        Assert.Contains("FineGrainedPermissions.TestTools.Access", testTools);
        Assert.Contains("serviceTests.IsActive = true;", testTools);
        Assert.Contains("TransactionsCreatePermissionSeeder.PermissionId", securityModel);
        Assert.DoesNotContain("PermissionConfiguration.ReadAchPermissionId", securityModel);
    }

    private static void AssertActionPolicy(string methodName, string expectedPolicy)
    {
        var action = typeof(NachaUploadController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(action);
        var authorize = action!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(expectedPolicy, authorize!.Policy);
    }

    private static ServiceProvider BuildExternalProvider()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["appSettings:tokenManager:issuerJwt"] = "issuer-test",
            ["appSettings:tokenManager:audienceJwt"] = "audience-test",
            ["appSettings:tokenManager:secretKetJwt"] = "this-is-a-test-secret-key-with-32-bytes"
        }).Build();
        services.AddExternal(config);
        return services.BuildServiceProvider();
    }

    private static ClaimsPrincipal Principal(params string[] permissions)
        => new(new ClaimsIdentity(
            permissions.Select(permission => new Claim("permission", permission)),
            "test"));

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
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }

    private sealed class StaticMenuRepository(IReadOnlyCollection<MenuItem> items) : IMenuQueryRepository
    {
        public Task<List<MenuItem>> GetActiveMenuItemsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(items.Where(item => item.IsActive).ToList());
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
