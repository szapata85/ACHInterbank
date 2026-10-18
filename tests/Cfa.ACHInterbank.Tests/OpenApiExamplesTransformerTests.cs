using System.Text.Json;
using System.Text.Json.Nodes;
using System.Reflection;
using Cfa.ACHInterbank.Api;
using Cfa.ACHInterbank.Api.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace Cfa.ACHInterbank.Tests;

public sealed class OpenApiExamplesTransformerTests
{
    private static readonly string[] ValidExamplePaths =
    [
        "__openapi-example-tests/request-response",
        "__openapi-example-tests/no-request-body",
        "__openapi-example-tests/missing-response",
        "__openapi-example-tests/missing-media-type",
        "__openapi-example-tests/preserve-existing"
    ];

    [Fact(Timeout = 20_000)]
    public async Task ExplicitMetadata_ShouldApplyExamplesWithoutChangingContractsOrProviderObjects()
    {
        var originalRequestObject = OpenApiExamplesRequestProvider.Instance;
        var originalDisplayName = originalRequestObject.DisplayName;
        var originalEmail = originalRequestObject.Email;

        using var json = await GenerateDocumentAsync(ValidExamplePaths);
        var paths = json.RootElement.GetProperty("paths");

        var documented = paths.GetProperty("/__openapi-example-tests/request-response").GetProperty("post");
        var requestExample = documented
            .GetProperty("requestBody")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("example");
        requestExample.GetProperty("displayName").GetString().Should().Be("usuario.demo");
        requestExample.GetProperty("email").GetString().Should().Be("persona@example.test");
        requestExample.TryGetProperty("DisplayName", out _).Should().BeFalse(
            "el ejemplo debe respetar la configuración JSON camelCase real de la API");

        var responseExample = documented
            .GetProperty("responses")
            .GetProperty("201")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("example");
        responseExample.GetProperty("referenceId").GetString()
            .Should().Be("11111111-1111-1111-1111-111111111111");
        responseExample.GetProperty("result").GetString().Should().Be("aceptado-demo");

        OpenApiExamplesRequestProvider.Instance.Should().BeSameAs(originalRequestObject);
        OpenApiExamplesRequestProvider.Instance.DisplayName.Should().Be(originalDisplayName);
        OpenApiExamplesRequestProvider.Instance.Email.Should().Be(originalEmail);

        var noRequestBody = paths.GetProperty("/__openapi-example-tests/no-request-body").GetProperty("get");
        noRequestBody.TryGetProperty("requestBody", out _).Should().BeFalse(
            "la metadata de ejemplo no debe crear un requestBody inexistente");

        var missingResponse = paths.GetProperty("/__openapi-example-tests/missing-response").GetProperty("get");
        missingResponse.GetProperty("responses").TryGetProperty("422", out _).Should().BeFalse(
            "la metadata de ejemplo no debe crear responses inexistentes");

        var missingMediaType = paths.GetProperty("/__openapi-example-tests/missing-media-type").GetProperty("post");
        var requestContent = missingMediaType.GetProperty("requestBody").GetProperty("content");
        requestContent.TryGetProperty("application/xml", out _).Should().BeFalse(
            "la metadata de ejemplo no debe crear content types inexistentes en el request");
        var responseContent = missingMediaType
            .GetProperty("responses")
            .GetProperty("200")
            .GetProperty("content");
        responseContent.TryGetProperty("application/xml", out _).Should().BeFalse(
            "la metadata de ejemplo no debe crear content types inexistentes en la response");

        var preservedExample = paths
            .GetProperty("/__openapi-example-tests/preserve-existing")
            .GetProperty("get")
            .GetProperty("responses")
            .GetProperty("200")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("example");
        preservedExample.GetProperty("source").GetString().Should().Be("existente");
        preservedExample.TryGetProperty("result", out _).Should().BeFalse(
            "un Example existente debe conservarse y no reemplazarse por el provider");
    }

    [Fact(Timeout = 20_000)]
    public async Task InvalidProvider_ShouldNotBreakDocumentGeneration()
    {
        using var json = await GenerateDocumentAsync(["__openapi-example-tests/invalid-provider"]);
        var mediaType = json.RootElement
            .GetProperty("paths")
            .GetProperty("/__openapi-example-tests/invalid-provider")
            .GetProperty("get")
            .GetProperty("responses")
            .GetProperty("200")
            .GetProperty("content")
            .GetProperty("application/json");

        mediaType.TryGetProperty("example", out _).Should().BeFalse(
            "an invalid example provider must be ignored at runtime instead of making /openapi/v1.json unavailable");
    }

    private static async Task<JsonDocument> GenerateDocumentAsync(IReadOnlyCollection<string> includedPaths)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(OpenApiExamplesTestController).Assembly.FullName,
            EnvironmentName = Environments.Production
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddOpenApi("v1", options =>
            options.AddOperationTransformer<SeedExistingOpenApiExampleTransformer>());
        builder.Services.AddWebApi(builder.Configuration);
        builder.Services.AddControllers().AddApplicationPart(typeof(OpenApiExamplesTestController).Assembly);

        var explicitPaths = includedPaths.ToHashSet(StringComparer.Ordinal);
        builder.Services.Configure<OpenApiOptions>("v1", options =>
        {
            options.ShouldInclude = description =>
                description.RelativePath is not null && explicitPaths.Contains(description.RelativePath);
        });

        await using var app = builder.Build();
        app.MapControllers();

        var provider = app.Services.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");
        var document = await provider.GetOpenApiDocumentAsync(timeout.Token).WaitAsync(timeout.Token);

        await using var stream = new MemoryStream();
        await document.SerializeAsJsonAsync(stream, OpenApiSpecVersion.OpenApi3_1, timeout.Token)
            .WaitAsync(timeout.Token);

        return JsonDocument.Parse(stream.ToArray());
    }
}

[ApiController]
[Route("__openapi-example-tests")]
public sealed class OpenApiExamplesTestController : ControllerBase
{
    [HttpPost("request-response")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<OpenApiExamplesResponse>(StatusCodes.Status201Created)]
    [OpenApiRequestExample(typeof(OpenApiExamplesRequestProvider))]
    [OpenApiResponseExample(StatusCodes.Status201Created, typeof(OpenApiExamplesResponseProvider))]
    public ActionResult<OpenApiExamplesResponse> RequestResponse([FromBody] OpenApiExamplesRequest request)
        => StatusCode(StatusCodes.Status201Created, new OpenApiExamplesResponse(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "aceptado-demo"));

    [HttpGet("no-request-body")]
    [Produces("application/json")]
    [ProducesResponseType<OpenApiExamplesResponse>(StatusCodes.Status200OK)]
    [OpenApiRequestExample(typeof(OpenApiExamplesRequestProvider))]
    public ActionResult<OpenApiExamplesResponse> NoRequestBody()
        => Ok(OpenApiExamplesResponseProvider.Instance);

    [HttpGet("missing-response")]
    [Produces("application/json")]
    [ProducesResponseType<OpenApiExamplesResponse>(StatusCodes.Status200OK)]
    [OpenApiResponseExample(StatusCodes.Status422UnprocessableEntity, typeof(OpenApiExamplesResponseProvider))]
    public ActionResult<OpenApiExamplesResponse> MissingResponse()
        => Ok(OpenApiExamplesResponseProvider.Instance);

    [HttpPost("missing-media-type")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<OpenApiExamplesResponse>(StatusCodes.Status200OK)]
    [OpenApiRequestExample(typeof(OpenApiExamplesRequestProvider), ContentType = "application/xml")]
    [OpenApiResponseExample(StatusCodes.Status200OK, typeof(OpenApiExamplesResponseProvider), ContentType = "application/xml")]
    public ActionResult<OpenApiExamplesResponse> MissingMediaType([FromBody] OpenApiExamplesRequest request)
        => Ok(OpenApiExamplesResponseProvider.Instance);

    [HttpGet("preserve-existing")]
    [Produces("application/json")]
    [ProducesResponseType<OpenApiExamplesResponse>(StatusCodes.Status200OK)]
    [SeedExistingOpenApiExample]
    [OpenApiResponseExample(StatusCodes.Status200OK, typeof(OpenApiExamplesResponseProvider))]
    public ActionResult<OpenApiExamplesResponse> PreserveExisting()
        => Ok(OpenApiExamplesResponseProvider.Instance);

    [HttpGet("invalid-provider")]
    [Produces("application/json")]
    [ProducesResponseType<OpenApiExamplesResponse>(StatusCodes.Status200OK)]
    [OpenApiResponseExample(StatusCodes.Status200OK, typeof(string))]
    public ActionResult<OpenApiExamplesResponse> InvalidProvider()
        => Ok(OpenApiExamplesResponseProvider.Instance);
}

public sealed record OpenApiExamplesRequest(string DisplayName, string Email);

public sealed record OpenApiExamplesResponse(Guid ReferenceId, string Result);

public sealed class OpenApiExamplesRequestProvider : IOpenApiExampleProvider
{
    public static OpenApiExamplesRequest Instance { get; } = new("usuario.demo", "persona@example.test");

    public object GetExample() => Instance;
}

public sealed class OpenApiExamplesResponseProvider : IOpenApiExampleProvider
{
    public static OpenApiExamplesResponse Instance { get; } = new(
        Guid.Parse("11111111-1111-1111-1111-111111111111"),
        "aceptado-demo");

    public object GetExample() => Instance;
}


[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class SeedExistingOpenApiExampleAttribute : Attribute;

public sealed class SeedExistingOpenApiExampleTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Description.ActionDescriptor is ControllerActionDescriptor action
            && action.MethodInfo.GetCustomAttribute<SeedExistingOpenApiExampleAttribute>(inherit: true) is not null
            && operation.Responses.TryGetValue("200", out var response)
            && response.Content is not null
            && response.Content.TryGetValue("application/json", out var mediaType))
        {
            mediaType.Example = JsonNode.Parse("{\"source\":\"existente\"}");
        }

        return Task.CompletedTask;
    }
}
