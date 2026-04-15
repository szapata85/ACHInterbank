using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;

public class AchCycleSchedulerHandler : ITaskHandler
{
    private readonly AchDbContext _db;
    private readonly IServiceProvider _sp;
    private readonly ILogger<AchCycleSchedulerHandler> _log;

    public string Code => "AchCycleScheduler";

    public AchCycleSchedulerHandler(
        AchDbContext db,
        IServiceProvider sp,
        ILogger<AchCycleSchedulerHandler> log)
    {
        _db = db;
        _sp = sp;
        _log = log;
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

        var clearingHouseIds = await query.Select(ch => ch.Id).ToListAsync(ct);
        if (clearingHouseIds.Count == 0)
            return "No se encontraron cámaras de compensación en BD (o el filtro no tuvo coincidencias).";

        // ✅ Fecha de procesamiento (día hábil)
        var txService = _sp.GetRequiredService<IAchTransactionService>();
        var processingDate = await txService.GetNextBusinessDayAsync(DateTime.Now, ct);

        int ok = 0, fail = 0;

        await Parallel.ForEachAsync(clearingHouseIds, ct, async (id, token) =>
        {
            try
            {
                using var scope = _sp.CreateScope();
                var scheduler = scope.ServiceProvider.GetRequiredService<IAchCycleScheduler>();

                // ⚠️ Delega la validación al scheduler interno
                await scheduler.ScheduleCyclesForClearingHouseAsync(id, processingDate);

                var db = scope.ServiceProvider.GetRequiredService<AchDbContext>();
                var executionService = scope.ServiceProvider.GetRequiredService<ICenitCycleExecutionService>();
                var cenitCyclesToRun = await db.AchCycles
                    .Include(x => x.ClearingHouse)
                    .Where(x => x.ClearingHouseId == id
                                && x.ProcessingDate.Date == processingDate.Date
                                && x.ClearingHouse != null
                                && x.ClearingHouse.Code == "CENIT"
                                && x.EndTime <= DateTime.UtcNow.TimeOfDay)
                    .ToListAsync(token);

                foreach (var cycle in cenitCyclesToRun)
                {
                    await executionService.StartExecutionAsync(cycle, token);
                }
                Interlocked.Increment(ref ok);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref fail);
                _log.LogError(ex, "Falló programación para ClearingHouseId={Id}", id);
            }
        });


        return $"Scheduler paralelo ejecutado. Cámaras: {clearingHouseIds.Count}. Éxitos: {ok}. Fallos: {fail}.";
    }
}
