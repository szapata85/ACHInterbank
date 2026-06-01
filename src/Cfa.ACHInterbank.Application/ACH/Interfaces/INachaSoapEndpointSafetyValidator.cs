using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaSoapEndpointSafetyValidator
{
    NachaSoapEndpointCheckResult Validate(
        NachaSoapEndpointDescriptor endpoint,
        NachaSoapUatControlOptions options);
}
