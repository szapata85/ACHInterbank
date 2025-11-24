using Cfa.ACHInterbank.Application.DataBase.Repositories.Security;
using Cfa.ACHInterbank.Application.Helpers.Hash;
using Cfa.ACHInterbank.Application.Services.Authentication.Interfaces;
using Cfa.ACHInterbank.Application.Services.Authentication.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Cfa.ACHInterbank.Application.Services.Authentication.Implementations;

[Scoped]
public class AuthService : IAuthService
{
    private readonly IUserAuthRepository _userRepository;
    private readonly Token _tokenSettings;

    public AuthService(IUserAuthRepository userRepository)
    {
        _userRepository = userRepository;
        _tokenSettings = AppSettings.Settings.TokenManager!;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new AuthResult { Success = false, Message = "Credenciales inválidas" };
        }

        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return new AuthResult { Success = false, Message = "Usuario o contraseña incorrectos" };
        }

        var incomingHash = HashHelper.GenerateHashSha1(request.Password);
        if (!string.Equals(user.PasswordHash, incomingHash, StringComparison.OrdinalIgnoreCase))
        {
            return new AuthResult { Success = false, Message = "Usuario o contraseña incorrectos" };
        }

        var roles = user.UserRoles.Select(x => x.Role!.Name!).Distinct().ToList();
        var permissions = user.UserRoles
            .SelectMany(x => x.Role!.RolePermissions)
            .Select(rp => rp.Permission!.Name!)
            .Distinct()
            .ToList();

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_tokenSettings.secretKetJwt!);
        var expires = CalculateExpiration();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username!),
            new("name", user.FullName ?? user.Username!),
            new("uid", user.Id.ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(permission => new Claim("permission", permission)));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires.UtcDateTime,
            Issuer = _tokenSettings.issuerJwt,
            Audience = _tokenSettings.audienceJwt,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(descriptor);
        var serializedToken = tokenHandler.WriteToken(token);

        return new AuthResult
        {
            Success = true,
            Token = serializedToken,
            ExpiresAt = expires,
            Username = user.Username,
            FullName = user.FullName,
            Roles = roles,
            Permissions = permissions
        };
    }

    private DateTimeOffset CalculateExpiration()
    {
        if (string.Equals(_tokenSettings.expirationType, "d", StringComparison.OrdinalIgnoreCase))
        {
            return DateTimeOffset.UtcNow.AddDays(_tokenSettings.accessExpiration);
        }

        return DateTimeOffset.UtcNow.AddMinutes(_tokenSettings.accessExpiration);
    }
}
