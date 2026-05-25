using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaSoapUatReadinessChecker
{
    NachaSoapReadinessCheckResult CheckReadiness(
        string correlationId,
        NachaSoapUatControlOptions options,
        IReadOnlyList<NachaSoapEndpointDescriptor> endpoints);
}
