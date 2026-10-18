using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cfa.ACHInterbank.Api.OpenApi;

public sealed class OpenApiTransversalHeadersTransformer : IOpenApiOperationTransformer
{
    private const string IdempotencyKeyHeaderName = "Idempotency-Key";
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    private const string IdempotencyKeyDescription =
        "Clave opcional utilizada por esta operación para idempotencia o reintentos.";

    private const string CorrelationIdDescription =
        "Identificador opcional utilizado por esta operación para correlación, trazabilidad o auditoría.";

    private static readonly IReadOnlyDictionary<OperationKey, HeaderKind> OperationHeaders =
        new Dictionary<OperationKey, HeaderKind>
        {
            [new("AchColombiaFileExchange", "ExecuteOutbound")] = HeaderKind.IdempotencyKey,
            [new("AchColombiaFileExchange", "ExecuteInbound")] = HeaderKind.IdempotencyKey,
            [new("AchColombiaFileExchange", "Retry")] = HeaderKind.IdempotencyKey,
            [new("AchClaims", "CreateDev14")] = HeaderKind.IdempotencyKey,
            [new("AchClaims", "RecordProgress")] = HeaderKind.IdempotencyKey,
            [new("AchClaims", "Resolve")] = HeaderKind.IdempotencyKey,
            [new("AchClaims", "Expire")] = HeaderKind.IdempotencyKey,

            [new("AchResponseOperations", "CreateMapping")] = HeaderKind.CorrelationId,
            [new("AchResponseOperations", "UpdateMapping")] = HeaderKind.CorrelationId,
            [new("AchResponseOperations", "ActivateMapping")] = HeaderKind.CorrelationId,
            [new("AchResponseOperations", "DeactivateMapping")] = HeaderKind.CorrelationId,
            [new("AchResponseOperations", "CreateOrphan")] = HeaderKind.CorrelationId,
            [new("AchResponseOperations", "BeginReview")] = HeaderKind.CorrelationId,
            [new("AchResponseOperations", "ResolveOrphan")] = HeaderKind.CorrelationId,
            [new("AchResponseOperations", "Reprocess")] = HeaderKind.CorrelationId,
            [new("OutgoingTransactionMonitoring", "Search")] = HeaderKind.CorrelationId,
            [new("OutgoingTransactionMonitoring", "GetDetail")] = HeaderKind.CorrelationId,
            [new("Scheduler", "Pause")] = HeaderKind.CorrelationId,
            [new("Scheduler", "Resume")] = HeaderKind.CorrelationId,
            [new("Scheduler", "UpdateSchedule")] = HeaderKind.CorrelationId
        };

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

        var key = new OperationKey(action.ControllerName, action.ActionName);
        if (!OperationHeaders.TryGetValue(key, out var headers))
        {
            return Task.CompletedTask;
        }

        if ((headers & HeaderKind.IdempotencyKey) != 0)
        {
            AddHeaderIfMissing(operation, IdempotencyKeyHeaderName, IdempotencyKeyDescription);
        }

        if ((headers & HeaderKind.CorrelationId) != 0)
        {
            AddHeaderIfMissing(operation, CorrelationIdHeaderName, CorrelationIdDescription);
        }

        return Task.CompletedTask;
    }

    private static void AddHeaderIfMissing(OpenApiOperation operation, string name, string description)
    {
        operation.Parameters ??= new List<IOpenApiParameter>();

        if (operation.Parameters.Any(parameter =>
                string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = name,
            In = ParameterLocation.Header,
            Required = false,
            Description = description,
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String
            }
        });
    }

    private readonly record struct OperationKey(string ControllerName, string ActionName);

    [Flags]
    private enum HeaderKind
    {
        None = 0,
        IdempotencyKey = 1,
        CorrelationId = 2
    }
}
