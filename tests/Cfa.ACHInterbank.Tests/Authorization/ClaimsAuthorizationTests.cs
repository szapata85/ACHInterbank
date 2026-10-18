using System.Reflection;
using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.External;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Tests.Authorization;

public sealed class ClaimsAuthorizationTests
{
    [Fact]
    public void AchClaimsController_UsesExpectedFineGrainedPolicies()
    {
        var classAuthorize = typeof(AchClaimsController).GetCustomAttributes<AuthorizeAttribute>(inherit: true).ToArray();
        Assert.Contains(classAuthorize, x => string.IsNullOrWhiteSpace(x.Policy));

        AssertPolicy(nameof(AchClaimsController.CreateDev14), P0Policies.ClaimsCreateDev14);
        AssertPolicy(nameof(AchClaimsController.Query), P0Policies.ClaimsRead);
        AssertPolicy(nameof(AchClaimsController.GetById), P0Policies.ClaimsRead);
        AssertPolicy(nameof(AchClaimsController.GetByTransaction), P0Policies.ClaimsRead);
        AssertPolicy(nameof(AchClaimsController.RecordProgress), P0Policies.ClaimsProgress);
        AssertPolicy(nameof(AchClaimsController.Resolve), P0Policies.ClaimsResolve);
        AssertPolicy(nameof(AchClaimsController.Expire), P0Policies.ClaimsExpire);
    }

    [Fact]
    public async Task ClaimsRead_AllowsOnlyClaimsReadPermission()
    {
        using var provider = Provider();
        var auth = provider.GetRequiredService<IAuthorizationService>();

        Assert.True((await auth.AuthorizeAsync(User(FineGrainedPermissions.Claims.Read), null, P0Policies.ClaimsRead)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(User("CanReadAch"), null, P0Policies.ClaimsRead)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(User("CanManageAch"), null, P0Policies.ClaimsRead)).Succeeded);
    }

    [Theory]
    [InlineData(P0Policies.ClaimsCreateDev14, FineGrainedPermissions.Claims.CreateDev14)]
    [InlineData(P0Policies.ClaimsProgress, FineGrainedPermissions.Claims.Progress)]
    [InlineData(P0Policies.ClaimsResolve, FineGrainedPermissions.Claims.Resolve)]
    [InlineData(P0Policies.ClaimsExpire, FineGrainedPermissions.Claims.Expire)]
    public async Task ClaimsMutations_AllowOnlyFinePermission(
        string policy,
        string finePermission)
    {
        using var provider = Provider();
        var auth = provider.GetRequiredService<IAuthorizationService>();

        Assert.True((await auth.AuthorizeAsync(User(finePermission), null, policy)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(User("CanManageAch"), null, policy)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(User("CanReadAch"), null, policy)).Succeeded);
    }

    [Fact]
    public void ClaimsPermissions_AreRegisteredInAllPermissions()
    {
        Assert.Contains(FineGrainedPermissions.Claims.Read, FineGrainedPermissions.AllPermissions);
        Assert.Contains(FineGrainedPermissions.Claims.CreateDev14, FineGrainedPermissions.AllPermissions);
        Assert.Contains(FineGrainedPermissions.Claims.Progress, FineGrainedPermissions.AllPermissions);
        Assert.Contains(FineGrainedPermissions.Claims.Resolve, FineGrainedPermissions.AllPermissions);
        Assert.Contains(FineGrainedPermissions.Claims.Expire, FineGrainedPermissions.AllPermissions);
    }

    private static void AssertPolicy(string methodName, string expectedPolicy)
    {
        var method = typeof(AchClaimsController).GetMethod(methodName);
        Assert.NotNull(method);
        var authorize = Assert.Single(method!.GetCustomAttributes<AuthorizeAttribute>(inherit: true));
        Assert.Equal(expectedPolicy, authorize.Policy);
        Assert.NotEqual("CanReadAch", authorize.Policy);
        Assert.NotEqual("CanManageAch", authorize.Policy);
    }

    private static ServiceProvider Provider()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["appSettings:tokenManager:issuerJwt"] = "issuer",
                ["appSettings:tokenManager:audienceJwt"] = "audience",
                ["appSettings:tokenManager:secretKetJwt"] = "this-is-a-test-secret-key-with-32-bytes"
            })
            .Build();
        services.AddExternal(configuration);
        return services.BuildServiceProvider();
    }

    private static ClaimsPrincipal User(string permission)
        => new(new ClaimsIdentity([new Claim("permission", permission)], "test"));
}
