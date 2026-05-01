using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cfa.ACHInterbank.Api.OpenApi;

public sealed class OpenApiSecurityMetadataTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var action = context.Description.ActionDescriptor as ControllerActionDescriptor;
        var endpointMetadata = action?.EndpointMetadata ?? Array.Empty<object>();

        if (endpointMetadata.OfType<AllowAnonymousAttribute>().Any())
        {
            operation.Security = null;
            return Task.CompletedTask;
        }

        if (!endpointMetadata.OfType<AuthorizeAttribute>().Any())
        {
            return Task.CompletedTask;
        }

        operation.Security ??= new List<OpenApiSecurityRequirement>();

        var bearerScheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        };

        var alreadyPresent = operation.Security.Any(static req => req.Keys.Any(k =>
            string.Equals(k.Scheme, "bearer", StringComparison.OrdinalIgnoreCase)
            && k.Type == SecuritySchemeType.Http));

        if (!alreadyPresent)
        {
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document, null)] = new List<string>()
            });
        }

        return Task.CompletedTask;
    }
}

public sealed class OpenApiBearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.OrdinalIgnoreCase);

        if (!document.Components.SecuritySchemes.ContainsKey("Bearer"))
        {
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Autenticación Bearer JWT para endpoints protegidos"
            };
        }

        return Task.CompletedTask;
    }
}
