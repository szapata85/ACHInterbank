using System.Reflection;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Tests;

public sealed class NavigationMenuNav33BServiceTestsProductionAuthorizationTests
{
    [Fact]
    public void ServiceTestsController_RequiresTestToolsAccess_WithoutEnvironmentOrLegacyActionPolicies()
    {
        var controllerPolicies = typeof(NachaSoapUatConsoleController)
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Select(attribute => attribute.Policy)
            .ToArray();

        Assert.Contains(FineGrainedPermissions.TestTools.Access, controllerPolicies);
        Assert.Single(controllerPolicies);

        var constructorParameters = typeof(NachaSoapUatConsoleController)
            .GetConstructors()
            .Single()
            .GetParameters()
            .Select(parameter => parameter.ParameterType.Name)
            .ToArray();
        Assert.Equal(new[] { "INachaSoapUatConsoleReadModelService" }, constructorParameters);

        foreach (var actionName in new[]
        {
            nameof(NachaSoapUatConsoleController.GetDashboard),
            nameof(NachaSoapUatConsoleController.GetCandidates),
            nameof(NachaSoapUatConsoleController.GetCandidate),
            nameof(NachaSoapUatConsoleController.GetAudit)
        })
        {
            var method = typeof(NachaSoapUatConsoleController).GetMethod(actionName)
                ?? throw new InvalidOperationException($"Action {actionName} not found.");
            Assert.NotEmpty(method.GetCustomAttributes<HttpGetAttribute>(inherit: true));
            Assert.Empty(method.GetCustomAttributes<AuthorizeAttribute>(inherit: true));
        }
    }

    [Fact]
    public async Task SpaRoute_KeepsExactUrl_AndDoesNotInheritSharedAchAuthorization()
    {
        var root = RepositoryRoot();
        var appRouting = await File.ReadAllTextAsync(Path.Combine(root, "web", "ach-interbank-ui", "src", "app", "app-routing.module.ts"));
        var operationalRouting = await File.ReadAllTextAsync(Path.Combine(root, "web", "ach-interbank-ui", "src", "app", "features", "nacha-operational", "nacha-operational-routing.module.ts"));

        var serviceRoute = Slice(appRouting, "path: 'ach/nacha/soap-uat-console'", "path: 'ach'");
        Assert.Contains("canActivate: [permissionGuard]", serviceRoute);
        Assert.Contains("permissions: ['TestTools.Access']", serviceRoute);
        Assert.DoesNotContain("roleGuard", serviceRoute);
        Assert.DoesNotContain("roles:", serviceRoute);
        Assert.DoesNotContain("CanReadAch", serviceRoute);
        Assert.DoesNotContain("CanManageAch", serviceRoute);
        Assert.Contains("NachaSoapUatConsoleComponent", serviceRoute);

        Assert.DoesNotContain("path: 'nacha/soap-uat-console'", operationalRouting);

        var achParent = Slice(appRouting, "path: 'ach'", "path: 'payment-rail-capability-registry'");
        Assert.Contains("canActivate: [roleGuard, permissionGuard]", achParent);
        Assert.Contains("roles: ['Admin', 'ACH.Operator']", achParent);
        Assert.Contains("permissions: ['CanReadAch']", achParent);
    }

    [Fact]
    public async Task Console_RemainsReadOnly_AndLiveContrapartidasSurfaceStaysGuardedAndSeparate()
    {
        var root = RepositoryRoot();
        var spaService = await File.ReadAllTextAsync(Path.Combine(root, "web", "ach-interbank-ui", "src", "app", "features", "nacha-operational", "services", "nacha-soap-uat-console.service.ts"));
        var controller = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Api", "Controllers", "NachaSoapUatConsoleController.cs"));
        var readModelService = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Persistence", "ACH", "Services", "Implementation", "NachaSoapUatConsoleReadModelService.cs"));
        var readStore = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Persistence", "ACH", "Services", "Implementation", "NachaOperationalReadStore.cs"));
        var uatContrapartidas = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Api", "Controllers", "UatContrapartidasController.cs"));

        Assert.Contains("api/ach/nacha/soap-uat-console", spaService);
        Assert.Contains("this.api.get", spaService);
        Assert.DoesNotContain("this.api.post", spaService);
        Assert.DoesNotContain("this.api.put", spaService);
        Assert.DoesNotContain("this.api.patch", spaService);
        Assert.DoesNotContain("this.api.delete", spaService);

        Assert.DoesNotContain("IsProduction", controller);
        Assert.DoesNotContain("IsUatLike", controller);
        Assert.DoesNotContain("UAT_FEATURE_UNAVAILABLE", controller);
        Assert.DoesNotContain("NachaSimulatorRead", controller);

        Assert.Contains("INachaOperationalReadStore", readModelService);
        Assert.Contains("ProductiveExecution = false", readModelService);
        Assert.Contains("WouldInvokeRealSoap = false", readModelService);
        Assert.DoesNotContain("IContrapartidaDispatchJobService", readModelService);
        Assert.DoesNotContain("ProcessTransactionAsync", readModelService);
        Assert.DoesNotContain("ProcessCycleAsync", readModelService);
        Assert.DoesNotContain("SaveChanges", readModelService);

        Assert.Contains("AsNoTracking()", readStore);
        Assert.DoesNotContain("SaveChanges", readStore);

        Assert.Contains("[Route(\"api/uat/contrapartidas\")]", uatContrapartidas);
        Assert.Contains("IContrapartidaDispatchJobService", uatContrapartidas);
        Assert.Contains("ProcessTransactionAsync", uatContrapartidas);
        Assert.Contains("_hostEnvironment.IsProduction()", uatContrapartidas);
        Assert.Contains("ACH_SOAP_LIVE_TESTS", uatContrapartidas);
    }

    [Fact]
    public async Task MenuActivation_HappensOnlyInPostNav2TestToolsAuthorizationReconciler()
    {
        var root = RepositoryRoot();
        var canonicalSeeder = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Persistence", "Navigation", "Seeders", "NavigationInformationArchitectureSeeder.cs"));
        var authorizationSeeder = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Persistence", "Security", "Seeders", "TestToolsMenuAuthorizationSeeder.cs"));
        var grantSeeder = await File.ReadAllTextAsync(Path.Combine(root, "src", "Cfa.ACHInterbank.Persistence", "Security", "Seeders", "TestToolsAdminRolePermissionSeeder.cs"));

        Assert.Contains("serviceTests.IsActive = false;", canonicalSeeder);
        Assert.Contains("RequireTestToolsAccessOnlyAsync(serviceTests.Id)", authorizationSeeder);
        Assert.Contains("serviceTests.IsActive = true;", authorizationSeeder);
        Assert.Contains("context.MenuItemRoles.RemoveRange(roleLinks);", authorizationSeeder);

        Assert.Contains("RoleConfiguration.AdminRoleId", grantSeeder);
        Assert.DoesNotContain("RoleConfiguration.OperatorRoleId", grantSeeder);
        Assert.DoesNotContain("UserId", grantSeeder, StringComparison.Ordinal);
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
}
