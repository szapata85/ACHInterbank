using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Application.NavigationLogs.Dtos;
using Cfa.ACHInterbank.Application.NavigationLogs.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Audit;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.NavigationLogs.Services;

[Scoped]
public class NavigationLogsService : INavigationLogsService
{
    private readonly AchDbContext _dbContext;

    public NavigationLogsService(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedResponse<NavigationLogDto>>> GetAsync(NavigationLogQuery query, CancellationToken ct = default)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        var logsQuery = _dbContext.NavigationLogs.AsNoTracking().AsQueryable();

        if (query.StartDate.HasValue)
        {
            var startValue = query.StartDate.Value;
            var startLocal = startValue.TimeOfDay == TimeSpan.Zero
                ? startValue.Date
                : startValue;
            logsQuery = logsQuery.Where(a => a.VisitedAt >= startLocal);
        }

        if (query.EndDate.HasValue)
        {
            var endValue = query.EndDate.Value;
            var upperBound = endValue.TimeOfDay == TimeSpan.Zero
                ? endValue.Date.AddDays(1).AddTicks(-1)
                : endValue;
            logsQuery = logsQuery.Where(a => a.VisitedAt <= upperBound);
        }


        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            var term = query.UserId.Trim();
            logsQuery = logsQuery.Where(a =>
                (a.UserId != null && a.UserId.Contains(term)) ||
                _dbContext.Users.Any(u => u.Id.ToString() == a.UserId &&
                    ((u.Username != null && u.Username.Contains(term)) ||
                     (u.FullName != null && u.FullName.Contains(term)))));
        }

        if (!string.IsNullOrWhiteSpace(query.Route))
        {
            var routeTerm = query.Route.Trim();
            logsQuery = logsQuery.Where(a => a.Route.Contains(routeTerm));
        }

        var total = await logsQuery.CountAsync(ct);

        var items = await (
                from log in logsQuery
                join user in _dbContext.Users.AsNoTracking()
                    on log.UserId equals user.Id.ToString() into users
                from user in users.DefaultIfEmpty()
                orderby log.VisitedAt descending
                select new NavigationLogDto
                {
                    Id = log.Id,
                    UserId = log.UserId,
                    Username = user != null
                        ? (!string.IsNullOrWhiteSpace(user.Username) ? user.Username : user.FullName)
                        : null,
                    Route = log.Route,
                    VisitedAt = log.VisitedAt,
                    SessionId = log.SessionId,
                    DurationMs = log.DurationMs,
                    IpAddress = log.IpAddress,
                    UserAgent = log.UserAgent
                })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Result<PagedResponse<NavigationLogDto>>.Success(new PagedResponse<NavigationLogDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<Result> AddAsync(
        NavigationLogCreate request,
        string? userId,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        var route = NormalizeRoute(request.Route);
        if (string.IsNullOrWhiteSpace(route))
        {
            return Result.Failure("NAV_ROUTE_REQUIRED", "La ruta de navegación es obligatoria.", ErrorType.Validation);
        }

        var log = new NavigationLog
        {
            Id = Guid.NewGuid(),
            UserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim(),
            Route = route,
            VisitedAt = request.VisitedAt?.ToUniversalTime() ?? DateTime.UtcNow,
            SessionId = NormalizeSessionId(request.SessionId),
            DurationMs = request.DurationMs is > 0 and <= 86_400_000 ? request.DurationMs : null,
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim(),
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : TrimTo(userAgent.Trim(), 512)
        };

        _dbContext.NavigationLogs.Add(log);
        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> PurgeOlderThanAsync(DateTime thresholdUtc, CancellationToken ct = default)
    {
        var oldLogs = await _dbContext.NavigationLogs
            .Where(x => x.VisitedAt < thresholdUtc)
            .ToListAsync(ct);

        if (oldLogs.Count == 0)
        {
            return Result.Success();
        }

        _dbContext.NavigationLogs.RemoveRange(oldLogs);
        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static string NormalizeRoute(string? route)
    {
        var value = (route ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var questionMarkIndex = value.IndexOf('?');
        if (questionMarkIndex >= 0)
        {
            value = value[..questionMarkIndex];
        }

        var hashIndex = value.IndexOf('#');
        if (hashIndex >= 0)
        {
            value = value[..hashIndex];
        }

        if (!value.StartsWith('/'))
        {
            value = $"/{value}";
        }

        return TrimTo(value, 300);
    }

    private static string? NormalizeSessionId(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        return TrimTo(sessionId.Trim(), 100);
    }

    private static string TrimTo(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
