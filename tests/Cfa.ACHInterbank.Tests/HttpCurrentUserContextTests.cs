using System.Security.Claims;
using Cfa.ACHInterbank.Api.Security;
using Microsoft.AspNetCore.Http;

namespace Cfa.ACHInterbank.Tests;

public sealed class HttpCurrentUserContextTests
{
    [Fact]
    public void Context_ShouldExposeRolesAndPermissionsFromCurrentPrincipal()
    {
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim("permission", "CanReadAch"),
                    new Claim("permission", "CanReadAch")
                ], "test"))
            }
        };

        var context = new HttpCurrentUserContext(accessor);

        Assert.Equal(["Admin"], context.Roles);
        Assert.Equal(["CanReadAch"], context.Permissions);
    }

    [Fact]
    public void Context_ShouldFailClosedToEmptyCollectionsWithoutHttpContext()
    {
        var context = new HttpCurrentUserContext(new HttpContextAccessor());

        Assert.Empty(context.Roles);
        Assert.Empty(context.Permissions);
    }
}
