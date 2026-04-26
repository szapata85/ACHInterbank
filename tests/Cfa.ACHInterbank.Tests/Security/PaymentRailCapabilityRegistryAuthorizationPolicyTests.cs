using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.External;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Cfa.ACHInterbank.Tests.Security;

public class PaymentRailCapabilityRegistryAuthorizationPolicyTests
{
    [Theory]
    [InlineData(FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry, true)]
    [InlineData("CanManageAch", true)]
    [InlineData("CanReadAch", true)]
    [InlineData("CanReadCatalogs", false)]
    public async Task CanViewPaymentRailCapabilityRegistryPolicy_AppliesExpectedFallback(string permission, bool expected)
    {
        AppSettings.Settings = new AppSettings
        {
            TokenManager = new Token
            {
                secretKetJwt = "test-super-secret-012345678901234567890123",
                issuerJwt = "issuer-test",
                audienceJwt = "audience-test"
            }
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["appSettings:tokenManager:secretKetJwt"] = "test-super-secret-012345678901234567890123",
                ["appSettings:tokenManager:issuerJwt"] = "issuer-test",
                ["appSettings:tokenManager:audienceJwt"] = "audience-test"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExternal(config);

        await using var provider = services.BuildServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("permission", permission)
        ], authenticationType: "unit-test"));

        var authResult = await authorizationService.AuthorizeAsync(principal, resource: null, FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry);

        authResult.Succeeded.Should().Be(expected);
    }
}
