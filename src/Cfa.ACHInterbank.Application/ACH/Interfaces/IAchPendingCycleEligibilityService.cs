using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

/// <summary>
/// Consulta de solo lectura: decide si una transacción pendiente podría cambiar de ciclo.
/// La decisión NO autoriza escrituras; I02 tendrá que revalidarla de forma atómica.
/// </summary>
public interface IAchPendingCycleEligibilityService
{
    Task<AchPendingCycleEligibilityResult> EvaluateAsync(int transactionId, CancellationToken ct = default);
}
