using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Application.NavigationLogs.Dtos;

namespace Cfa.ACHInterbank.Application.NavigationLogs.Interfaces;

public interface INavigationLogsService
{
    Task<Result<PagedResponse<NavigationLogDto>>> GetAsync(NavigationLogQuery query, CancellationToken ct = default);
    Task<Result> AddAsync(NavigationLogCreate request, string? userId, string? ipAddress, string? userAgent, CancellationToken ct = default);
    Task<Result> PurgeOlderThanAsync(DateTime thresholdUtc, CancellationToken ct = default);
}
