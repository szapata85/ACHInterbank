using System.Globalization;
using System.Reflection;
using System.Text.Json;
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

public sealed class OpenApiResponseDescriptionsTransformerTests
{
    private static readonly string[] ValidPaths =
    [
        "__openapi-response-description-tests/described",
        "__openapi-response-description-tests/multiple",
        "__openapi-response-description-tests/no-content",
        "__openapi-response-description-tests/preserve-existing",
        "__openapi-response-description-tests/without-metadata"
    ];

    [Fact(Timeout = 20_000)]
    public async Task ExplicitMetadata_ShouldDescribeOnlyExistingResponsesAndPreserveContracts()
    {
        using var json = await GenerateDocumentAsync(ValidPaths);
        var paths = json.RootElement.GetProperty("paths");

        var described400 = paths
            .GetProperty("/__openapi-response-description-tests/described")
            .GetProperty("get")
            .GetProperty("responses")
            .GetProperty("400");
        described400.GetProperty("description").GetString()
            .Should().Be("La solicitud de prueba no cumple el contrato esperado.");
        described400.GetProperty("content")
            .GetProperty("application/problem+json")
            .GetProperty("schema")
            .ValueKind.Should().Be(JsonValueKind.Object,
                "el atributo de descripción no debe sustituir el schema contractual publicado");

        var multipleResponses = paths
            .GetProperty("/__openapi-response-description-tests/multiple")
            .GetProperty("get")
            .GetProperty("responses");
        multipleResponses.GetProperty("200").GetProperty("description").GetString()
            .Should().Be("La consulta de prueba finalizó correctamente.");
        multipleResponses.GetProperty("400").GetProperty("description").GetString()
            .Should().Be("La solicitud de prueba es inválida.");
        multipleResponses.GetProperty("404").GetProperty("description").GetString()
            .Should().Be("El recurso ficticio de prueba no existe.");
        multipleResponses.TryGetProperty("409", out _).Should().BeFalse(
            "el transformer de descripciones no debe crear status codes que el contrato no publique");

        var noContentResponse = paths
            .GetProperty("/__openapi-response-description-tests/no-content")
            .GetProperty("delete")
            .GetProperty("responses")
            .GetProperty("204");
        noContentResponse.GetProperty("description").GetString()
            .Should().Be("La operación de prueba finalizó sin cuerpo de respuesta.");
        if (noContentResponse.TryGetProperty("content", out var noContentMediaTypes))
        {
            noContentMediaTypes.EnumerateObject().Should().BeEmpty(
                "una descripción no debe inventar content types para una response sin cuerpo");
        }

        var preserved400 = paths
            .GetProperty("/__openapi-response-description-tests/preserve-existing")
            .GetProperty("get")
            .GetProperty("responses")
            .GetProperty("400");
        preserved400.GetProperty("description").GetString()
            .Should().Be("Descripción previa deliberada.",
                "una descripción específica existente tiene precedencia sobre la metadata posterior");

        var withoutMetadata400 = paths
            .GetProperty("/__openapi-response-description-tests/without-metadata")
            .GetProperty("get")
            .GetProperty("responses")
            .GetProperty("400");
        withoutMetadata400.GetProperty("description").GetString()
            .Should().Be("Descripción sembrada sin metadata SC-103.",
                "una operación sin OpenApiResponseDescriptionAttribute debe permanecer intacta");
    }

    [Fact(Timeout = 20_000)]
    public async Task MetadataForMissingResponse_ShouldNotBreakDocumentGeneration()
    {
        using var json = await GenerateDocumentAsync(["__openapi-response-description-tests/missing-response"]);
        var responses = json.RootElement
            .GetProperty("paths")
            .GetProperty("/__openapi-response-description-tests/missing-response")
            .GetProperty("get")
            .GetProperty("responses");

        responses.TryGetProperty("200", out _).Should().BeTrue();
        responses.TryGetProperty("409", out _).Should().BeFalse(
            "runtime OpenAPI generation must preserve the contract and ignore stale description metadata instead of taking down the whole document");
    }

    private static async Task<JsonDocument> GenerateDocumentAsync(IReadOnlyCollection<string> includedPaths)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(OpenApiResponseDescriptionsTestController).Assembly.FullName,
            EnvironmentName = Environments.Production
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddOpenApi("v1", options =>
            options.AddOperationTransformer<SeedResponseDescriptionTransformer>());
        builder.Services.AddWebApi(builder.Configuration);
        builder.Services.AddControllers().AddApplicationPart(typeof(OpenApiResponseDescriptionsTestController).Assembly);

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
[Route("__openapi-response-description-tests")]
public sealed class OpenApiResponseDescriptionsTestController : ControllerBase
{
    [HttpGet("described")]
    [Produces("application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [SeedResponseDescription(StatusCodes.Status400BadRequest, "Bad Request")]
    [OpenApiResponseDescription(
        StatusCodes.Status400BadRequest,
        "La solicitud de prueba no cumple el contrato esperado.")]
    public ActionResult<ProblemDetails> Described()
        => BadRequest(new ProblemDetails { Title = "Error ficticio de prueba" });

    [HttpGet("multiple")]
    [Produces("application/json")]
    [ProducesResponseType<OpenApiResponseDescriptionTestResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "La consulta de prueba finalizó correctamente.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La solicitud de prueba es inválida.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "El recurso ficticio de prueba no existe.")]
    public ActionResult<OpenApiResponseDescriptionTestResult> Multiple()
        => Ok(new OpenApiResponseDescriptionTestResult("resultado-ficticio"));

    [HttpDelete("no-content")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [OpenApiResponseDescription(StatusCodes.Status204NoContent, "La operación de prueba finalizó sin cuerpo de respuesta.")]
    public IActionResult NoContentResponse() => NoContent();

    [HttpGet("preserve-existing")]
    [Produces("application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [SeedResponseDescription(StatusCodes.Status400BadRequest, "Descripción previa deliberada.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "Esta descripción no debe reemplazar la anterior.")]
    public ActionResult<ProblemDetails> PreserveExisting()
        => BadRequest(new ProblemDetails { Title = "Error ficticio preservado" });

    [HttpGet("without-metadata")]
    [Produces("application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [SeedResponseDescription(StatusCodes.Status400BadRequest, "Descripción sembrada sin metadata SC-103.")]
    public ActionResult<ProblemDetails> WithoutMetadata()
        => BadRequest(new ProblemDetails { Title = "Error ficticio sin metadata" });

    [HttpGet("missing-response")]
    [Produces("application/json")]
    [ProducesResponseType<OpenApiResponseDescriptionTestResult>(StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "Este response no forma parte del contrato publicado.")]
    public ActionResult<OpenApiResponseDescriptionTestResult> MissingResponse()
        => Ok(new OpenApiResponseDescriptionTestResult("resultado-ficticio"));
}

public sealed record OpenApiResponseDescriptionTestResult(string Result);

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class SeedResponseDescriptionAttribute(int statusCode, string description) : Attribute
{
    public int StatusCode { get; } = statusCode;
    public string Description { get; } = description;
}

public sealed class SeedResponseDescriptionTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Description.ActionDescriptor is not ControllerActionDescriptor action)
        {
            return Task.CompletedTask;
        }

        foreach (var metadata in action.MethodInfo.GetCustomAttributes<SeedResponseDescriptionAttribute>(inherit: true))
        {
            var statusCode = metadata.StatusCode.ToString(CultureInfo.InvariantCulture);
            if (operation.Responses.TryGetValue(statusCode, out var response))
            {
                response.Description = metadata.Description;
            }
        }

        return Task.CompletedTask;
    }
}
