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

public sealed class NavigationMenuNav34QParticipantConnectionAuthorizationTests
{
    private const string Route = "/integraciones/conexion";

    [Fact]
    public async Task ManageConnections_IsDedicatedCapability_AndSeederPersistsItIdempotentlyWithoutGrants()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;
        var grantsBefore = await RoleGrantsAsync(context);

        Assert.Contains(FineGrainedPermissions.Integrations.ManageConnections, FineGrainedPermissions.AllPermissions);
        Assert.DoesNotContain(FineGrainedPermissions.Integrations.ManageConnections,
            new[]
            {
                FineGrainedPermissions.Integrations.ManageContracts,
                FineGrainedPermissions.Integrations.ManageMappings,
                FineGrainedPermissions.Integrations.Validate,
                FineGrainedPermissions.Integrations.Publish
            });

        var seeder = new IntegrationConnectionPermissionSeeder(context);
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var permission = await context.Permissions.AsNoTracking()
            .SingleAsync(item => item.Name == FineGrainedPermissions.Integrations.ManageConnections);
        Assert.Equal(IntegrationConnectionPermissionSeeder.ManageConnectionsPermissionId, permission.Id);
        Assert.Equal("Administrar conexiones de integración", permission.Description);
        Assert.Empty(await context.RolePermissions.AsNoTracking()
            .Where(link => link.PermissionId == permission.Id)
            .ToArrayAsync());
        Assert.Equal(grantsBefore, await RoleGrantsAsync(context));
    }

    [Fact]
    public async Task ParticipantConnectionMenu_UsesCanReadAch_PreservesCanonicalStructureAndAdminAudience_AndAddsNoGrants()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;
        var item = new MenuItem
        {
            Id = 7403,
            MenuId = 1,
            ParentId = MenuItemConfiguration.IntegrationsId,
            Label = "Conexión entidad participante",
            Route = Route,
            Icon = "link",
            Order = 30,
            Exact = true,
            IsActive = true
        };
        context.MenuItems.Add(item);
        context.MenuItemPermissions.Add(new MenuItemPermission
        {
            MenuItemId = item.Id,
            PermissionId = PermissionConfiguration.ManageUsersPermissionId
        });
        context.MenuItemRoles.Add(new MenuItemRole
        {
            MenuItemId = item.Id,
            RoleId = RoleConfiguration.AdminRoleId
        });
        await context.SaveChangesAsync();

        var grantsBefore = await RoleGrantsAsync(context);
        var seeder = new ParticipantConnectionMenuAuthorizationSeeder(context);
        Assert.True(InformationRelationshipMenuAuthorizationSeeder.AuthorizationReconciliationOrder < seeder.Order);
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var after = await context.MenuItems.AsNoTracking().SingleAsync(value => value.Id == item.Id);
        Assert.Equal("Conexión entidad participante", after.Label);
        Assert.Equal(Route, after.Route);
        Assert.Equal(MenuItemConfiguration.IntegrationsId, after.ParentId);
        Assert.Equal(30, after.Order);
        Assert.True(after.IsActive);
        Assert.True(after.Exact);

        var permissions = await context.MenuItemPermissions.AsNoTracking()
            .Where(link => link.MenuItemId == item.Id)
            .Select(link => link.PermissionId)
            .ToArrayAsync();
        Assert.Equal(new[] { PermissionConfiguration.ReadAchPermissionId }, permissions);

        var roles = await context.MenuItemRoles.AsNoTracking()
            .Where(link => link.MenuItemId == item.Id)
            .Select(link => link.RoleId)
            .ToArrayAsync();
        Assert.Equal(new[] { RoleConfiguration.AdminRoleId }, roles);
        Assert.Equal(grantsBefore, await RoleGrantsAsync(context));
    }

    [Fact]
    public async Task ParticipantConnectionMenu_DirectBoundary_RemainsAdminAudienceButNoLongerDependsOnCanManageUsers()
    {
        var item = new MenuItem { Id = 7403, Label = "Conexión entidad participante", Route = Route, Order = 30, Exact = true, IsActive = true };
        item.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = item.Id, Permission = new Permission { Name = "CanReadAch" } });
        item.MenuItemRoles.Add(new MenuItemRole { MenuItemId = item.Id, Role = new Role { Name = "Admin" } });
        var repository = new StaticMenuRepository([item]);

        Assert.Contains(await MenuFor(repository, [new Claim(ClaimTypes.Role, "Admin"), new Claim("permission", "CanReadAch")]), value => value.Route == Route);
        Assert.DoesNotContain(await MenuFor(repository, [new Claim(ClaimTypes.Role, "Admin"), new Claim("permission", "CanManageUsers")]), value => value.Route == Route);
        Assert.DoesNotContain(await MenuFor(repository, [new Claim(ClaimTypes.Role, "ACH.Operator"), new Claim("permission", "CanReadAch")]), value => value.Route == Route);
        Assert.DoesNotContain(await MenuFor(repository, [new Claim("permission", "CanReadAch")]), value => value.Route == Route);
    }

    [Fact]
    public async Task ConnectionManagementPolicy_AllowsDedicatedOrLegacyManagement_ButNotReadOrOtherIntegrationCapabilities()
    {
        using var provider = BuildExternalProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        Assert.True((await authorization.AuthorizeAsync(
            Principal(FineGrainedPermissions.Integrations.ManageConnections), null, P1Policies.IntegrationConnectionsManage)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(
            Principal("CanManageAch"), null, P1Policies.IntegrationConnectionsManage)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            Principal("CanReadAch"), null, P1Policies.IntegrationConnectionsManage)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            Principal(FineGrainedPermissions.Integrations.Read), null, P1Policies.IntegrationConnectionsManage)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            Principal(FineGrainedPermissions.Integrations.ManageContracts), null, P1Policies.IntegrationConnectionsManage)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            Principal(FineGrainedPermissions.Integrations.ManageMappings), null, P1Policies.IntegrationConnectionsManage)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            Principal(FineGrainedPermissions.Integrations.Validate), null, P1Policies.IntegrationConnectionsManage)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            Principal(FineGrainedPermissions.Integrations.Publish), null, P1Policies.IntegrationConnectionsManage)).Succeeded);
    }

    [Fact]
    public void ParticipantConnectionApi_SeparatesReadFromConnectionManagement_AndHasNoTestOrInvokeAction()
    {
        AssertActionPolicy(nameof(SoapIntegrationSettingsController.GetAsync), typeof(HttpGetAttribute), P1Policies.IntegrationsRead);
        AssertActionPolicy(nameof(SoapIntegrationSettingsController.PreviewChangeAsync), typeof(HttpPostAttribute), P1Policies.IntegrationConnectionsManage);
        AssertActionPolicy(nameof(SoapIntegrationSettingsController.SaveAsync), typeof(HttpPutAttribute), P1Policies.IntegrationConnectionsManage);

        var publicMethods = typeof(SoapIntegrationSettingsController).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.DoesNotContain(publicMethods, method => method.Name.Contains("Test", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicMethods, method => method.Name.Contains("Invoke", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(publicMethods, method => method.GetCustomAttribute<HttpDeleteAttribute>() is not null);
    }

    [Fact]
    public async Task AdministrationResponse_SanitizesDestinationAndAction_AndConnectionConfigurationContainsNoCredentialFields()
    {
        var root = RepositoryRoot();
        var service = await Read(root, "src/Cfa.ACHInterbank.Persistence/Security/Services/SoapIntegrationSettingsService.cs");
        var mappingDto = await Read(root, "src/Cfa.ACHInterbank.Application/Security/Dtos/SoapEndpointMethodMappingDto.cs");
        var settingsDto = await Read(root, "src/Cfa.ACHInterbank.Application/Security/Dtos/SoapIntegrationSettingsDto.cs");
        var parameterDto = await Read(root, "src/Cfa.ACHInterbank.Application/Security/Dtos/SoapInputParameterMappingDto.cs");
        var entity = await Read(root, "src/Cfa.ACHInterbank.Domain/Entities/User/SoapIntegrationSetting.cs");

        Assert.Contains("Endpoint = sanitizedEndpoint", service);
        Assert.Contains("SoapAction = sanitizedSoapAction", service);
        Assert.Contains("SanitizeEndpoint", service);
        Assert.Contains("SanitizeTechnicalUri", service);
        Assert.Contains("PreservePersistedSensitiveValues", service);
        Assert.Contains("WscfaachMappingsJson", entity);
        Assert.Contains("WsAxonRespuestaTransaccionesMappingsJson", entity);

        var publicSurface = mappingDto + settingsDto + parameterDto + entity;
        foreach (var forbidden in new[] { "Password", "AccessToken", "ApiKey", "ClientSecret", "PrivateKey", "SecretRef", "EncryptedCredential" })
            Assert.False(publicSurface.Contains(forbidden, StringComparison.OrdinalIgnoreCase));

        Assert.Contains("Endpoint", mappingDto);
        Assert.Contains("SoapAction", mappingDto);
        Assert.Contains("OperatingMode", mappingDto);
        Assert.Contains("TimeoutSeconds", mappingDto);
        Assert.Contains("Enabled", mappingDto);
        Assert.Contains("DefaultValue", parameterDto);
    }

    [Fact]
    public async Task SettingsRequests_DoNotInvokeExternalTransport_ButSavedLiveConfigurationFeedsGuardedDownstreamSoapClients()
    {
        var root = RepositoryRoot();
        var settingsService = await Read(root, "src/Cfa.ACHInterbank.Persistence/Security/Services/SoapIntegrationSettingsService.cs");
        var wscClient = await Read(root, "src/Cfa.ACHInterbank.External/Connections/WscfaachSoapClient.cs");
        var wsAxonClient = await Read(root, "src/Cfa.ACHInterbank.External/Connections/WsAxonRespuestaTransaccionesSoapClient.cs");
        var endpointOptions = await Read(root, "src/Cfa.ACHInterbank.Application/Security/Dtos/WsAxonEndpointSecurityOptions.cs");
        var transportResolver = await Read(root, "src/Cfa.ACHInterbank.External/Connections/ControlledLocalSoapTransportResolver.cs");

        Assert.DoesNotContain("HttpClient", settingsService);
        Assert.DoesNotContain("HttpRequestMessage", settingsService);
        Assert.DoesNotContain("SendAsync(request", settingsService);
        Assert.Contains("SaveChangesAsync", settingsService);
        Assert.Contains("mapping.OperatingMode", settingsService);
        Assert.Contains("mapping.Enabled", settingsService);

        Assert.Contains("HttpRequestMessage(HttpMethod.Post", wscClient);
        Assert.Contains("_soapSettingsService.GetAsync", wscClient);
        Assert.Contains("not enabled for Live execution by database configuration", wscClient);
        Assert.Contains("HttpRequestMessage(HttpMethod.Post", wsAxonClient);
        Assert.Contains("_soapSettingsService.GetAsync", wsAxonClient);
        Assert.Contains("not enabled for Live execution by database configuration", wsAxonClient);
        Assert.Contains("WsAxonEndpointSecurityMode.ControlledLocal", wsAxonClient);
        Assert.Contains("WsAxonEndpointSecurityMode.ConfiguredAllowlist", wsAxonClient);
        Assert.Contains("WsAxonEndpointSecurityMode.Unconfigured", endpointOptions);
        Assert.Contains("SoapEndpointSecurity:WsAxonRespuestaTransacciones", endpointOptions);
        Assert.Contains("SoapTransport:ControlledLocal", endpointOptions);
        Assert.Contains("authorized local hosts", transportResolver);
    }

    private static void AssertActionPolicy(string methodName, Type httpMethodAttribute, string expectedPolicy)
    {
        var action = typeof(SoapIntegrationSettingsController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
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
