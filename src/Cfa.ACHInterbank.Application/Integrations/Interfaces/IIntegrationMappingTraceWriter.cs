using Cfa.ACHInterbank.Application.Integrations.Models;

namespace Cfa.ACHInterbank.Application.Integrations.Interfaces;

public interface IIntegrationMappingTraceWriter
{
    Task<IntegrationMappingTraceWriteResult> WriteAsync(
        TransactionIntegrationOperationResult operation,
        object sourcePayload,
        int? transactionId,
        string reference,
        string correlationId,
        bool dryRun,
        bool externalTransmission,
        CancellationToken ct = default);
}
