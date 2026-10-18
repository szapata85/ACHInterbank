using System.Security.Claims;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Cfa.ACHInterbank.Api.Security;

public sealed class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public IReadOnlyCollection<string> Roles
    {
        get
        {
            var principal = CurrentPrincipal;
            var roleClaimType = (principal.Identity as ClaimsIdentity)?.RoleClaimType ?? ClaimTypes.Role;
            return principal.FindAll(roleClaimType)
                .Select(claim => claim.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public IReadOnlyCollection<string> Permissions
        => CurrentPrincipal.FindAll("permission")
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private ClaimsPrincipal CurrentPrincipal
        => _httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();
}
