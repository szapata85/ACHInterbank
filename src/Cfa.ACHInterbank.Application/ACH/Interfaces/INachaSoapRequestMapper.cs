using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaSoapRequestMapper
{
    NachaSoapMappedRequest Map(NachaSoapExecutionRequest request);
}
