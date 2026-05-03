using System.Reflection;
using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.External;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Tests.Authorization;

public class P1RolesPermissionsFineGrainedPolicyMigrationTests
{
    [Fact]
    public void RolesPermissionsControllers_ComposicionAuthorizeCorrecta()
    {
        AssertClassAuthorizeWithoutPolicy<RolesController>();
        AssertClassAuthorizeWithoutPolicy<PermissionsController>();

        AssertActionPolicy<RolesController>(nameof(RolesController.GetRolesAsync), P1Policies.RolesRead);
        AssertActionPolicy<PermissionsController>(nameof(PermissionsController.GetPermissionsAsync), P1Policies.PermissionsRead);

        AssertActionDoesNotUseLegacyPermissions<RolesController>(nameof(RolesController.GetRolesAsync));
        AssertActionDoesNotUseLegacyPermissions<PermissionsController>(nameof(PermissionsController.GetPermissionsAsync));

        AssertNoAllowAnonymous<RolesController>();
        AssertNoAllowAnonymous<PermissionsController>();
    }

    [Fact]
    public async Task RolesPermissionsPolicies_CompatibilidadOr()
    {
        await AssertPolicy(P1Policies.RolesRead, FineGrainedPermissions.Roles.Read, "CanReadAch", "CanManageAch");
        await AssertPolicy(P1Policies.RolesCreate, FineGrainedPermissions.Roles.Create, "CanManageAch", "CanReadAch");
        await AssertPolicy(P1Policies.RolesUpdate, FineGrainedPermissions.Roles.Update, "CanManageAch", "CanReadAch");
        await AssertPolicy(P1Policies.RolesDelete, FineGrainedPermissions.Roles.Delete, "CanManageAch", "CanReadAch");
        await AssertPolicy(P1Policies.PermissionsRead, FineGrainedPermissions.Permissions.Read, "CanReadAch", "CanManageAch");
        await AssertPolicy(P1Policies.PermissionsAssign, FineGrainedPermissions.Permissions.Assign, "CanManageAch", "CanReadAch");
    }

    private static async Task AssertPolicy(string policy, string fine, string okLegacy, string badLegacy)
    {
        using var p = Provider();
        var auth = p.GetRequiredService<IAuthorizationService>();
        Assert.True((await auth.AuthorizeAsync(User(fine), null, policy)).Succeeded);
        Assert.True((await auth.AuthorizeAsync(User(okLegacy), null, policy)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(User(badLegacy), null, policy)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), null, policy)).Succeeded);
    }

    private static void AssertClassAuthorizeWithoutPolicy<TController>()
    {
        var attrs = typeof(TController).GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToList();
        Assert.NotEmpty(attrs);
        Assert.Contains(attrs, a => string.IsNullOrWhiteSpace(a.Policy));
    }

    private static void AssertActionPolicy<TController>(string actionName, string expectedPolicy)
    {
        var method = typeof(TController).GetMethod(actionName);
        var attr = method!.GetCustomAttributes<AuthorizeAttribute>(true).FirstOrDefault();
        Assert.NotNull(attr);
        Assert.Equal(expectedPolicy, attr!.Policy);
    }

    private static void AssertActionDoesNotUseLegacyPermissions<TController>(string actionName)
    {
        var method = typeof(TController).GetMethod(actionName);
        var attrs = method!.GetCustomAttributes<AuthorizeAttribute>(true);
        Assert.DoesNotContain(attrs, a => a.Policy is "CanReadAch" or "CanManageAch");
    }

    private static void AssertNoAllowAnonymous<TController>()
    {
        var methods = typeof(TController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.DoesNotContain(methods, m => m.GetCustomAttribute<AllowAnonymousAttribute>() is not null);
    }

    private static ServiceProvider Provider(){var s=new ServiceCollection();var c=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"appSettings:tokenManager:issuerJwt","i"},{"appSettings:tokenManager:audienceJwt","a"},{"appSettings:tokenManager:secretKetJwt","this-is-a-test-secret-key-with-32-bytes"}}).Build();s.AddExternal(c);return s.BuildServiceProvider();}
    private static ClaimsPrincipal User(string p)=>new(new ClaimsIdentity([new Claim("permission",p)],"t"));
}
