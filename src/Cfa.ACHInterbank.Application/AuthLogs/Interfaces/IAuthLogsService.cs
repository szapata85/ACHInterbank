using Cfa.ACHInterbank.Application.AuthLogs.Dtos;
using Cfa.ACHInterbank.Application.Common;

namespace Cfa.ACHInterbank.Application.AuthLogs.Interfaces;

public interface IAuthLogsService
{
    Task<Result<PagedResponse<AuthLogDto>>> GetAsync(AuthLogQuery query, CancellationToken ct = default);
    Task<Result> AddAsync(AuthLogCreate request, CancellationToken ct = default);
}
