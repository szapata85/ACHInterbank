using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using MvcJsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;

namespace Cfa.ACHInterbank.Api.OpenApi;

public sealed class OpenApiExamplesTransformer(
    IOptions<MvcJsonOptions> jsonOptions,
    ILogger<OpenApiExamplesTransformer> logger) : IOpenApiOperationTransformer
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = jsonOptions.Value.JsonSerializerOptions;
    private readonly ILogger<OpenApiExamplesTransformer> _logger = logger;

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

        try
        {
            ApplyRequestExample(operation, action.MethodInfo);
            ApplyResponseExamples(operation, action.MethodInfo);
        }
        catch (Exception exception)
        {
            // Documentation enrichment must never make the whole OpenAPI document unavailable.
            // Contract validation remains test-time responsibility; runtime skips only the broken example.
            _logger.LogWarning(
                exception,
                "OpenAPI examples were skipped for {Controller}.{Action} because example metadata could not be materialized.",
                action.ControllerName,
                action.ActionName);
        }

        return Task.CompletedTask;
    }

    private void ApplyRequestExample(OpenApiOperation operation, MethodInfo methodInfo)
    {
        var metadata = methodInfo.GetCustomAttribute<OpenApiRequestExampleAttribute>(inherit: true);
        if (metadata is null || operation.RequestBody?.Content is null)
        {
            return;
        }

        if (!TryGetMediaType(operation.RequestBody.Content, metadata.ContentType, out var mediaType)
            || HasExample(mediaType))
        {
            return;
        }

        mediaType.Example = CreateExampleNode(metadata.ProviderType);
    }

    private void ApplyResponseExamples(OpenApiOperation operation, MethodInfo methodInfo)
    {
        var metadataItems = methodInfo.GetCustomAttributes<OpenApiResponseExampleAttribute>(inherit: true);
        foreach (var metadata in metadataItems)
        {
            var statusCode = metadata.StatusCode.ToString(CultureInfo.InvariantCulture);
            if (operation.Responses is null
                || !operation.Responses.TryGetValue(statusCode, out var response)
                || response.Content is null
                || !TryGetMediaType(response.Content, metadata.ContentType, out var mediaType)
                || HasExample(mediaType))
            {
                continue;
            }

            mediaType.Example = CreateExampleNode(metadata.ProviderType);
        }
    }

    private JsonNode CreateExampleNode(Type providerType)
    {
        if (!typeof(IOpenApiExampleProvider).IsAssignableFrom(providerType))
        {
            throw new InvalidOperationException(
                $"El provider de ejemplo '{providerType.FullName}' debe implementar {nameof(IOpenApiExampleProvider)}.");
        }

        IOpenApiExampleProvider provider;
        try
        {
            provider = (IOpenApiExampleProvider)(Activator.CreateInstance(providerType)
                ?? throw new InvalidOperationException(
                    $"No fue posible crear el provider de ejemplo '{providerType.FullName}'."));
        }
        catch (Exception exception) when (exception is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"No fue posible crear el provider de ejemplo '{providerType.FullName}'. Debe tener un constructor público sin parámetros.",
                exception);
        }

        var example = provider.GetExample()
            ?? throw new InvalidOperationException(
                $"El provider de ejemplo '{providerType.FullName}' devolvió un valor nulo.");

        return JsonSerializer.SerializeToNode(example, example.GetType(), _jsonSerializerOptions)
            ?? throw new InvalidOperationException(
                $"No fue posible serializar el ejemplo producido por '{providerType.FullName}'.");
    }

    private static bool TryGetMediaType(
        IDictionary<string, OpenApiMediaType> content,
        string contentType,
        out OpenApiMediaType mediaType)
    {
        mediaType = null!;
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        foreach (var entry in content)
        {
            if (string.Equals(entry.Key, contentType, StringComparison.OrdinalIgnoreCase))
            {
                mediaType = entry.Value;
                return true;
            }
        }

        return false;
    }

    private static bool HasExample(OpenApiMediaType mediaType)
        => mediaType.Example is not null || mediaType.Examples is { Count: > 0 };
}
