using Cfa.ACHInterbank.Application.AuthLogs.Dtos;
using Cfa.ACHInterbank.Application.Common;

namespace Cfa.ACHInterbank.Application.AuthLogs.Interfaces;

public interface IAuthLogsService
{
    Task<PagedResponse<AuthLogDto>> GetAsync(AuthLogQuery query, CancellationToken ct = default);
    Task AddAsync(AuthLogCreate request, CancellationToken ct = default);
}
