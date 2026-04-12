using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Reports;

[Scoped]
public sealed class AchReconciliationReportService : IAchReconciliationReportService
{
    private readonly AchDbContext _context;

    public AchReconciliationReportService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<AchReconciliationReportResponseDto> GetReconciliationAsync(AchReconciliationReportFilter filter, CancellationToken ct = default)
    {
        var baseQuery = _context.AchTransactions
            .AsNoTracking()
            .Include(t => t.AchCycle)
            .AsQueryable();

        if (filter.Date.HasValue)
        {
            var targetDate = filter.Date.Value.Date;
            baseQuery = baseQuery.Where(t => t.EffectiveEntryDate.Date == targetDate);
        }

        if (filter.ClearingHouseId.HasValue)
        {
            baseQuery = baseQuery.Where(t => t.AchCycle.ClearingHouseId == filter.ClearingHouseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.AchCycleId))
        {
            var achCycleId = filter.AchCycleId.Trim();
            baseQuery = baseQuery.Where(t => t.AchCycleId == achCycleId);
        }

        var sentQuery = baseQuery.Where(t => t.Type != TransactionTypeEnum.Return);
        var receivedQuery = baseQuery.Where(t => t.State == AchTransferStateEnum.Certified || t.State == AchTransferStateEnum.AppliedTacitly);
        var returnedQuery = baseQuery.Where(t => t.State == AchTransferStateEnum.ReturnedByOperator || t.State == AchTransferStateEnum.ReturnedByEpr);

        var sentCount = await sentQuery.CountAsync(ct);
        var sentAmount = await sentQuery.Select(t => (decimal?)t.Amount).SumAsync(ct) ?? 0m;

        var receivedCount = await receivedQuery.CountAsync(ct);
        var receivedAmount = await receivedQuery.Select(t => (decimal?)t.Amount).SumAsync(ct) ?? 0m;

        var returnedCount = await returnedQuery.CountAsync(ct);
        var returnedAmount = await returnedQuery.Select(t => (decimal?)t.Amount).SumAsync(ct) ?? 0m;

        var returnedWithoutCausalCount = await returnedQuery.CountAsync(t => string.IsNullOrWhiteSpace(t.ReturnReasonCode), ct);
        var nonReturnedWithCausalCount = await baseQuery.CountAsync(
            t => (t.State != AchTransferStateEnum.ReturnedByOperator && t.State != AchTransferStateEnum.ReturnedByEpr)
                && !string.IsNullOrWhiteSpace(t.ReturnReasonCode),
            ct);
        var negativeAmountCount = await baseQuery.CountAsync(t => t.Amount < 0, ct);

        var inconsistencies = new List<AchReconciliationInconsistencyDto>();

        if (returnedWithoutCausalCount > 0)
        {
            inconsistencies.Add(new AchReconciliationInconsistencyDto
            {
                Code = "INC-RET-NO-CAUSAL",
                Description = "Transacciones devueltas sin causal registrada.",
                AffectedCount = returnedWithoutCausalCount
            });
        }

        if (nonReturnedWithCausalCount > 0)
        {
            inconsistencies.Add(new AchReconciliationInconsistencyDto
            {
                Code = "INC-CAUSAL-STATE",
                Description = "Transacciones con causal de devolución pero estado no devuelto.",
                AffectedCount = nonReturnedWithCausalCount
            });
        }

        if (negativeAmountCount > 0)
        {
            inconsistencies.Add(new AchReconciliationInconsistencyDto
            {
                Code = "INC-NEG-AMOUNT",
                Description = "Transacciones con monto negativo.",
                AffectedCount = negativeAmountCount
            });
        }

        return new AchReconciliationReportResponseDto
        {
            Totals = new AchReconciliationTotalsDto
            {
                SentCount = sentCount,
                SentAmount = sentAmount,
                ReceivedCount = receivedCount,
                ReceivedAmount = receivedAmount,
                ReturnedCount = returnedCount,
                ReturnedAmount = returnedAmount
            },
            Differences = new AchReconciliationDifferencesDto
            {
                SentVsReceivedCountDiff = sentCount - receivedCount,
                SentVsReceivedAmountDiff = sentAmount - receivedAmount,
                SentVsReturnedCountDiff = sentCount - returnedCount,
                SentVsReturnedAmountDiff = sentAmount - returnedAmount,
                ReceivedVsReturnedCountDiff = receivedCount - returnedCount,
                ReceivedVsReturnedAmountDiff = receivedAmount - returnedAmount
            },
            Inconsistencies = inconsistencies
        };
    }
}
