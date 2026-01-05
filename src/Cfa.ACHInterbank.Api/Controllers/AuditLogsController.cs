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

    [HttpGet]
    public async Task<ActionResult<PagedResponse<AuditLogDto>>> GetAuditLogsAsync(
        [FromQuery] DateTimeOffset? startDate,
        [FromQuery] DateTimeOffset? endDate,
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
            var startUtc = startDate.Value.ToUniversalTime();
            query = query.Where(a => a.ChangedAt >= startUtc);
        }

        if (endDate.HasValue)
        {
            var upperBound = endDate.Value.ToUniversalTime();
            if (upperBound.TimeOfDay == TimeSpan.Zero)
            {
                upperBound = upperBound.AddDays(1).AddTicks(-1);
            }

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

        var items = await query
            .OrderByDescending(a => a.ChangedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                Action = a.Action,
                ChangedBy = a.ChangedBy,
                ChangedAt = a.ChangedAt,
                ChangedFields = a.ChangedFields
            })
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
    public DateTimeOffset ChangedAt { get; init; }
    public string? ChangedFields { get; init; }
}
