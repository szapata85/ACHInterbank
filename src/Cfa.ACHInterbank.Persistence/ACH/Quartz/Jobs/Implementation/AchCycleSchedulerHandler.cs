using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;

public class AchCycleSchedulerHandler : ITaskHandler
{
    private readonly AchDbContext _db;
    private readonly IServiceProvider _sp;
    private readonly ILogger<AchCycleSchedulerHandler> _log;
    private readonly TimeProvider _timeProvider;
    private readonly IOperationalCycleWindowResolver _windowResolver;

    public string Code => "AchCycleScheduler";

    public AchCycleSchedulerHandler(
        AchDbContext db,
        IServiceProvider sp,
        ILogger<AchCycleSchedulerHandler> log,
        TimeProvider? timeProvider = null,
        IOperationalCycleWindowResolver? windowResolver = null)
    {
        _db = db;
        _sp = sp;
        _log = log;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _windowResolver = windowResolver ?? new OperationalCycleWindowResolver();
    }

    public async Task<string> ExecuteAsync(TaskDefinition task, CancellationToken ct)
    {
        // 🔎 Filtro opcional por códigos de cámara
        var rawCodes = task.Parameters.FirstOrDefault(p => p.Key == "ClearingHouseCodes")?.Value;
        var filterCodes = string.IsNullOrWhiteSpace(rawCodes)
            ? Array.Empty<string>()
            : rawCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        IQueryable<ClearingHouse> query = _db.ClearingHouses.AsNoTracking();
        if (filterCodes.Length > 0)
            query = query.Where(ch => filterCodes.Contains(ch.Code));

        var clearingHouses = await query
            .Select(ch => new { ch.Id, ch.Code, TimeZoneId = ch.ClearingHouseConfig.TimeZoneId })
            .ToListAsync(ct);
        if (clearingHouses.Count == 0)
            return "No se encontraron cámaras de compensación en BD (o el filtro no tuvo coincidencias).";

        int ok = 0, fail = 0;
        var nowInstant = _timeProvider.GetUtcNow();

        await Parallel.ForEachAsync(clearingHouses, ct, async (house, token) =>
        {
            try
            {
                using var scope = _sp.CreateScope();
                var scheduler = scope.ServiceProvider.GetRequiredService<IAchCycleScheduler>();
                var calendar = scope.ServiceProvider.GetRequiredService<IOperationalCalendarService>();
                var localNow = _windowResolver.Resolve(
                    nowInstant.UtcDateTime.Date,
                    TimeSpan.Zero,
                    new TimeSpan(23, 59, 59),
                    house.TimeZoneId,
                    nowInstant).LocalNow;
                var processingDateOnly = await calendar.GetNextBusinessDayAsync(
                    DateOnly.FromDateTime(localNow),
                    house.Id,
                    token);
                var processingDate = processingDateOnly.ToDateTime(TimeOnly.MinValue);

                // ⚠️ Delega la validación al scheduler interno
                await scheduler.ScheduleCyclesForClearingHouseAsync(house.Id, processingDate);

                var db = scope.ServiceProvider.GetRequiredService<AchDbContext>();
                var executionService = scope.ServiceProvider.GetRequiredService<ICenitCycleExecutionService>();
                var cenitCyclesToRun = await db.AchCycles
                    .Include(x => x.ClearingHouse)
                        .ThenInclude(clearingHouse => clearingHouse!.ClearingHouseConfig)
                    .Where(x => x.ClearingHouseId == house.Id
                                && x.ProcessingDate.Date == processingDate.Date
                                && x.ClearingHouse != null
                                && x.ClearingHouse.Code == "CENIT")
                    .ToListAsync(token);

                foreach (var cycle in cenitCyclesToRun.Where(cycle => _windowResolver.Resolve(
                    cycle.ProcessingDate,
                    cycle.StartTime,
                    cycle.EndTime,
                    ClearingHouseOperationalTimeZone.Resolve(cycle),
                    nowInstant).Status == OperationalCycleWindowStatus.After))
                {
                    try
                    {
                        await executionService.StartExecutionAsync(cycle, token);
                    }
                    catch (CycleDeferredByCalendarException deferred)
                    {
                        _log.LogInformation(
                            "Ciclo {CycleId} diferido por calendario hasta {RescheduledDate}; no se inició ejecución CENIT.",
                            cycle.Id,
                            deferred.Result.RescheduledDate);
                    }
                }
                Interlocked.Increment(ref ok);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref fail);
                _log.LogError(ex, "Falló programación para ClearingHouseId={Id}", house.Id);
            }
        });


        return $"Scheduler paralelo ejecutado. Cámaras: {clearingHouses.Count}. Éxitos: {ok}. Fallos: {fail}.";
    }
}
