using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IProcContrapartidasRequestMapper
{
    ProcContrapartidasRequestContract Map(
        AchCycle cycle,
        IReadOnlyCollection<AchTransaction> transactions,
        DateTime executionDateTime);

    string BuildSoapBody(ProcContrapartidasRequestContract request);
}
