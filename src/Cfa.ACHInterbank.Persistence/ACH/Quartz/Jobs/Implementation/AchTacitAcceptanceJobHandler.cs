using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;

[DisallowConcurrentExecution]
public class AchTacitAcceptanceJobHandler : ITaskHandler
{
    private readonly AchDbContext _db;
    private readonly IAchStateTransitionService _stateTransitionService;
    private readonly ILogger<AchTacitAcceptanceJobHandler> _log;

    public string Code => "AchTacitAcceptanceJob";

    public AchTacitAcceptanceJobHandler(
        AchDbContext db,
        IAchStateTransitionService stateTransitionService,
        ILogger<AchTacitAcceptanceJobHandler> log)
    {
        _db = db;
        _stateTransitionService = stateTransitionService;
        _log = log;
    }

    public async Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var batchSize = ParsePositiveInt(task, "BatchSize", 500);

        var candidates = await _db.AchTransactions
            .AsNoTracking()
            .Where(t => t.IsPrenotification && t.State == AchTransferStateEnum.Pending)
            .Where(t =>
                (t.SlaDeadlineAtUtc.HasValue && t.SlaDeadlineAtUtc.Value <= utcNow)
                || (!t.SlaDeadlineAtUtc.HasValue && t.EffectiveEntryDate.Date < utcNow.Date))
            .OrderBy(t => t.StateChangedAtUtc)
            .Select(t => t.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var applied = 0;
        var skipped = 0;

        foreach (var transactionId in candidates)
        {
            try
            {
                await _stateTransitionService.TransitionAsync(
                    transactionId,
                    AchTransferStateEnum.AppliedTacitly,
                    AchStateEventSourceEnum.System,
                    reasonCode: null,
                    payloadJson: "{\"origin\":\"quartz-ach-tacit-acceptance\"}",
                    originalTraceRef: null,
                    changedAtUtc: utcNow,
                    ct: cancellationToken);

                applied++;
            }
            catch (InvalidOperationException ex)
            {
                skipped++;
                _log.LogWarning(ex,
                    "Transacción {TransactionId} omitida por transición no válida hacia AppliedTacitly.",
                    transactionId);
            }
        }

        return $"Tacit acceptance ejecutado. Candidatas: {candidates.Count}. Aplicadas: {applied}. Omitidas: {skipped}. UtcNow: {utcNow:O}.";
    }

    private static int ParsePositiveInt(TaskDefinition task, string key, int defaultValue)
    {
        var raw = task.Parameters.FirstOrDefault(p => p.Key == key)?.Value;
        return int.TryParse(raw, out var value) && value > 0 ? value : defaultValue;
    }
}
