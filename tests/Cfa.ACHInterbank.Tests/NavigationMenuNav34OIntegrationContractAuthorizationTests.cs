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

public sealed class NavigationMenuNav34OIntegrationContractAuthorizationTests
{
    private const string ContractRoute = "/integraciones/contrato";

    [Fact]
    public async Task ManageContracts_IsDedicatedCapability_AndSeederPersistsItIdempotentlyWithoutGrants()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;
        var grantsBefore = await RoleGrantsAsync(context);

        Assert.Contains(FineGrainedPermissions.Integrations.ManageContracts, FineGrainedPermissions.AllPermissions);
        Assert.DoesNotContain(FineGrainedPermissions.Integrations.ManageContracts,
            new[]
            {
                FineGrainedPermissions.Integrations.ManageMappings,
                FineGrainedPermissions.Integrations.Validate,
                FineGrainedPermissions.Integrations.Publish,
                FineGrainedPermissions.Integrations.Compare
            });

        var seeder = new IntegrationContractPermissionSeeder(context);
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var permission = await context.Permissions.AsNoTracking()
            .SingleAsync(item => item.Name == FineGrainedPermissions.Integrations.ManageContracts);
        Assert.Equal(IntegrationContractPermissionSeeder.ManageContractsPermissionId, permission.Id);
        Assert.Equal("Administrar contratos de integración", permission.Description);
        Assert.Empty(await context.RolePermissions.AsNoTracking()
            .Where(link => link.PermissionId == permission.Id)
            .ToArrayAsync());
        Assert.Equal(grantsBefore, await RoleGrantsAsync(context));
    }

    [Fact]
    public async Task ContractMenu_UsesCanReadAch_PreservesCanonicalStructureAndAdminAudience_AndAddsNoGrants()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;
        var item = new MenuItem
        {
            Id = 7201,
            MenuId = 1,
            ParentId = MenuItemConfiguration.IntegrationsId,
            Label = "Contrato del servicio",
            Route = ContractRoute,
            Icon = "description",
            Order = 10,
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
        var seeder = new IntegrationContractMenuAuthorizationSeeder(context);
        Assert.True(ConnectionsStructuralMenuAuthorizationSeeder.AuthorizationReconciliationOrder < seeder.Order);
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var after = await context.MenuItems.AsNoTracking().SingleAsync(value => value.Id == item.Id);
        Assert.Equal("Contrato del servicio", after.Label);
        Assert.Equal(ContractRoute, after.Route);
        Assert.Equal(MenuItemConfiguration.IntegrationsId, after.ParentId);
        Assert.Equal(10, after.Order);
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
    public async Task ContractMenu_DirectBoundary_RemainsAdminAudienceButNoLongerDependsOnCanManageUsers()
    {
        var item = new MenuItem
        {
            Id = 7201,
            Label = "Contrato del servicio",
            Route = ContractRoute,
            Order = 10,
            Exact = true,
            IsActive = true
        };
        item.MenuItemPermissions.Add(new MenuItemPermission
        {
            MenuItemId = item.Id,
            Permission = new Permission { Name = "CanReadAch" }
        });
        item.MenuItemRoles.Add(new MenuItemRole
        {
            MenuItemId = item.Id,
            Role = new Role { Name = "Admin" }
        });
        var repository = new StaticMenuRepository([item]);

        Assert.Contains(await MenuFor(repository,
            [new Claim(ClaimTypes.Role, "Admin"), new Claim("permission", "CanReadAch")]), value => value.Route == ContractRoute);
        Assert.DoesNotContain(await MenuFor(repository,
            [new Claim(ClaimTypes.Role, "Admin"), new Claim("permission", "CanManageUsers")]), value => value.Route == ContractRoute);
        Assert.DoesNotContain(await MenuFor(repository,
            [new Claim(ClaimTypes.Role, "ACH.Operator"), new Claim("permission", "CanReadAch")]), value => value.Route == ContractRoute);
        Assert.DoesNotContain(await MenuFor(repository,
            [new Claim("permission", "CanReadAch")]), value => value.Route == ContractRoute);
    }

    [Fact]
    public async Task ContractManagementPolicy_AllowsDedicatedOrLegacyManagement_ButNotReadCapabilities()
    {
        using var provider = BuildExternalProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        Assert.True((await authorization.AuthorizeAsync(
            Principal(FineGrainedPermissions.Integrations.ManageContracts), null, P1Policies.IntegrationContractsManage)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(
            Principal("CanManageAch"), null, P1Policies.IntegrationContractsManage)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            Principal("CanReadAch"), null, P1Policies.IntegrationContractsManage)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            Principal(FineGrainedPermissions.Integrations.Read), null, P1Policies.IntegrationContractsManage)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(
            Principal(FineGrainedPermissions.Integrations.ManageMappings), null, P1Policies.IntegrationContractsManage)).Succeeded);
    }

    [Fact]
    public void ContractPageConsumedApi_SeparatesReadAndManagement_WithoutChangingOtherLifecycleActions()
    {
        AssertActionPolicy(nameof(IntegrationContractsController.GetByMethod), typeof(HttpGetAttribute), P1Policies.IntegrationsRead);
        AssertActionPolicy(nameof(IntegrationContractsController.GetById), typeof(HttpGetAttribute), P1Policies.IntegrationsRead);
        AssertActionPolicy(nameof(IntegrationContractsController.UpdatePresentation), typeof(HttpPutAttribute), P1Policies.IntegrationContractsManage);
        AssertActionPolicy(nameof(IntegrationContractsController.AnalyzeArtifact), typeof(HttpPostAttribute), P1Policies.IntegrationContractsManage);
        AssertActionPolicy(nameof(IntegrationContractsController.PreviewArtifact), typeof(HttpPostAttribute), P1Policies.IntegrationContractsManage);
        AssertActionPolicy(nameof(IntegrationContractsController.ImportArtifact), typeof(HttpPostAttribute), P1Policies.IntegrationContractsManage);

        AssertActionPolicy(nameof(IntegrationContractsController.Create), typeof(HttpPostAttribute), "CanManageAch");
        AssertActionPolicy(nameof(IntegrationContractsController.Clone), typeof(HttpPostAttribute), "CanManageAch");
        AssertActionPolicy(nameof(IntegrationContractsController.Compare), typeof(HttpGetAttribute), P1Policies.IntegrationsRead);
        AssertActionPolicy(nameof(IntegrationContractsController.Activate), typeof(HttpPostAttribute), "CanManageAch");
        AssertActionPolicy(nameof(IntegrationContractsController.Rollback), typeof(HttpPostAttribute), "CanManageAch");
    }

    [Fact]
    public async Task ContractSurface_UsesLocalArtifactAnalysis_DraftPersistence_AndDoesNotExposeSecretsOrInvokeExternalTransport()
    {
        var root = RepositoryRoot();
        var controller = await Read(root, "src/Cfa.ACHInterbank.Api/Controllers/IntegrationContractsController.cs");
        var analyzer = await Read(root, "src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationContractArtifactAnalyzer.cs");
        var importer = await Read(root, "src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationContractArtifactImportService.cs");
        var versionService = await Read(root, "src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationContractVersionService.cs");
        var dto = await Read(root, "src/Cfa.ACHInterbank.Application/Integrations/Dtos/IntegrationContractVersionAdministrationDtos.cs");
        var artifactDto = await Read(root, "src/Cfa.ACHInterbank.Application/Integrations/Dtos/IntegrationContractArtifactDtos.cs");
        var catalogDto = await Read(root, "src/Cfa.ACHInterbank.Application/Integrations/Dtos/IntegrationCatalogDtos.cs");

        Assert.Contains("DtdProcessing = DtdProcessing.Prohibit", analyzer);
        Assert.Contains("XmlResolver = null", analyzer);
        Assert.Contains("EXTERNAL_REFERENCE_NOT_ALLOWED", analyzer);
        Assert.Contains("Status = IntegrationContractVersionStatusEnum.Draft", importer);
        Assert.Contains("SourceReference = \"local-upload\"", importer);
        Assert.Contains("SaveChangesAsync", importer);
        Assert.Contains("UpdateParameterPresentationAsync", versionService);
        Assert.Contains("deliberately leaves all technical/runtime fields untouched", versionService);

        foreach (var source in new[] { controller, analyzer, importer, versionService })
        {
            Assert.DoesNotContain("HttpClient", source);
            Assert.DoesNotContain("SftpClient", source);
            Assert.DoesNotContain("IWscfaachSoapClient", source);
        }

        var dtoSurface = dto + artifactDto + catalogDto;
        Assert.False(dtoSurface.Contains("Password", StringComparison.OrdinalIgnoreCase));
        Assert.False(dtoSurface.Contains("PrivateKey", StringComparison.OrdinalIgnoreCase));
        Assert.False(dtoSurface.Contains("AccessToken", StringComparison.OrdinalIgnoreCase));
        Assert.False(dtoSurface.Contains("ClientSecret", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("SoapAction", dtoSurface);
        Assert.Contains("TargetNamespace", dtoSurface);
    }

    private static void AssertActionPolicy(string methodName, Type httpMethodAttribute, string expectedPolicy)
    {
        var action = typeof(IntegrationContractsController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
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
