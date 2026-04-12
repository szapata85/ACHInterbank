using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Reports;

[Scoped]
public sealed class AchReturnRejectionReportService : IAchReturnRejectionReportService
{
    private readonly AchDbContext _context;

    public AchReturnRejectionReportService(AchDbContext context)
    {
        _context = context;
    }

    public Task<AchReturnRejectionReportResponseDto> GetReturnsAsync(AchReturnRejectionReportFilter filter, CancellationToken ct = default)
        => QueryAsync(filter, isReturnReport: true, ct);

    public Task<AchReturnRejectionReportResponseDto> GetRejectionsAsync(AchReturnRejectionReportFilter filter, CancellationToken ct = default)
        => QueryAsync(filter, isReturnReport: false, ct);

    private async Task<AchReturnRejectionReportResponseDto> QueryAsync(
        AchReturnRejectionReportFilter filter,
        bool isReturnReport,
        CancellationToken ct)
    {
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var pageSize = filter.PageSize <= 0 ? 25 : Math.Min(filter.PageSize, 200);

        var query = _context.AchTransactions
            .AsNoTracking()
            .Include(t => t.AchCycle)
            .ThenInclude(c => c.ClearingHouse)
            .AsQueryable();

        query = isReturnReport
            ? query.Where(t => !string.IsNullOrWhiteSpace(t.ReturnReasonCode) &&
                               (EF.Functions.Like(t.ReturnReasonCode, "R__") || EF.Functions.Like(t.ReturnReasonCode, "DEV__")))
            : query.Where(t => !string.IsNullOrWhiteSpace(t.ReturnReasonCode) && EF.Functions.Like(t.ReturnReasonCode, "D__"));

        if (filter.Date.HasValue)
        {
            var targetDate = filter.Date.Value.Date;
            query = query.Where(t => t.StateChangedAtUtc.Date == targetDate);
        }

        if (!string.IsNullOrWhiteSpace(filter.Causal))
        {
            var causal = filter.Causal.Trim().ToUpperInvariant();
            query = query.Where(t => t.ReturnReasonCode == causal);
        }

        if (filter.ClearingHouseId.HasValue)
        {
            query = query.Where(t => t.AchCycle.ClearingHouseId == filter.ClearingHouseId.Value);
        }

        if (filter.State.HasValue)
        {
            query = query.Where(t => t.State == filter.State.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Reference))
        {
            var reference = filter.Reference.Trim();
            query = query.Where(t => t.Reference.Contains(reference));
        }

        var total = await query.CountAsync(ct);
        var totalAmount = await query.Select(t => (decimal?)t.Amount).SumAsync(ct) ?? 0m;

        var items = await query
            .OrderByDescending(t => t.StateChangedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new AchReturnRejectionReportRowDto
            {
                TransactionId = t.Id,
                EffectiveEntryDate = t.EffectiveEntryDate,
                Reference = t.Reference,
                Amount = t.Amount,
                State = t.State,
                CausalCode = t.ReturnReasonCode,
                CausalDescription = _context.ReturnReasons
                    .Where(r => r.Code == t.ReturnReasonCode)
                    .Select(r => r.Description)
                    .FirstOrDefault() ?? string.Empty,
                ClearingHouseName = t.AchCycle.ClearingHouse!.Name,
                AchCycleId = t.AchCycleId,
                AchCycleName = t.AchCycle.CycleName,
                OriginalTraceRef = t.OriginalTraceRef,
                OriginalTransactionId = _context.AchTransactions
                    .Where(o => o.TraceNumber == t.OriginalTraceRef)
                    .Select(o => (int?)o.Id)
                    .FirstOrDefault(),
                OriginalTransactionReference = _context.AchTransactions
                    .Where(o => o.TraceNumber == t.OriginalTraceRef)
                    .Select(o => o.Reference)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        return new AchReturnRejectionReportResponseDto
        {
            Items = items,
            Totals = new AchReturnRejectionReportTotalsDto
            {
                TotalRecords = total,
                TotalAmount = totalAmount
            },
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
