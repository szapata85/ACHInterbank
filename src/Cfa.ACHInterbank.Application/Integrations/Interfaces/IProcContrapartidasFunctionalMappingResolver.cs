using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.Integrations.Interfaces;

public interface IProcContrapartidasFunctionalMappingResolver
{
    Task<ProcContrapartidasRequestResolution?> TryResolveAsync(
        AchCycle cycle,
        IReadOnlyCollection<AchTransaction> transactions,
        DateTime executionDateTime,
        CancellationToken ct = default);
}
