using System.Reflection;
using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.DataBase.Queries.Navigation;
using Cfa.ACHInterbank.Application.Navigation;
using Cfa.ACHInterbank.Application.Navigation.Queries;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Persistence.Navigation.Seeders;
using Microsoft.AspNetCore.Authorization;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav33AInboundSimulatorProductionAuthorizationTests
{
    [Fact]
    public async Task MenuHandler_RequiresTestToolsAccess_WithoutEnvironmentDependentFiltering()
    {
        var permission = new Permission { Name = FineGrainedPermissions.TestTools.Access };
        var parent = MenuItemWithPermission(9000, null, "Herramientas de prueba", "/uat", 90, permission);
        var simulator = MenuItemWithPermission(9001, parent.Id, "Simulador de archivos recibidos", "/uat/nacha-inbound-simulator", 10, permission);
        var repository = new StaticMenuRepository([parent, simulator]);

        var withoutPermission = await new GetMenuForCurrentUserHandler(
                repository,
                new TestCurrentUserContext([]))
            .Handle(new GetMenuForCurrentUserQuery(), CancellationToken.None);
        Assert.DoesNotContain(Flatten(withoutPermission), item => item.Route == "/uat");
        Assert.DoesNotContain(Flatten(withoutPermission), item => item.Route == "/uat/nacha-inbound-simulator");

        var withPermission = await new GetMenuForCurrentUserHandler(
                repository,
                new TestCurrentUserContext([new Claim("permission", FineGrainedPermissions.TestTools.Access)]))
            .Handle(new GetMenuForCurrentUserQuery(), CancellationToken.None);
        var testTools = Assert.Single(withPermission, item => item.Route == "/uat");
        Assert.Contains(testTools.Children, item => item.Route == "/uat/nacha-inbound-simulator");
    }

    [Fact]
    public void SimulatorController_KeepsTestToolsAccessAndSpecificActionPolicies_WithoutEnvironmentDependency()
    {
        var controllerPolicies = typeof(NachaInboundSimulatorController)
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Select(attribute => attribute.Policy)
            .ToArray();
        Assert.Contains(FineGrainedPermissions.TestTools.Access, controllerPolicies);

        AssertActionPolicy(nameof(NachaInboundSimulatorController.List), P1Policies.NachaSimulatorRead);
        AssertActionPolicy(nameof(NachaInboundSimulatorController.GetById), P1Policies.NachaSimulatorRead);
        AssertActionPolicy(nameof(NachaInboundSimulatorController.DownloadFile), P1Policies.NachaSimulatorDownload);
        AssertActionPolicy(nameof(NachaInboundSimulatorController.Preview), P1Policies.NachaSimulatorRead);
        AssertActionPolicy(nameof(NachaInboundSimulatorController.EligibleDifferentialTransactions), P1Policies.NachaSimulatorRead);
        AssertActionPolicy(nameof(NachaInboundSimulatorController.AvailableCycles), P1Policies.NachaSimulatorRead);

        var constructorTypes = typeof(NachaInboundSimulatorController).GetConstructors().Single().GetParameters()
            .Select(parameter => parameter.ParameterType.Name)
            .ToArray();
        Assert.DoesNotContain("IHostEnvironment", constructorTypes);
        Assert.DoesNotContain("IOptions`1", constructorTypes);
    }

    [Fact]
    public async Task CurrentCode_KeepsInboundSimulatorState_WhileNav33BClosesServiceToolEnvironmentGate()
    {
        var root = RepositoryRoot();
        var menuHandler = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Application", "Navigation", "Queries", "GetMenuForCurrentUserHandler.cs"));
        var simulatorController = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Api", "Controllers", "NachaInboundSimulatorController.cs"));
        var simulatorService = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Persistence", "ACH", "Services", "Implementation", "NachaInboundSimulationService.cs"));
        var soapController = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Api", "Controllers", "NachaSoapUatConsoleController.cs"));
        var uatContrapartidasController = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Api", "Controllers", "UatContrapartidasController.cs"));
        var navigationSeeder = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Persistence", "Navigation", "Seeders", "NavigationInformationArchitectureSeeder.cs"));
        var grantSeeder = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Persistence", "Security", "Seeders", "TestToolsAdminRolePermissionSeeder.cs"));
        var appRouting = await File.ReadAllTextAsync(Path.Combine(root, "web", "ach-interbank-ui", "src", "app", "app-routing.module.ts"));
        var uatRouting = await File.ReadAllTextAsync(Path.Combine(root, "web", "ach-interbank-ui", "src", "app", "features", "uat", "uat-routing.module.ts"));

        Assert.DoesNotContain("IsProduction", menuHandler);
        Assert.DoesNotContain("IsUatLike", menuHandler);
        Assert.DoesNotContain("/uat/", menuHandler);

        Assert.Contains("[Authorize(Policy = FineGrainedPermissions.TestTools.Access)]", simulatorController);
        Assert.DoesNotContain("IsProduction", simulatorController);
        Assert.DoesNotContain("IsUatLike", simulatorController);
        Assert.DoesNotContain("UAT_FEATURE_UNAVAILABLE", simulatorController);

        Assert.Contains("if (!_options.Enabled)", simulatorService);
        Assert.DoesNotContain("if (!_options.IsUatLike())", simulatorService);
        Assert.Contains("_options.AllowAutoImport || _options.AllowExternalTransmission", simulatorService);
        Assert.Contains("GeneratedOnly = true", simulatorService);
        Assert.Contains("AutoImported = false", simulatorService);
        Assert.Contains("UploadRequired = true", simulatorService);
        Assert.Contains("ExternalTransmission = false", simulatorService);
        Assert.DoesNotContain("ProcessTransactionAsync", simulatorService);
        Assert.DoesNotContain("ProcessCycleAsync", simulatorService);

        var uatBlock = Slice(appRouting, "path: 'uat'", "path: 'incoming-nacha-command-center'");
        Assert.Contains("canActivate: [permissionGuard]", uatBlock);
        Assert.Contains("permissions: ['TestTools.Access']", uatBlock);
        Assert.DoesNotContain("roles:", uatBlock);
        Assert.DoesNotContain("CanReadAch", uatBlock);
        Assert.DoesNotContain("CanManageAch", uatBlock);
        Assert.Contains("permissions: ['TestTools.Access']", uatRouting);
        Assert.DoesNotContain("CanReadAch", uatRouting);
        Assert.DoesNotContain("CanManageAch", uatRouting);

        Assert.Contains("serviceTests.IsActive = false;", navigationSeeder);
        Assert.DoesNotContain("IsProduction", soapController);
        Assert.DoesNotContain("IsUatLike", soapController);
        Assert.DoesNotContain("NachaSimulatorRead", soapController);
        Assert.Contains("_hostEnvironment.IsProduction()", uatContrapartidasController);
        Assert.Contains("ACH_SOAP_LIVE_TESTS", uatContrapartidasController);

        Assert.Contains("RoleConfiguration.AdminRoleId", grantSeeder);
        Assert.DoesNotContain("RoleConfiguration.OperatorRoleId", grantSeeder);
        Assert.DoesNotContain("UserId", grantSeeder, StringComparison.Ordinal);
    }

    private static MenuItem MenuItemWithPermission(int id, int? parentId, string label, string route, int order, Permission permission)
    {
        var item = new MenuItem
        {
            Id = id,
            ParentId = parentId,
            Label = label,
            Route = route,
            Order = order,
            Exact = true,
            IsActive = true
        };
        item.MenuItemPermissions.Add(new MenuItemPermission { MenuItemId = id, Permission = permission });
        return item;
    }

    private static IEnumerable<MenuItemDto> Flatten(IEnumerable<MenuItemDto> items)
        => items.SelectMany(item => new[] { item }.Concat(Flatten(item.Children)));

    private static void AssertActionPolicy(string actionName, string policy)
    {
        var method = typeof(NachaInboundSimulatorController).GetMethod(actionName)
            ?? throw new InvalidOperationException($"Action {actionName} not found.");
        Assert.Contains(method.GetCustomAttributes<AuthorizeAttribute>(inherit: true), attribute => attribute.Policy == policy);
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

    private sealed class StaticMenuRepository : IMenuQueryRepository
    {
        private readonly IReadOnlyCollection<MenuItem> _items;

        public StaticMenuRepository(IReadOnlyCollection<MenuItem> items)
        {
            _items = items;
        }

        public Task<List<MenuItem>> GetActiveMenuItemsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_items.Where(item => item.IsActive).ToList());
    }
}
