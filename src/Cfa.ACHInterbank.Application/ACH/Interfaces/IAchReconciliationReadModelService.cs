using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchReconciliationReadModelService
{
    Task<AchReconciliationDashboardReadModel> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AchReconciliationItemReadModel>> GetItemsAsync(CancellationToken cancellationToken = default);
    Task<AchReconciliationDetailReadModel?> GetItemAsync(string reconciliationId, CancellationToken cancellationToken = default);
    Task<AchReconciliationDetailReadModel?> GetItemByCorrelationAsync(string correlationId, CancellationToken cancellationToken = default);
}
