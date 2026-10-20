using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

/// <summary>
/// Punto de extensión para un transporte real. No existe una implementación simulada:
/// solo un adaptador con evidencia externa verificable puede avanzar el ciclo de vida.
/// </summary>
public interface IAchFileTransmissionEvidenceRecorder
{
    Task RecordAsync(AchFileTransmissionEvidence evidence, CancellationToken ct = default);
}
