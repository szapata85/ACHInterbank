using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.Application.Integrations.Models;

public sealed record IntegrationResponseCatalogResult(
    long? CatalogId,
    string Code,
    string Description,
    string Source,
    string Category,
    string Method,
    IntegrationResponseBusinessStatus BusinessStatus,
    bool RetryAllowed,
    bool RequiresManualReview,
    bool IsActive,
    string TargetTransactionState,
    bool IsKnownCode);
