using Cfa.ACHInterbank.Application.Audit.Dtos;
using Cfa.ACHInterbank.Application.Audit.Interfaces;
using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Audit.Services;

[Scoped]
public class AuditLogsService : IAuditLogsService
{
    private readonly AchDbContext _dbContext;

    public AuditLogsService(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResponse<AuditLogDto>> GetAsync(AuditLogQuery query, CancellationToken ct = default)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var auditQuery = _dbContext.AuditLogs.AsNoTracking().AsQueryable();

        if (query.StartDate.HasValue)
        {
            var startValue = query.StartDate.Value;
            var startLocal = startValue.TimeOfDay == TimeSpan.Zero
                ? startValue.Date
                : startValue;
            auditQuery = auditQuery.Where(a => a.ChangedAt >= startLocal);
        }

        if (query.EndDate.HasValue)
        {
            var endValue = query.EndDate.Value;
            var upperBound = endValue.TimeOfDay == TimeSpan.Zero
                ? endValue.Date.AddDays(1).AddTicks(-1)
                : endValue;
            auditQuery = auditQuery.Where(a => a.ChangedAt <= upperBound);
        }

        if (!string.IsNullOrWhiteSpace(query.ChangedBy))
        {
            var term = query.ChangedBy.Trim();
            auditQuery = auditQuery.Where(a => a.ChangedBy.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            var normalizedAction = query.Action.Trim().ToLowerInvariant();
            auditQuery = auditQuery.Where(a => a.Action.ToLower() == normalizedAction);
        }

        var total = await auditQuery.CountAsync(ct);

        var items = await (
                from audit in auditQuery
                join user in _dbContext.Users.AsNoTracking()
                    on audit.ChangedBy equals user.Id.ToString() into users
                from user in users.DefaultIfEmpty()
                orderby audit.ChangedAt descending
                select new AuditLogDto
                {
                    Id = audit.Id,
                    EntityName = audit.EntityName,
                    EntityId = audit.EntityId,
                    Action = audit.Action,
                    ChangedBy = user != null && !string.IsNullOrWhiteSpace(user.Username)
                        ? user.Username
                        : audit.ChangedBy,
                    ChangedAt = audit.ChangedAt,
                    ChangedFields = audit.ChangedFields
                })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResponse<AuditLogDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }
}
