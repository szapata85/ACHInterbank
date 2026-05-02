using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.External;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Tests.Authorization;

public class FineGrainedPermissionsRegistrationTests
{
    [Fact]
    public void AllPermissions_NoDebeEstarVacio_NiDuplicado_YContenerCriticos()
    {
        Assert.NotEmpty(FineGrainedPermissions.AllPermissions);
        Assert.Equal(FineGrainedPermissions.AllPermissions.Count, FineGrainedPermissions.AllPermissions.Distinct(StringComparer.Ordinal).Count());

        Assert.Contains(FineGrainedPermissions.Transactions.Read, FineGrainedPermissions.AllPermissions);
        Assert.Contains(FineGrainedPermissions.Transactions.Create, FineGrainedPermissions.AllPermissions);
        Assert.Contains(FineGrainedPermissions.Nacha.Upload, FineGrainedPermissions.AllPermissions);
        Assert.Contains(FineGrainedPermissions.NachaSecurity.ManualDecrypt, FineGrainedPermissions.AllPermissions);
        Assert.Contains(FineGrainedPermissions.Certificates.Activate, FineGrainedPermissions.AllPermissions);
        Assert.Contains(FineGrainedPermissions.CommandCenter.MarkFailedFinal, FineGrainedPermissions.AllPermissions);
        Assert.Contains(FineGrainedPermissions.Users.AssignRoles, FineGrainedPermissions.AllPermissions);
        Assert.Contains(FineGrainedPermissions.Maintenance.Seed, FineGrainedPermissions.AllPermissions);
    }

    [Fact]
    public void PermisosLegacy_CanReadAch_Y_CanManageAch_SiguenExistiendo()
    {
        const string canReadAch = "CanReadAch";
        const string canManageAch = "CanManageAch";

        Assert.False(string.IsNullOrWhiteSpace(canReadAch));
        Assert.False(string.IsNullOrWhiteSpace(canManageAch));
    }

    [Fact]
    public void AddExternal_DebeRegistrarTodasLasPoliciesFinas_DeAllPermissions()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["appSettings:tokenManager:issuerJwt"] = "issuer-test",
                ["appSettings:tokenManager:audienceJwt"] = "audience-test",
                ["appSettings:tokenManager:secretKetJwt"] = "this-is-a-test-secret-key-with-32-bytes"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddExternal(config);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        Assert.NotNull(options.GetPolicy("CanReadAch"));
        Assert.NotNull(options.GetPolicy("CanManageAch"));

        foreach (var permission in FineGrainedPermissions.AllPermissions)
        {
            var policy = options.GetPolicy(permission);
            Assert.NotNull(policy);
            Assert.Contains(policy!.Requirements, r =>
                r is ClaimsAuthorizationRequirement claimReq
                && claimReq.ClaimType == "permission"
                && claimReq.AllowedValues is not null
                && claimReq.AllowedValues.Contains(permission));
        }
    }
}
