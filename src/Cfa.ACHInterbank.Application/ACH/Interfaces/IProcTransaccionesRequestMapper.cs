using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IProcTransaccionesRequestMapper
{
    Task<ProcTransaccionesRequestResolution> ResolveAsync(
        IncomingNachaDispatchQueue queueItem,
        IncomingNachaFileIngestion ingestion,
        IncomingNachaEntryClassification classification,
        AchTransaction transaction,
        AchCycle cycle,
        DateTime executionDateTime,
        CancellationToken ct = default);

    /// <summary>
    /// Build an ACHCOL NACHA entry-scoped, non-transmittable DryRun preview.
    /// Does not invoke SOAP, mutate queue state, or fabricate an AchCycle.
    /// </summary>
    Task<AchColProcTransaccionesDryRunPreview> PreviewAchColFileBasedAsync(
        IncomingNachaDispatchQueue queueItem,
        IncomingNachaFileIngestion ingestion,
        IncomingNachaEntryClassification classification,
        CancellationToken ct = default);

    string BuildSoapBody(ProcTransaccionesRequestContract request);
}
