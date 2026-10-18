using System.Net;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.Helpers.DNS.Interfaces;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.External;
using Cfa.ACHInterbank.External.Connections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class ExternalHttpResilienceTests
{
    [Fact]
    public async Task HealthProbe_IdempotentGet_RetriesTransient5xxThenSucceeds()
    {
        var handler = new ScriptedHttpMessageHandler(
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)),
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        using var provider = BuildProvider(handler, healthRetries: 1);
        var probe = provider.GetRequiredService<IHealthProbeClient>();

        var healthy = await probe.IsHealthyAsync("http://localhost/health");

        Assert.True(healthy);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task HealthProbe_IdempotentGet_RetriesConnectionFailureThenSucceeds()
    {
        var handler = new ScriptedHttpMessageHandler(
            _ => Task.FromException<HttpResponseMessage>(new HttpRequestException("synthetic connection refused")),
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        using var provider = BuildProvider(handler, healthRetries: 1);
        var probe = provider.GetRequiredService<IHealthProbeClient>();

        var healthy = await probe.IsHealthyAsync("http://localhost/health");

        Assert.True(healthy);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task HealthProbe_WhenRetriesAreExhausted_ReturnsUnhealthyWithoutExtraAttempt()
    {
        var handler = new ScriptedHttpMessageHandler(
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)),
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        using var provider = BuildProvider(handler, healthRetries: 1);
        var probe = provider.GetRequiredService<IHealthProbeClient>();

        var healthy = await probe.IsHealthyAsync("http://localhost/health");

        Assert.False(healthy);
        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task HealthProbe_TimeoutWithoutRetry_PropagatesUncertainTransportFailure()
    {
        var handler = new ScriptedHttpMessageHandler(async cancellationToken =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var provider = BuildProvider(handler, healthRetries: 0, timeoutSeconds: 1);
        var probe = provider.GetRequiredService<IHealthProbeClient>();

        await Assert.ThrowsAnyAsync<Exception>(
            () => probe.IsHealthyAsync("http://localhost/health"));
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task HealthProbe_CallerCancellation_IsNotConvertedIntoRetry()
    {
        var handler = new ScriptedHttpMessageHandler(async cancellationToken =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var provider = BuildProvider(handler, healthRetries: 2, timeoutSeconds: 5);
        var probe = provider.GetRequiredService<IHealthProbeClient>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => probe.IsHealthyAsync("http://localhost/health", cts.Token));
        Assert.InRange(handler.CallCount, 0, 1);
    }

    [Fact]
    public async Task HealthProbe_CircuitOpen_BlocksFurtherTransportCalls()
    {
        var handler = new ScriptedHttpMessageHandler(
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)),
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)),
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        using var provider = BuildProvider(handler, healthRetries: 0, breakerMinimumThroughput: 2);
        var probe = provider.GetRequiredService<IHealthProbeClient>();

        Assert.False(await probe.IsHealthyAsync("http://localhost/health"));
        Assert.False(await probe.IsHealthyAsync("http://localhost/health"));
        await Assert.ThrowsAnyAsync<Exception>(() => probe.IsHealthyAsync("http://localhost/health"));

        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task GenericPost_WhenNonSuccessBodyIsEmpty_PreservesLegacyDefaultResultWithoutRetry()
    {
        var handler = new ScriptedHttpMessageHandler(
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(string.Empty)
            }));
        using var provider = BuildProvider(
            handler,
            healthRetries: 0,
            clientName: ExternalHttpClientNames.GenericNoRetry);
        var client = provider.GetRequiredService<IHttpClientService>();

        var result = await client.SendPostRequestAsync<Dictionary<string, string>>(
            "http://localhost/token",
            new { client_assertion = "synthetic" },
            HttpMethod.Post,
            TypeBody.Query);

        Assert.Null(result);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task GenericPost_WhenServerReturns5xx_DoesNotRetryAndDoesNotExposeResponseBody()
    {
        const string sensitiveResponse = "SENSITIVE-EXTERNAL-RESPONSE";
        var handler = new ScriptedHttpMessageHandler(
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(sensitiveResponse)
            }));
        var logger = new Mock<ILoggerManager>();
        using var provider = BuildProvider(handler, healthRetries: 0, logger: logger.Object,
            clientName: ExternalHttpClientNames.GenericNoRetry);
        var client = provider.GetRequiredService<IHttpClientService>();

        var error = await Assert.ThrowsAsync<Exception>(() =>
            client.SendPostRequestAsync<Dictionary<string, string>>(
                "http://localhost/token",
                new { client_assertion = "SENSITIVE-ASSERTION" },
                HttpMethod.Post,
                TypeBody.Query,
                "SENSITIVE-BEARER"));

        Assert.Equal(1, handler.CallCount);
        Assert.DoesNotContain(sensitiveResponse, error.Message, StringComparison.Ordinal);
        logger.Verify(x => x.LogInfo(It.Is<string>(message =>
            !message.Contains("SENSITIVE-ASSERTION", StringComparison.Ordinal)
            && !message.Contains("SENSITIVE-BEARER", StringComparison.Ordinal)
            && !message.Contains(sensitiveResponse, StringComparison.Ordinal))), Times.AtLeastOnce);
    }

    private static ServiceProvider BuildProvider(
        ScriptedHttpMessageHandler handler,
        int healthRetries,
        int timeoutSeconds = 5,
        int breakerMinimumThroughput = 5,
        ILoggerManager? logger = null,
        string? clientName = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["appSettings:tokenManager:issuerJwt"] = "synthetic-test-issuer",
            ["appSettings:tokenManager:audienceJwt"] = "synthetic-test-audience",
            ["appSettings:tokenManager:secretKetJwt"] = "synthetic-unit-test-key-32-bytes-minimum",
            ["appSettings:tokenManager:accessExpiration"] = "30",
            ["Resilience:ExternalHttp:HealthProbe:TimeoutSeconds"] = timeoutSeconds.ToString(),
            ["Resilience:ExternalHttp:HealthProbe:MaxRetryAttempts"] = healthRetries.ToString(),
            ["Resilience:ExternalHttp:HealthProbe:RetryBaseDelayMilliseconds"] = "0",
            ["Resilience:ExternalHttp:HealthProbe:CircuitBreakerFailureRatio"] = "0.5",
            ["Resilience:ExternalHttp:HealthProbe:CircuitBreakerSamplingDurationSeconds"] = "30",
            ["Resilience:ExternalHttp:HealthProbe:CircuitBreakerMinimumThroughput"] = breakerMinimumThroughput.ToString(),
            ["Resilience:ExternalHttp:HealthProbe:CircuitBreakerBreakDurationSeconds"] = "30",
            ["Resilience:ExternalHttp:HealthProbe:BulkheadPermitLimit"] = "10",
            ["Resilience:ExternalHttp:HealthProbe:BulkheadQueueLimit"] = "10",
            ["Resilience:ExternalHttp:Generic:TimeoutSeconds"] = "5",
            ["Resilience:ExternalHttp:Generic:MaxRetryAttempts"] = "0",
            ["Resilience:ExternalHttp:Soap:TimeoutSeconds"] = "0",
            ["Resilience:ExternalHttp:Soap:MaxRetryAttempts"] = "0"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILoggerManager>(logger ?? Mock.Of<ILoggerManager>());
        services.AddExternal(configuration);

        var targetName = clientName ?? ExternalHttpClientNames.HealthProbe;
        services.AddHttpClient(targetName)
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        return services.BuildServiceProvider();
    }

    private sealed class ScriptedHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<CancellationToken, Task<HttpResponseMessage>>> _steps;
        private int _callCount;

        public ScriptedHttpMessageHandler(params Func<CancellationToken, Task<HttpResponseMessage>>[] steps)
        {
            _steps = new Queue<Func<CancellationToken, Task<HttpResponseMessage>>>(steps);
        }

        public int CallCount => Volatile.Read(ref _callCount);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _callCount);
            if (_steps.Count == 0)
            {
                throw new InvalidOperationException("No scripted HTTP response remains.");
            }

            return _steps.Dequeue()(cancellationToken);
        }
    }
}
