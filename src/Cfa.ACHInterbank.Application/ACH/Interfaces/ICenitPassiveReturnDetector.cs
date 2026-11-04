using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

/// <summary>Consulta pasiva e independiente de indicios de fallo funcional en CENIT.</summary>
public interface ICenitPassiveReturnDetector
{
    Task<CenitPassiveReturnDetection> DetectAsync(int maxTransactions = 100,
        CancellationToken cancellationToken = default);
}
