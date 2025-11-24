using Cfa.ACHInterbank.Application.Services.Authentication.Models;

namespace Cfa.ACHInterbank.Application.Services.Authentication.Interfaces;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
