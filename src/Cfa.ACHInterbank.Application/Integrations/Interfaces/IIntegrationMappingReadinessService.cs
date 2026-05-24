using Cfa.ACHInterbank.Application.Integrations.Models;

namespace Cfa.ACHInterbank.Application.Integrations.Interfaces;

public interface IIntegrationMappingReadinessService
{
    Task<IntegrationMappingReadinessResult> EvaluateAsync(
        TransactionIntegrationOperationResult operation,
        CancellationToken ct = default);

    Task<IntegrationMappingReadinessResult> EvaluateAsync(
        string integrationKey,
        string operationKey,
        string mappingPurpose,
        string mappingDirection,
        int? transactionId = null,
        object? sourcePayload = null,
        CancellationToken ct = default);
}
