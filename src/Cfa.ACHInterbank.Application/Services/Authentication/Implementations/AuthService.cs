using Cfa.ACHInterbank.Application.DataBase.Repositories.Security;
using Cfa.ACHInterbank.Application.Helpers.Hash;
using Cfa.ACHInterbank.Application.Services.Authentication.Interfaces;
using Cfa.ACHInterbank.Application.Services.Authentication.Models;
using Cfa.ACHInterbank.Application.Services.Notifications.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Cfa.ACHInterbank.Application.Services.Authentication.Implementations;

[Scoped]
public class AuthService : IAuthService
{
    private const int DefaultMaxFailedAttempts = 5;
    private const int DefaultLockoutMinutes = 5;
    private const int MaxAllowedFailedAttempts = 20;
    private const int MaxAllowedLockoutMinutes = 60;
    private const string LoginErrorMessage = "Usuario no válido o la contraseña o la cuenta está bloqueada debido a múltiples intentos de inicio de sesión fallidos. Si es así, se desbloquea automáticamente en poco tiempo.";
    private readonly IUserAuthRepository _userRepository;
    private readonly ILoginLockoutSettingsRepository _lockoutSettingsRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IEmailSender _emailSender;
    private readonly Token _tokenSettings;

    public AuthService(
        IUserAuthRepository userRepository,
        ILoginLockoutSettingsRepository lockoutSettingsRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IEmailSender emailSender)
    {
        _userRepository = userRepository;
        _lockoutSettingsRepository = lockoutSettingsRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _emailSender = emailSender;
        _tokenSettings = AppSettings.Settings.TokenManager!;
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new AuthResult { Success = false, Message = LoginErrorMessage };
        }

        var lockoutSettings = await _lockoutSettingsRepository.GetSettingsAsync(cancellationToken);
        var maxFailedAttempts = lockoutSettings?.MaxFailedAttempts > 0
            ? Math.Clamp(lockoutSettings.MaxFailedAttempts, 1, MaxAllowedFailedAttempts)
            : DefaultMaxFailedAttempts;
        var lockoutMinutes = lockoutSettings?.LockoutMinutes > 0
            ? Math.Clamp(lockoutSettings.LockoutMinutes, 1, MaxAllowedLockoutMinutes)
            : DefaultLockoutMinutes;
        var lockoutDuration = TimeSpan.FromMinutes(lockoutMinutes);

        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return new AuthResult { Success = false, Message = LoginErrorMessage };
        }

        if (user.LockoutEnd.HasValue)
        {
            if (user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                return new AuthResult { Success = false, Message = LoginErrorMessage };
            }

            await _userRepository.UpdateLoginStateAsync(user.Id, 0, null, cancellationToken);
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
        }

        var incomingHash = HashHelper.GenerateHashSha256(request.Password);
        if (!string.Equals(user.PasswordHash, incomingHash, StringComparison.OrdinalIgnoreCase))
        {
            var failedAttempts = user.FailedLoginAttempts + 1;
            DateTimeOffset? lockoutEnd = null;

            if (failedAttempts >= maxFailedAttempts)
            {
                lockoutEnd = DateTimeOffset.UtcNow.Add(lockoutDuration);
            }

            await _userRepository.UpdateLoginStateAsync(user.Id, failedAttempts, lockoutEnd, cancellationToken);
            return new AuthResult { Success = false, Message = LoginErrorMessage };
        }

        if (user.FailedLoginAttempts > 0 || user.LockoutEnd.HasValue)
        {
            await _userRepository.UpdateLoginStateAsync(user.Id, 0, null, cancellationToken);
        }

        return BuildAuthResult(user);
    }

    public async Task<OperationResult> RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return new OperationResult { Success = false, Message = "El correo es requerido" };
        }

        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Siempre responder genérico para no filtrar datos
        const string genericMessage = "Si el correo existe, se ha enviado un enlace de recuperación";

        if (user is null || !user.IsActive)
        {
            return new OperationResult { Success = true, Message = genericMessage };
        }

        var resetToken = BuildToken(user);
        await _passwordResetTokenRepository.AddAsync(resetToken, cancellationToken);

        var resetLink = BuildResetLink(resetToken.Token);
        await _emailSender.SendPasswordResetAsync(user, resetLink, cancellationToken);

        return new OperationResult { Success = true, Message = genericMessage };
    }

    public async Task<OperationResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return new OperationResult { Success = false, Message = "Token inválido" };
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || string.IsNullOrWhiteSpace(request.ConfirmPassword))
        {
            return new OperationResult { Success = false, Message = "La nueva contraseña es requerida" };
        }

        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return new OperationResult { Success = false, Message = "Las contraseñas no coinciden" };
        }

        var tokenEntity = await _passwordResetTokenRepository.GetValidTokenAsync(request.Token, cancellationToken);

        if (tokenEntity?.User is null)
        {
            return new OperationResult { Success = false, Message = "Token inválido o expirado" };
        }

        var newHash = HashHelper.GenerateHashSha256(request.NewPassword);
        await _userRepository.UpdatePasswordHashAsync(tokenEntity.User, newHash, cancellationToken);
        await _passwordResetTokenRepository.MarkAsUsedAsync(tokenEntity, cancellationToken);

        return new OperationResult { Success = true, Message = "Contraseña actualizada correctamente" };
    }

    public async Task<AuthResult> RefreshSessionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return new AuthResult { Success = false, Message = "Sesión inválida" };
        }

        return BuildAuthResult(user);
    }

    private static PasswordResetToken BuildToken(User user)
    {
        return new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = Guid.NewGuid().ToString("N"),
            Expiration = DateTimeOffset.UtcNow.AddMinutes(60),
            IsUsed = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static string BuildResetLink(string token)
    {
        var baseUrl = AppSettings.Settings.address ?? "https://localhost:4200";
        return $"{baseUrl.TrimEnd('/')}/reset-password?token={token}";
    }

    private AuthResult BuildAuthResult(User user)
    {
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
