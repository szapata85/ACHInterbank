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

public sealed class NavigationMenuNav34DReturnsAuthorizationTests
{
    [Fact]
    public async Task ReturnsMenu_UsesReadCapability_AndPreservesIdentityAndOtherOperationItems()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;

        var before = await context.MenuItems.AsNoTracking()
            .SingleAsync(item => item.Id == MenuItemConfiguration.TransactionsReturnsId);
        var createLinksBefore = await PermissionLinks(context, MenuItemConfiguration.TransactionsCreateId);
        var uploadLinksBefore = await PermissionLinks(context, MenuItemConfiguration.NachaUploadId);
        var rolesBefore = await context.MenuItemRoles.AsNoTracking()
            .Where(link => link.MenuItemId == MenuItemConfiguration.TransactionsReturnsId)
            .OrderBy(link => link.RoleId)
            .Select(link => link.RoleId)
            .ToArrayAsync();

        var permissionSeeder = new ReturnsPermissionSeeder(context);
        var authorizationSeeder = new ReturnsMenuAuthorizationSeeder(context);
        Assert.True(permissionSeeder.Order < authorizationSeeder.Order);
        Assert.True(NachaUploadMenuAuthorizationSeeder.AuthorizationReconciliationOrder < authorizationSeeder.Order);

        await permissionSeeder.SeedAsync();
        await authorizationSeeder.SeedAsync();
        await permissionSeeder.SeedAsync();
        await authorizationSeeder.SeedAsync();
        context.ChangeTracker.Clear();

        var after = await context.MenuItems.AsNoTracking()
            .SingleAsync(item => item.Id == MenuItemConfiguration.TransactionsReturnsId);
        Assert.Equal(before.Label, after.Label);
        Assert.Equal(before.Route, after.Route);
        Assert.Equal(before.ParentId, after.ParentId);
        Assert.Equal(before.Order, after.Order);
        Assert.Equal(before.Exact, after.Exact);
        Assert.Equal(before.IsActive, after.IsActive);
        Assert.Equal("/transactions/returns", after.Route);

        var permissions = await PermissionLinks(context, MenuItemConfiguration.TransactionsReturnsId);
        Assert.Single(permissions);
        Assert.Contains(PermissionConfiguration.ReadAchPermissionId, permissions);
        Assert.DoesNotContain(PermissionConfiguration.ManageAchPermissionId, permissions);
        Assert.DoesNotContain(ReturnsPermissionSeeder.GenerateFilePermissionId, permissions);

        var rolesAfter = await context.MenuItemRoles.AsNoTracking()
            .Where(link => link.MenuItemId == MenuItemConfiguration.TransactionsReturnsId)
            .OrderBy(link => link.RoleId)
            .Select(link => link.RoleId)
            .ToArrayAsync();
        Assert.Equal(rolesBefore, rolesAfter);

        Assert.Equal(createLinksBefore, await PermissionLinks(context, MenuItemConfiguration.TransactionsCreateId));
        Assert.Equal(uploadLinksBefore, await PermissionLinks(context, MenuItemConfiguration.NachaUploadId));
    }

    [Fact]
    public async Task ReturnsMenuHandler_RequiresAggregateScreenReadCapability_AndManageOnlyDoesNotReplaceRead()
    {
        var legacyRead = new Permission { Name = "CanReadAch" };
        var item = new MenuItem
        {
            Id = MenuItemConfiguration.TransactionsReturnsId,
            Label = "Devoluciones",
            Route = "/transactions/returns",
            Order = 60,
            Exact = true,
            IsActive = true
        };
        item.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = item.Id, Permission = legacyRead });

        var repository = new StaticMenuRepository([item]);

        var fineOnly = await MenuFor(repository, FineGrainedPermissions.Returns.Read);
        Assert.DoesNotContain(fineOnly, menu => menu.Route == "/transactions/returns");

        var read = await MenuFor(repository, "CanReadAch");
        Assert.Contains(read, menu => menu.Route == "/transactions/returns");

        var manageOnly = await MenuFor(repository, "CanManageAch");
        Assert.DoesNotContain(manageOnly, menu => menu.Route == "/transactions/returns");

        var none = await MenuFor(repository);
        Assert.DoesNotContain(none, menu => menu.Route == "/transactions/returns");
    }

    [Fact]
    public async Task ReturnsGeneratePermission_IsExistingFineGrainedContract_AndSeederAddsNoRoleGrant()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;

        Assert.Contains(FineGrainedPermissions.Returns.Read, FineGrainedPermissions.AllPermissions);
        Assert.Contains(FineGrainedPermissions.Returns.GenerateFile, FineGrainedPermissions.AllPermissions);

        var seeder = new ReturnsPermissionSeeder(context);
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var generate = await context.Permissions.AsNoTracking()
            .SingleAsync(item => item.Id == ReturnsPermissionSeeder.GenerateFilePermissionId);
        Assert.Equal(FineGrainedPermissions.Returns.GenerateFile, generate.Name);

        var grants = await context.RolePermissions.AsNoTracking()
            .Where(link => link.PermissionId == ReturnsPermissionSeeder.GenerateFilePermissionId)
            .ToArrayAsync();
        Assert.Empty(grants);
    }

    [Fact]
    public async Task ReturnsSpa_IsMixedReadAndMutation_UsingBackendEquivalentCapabilities()
    {
        var root = RepositoryRoot();
        var routing = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts");
        var component = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/components/ach-returns-management/ach-returns-management.component.ts");
        var template = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/components/ach-returns-management/ach-returns-management.component.html");
        var appRouting = await Read(root, "web/ach-interbank-ui/src/app/app-routing.module.ts");

        var returnsRoute = Slice(routing, "path: 'returns'", "path: 'returns-ror'");
        Assert.Contains("permissions: ['CanReadAch']", returnsRoute);
        Assert.DoesNotContain("permissions: ['CanManageAch']", returnsRoute);
        Assert.DoesNotContain("roles:", returnsRoute);

        Assert.Contains("RETURNS_GENERATE_PERMISSIONS = ['Returns.GenerateFile', 'CanManageAch']", component);
        Assert.Contains("get canGenerateReturns(): boolean", component);
        Assert.Contains("if (!this.ensureCanGenerateReturns())", component);
        Assert.Contains("No tiene permisos para generar devoluciones.", component);
        Assert.Contains("*ngIf=\"canGenerateReturns\"", template);

        var parentRoute = Slice(appRouting, "path: 'transactions'", "path: 'integraciones'");
        Assert.Contains("roles: ['Admin', 'ACH.Operator']", parentRoute);
        Assert.Contains("permissions: ['CanManageAch', 'CanReadAch']", parentRoute);
    }

    [Fact]
    public async Task ReturnsApi_SeparatesReadFromGenerate_AndReadClaimCannotGenerate()
    {
        AssertActionPolicy(nameof(AchReturnsController.GetTransactionsByCycle), P0Policies.ReturnsRead);
        AssertActionPolicy(nameof(AchReturnsController.GenerateFile), P0Policies.ReturnsGenerateFile);
        AssertActionPolicy(nameof(AchCyclesController.Get), "CanReadAch");
        AssertActionPolicy(nameof(ReturnReasonsController.GetAll), "CanReadAch");

        using var provider = BuildExternalProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        Assert.True((await authorization.AuthorizeAsync(
            Principal(FineGrainedPermissions.Returns.Read), null, P0Policies.ReturnsRead)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(
            Principal("CanReadAch"), null, P0Policies.ReturnsRead)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            Principal("CanManageAch"), null, P0Policies.ReturnsRead)).Succeeded);

        Assert.True((await authorization.AuthorizeAsync(
            Principal(FineGrainedPermissions.Returns.GenerateFile), null, P0Policies.ReturnsGenerateFile)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(
            Principal("CanManageAch"), null, P0Policies.ReturnsGenerateFile)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            Principal("CanReadAch"), null, P0Policies.ReturnsGenerateFile)).Succeeded);
    }

    [Fact]
    public async Task ReturnsFlow_ReadsCatalogs_ThenPersistsReturnArtifactAndState_WithoutDirectTransmission()
    {
        var root = RepositoryRoot();
        var component = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/components/ach-returns-management/ach-returns-management.component.ts");
        var spaService = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/services/ach-returns-api.service.ts");
        var reasonService = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/services/return-reasons-api.service.ts");
        var controller = await Read(root, "src/Cfa.ACHInterbank.Api/Controllers/AchReturnsController.cs");
        var returnsService = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnsService.cs");
        var artifact = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnArtifactGenerationComponent.cs");
        var transitions = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnStateTransitionComponent.cs");
        var audit = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnAuditMonitoringComponent.cs");

        Assert.Contains("this.cyclesApi.search({ page: 1, pageSize: 100 })", component);
        Assert.Contains("this.returnReasonsApi.getForReturns()", component);
        Assert.Contains("this.returnsApi.getTransactionsByCycle(cycleId)", component);
        Assert.Contains("this.returnsApi.generateFile({ cycleId, items: selectedItems })", component);
        Assert.Contains("ach-returns/cycles/${encodeURIComponent(cycleId)}/transactions", spaService);
        Assert.Contains("postBlob('ach-returns/generate-file'", spaService);
        Assert.Contains("return-reasons", reasonService);

        Assert.Contains("P0Policies.ReturnsRead", controller);
        Assert.Contains("P0Policies.ReturnsGenerateFile", controller);
        Assert.Contains("_context.Set<AchReturnGenerated>().AddRange", returnsService);
        Assert.Contains("_artifactGeneration.GenerateAsync", returnsService);
        Assert.Contains("_stateTransitions.ApplyAsync", returnsService);
        Assert.Contains("BuildReturnOutAsync", artifact);
        Assert.Contains("PersistAudit = true", artifact);
        Assert.Contains("AchTransferStateEnum.ReturnedByEpr", transitions);
        Assert.Contains("transmissionStatus = \"GeneratedNotTransmitted\"", audit);
        Assert.Contains("productiveStatus = \"TechnicalGeneratedOnly\"", audit);

        Assert.DoesNotContain("IAchOutboundReturnDispatchService", controller);
        Assert.DoesNotContain("IAchOutboundReturnDispatchService", returnsService);
        Assert.DoesNotContain("IContrapartidaDispatchJobService", returnsService);
        Assert.DoesNotContain("IWscfaachSoapClient", returnsService);
    }

    [Fact]
    public async Task ReturnOfReturn_SharedServiceAndControllerPoliciesRemainStableAfterLaterAuthorizationNormalization()
    {
        var root = RepositoryRoot();
        var routing = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts");
        var rorController = await Read(root, "src/Cfa.ACHInterbank.Api/Controllers/AchReturnOfReturnController.cs");
        var spaService = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/services/ach-returns-api.service.ts");

        // NAV3.4K owns the returns-ror child-route authorization. NAV3.4D keeps
        // regression coverage for the shared service and backend policy contracts.
        Assert.Contains("path: 'returns-ror'", routing);
        Assert.Contains("evaluateReturnOfReturn", spaService);
        Assert.Contains("generateReturnOfReturnAuditFile", spaService);
        Assert.Contains("generateReturnOfReturnNachaFile", spaService);
        Assert.Contains("P0Policies.ReturnsGenerateFile", rorController);
        Assert.Contains("P0Policies.ReturnsRead", rorController);
    }

    [Fact]
    public async Task Nav34D_DoesNotRegressPreviouslyClosedNavigationSecurity()
    {
        var root = RepositoryRoot();
        var create = await Read(root, "src/Cfa.ACHInterbank.Persistence/Security/Seeders/TransactionsCreateMenuAuthorizationSeeder.cs");
        var upload = await Read(root, "src/Cfa.ACHInterbank.Persistence/Security/Seeders/NachaUploadMenuAuthorizationSeeder.cs");
        var manualReview = await Read(root, "web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-manual-review-page.component.ts");
        var testTools = await Read(root, "src/Cfa.ACHInterbank.Persistence/Security/Seeders/TestToolsMenuAuthorizationSeeder.cs");

        Assert.Contains("TransactionsCreatePermissionSeeder.PermissionId", create);
        Assert.DoesNotContain("PermissionConfiguration.ReadAchPermissionId", create);
        Assert.Contains("NachaUploadPermissionSeeder.PermissionId", upload);
        Assert.DoesNotContain("PermissionConfiguration.ReadAchPermissionId", upload);
        Assert.Contains("Nacha.Generate", manualReview);
        Assert.Contains("CanManageAch", manualReview);
        Assert.Contains("FineGrainedPermissions.TestTools.Access", testTools);
        Assert.Contains("serviceTests.IsActive = true;", testTools);
    }

    private static async Task<Guid[]> PermissionLinks(AchDbContext context, int menuItemId)
        => await context.MenuItemPermissions.AsNoTracking()
            .Where(link => link.MenuItemId == menuItemId)
            .OrderBy(link => link.PermissionId)
            .Select(link => link.PermissionId)
            .ToArrayAsync();

    private static async Task<IList<MenuItemDto>> MenuFor(StaticMenuRepository repository, params string[] permissions)
        => await new GetMenuForCurrentUserHandler(
                repository,
                new TestCurrentUserContext(permissions.Select(permission => new Claim("permission", permission))))
            .Handle(new GetMenuForCurrentUserQuery(), CancellationToken.None);

    private static void AssertActionPolicy(string methodName, string expectedPolicy)
    {
        var action = typeof(AchReturnsController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
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

    private static string SliceToEnd(string source, string startMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Start marker not found: {startMarker}");
        return source[start..];
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
