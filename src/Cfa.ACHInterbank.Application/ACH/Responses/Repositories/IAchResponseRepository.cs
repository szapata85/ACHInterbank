using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Application.ACH.Responses.Queries.Models;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Repositories;

public interface IAchResponseRepository
{
    Task<AchResponse?> FindByIdempotencyHashAsync(string hashIdempotencia, CancellationToken cancellationToken = default);
    Task AddAsync(AchResponse response, CancellationToken cancellationToken = default);
    Task UpdateAsync(AchResponse response, CancellationToken cancellationToken = default);
    Task<PagedResult<AchResponseListItemModel>> SearchAsync(AchResponseSearchQuery query, CancellationToken cancellationToken = default);
    Task<AchResponseDashboardModel> GetDashboardAsync(AchResponseDashboardQuery query, CancellationToken cancellationToken = default);
    Task<AchResponseDetailModel?> FindDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAuditAsync(AchResponseAudit audit, CancellationToken cancellationToken = default);
}
