using Cfa.ACHInterbank.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Json;

namespace Cfa.ACHInterbank.Tests;

public sealed class CorsPolicyIntegrationTests
{
    private const string LocalSpaOrigin = "http://localhost:743";
    private const string AngularDevOrigin = "http://localhost:4200";
    private const string UnauthorizedOrigin = "http://origen-no-autorizado.invalid";

    [Fact]
    public async Task BrandingGet_FromConfiguredLocalSpa_ReturnsExactCorsHeaders()
    {
        await using var app = await CreateAppAsync([LocalSpaOrigin, AngularDevOrigin]);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/branding");
        request.Headers.Add("Origin", LocalSpaOrigin);

        using var response = await app.GetTestClient().SendAsync(request);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle(LocalSpaOrigin);
        response.Headers.GetValues("Access-Control-Allow-Credentials").Should().ContainSingle("true");
        response.Headers.Vary.Should().Contain("Origin");
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().NotContain("*");
    }

    [Fact]
    public async Task BrandingPreflight_FromConfiguredLocalSpa_IsHandledWithoutAuthentication()
    {
        await using var app = await CreateAppAsync([LocalSpaOrigin, AngularDevOrigin]);
        using var request = CreatePreflight("/api/users/branding", LocalSpaOrigin, "GET");

        using var response = await app.GetTestClient().SendAsync(request);

        response.StatusCode.Should().BeOneOf(System.Net.HttpStatusCode.OK, System.Net.HttpStatusCode.NoContent);
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle(LocalSpaOrigin);
        response.Headers.GetValues("Access-Control-Allow-Methods").Should().Contain(value => value.Contains("GET"));
        response.Headers.GetValues("Access-Control-Allow-Headers").Should().Contain(value =>
            value.Contains("authorization", StringComparison.OrdinalIgnoreCase)
            && value.Contains("content-type", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_PreflightAndPost_FromConfiguredLocalSpa_ReturnCorsHeaders()
    {
        await using var app = await CreateAppAsync([LocalSpaOrigin, AngularDevOrigin]);
        var client = app.GetTestClient();

        using var preflight = CreatePreflight("/Auth/login", LocalSpaOrigin, "POST");
        using var preflightResponse = await client.SendAsync(preflight);

        preflightResponse.StatusCode.Should().BeOneOf(System.Net.HttpStatusCode.OK, System.Net.HttpStatusCode.NoContent);
        preflightResponse.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle(LocalSpaOrigin);

        using var post = new HttpRequestMessage(HttpMethod.Post, "/Auth/login")
        {
            Content = JsonContent.Create(new { userName = "synthetic", password = "synthetic" })
        };
        post.Headers.Add("Origin", LocalSpaOrigin);
        using var postResponse = await client.SendAsync(post);

        postResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        postResponse.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle(LocalSpaOrigin);
    }

    [Fact]
    public async Task UnknownOrigin_IsNotReflected()
    {
        await using var app = await CreateAppAsync([LocalSpaOrigin, AngularDevOrigin]);
        using var request = CreatePreflight("/api/users/branding", UnauthorizedOrigin, "GET");

        using var response = await app.GetTestClient().SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Fact]
    public async Task AngularDevelopmentOrigin_RemainsAllowed()
    {
        await using var app = await CreateAppAsync([LocalSpaOrigin, AngularDevOrigin]);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/branding");
        request.Headers.Add("Origin", AngularDevOrigin);

        using var response = await app.GetTestClient().SendAsync(request);

        response.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle(AngularDevOrigin);
    }

    [Fact]
    public async Task EnvironmentWithoutConfiguredOrigins_DoesNotAuthorizeLocalSpa()
    {
        await using var app = await CreateAppAsync([]);
        using var request = CreatePreflight("/api/users/branding", LocalSpaOrigin, "GET");

        using var response = await app.GetTestClient().SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    private static HttpRequestMessage CreatePreflight(string path, string origin, string method)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, path);
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", method);
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
        return request;
    }

    private static async Task<WebApplication> CreateAppAsync(string[] origins)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production
        });
        builder.WebHost.UseTestServer();

        var configuration = origins
            .Select((origin, index) => KeyValuePair.Create<string, string?>($"Cors:Origins:{index}", origin));
        builder.Configuration.AddInMemoryCollection(configuration);
        builder.Services.AddWebApi(builder.Configuration);

        var app = builder.Build();
        app.UseRouting();
        app.UseCors("CorsPolicy");
        app.MapGet("/api/users/branding", () => Results.Ok(new { application = "ACHInterbank" }));
        app.MapPost("/Auth/login", () => Results.Ok(new { accepted = true }));
        await app.StartAsync();
        return app;
    }
}
