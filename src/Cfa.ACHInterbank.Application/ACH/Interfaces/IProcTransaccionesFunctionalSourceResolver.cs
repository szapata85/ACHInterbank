using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IProcTransaccionesFunctionalSourceResolver
{
    Task<ProcTransaccionesFunctionalSourceContext> ResolveAsync(
        IncomingNachaDispatchQueue queueItem,
        IncomingNachaFileIngestion ingestion,
        IncomingNachaEntryClassification classification,
        AchTransaction transaction,
        AchCycle cycle,
        CancellationToken ct = default);

    Task<ProcTransaccionesFunctionalSourceContext> ResolveAsync(
        IntegrationExecutionContext context,
        CancellationToken ct = default);
}
