using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaOperationalReadModelService
{
    Task<NachaOperationalDashboardReadModel> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<NachaOperationalSummaryReadModel> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NachaOperationalFileReadModel>> GetFilesAsync(CancellationToken cancellationToken = default);

    Task<NachaOperationalFileDetailReadModel?> GetFileDetailAsync(string fileId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NachaOperationalDecisionReadModel>> GetDecisionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NachaSoapReadinessReadModel>> GetSoapReadinessAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NachaOperationalAuditReadModel>> GetAuditAsync(CancellationToken cancellationToken = default);
}
