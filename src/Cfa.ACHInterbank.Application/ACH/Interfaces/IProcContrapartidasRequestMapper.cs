using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IProcContrapartidasRequestMapper
{
    Task<ProcContrapartidasRequestResolution> ResolveAsync(
        AchCycle cycle,
        IReadOnlyCollection<AchTransaction> transactions,
        DateTime executionDateTime,
        CancellationToken ct = default);

    string BuildSoapBody(ProcContrapartidasRequestContract request);
}
