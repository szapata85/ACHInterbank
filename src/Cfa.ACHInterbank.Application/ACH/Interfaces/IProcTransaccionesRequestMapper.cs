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

    string BuildSoapBody(ProcTransaccionesRequestContract request);
}
