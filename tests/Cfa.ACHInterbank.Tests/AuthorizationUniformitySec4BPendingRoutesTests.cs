using System.Reflection;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class AuthorizationUniformitySec4BPendingRoutesTests
{
    [Fact]
    public void Branding_Get_MantieneAllowAnonymousExplicito()
    {
        var method = typeof(BrandingController).GetMethod(nameof(BrandingController.GetBrandingAsync));
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void Branding_Put_RequiereCanManageAchYSinAllowAnonymous()
    {
        var method = typeof(BrandingController).GetMethod(nameof(BrandingController.SaveBrandingAsync));
        Assert.NotNull(method);
        Assert.Null(method!.GetCustomAttribute<AllowAnonymousAttribute>());
        var authorize = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal("CanManageAch", authorize!.Policy);
    }

    [Fact]
    public void NachaHeader_Post_RequiereCanManageAchYSinAllowAnonymous()
    {
        Assert.NotNull(typeof(NachaController).GetCustomAttribute<AuthorizeAttribute>());
        Assert.Null(typeof(NachaController).GetCustomAttribute<AllowAnonymousAttribute>());

        var method = typeof(NachaController).GetMethod(nameof(NachaController.SaveHeader));
        Assert.NotNull(method);
        Assert.Null(method!.GetCustomAttribute<AllowAnonymousAttribute>());
        var authorize = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorize);
        Assert.Equal(P1Policies.NachaGenerate, authorize!.Policy);
    }
}
