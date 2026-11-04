using System.Collections.Generic;
using System.Net;
using Cfa.ACHInterbank.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class RateLimitClientIdentitySecI05Tests
{
    [Fact]
    public void UntrustedProxyCannotOverrideSocketIdentity()
    {
        var ctx = CreateContext("203.0.113.10", "198.51.100.123", isDevelopment: false);
        Assert.Equal("203.0.113.10", RateLimitClientIdentity.GetClientKey(ctx));
    }

    [Fact]
    public void ExplicitlyTrustedProxyUsesValidatedClientAddress()
    {
        var ctx = CreateContext("192.0.2.9", "198.51.100.1", isDevelopment: false,
            trustedProxies: new[] { "192.0.2.9" });
        Assert.Equal("198.51.100.1", RateLimitClientIdentity.GetClientKey(ctx));
    }

    [Fact]
    public void InvalidForwardedAddressFallsBackToSocketIdentity()
    {
        var ctx = CreateContext("192.0.2.9", "not-an-ip", isDevelopment: false,
            trustedProxies: new[] { "192.0.2.9" });
        Assert.Equal("192.0.2.9", RateLimitClientIdentity.GetClientKey(ctx));
    }

    private static DefaultHttpContext CreateContext(string remote, string forwarded,
        bool isDevelopment, string[]? trustedProxies = null)
    {
        var values = new Dictionary<string, string?>();
        if (trustedProxies is not null)
        {
            for (var i = 0; i < trustedProxies.Length; i++)
                values[$"Security:TrustedReverseProxyIps:{i}"] = trustedProxies[i];
        }
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<IHostEnvironment>(new TestHostEnvironment(isDevelopment ? Environments.Development : Environments.Production))
            .BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Connection.RemoteIpAddress = IPAddress.Parse(remote);
        context.Request.Headers["X-Forwarded-For"] = forwarded;
        return context;
    }

    private sealed class TestHostEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = "/tmp";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
