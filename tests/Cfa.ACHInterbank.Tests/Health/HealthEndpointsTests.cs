using Xunit;

namespace Cfa.ACHInterbank.Tests.Health;

public class HealthEndpointsTests
{
    private static string ResolveBootstrapFile()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            var candidate = Path.Combine(dir, "src", "Cfa.ACHInterbank.Api", "DependencyInjectionService.cs");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
        }

        throw new FileNotFoundException("No se encontró DependencyInjectionService.cs desde AppContext.BaseDirectory.");
    }

    [Fact]
    public void LiveEndpoint_IsMappedAsAnonymous()
    {
        var content = File.ReadAllText(ResolveBootstrapFile());
        Assert.Contains("MapGet(\"/health/live\"", content);
        Assert.Contains("/health/live", content);
        Assert.Contains("}).AllowAnonymous();", content);
    }

    [Fact]
    public void ReadyEndpoint_IsMappedAsAnonymous()
    {
        var content = File.ReadAllText(ResolveBootstrapFile());
        Assert.Contains("MapGet(\"/health/ready\"", content);
        Assert.Contains("check = \"ready\"", content);
        Assert.Contains("Status503ServiceUnavailable", content);
    }
}
