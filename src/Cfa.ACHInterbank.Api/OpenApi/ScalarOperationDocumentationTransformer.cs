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
        var domain = ResolveDomain(action, route);
        var purpose = ResolvePurpose(httpMethod, action, route, domain);
        var usage = ResolveUsage(httpMethod, route, domain);
        var consumerProfile = ResolveConsumerProfile(route, domain);
        var operationType = IsReadOnlyOperation(httpMethod) ? "solo consulta" : "modifica información";
        var auditInfo = IsReadOnlyOperation(httpMethod)
            ? "sí, por trazas de acceso y correlación operativa"
            : "sí, explícita por cambio de estado o acción operativa";

        if (string.IsNullOrWhiteSpace(operation.Summary))
        {
            operation.Summary = BuildFallbackSummary(httpMethod, domain, purpose);
        }

        if (string.IsNullOrWhiteSpace(operation.Description))
        {
            var responseCodes = BuildResponseCodes(operation, httpMethod);
            var risk = ResolveRisk(route, httpMethod);
            var descriptionBuilder = new StringBuilder();
            descriptionBuilder.Append("Qué hace: ");
            descriptionBuilder.Append(purpose);
            descriptionBuilder.Append(". ");
            descriptionBuilder.Append("Cuándo se usa: ");
            descriptionBuilder.Append(usage);
            descriptionBuilder.Append(". ");
            descriptionBuilder.Append("Perfil consumidor: ");
            descriptionBuilder.Append(consumerProfile);
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
            descriptionBuilder.Append("Relación ACH/CENIT/NACHA-M: ");
            descriptionBuilder.Append(ResolveBusinessRelation(route, domain));
            descriptionBuilder.Append(". ");
            descriptionBuilder.Append("Precauciones para desarrollo u operación: ");
            descriptionBuilder.Append(ResolvePrecaution(httpMethod, route));
            descriptionBuilder.Append(".");

            operation.Description = descriptionBuilder.ToString();
        }

        return Task.CompletedTask;
    }

    private static string BuildFallbackSummary(string httpMethod, string domain, string purpose)
    {
        var verb = httpMethod switch
        {
            "GET" => "Consulta",
            "POST" => "Registro",
            "PUT" => "Actualización",
            "PATCH" => "Ajuste",
            "DELETE" => "Eliminación",
            _ => "Operación"
        };

        return $"{verb} de {domain}: {purpose}";
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

    private static string ResolveDomain(ControllerActionDescriptor? action, string route)
    {
        var controller = action?.ControllerName?.Replace("Controller", string.Empty, StringComparison.OrdinalIgnoreCase) ?? "Operaciones";
        var routeLower = route.ToLowerInvariant();

        if (routeLower.Contains("nacha"))
        {
            return "operación NACHA-M";
        }

        if (routeLower.Contains("certificate") || routeLower.Contains("security") || routeLower.Contains("auth"))
        {
            return "seguridad bancaria";
        }

        if (routeLower.Contains("report"))
        {
            return "reportería ACH";
        }

        if (routeLower.Contains("cycle") || routeLower.Contains("batch"))
        {
            return "ciclos y lotes ACH";
        }

        return Humanize(controller);
    }

    private static string ResolvePurpose(string httpMethod, ControllerActionDescriptor? action, string route, string domain)
    {
        var actionName = Humanize(action?.ActionName ?? "gestión");
        return httpMethod switch
        {
            "GET" => $"consulta {actionName} dentro de {domain} para la ruta {route}",
            "POST" => $"registra una solicitud de {actionName} dentro de {domain} para la ruta {route}",
            "PUT" => $"actualiza información de {actionName} dentro de {domain} para la ruta {route}",
            "PATCH" => $"aplica ajuste parcial de {actionName} dentro de {domain} para la ruta {route}",
            "DELETE" => $"elimina recursos asociados a {actionName} dentro de {domain} para la ruta {route}",
            _ => $"ejecuta {actionName} dentro de {domain} para la ruta {route}"
        };
    }

    private static string ResolveUsage(string httpMethod, string route, string domain)
    {
        return IsReadOnlyOperation(httpMethod)
            ? $"se utiliza en monitoreo operativo, conciliación y soporte cuando se requiere visibilidad puntual en {domain} ({route})"
            : $"se utiliza durante ventanas operativas controladas cuando se requiere aplicar cambios en {domain} ({route})";
    }

    private static string ResolveConsumerProfile(string route, string domain)
    {
        var routeLower = route.ToLowerInvariant();
        if (routeLower.Contains("security") || routeLower.Contains("certificate") || routeLower.Contains("auth"))
        {
            return "seguridad bancaria, administradores de plataforma y auditoría técnica";
        }

        if (routeLower.Contains("report"))
        {
            return "operación ACH, conciliación financiera, cumplimiento y auditoría interna";
        }

        if (routeLower.Contains("incoming") || routeLower.Contains("ingestion") || routeLower.Contains("queue"))
        {
            return "operación ACH, soporte de incidentes y monitoreo de procesamiento";
        }

        return $"equipos funcionales y técnicos responsables de {domain}";
    }

    private static string ResolveBusinessRelation(string route, string domain)
    {
        var routeLower = route.ToLowerInvariant();
        if (routeLower.Contains("nacha"))
        {
            return "controla trazabilidad, generación, recepción o aseguramiento de archivos NACHA-M con impacto directo en compensación ACH/CENIT";
        }

        if (routeLower.Contains("cycle") || routeLower.Contains("batch"))
        {
            return "impacta la preparación y cierre de ciclos ACH con efectos en conciliación CENIT";
        }

        if (routeLower.Contains("report"))
        {
            return "provee evidencia operativa y regulatoria de flujos ACH/CENIT/NACHA-M";
        }

        return $"mantiene consistencia operativa del dominio {domain} dentro del circuito ACH/CENIT/NACHA-M";
    }

    private static string ResolvePrecaution(string httpMethod, string route)
    {
        if (IsReadOnlyOperation(httpMethod))
        {
            return $"validar filtros, rangos de fecha y correlación de identificadores antes de usar resultados de {route} en decisiones operativas";
        }

        return $"aplicar segregación de funciones, bitácora de cambio y verificación de impacto antes de confirmar la acción en {route}";
    }

    private static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "operación";
        }

        var sb = new StringBuilder();
        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            if (i > 0 && char.IsUpper(ch) && char.IsLetter(value[i - 1]))
            {
                sb.Append(' ');
            }

            sb.Append(char.ToLowerInvariant(ch));
        }

        return sb.ToString().Trim();
    }
}
