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
    private readonly IServiceProvider _provider;
    private readonly ICenitOperatingCalendarPolicy _cenitCalendarPolicy;
    private readonly TimeProvider _timeProvider;
    private readonly IOperationalCycleWindowResolver _windowResolver;
    private readonly IOperationalCalendarService _calendar;
    private readonly IClearingHouseCyclePolicyResolver _cyclePolicyResolver;

    public AchCycleScheduler(AchDbContext context,
                             IBankHoliday holidayService,
                             IServiceProvider provider,
                             ICenitOperatingCalendarPolicy cenitCalendarPolicy,
                             TimeProvider? timeProvider = null,
                             IOperationalCycleWindowResolver? windowResolver = null,
                             IOperationalCalendarService? calendar = null,
                             IClearingHouseCyclePolicyResolver? cyclePolicyResolver = null)
    {
        _context = context;
        _ = holidayService;
        _provider = provider;
        _cenitCalendarPolicy = cenitCalendarPolicy;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _windowResolver = windowResolver ?? new OperationalCycleWindowResolver();
        _calendar = calendar ?? new OperationalCalendarService(context);
        _cyclePolicyResolver = cyclePolicyResolver
            ?? new ClearingHouseCyclePolicyResolver(context, new Cfa.ACHInterbank.Application.ACH.Services.CycleNumberResolver());
    }

    public async Task ScheduleCyclesForClearingHouseAsync(int clearingHouseId)
    {
        var clearingHouse = await _context.ClearingHouses
            .AsNoTracking()
            .Include(house => house.ClearingHouseConfig)
            .SingleOrDefaultAsync(house => house.Id == clearingHouseId)
            ?? throw new InvalidOperationException("Clearing house not found");
        var localNow = _windowResolver.Resolve(
            _timeProvider.GetUtcNow().UtcDateTime.Date,
            TimeSpan.Zero,
            new TimeSpan(23, 59, 59),
            clearingHouse.ClearingHouseConfig.TimeZoneId,
            _timeProvider.GetUtcNow()).LocalNow;
        var nextBusinessDate = await _calendar.GetNextBusinessDayAsync(
            DateOnly.FromDateTime(localNow),
            clearingHouseId);

        // La generación es idempotente ciclo por ciclo. No saltar toda la fecha cuando
        // ya existe un subconjunto de ciclos: podrían faltar registros por una caída.
        await ScheduleCyclesForClearingHouseAsync(
            clearingHouseId,
            nextBusinessDate.ToDateTime(TimeOnly.MinValue));
    }


    public async Task ScheduleCyclesForClearingHouseAsync(int clearingHouseId, DateTime processingDate)
    {


        ClearingHouse? clearingHouse = await _context.ClearingHouses
                           .Include(ch => ch.ClearingHouseConfig) // solo si necesitas la config
                           .FirstOrDefaultAsync(ch => ch.Id == clearingHouseId);


        if (clearingHouse == null)
            throw new InvalidOperationException("Clearing house not found");

        await _cenitCalendarPolicy.ValidateCycleConsistencyAsync(clearingHouse.Id, processingDate, CancellationToken.None);

        var policy = await _cyclePolicyResolver.ResolveAsync(
            clearingHouse.Id,
            processingDate,
            CancellationToken.None);
        var cycles = policy.Cycles;


        if (!await _calendar.IsBusinessDayAsync(
                DateOnly.FromDateTime(processingDate),
                clearingHouseId))
        {
            return;
        }

        // Cargar una sola vez los ciclos de esta fecha. El alta de una transacción
        // puede invocar el respaldo de Quartz muchas veces durante el día.
        var existingCycles = await _context.AchCycles
            .Where(c => c.ClearingHouseId == clearingHouseId
                && c.ProcessingDate.Date == processingDate.Date)
            .ToListAsync();
        var byName = existingCycles.ToDictionary(c => c.CycleName, StringComparer.OrdinalIgnoreCase);

        foreach (var cfg in cycles)
        {
            if (!byName.TryGetValue(cfg.CycleName, out var existing))
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
                    OutputReleaseTime = cfg.OutputReleaseTime,
                    CompensationStatementStartTime = cfg.CompensationStatementStartTime,
                    CompensationStatementEndTime = cfg.CompensationStatementEndTime,
                    CompensationPaymentStartTime = cfg.CompensationPaymentStartTime,
                    CompensationPaymentEndTime = cfg.CompensationPaymentEndTime,
                    OutputReleaseStartTime = cfg.OutputReleaseStartTime,
                    OutputReleaseEndTime = cfg.OutputReleaseEndTime,
                    RescheduleOnHoliday = true,
                    ClearingHouseCycleConfigId = cfg.Id
                });
                continue;
            }

            if (!SnapshotMatches(existing, cfg))
            {
                // No reescribir horarios de un ciclo ya referenciado por transacciones
                // o archivos. Es preferible una alerta visible a mutar historia financiera.
                bool hasMovements = await _context.AchTransactions.AnyAsync(t => t.AchCycleId == existing.Id)
                    || await _context.AchBatches.AnyAsync(b => b.AchCycleId == existing.Id)
                    || await _context.NachaHeaders.AnyAsync(h => h.AchCycleId == existing.Id);
                if (hasMovements)
                {
                    throw new InvalidOperationException(
                        $"El ciclo '{cfg.CycleName}' de {processingDate:yyyy-MM-dd} pertenece a una versión horaria diferente y ya tiene operaciones asociadas. Requiere conciliación manual antes de actualizar.");
                }

                existing.StartTime = cfg.StartTime;
                existing.EndTime = cfg.EndTime;
                existing.CutoffTime = cfg.CutoffTime;
                existing.OutputReleaseTime = cfg.OutputReleaseTime;
                existing.CompensationStatementStartTime = cfg.CompensationStatementStartTime;
                existing.CompensationStatementEndTime = cfg.CompensationStatementEndTime;
                existing.CompensationPaymentStartTime = cfg.CompensationPaymentStartTime;
                existing.CompensationPaymentEndTime = cfg.CompensationPaymentEndTime;
                existing.OutputReleaseStartTime = cfg.OutputReleaseStartTime;
                existing.OutputReleaseEndTime = cfg.OutputReleaseEndTime;
                existing.ClearingHouseCycleConfigId = cfg.Id;
            }
        }

        if (_context.ChangeTracker.HasChanges())
        {
            await _context.SaveChangesAsync();
        }
    }

    private static bool SnapshotMatches(AchCycle cycle, ClearingHouseCycleConfig config)
        => cycle.ClearingHouseCycleConfigId == config.Id
            && cycle.StartTime == config.StartTime
            && cycle.EndTime == config.EndTime
            && cycle.CutoffTime == config.CutoffTime
            && cycle.OutputReleaseTime == config.OutputReleaseTime
            && cycle.CompensationStatementStartTime == config.CompensationStatementStartTime
            && cycle.CompensationStatementEndTime == config.CompensationStatementEndTime
            && cycle.CompensationPaymentStartTime == config.CompensationPaymentStartTime
            && cycle.CompensationPaymentEndTime == config.CompensationPaymentEndTime
            && cycle.OutputReleaseStartTime == config.OutputReleaseStartTime
            && cycle.OutputReleaseEndTime == config.OutputReleaseEndTime;

    public async Task<List<AchCycle>> GetScheduledCyclesAsync(int clearingHouseId, DateTime date)
    {
        return await _context.AchCycles
            .Where(c => c.ClearingHouseId == clearingHouseId && c.ProcessingDate.Date == date.Date)
            .ToListAsync();
    }

    public DateTime GetNextValidProcessingDate(DateTime baseDate, int clearingHouseId)
        => _calendar.GetNextBusinessDayAsync(DateOnly.FromDateTime(baseDate), clearingHouseId)
            .GetAwaiter().GetResult()
            .ToDateTime(TimeOnly.MinValue);
}
