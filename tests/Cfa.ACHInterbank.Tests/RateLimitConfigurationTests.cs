using Cfa.ACHInterbank.Api.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cfa.ACHInterbank.Tests;

public sealed class RateLimitConfigurationTests
{
    [Fact]
    public void Uses_safe_production_defaults_when_section_is_absent()
    {
        var configuration = new ConfigurationBuilder().Build();

        var options = ApiRateLimitingOptions.FromConfiguration(configuration);

        Assert.Equal(10, options.PermitLimit);
        Assert.Equal(1, options.WindowSeconds);
        Assert.Equal(2, options.QueueLimit);
    }

    [Fact]
    public void Binds_bounded_scheduler_cluster_profile()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:PermitLimit"] = "60",
                ["RateLimiting:WindowSeconds"] = "1",
                ["RateLimiting:QueueLimit"] = "20"
            })
            .Build();

        var options = ApiRateLimitingOptions.FromConfiguration(configuration);

        Assert.Equal(60, options.PermitLimit);
        Assert.Equal(1, options.WindowSeconds);
        Assert.Equal(20, options.QueueLimit);
    }

    [Theory]
    [InlineData("PermitLimit", "0")]
    [InlineData("WindowSeconds", "0")]
    [InlineData("QueueLimit", "-1")]
    public void Rejects_unbounded_or_invalid_values(string key, string value)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"RateLimiting:{key}"] = value
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
            ApiRateLimitingOptions.FromConfiguration(configuration));
    }
}
