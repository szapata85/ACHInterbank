using Cfa.ACHInterbank.Application.Integrations.Models;

namespace Cfa.ACHInterbank.Application.Integrations.Interfaces;

public interface ISoapRequestBodyWriter
{
    string Write(ResolvedIntegrationRequest request);
}
