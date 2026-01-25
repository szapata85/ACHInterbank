using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly AchDbContext _dbContext;

    public AuditLogsController(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpGet]
    public async Task<ActionResult<PagedResponse<AuditLogDto>>> GetAuditLogsAsync(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? changedBy,
        [FromQuery] string? action,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (page <= 0)
        {
            page = 1;
        }

        if (pageSize <= 0)
        {
            pageSize = 50;
        }

        var query = _dbContext.AuditLogs.AsNoTracking().AsQueryable();

        if (startDate.HasValue)
        {
            var startValue = startDate.Value;
            var startLocal = startValue.TimeOfDay == TimeSpan.Zero
                ? startValue.Date
                : startValue;
            query = query.Where(a => a.ChangedAt >= startLocal);
        }

        if (endDate.HasValue)
        {
            var endValue = endDate.Value;
            var upperBound = endValue.TimeOfDay == TimeSpan.Zero
                ? endValue.Date.AddDays(1).AddTicks(-1)
                : endValue;
            query = query.Where(a => a.ChangedAt <= upperBound);
        }

        if (!string.IsNullOrWhiteSpace(changedBy))
        {
            var term = changedBy.Trim();
            query = query.Where(a => a.ChangedBy.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            var normalizedAction = action.Trim().ToLowerInvariant();
            query = query.Where(a => a.Action.ToLower() == normalizedAction);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await (
                from audit in query
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
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<AuditLogDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }
}

public record AuditLogDto
{
    public Guid Id { get; init; }
    public string EntityName { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string ChangedBy { get; init; } = string.Empty;
    public DateTime ChangedAt { get; init; }
    public string? ChangedFields { get; init; }
}
