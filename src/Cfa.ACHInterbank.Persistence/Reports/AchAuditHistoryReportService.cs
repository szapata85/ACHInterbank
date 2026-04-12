using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Models;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Reports;

[Scoped]
public sealed class AchAuditHistoryReportService : IAchAuditHistoryReportService
{
    private readonly AchDbContext _context;

    public AchAuditHistoryReportService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<AchAuditReportResponseDto> GetAuditAsync(AchAuditReportFilter filter, CancellationToken ct = default)
    {
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var pageSize = filter.PageSize <= 0 ? 25 : Math.Min(filter.PageSize, 200);

        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.User))
        {
            var user = filter.User.Trim();
            query = query.Where(x => x.ChangedBy.Contains(user));
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            var action = filter.Action.Trim();
            query = query.Where(x => x.Action.Contains(action));
        }

        if (!string.IsNullOrWhiteSpace(filter.Entity))
        {
            var entity = filter.Entity.Trim();
            query = query.Where(x => x.EntityName.Contains(entity));
        }

        if (filter.FromUtc.HasValue)
        {
            query = query.Where(x => x.ChangedAt >= filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue)
        {
            query = query.Where(x => x.ChangedAt <= filter.ToUtc.Value);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.ChangedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AchAuditReportRowDto
            {
                User = x.ChangedBy,
                Action = x.Action,
                Entity = x.EntityName,
                EntityId = x.EntityId,
                DateUtc = x.ChangedAt
            })
            .ToListAsync(ct);

        return new AchAuditReportResponseDto
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }

    public async Task<AchHistoryReportResponseDto> GetHistoryAsync(AchHistoryReportFilter filter, CancellationToken ct = default)
    {
        var page = filter.Page <= 0 ? 1 : filter.Page;
        var pageSize = filter.PageSize <= 0 ? 25 : Math.Min(filter.PageSize, 200);

        var query = _context.AchTransactionStateEvents
            .AsNoTracking()
            .AsQueryable();

        if (filter.FromUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= filter.ToUtc.Value);
        }

        if (filter.TransactionId.HasValue)
        {
            query = query.Where(x => x.AchTransactionId == filter.TransactionId.Value);
        }

        if (filter.ToState.HasValue)
        {
            query = query.Where(x => x.ToState == filter.ToState.Value);
        }

        if (filter.Source.HasValue)
        {
            query = query.Where(x => x.Source == filter.Source.Value);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AchHistoryReportRowDto
            {
                TransactionId = x.AchTransactionId,
                FromState = x.FromState,
                ToState = x.ToState,
                Source = x.Source,
                ReasonCode = x.ReasonCode,
                DateUtc = x.CreatedAt,
                ChangedBy = x.CreatedBy
            })
            .ToListAsync(ct);

        return new AchHistoryReportResponseDto
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
    }
}
