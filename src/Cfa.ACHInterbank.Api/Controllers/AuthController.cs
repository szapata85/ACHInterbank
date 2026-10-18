using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Api.Exceptions;
using Cfa.ACHInterbank.Application.AuthLogs.Dtos;
using Cfa.ACHInterbank.Application.AuthLogs.Interfaces;
using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Application.Features;
using Cfa.ACHInterbank.Application.Services.Authentication.Interfaces;
using Cfa.ACHInterbank.Application.Services.Authentication.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("[controller]")]
[TypeFilter(typeof(ExceptionManager))]
public class AuthController : ControllerBase
{
    /// <summary>
    /// Inicia una sesión a partir de las credenciales proporcionadas.
    /// </summary>
    [EndpointSummary("Iniciar sesión")]
    [EndpointDescription("Autentica Username y Password, registra el intento de autenticación y devuelve un BaseResponseModel. Cuando la autenticación es correcta, Data contiene el AuthResult con el JWT y la información de autorización del usuario.")]
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BaseResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponseModel), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BaseResponseModel), StatusCodes.Status500InternalServerError)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Autenticación correcta. Data contiene el token JWT y la información de autorización del usuario.")]
    [OpenApiResponseDescription(StatusCodes.Status401Unauthorized, "Las credenciales o el estado de la cuenta no permiten iniciar sesión.")]
    [OpenApiResponseDescription(StatusCodes.Status500InternalServerError, "No fue posible registrar el intento de autenticación.")]
    [OpenApiRequestExample(typeof(LoginRequestExampleProvider))]
    [OpenApiResponseExample(StatusCodes.Status200OK, typeof(AuthSuccessResponseExampleProvider))]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] IAuthService authService,
        [FromServices] IAuthLogsService authLogsService,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);

        var authLogResult = await authLogsService.AddAsync(new AuthLogCreate
        {
            Username = result.Username ?? request.Username ?? string.Empty,
            Success = result.Success,
            FailureReason = result.Success ? null : result.Message,
            IpAddress = GetClientIpAddress(),
            UserAgent = Request.Headers.UserAgent.ToString()
        }, cancellationToken);

        if (!authLogResult.IsSuccess)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ResponseApiService.Response(StatusCodes.Status500InternalServerError, authLogResult));
        }

        if (!result.Success)
        {
            var failure = Result.Failure("AUTH_LOGIN_FAILED", result.Message ?? "No autorizado", ErrorType.Unauthorized);
            return StatusCode(StatusCodes.Status401Unauthorized, ResponseApiService.Response(StatusCodes.Status401Unauthorized, failure));
        }

        return Ok(ResponseApiService.Response(StatusCodes.Status200OK, Result<AuthResult>.Success(result)));
    }

    private string? GetClientIpAddress()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].ToString();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            var first = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
            {
                return NormalizeIp(first);
            }
        }

        var remoteIp = HttpContext.Connection.RemoteIpAddress;
        return remoteIp is null ? null : NormalizeIp(remoteIp.ToString());
    }

    private static string NormalizeIp(string value)
    {
        if (IPAddress.TryParse(value, out var parsed) && parsed.IsIPv4MappedToIPv6)
        {
            return parsed.MapToIPv4().ToString();
        }

        return value;
    }

    /// <summary>
    /// Solicita el inicio del flujo de recuperación de contraseña.
    /// </summary>
    [EndpointSummary("Solicitar recuperación de contraseña")]
    [EndpointDescription("Procesa una solicitud de recuperación para el correo indicado. Cuando el correo tiene valor, la respuesta exitosa es deliberadamente genérica y no confirma si existe una cuenta registrada con ese correo.")]
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BaseResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponseModel), StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Solicitud procesada con mensaje genérico sin revelar si el correo está registrado.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "El correo requerido no fue proporcionado.")]
    [OpenApiRequestExample(typeof(ForgotPasswordRequestExampleProvider))]
    [OpenApiResponseExample(StatusCodes.Status200OK, typeof(ForgotPasswordSuccessResponseExampleProvider))]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.RequestPasswordResetAsync(request, cancellationToken);
        var statusCode = result.Success ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest;
        var response = result.Success
            ? Result<string>.Success(result.Message)
            : Result<string>.Failure("AUTH_FORGOT_PASSWORD_FAILED", result.Message, ErrorType.Validation);
        return StatusCode(statusCode, ResponseApiService.Response(statusCode, response));
    }

    /// <summary>
    /// Restablece una contraseña a partir de un token de recuperación válido.
    /// </summary>
    [EndpointSummary("Restablecer contraseña")]
    [EndpointDescription("Valida el token de recuperación, la nueva contraseña y su confirmación. Si la solicitud es válida, actualiza la contraseña y consume el token de recuperación; la respuesta se entrega como BaseResponseModel.")]
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(BaseResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponseModel), StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "La contraseña fue actualizada correctamente.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La solicitud de restablecimiento no es válida o el token no puede utilizarse.")]
    [OpenApiRequestExample(typeof(ResetPasswordRequestExampleProvider))]
    [OpenApiResponseExample(StatusCodes.Status200OK, typeof(ResetPasswordSuccessResponseExampleProvider))]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.ResetPasswordAsync(request, cancellationToken);
        var statusCode = result.Success ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest;
        var response = result.Success
            ? Result<string>.Success(result.Message)
            : Result<string>.Failure("AUTH_RESET_PASSWORD_FAILED", result.Message, ErrorType.Validation);
        return StatusCode(statusCode, ResponseApiService.Response(statusCode, response));
    }

    /// <summary>
    /// Renueva la sesión autenticada a partir de la identidad contenida en el JWT actual.
    /// </summary>
    [EndpointSummary("Renovar sesión autenticada")]
    [EndpointDescription("Renueva la autenticación usando la identidad de los claims sub o uid del JWT Bearer actual. No recibe un refresh token separado ni request body; cuando la renovación es correcta, Data contiene un nuevo AuthResult.")]
    [HttpPost("refresh")]
    [Authorize]
    [ProducesResponseType(typeof(BaseResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponseModel), StatusCodes.Status401Unauthorized)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "La sesión fue renovada y Data contiene un nuevo resultado de autenticación.")]
    [OpenApiResponseDescription(StatusCodes.Status401Unauthorized, "La identidad de la sesión actual no es válida o el usuario no puede renovar la sesión.")]
    [OpenApiResponseExample(StatusCodes.Status200OK, typeof(AuthSuccessResponseExampleProvider))]
    public async Task<IActionResult> RefreshSession(
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue("uid");

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            var failure = Result.Failure("AUTH_INVALID_SESSION", "Sesión inválida", ErrorType.Unauthorized);
            return StatusCode(StatusCodes.Status401Unauthorized, ResponseApiService.Response(StatusCodes.Status401Unauthorized, failure));
        }

        var result = await authService.RefreshSessionAsync(userId, cancellationToken);

        if (!result.Success)
        {
            var failure = Result.Failure("AUTH_REFRESH_FAILED", result.Message ?? "Sesión inválida", ErrorType.Unauthorized);
            return StatusCode(StatusCodes.Status401Unauthorized, ResponseApiService.Response(StatusCodes.Status401Unauthorized, failure));
        }

        return Ok(ResponseApiService.Response(StatusCodes.Status200OK, Result<AuthResult>.Success(result)));
    }
}
