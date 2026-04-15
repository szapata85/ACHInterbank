using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class CenitCycleQueueService : ICenitCycleQueueService
{
    private readonly ICenitOperatingCalendarPolicy _calendarPolicy;
    private readonly AchDbContext _context;

    public CenitCycleQueueService(ICenitOperatingCalendarPolicy calendarPolicy, AchDbContext context)
    {
        _calendarPolicy = calendarPolicy;
        _context = context;
    }

    public async Task<CenitCycleQueue> EnqueueAsync(AchTransaction transaction, DateTime receivedAtUtc, string reason, CancellationToken ct)
    {
        var clearingHouseId = await _context.AchCycles
            .Where(x => x.Id == transaction.AchCycleId)
            .Select(x => x.ClearingHouseId)
            .FirstAsync(ct);
        var targetCycle = await _calendarPolicy.ResolveTargetCycleAsync(clearingHouseId, receivedAtUtc, ct);
        var queue = new CenitCycleQueue
        {
            AchTransactionId = transaction.Id,
            OriginalAchCycleId = transaction.AchCycleId,
            TargetAchCycleId = targetCycle.Id,
            QueueReason = reason,
            Status = "Queued",
            EnqueuedAtUtc = DateTime.UtcNow
        };

        _context.CenitCycleQueues.Add(queue);
        await _context.SaveChangesAsync(ct);
        return queue;
    }
}
