using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaOperationalReadStore
{
    Task<NachaOperationalDashboardReadModel> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<NachaOperationalSummaryReadModel> GetOperationalSummaryAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NachaOperationalFileReadModel>> GetOperationalFilesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NachaOperationalDecisionReadModel>> GetOperationalDecisionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NachaSoapReadinessReadModel>> GetSoapReadinessAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NachaOperationalAuditReadModel>> GetOperationalAuditAsync(CancellationToken cancellationToken = default);
}
