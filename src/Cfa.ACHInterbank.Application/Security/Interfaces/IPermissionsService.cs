using Cfa.ACHInterbank.Application.Security.Dtos;

namespace Cfa.ACHInterbank.Application.Security.Interfaces;

public interface IPermissionsService
{
    Task<IEnumerable<PermissionSummaryDto>> GetAllAsync(CancellationToken ct = default);
}
