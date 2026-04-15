using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ICenitCycleQueueService
{
    Task<CenitCycleQueue> EnqueueAsync(AchTransaction transaction, DateTime receivedAtUtc, string reason, CancellationToken ct);
}
