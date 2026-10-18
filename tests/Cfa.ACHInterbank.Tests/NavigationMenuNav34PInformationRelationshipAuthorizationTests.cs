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
using Cfa.ACHInterbank.Persistence.Security.Seeders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav34PInformationRelationshipAuthorizationTests
{
    private const string Route = "/integraciones/relacion-informacion";

    [Fact]
    public async Task MappingCapabilities_ArePreExistingDistinctCapabilities_AndSeederPersistsThemIdempotentlyWithoutGrants()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;
        var grantsBefore = await RoleGrantsAsync(context);

        var expected = new[]
        {
            FineGrainedPermissions.Integrations.ManageMappings,
            FineGrainedPermissions.Integrations.Validate,
            FineGrainedPermissions.Integrations.Publish
        };
        foreach (var permission in expected)
            Assert.Contains(permission, FineGrainedPermissions.AllPermissions);
        Assert.Equal(3, expected.Distinct(StringComparer.Ordinal).Count());

        var seeder = new IntegrationMappingPermissionSeeder(context);
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var persisted = await context.Permissions.AsNoTracking()
            .Where(item => expected.Contains(item.Name))
            .OrderBy(item => item.Name)
            .ToArrayAsync();
        Assert.Equal(3, persisted.Length);
        Assert.Contains(persisted, item => item.Id == IntegrationMappingPermissionSeeder.ManageMappingsPermissionId && item.Name == FineGrainedPermissions.Integrations.ManageMappings);
        Assert.Contains(persisted, item => item.Id == IntegrationMappingPermissionSeeder.ValidatePermissionId && item.Name == FineGrainedPermissions.Integrations.Validate);
        Assert.Contains(persisted, item => item.Id == IntegrationMappingPermissionSeeder.PublishPermissionId && item.Name == FineGrainedPermissions.Integrations.Publish);
        Assert.Empty(await context.RolePermissions.AsNoTracking().Where(link => persisted.Select(item => item.Id).Contains(link.PermissionId)).ToArrayAsync());
        Assert.Equal(grantsBefore, await RoleGrantsAsync(context));
    }

    [Fact]
    public async Task InformationRelationshipMenu_UsesCanReadAch_PreservesCanonicalStructureAndAdminAudience_AndAddsNoGrants()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;
        var item = new MenuItem
        {
            Id = 7302,
            MenuId = 1,
            ParentId = MenuItemConfiguration.IntegrationsId,
            Label = "Relación de información",
            Route = Route,
            Icon = "account_tree",
            Order = 20,
            Exact = true,
            IsActive = true
        };
        context.MenuItems.Add(item);
        context.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = item.Id, PermissionId = PermissionConfiguration.ManageUsersPermissionId });
        context.MenuItemRoles.Add(new MenuItemRole { MenuItemId = item.Id, RoleId = RoleConfiguration.AdminRoleId });
        await context.SaveChangesAsync();

        var grantsBefore = await RoleGrantsAsync(context);
        var seeder = new InformationRelationshipMenuAuthorizationSeeder(context);
        Assert.True(IntegrationContractMenuAuthorizationSeeder.AuthorizationReconciliationOrder < seeder.Order);
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var after = await context.MenuItems.AsNoTracking().SingleAsync(value => value.Id == item.Id);
        Assert.Equal("Relación de información", after.Label);
        Assert.Equal(Route, after.Route);
        Assert.Equal(MenuItemConfiguration.IntegrationsId, after.ParentId);
        Assert.Equal(20, after.Order);
        Assert.True(after.IsActive);
        Assert.True(after.Exact);

        var permissions = await context.MenuItemPermissions.AsNoTracking().Where(link => link.MenuItemId == item.Id).Select(link => link.PermissionId).ToArrayAsync();
        Assert.Equal(new[] { PermissionConfiguration.ReadAchPermissionId }, permissions);
        var roles = await context.MenuItemRoles.AsNoTracking().Where(link => link.MenuItemId == item.Id).Select(link => link.RoleId).ToArrayAsync();
        Assert.Equal(new[] { RoleConfiguration.AdminRoleId }, roles);
        Assert.Equal(grantsBefore, await RoleGrantsAsync(context));
    }

    [Fact]
    public async Task InformationRelationshipMenu_DirectBoundary_RemainsAdminAudienceButNoLongerDependsOnCanManageUsers()
    {
        var item = new MenuItem { Id = 7302, Label = "Relación de información", Route = Route, Order = 20, Exact = true, IsActive = true };
        item.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = item.Id, Permission = new Permission { Name = "CanReadAch" } });
        item.MenuItemRoles.Add(new MenuItemRole { MenuItemId = item.Id, Role = new Role { Name = "Admin" } });
        var repository = new StaticMenuRepository([item]);

        Assert.Contains(await MenuFor(repository, [new Claim(ClaimTypes.Role, "Admin"), new Claim("permission", "CanReadAch")]), value => value.Route == Route);
        Assert.DoesNotContain(await MenuFor(repository, [new Claim(ClaimTypes.Role, "Admin"), new Claim("permission", "CanManageUsers")]), value => value.Route == Route);
        Assert.DoesNotContain(await MenuFor(repository, [new Claim(ClaimTypes.Role, "ACH.Operator"), new Claim("permission", "CanReadAch")]), value => value.Route == Route);
        Assert.DoesNotContain(await MenuFor(repository, [new Claim("permission", "CanReadAch")]), value => value.Route == Route);
    }

    [Fact]
    public async Task MappingP1Policies_PreserveLegacyCanManageAchFallback_AndKeepManageValidatePublishSeparate()
    {
        using var provider = BuildExternalProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        await AssertPolicy(authorization, P1Policies.IntegrationMappingsManage, FineGrainedPermissions.Integrations.ManageMappings, FineGrainedPermissions.Integrations.Validate, FineGrainedPermissions.Integrations.Publish);
        await AssertPolicy(authorization, P1Policies.IntegrationMappingsValidate, FineGrainedPermissions.Integrations.Validate, FineGrainedPermissions.Integrations.ManageMappings, FineGrainedPermissions.Integrations.Publish);
        await AssertPolicy(authorization, P1Policies.IntegrationMappingsPublish, FineGrainedPermissions.Integrations.Publish, FineGrainedPermissions.Integrations.ManageMappings, FineGrainedPermissions.Integrations.Validate);
    }

    [Fact]
    public void MappingApi_SeparatesReadManageValidateAndPublish_WhileLeavingUnconsumedCrossContractActionsLegacy()
    {
        AssertActionPolicy(nameof(IntegrationMappingSetsController.Get), typeof(HttpGetAttribute), P1Policies.IntegrationsRead);
        AssertActionPolicy(nameof(IntegrationMappingSetsController.GetPublished), typeof(HttpGetAttribute), P1Policies.IntegrationsRead);
        AssertActionPolicy(nameof(IntegrationMappingSetsController.GetById), typeof(HttpGetAttribute), P1Policies.IntegrationsRead);
        AssertActionPolicy(nameof(IntegrationMappingSetsController.GetWorkspace), typeof(HttpGetAttribute), P1Policies.IntegrationsRead);
        AssertActionPolicy(nameof(IntegrationMappingSetsController.History), typeof(HttpGetAttribute), P1Policies.IntegrationsRead);

        AssertActionPolicy(nameof(IntegrationMappingSetsController.Create), typeof(HttpPostAttribute), P1Policies.IntegrationMappingsManage);
        AssertActionPolicy(nameof(IntegrationMappingSetsController.Update), typeof(HttpPutAttribute), P1Policies.IntegrationMappingsManage);
        AssertActionPolicy(nameof(IntegrationMappingSetsController.UpsertRules), typeof(HttpPutAttribute), P1Policies.IntegrationMappingsManage);
        AssertActionPolicy(nameof(IntegrationMappingSetsController.UpdateParameterMapping), typeof(HttpPutAttribute), P1Policies.IntegrationMappingsManage);
        AssertActionPolicy(nameof(IntegrationMappingSetsController.Clone), typeof(HttpPostAttribute), P1Policies.IntegrationMappingsManage);
        AssertActionPolicy(nameof(IntegrationMappingSetsController.Validate), typeof(HttpPostAttribute), P1Policies.IntegrationMappingsValidate);
        AssertActionPolicy(nameof(IntegrationMappingSetsController.Preview), typeof(HttpPostAttribute), P1Policies.IntegrationMappingsValidate);
        AssertActionPolicy(nameof(IntegrationMappingSetsController.Publish), typeof(HttpPostAttribute), P1Policies.IntegrationMappingsPublish);

        AssertActionPolicy(nameof(IntegrationMappingSetsController.CloneToContractVersion), typeof(HttpPostAttribute), "CanManageAch");
        AssertActionPolicy(nameof(IntegrationMappingSetsController.ValidateContractMapping), typeof(HttpPostAttribute), "CanManageAch");
        AssertActionPolicy(nameof(IntegrationMappingSetsController.Compare), typeof(HttpPostAttribute), "CanManageAch");
        Assert.DoesNotContain(typeof(IntegrationMappingSetsController).GetMethods(BindingFlags.Instance | BindingFlags.Public),
            method => method.GetCustomAttribute<HttpDeleteAttribute>() is not null);
    }

    [Fact]
    public async Task MappingSurface_PersistsRulesValidationAndPublication_ForFutureRuntimeButDoesNotInvokeExternalTransportDirectly()
    {
        var root = RepositoryRoot();
        var setService = await Read(root, "src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingSetService.cs");
        var validation = await Read(root, "src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingValidationService.cs");
        var preview = await Read(root, "src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingPreviewService.cs");
        var resolver = await Read(root, "src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationExecutionContractResolver.cs");
        var entity = await Read(root, "src/Cfa.ACHInterbank.Domain/Entities/Integrations/IntegrationMappingModels.cs");

        Assert.Contains("ValidationSummaryJson = JsonSerializer.Serialize", validation);
        Assert.Contains("SaveChangesAsync", validation);
        Assert.Contains("set.Status = IntegrationMappingSetStatusEnum.Published", setService);
        Assert.Contains("published.IsActive = false", setService);
        Assert.Contains("set.IsActive = true", setService);
        Assert.Contains("IntegrationMappingSetStatusEnum.Published", resolver);
        Assert.Contains("IsActive", resolver);
        Assert.Contains("TransformationCode", entity);
        Assert.Contains("DefaultValue", entity);
        Assert.Contains("Priority", entity);
        Assert.Contains("RequiredOverride", entity);

        Assert.Contains("SensitivePathFragments", preview);
        Assert.Contains("SanitizePreviewValue", preview);
        foreach (var source in new[] { setService, validation, preview })
        {
            Assert.DoesNotContain("HttpClient", source);
            Assert.DoesNotContain("SftpClient", source);
            Assert.DoesNotContain("IWscfaachSoapClient", source);
        }
    }

    private static async Task AssertPolicy(IAuthorizationService authorization, string policy, string dedicated, params string[] otherFine)
    {
        Assert.True((await authorization.AuthorizeAsync(Principal(dedicated), null, policy)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(Principal("CanManageAch"), null, policy)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal("CanReadAch"), null, policy)).Succeeded);
        foreach (var other in otherFine)
            Assert.False((await authorization.AuthorizeAsync(Principal(other), null, policy)).Succeeded);
    }

    private static void AssertActionPolicy(string methodName, Type httpMethodAttribute, string expectedPolicy)
    {
        var action = typeof(IntegrationMappingSetsController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
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
        var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options);
        context.Database.EnsureCreated();
        return new Fixture(context, connection);
    }

    private static async Task<string[]> RoleGrantsAsync(AchDbContext context)
        => await context.RolePermissions.AsNoTracking().OrderBy(link => link.RoleId).ThenBy(link => link.PermissionId)
            .Select(link => $"{link.RoleId:D}|{link.PermissionId:D}").ToArrayAsync();

    private static async Task<string> Read(string root, string relativePath)
        => await File.ReadAllTextAsync(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ACHInterbank.sln"))) directory = directory.Parent;
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
