using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaSoapPayloadMapper
{
    NachaSoapPayloadMappingResult Map(NachaIncomingDecision decision, NachaSoapExecutionContext context);
}
