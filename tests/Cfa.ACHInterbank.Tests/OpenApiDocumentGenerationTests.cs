using System.Diagnostics;
using System.Text.Json;
using Cfa.ACHInterbank.Api;
using Cfa.ACHInterbank.Api.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Xunit.Abstractions;

namespace Cfa.ACHInterbank.Tests;

public sealed class OpenApiDocumentGenerationTests
{
    private static readonly HashSet<string> HttpMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "get", "put", "post", "delete", "options", "head", "patch", "trace"
    };

    private readonly ITestOutputHelper _output;

    public OpenApiDocumentGenerationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Timeout = 20_000)]
    public async Task GenerateAndSerialize_ShouldCompleteWithBearerAndEnrichedDocumentation()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(AchCyclesController).Assembly.FullName,
            EnvironmentName = Environments.Production
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddWebApi(builder.Configuration);

        await using var app = builder.Build();
        app.MapControllers();

        var provider = app.Services.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");
        var generationTimer = Stopwatch.StartNew();
        var document = await Task.Run(
                () => provider.GetOpenApiDocumentAsync(timeout.Token),
                CancellationToken.None)
            .WaitAsync(timeout.Token);
        generationTimer.Stop();

        await using var jsonStream = new MemoryStream();
        var serializationTimer = Stopwatch.StartNew();
        await document.SerializeAsJsonAsync(jsonStream, OpenApiSpecVersion.OpenApi3_1, timeout.Token)
            .WaitAsync(timeout.Token);
        serializationTimer.Stop();

        var jsonBytes = jsonStream.ToArray();
        using var json = JsonDocument.Parse(jsonBytes);
        var root = json.RootElement;
        var paths = root.GetProperty("paths");
        var components = root.GetProperty("components");
        var schemas = components.GetProperty("schemas");
        var operations = CountOperations(paths);

        _output.WriteLine(
            "OpenAPI generation={0:F3}ms serialization={1:F3}ms paths={2} operations={3} schemas={4} size={5}bytes",
            generationTimer.Elapsed.TotalMilliseconds,
            serializationTimer.Elapsed.TotalMilliseconds,
            paths.EnumerateObject().Count(),
            operations,
            schemas.EnumerateObject().Count(),
            jsonBytes.Length);

        root.GetProperty("openapi").GetString().Should().StartWith("3.");
        paths.EnumerateObject().Should().NotBeEmpty();
        operations.Should().BeGreaterThan(0);
        schemas.EnumerateObject().Should().NotBeEmpty();

        var bearer = components.GetProperty("securitySchemes").GetProperty("Bearer");
        bearer.GetProperty("type").GetString().Should().Be("http");
        bearer.GetProperty("scheme").GetString().Should().Be("bearer");
        bearer.GetProperty("bearerFormat").GetString().Should().Be("JWT");

        var protectedOperation = paths.GetProperty("/ach-cycles").GetProperty("get");
        var protectedSecurity = protectedOperation.GetProperty("security");
        protectedSecurity.GetArrayLength().Should().BeGreaterThan(0);
        protectedSecurity[0].TryGetProperty("Bearer", out _).Should().BeTrue();

        var anonymousOperation = paths.GetProperty("/api/users/branding").GetProperty("get");
        anonymousOperation.TryGetProperty("security", out _).Should().BeFalse();
        paths.GetProperty("/Nacha/header").GetProperty("post").ValueKind.Should().Be(JsonValueKind.Object);
        paths.GetProperty("/Transactions").GetProperty("post").ValueKind.Should().Be(JsonValueKind.Object);

        protectedOperation.GetProperty("summary").GetString().Should().NotBeNullOrWhiteSpace();
        var description = protectedOperation.GetProperty("description").GetString();
        description.Should().Contain("ACH/CENIT/NACHA-M");
        description.Should().Contain("Precauciones");

        var nachaHeaderProperties = schemas.GetProperty("NachaHeader").GetProperty("properties");
        nachaHeaderProperties.TryGetProperty("clearingHouse", out _).Should().BeFalse();
        nachaHeaderProperties.TryGetProperty("batches", out _).Should().BeFalse();

        var transactionProperties = schemas.GetProperty("AchTransaction").GetProperty("properties");
        transactionProperties.TryGetProperty("sourceInstitution", out _).Should().BeFalse();
        transactionProperties.TryGetProperty("achCycle", out _).Should().BeFalse();
        transactionProperties.TryGetProperty("addendas", out _).Should().BeFalse();
        transactionProperties.TryGetProperty("fileExportMemberships", out _).Should().BeFalse();

        AssertAllReferencesAreLocalAndResolvable(root);
    }

    [Fact(Timeout = 20_000)]
    public async Task OpenApiEndpoint_ShouldUseExclusiveOutputCachePolicy()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(AchCyclesController).Assembly.FullName,
            EnvironmentName = Environments.Production
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddWebApi(builder.Configuration);

        await using var app = builder.Build();
        app.UseWhen(
            context => context.Request.Path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase),
            branch => branch.UseResponseCompression());
        app.UseOutputCache();
        app.MapOpenApi("/openapi/{documentName}.json")
            .CacheOutput("OpenApiDocument")
            .AllowAnonymous();
        app.MapControllers();

        await app.StartAsync(timeout.Token);
        var endpoints = app.Services.GetRequiredService<EndpointDataSource>().Endpoints;
        var openApiEndpoint = endpoints.OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.RoutePattern.RawText?.Contains(
                "openapi",
                StringComparison.OrdinalIgnoreCase) == true);
        openApiEndpoint.Metadata.GetMetadata<IOutputCachePolicy>().Should().NotBeNull();
        endpoints.Where(endpoint => endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is not null)
            .Should()
            .OnlyContain(endpoint => endpoint.Metadata.GetMetadata<IOutputCachePolicy>() == null);

        using var client = app.GetTestClient();
        var coldTimer = Stopwatch.StartNew();
        var cold = await client.GetByteArrayAsync("/openapi/v1.json", timeout.Token);
        coldTimer.Stop();
        var warmTimer = Stopwatch.StartNew();
        var warm = await client.GetByteArrayAsync("/openapi/v1.json", timeout.Token);
        warmTimer.Stop();

        cold.Should().Equal(warm);
        using var warmJson = JsonDocument.Parse(warm);
        warmJson.RootElement.GetProperty("paths").EnumerateObject().Should().NotBeEmpty();

        client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("br, gzip");
        using var compressed = await client.GetAsync("/openapi/v1.json", timeout.Token);
        compressed.EnsureSuccessStatusCode();
        compressed.Content.Headers.ContentEncoding.Should().ContainSingle()
            .Which.Should().BeOneOf("br", "gzip");
        compressed.Headers.Vary.Should().Contain("Accept-Encoding");

        _output.WriteLine(
            "OpenAPI cache cold={0:F3}ms warm={1:F3}ms size={2}bytes",
            coldTimer.Elapsed.TotalMilliseconds,
            warmTimer.Elapsed.TotalMilliseconds,
            warm.Length);
    }

    private static int CountOperations(JsonElement paths)
    {
        return paths.EnumerateObject()
            .Sum(path => path.Value.EnumerateObject().Count(property => HttpMethods.Contains(property.Name)));
    }

    private static void AssertAllReferencesAreLocalAndResolvable(JsonElement root)
    {
        foreach (var reference in EnumerateReferences(root))
        {
            reference.Should().StartWith("#/components/");
            ResolveJsonPointer(root, reference).Should().BeTrue($"reference '{reference}' must resolve");
        }
    }

    private static IEnumerable<string> EnumerateReferences(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.NameEquals("$ref") && property.Value.ValueKind == JsonValueKind.String)
                    {
                        yield return property.Value.GetString()!;
                    }

                    foreach (var nested in EnumerateReferences(property.Value))
                    {
                        yield return nested;
                    }
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var nested in EnumerateReferences(item))
                    {
                        yield return nested;
                    }
                }

                break;
        }
    }

    private static bool ResolveJsonPointer(JsonElement root, string reference)
    {
        if (!reference.StartsWith("#/", StringComparison.Ordinal))
        {
            return false;
        }

        var current = root;
        foreach (var rawSegment in reference[2..].Split('/'))
        {
            var segment = rawSegment.Replace("~1", "/", StringComparison.Ordinal)
                .Replace("~0", "~", StringComparison.Ordinal);
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
            {
                return false;
            }
        }

        return true;
    }
}
