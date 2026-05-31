using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaSoapUatConsoleReadModelService
{
    Task<NachaSoapUatConsoleDashboardReadModel> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NachaSoapUatCandidateReadModel>> GetCandidatesAsync(CancellationToken cancellationToken = default);

    Task<NachaSoapUatCandidateReadModel?> GetCandidateAsync(string correlationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NachaSoapUatAuditReadModel>> GetAuditAsync(CancellationToken cancellationToken = default);
}
