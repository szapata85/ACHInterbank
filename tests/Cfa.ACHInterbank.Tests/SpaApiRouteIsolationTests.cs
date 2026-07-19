using System.Reflection;
using Cfa.ACHInterbank.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class SpaApiRouteIsolationTests
{
    [Fact]
    public void SpaCollidingControllers_ShouldExposeApiPrefixedRoutes()
    {
        AssertApiRoute<AchCyclesController>("api/ach-cycles");
        AssertApiRoute<TransactionsController>("api/transactions");
        AssertApiRoute<NavigationController>("api/navigation");
        AssertApiRoute<MenuItemsController>("api/navigation/menu-items");

#pragma warning disable CS0618
        AssertApiRoute<NachaRecordLayoutsController>("api/nacha-layouts");
        AssertApiRoute<NachaRecordDefinitionsController>("api/nacha-record-definitions");
#pragma warning restore CS0618
    }

    private static void AssertApiRoute<TController>(string expectedRoute)
    {
        var routes = typeof(TController).GetCustomAttributes<RouteAttribute>();

        Assert.Contains(routes, route =>
            string.Equals(route.Template, expectedRoute, StringComparison.OrdinalIgnoreCase));
    }
}
