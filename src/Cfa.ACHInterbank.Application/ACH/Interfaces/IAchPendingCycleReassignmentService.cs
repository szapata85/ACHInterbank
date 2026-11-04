using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

/// <summary>Comando focal I02; no agenda tareas ni transmite archivos.</summary>
public interface IAchPendingCycleReassignmentService
{
    Task<AchPendingCycleReassignmentResult> ReassignAsync(int transactionId, CancellationToken ct = default);
}
