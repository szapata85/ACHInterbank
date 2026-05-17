using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchReturnOfReturnFileGenerationService
{
    Task<AchReturnOfReturnFileGenerationResult> GenerateAsync(
        AchReturnOfReturnFileGenerationRequest request,
        CancellationToken cancellationToken);

    Task<AchReturnOfReturnFileGenerationResult> GenerateNachaAsync(
        AchReturnOfReturnFileGenerationRequest request,
        CancellationToken cancellationToken);
}
