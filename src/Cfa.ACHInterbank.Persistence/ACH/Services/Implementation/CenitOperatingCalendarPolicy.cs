using Cfa.ACHInterbank.Application.ACH.Interfaces;
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

    public CenitOperatingCalendarPolicy(AchDbContext context)
    {
        _context = context;
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
            .OrderBy(x => x.StartTime)
            .ToListAsync(ct);

        if (cycleConfigs.Count != 5)
        {
            throw new InvalidOperationException($"CENIT debe operar con 5 ciclos diarios. Se encontraron {cycleConfigs.Count} ciclos activos para {processingDate:yyyy-MM-dd}.");
        }

        var ordered = cycleConfigs
            .Select(x => ParseCycleIndex(x.CycleName))
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
        var date = receivedAtUtc.Date;
        var cycles = await _context.AchCycles
            .Where(x => x.ClearingHouseId == clearingHouseId && x.ProcessingDate.Date == date)
            .OrderBy(x => x.CutoffTime)
            .ToListAsync(ct);

        if (cycles.Count == 0)
        {
            throw new InvalidOperationException("No existen ciclos programados para determinar ciclo objetivo CENIT.");
        }

        var nowTime = receivedAtUtc.TimeOfDay;
        var target = cycles.FirstOrDefault(x => nowTime <= x.CutoffTime);
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

    private static int ParseCycleIndex(string cycleName)
    {
        var digits = new string(cycleName.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var value) ? value : -1;
    }
}
