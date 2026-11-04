using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ICenitReturnPreparationService
{
    Task<CenitReturnPreparationItem> PrepareAsync(PrepareCenitReturnRequest request,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CenitReturnPreparationItem>> ListAsync(int maxItems = 100,
        CancellationToken cancellationToken = default);
}
