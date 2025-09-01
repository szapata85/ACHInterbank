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
        var rawCodes = task.Parameters.FirstOrDefault(p => p.Key == "ClearingHouseCodes")?.Value;
        var filterCodes = string.IsNullOrWhiteSpace(rawCodes)
            ? Array.Empty<string>()
            : rawCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        IQueryable<ClearingHouse> q = _db.ClearingHouses.AsNoTracking();
        if (filterCodes.Length > 0)
            q = q.Where(ch => filterCodes.Contains(ch.Code));

        var ids = await q.Select(ch => ch.Id).ToListAsync(ct);
        if (ids.Count == 0) return "No se encontraron cámaras de compensación en BD (o el filtro no tuvo coincidencias).";

        int ok = 0, fail = 0;
        var gate = new object();

        await Parallel.ForEachAsync(ids, ct, async (id, token) =>
        {
            try
            {
                using var scope = _sp.CreateScope();
                var scheduler = scope.ServiceProvider.GetRequiredService<IAchCycleScheduler>();
                await scheduler.ScheduleCyclesForClearingHouseAsync(id);
                lock (gate) ok++;
            }
            catch (Exception ex)
            {
                lock (gate) fail++;
                _log.LogError(ex, "Falló programación para ClearingHouseId={Id}", id);
            }
        });

        return $"Scheduler paralelo ejecutado. Cámaras: {ids.Count}. Éxitos: {ok}. Fallos: {fail}.";
    }
}

