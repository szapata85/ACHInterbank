using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.OpenApi;

namespace Cfa.ACHInterbank.Api.OpenApi;

public sealed class OpenApiResponseDescriptionsTransformer(ILogger<OpenApiResponseDescriptionsTransformer> logger) : IOpenApiOperationTransformer
{
    private readonly ILogger<OpenApiResponseDescriptionsTransformer> _logger = logger;
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

        var metadataItems = action.MethodInfo.GetCustomAttributes<OpenApiResponseDescriptionAttribute>(inherit: true);
        foreach (var metadata in metadataItems)
        {
            try
            {
                ApplyDescription(operation, action, metadata);
            }
            catch (InvalidOperationException exception)
            {
                // A stale documentation annotation must not take down /openapi/v1.json and Scalar.
                // Keep the actual response contract emitted by ApiExplorer and surface the mismatch in logs.
                _logger.LogWarning(
                    exception,
                    "OpenAPI response description metadata was ignored for {Controller}.{Action} (HTTP {StatusCode}).",
                    action.ControllerName,
                    action.ActionName,
                    metadata.StatusCode);
            }
        }

        return Task.CompletedTask;
    }

    private static void ApplyDescription(
        OpenApiOperation operation,
        ControllerActionDescriptor action,
        OpenApiResponseDescriptionAttribute metadata)
    {
        var statusCode = metadata.StatusCode.ToString(CultureInfo.InvariantCulture);
        if (operation.Responses is null || !operation.Responses.TryGetValue(statusCode, out var response))
        {
            throw new InvalidOperationException(
                $"{nameof(OpenApiResponseDescriptionAttribute)} en '{action.ControllerName}.{action.ActionName}' " +
                $"referencia el response HTTP {statusCode}, pero ese response no existe en el contrato OpenAPI publicado.");
        }

        if (CanApplyExplicitDescription(response.Description, metadata.StatusCode))
        {
            response.Description = metadata.Description;
        }
    }

    private static bool CanApplyExplicitDescription(string? currentDescription, int statusCode)
    {
        if (string.IsNullOrWhiteSpace(currentDescription))
        {
            return true;
        }

        var standardReasonPhrase = ReasonPhrases.GetReasonPhrase(statusCode);
        return !string.IsNullOrWhiteSpace(standardReasonPhrase)
            && string.Equals(
                currentDescription.Trim(),
                standardReasonPhrase,
                StringComparison.Ordinal);
    }
}
