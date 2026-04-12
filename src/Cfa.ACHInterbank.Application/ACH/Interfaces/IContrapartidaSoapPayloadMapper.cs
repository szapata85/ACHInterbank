using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IContrapartidaSoapPayloadMapper
{
    IReadOnlyDictionary<string, object?> BuildProcContrapartidasPayload(
        AchCycle cycle,
        IReadOnlyCollection<AchTransaction> transactions,
        DateTime executionDateTime);
}
