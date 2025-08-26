using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public class AchCycleScheduler : IAchCycleScheduler
{
    private readonly AchDbContext _context;
    private readonly IBankHoliday _holidayService;

    public AchCycleScheduler(AchDbContext context, IBankHoliday holidayService)
    {
        _context = context;
        _holidayService = holidayService;
    }

    public async Task ScheduleCyclesForClearingHouseAsync(int clearingHouseId)
    {
        var clearingHouse = await _context.ClearingHouses
            .FirstOrDefaultAsync(ch => ch.Id == clearingHouseId);

        if (clearingHouse == null) throw new Exception("Clearing house not found");

        var today = DateTime.Today;
        var currentYear = today.Year;

        var holidays = await _context.BankHolidays
            .Where(h => h.Date.Year == currentYear)
            .Select(h => h.Date)
            .ToListAsync();

        // 🔹 Trae las configuraciones activas vigentes
        var cycleConfigs = await _context.ClearingHouseCycleConfigs
            .Where(c => c.ClearingHouseId == clearingHouseId
                     && c.IsActive
                     && c.EffectiveFrom <= today
                     && (c.EffectiveTo == null || c.EffectiveTo >= today))
            .ToListAsync();

        foreach (var config in cycleConfigs)
        {
            var processingDate = today;

            if (holidays.Contains(DateOnly.FromDateTime(processingDate)) ||
                processingDate.DayOfWeek == DayOfWeek.Saturday ||
                processingDate.DayOfWeek == DayOfWeek.Sunday)
            {
                if (true) // puedes leer config.RescheduleOnHoliday si la agregas
                {
                    processingDate = GetNextBusinessDay(processingDate, holidays);
                }
                else
                {
                    continue;
                }
            }

            var exists = await _context.AchCycles.AnyAsync(c =>
                c.ClearingHouseId == clearingHouseId &&
                c.ProcessingDate == processingDate &&
                c.CycleName == config.CycleName);

            if (!exists)
            {
                _context.AchCycles.Add(new AchCycle
                {
                    ClearingHouseId = clearingHouseId,
                    CycleName = config.CycleName,
                    CutoffTime = config.CutoffTime,
                    ProcessingDate = processingDate,
                    RescheduleOnHoliday = true // o parametrizable si lo agregas en config
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

    public DateTime GetNextValidProcessingDate(DateTime baseDate)
    {
        var date = baseDate;

        while (IsNonWorkingDay(date))
        {
            date = date.AddDays(1);
        }

        return date;
    }

    private bool IsNonWorkingDay(DateTime date)
    {
        List<Domain.Models.ACH.BankHolidayModel> holidays = _holidayService.GetHolidays(date.Year);

        // 1. Convierte el DateTime de entrada a DateOnly para la comparación.
        DateOnly dateOnly = DateOnly.FromDateTime(date);

        // 2. Verifica si es fin de semana.
        bool isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

        // 3. Usa Any() para buscar un BankHoliday cuya fecha coincida con la fecha que se está evaluando.
        //    Esto compara dos objetos DateOnly.
        bool isBankHoliday = holidays.Any(h => h.Date == dateOnly);

        // Retorna true si es fin de semana o si es un día festivo.
        return isWeekend || isBankHoliday;
    }

    private DateTime GetNextBusinessDay(DateTime date, List<DateOnly> holidays)
    {
        do
        {
            date = date.AddDays(1);
        } while (holidays.Contains(DateOnly.FromDateTime(date.Date)) || date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday);

        return date;
    }
}
