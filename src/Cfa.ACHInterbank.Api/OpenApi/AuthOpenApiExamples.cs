using Cfa.ACHInterbank.Application.Services.Authentication.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Api.OpenApi;

public sealed class LoginRequestExampleProvider : IOpenApiExampleProvider
{
    public object GetExample() => new LoginRequest
    {
        Username = "usuario.demo",
        Password = "ClaveDemo-123!"
    };
}

public sealed class AuthSuccessResponseExampleProvider : IOpenApiExampleProvider
{
    public object GetExample() => new BaseResponseModel
    {
        StatusCode = StatusCodes.Status200OK,
        Sucess = true,
        Data = new AuthResult
        {
            Success = true,
            Token = "jwt-ficticio-no-utilizable",
            ExpiresAt = new DateTimeOffset(2030, 1, 15, 12, 0, 0, TimeSpan.Zero),
            Username = "usuario.demo",
            FullName = "Usuario Demostración",
            Roles = ["rol.demo"],
            Permissions = ["permiso.demo"]
        }
    };
}

public sealed class ForgotPasswordRequestExampleProvider : IOpenApiExampleProvider
{
    public object GetExample() => new ForgotPasswordRequest
    {
        Email = "persona@example.test"
    };
}

public sealed class ForgotPasswordSuccessResponseExampleProvider : IOpenApiExampleProvider
{
    public object GetExample() => new BaseResponseModel
    {
        StatusCode = StatusCodes.Status200OK,
        Sucess = true,
        Data = "Si el correo existe, se ha enviado un enlace de recuperación"
    };
}

public sealed class ResetPasswordRequestExampleProvider : IOpenApiExampleProvider
{
    public object GetExample() => new ResetPasswordRequest
    {
        Token = "token-recuperacion-ficticio",
        NewPassword = "ClaveDemo-123!",
        ConfirmPassword = "ClaveDemo-123!"
    };
}

public sealed class ResetPasswordSuccessResponseExampleProvider : IOpenApiExampleProvider
{
    public object GetExample() => new BaseResponseModel
    {
        StatusCode = StatusCodes.Status200OK,
        Sucess = true,
        Data = "Contraseña actualizada correctamente"
    };
}
