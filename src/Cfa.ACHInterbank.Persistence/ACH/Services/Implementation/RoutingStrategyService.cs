using Cfa.ACHInterbank.Application.ACH.Interfaces;
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

    public async Task<int> ResolveClearingHouseForTransactionAsync(
        int destinationInstitutionId,
        DateTime now,
        CancellationToken ct)
    {
        var fi = await _context.FinancialInstitutions
            .Include(f => f.ClearingHousePreferences)
            .FirstOrDefaultAsync(f => f.Id == destinationInstitutionId, ct)
            ?? throw new InvalidOperationException("Institución destino no encontrada.");

        // Preferencias default (o todas si no hay default)
        var prefsQuery = fi.ClearingHousePreferences.AsQueryable();
        var hasDefaults = prefsQuery.Any(p => p.IsDefault);

        var prefs = (hasDefaults ? prefsQuery.Where(p => p.IsDefault) : prefsQuery)
            .OrderBy(p => p.Priority)
            .ToList();

        if (!prefs.Any())
            throw new InvalidOperationException("La institución no tiene preferencias de cámara configuradas.");

        // 1️⃣ Avanzar a la próxima fecha hábil si hoy es festivo o fin de semana
        var processingDate = GetNextBusinessDay(now.Date);

        // 2️⃣ Buscar ciclo abierto en esa fecha
        foreach (var pref in prefs)
        {
            var openCycle = await _context.AchCycles
                .Where(c => c.ClearingHouseId == pref.ClearingHouseId &&
                            c.ProcessingDate == processingDate &&
                            c.CutoffTime > now.TimeOfDay)
                .OrderBy(c => c.CutoffTime)
                .FirstOrDefaultAsync(ct);

            if (openCycle != null)
                return openCycle.Id;
        }

        // ...
        // 3️⃣ Si no hay ciclos abiertos, buscar el próximo ciclo válido en días hábiles
        foreach (var pref in prefs)
        {
            var nextCycle = await _context.AchCycles
                .Where(c => c.ClearingHouseId == pref.ClearingHouseId &&
                            c.ProcessingDate >= processingDate)
                .OrderBy(c => c.ProcessingDate)
                .ThenBy(c => c.CutoffTime)
                .FirstOrDefaultAsync(ct);

            if (nextCycle == null)
            {
                // ⚡ Crear ciclos “on-demand” para la fecha hábil
                await _cycleScheduler.ScheduleCyclesForClearingHouseAsync(
                    pref.ClearingHouseId, processingDate);

                // Reintentar una vez creada la programación
                nextCycle = await _context.AchCycles
                    .Where(c => c.ClearingHouseId == pref.ClearingHouseId &&
                                c.ProcessingDate == processingDate)
                    .OrderBy(c => c.CutoffTime)
                    .FirstOrDefaultAsync(ct);
            }

            if (nextCycle != null)
                return nextCycle.Id;
        }


        throw new InvalidOperationException("No hay ciclos disponibles para las cámaras preferidas.");
    }

    /// Devuelve la próxima fecha hábil (no fin de semana, no festivo)
    private DateTime GetNextBusinessDay(DateTime startDate)
    {
        var date = startDate;
        var holidays = _holidayService.GetHolidays(date.Year)
                                      .Select(h => h.Date)
                                      .ToHashSet();

        while (date.DayOfWeek == DayOfWeek.Saturday ||
               date.DayOfWeek == DayOfWeek.Sunday ||
               holidays.Contains(DateOnly.FromDateTime(date)))
        {
            date = date.AddDays(1);

            // Si cambia de año, refresca el listado de festivos
            if (!holidays.Any(h => h.Year == date.Year))
            {
                holidays = _holidayService.GetHolidays(date.Year)
                                          .Select(h => h.Date)
                                          .ToHashSet();
            }
        }
        return date;
    }
}
