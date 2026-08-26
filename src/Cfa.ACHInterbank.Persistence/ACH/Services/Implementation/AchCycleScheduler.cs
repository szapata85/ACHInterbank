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

        // ✅ Validar que no existan ciclos para esa cámara en la fecha
        bool exists = await _context.AchCycles
            .AnyAsync(c => c.ClearingHouseId == clearingHouseId &&
                           c.ProcessingDate.Date == nextBusinessDate.ToDateTime(TimeOnly.MinValue).Date);
        if (exists)
        {
            // Si ya hay ciclos, no continuar
            return;
        }

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
                    OutputReleaseTime = cfg.OutputReleaseTime,
                    RescheduleOnHoliday = true,
                    ClearingHouseCycleConfigId = cfg.Id
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

    public DateTime GetNextValidProcessingDate(DateTime baseDate, int clearingHouseId)
        => _calendar.GetNextBusinessDayAsync(DateOnly.FromDateTime(baseDate), clearingHouseId)
            .GetAwaiter().GetResult()
            .ToDateTime(TimeOnly.MinValue);
}
