using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaSoapOperationalGate
{
    NachaSoapOperationalGateResult Evaluate(
        NachaSoapExecutionRequest request,
        NachaSoapUatControlOptions options,
        IReadOnlyList<NachaSoapEndpointDescriptor> endpoints);
}
