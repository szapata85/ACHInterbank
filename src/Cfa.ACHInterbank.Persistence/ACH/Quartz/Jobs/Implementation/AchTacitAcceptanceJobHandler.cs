using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
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
    private readonly IClearingHouseValidationPolicyService _validationPolicyService;
    private readonly ILogger<AchTacitAcceptanceJobHandler> _log;

    public string Code => "AchTacitAcceptanceJob";

    public AchTacitAcceptanceJobHandler(
        AchDbContext db,
        IAchStateTransitionService stateTransitionService,
        IClearingHouseValidationPolicyService validationPolicyService,
        ILogger<AchTacitAcceptanceJobHandler> log)
    {
        _db = db;
        _stateTransitionService = stateTransitionService;
        _validationPolicyService = validationPolicyService;
        _log = log;
    }

    public async Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var batchSize = ParsePositiveInt(task, "BatchSize", 500);

        // Canonical TPA3 attempts carry their own response deadline. This deadline is deliberately
        // distinct from AchTransaction.SlaDeadlineAtUtc, which remains the return SLA.
        var canonicalDue = await _db.PrenotificationValidationAttempts
            .AsNoTracking()
            .Where(x => x.AchTransactionId.HasValue
                        && x.AchTransaction!.IsPrenotification
                        && x.AchTransaction.State == AchTransferStateEnum.Pending
                        && x.Status == PrenotificationValidationAttemptStatus.AwaitingResponse
                        && x.ResponseDeadlineAtUtc.HasValue
                        && x.ResponseDeadlineAtUtc.Value <= utcNow
                        && x.ThirdPartyAccountValidation.PolicyRule != null
                        && x.ThirdPartyAccountValidation.PolicyRule.TacitAcceptanceEnabled)
            .OrderBy(x => x.ResponseDeadlineAtUtc)
            .Select(x => x.AchTransactionId!.Value)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var candidates = new HashSet<int>(canonicalDue);
        if (candidates.Count < batchSize)
        {
            // Compatibility path for prenotifications created before canonical validation existed.
            // It resolves the same effective chamber policy and never falls back to return SLA or UTC-date heuristics.
            var legacyCandidates = await _db.AchTransactions
                .AsNoTracking()
                .Include(x => x.AchCycle)
                .Where(t => t.IsPrenotification
                            && t.State == AchTransferStateEnum.Pending
                            && !_db.PrenotificationValidationAttempts.Any(a => a.AchTransactionId == t.Id))
                .OrderBy(t => t.StateChangedAtUtc)
                .Take(batchSize * 4)
                .ToListAsync(cancellationToken);

            foreach (var transaction in legacyCandidates)
            {
                if (candidates.Count >= batchSize)
                {
                    break;
                }

                var prenotificationType = NachaTransactionCodeTaxonomy.ResolvePrenotificationDirection(transaction.TransactionCode);
                var nature = prenotificationType.HasValue
                    ? TransactionPrerequisitePolicyService.ResolveNature(prenotificationType.Value)
                    : null;
                if (!nature.HasValue || transaction.AchCycle is null)
                {
                    continue;
                }

                var policy = await _validationPolicyService.ResolveAsync(
                    transaction.AchCycle.ClearingHouseId,
                    nature.Value,
                    transaction.EffectiveEntryDate,
                    cancellationToken);
                if (policy.Rule is null || !policy.Rule.TacitAcceptanceEnabled)
                {
                    continue;
                }

                var deadline = await _validationPolicyService.ResolvePrenotificationResponseDeadlineAsync(
                    policy.Rule,
                    transaction.AchCycleId,
                    cancellationToken);
                if (deadline.HasValue && deadline.Value <= utcNow)
                {
                    candidates.Add(transaction.Id);
                }
            }
        }

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
                    payloadJson: "{\"origin\":\"quartz-ach-tacit-acceptance\",\"deadlineAuthority\":\"clearing-house-policy\"}",
                    originalTraceRef: null,
                    changedAtUtc: utcNow.UtcDateTime,
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
