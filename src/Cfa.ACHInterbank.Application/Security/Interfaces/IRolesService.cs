using Cfa.ACHInterbank.Application.Security.Dtos;

namespace Cfa.ACHInterbank.Application.Security.Interfaces;

public interface IRolesService
{
    Task<IEnumerable<RoleSummaryDto>> GetAllAsync(CancellationToken ct = default);
}
