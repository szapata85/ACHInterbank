using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Helpers;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public class AchCycleScheduler : IAchCycleScheduler
{
    private readonly AchDbContext _context;
    private readonly IBankHoliday _holidayService;
    private readonly IServiceProvider _provider;

    public AchCycleScheduler(AchDbContext context,
                             IBankHoliday holidayService, IServiceProvider provider)
    {
        _context = context;
        _holidayService = holidayService;
        _provider = provider;
    }

    public async Task ScheduleCyclesForClearingHouseAsync(int clearingHouseId)
    {
        // Servicio para obtener el próximo día hábil
        var txService = _provider.GetRequiredService<IAchTransactionService>();
        DateTime nextBusinessDate = await txService.GetNextBusinessDayAsync(DateTime.Now);

        // ✅ Validar que no existan ciclos para esa cámara en la fecha
        bool exists = await _context.AchCycles
            .AnyAsync(c => c.ClearingHouseId == clearingHouseId &&
                           c.ProcessingDate.Date == nextBusinessDate.Date);
        if (exists)
        {
            // Si ya hay ciclos, no continuar
            return;
        }

        // 🔄 Si quieres programar para TODAS las cámaras, hazlo una sola vez
        var houseIds = await _context.ClearingHouses
            .Select(ch => ch.Id)
            .ToListAsync();

        foreach (int id in houseIds)
        {
            // Validar por cada cámara para la misma fecha
            bool alreadyHas = await _context.AchCycles
                .AnyAsync(c => c.ClearingHouseId == id &&
                               c.ProcessingDate.Date == nextBusinessDate.Date);

            if (!alreadyHas)
            {
                await ScheduleCyclesForClearingHouseAsync(id, nextBusinessDate);
            }
        }
    }


    public async Task ScheduleCyclesForClearingHouseAsync(int clearingHouseId, DateTime processingDate)
    {


        ClearingHouse? clearingHouse = await _context.ClearingHouses
                           .Include(ch => ch.ClearingHouseConfig) // solo si necesitas la config
                           .FirstOrDefaultAsync(ch => ch.Id == clearingHouseId);


        if (clearingHouse == null)
            throw new InvalidOperationException("Clearing house not found");

        // 🔹 Obtener ciclos vigentes por nombre en la fecha de procesamiento
        List<ClearingHouseCycleConfig> cycles = await _context.ClearingHouseCycleConfigs
            .Where(cfg => cfg.ClearingHouseId == clearingHouse.Id &&
                          cfg.IsActive &&
                          cfg.EffectiveFrom.Date <= processingDate.Date &&
                          (!cfg.EffectiveTo.HasValue || cfg.EffectiveTo.Value.Date >= processingDate.Date))
            .GroupBy(cfg => cfg.CycleName)
            .Select(g => g.OrderByDescending(cfg => cfg.EffectiveFrom).ThenByDescending(cfg => cfg.Id).First())
            .OrderBy(cfg => cfg.CutoffTime)
            .ToListAsync();



        // Festivos y fechas especiales de la cámara para ese año
        var holidays = await _context.BankHolidays
            .Where(h => h.Date.Year == processingDate.Year)
            .Select(h => h.Date)
            .ToListAsync();

        var specialDates = await _context.ClearingHouseSpecialDates
            .Where(d => d.ClearingHouseId == clearingHouseId && d.Date.Year == processingDate.Year)
            .Select(d => d.Date)
            .ToListAsync();

        // Saltar si la fecha no es hábil
        if (processingDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ||
            holidays.Contains(DateOnly.FromDateTime(processingDate)) ||
            specialDates.Contains(DateOnly.FromDateTime(processingDate)))
        {
            return;
        }

        // Crear los ciclos para la fecha indicada según la configuración
        foreach (var cfg in cycles)
        {
            bool exists = await _context.AchCycles.AnyAsync(c =>
                c.ClearingHouseId == clearingHouseId &&
                c.CycleName == cfg.CycleName &&
                c.ProcessingDate.Date == processingDate.Date);

            if (!exists)
            {
                _context.AchCycles.Add(new AchCycle
                {
                    Id = AchCycleIdHelper.GenerateId(clearingHouseId, cfg.CycleName, processingDate.Date),
                    ClearingHouseId = clearingHouseId,
                    CycleName = cfg.CycleName,
                    ProcessingDate = processingDate.Date,
                    StartTime = cfg.StartTime,
                    EndTime = cfg.EndTime,
                    CutoffTime = cfg.CutoffTime,
                    RescheduleOnHoliday = true
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
