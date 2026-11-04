using System.Net;
using Cfa.ACHInterbank.Api.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Cfa.ACHInterbank.Tests.Security;

/// <summary>
/// SEC-I06A: tests aislados del middleware HTTP. No inicia la API real, ni conecta a BD,
/// cámaras, MFT, CFA o servicios externos. WRITTEN_NOT_EXECUTED hasta disponer de .NET 10.
/// </summary>
public sealed class SecI06AHeadersRegressionTests
{
    [Theory]
    [InlineData("Production", "https://localhost/security-check", true)]
    [InlineData("Production", "http://localhost/security-check", false)]
    [InlineData("Development", "https://localhost/security-check", false)]
    public async Task HstsOnlyOnHttpsInNonDevelopment(string environment, string url, bool expectsHsts)
    {
        using var server = CreateIsolatedServer(environment);
        using var client = server.CreateClient();
        using var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(expectsHsts, response.Headers.Contains("Strict-Transport-Security"));
        if (expectsHsts)
        {
            Assert.Equal("max-age=31536000",
                Assert.Single(response.Headers.GetValues("Strict-Transport-Security")));
        }
    }

    [Fact]
    public async Task ProductionResponseEmitsMinimumBrowserHeadersAndStripsBanner()
    {
        using var server = CreateIsolatedServer("Production");
        using var client = server.CreateClient();
        using var response = await client.GetAsync("http://localhost/synthetic");

        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("strict-origin-when-cross-origin",
            Assert.Single(response.Headers.GetValues("Referrer-Policy")));
        var csp = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("default-src 'self'", csp);
        Assert.Contains("frame-ancestors 'self'", csp);
        Assert.False(response.Headers.Contains("X-Powered-By"));
        Assert.False(response.Headers.Contains("X-AspNet-Version"));
    }

    private static TestServer CreateIsolatedServer(string environment)
    {
        var builder = new WebHostBuilder()
            .UseEnvironment(environment)
            .Configure(app =>
            {
                app.UseMiddleware<SecurityHeadersMiddleware>();
                app.Run(async context =>
                {
                    // Respuesta 100 % sintética; el middleware debe limpiar las cabeceras.
                    context.Response.Headers["X-Powered-By"] = "synthetic-server";
                    context.Response.Headers["X-AspNet-Version"] = "synthetic-version";
                    await context.Response.WriteAsync("synthetic-ok");
                });
            });

        return new TestServer(builder);
    }
}
