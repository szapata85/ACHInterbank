namespace Cfa.ACHInterbank.Application.Integrations.Models;

public sealed record TransactionIntegrationResultItemDto(
    long? CatalogId,
    string Method,
    string TransportStatus,
    string BusinessStatus,
    string ResponseCode,
    string ResponseDescription,
    DateTime? ProcessedAt,
    int AttemptNumber,
    bool RetryAllowed,
    bool RequiresManualReview,
    string TransactionState);

public sealed record TransactionIntegrationResultDto(
    int TransactionId,
    TransactionIntegrationResultItemDto? Latest,
    IReadOnlyList<TransactionIntegrationResultItemDto> History);
