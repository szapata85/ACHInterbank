using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Application.Security.Dtos;

namespace Cfa.ACHInterbank.Application.Security.Interfaces;

public interface IUsersService
{
    Task<PagedResponse<UserSummaryDto>> GetUsersAsync(UserQueryRequest request, CancellationToken ct = default);
    Task<bool> ValidateEmailDomainAsync(string email, CancellationToken ct = default);
    Task<UserSummaryDto?> GetUserAsync(Guid id, CancellationToken ct = default);
    Task<UserOperationResult> CreateAsync(SaveUserRequest? request, CancellationToken ct = default);
    Task<UserOperationResult> UpdateAsync(Guid id, SaveUserRequest? request, CancellationToken ct = default);
    Task<UserOperationResult> AssignRolesAsync(Guid id, AssignRolesRequest? request, CancellationToken ct = default);
    Task<UserOperationStatus> DeactivateAsync(Guid id, CancellationToken ct = default);
}
