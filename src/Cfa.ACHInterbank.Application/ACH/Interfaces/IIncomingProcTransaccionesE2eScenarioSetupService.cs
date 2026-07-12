using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IIncomingProcTransaccionesE2eScenarioSetupService
{
    Task<IncomingProcTransaccionesE2eScenarioResult> InspectAsync(
        IncomingProcTransaccionesE2eScenarioRequest request,
        CancellationToken ct = default);

    Task<IncomingProcTransaccionesE2eScenarioResult> EnsureAsync(
        IncomingProcTransaccionesE2eScenarioRequest request,
        CancellationToken ct = default);
}
