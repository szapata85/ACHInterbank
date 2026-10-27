using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// I01: evaluación conservadora, sin mutaciones y limitada al mismo día/fecha valor.
/// Reutiliza calendario, ventana y política regulatoria existentes.
/// </summary>
[Scoped]
public sealed class AchPendingCycleEligibilityService : IAchPendingCycleEligibilityService
{
    private readonly AchDbContext _context;
    private readonly IOperationalCalendarService _calendar;
    private readonly IOperationalCycleWindowResolver _window;
    private readonly ICycleTransactionPolicy _policy;
    private readonly TimeProvider _clock;

    public AchPendingCycleEligibilityService(
        AchDbContext context,
        IOperationalCalendarService calendar,
        IOperationalCycleWindowResolver window,
        ICycleTransactionPolicy policy,
        TimeProvider clock)
    {
        _context = context;
        _calendar = calendar;
        _window = window;
        _policy = policy;
        _clock = clock;
    }

    public async Task<AchPendingCycleEligibilityResult> EvaluateAsync(int transactionId, CancellationToken ct = default)
    {
        AchPendingCycleEligibilityResult Deny(string code, AchCycle? cycle = null) =>
            new(transactionId, false, code, cycle?.Id, null, cycle?.ClearingHouseId, cycle?.ProcessingDate.Date);

        if (transactionId <= 0)
        {
            return Deny("INVALID_TRANSACTION_ID");
        }

        var transaction = await _context.AchTransactions.AsNoTracking()
            .Include(item => item.AchCycle)
                .ThenInclude(cycle => cycle.ClearingHouse)
                    .ThenInclude(house => house!.ClearingHouseConfig)
            .SingleOrDefaultAsync(item => item.Id == transactionId, ct);
        if (transaction is null)
        {
            return Deny("TRANSACTION_NOT_FOUND");
        }

        var original = transaction.AchCycle;
        if (original is null || original.ClearingHouse is null
            || original.ClearingHouseId <= 0 || !original.ClearingHouse.IsActive)
        {
            return Deny("SOURCE_CYCLE_UNRESOLVED", original);
        }

        if (transaction.State != AchTransferStateEnum.Pending)
        {
            return Deny("NOT_PENDING", original);
        }

        if (transaction.Direction != AchTransactionDirection.Outgoing
            || transaction.Origin != AchTransactionOrigin.Cfa
            || transaction.ClassificationStatus != AchTransactionClassificationStatus.Determined
            || transaction.Type is TransactionTypeEnum.Return or TransactionTypeEnum.Reversal
            || !string.IsNullOrWhiteSpace(transaction.ReturnReasonCode)
            || !string.IsNullOrWhiteSpace(transaction.OriginalTraceRef))
        {
            return Deny("OUT_OF_SCOPE", original);
        }

        if (original.OperationalStatus is AchCycleOperationalStatus.Cancelled
            || !await _context.AchBatches.AsNoTracking().AnyAsync(batch =>
                batch.Id == transaction.AchBatchId
                && batch.AchCycleId == original.Id
                && batch.EffectiveEntryDate.Date == transaction.EffectiveEntryDate.Date, ct))
        {
            return Deny("SOURCE_ASSIGNMENT_INCONSISTENT", original);
        }

        // I04: nunca modificar ni inferir una nueva fecha valor en esta etapa.
        if (transaction.EffectiveEntryDate.Date != original.ProcessingDate.Date)
        {
            return Deny("VALUE_DATE_MISMATCH", original);
        }

        var evaluatedAtUtc = _clock.GetUtcNow();
        string timeZoneId;
        OperationalCycleWindow originalWindow;
        try
        {
            timeZoneId = ClearingHouseOperationalTimeZone.Resolve(original);
            originalWindow = _window.Resolve(
                original.ProcessingDate, original.StartTime, original.CutoffTime,
                timeZoneId, evaluatedAtUtc);
        }
        catch (Exception exception) when (exception is InvalidOperationException
            or TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return Deny("SOURCE_WINDOW_UNRESOLVED", original);
        }

        if (originalWindow.Status != OperationalCycleWindowStatus.After)
        {
            return Deny("CYCLE_NOT_EXPIRED", original);
        }

        if (!await _calendar.IsBusinessDayAsync(
                DateOnly.FromDateTime(original.ProcessingDate), original.ClearingHouseId, ct))
        {
            return Deny("NOT_BUSINESS_DAY", original);
        }

        // Cualquier evidencia incierta tiene precedencia frente a una decisión positiva.
        if (await _context.AchFileExportTransactions.AsNoTracking()
            .AnyAsync(item => item.AchTransactionId == transactionId, ct))
        {
            return Deny("EXPORT_EVIDENCE_PRESENT", original);
        }

        if (await _context.ContrapartidaDispatchItems.AsNoTracking()
                .AnyAsync(item => item.AchTransactionId == transactionId, ct)
            || await _context.CenitCycleQueues.AsNoTracking()
                .AnyAsync(item => item.AchTransactionId == transactionId, ct)
            || await _context.LiquidityOptimizationDecisions.AsNoTracking()
                .AnyAsync(item => item.AchTransactionId == transactionId, ct)
            || await _context.CenitNettingDetails.AsNoTracking()
                .AnyAsync(item => item.AchTransactionId == transactionId, ct))
        {
            return Deny("PROCESSING_EVIDENCE_PRESENT", original);
        }

        // I05: una salida ya generada para el ciclo, incluso con membresía completa de
        // otras transacciones, es evidencia operacional que no se debe reinterpretar.
        // El traslado desde ese ciclo se suspende para evitar nuevas presentaciones ambiguas.
        if (await _context.AchFileExports.AsNoTracking()
            .AnyAsync(file => file.AchCycleId == original.Id
                && (file.ExportKind == "NACHA" || file.ExportKind == "PUBLISH"), ct))
        {
            return Deny("SOURCE_CYCLE_EXPORT_EXISTS", original);
        }

        // CENIT ejecuta compensación, cola y optimización propios. Si comenzó cualquier
        // actividad del ciclo fuente, dejar la decisión al flujo regulado existente.
        if (string.Equals(original.ClearingHouse.Code, "CENIT", StringComparison.OrdinalIgnoreCase)
            && (await _context.CenitCycleExecutions.AsNoTracking()
                    .AnyAsync(execution => execution.AchCycleId == original.Id, ct)
                || await _context.CenitCycleQueues.AsNoTracking()
                    .AnyAsync(queue => queue.OriginalAchCycleId == original.Id
                        || queue.TargetAchCycleId == original.Id, ct)
                || await _context.LiquidityOptimizationDecisions.AsNoTracking()
                    .AnyAsync(decision => decision.FromCycleId == original.Id
                        || decision.ToCycleId == original.Id, ct)))
        {
            return Deny("CENIT_CYCLE_PROCESSING_EVIDENCE", original);
        }

        // I01 evalúa solo ciclos posteriores de la misma fecha valor. I04 atenderá otra jornada.
        var candidates = await _context.AchCycles.AsNoTracking()
            .Where(cycle => cycle.ClearingHouseId == original.ClearingHouseId
                && cycle.ProcessingDate.Date == original.ProcessingDate.Date
                && cycle.CutoffTime > original.CutoffTime
                && cycle.OperationalStatus != AchCycleOperationalStatus.Closed
                && cycle.OperationalStatus != AchCycleOperationalStatus.Cancelled)
            .OrderBy(cycle => cycle.CutoffTime)
            .ThenBy(cycle => cycle.Id)
            .ToListAsync(ct);

        foreach (var next in candidates)
        {
            if (next.Id == original.Id || next.ClearingHouseId != original.ClearingHouseId)
            {
                continue;
            }

            OperationalCycleWindow nextWindow;
            try
            {
                nextWindow = _window.Resolve(next.ProcessingDate, next.StartTime,
                    next.CutoffTime, timeZoneId, evaluatedAtUtc);
            }
            catch (Exception exception) when (exception is InvalidOperationException
                or TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                continue;
            }

            if (nextWindow.Status == OperationalCycleWindowStatus.After)
            {
                continue;
            }

            var decision = await _policy.EvaluateAsync(new CycleTransactionPolicyRequest(
                next.ClearingHouseId,
                next.ClearingHouseCycleConfigId,
                next.ProcessingDate,
                original.ClearingHouse.Code,
                original.ClearingHouse.ClearingHouseConfig?.PaymentRailCode,
                next.CycleName,
                transaction.Type,
                transaction.IsPrenotification,
                TransactionCode: transaction.TransactionCode), ct);
            if (!decision.IsAllowed)
            {
                continue;
            }

            return new AchPendingCycleEligibilityResult(transactionId, true, "ELIGIBLE",
                original.Id, next.Id, original.ClearingHouseId, original.ProcessingDate.Date);
        }

        return Deny("NO_ELIGIBLE_TARGET_SAME_VALUE_DATE", original);
    }
}
