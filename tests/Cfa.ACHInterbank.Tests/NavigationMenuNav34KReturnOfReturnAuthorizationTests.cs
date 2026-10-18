using System.Reflection;
using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.DataBase;
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

public sealed class NavigationMenuNav34KReturnOfReturnAuthorizationTests
{
    [Fact]
    public async Task ReturnsReadCapability_IsExistingFineGrainedContract_AndSeederPersistsItIdempotentlyWithoutGrants()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;
        var grantsBefore = await RoleGrantsAsync(context);

        Assert.Contains(FineGrainedPermissions.Returns.Read, FineGrainedPermissions.AllPermissions);
        Assert.False(await context.Permissions.AsNoTracking()
            .AnyAsync(item => item.Name == FineGrainedPermissions.Returns.Read));

        var seeder = new ReturnsReadPermissionSeeder(context);
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var permission = await context.Permissions.AsNoTracking()
            .SingleAsync(item => item.Name == FineGrainedPermissions.Returns.Read);
        Assert.Equal(ReturnsReadPermissionSeeder.ReadPermissionId, permission.Id);
        Assert.Equal("Consultar devoluciones ACH", permission.Description);
        Assert.Empty(await context.RolePermissions.AsNoTracking()
            .Where(link => link.PermissionId == permission.Id)
            .ToArrayAsync());
        Assert.Equal(grantsBefore, await RoleGrantsAsync(context));
    }

    [Fact]
    public async Task ReturnOfReturnMenu_UsesReturnsReadOrCanReadAch_WithoutRoleGate_AndKeepsCanonicalStructure()
    {
        await using var fixture = CreateFixture();
        var context = fixture.Context;
        await SeedFunctionalNavigationAsync(context);
        await new NavigationInformationArchitectureSeeder(context).SeedAsync();
        await new ReturnsReadPermissionSeeder(context).SeedAsync();
        context.ChangeTracker.Clear();

        var before = await context.MenuItems.AsNoTracking()
            .SingleAsync(item => item.Route == "/transactions/returns-ror");
        var grantsBefore = await RoleGrantsAsync(context);

        var seeder = new ReturnOfReturnMenuAuthorizationSeeder(context);
        Assert.True(NavigationInformationArchitectureSeeder.FinalReconciliationOrder < seeder.Order);
        Assert.True(DigitalEnvelopeMenuAuthorizationSeeder.AuthorizationReconciliationOrder < seeder.Order);
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        context.ChangeTracker.Clear();

        var parent = await context.MenuItems.AsNoTracking()
            .SingleAsync(item => item.Route == NavigationInformationArchitectureSeeder.OperationRoute);
        var item = await context.MenuItems.AsNoTracking()
            .SingleAsync(value => value.Route == "/transactions/returns-ror");

        Assert.Equal(before.Id, item.Id);
        Assert.Equal("Devolución de una devolución", item.Label);
        Assert.Equal(parent.Id, item.ParentId);
        Assert.Equal(70, item.Order);
        Assert.Equal(before.Exact, item.Exact);
        Assert.True(item.IsActive);

        var permissions = await context.MenuItemPermissions.AsNoTracking()
            .Where(link => link.MenuItemId == item.Id)
            .OrderBy(link => link.PermissionId)
            .Select(link => link.PermissionId)
            .ToArrayAsync();
        Assert.Equal(
            new[] { PermissionConfiguration.ReadAchPermissionId, ReturnsReadPermissionSeeder.ReadPermissionId }.OrderBy(x => x).ToArray(),
            permissions);
        Assert.DoesNotContain(PermissionConfiguration.ManageAchPermissionId, permissions);
        Assert.Empty(await context.MenuItemRoles.AsNoTracking().Where(link => link.MenuItemId == item.Id).ToArrayAsync());
        Assert.Equal(grantsBefore, await RoleGrantsAsync(context));
    }

    [Fact]
    public async Task MenuReadBoundary_AllowsEitherCanonicalReadCapability_ButNotManageOnlyOrNoPermission()
    {
        var fineRead = new Permission { Name = FineGrainedPermissions.Returns.Read };
        var legacyRead = new Permission { Name = "CanReadAch" };
        var item = new MenuItem
        {
            Id = 9001,
            Label = "Devolución de una devolución",
            Route = "/transactions/returns-ror",
            Order = 70,
            Exact = true,
            IsActive = true
        };
        item.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = item.Id, Permission = fineRead });
        item.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = item.Id, Permission = legacyRead });
        var repository = new StaticMenuRepository([item]);

        Assert.Contains(await MenuFor(repository, FineGrainedPermissions.Returns.Read), menu => menu.Route == item.Route);
        Assert.Contains(await MenuFor(repository, "CanReadAch"), menu => menu.Route == item.Route);
        Assert.DoesNotContain(await MenuFor(repository, "CanManageAch"), menu => menu.Route == item.Route);
        Assert.DoesNotContain(await MenuFor(repository), menu => menu.Route == item.Route);
    }

    [Fact]
    public async Task SpaModel_IsMixedReadEvaluation_WithFailClosedGenerationActions_AndParentRestrictionRemainsUntouched()
    {
        var root = RepositoryRoot();
        var routing = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts");
        var component = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/components/ach-return-of-return-management/ach-return-of-return-management.component.ts");
        var template = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/components/ach-return-of-return-management/ach-return-of-return-management.component.html");
        var appRouting = await Read(root, "web/ach-interbank-ui/src/app/app-routing.module.ts");

        var rorRoute = SliceToEnd(routing, "path: 'returns-ror'");
        Assert.Contains("permissions: ['Returns.Read', 'CanReadAch']", rorRoute);
        Assert.DoesNotContain("permissions: ['CanManageAch']", rorRoute);
        Assert.DoesNotContain("roles:", rorRoute);

        Assert.Contains("RETURN_OF_RETURN_GENERATE_PERMISSIONS = ['Returns.GenerateFile', 'CanManageAch']", component);
        Assert.Contains("get canGenerateFiles(): boolean", component);
        Assert.True(Count(component, "if (!this.ensureCanGenerateFiles())") >= 2);
        Assert.Contains("No tiene permisos para generar archivos de devolución de devolución.", component);
        Assert.Contains("*ngIf=\"canGenerateFiles; else readOnlyGenerationNotice\"", template);
        Assert.Contains("permite evaluar elegibilidad, pero no generar archivos", template);

        var parentRoute = Slice(appRouting, "path: 'transactions'", "path: 'integraciones'");
        Assert.Contains("roles: ['Admin', 'ACH.Operator']", parentRoute);
        Assert.Contains("permissions: ['CanManageAch', 'CanReadAch']", parentRoute);
    }

    [Fact]
    public async Task ApiPolicies_KeepEvaluationReadOnlySemanticsAndGenerationMutationBoundary()
    {
        AssertActionPolicy(nameof(AchReturnOfReturnController.Evaluate), typeof(HttpPostAttribute), P0Policies.ReturnsRead);
        AssertActionPolicy(nameof(AchReturnOfReturnController.GenerateAuditFile), typeof(HttpPostAttribute), P0Policies.ReturnsGenerateFile);
        AssertActionPolicy(nameof(AchReturnOfReturnController.GenerateNachaFile), typeof(HttpPostAttribute), P0Policies.ReturnsGenerateFile);
        AssertActionPolicy(nameof(AchReturnOfReturnController.CreateCenitOut), typeof(HttpPostAttribute), P0Policies.ReturnsGenerateFile);

        using var provider = BuildExternalProvider();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        Assert.True((await authorization.AuthorizeAsync(Principal(FineGrainedPermissions.Returns.Read), null, P0Policies.ReturnsRead)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(Principal("CanReadAch"), null, P0Policies.ReturnsRead)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal("CanManageAch"), null, P0Policies.ReturnsRead)).Succeeded);

        Assert.True((await authorization.AuthorizeAsync(Principal(FineGrainedPermissions.Returns.GenerateFile), null, P0Policies.ReturnsGenerateFile)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(Principal("CanManageAch"), null, P0Policies.ReturnsGenerateFile)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal("CanReadAch"), null, P0Policies.ReturnsGenerateFile)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal(FineGrainedPermissions.Returns.Read), null, P0Policies.ReturnsGenerateFile)).Succeeded);
    }

    [Fact]
    public async Task UiConsumesOnlyEvaluateAndTwoGenerationEndpoints_AndDoesNotInvokeCenitOutOrExternalTransport()
    {
        var root = RepositoryRoot();
        var service = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/services/ach-returns-api.service.ts");
        var component = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/components/ach-return-of-return-management/ach-return-of-return-management.component.ts");

        Assert.Contains("ach-returns/return-of-return/evaluate", service);
        Assert.Contains("ach-returns/return-of-return/generate-audit-file", service);
        Assert.Contains("ach-returns/return-of-return/generate-nacha-file", service);
        Assert.DoesNotContain("return-of-return/cenit/out", service);
        Assert.DoesNotContain("dispatch", component, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SOAP", component, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MFT", component, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FunctionalEffects_EvaluationIsReadOnly_AndGenerationPersistsOnlyGeneratedFileAuditWithoutDirectTransmission()
    {
        var root = RepositoryRoot();
        var eligibility = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnOfReturnEligibilityService.cs");
        var generation = await Read(root, "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnOfReturnFileGenerationService.cs");

        Assert.Contains("AsNoTracking()", eligibility);
        Assert.DoesNotContain("SaveChanges", eligibility);
        Assert.DoesNotContain("ReturnOfReturnFlows.Add", eligibility);

        Assert.Contains("context.AchReturnOfReturnGeneratedFileAudits.Add(audit)", generation);
        Assert.Contains("GenerateNachaAsync", generation);
        Assert.Contains("BuildReturnOutAsync", generation);
        Assert.DoesNotContain("ReturnOfReturnFlows.Add", generation);
        Assert.DoesNotContain("IContrapartidaDispatchJobService", generation);
        Assert.DoesNotContain("IWscfaachSoapClient", generation);
        Assert.DoesNotContain("IAchOutboundReturnDispatchService", generation);
        Assert.DoesNotContain("HttpClient", generation);
    }

    [Fact]
    public async Task Regression_OrdinaryReturnsNav34D_RemainsUntouched()
    {
        var root = RepositoryRoot();
        var routing = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts");
        var component = await Read(root, "web/ach-interbank-ui/src/app/features/transactions/components/ach-returns-management/ach-returns-management.component.ts");
        var controller = await Read(root, "src/Cfa.ACHInterbank.Api/Controllers/AchReturnsController.cs");
        var menuSeeder = await Read(root, "src/Cfa.ACHInterbank.Persistence/Security/Seeders/ReturnsMenuAuthorizationSeeder.cs");

        var returnsRoute = Slice(routing, "path: 'returns'", "path: 'returns-ror'");
        Assert.Contains("permissions: ['CanReadAch']", returnsRoute);
        Assert.Contains("RETURNS_GENERATE_PERMISSIONS = ['Returns.GenerateFile', 'CanManageAch']", component);
        Assert.Contains("P0Policies.ReturnsRead", controller);
        Assert.Contains("P0Policies.ReturnsGenerateFile", controller);
        Assert.Contains("PermissionConfiguration.ReadAchPermissionId", menuSeeder);
        Assert.DoesNotContain("ReturnsReadPermissionSeeder", menuSeeder);
    }

    private static void AssertActionPolicy(string actionName, Type httpAttributeType, string expectedPolicy)
    {
        var method = typeof(AchReturnOfReturnController).GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        Assert.Contains(method!.GetCustomAttributes(true), attribute => httpAttributeType.IsInstanceOfType(attribute));
        Assert.Contains(method.GetCustomAttributes<AuthorizeAttribute>(true), attribute =>
            attribute.Policy == expectedPolicy && string.IsNullOrWhiteSpace(attribute.Roles));
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
