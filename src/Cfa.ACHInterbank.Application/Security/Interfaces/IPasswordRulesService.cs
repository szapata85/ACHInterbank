using Cfa.ACHInterbank.Application.Security.Dtos;

namespace Cfa.ACHInterbank.Application.Security.Interfaces;

public interface IPasswordRulesService
{
    Task<PasswordRulesDto> GetAsync(CancellationToken ct = default);
    Task<PasswordRulesDto> SaveAsync(PasswordRulesDto request, CancellationToken ct = default);
}
