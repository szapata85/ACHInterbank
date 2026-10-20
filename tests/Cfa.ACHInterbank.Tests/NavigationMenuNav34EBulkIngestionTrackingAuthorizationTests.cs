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

public sealed class NavigationMenuNav34EBulkIngestionTrackingAuthorizationTests
{
    private const string TrackingRoute = "/transactions/bulk-ingestion/tracking";

    [Fact]
    public async Task TrackingPermissionSeeder_MaterializesExistingReadRetryCancelCapabilities_WithoutGrants()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;

        Assert.Contains(FineGrainedPermissions.BulkIngestion.Read, FineGrainedPermissions.AllPermissions);
        Assert.Contains(FineGrainedPermissions.BulkIngestion.Retry, FineGrainedPermissions.AllPermissions);
        Assert.Contains(FineGrainedPermissions.BulkIngestion.Cancel, FineGrainedPermissions.AllPermissions);

        var seeder = new BulkIngestionTrackingPermissionSeeder(context);
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var expected = new Dictionary<Guid, string>
        {
            [BulkIngestionTrackingPermissionSeeder.ReadPermissionId] = FineGrainedPermissions.BulkIngestion.Read,
            [BulkIngestionTrackingPermissionSeeder.RetryPermissionId] = FineGrainedPermissions.BulkIngestion.Retry,
            [BulkIngestionTrackingPermissionSeeder.CancelPermissionId] = FineGrainedPermissions.BulkIngestion.Cancel
        };

        var permissions = await context.Permissions.AsNoTracking()
            .Where(item => expected.Keys.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Name);
        Assert.Equal(expected.Count, permissions.Count);
        foreach (var pair in expected)
            Assert.Equal(pair.Value, permissions[pair.Key]);

        var grants = await context.RolePermissions.AsNoTracking()
            .Where(link => expected.Keys.Contains(link.PermissionId))
            .ToArrayAsync();
        Assert.Empty(grants);
    }

    [Fact]
    public async Task TrackingMenu_UsesBackendReadAlternatives_AndPreservesIdentityRolesAndUploadMenu()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;

        var tracking = new MenuItem
        {
            Id = 5001,
            MenuId = MenuConfiguration.MainMenuId,
            ParentId = MenuItemConfiguration.TransactionsId,
            Label = "Seguimiento de cargas",
            Route = TrackingRoute,
            Icon = "manage_search",
            Order = 40,
            Exact = true,
            IsActive = true
        };
        var upload = new MenuItem
        {
            Id = 5002,
            MenuId = MenuConfiguration.MainMenuId,
            ParentId = MenuItemConfiguration.TransactionsId,
            Label = "Cargar transacciones desde archivo",
            Route = "/transactions/bulk-ingestion/upload",
            Icon = "upload_file",
            Order = 30,
            Exact = true,
            IsActive = true
        };
        context.MenuItems.AddRange(tracking, upload);
        context.MenuItemPermissions.AddRange(
            new MenuItemPermission
            {
                MenuItemId = tracking.Id,
                PermissionId = PermissionConfiguration.ManageAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = upload.Id,
                PermissionId = PermissionConfiguration.ManageAchPermissionId
            });
        context.MenuItemRoles.AddRange(
            new MenuItemRole { MenuItemId = tracking.Id, RoleId = RoleConfiguration.AdminRoleId },
            new MenuItemRole { MenuItemId = tracking.Id, RoleId = RoleConfiguration.OperatorRoleId });
        await context.SaveChangesAsync();

        var before = await context.MenuItems.AsNoTracking().SingleAsync(item => item.Id == tracking.Id);
        var rolesBefore = await context.MenuItemRoles.AsNoTracking()
            .Where(link => link.MenuItemId == tracking.Id)
            .OrderBy(link => link.RoleId)
            .Select(link => link.RoleId)
            .ToArrayAsync();
        var uploadLinksBefore = await context.MenuItemPermissions.AsNoTracking()
            .Where(link => link.MenuItemId == upload.Id)
            .OrderBy(link => link.PermissionId)
            .Select(link => link.PermissionId)
            .ToArrayAsync();

        var permissionSeeder = new BulkIngestionTrackingPermissionSeeder(context);
        var authorizationSeeder = new BulkIngestionTrackingMenuAuthorizationSeeder(context);
        Assert.True(permissionSeeder.Order < authorizationSeeder.Order);
        Assert.True(ReturnsMenuAuthorizationSeeder.AuthorizationReconciliationOrder < authorizationSeeder.Order);

        await permissionSeeder.SeedAsync();
        await authorizationSeeder.SeedAsync();
        await permissionSeeder.SeedAsync();
        await authorizationSeeder.SeedAsync();
        context.ChangeTracker.Clear();

        var after = await context.MenuItems.AsNoTracking().SingleAsync(item => item.Id == tracking.Id);
        Assert.Equal(before.Label, after.Label);
        Assert.Equal(before.Route, after.Route);
        Assert.Equal(before.ParentId, after.ParentId);
        Assert.Equal(before.Order, after.Order);
        Assert.Equal(before.Icon, after.Icon);
        Assert.Equal(before.Exact, after.Exact);
        Assert.Equal(before.IsActive, after.IsActive);

        var permissionLinks = await context.MenuItemPermissions.AsNoTracking()
            .Where(link => link.MenuItemId == tracking.Id)
            .OrderBy(link => link.PermissionId)
            .Select(link => link.PermissionId)
            .ToArrayAsync();
        Assert.Equal(2, permissionLinks.Length);
        Assert.Contains(BulkIngestionTrackingPermissionSeeder.ReadPermissionId, permissionLinks);
        Assert.Contains(PermissionConfiguration.ReadAchPermissionId, permissionLinks);
        Assert.DoesNotContain(PermissionConfiguration.ManageAchPermissionId, permissionLinks);
        Assert.DoesNotContain(BulkIngestionTrackingPermissionSeeder.RetryPermissionId, permissionLinks);
        Assert.DoesNotContain(BulkIngestionTrackingPermissionSeeder.CancelPermissionId, permissionLinks);

        var rolesAfter = await context.MenuItemRoles.AsNoTracking()
            .Where(link => link.MenuItemId == tracking.Id)
            .OrderBy(link => link.RoleId)
            .Select(link => link.RoleId)
            .ToArrayAsync();
        Assert.Equal(rolesBefore, rolesAfter);
        Assert.Equal(uploadLinksBefore, await context.MenuItemPermissions.AsNoTracking()
            .Where(link => link.MenuItemId == upload.Id)
            .OrderBy(link => link.PermissionId)
            .Select(link => link.PermissionId)
            .ToArrayAsync());
    }

    [Fact]
    public async Task TrackingMenuDirectPermissionSemantics_AreReadOnlyAlternatives()
    {
        var fine = new Permission { Name = FineGrainedPermissions.BulkIngestion.Read };
        var legacy = new Permission { Name = "CanReadAch" };
        var item = new MenuItem
        {
            Id = 5001,
            Label = "Seguimiento de cargas",
            Route = TrackingRoute,
            Order = 40,
            Exact = true,
            IsActive = true
        };
        item.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = item.Id, Permission = fine });
        item.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = item.Id, Permission = legacy });
        var repository = new StaticMenuRepository([item]);

        Assert.Contains(await MenuFor(repository, FineGrainedPermissions.BulkIngestion.Read), x => x.Route == TrackingRoute);
        Assert.Contains(await MenuFor(repository, "CanReadAch"), x => x.Route == TrackingRoute);
        Assert.DoesNotContain(await MenuFor(repository, "CanManageAch"), x => x.Route == TrackingRoute);
        Assert.DoesNotContain(await MenuFor(repository), x => x.Route == TrackingRoute);
    }

    [Fact]
    public async Task BulkIngestionApi_SeparatesReadRetryAndCancelCapabilities()
    {
        AssertActionPolicy(nameof(BulkIngestionController.GetBatch), P1Policies.BulkIngestionRead);
        AssertActionPolicy(nameof(BulkIngestionController.GetBatchItems), P1Policies.BulkIngestionRead);
        AssertActionPolicy(nameof(BulkIngestionController.GetBatchSummary), P1Policies.BulkIngestionRead);
        AssertActionPolicy(nameof(BulkIngestionController.Retry), P1Policies.BulkIngestionRetry);
        AssertActionPolicy(nameof(BulkIngestionController.Cancel), P1Policies.BulkIngestionCancel);
        AssertActionPolicy(nameof(BulkIngestionController.Upload), P1Policies.BulkIngestionUpload);

        using var provider = BuildExternalProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        Assert.True((await authorization.AuthorizeAsync(Principal(FineGrainedPermissions.BulkIngestion.Read), null, P1Policies.BulkIngestionRead)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(Principal("CanReadAch"), null, P1Policies.BulkIngestionRead)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal("CanManageAch"), null, P1Policies.BulkIngestionRead)).Succeeded);

        Assert.True((await authorization.AuthorizeAsync(Principal(FineGrainedPermissions.BulkIngestion.Retry), null, P1Policies.BulkIngestionRetry)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(Principal("CanManageAch"), null, P1Policies.BulkIngestionRetry)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal("CanReadAch"), null, P1Policies.BulkIngestionRetry)).Succeeded);

        Assert.True((await authorization.AuthorizeAsync(Principal(FineGrainedPermissions.BulkIngestion.Cancel), null, P1Policies.BulkIngestionCancel)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(Principal("CanManageAch"), null, P1Policies.BulkIngestionCancel)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal("CanReadAch"), null, P1Policies.BulkIngestionCancel)).Succeeded);
    }

    [Fact]
    public async Task TrackingSpa_IsMixedReadAndMutation_WithFailClosedActionGates()
    {
        var root = RepositoryRoot();
        var routing = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts");
        var appRouting = await Read(root, "web/ach-interbank-ui/src/app/app-routing.module.ts");
        var tracking = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-tracking/bulk-ingestion-tracking.component.ts");
        var detail = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.ts");
        var detailTemplate = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.html");

        var trackingRoute = Slice(routing, "path: 'bulk-ingestion/tracking'", "path: 'bulk-ingestion/:batchId'");
        Assert.Contains("permissions: ['BulkIngestion.Read', 'CanReadAch']", trackingRoute);
        Assert.DoesNotContain("CanManageAch", trackingRoute);
        Assert.DoesNotContain("roles:", trackingRoute);

        var detailRoute = Slice(routing, "path: 'bulk-ingestion/:batchId'", "path: 'nacha-upload'");
        Assert.Contains("permissions: ['BulkIngestion.Read', 'CanReadAch']", detailRoute);
        Assert.DoesNotContain("CanManageAch", detailRoute);
        Assert.DoesNotContain("roles:", detailRoute);

        Assert.Contains("BULK_INGESTION_RETRY_PERMISSIONS = ['BulkIngestion.Retry', 'CanManageAch']", tracking);
        Assert.Contains("if (!this.ensureCanRetry())", tracking);
        Assert.Contains("No tiene permisos para reintentar lotes de carga.", tracking);

        Assert.Contains("BULK_INGESTION_RETRY_PERMISSIONS = ['BulkIngestion.Retry', 'CanManageAch']", detail);
        Assert.Contains("BULK_INGESTION_CANCEL_PERMISSIONS = ['BulkIngestion.Cancel', 'CanManageAch']", detail);
        Assert.Contains("if (!this.ensureCanRetry())", detail);
        Assert.Contains("if (!this.ensureCanCancel())", detail);
        Assert.Contains("canRetryBatchAction && canRetry()", detailTemplate);
        Assert.Contains("canCancelBatchAction && canCancel()", detailTemplate);

        Assert.DoesNotContain("Admin", tracking);
        Assert.DoesNotContain("ACH.Operator", tracking);
        Assert.DoesNotContain("Admin", detail);
        Assert.DoesNotContain("ACH.Operator", detail);

        var parent = Slice(appRouting, "path: 'transactions'", "path: 'integraciones'");
        Assert.Contains("roles: ['Admin', 'ACH.Operator']", parent);
        Assert.Contains("permissions: ['CanManageAch', 'CanReadAch']", parent);
    }

    [Fact]
    public async Task TrackingFunctionalBehavior_RetryRequeuesProcessing_CancelPersistsState_AndNoDirectExternalCallIsAdded()
    {
        var root = RepositoryRoot();
        var retry = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchBulkBatchRetryService.cs");
        var lifecycle = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/BulkIngestionLifecycleService.cs");
        var processor = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchBulkBatchProcessingService.cs");
        var bulkTransactions = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchBulkTransactionService.cs");
        var controller = await Read(root, "src/Cfa.ACHInterbank.Api/Controllers/BulkIngestionController.cs");

        Assert.Contains("item.Status = BulkIngestionItemStatusEnum.Ready", retry);
        Assert.Contains("batch.Status = BulkIngestionBatchStatusEnum.Retrying", retry);
        Assert.Contains("DispatchProcessingAsync", retry);
        Assert.Contains("_bulkTransactionService.RegisterBulkAsync", processor);
        Assert.Contains("_contrapartidaDispatchPersistenceService.EnsurePendingDispatchAsync", bulkTransactions);

        Assert.Contains("batch.Status = BulkIngestionBatchStatusEnum.Cancelled", lifecycle);
        Assert.Contains("ProcessingFinishedAtUtc", lifecycle);
        Assert.DoesNotContain("Unschedule", lifecycle);
        Assert.DoesNotContain("unschedule", lifecycle);
        Assert.DoesNotContain("DeleteJob", lifecycle);
        Assert.DoesNotContain("deleteJob", lifecycle);

        foreach (var source in new[] { controller, retry, lifecycle })
        {
            Assert.DoesNotContain("IWscfaachSoapClient", source);
            Assert.DoesNotContain("IContrapartidaDispatchJobService", source);
        }
    }

    [Fact]
    public async Task Nav34E_DoesNotModifyBulkUploadOrPreviouslyClosedSecurityModels()
    {
        var root = RepositoryRoot();
        var routing = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts");
        var uploadRoute = Slice(routing, "path: 'bulk-ingestion/upload'", "path: 'bulk-ingestion/tracking'");
        Assert.Contains("CanManageAch", uploadRoute);
        Assert.DoesNotContain("BulkIngestion.Read", uploadRoute);
        Assert.DoesNotContain("CanReadAch", uploadRoute);

        var create = await Read(root, "src/Cfa.ACHInterbank.Persistence/Security/Seeders/TransactionsCreateMenuAuthorizationSeeder.cs");
        var nacha = await Read(root, "src/Cfa.ACHInterbank.Persistence/Security/Seeders/NachaUploadMenuAuthorizationSeeder.cs");
        var returns = await Read(root, "src/Cfa.ACHInterbank.Persistence/Security/Seeders/ReturnsMenuAuthorizationSeeder.cs");
        var manualReview = await Read(root, "web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-manual-review-page.component.ts");
        var testTools = await Read(root, "src/Cfa.ACHInterbank.Persistence/Security/Seeders/TestToolsMenuAuthorizationSeeder.cs");

        Assert.Contains("TransactionsCreatePermissionSeeder.PermissionId", create);
        Assert.Contains("NachaUploadPermissionSeeder.PermissionId", nacha);
        Assert.Contains("PermissionConfiguration.ReadAchPermissionId", returns);
        Assert.Contains("Nacha.Generate", manualReview);
        Assert.Contains("FineGrainedPermissions.TestTools.Access", testTools);
    }

    private static void AssertActionPolicy(string methodName, string expectedPolicy)
    {
        var action = typeof(BulkIngestionController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(action);
        var authorize = action!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(expectedPolicy, authorize!.Policy);
    }

    private static async Task<IList<MenuItemDto>> MenuFor(StaticMenuRepository repository, params string[] permissions)
        => await new GetMenuForCurrentUserHandler(
                repository,
                new TestCurrentUserContext(permissions.Select(permission => new Claim("permission", permission))))
            .Handle(new GetMenuForCurrentUserQuery(), CancellationToken.None);

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
