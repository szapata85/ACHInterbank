using Cfa.ACHInterbank.Application.Audit.Dtos;
using Cfa.ACHInterbank.Application.Common;

namespace Cfa.ACHInterbank.Application.Audit.Interfaces;

public interface IAuditLogsService
{
    Task<Result<PagedResponse<AuditLogDto>>> GetAsync(AuditLogQuery query, CancellationToken ct = default);
}
