using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;

public class AchContrapartidasByCycleHandler : ITaskHandler
{
    private readonly AchDbContext _db;
    private readonly IWscfaachSoapClient _soapClient;
    private readonly ILogger<AchContrapartidasByCycleHandler> _log;

    public string Code => "AchContrapartidasByCycle";

    public AchContrapartidasByCycleHandler(
        AchDbContext db,
        IWscfaachSoapClient soapClient,
        ILogger<AchContrapartidasByCycleHandler> log)
    {
        _db = db;
        _soapClient = soapClient;
        _log = log;
    }

    public async Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var maxTransactions = ParsePositiveInt(task, "MaxTransactionsPerCycle", 1000);

        var activeCycles = await _db.AchCycles
            .AsNoTracking()
            .Include(c => c.ClearingHouse)
            .Where(c => IsWithinCycleWindow(now, c.ProcessingDate, c.StartTime, c.EndTime))
            .OrderBy(c => c.ClearingHouseId)
            .ThenBy(c => c.CutoffTime)
            .ToListAsync(cancellationToken);

        if (!activeCycles.Any())
        {
            return $"Sin ciclos activos para ejecutar contrapartidas. FechaHora={now:O}";
        }

        var cycleIds = activeCycles.Select(c => c.Id).ToList();

        var transactions = await _db.AchTransactions
            .AsNoTracking()
            .Where(t => cycleIds.Contains(t.AchCycleId))
            .Where(t => t.State == AchTransferStateEnum.Pending)
            .OrderBy(t => t.Id)
            .ToListAsync(cancellationToken);

        var txByCycle = transactions
            .GroupBy(t => t.AchCycleId)
            .ToDictionary(g => g.Key, g => g.Take(maxTransactions).ToList());

        var sent = 0;
        var skipped = 0;
        var errors = 0;

        foreach (var cycle in activeCycles)
        {
            if (!txByCycle.TryGetValue(cycle.Id, out var cycleTx) || cycleTx.Count == 0)
            {
                skipped++;
                continue;
            }

            try
            {
                var parameters = BuildRequestParameters(cycle, cycleTx, now);
                await _soapClient.ProcContrapartidasAsync(parameters, cancellationToken);
                sent++;
            }
            catch (Exception ex)
            {
                errors++;
                _log.LogError(ex,
                    "Error enviando Proc_Contrapartidas para ciclo {CycleId} ({CycleName}) cámara {ClearingHouseCode}.",
                    cycle.Id,
                    cycle.CycleName,
                    cycle.ClearingHouse?.Code);
            }
        }

        return $"Proc_Contrapartidas ejecutado por ciclo/cámara. CiclosActivos={activeCycles.Count}. Enviados={sent}. SinTransacciones={skipped}. Errores={errors}. FechaHora={now:O}";
    }

    private static IReadOnlyDictionary<string, object?> BuildRequestParameters(
        Domain.Models.ACH.AchCycle cycle,
        IReadOnlyCollection<Domain.Models.ACH.AchTransaction> transactions,
        DateTime executionDateTime)
    {
        var txPayload = transactions.Select(t => new Dictionary<string, object?>
        {
            ["TransactionId"] = t.Id,
            ["AchCycleId"] = t.AchCycleId,
            ["Amount"] = t.Amount,
            ["Type"] = t.Type.ToString(),
            ["TransactionCode"] = t.TransactionCode,
            ["TraceNumber"] = t.TraceNumber,
            ["Reference"] = t.Reference,
            ["OriginatingDFI"] = t.OriginatingDFI,
            ["ReceivingDFI"] = t.ReceivingDFI,
            ["CompanyIdentification"] = t.CompanyIdentification,
            ["EffectiveEntryDate"] = t.EffectiveEntryDate,
            ["DestinationInstitutionId"] = t.DestinationInstitutionId,
            ["SourceInstitutionId"] = t.SourceInstitutionId
        }).ToList();

        return new Dictionary<string, object?>
        {
            ["ClearingHouseId"] = cycle.ClearingHouseId,
            ["ClearingHouseCode"] = cycle.ClearingHouse?.Code,
            ["CycleId"] = cycle.Id,
            ["CycleName"] = cycle.CycleName,
            ["ProcessingDate"] = cycle.ProcessingDate,
            ["StartTime"] = cycle.StartTime,
            ["EndTime"] = cycle.EndTime,
            ["CutoffTime"] = cycle.CutoffTime,
            ["ExecutionDateTime"] = executionDateTime,
            ["Transactions"] = txPayload
        };
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
