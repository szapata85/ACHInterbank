using System.Reflection;
using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.DataBase.Queries.Navigation;
using Cfa.ACHInterbank.Application.Navigation;
using Cfa.ACHInterbank.Application.Navigation.Queries;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Entities.User;
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

public sealed class NavigationMenuNav34LBulkIngestionUploadAuthorizationTests
{
    private const string UploadRoute = "/transactions/bulk-ingestion/upload";

    [Fact]
    public async Task BulkIngestionUpload_IsExistingFineGrainedContract_AndSeederPersistsItIdempotentlyWithoutGrants()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;
        var grantsBefore = await RoleGrantsAsync(context);

        Assert.Contains(FineGrainedPermissions.BulkIngestion.Upload, FineGrainedPermissions.AllPermissions);
        Assert.False(await context.Permissions.AsNoTracking()
            .AnyAsync(item => item.Name == FineGrainedPermissions.BulkIngestion.Upload));

        var seeder = new BulkIngestionUploadPermissionSeeder(context);
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var permission = await context.Permissions.AsNoTracking()
            .SingleAsync(item => item.Name == FineGrainedPermissions.BulkIngestion.Upload);
        Assert.Equal(BulkIngestionUploadPermissionSeeder.UploadPermissionId, permission.Id);
        Assert.Equal("Cargar transacciones desde archivo", permission.Description);
        Assert.Empty(await context.RolePermissions.AsNoTracking()
            .Where(link => link.PermissionId == permission.Id)
            .ToArrayAsync());
        Assert.Equal(grantsBefore, await RoleGrantsAsync(context));
    }

    [Fact]
    public async Task UploadMenu_UsesUploadOrCanManageAch_PreservesStructureAndLegacyRoles_AndAddsNoGrants()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;

        var item = new MenuItem
        {
            Id = 7101,
            MenuId = MenuConfiguration.MainMenuId,
            ParentId = MenuItemConfiguration.TransactionsId,
            Label = "Cargar transacciones desde archivo",
            Route = UploadRoute,
            Icon = "upload_file",
            Order = 30,
            Exact = true,
            IsActive = true
        };
        context.MenuItems.Add(item);
        context.MenuItemPermissions.Add(new MenuItemPermission
        {
            MenuItemId = item.Id,
            PermissionId = PermissionConfiguration.ManageAchPermissionId
        });
        context.MenuItemRoles.AddRange(
            new MenuItemRole { MenuItemId = item.Id, RoleId = RoleConfiguration.AdminRoleId },
            new MenuItemRole { MenuItemId = item.Id, RoleId = RoleConfiguration.OperatorRoleId });
        await context.SaveChangesAsync();

        var before = await context.MenuItems.AsNoTracking().SingleAsync(value => value.Id == item.Id);
        var rolesBefore = await context.MenuItemRoles.AsNoTracking()
            .Where(link => link.MenuItemId == item.Id)
            .OrderBy(link => link.RoleId)
            .Select(link => link.RoleId)
            .ToArrayAsync();
        var grantsBefore = await RoleGrantsAsync(context);

        await new BulkIngestionUploadPermissionSeeder(context).SeedAsync();
        var seeder = new BulkIngestionUploadMenuAuthorizationSeeder(context);
        Assert.True(NavigationInformationArchitectureSeeder.FinalReconciliationOrder < seeder.Order);
        Assert.True(ReturnOfReturnMenuAuthorizationSeeder.AuthorizationReconciliationOrder < seeder.Order);
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var after = await context.MenuItems.AsNoTracking().SingleAsync(value => value.Id == item.Id);
        Assert.Equal(before.Label, after.Label);
        Assert.Equal(before.Route, after.Route);
        Assert.Equal(before.ParentId, after.ParentId);
        Assert.Equal(before.Order, after.Order);
        Assert.Equal(before.Icon, after.Icon);
        Assert.Equal(before.Exact, after.Exact);
        Assert.Equal(before.IsActive, after.IsActive);
        Assert.Equal(30, after.Order);
        Assert.Equal(MenuItemConfiguration.TransactionsId, after.ParentId);

        var permissions = await context.MenuItemPermissions.AsNoTracking()
            .Where(link => link.MenuItemId == item.Id)
            .OrderBy(link => link.PermissionId)
            .Select(link => link.PermissionId)
            .ToArrayAsync();
        Assert.Equal(
            new[] { PermissionConfiguration.ManageAchPermissionId, BulkIngestionUploadPermissionSeeder.UploadPermissionId }
                .OrderBy(value => value)
                .ToArray(),
            permissions);
        Assert.DoesNotContain(PermissionConfiguration.ReadAchPermissionId, permissions);
        Assert.DoesNotContain(BulkIngestionTrackingPermissionSeeder.ReadPermissionId, permissions);

        var rolesAfter = await context.MenuItemRoles.AsNoTracking()
            .Where(link => link.MenuItemId == item.Id)
            .OrderBy(link => link.RoleId)
            .Select(link => link.RoleId)
            .ToArrayAsync();
        Assert.Equal(rolesBefore, rolesAfter);
        Assert.Equal(grantsBefore, await RoleGrantsAsync(context));
    }

    [Fact]
    public async Task UploadMenuDirectBoundary_RequiresLegacyRoleAndEitherUploadOrManage_NotReadOnly()
    {
        var fine = new Permission { Name = FineGrainedPermissions.BulkIngestion.Upload };
        var manage = new Permission { Name = "CanManageAch" };
        var admin = new Role { Name = "Admin" };
        var item = new MenuItem
        {
            Id = 7101,
            Label = "Cargar transacciones desde archivo",
            Route = UploadRoute,
            Order = 30,
            Exact = true,
            IsActive = true
        };
        item.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = item.Id, Permission = fine });
        item.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = item.Id, Permission = manage });
        item.MenuItemRoles.Add(new MenuItemRole { MenuItemId = item.Id, Role = admin });
        var repository = new StaticMenuRepository([item]);

        Assert.Contains(await MenuFor(repository, [new Claim(ClaimTypes.Role, "Admin"), new Claim("permission", FineGrainedPermissions.BulkIngestion.Upload)]), x => x.Route == UploadRoute);
        Assert.Contains(await MenuFor(repository, [new Claim(ClaimTypes.Role, "Admin"), new Claim("permission", "CanManageAch")]), x => x.Route == UploadRoute);
        Assert.DoesNotContain(await MenuFor(repository, [new Claim(ClaimTypes.Role, "Admin"), new Claim("permission", "CanReadAch")]), x => x.Route == UploadRoute);
        Assert.DoesNotContain(await MenuFor(repository, [new Claim("permission", FineGrainedPermissions.BulkIngestion.Upload)]), x => x.Route == UploadRoute);
    }

    [Fact]
    public async Task SpaUploadRouteAndAction_UseUploadOrManage_FailClosed_AndSharedParentRemainsLegacyPrerequisite()
    {
        var root = RepositoryRoot();
        var routing = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts");
        var appRouting = await Read(root, "web/ach-interbank-ui/src/app/app-routing.module.ts");
        var component = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-upload/bulk-ingestion-upload.component.ts");
        var template = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-upload/bulk-ingestion-upload.component.html");

        var uploadRoute = Slice(routing, "path: 'bulk-ingestion/upload'", "path: 'bulk-ingestion/tracking'");
        Assert.Contains("canActivate: [permissionGuard]", uploadRoute);
        Assert.Contains("permissions: ['BulkIngestion.Upload', 'CanManageAch']", uploadRoute);
        Assert.DoesNotContain("CanReadAch", uploadRoute);
        Assert.DoesNotContain("BulkIngestion.Read", uploadRoute);
        Assert.DoesNotContain("roles:", uploadRoute);

        Assert.Contains("BULK_INGESTION_UPLOAD_PERMISSIONS = ['BulkIngestion.Upload', 'CanManageAch']", component);
        Assert.Contains("get canUploadBatch(): boolean", component);
        Assert.Contains("if (!this.ensureCanUpload())", component);
        Assert.Contains("No tiene permisos para cargar transacciones desde archivo.", component);
        Assert.Contains("*ngIf=\"canUploadBatch\"", template);
        Assert.Contains("*ngIf=\"!canUploadBatch\"", template);
        Assert.DoesNotContain("Admin", component);
        Assert.DoesNotContain("ACH.Operator", component);

        var parent = Slice(appRouting, "path: 'transactions'", "path: 'integraciones'");
        Assert.Contains("roles: ['Admin', 'ACH.Operator']", parent);
        Assert.Contains("permissions: ['CanManageAch', 'CanReadAch']", parent);
    }

    [Fact]
    public async Task UploadApi_AlreadyUsesFineOrLegacyManagement_AndRejectsReadOnlyCapabilities()
    {
        AssertActionPolicy(nameof(BulkIngestionController.Upload), typeof(HttpPostAttribute), P1Policies.BulkIngestionUpload);

        using var provider = BuildExternalProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        Assert.True((await authorization.AuthorizeAsync(Principal(FineGrainedPermissions.BulkIngestion.Upload), null, P1Policies.BulkIngestionUpload)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(Principal("CanManageAch"), null, P1Policies.BulkIngestionUpload)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal("CanReadAch"), null, P1Policies.BulkIngestionUpload)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal(FineGrainedPermissions.BulkIngestion.Read), null, P1Policies.BulkIngestionUpload)).Succeeded);
    }

    [Fact]
    public async Task UploadSurface_HasOnlyLocalPreparationAndOneMutablePost_ThenPersistsAndQueuesAsyncProcessing()
    {
        var root = RepositoryRoot();
        var component = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-upload/bulk-ingestion-upload.component.ts");
        var spaService = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/services/bulk-ingestion-tracking-api.service.ts");
        var controller = await Read(root, "src/Cfa.ACHInterbank.Api/Controllers/BulkIngestionController.cs");
        var ingestion = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchBulkFileIngestionService.cs");
        var dispatcher = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/BulkIngestionWorkDispatcher.cs");
        var scheduler = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchBulkJobScheduler.cs");
        var job = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/ProcessBulkIngestionBatchJob.cs");
        var processor = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchBulkBatchProcessingService.cs");
        var bulkTransactions = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchBulkTransactionService.cs");

        Assert.Contains("acceptedFormats = '.json,.csv,.xlsx,.xls'", component);
        Assert.Contains("const maxSizeBytes = 20 * 1024 * 1024", component);
        Assert.Contains("this.api.upload(file", component);
        Assert.DoesNotContain("this.api.getBatch(", component);
        Assert.DoesNotContain("this.api.retry(", component);
        Assert.DoesNotContain("this.api.cancel(", component);
        Assert.Contains("this.api.post<BulkFileUploadResponse>('transactions/bulk-ingestion/upload'", spaService);

        Assert.Contains("[HttpPost(\"upload\")]", controller);
        Assert.Contains("[Authorize(Policy = P1Policies.BulkIngestionUpload)]", controller);
        Assert.Contains("parser.ParseAsync", ingestion);
        Assert.Contains("_structuralValidator.Validate", ingestion);
        Assert.Contains("_context.BulkIngestionBatches.Add(batch)", ingestion);
        Assert.Contains("batch.Items.Add(new BulkIngestionItem", ingestion);
        Assert.Contains("new BulkIngestionAttempt", ingestion);
        Assert.Contains("DispatchProcessingAsync(batch.Id, attempt.Id, ct)", ingestion);

        Assert.Contains("_jobScheduler.EnqueueBatchAsync", dispatcher);
        Assert.Contains("JobBuilder.Create<ProcessBulkIngestionBatchJob>()", scheduler);
        Assert.Contains("_processingService.ProcessBatchAsync", job);
        Assert.Contains("_bulkTransactionService.RegisterBulkAsync", processor);
        Assert.Contains("_transactionPersister.PersistAsync", bulkTransactions);
        Assert.Contains("_contrapartidaDispatchPersistenceService.EnsurePendingDispatchAsync", bulkTransactions);
        Assert.Contains("_cenitCycleQueueService.EnqueueAsync", bulkTransactions);

        foreach (var source in new[] { controller, ingestion, dispatcher, scheduler, job, processor, bulkTransactions })
        {
            Assert.DoesNotContain("IWscfaachSoapClient", source);
            Assert.DoesNotContain("SftpClient", source);
            Assert.DoesNotContain("HttpClient", source);
        }
    }

    [Fact]
    public async Task TrackingAndNachaUploadSecurityModels_RemainIntact()
    {
        var root = RepositoryRoot();
        var routing = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts");
        var tracking = Slice(routing, "path: 'bulk-ingestion/tracking'", "path: 'bulk-ingestion/:batchId'");
        var detail = Slice(routing, "path: 'bulk-ingestion/:batchId'", "path: 'nacha-upload'");
        var nacha = Slice(routing, "path: 'nacha-upload'", "path: 'cycle-configs'");
        var trackingComponent = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-tracking/bulk-ingestion-tracking.component.ts");
        var detailComponent = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.ts");

        Assert.Contains("permissions: ['BulkIngestion.Read', 'CanReadAch']", tracking);
        Assert.Contains("permissions: ['BulkIngestion.Read', 'CanReadAch']", detail);
        Assert.Contains("BULK_INGESTION_RETRY_PERMISSIONS = ['BulkIngestion.Retry', 'CanManageAch']", trackingComponent);
        Assert.Contains("BULK_INGESTION_RETRY_PERMISSIONS = ['BulkIngestion.Retry', 'CanManageAch']", detailComponent);
        Assert.Contains("BULK_INGESTION_CANCEL_PERMISSIONS = ['BulkIngestion.Cancel', 'CanManageAch']", detailComponent);
        Assert.Contains("permissions: ['Nacha.Upload', 'CanManageAch']", nacha);
    }

    private static void AssertActionPolicy(string methodName, Type httpMethodAttribute, string expectedPolicy)
    {
        var action = typeof(BulkIngestionController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(action);
        Assert.NotNull(action!.GetCustomAttribute(httpMethodAttribute));
        var authorize = action.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(expectedPolicy, authorize!.Policy);
    }

    private static async Task<IList<MenuItemDto>> MenuFor(StaticMenuRepository repository, IEnumerable<Claim> claims)
        => await new GetMenuForCurrentUserHandler(repository, new TestCurrentUserContext(claims))
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
        => new(new ClaimsIdentity(permissions.Select(permission => new Claim("permission", permission)), "test"));

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

    private static async Task<string[]> RoleGrantsAsync(AchDbContext context)
        => await context.RolePermissions.AsNoTracking()
            .OrderBy(link => link.RoleId)
            .ThenBy(link => link.PermissionId)
            .Select(link => $"{link.RoleId:D}|{link.PermissionId:D}")
            .ToArrayAsync();

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
