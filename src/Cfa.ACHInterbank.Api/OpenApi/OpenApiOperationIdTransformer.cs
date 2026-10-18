using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cfa.ACHInterbank.Api.OpenApi;

public sealed class OpenApiOperationIdTransformer : IOpenApiOperationTransformer
{
    private const string RootRouteComponent = "Root";
    private const string UnknownHttpMethodComponent = "UnknownMethod";

    private static readonly Regex RouteParameterPattern = new(
        @"\{(?:\*{1,2})?([^}:?]+)(?:[^}]*)\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex NonAlphanumericPattern = new(
        @"[^A-Za-z0-9]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.IsNullOrWhiteSpace(operation.OperationId))
        {
            return Task.CompletedTask;
        }

        var description = context.Description;
        var httpMethod = NormalizeComponent(description.HttpMethod?.ToUpperInvariant(), UnknownHttpMethodComponent);
        var route = NormalizeRoute(description.RelativePath);

        if (description.ActionDescriptor is ControllerActionDescriptor action)
        {
            var controllerName = NormalizeComponent(action.ControllerName);
            var actionName = NormalizeComponent(action.ActionName);

            if (controllerName.Length > 0 && actionName.Length > 0)
            {
                operation.OperationId = $"{controllerName}_{actionName}_{httpMethod}_{route}";
                return Task.CompletedTask;
            }
        }

        operation.OperationId = $"{httpMethod}_{route}";
        return Task.CompletedTask;
    }

    private static string NormalizeRoute(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return RootRouteComponent;
        }

        var querySeparator = relativePath.IndexOf('?');
        var pathOnly = querySeparator >= 0
            ? relativePath[..querySeparator]
            : relativePath;

        pathOnly = pathOnly.Trim('/');
        if (pathOnly.Length == 0)
        {
            return RootRouteComponent;
        }

        var routeWithStableParameters = RouteParameterPattern.Replace(
            pathOnly,
            match => match.Groups[1].Value);

        return NormalizeComponent(routeWithStableParameters, RootRouteComponent);
    }

    private static string NormalizeComponent(string? value, string fallback = "")
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var normalized = NonAlphanumericPattern.Replace(value.Trim(), "_").Trim('_');
        return normalized.Length == 0 ? fallback : normalized;
    }
}
