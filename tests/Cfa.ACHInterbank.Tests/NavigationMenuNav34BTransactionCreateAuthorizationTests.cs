using System.Reflection;
using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Application.DataBase.Queries.Navigation;
using Cfa.ACHInterbank.Application.Navigation;
using Cfa.ACHInterbank.Application.Navigation.Queries;
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

public sealed class NavigationMenuNav34BTransactionCreateAuthorizationTests
{
    [Fact]
    public async Task CreateTransactionMenu_UsesCreationCapabilityWithoutReadFallback_AndPreservesIdentity()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;

        var before = await context.MenuItems.AsNoTracking()
            .SingleAsync(item => item.Id == MenuItemConfiguration.TransactionsCreateId);
        var listLinksBefore = await context.MenuItemPermissions.AsNoTracking()
            .Where(link => link.MenuItemId == MenuItemConfiguration.TransactionsListId)
            .OrderBy(link => link.PermissionId)
            .Select(link => link.PermissionId)
            .ToArrayAsync();
        var rolesBefore = await context.MenuItemRoles.AsNoTracking()
            .Where(link => link.MenuItemId == MenuItemConfiguration.TransactionsCreateId)
            .OrderBy(link => link.RoleId)
            .Select(link => link.RoleId)
            .ToArrayAsync();

        var permissionSeeder = new TransactionsCreatePermissionSeeder(context);
        var authorizationSeeder = new TransactionsCreateMenuAuthorizationSeeder(context);
        Assert.True(permissionSeeder.Order < authorizationSeeder.Order);

        await permissionSeeder.SeedAsync();
        await authorizationSeeder.SeedAsync();
        await permissionSeeder.SeedAsync();
        await authorizationSeeder.SeedAsync();
        context.ChangeTracker.Clear();

        var after = await context.MenuItems.AsNoTracking()
            .SingleAsync(item => item.Id == MenuItemConfiguration.TransactionsCreateId);
        Assert.Equal(before.Label, after.Label);
        Assert.Equal(before.Route, after.Route);
        Assert.Equal(before.ParentId, after.ParentId);
        Assert.Equal(before.Order, after.Order);
        Assert.Equal(before.Exact, after.Exact);
        Assert.Equal(before.IsActive, after.IsActive);
        Assert.Equal("Crear transacción", after.Label);
        Assert.Equal("/transactions/create", after.Route);

        var permissions = await context.MenuItemPermissions.AsNoTracking()
            .Where(link => link.MenuItemId == MenuItemConfiguration.TransactionsCreateId)
            .OrderBy(link => link.PermissionId)
            .Select(link => link.PermissionId)
            .ToArrayAsync();

        Assert.Equal(2, permissions.Length);
        Assert.Contains(PermissionConfiguration.ManageAchPermissionId, permissions);
        Assert.Contains(TransactionsCreatePermissionSeeder.PermissionId, permissions);
        Assert.DoesNotContain(PermissionConfiguration.ReadAchPermissionId, permissions);

        var rolesAfter = await context.MenuItemRoles.AsNoTracking()
            .Where(link => link.MenuItemId == MenuItemConfiguration.TransactionsCreateId)
            .OrderBy(link => link.RoleId)
            .Select(link => link.RoleId)
            .ToArrayAsync();
        Assert.Equal(rolesBefore, rolesAfter);

        var listLinksAfter = await context.MenuItemPermissions.AsNoTracking()
            .Where(link => link.MenuItemId == MenuItemConfiguration.TransactionsListId)
            .OrderBy(link => link.PermissionId)
            .Select(link => link.PermissionId)
            .ToArrayAsync();
        Assert.Equal(listLinksBefore, listLinksAfter);
        Assert.Contains(PermissionConfiguration.ReadAchPermissionId, listLinksAfter);
    }


    [Fact]
    public async Task CreateTransactionMenuHandler_ReadOnlyDoesNotExposeCreate_ButCreationAlternativesDo()
    {
        var admin = new Role { Name = "Admin" };
        var fine = new Permission { Name = FineGrainedPermissions.Transactions.Create };
        var manage = new Permission { Name = "CanManageAch" };
        var item = new MenuItem
        {
            Id = MenuItemConfiguration.TransactionsCreateId,
            Label = "Crear transacción",
            Route = "/transactions/create",
            Order = 20,
            Exact = true,
            IsActive = true
        };
        item.MenuItemRoles.Add(new MenuItemRole { MenuItemId = item.Id, Role = admin });
        item.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = item.Id, Permission = fine });
        item.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = item.Id, Permission = manage });

        var repository = new StaticMenuRepository([item]);

        var readOnly = await new GetMenuForCurrentUserHandler(
                repository,
                new TestCurrentUserContext([
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim("permission", "CanReadAch")
                ]))
            .Handle(new GetMenuForCurrentUserQuery(), CancellationToken.None);
        Assert.DoesNotContain(readOnly, menu => menu.Route == "/transactions/create");

        var fineGrained = await new GetMenuForCurrentUserHandler(
                repository,
                new TestCurrentUserContext([
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim("permission", FineGrainedPermissions.Transactions.Create)
                ]))
            .Handle(new GetMenuForCurrentUserQuery(), CancellationToken.None);
        Assert.Contains(fineGrained, menu => menu.Route == "/transactions/create");

        var legacyManage = await new GetMenuForCurrentUserHandler(
                repository,
                new TestCurrentUserContext([
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim("permission", "CanManageAch")
                ]))
            .Handle(new GetMenuForCurrentUserQuery(), CancellationToken.None);
        Assert.Contains(legacyManage, menu => menu.Route == "/transactions/create");
    }

    [Fact]
    public async Task TransactionsCreatePermission_IsExistingFineGrainedContract_AndSeederAddsNoRoleGrant()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;

        Assert.Contains(FineGrainedPermissions.Transactions.Create, FineGrainedPermissions.AllPermissions);

        var seeder = new TransactionsCreatePermissionSeeder(context);
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var permission = await context.Permissions.AsNoTracking()
            .SingleAsync(item => item.Id == TransactionsCreatePermissionSeeder.PermissionId);
        Assert.Equal(FineGrainedPermissions.Transactions.Create, permission.Name);

        var grants = await context.RolePermissions.AsNoTracking()
            .Where(link => link.PermissionId == TransactionsCreatePermissionSeeder.PermissionId)
            .ToArrayAsync();
        Assert.Empty(grants);
    }

    [Fact]
    public async Task CreateTransactionSpa_UsesSameCreationAlternatives_WhileListRemainsReadOnly()
    {
        var root = RepositoryRoot();
        var routing = await File.ReadAllTextAsync(Path.Combine(
            root,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "transactions",
            "transactions-routing.module.ts"));

        var listRoute = Slice(routing, "path: 'list'", "path: 'create'");
        Assert.Contains("permissions: ['CanReadAch']", listRoute);

        var createRoute = Slice(routing, "path: 'create'", "path: 'bulk-create'");
        Assert.Contains("canActivate: [permissionGuard]", createRoute);
        Assert.Contains("permissions: ['Transactions.Create', 'CanManageAch']", createRoute);
        Assert.DoesNotContain("CanReadAch", createRoute);
        Assert.DoesNotContain("roles:", createRoute);
    }

    [Fact]
    public async Task CreateTransactionApi_AcceptsSpecificOrLegacyManagement_ButRejectsReadOnly()
    {
        var action = typeof(TransactionsController).GetMethod(nameof(TransactionsController.CreateTransaction));
        Assert.NotNull(action);
        var authorize = action!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(P0Policies.TransactionsCreate, authorize!.Policy);

        using var provider = BuildExternalProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        Assert.True((await authorization.AuthorizeAsync(
            Principal(FineGrainedPermissions.Transactions.Create), null, P0Policies.TransactionsCreate)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(
            Principal("CanManageAch"), null, P0Policies.TransactionsCreate)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            Principal("CanReadAch"), null, P0Policies.TransactionsCreate)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            new ClaimsPrincipal(new ClaimsIdentity()), null, P0Policies.TransactionsCreate)).Succeeded);
    }

    [Fact]
    public void CreateTransactionAuxiliaryEndpoints_RemainReadOrPreviewPolicies()
    {
        AssertActionPolicy(typeof(TransactionsController), nameof(TransactionsController.GetCompanyEntryDescriptions), P0Policies.TransactionsRead);
        AssertActionPolicy(typeof(TransactionsController), nameof(TransactionsController.GetThirdPartyAccountSelector), P0Policies.TransactionsRead);
        AssertActionPolicy(typeof(TransactionsController), nameof(TransactionsController.PreviewPolicy), P0Policies.TransactionsPolicyPreview);
        AssertActionPolicy(typeof(CustomersController), nameof(CustomersController.GetAll), "CanReadAch");
        AssertActionPolicy(typeof(FinancialInstitutionsController), nameof(FinancialInstitutionsController.GetAll), "CanReadAch");
    }

    [Fact]
    public async Task CreateTransactionFlow_PersistsAndPreparesInternalWork_WithoutDirectExternalDispatchOrFileGeneration()
    {
        var root = RepositoryRoot();
        var component = await File.ReadAllTextAsync(Path.Combine(
            root,
            "web",
            "ach-interbank-ui",
            "src",
            "app",
            "features",
            "transactions",
            "components",
            "transaction-create",
            "transaction-create.component.ts"));
        var transactionService = await File.ReadAllTextAsync(Path.Combine(
            root,
            "src",
            "Cfa.ACHInterbank.Persistence",
            "ACH",
            "Services",
            "Implementation",
            "AchTransactionService.cs"));
        var dispatchPersistence = await File.ReadAllTextAsync(Path.Combine(
            root,
            "src",
            "Cfa.ACHInterbank.Persistence",
            "ACH",
            "Services",
            "Implementation",
            "ContrapartidaDispatchPersistenceService.cs"));

        Assert.Contains("this.api.getCompanyEntryDescriptions()", component);
        Assert.Contains(".getContextualThirdPartyAccounts(", component);
        Assert.Contains("this.api.previewPolicy(", component);
        Assert.Contains(".createTransaction(sanitized)", component);
        Assert.DoesNotContain("createBulkTransaction(", component);

        Assert.Contains("_transactionPersister.PersistAsync", transactionService);
        Assert.Contains("_cenitCycleQueueService.EnqueueAsync", transactionService);
        Assert.Contains("_contrapartidaDispatchPersistenceService.EnsurePendingDispatchAsync", transactionService);
        Assert.Contains("_unitOfWork.CommitAsync", transactionService);
        Assert.DoesNotContain("IContrapartidaDispatchJobService", transactionService);
        Assert.DoesNotContain("ProcessTransactionAsync", transactionService);
        Assert.DoesNotContain("Soap", transactionService, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GenerateFile", transactionService);

        Assert.Contains("State = ContrapartidaDispatchItemStateEnum.PendingContrapartidaReport", dispatchPersistence);
        Assert.DoesNotContain("IContrapartidaDispatchJobService", dispatchPersistence);
        Assert.DoesNotContain("ProcessTransactionAsync", dispatchPersistence);
    }

    private static void AssertActionPolicy(Type controllerType, string methodName, string expectedPolicy)
    {
        var action = controllerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
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
