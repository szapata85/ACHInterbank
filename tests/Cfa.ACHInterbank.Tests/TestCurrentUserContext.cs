using System.Security.Claims;
using Cfa.ACHInterbank.Application.Security.Interfaces;

namespace Cfa.ACHInterbank.Tests;

internal sealed class TestCurrentUserContext : ICurrentUserContext
{
    public TestCurrentUserContext(IEnumerable<Claim> claims)
    {
        var claimList = claims.ToArray();
        Roles = claimList
            .Where(claim => claim.Type == ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Permissions = claimList
            .Where(claim => claim.Type == "permission")
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyCollection<string> Roles { get; }
    public IReadOnlyCollection<string> Permissions { get; }
}
