using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaSoapCertificateReadinessValidator
{
    NachaSoapCertificateCheckResult Validate(
        NachaSoapEndpointDescriptor endpoint,
        NachaSoapUatControlOptions options);
}
