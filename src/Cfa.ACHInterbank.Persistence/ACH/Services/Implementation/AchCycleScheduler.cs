using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public class AchCycleScheduler : IAchCycleScheduler
{
    private readonly AchDbContext _context;

    public AchCycleScheduler(AchDbContext context)
    {
        _context = context;
    }

    public async Task ScheduleCyclesForClearingHouseAsync(int clearingHouseId)
    {
        var clearingHouse = await _context.ClearingHouses
            .Include(ch => ch.AchCycles)
            .FirstOrDefaultAsync(ch => ch.Id == clearingHouseId);

        if (clearingHouse == null) throw new Exception("Clearing house not found");

        var currentYear = DateTime.Now.Year;
        var holidays = await _context.BankHolidays
            .Where(h => h.Date.Year == currentYear)
            .Select(h => h.Date)
            .ToListAsync();

        foreach (var cycle in clearingHouse.AchCycles)
        {
            var processingDate = cycle.ProcessingDate;

            if (holidays.Contains(processingDate.Date) ||
                processingDate.DayOfWeek == DayOfWeek.Saturday ||
                processingDate.DayOfWeek == DayOfWeek.Sunday)
            {
                if (cycle.RescheduleOnHoliday)
                {
                    processingDate = GetNextBusinessDay(processingDate, holidays);
                }
                else
                {
                    continue; // Skip if cannot reschedule
                }
            }

            var exists = await _context.AchCycles.AnyAsync(c =>
                c.ClearingHouseId == clearingHouseId &&
                c.ProcessingDate == processingDate &&
                c.CycleName == cycle.CycleName);

            if (!exists)
            {
                _context.AchCycles.Add(new AchCycle
                {
                    ClearingHouseId = clearingHouseId,
                    CycleName = cycle.CycleName,
                    CutoffTime = cycle.CutoffTime,
                    ProcessingDate = processingDate,
                    RescheduleOnHoliday = cycle.RescheduleOnHoliday
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<AchCycle>> GetScheduledCyclesAsync(int clearingHouseId, DateTime date)
    {
        return await _context.AchCycles
            .Where(c => c.ClearingHouseId == clearingHouseId && c.ProcessingDate.Date == date.Date)
            .ToListAsync();
    }

    private DateTime GetNextBusinessDay(DateTime date, List<DateTime> holidays)
    {
        do
        {
            date = date.AddDays(1);
        } while (holidays.Contains(date.Date) || date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday);

        return date;
    }
}
