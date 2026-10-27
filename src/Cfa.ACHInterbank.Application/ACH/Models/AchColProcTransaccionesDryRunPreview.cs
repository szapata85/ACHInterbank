namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>
/// F5.2 diagnostic-only projection. This is NOT a sendable integration request:
/// its SOAP-shaped XML intentionally omits all unhomologated values.
/// No caller may convert this into a ProcTransaccionesRequestContract for transport.
/// </summary>
public sealed record AchColProcTransaccionesDryRunPreview(
    Guid DispatchQueueId,
    int EntryDetailId,
    Guid MappingSetId,
    int MappingVersion,
    IReadOnlyDictionary<string, string> ObservedCandidateParameters,
    IReadOnlyList<string> PendingHomologationParameters,
    IReadOnlyList<string> MissingRequiredParameters,
    string SoapPreviewXml,
    string BlockingCode)
{
    // Immutable fail-closed marker; callers cannot set this to true with a record clone.
    public bool CanTransmit => false;
}
