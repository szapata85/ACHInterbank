using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class CenitOperatingCalendarPolicy : ICenitOperatingCalendarPolicy
{
    private const string CenitCode = "CENIT";
    private readonly AchDbContext _context;
    private readonly ICycleNumberResolver _cycleNumberResolver;
    private readonly IOperationalCycleWindowResolver _windowResolver;

    public CenitOperatingCalendarPolicy(
        AchDbContext context,
        ICycleNumberResolver? cycleNumberResolver = null,
        IOperationalCycleWindowResolver? windowResolver = null)
    {
        _context = context;
        _cycleNumberResolver = cycleNumberResolver ?? new CycleNumberResolver();
        _windowResolver = windowResolver ?? new OperationalCycleWindowResolver();
    }

    public async Task ValidateCycleConsistencyAsync(int clearingHouseId, DateTime processingDate, CancellationToken ct)
    {
        var clearingHouse = await _context.ClearingHouses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == clearingHouseId, ct);

        if (clearingHouse is null || !string.Equals(clearingHouse.Code, CenitCode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var cycleConfigs = await _context.ClearingHouseCycleConfigs
            .AsNoTracking()
            .Where(x => x.ClearingHouseId == clearingHouseId
                        && x.IsActive
                        && x.EffectiveFrom.Date <= processingDate.Date
                        && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= processingDate.Date))
            .ToListAsync(ct);

        // Provider compatibility: SQLite can't translate ORDER BY TimeSpan in all cases.
        // Sort in-memory after materialization; configuration set is small and bounded.
        cycleConfigs = cycleConfigs
            .OrderBy(x => x.StartTime)
            .ToList();

        if (cycleConfigs.Count != 5)
        {
            throw new InvalidOperationException($"CENIT debe operar con 5 ciclos diarios. Se encontraron {cycleConfigs.Count} ciclos activos para {processingDate:yyyy-MM-dd}.");
        }

        var ordered = cycleConfigs
            .Select(x => _cycleNumberResolver.Resolve(x.CycleName) ?? -1)
            .OrderBy(x => x)
            .ToArray();

        var expected = new[] { 1, 2, 3, 4, 5 };
        if (!ordered.SequenceEqual(expected))
        {
            throw new InvalidOperationException("CENIT requiere ciclos operativos consecutivos 1..5.");
        }
    }

    public async Task<AchCycle> ResolveTargetCycleAsync(int clearingHouseId, DateTime receivedAtUtc, CancellationToken ct)
    {
        var clearingHouse = await _context.ClearingHouses
            .AsNoTracking()
            .Include(house => house.ClearingHouseConfig)
            .SingleAsync(house => house.Id == clearingHouseId, ct);
        var timeZoneId = clearingHouse.ClearingHouseConfig.TimeZoneId;
        var receivedInstant = new DateTimeOffset(
            DateTime.SpecifyKind(receivedAtUtc, DateTimeKind.Utc),
            TimeSpan.Zero);
        var localNow = _windowResolver.Resolve(
            receivedAtUtc.Date,
            TimeSpan.Zero,
            new TimeSpan(23, 59, 59),
            timeZoneId,
            receivedInstant).LocalNow;
        var date = localNow.Date;
        var cycles = await _context.AchCycles
            .Where(x => x.ClearingHouseId == clearingHouseId && x.ProcessingDate.Date == date)
            .OrderBy(x => x.CutoffTime)
            .ToListAsync(ct);

        if (cycles.Count == 0)
        {
            throw new InvalidOperationException("No existen ciclos programados para determinar ciclo objetivo CENIT.");
        }

        var target = cycles.FirstOrDefault(cycle => receivedInstant <= _windowResolver.Resolve(
            cycle.ProcessingDate,
            cycle.StartTime,
            cycle.EndTime,
            timeZoneId,
            receivedInstant).EndInstant);
        if (target is not null)
        {
            return target;
        }

        var nextCycle = await _context.AchCycles
            .Where(x => x.ClearingHouseId == clearingHouseId && x.ProcessingDate.Date > date)
            .OrderBy(x => x.ProcessingDate)
            .ThenBy(x => x.CutoffTime)
            .FirstOrDefaultAsync(ct);

        return nextCycle ?? cycles[^1];
    }
}
