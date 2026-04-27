using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;

public class AchContrapartidasByCycleHandler : ITaskHandler
{
    private readonly AchDbContext _db;
    private readonly IContrapartidaDispatchJobService _dispatchJobService;
    private readonly ILogger<AchContrapartidasByCycleHandler> _log;

    public string Code => "AchContrapartidasByCycle";

    public AchContrapartidasByCycleHandler(
        AchDbContext db,
        IContrapartidaDispatchJobService dispatchJobService,
        ILogger<AchContrapartidasByCycleHandler> log)
    {
        _db = db;
        _dispatchJobService = dispatchJobService;
        _log = log;
    }

    public async Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var chunkSize = ParsePositiveInt(task, "ChunkSize", 300);
        var maxCyclesPerRun = ParsePositiveInt(task, "MaxCyclesPerRun", 20);

        var candidateCycles = await _db.AchCycles
            .AsNoTracking()
            .Include(c => c.ClearingHouse)
            .Include(c => c.ClearingHouseCycleConfig)
            .ToListAsync(cancellationToken);

        var activeCycles = candidateCycles
            .Where(c => IsWithinCycleWindow(now, c.ProcessingDate, c.StartTime, c.EndTime))
            .OrderBy(c => c.ClearingHouseId)
            .ThenBy(c => c.CutoffTime)
            .Take(maxCyclesPerRun)
            .ToList();

        if (!activeCycles.Any())
        {
            return $"Sin ciclos activos para ejecutar contrapartidas. FechaHora={now:O}";
        }

        var summaries = new List<string>(activeCycles.Count);
        var totalProcessed = 0;
        var totalSucceeded = 0;
        var totalFailed = 0;
        var totalPartial = 0;

        foreach (var cycle in activeCycles)
        {
            try
            {
                var result = await _dispatchJobService.ProcessCycleAsync(
                    cycle.Id,
                    cycle.ClearingHouseId,
                    triggeredBy: $"task:{Code}",
                    chunkSize,
                    cancellationToken);

                totalProcessed += result.Processed;
                totalSucceeded += result.Succeeded;
                totalFailed += result.Failed;
                totalPartial += result.Partial;
                summaries.Add(result.Summary);
            }
            catch (Exception ex)
            {
                _log.LogError(ex,
                    "Error ejecutando contrapartidas para ciclo {CycleId} cámara {ClearingHouseId}",
                    cycle.Id,
                    cycle.ClearingHouseId);
                summaries.Add($"Ciclo {cycle.Id} cámara {cycle.ClearingHouseId}: ERROR={ex.Message}");
            }
        }

        return $"Proc_Contrapartidas ejecutado. Ciclos={activeCycles.Count}, Processed={totalProcessed}, Success={totalSucceeded}, Failed={totalFailed}, Partial={totalPartial}. Detalle=[{string.Join(" | ", summaries)}]";
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

    private static int ParsePositiveInt(TaskDefinition task, string key, int defaultValue)
    {
        var raw = task.Parameters.FirstOrDefault(p => p.Key == key)?.Value;
        return int.TryParse(raw, out var parsed) && parsed > 0 ? parsed : defaultValue;
    }
}
