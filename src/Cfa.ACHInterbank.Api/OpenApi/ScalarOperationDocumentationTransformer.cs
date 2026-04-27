using System.Reflection;
using System.Text;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cfa.ACHInterbank.Api.OpenApi;

public sealed class ScalarOperationDocumentationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var action = context.Description.ActionDescriptor as ControllerActionDescriptor;
        var httpMethod = context.Description.HttpMethod?.ToUpperInvariant() ?? "GET";
        var route = NormalizeRoute(context.Description.RelativePath);
        var permission = ResolvePermission(action);
        var operationType = IsReadOnlyOperation(httpMethod) ? "solo consulta" : "modifica información";
        var auditInfo = IsReadOnlyOperation(httpMethod)
            ? "sí, por trazas de acceso y correlación operativa"
            : "sí, explícita por cambio de estado o acción operativa";

        if (string.IsNullOrWhiteSpace(operation.Summary))
        {
            operation.Summary = BuildFallbackSummary(action, httpMethod, route);
        }

        if (string.IsNullOrWhiteSpace(operation.Description))
        {
            var responseCodes = BuildResponseCodes(operation, httpMethod);
            var risk = ResolveRisk(route, httpMethod);
            var actionName = action?.ActionName ?? "operación";

            var descriptionBuilder = new StringBuilder();
            descriptionBuilder.Append("Qué hace: ejecuta ");
            descriptionBuilder.Append(actionName);
            descriptionBuilder.Append(" sobre la ruta ");
            descriptionBuilder.Append(route);
            descriptionBuilder.Append(". ");
            descriptionBuilder.Append("Permiso requerido: ");
            descriptionBuilder.Append(permission);
            descriptionBuilder.Append(". ");
            descriptionBuilder.Append("Tipo de operación: ");
            descriptionBuilder.Append(httpMethod);
            descriptionBuilder.Append(". ");
            descriptionBuilder.Append("Naturaleza: ");
            descriptionBuilder.Append(operationType);
            descriptionBuilder.Append(". ");
            descriptionBuilder.Append("Genera auditoría: ");
            descriptionBuilder.Append(auditInfo);
            descriptionBuilder.Append(". ");
            descriptionBuilder.Append("Códigos de respuesta deducibles: ");
            descriptionBuilder.Append(responseCodes);
            descriptionBuilder.Append(". ");
            descriptionBuilder.Append("Riesgo operativo: ");
            descriptionBuilder.Append(risk);
            descriptionBuilder.Append(". ");
            descriptionBuilder.Append("Relación ACH/CENIT/NACHA-M: mantiene trazabilidad y control documental del flujo interbancario según la capacidad expuesta por este endpoint.");

            operation.Description = descriptionBuilder.ToString();
        }

        return Task.CompletedTask;
    }

    private static string BuildFallbackSummary(ControllerActionDescriptor? action, string httpMethod, string route)
    {
        if (action is null)
        {
            return $"{httpMethod} {route}";
        }

        var controllerName = action.ControllerName;
        return $"{httpMethod} {controllerName}.{action.ActionName}";
    }

    private static string NormalizeRoute(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return "/";
        }

        var pathWithoutQuery = relativePath.Split('?', 2)[0].Trim();
        return pathWithoutQuery.StartsWith('/') ? pathWithoutQuery : $"/{pathWithoutQuery}";
    }

    private static bool IsReadOnlyOperation(string httpMethod)
    {
        return httpMethod is "GET" or "HEAD" or "OPTIONS";
    }

    private static string ResolvePermission(ControllerActionDescriptor? action)
    {
        if (action is null)
        {
            return "autenticación/autorización definida por middleware";
        }

        var endpointMetadata = action.EndpointMetadata;
        if (endpointMetadata.OfType<AllowAnonymousAttribute>().Any())
        {
            return "acceso anónimo";
        }

        var authorizeAttributes = endpointMetadata.OfType<AuthorizeAttribute>().ToArray();
        if (authorizeAttributes.Length == 0)
        {
            return "requiere token válido (sin política explícita en atributo)";
        }

        var permissions = new List<string>();
        foreach (var attribute in authorizeAttributes)
        {
            if (!string.IsNullOrWhiteSpace(attribute.Policy))
            {
                permissions.Add(attribute.Policy!);
            }

            if (!string.IsNullOrWhiteSpace(attribute.Roles))
            {
                permissions.Add($"roles: {attribute.Roles}");
            }
        }

        return permissions.Count == 0
            ? "requiere token válido con autorización"
            : string.Join(" + ", permissions.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string BuildResponseCodes(OpenApiOperation operation, string httpMethod)
    {
        if (operation.Responses is { Count: > 0 })
        {
            return string.Join(", ", operation.Responses.Keys.OrderBy(static x => x, StringComparer.Ordinal));
        }

        return httpMethod switch
        {
            "POST" => "200, 201, 400, 401, 403",
            "PUT" or "PATCH" => "200, 204, 400, 401, 403, 404",
            "DELETE" => "200, 204, 401, 403, 404",
            _ => "200, 400, 401, 403, 404"
        };
    }

    private static string ResolveRisk(string route, string httpMethod)
    {
        var routeLower = route.ToLowerInvariant();
        var isSensitive = routeLower.Contains("security")
                          || routeLower.Contains("certificate")
                          || routeLower.Contains("auth")
                          || routeLower.Contains("password")
                          || routeLower.Contains("token")
                          || routeLower.Contains("encrypt")
                          || routeLower.Contains("decrypt");

        if (isSensitive)
        {
            return "alto; requiere segregación de funciones, revisión de autorizaciones y manejo seguro de evidencia";
        }

        if (!IsReadOnlyOperation(httpMethod))
        {
            return "medio/alto; un cambio incorrecto puede afectar conciliación ACH, trazabilidad CENIT o integridad NACHA-M";
        }

        return "medio; consultas con filtros incorrectos pueden producir diagnósticos operativos erróneos";
    }
}
