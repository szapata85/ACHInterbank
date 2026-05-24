namespace Cfa.ACHInterbank.Application.Integrations.Models;

public sealed record IntegrationMappingTraceWriteResult(
    Guid TraceId,
    int EntryCount,
    IReadOnlyCollection<string> MissingRequiredFields,
    IReadOnlyCollection<string> Errors);
