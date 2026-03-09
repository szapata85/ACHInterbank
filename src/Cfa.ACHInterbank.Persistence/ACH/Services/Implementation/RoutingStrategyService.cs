using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class RoutingStrategyService : IRoutingStrategyService
{
    private readonly AchDbContext _context;
    private readonly IBankHoliday _holidayService;
    private readonly IAchCycleScheduler _cycleScheduler;

    public RoutingStrategyService(
        AchDbContext context,
        IBankHoliday holidayService,
        IAchCycleScheduler cycleScheduler)
    {
        _context = context;
        _holidayService = holidayService;
        _cycleScheduler = cycleScheduler;
    }

    public async Task<string> ResolveClearingHouseForTransactionAsync(
    int destinationInstitutionId,
    DateTime now,
    CancellationToken ct)
    {
        // 1️) Cargar institución y preferencias
        FinancialInstitution? fi = await _context.FinancialInstitutions
            .Include(f => f.ClearingHousePreferences)
            .FirstOrDefaultAsync(f => f.Id == destinationInstitutionId, ct)
            ?? throw new InvalidOperationException("Institución destino no encontrada.");

        // Primero los que son default, luego por prioridad
        List<InstitutionClearingHousePreference> allPreferences = fi.ClearingHousePreferences
            .OrderByDescending(p => p.IsDefault)  // true primero
            .ThenBy(p => NormalizePriority(p.Priority))
            .ThenBy(p => p.Id)
            .ToList();

        if (!allPreferences.Any())
        {
            throw new InvalidOperationException("La institución no tiene cámaras asociadas.");
        }

        List<InstitutionClearingHousePreference> preferences = allPreferences
            .Where(p => p.IsActive)
            .ToList();

        if (!preferences.Any())
        {
            throw new InvalidOperationException("La institución destino no tiene cámaras activas configuradas.");
        }

        // 2️) Próxima fecha hábil
        DateTime processingDate = GetNextBusinessDay(now.Date);

        // 3️) Buscar el siguiente ciclo disponible, avanzando al próximo día hábil
        //     cuando los cortes de hoy ya pasaron.
        for (int attempt = 0; attempt < 15; attempt++)
        {
            // Crear ciclos on-demand solo si faltan
            List<int> prefIds = preferences.Select(p => p.ClearingHouseId).ToList();
            List<int> existing = await _context.AchCycles
                .Where(c => prefIds.Contains(c.ClearingHouseId) &&
                            c.ProcessingDate == processingDate)
                .Select(c => c.ClearingHouseId)
                .Distinct()
                .ToListAsync(ct);

            List<int> missing = prefIds.Except(existing).ToList();
            foreach (int chId in missing)
            {
                await _cycleScheduler.ScheduleCyclesForClearingHouseAsync(chId, processingDate);
            }

            // 4️) Evaluar cámaras en el orden IsDefault + Priority
            foreach (InstitutionClearingHousePreference pref in preferences)
            {
                var nextCycle = await _context.AchCycles
                    .Where(c =>
                        c.ClearingHouseId == pref.ClearingHouseId &&
                        c.ProcessingDate == processingDate &&
                        IsWithinCycleWindow(now, c.ProcessingDate, c.StartTime, c.EndTime))
                    .OrderBy(c => c.ProcessingDate)
                    .ThenBy(c => c.CutoffTime)
                    .FirstOrDefaultAsync(ct);

                if (nextCycle != null)
                    return nextCycle.Id;
            }

            // Si no hay ciclos con cortes futuros en la fecha actual,
            // avanzamos al siguiente día hábil y volvemos a intentar.
            processingDate = GetNextBusinessDay(processingDate.AddDays(1));
        }

        throw new InvalidOperationException(
            "No hay ciclos disponibles para las cámaras habilitadas en el orden de IsDefault y prioridad.");
    }


    private DateTime GetNextBusinessDay(DateTime startDate)
    {
        var date = startDate;
        var holidays = _holidayService.GetHolidays(date.Year)
                                      .Select(h => h.Date)
                                      .ToHashSet();

        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ||
               holidays.Contains(DateOnly.FromDateTime(date)))
        {
            date = date.AddDays(1);

            if (!holidays.Any(h => h.Year == date.Year))
            {
                holidays = _holidayService.GetHolidays(date.Year)
                                          .Select(h => h.Date)
                                          .ToHashSet();
            }
        }
        return date;
    }

    private static int NormalizePriority(int priority)
    {
        if (priority <= 1)
        {
            return 1;
        }

        if (priority >= 3)
        {
            return 3;
        }

        return 2;
    }

    private static bool IsWithinCycleWindow(DateTime now, DateTime processingDate, TimeSpan startTime, TimeSpan endTime)
    {
        if (startTime <= endTime)
        {
            var start = processingDate.Date + startTime;
            var end = processingDate.Date + endTime;
            return now >= start && now <= end;
        }

        var overnightStart = processingDate.Date.AddDays(-1) + startTime;
        var overnightEnd = processingDate.Date + endTime;
        return now >= overnightStart && now <= overnightEnd;
    }
}
