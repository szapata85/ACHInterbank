using Cfa.ACHInterbank.Application.AuthLogs.Dtos;
using Cfa.ACHInterbank.Application.AuthLogs.Interfaces;
using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Domain.Entities.Audit;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.AuthLogs.Services;

[Scoped]
public class AuthLogsService : IAuthLogsService
{
    private readonly AchDbContext _dbContext;

    public AuthLogsService(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResponse<AuthLogDto>> GetAsync(AuthLogQuery query, CancellationToken ct = default)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var authQuery = _dbContext.AuthLogs.AsNoTracking().AsQueryable();

        if (query.StartDate.HasValue)
        {
            var startValue = query.StartDate.Value;
            var startLocal = startValue.TimeOfDay == TimeSpan.Zero
                ? startValue.Date
                : startValue;
            authQuery = authQuery.Where(a => a.LoggedAt >= startLocal);
        }

        if (query.EndDate.HasValue)
        {
            var endValue = query.EndDate.Value;
            var upperBound = endValue.TimeOfDay == TimeSpan.Zero
                ? endValue.Date.AddDays(1).AddTicks(-1)
                : endValue;
            authQuery = authQuery.Where(a => a.LoggedAt <= upperBound);
        }

        if (!string.IsNullOrWhiteSpace(query.Username))
        {
            var term = query.Username.Trim();
            authQuery = authQuery.Where(a => a.Username.Contains(term));
        }

        if (query.Success.HasValue)
        {
            authQuery = authQuery.Where(a => a.Success == query.Success.Value);
        }

        var total = await authQuery.CountAsync(ct);

        var items = await authQuery
            .OrderByDescending(a => a.LoggedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuthLogDto
            {
                Id = a.Id,
                Username = a.Username,
                Success = a.Success,
                FailureReason = a.FailureReason,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                LoggedAt = a.LoggedAt
            })
            .ToListAsync(ct);

        return new PagedResponse<AuthLogDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task AddAsync(AuthLogCreate request, CancellationToken ct = default)
    {
        var log = new AuthLog
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Success = request.Success,
            FailureReason = request.FailureReason,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent,
            LoggedAt = DateTime.UtcNow
        };

        _dbContext.AuthLogs.Add(log);
        await _dbContext.SaveChangesAsync(ct);
    }
}
