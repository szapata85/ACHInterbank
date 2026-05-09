using System.Reflection;
using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.External;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Tests.Authorization;

public class P1ConfigLoginLockoutPolicyMigrationTests
{
    [Fact]
    public void LoginLockoutSettingsController_ComposicionAuthorizeCorrecta()
    {
        AssertControllerAuthorizeComposition<LoginLockoutSettingsController>(
            nameof(LoginLockoutSettingsController.GetAsync),
            nameof(LoginLockoutSettingsController.SaveAsync));
    }

    [Fact]
    public void SoapIntegrationSettingsController_ComposicionAuthorizeCorrecta()
    {
        AssertControllerAuthorizeComposition<SoapIntegrationSettingsController>(
            nameof(SoapIntegrationSettingsController.GetAsync),
            nameof(SoapIntegrationSettingsController.SaveAsync));
    }

    [Fact]
    public async Task ConfigPolicies_CompatibilidadOr()
    {
        await AssertPolicy(P1Policies.ConfigRead, FineGrainedPermissions.Config.Read, "CanReadAch", "CanManageAch");
        await AssertPolicy(P1Policies.ConfigManage, FineGrainedPermissions.Config.Manage, "CanManageAch", "CanReadAch");
    }

    private static void AssertControllerAuthorizeComposition<TController>(string getActionName, string writeActionName)
    {
        var attrs = typeof(TController).GetCustomAttributes<AuthorizeAttribute>(true).ToList();
        Assert.Contains(attrs, a => string.IsNullOrWhiteSpace(a.Policy));

        AssertActionPolicy<TController>(getActionName, P1Policies.ConfigRead);
        AssertActionPolicy<TController>(writeActionName, P1Policies.ConfigManage);

        AssertNoLegacyPolicy<TController>(getActionName);
        AssertNoLegacyPolicy<TController>(writeActionName);
        AssertNoNewAllowAnonymous<TController>();
    }

    private static void AssertActionPolicy<TController>(string actionName, string expected)
    {
        var method = typeof(TController).GetMethod(actionName);
        var attr = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(expected, attr!.Policy);
    }

    private static void AssertNoLegacyPolicy<TController>(string actionName)
    {
        var method = typeof(TController).GetMethod(actionName);
        var attrs = method!.GetCustomAttributes<AuthorizeAttribute>(true);
        Assert.DoesNotContain(attrs, a => a.Policy is "CanReadAch" or "CanManageAch");
    }

    private static void AssertNoNewAllowAnonymous<TController>()
    {
        var methods = typeof(TController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.DoesNotContain(methods, m => m.GetCustomAttribute<AllowAnonymousAttribute>() is not null);
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

    private static ServiceProvider Provider(){var s=new ServiceCollection();var c=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"appSettings:tokenManager:issuerJwt","i"},{"appSettings:tokenManager:audienceJwt","a"},{"appSettings:tokenManager:secretKetJwt","this-is-a-test-secret-key-with-32-bytes"}}).Build();s.AddExternal(c);return s.BuildServiceProvider();}
    private static ClaimsPrincipal User(string p)=>new(new ClaimsIdentity([new Claim("permission",p)],"t"));
}
