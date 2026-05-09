using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Responses.Repositories;

public interface IAchResponseNotificationAttemptRepository
{
    Task AddAsync(AchResponseNotificationAttempt attempt, CancellationToken cancellationToken = default);
    Task<int> GetNextAttemptNumberAsync(Guid achResponseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AchResponseNotificationAttempt>> FindByResponseIdAsync(Guid achResponseId, CancellationToken cancellationToken = default);
    Task<AchResponseNotificationAttempt?> FindByIdAsync(long id, CancellationToken cancellationToken = default);
    Task UpdateAsync(AchResponseNotificationAttempt attempt, CancellationToken cancellationToken = default);
}
