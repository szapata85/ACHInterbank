using System.Reflection;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Tests.Authorization;

public class P1ConfigClearingHouseCycleConfigsPolicyMigrationTests
{
    [Fact]
    public void ClearingHouseCycleConfigsController_ComposicionAuthorizeCorrecta()
    {
        var controllerAttrs = typeof(ClearingHouseCycleConfigsController).GetCustomAttributes<AuthorizeAttribute>(true).ToList();
        Assert.Contains(controllerAttrs, a => string.IsNullOrWhiteSpace(a.Policy));

        var methods = typeof(ClearingHouseCycleConfigsController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        foreach (var method in methods)
        {
            var hasGet = method.GetCustomAttribute<HttpGetAttribute>() is not null;
            var hasWrite = method.GetCustomAttribute<HttpPostAttribute>() is not null
                || method.GetCustomAttribute<HttpPutAttribute>() is not null
                || method.GetCustomAttribute<HttpPatchAttribute>() is not null
                || method.GetCustomAttribute<HttpDeleteAttribute>() is not null;
            if (!hasGet && !hasWrite) continue;

            var attrs = method.GetCustomAttributes<AuthorizeAttribute>(true).ToList();
            Assert.DoesNotContain(attrs, a => a.Policy is "CanReadAch" or "CanManageAch");

            var expected = hasWrite
                ? FineGrainedPermissions.ClearingHouses.ManageCycles
                : FineGrainedPermissions.ClearingHouses.View;
            Assert.Contains(attrs, a => a.Policy == expected);
        }

        Assert.DoesNotContain(methods, m => m.GetCustomAttribute<AllowAnonymousAttribute>() is not null);
    }
}
