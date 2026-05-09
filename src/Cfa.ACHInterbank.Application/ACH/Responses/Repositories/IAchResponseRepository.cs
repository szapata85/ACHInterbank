using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Repositories;

public interface IAchResponseRepository
{
    Task<AchResponse?> FindByIdempotencyHashAsync(string hashIdempotencia, CancellationToken cancellationToken = default);
    Task AddAsync(AchResponse response, CancellationToken cancellationToken = default);
    Task UpdateAsync(AchResponse response, CancellationToken cancellationToken = default);
}
