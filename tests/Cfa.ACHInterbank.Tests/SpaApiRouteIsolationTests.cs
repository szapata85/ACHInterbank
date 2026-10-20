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

        AssertCanonicalOnlyRoute<NachaRecordLayoutsController>("nacha-layouts", "api/nacha-layouts");
        AssertCanonicalOnlyRoute<NachaRecordDefinitionsController>("nacha-record-definitions", "api/nacha-record-definitions");
    }

    private static void AssertCanonicalOnlyRoute<TController>(string canonicalRoute, string removedAlias)
    {
        var routes = typeof(TController).GetCustomAttributes<RouteAttribute>().ToArray();

        Assert.Contains(routes, route =>
            string.Equals(route.Template, canonicalRoute, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(routes, route =>
            string.Equals(route.Template, removedAlias, StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertApiRoute<TController>(string expectedRoute)
    {
        var routes = typeof(TController).GetCustomAttributes<RouteAttribute>();

        Assert.Contains(routes, route =>
            string.Equals(route.Template, expectedRoute, StringComparison.OrdinalIgnoreCase));
    }
}
