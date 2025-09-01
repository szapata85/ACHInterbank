using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;

[DisallowConcurrentExecution]
public class AchCycleSeederHandler : ITaskHandler
{
    private readonly AchDbContext _db;
    private readonly IServiceProvider _sp;
    private readonly ILogger<AchCycleSeederHandler> _log;

    public string Code => "AchCycleSeeder";

    public AchCycleSeederHandler(
        AchDbContext db,
        IServiceProvider sp,
        ILogger<AchCycleSeederHandler> log)
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

        var houses = await q.Select(ch => new { ch.Id, ch.Code, ch.Name }).ToListAsync(ct);
        if (houses.Count == 0) return "No hay ClearingHouses en BD (o el filtro no tuvo coincidencias).";

        var years = ParseYears(task) ?? BuildDefaultYears();

        var combos = from ch in houses
                     from year in years
                     select new { ch.Id, ch.Code, ch.Name, Year = year };

        int ok = 0, fail = 0;
        var gate = new object();

        await Parallel.ForEachAsync(combos, ct, async (item, token) =>
        {
            try
            {
                using var scope = _sp.CreateScope();
                var seeder = scope.ServiceProvider.GetRequiredService<IAchCycleSeeder>();
                await seeder.SeedCyclesIfNotExistsAsync(item.Id, item.Year);

                lock (gate) ok++;
            }
            catch (Exception ex)
            {
                lock (gate) fail++;
                _log.LogError(ex, "Seed falló para CH {Code}/{Name} (Id={Id}) año {Year}",
                    item.Code, item.Name, item.Id, item.Year);
            }
        });

        return $"Seeder paralelo ejecutado. CH: {houses.Count}. Años: {string.Join(",", years)}. Éxitos: {ok}. Fallos: {fail}.";
    }

    private static List<int> BuildDefaultYears()
    {
        var y = DateTime.Now.Year;
        return new List<int> { y, y + 1 };
    }

    private static List<int>? ParseYears(TaskDefinition task)
    {
        var yearsParam = task.Parameters.FirstOrDefault(p => p.Key == "Years")?.Value;
        if (!string.IsNullOrWhiteSpace(yearsParam))
        {
            var parsed = new List<int>();
            foreach (var s in yearsParam.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                if (int.TryParse(s, out var yy)) parsed.Add(yy);
            if (parsed.Count > 0) return parsed;
        }

        var nextParam = task.Parameters.FirstOrDefault(p => p.Key == "SeedNextYears")?.Value;
        if (!string.IsNullOrWhiteSpace(nextParam) && int.TryParse(nextParam, out var n) && n >= 0)
        {
            var baseYear = DateTime.Now.Year;
            return Enumerable.Range(baseYear, n + 1).ToList();
        }

        return null;
    }
}
