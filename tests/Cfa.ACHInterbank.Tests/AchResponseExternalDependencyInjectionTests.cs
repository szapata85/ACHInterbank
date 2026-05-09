using Cfa.ACHInterbank.Application.ACH.Responses.Interfaces;
using Cfa.ACHInterbank.External;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Tests;

public class AchResponseExternalDependencyInjectionTests
{
    [Fact]
    public void AddExternal_ShouldRegisterRespuestaTransaccionesAchGateway()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["appSettings:tokenManager:issuerJwt"] = "issuer",
            ["appSettings:tokenManager:audienceJwt"] = "audience",
            ["appSettings:tokenManager:secretKetJwt"] = "this-is-a-test-secret-key-with-32-bytes"
        }).Build();

        services.AddExternal(configuration);

        var provider = services.BuildServiceProvider();
        var gateway = provider.GetService<IRespuestaTransaccionesAchGateway>();

        gateway.Should().NotBeNull();
    }
}
