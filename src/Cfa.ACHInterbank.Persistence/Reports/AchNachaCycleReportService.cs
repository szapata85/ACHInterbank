using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Models;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Reports;

[Scoped]
public sealed class AchNachaCycleReportService : IAchNachaCycleReportService
{
    private readonly AchDbContext _context;

    public AchNachaCycleReportService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<AchNachaFileReportResponseDto> GetNachaFilesAsync(AchNachaFileReportFilter filter, CancellationToken ct = default)
    {
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var pageSize = filter.PageSize <= 0 ? 25 : Math.Min(filter.PageSize, 200);

        var query = _context.AchFileExports
            .AsNoTracking()
            .Include(f => f.ClearingHouse)
            .AsQueryable();

        if (filter.Date.HasValue)
        {
            var targetDate = filter.Date.Value.Date;
            query = query.Where(f => f.GeneratedAtUtc.Date == targetDate);
        }

        if (filter.ClearingHouseId.HasValue)
        {
            query = query.Where(f => f.ClearingHouseId == filter.ClearingHouseId.Value);
        }

        var total = await query.CountAsync(ct);
        var totalRecords = await query.Select(f => (int?)f.TotalRecords).SumAsync(ct) ?? 0;
        var totalTransactions = await query.Select(f => (int?)f.TotalTransactions).SumAsync(ct) ?? 0;

        var items = await query
            .OrderByDescending(f => f.GeneratedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new AchNachaFileReportRowDto
            {
                FileName = f.FileName,
                GeneratedAtUtc = f.GeneratedAtUtc,
                ClearingHouseName = f.ClearingHouse.Name,
                ExportKind = f.ExportKind,
                TotalRecords = f.TotalRecords,
                TotalTransactions = f.TotalTransactions
            })
            .ToListAsync(ct);

        return new AchNachaFileReportResponseDto
        {
            Items = items,
            Totals = new AchNachaFileReportTotalsDto
            {
                TotalFiles = total,
                TotalRecords = totalRecords,
                TotalTransactions = totalTransactions
            },
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<AchCycleReportResponseDto> GetCyclesAsync(AchCycleReportFilter filter, CancellationToken ct = default)
    {
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var pageSize = filter.PageSize <= 0 ? 25 : Math.Min(filter.PageSize, 200);

        var query = _context.AchCycles
            .AsNoTracking()
            .Include(c => c.ClearingHouse)
            .AsQueryable();

        if (filter.Date.HasValue)
        {
            var targetDate = filter.Date.Value.Date;
            query = query.Where(c => c.ProcessingDate.Date == targetDate);
        }

        if (filter.ClearingHouseId.HasValue)
        {
            query = query.Where(c => c.ClearingHouseId == filter.ClearingHouseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            var name = filter.Name.Trim();
            query = query.Where(c => c.CycleName.Contains(name));
        }

        var total = await query.CountAsync(ct);

        var cycleIds = await query.Select(c => c.Id).ToListAsync(ct);
        var totalsByCycle = await _context.AchTransactions
            .AsNoTracking()
            .Where(t => cycleIds.Contains(t.AchCycleId))
            .GroupBy(t => t.AchCycleId)
            .Select(g => new { CycleId = g.Key, TotalTransactions = g.Count(), TotalAmount = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        var totalTransactions = totalsByCycle.Sum(x => x.TotalTransactions);
        var totalAmount = totalsByCycle.Sum(x => x.TotalAmount);

        var totalsLookup = totalsByCycle.ToDictionary(x => x.CycleId, StringComparer.OrdinalIgnoreCase);
        var nowUtc = DateTime.UtcNow;

        var items = await query
            .OrderByDescending(c => c.ProcessingDate)
            .ThenBy(c => c.CutoffTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new AchCycleReportRowDto
            {
                CycleId = c.Id,
                CycleName = c.CycleName,
                ProcessingDate = c.ProcessingDate,
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                CutoffTime = c.CutoffTime,
                ClearingHouseName = c.ClearingHouse!.Name,
                TotalTransactions = 0,
                TotalAmount = 0m
            })
            .ToListAsync(ct);

        foreach (var item in items)
        {
            if (totalsLookup.TryGetValue(item.CycleId, out var cycleTotals))
            {
                item.Status = ResolveCycleStatus(item.ProcessingDate, item.StartTime, item.EndTime, nowUtc);
                item.TotalTransactions = cycleTotals.TotalTransactions;
                item.TotalAmount = cycleTotals.TotalAmount;
            }
            else
            {
                item.Status = ResolveCycleStatus(item.ProcessingDate, item.StartTime, item.EndTime, nowUtc);
            }
        }

        return new AchCycleReportResponseDto
        {
            Items = items,
            Totals = new AchCycleReportTotalsDto
            {
                TotalCycles = total,
                TotalTransactions = totalTransactions,
                TotalAmount = totalAmount
            },
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    private static string ResolveCycleStatus(DateTime processingDate, TimeSpan startTime, TimeSpan endTime, DateTime nowUtc)
    {
        var start = processingDate.Date + startTime;
        var end = endTime >= startTime
            ? processingDate.Date + endTime
            : processingDate.Date.AddDays(1) + endTime;

        if (nowUtc < start)
        {
            return "Programado";
        }

        if (nowUtc <= end)
        {
            return "En ejecución";
        }

        return "Cerrado";
    }
}
