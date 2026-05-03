using System.Reflection;
using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.External;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Tests.Authorization;

public class P1UsersFineGrainedPolicyMigrationTests
{
    [Fact]
    public void UsersController_ComposicionAuthorizeCorrecta()
    {
        var attrs = typeof(UsersController).GetCustomAttributes<AuthorizeAttribute>(true).ToList();
        Assert.Contains(attrs, a => string.IsNullOrWhiteSpace(a.Policy));

        AssertActionPolicy(nameof(UsersController.GetUsersAsync), P1Policies.UsersRead);
        AssertActionPolicy(nameof(UsersController.ValidateEmailDomainAsync), P1Policies.UsersRead);
        AssertActionPolicy(nameof(UsersController.GetUserAsync), P1Policies.UsersRead);
        AssertActionPolicy(nameof(UsersController.CreateUserAsync), P1Policies.UsersCreate);
        AssertActionPolicy(nameof(UsersController.UpdateUserAsync), P1Policies.UsersUpdate);
        AssertActionPolicy(nameof(UsersController.AssignRolesAsync), P1Policies.UsersAssignRoles);
        AssertActionPolicy(nameof(UsersController.DeactivateUserAsync), P1Policies.UsersDeactivate);

        AssertNoLegacyPolicy(nameof(UsersController.GetUsersAsync));
        AssertNoLegacyPolicy(nameof(UsersController.CreateUserAsync));
        AssertNoLegacyPolicy(nameof(UsersController.UpdateUserAsync));
        AssertNoLegacyPolicy(nameof(UsersController.AssignRolesAsync));
        AssertNoLegacyPolicy(nameof(UsersController.DeactivateUserAsync));
        AssertNoNewAllowAnonymous();
    }

    [Fact]
    public async Task UsersPolicies_CompatibilidadOr()
    {
        await AssertPolicy(P1Policies.UsersRead, FineGrainedPermissions.Users.Read, "CanReadAch", "CanManageAch");
        await AssertPolicy(P1Policies.UsersCreate, FineGrainedPermissions.Users.Create, "CanManageAch", "CanReadAch");
        await AssertPolicy(P1Policies.UsersUpdate, FineGrainedPermissions.Users.Update, "CanManageAch", "CanReadAch");
        await AssertPolicy(P1Policies.UsersDeactivate, FineGrainedPermissions.Users.Deactivate, "CanManageAch", "CanReadAch");
        await AssertPolicy(P1Policies.UsersAssignRoles, FineGrainedPermissions.Users.AssignRoles, "CanManageAch", "CanReadAch");
        await AssertPolicy(P1Policies.UsersManageBranding, FineGrainedPermissions.Users.ManageBranding, "CanManageAch", "CanReadAch");
        await AssertPolicy(P1Policies.UsersManagePasswordRules, FineGrainedPermissions.Users.ManagePasswordRules, "CanManageAch", "CanReadAch");
        await AssertPolicy(P1Policies.UsersManageLockout, FineGrainedPermissions.Users.ManageLockout, "CanManageAch", "CanReadAch");
    }

    private static void AssertActionPolicy(string actionName, string expected)
    {
        var method = typeof(UsersController).GetMethod(actionName);
        var attr = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(expected, attr!.Policy);
    }

    private static void AssertNoLegacyPolicy(string actionName)
    {
        var method = typeof(UsersController).GetMethod(actionName);
        var attrs = method!.GetCustomAttributes<AuthorizeAttribute>(true);
        Assert.DoesNotContain(attrs, a => a.Policy is "CanReadAch" or "CanManageAch");
    }

    private static void AssertNoNewAllowAnonymous()
    {
        var methods = typeof(UsersController).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.DoesNotContain(methods, m => m.GetCustomAttribute<AllowAnonymousAttribute>() is not null);
    }

    private static async Task AssertPolicy(string policy, string fine, string okLegacy, string badLegacy)
    {
        using var p = Provider(); var auth = p.GetRequiredService<IAuthorizationService>();
        Assert.True((await auth.AuthorizeAsync(User(fine), null, policy)).Succeeded);
        Assert.True((await auth.AuthorizeAsync(User(okLegacy), null, policy)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(User(badLegacy), null, policy)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), null, policy)).Succeeded);
    }

    private static ServiceProvider Provider(){var s=new ServiceCollection();var c=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"appSettings:tokenManager:issuerJwt","i"},{"appSettings:tokenManager:audienceJwt","a"},{"appSettings:tokenManager:secretKetJwt","this-is-a-test-secret-key-with-32-bytes"}}).Build();s.AddExternal(c);return s.BuildServiceProvider();}
    private static ClaimsPrincipal User(string p)=>new(new ClaimsIdentity([new Claim("permission",p)],"t"));
}
