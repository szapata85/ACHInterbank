using Cfa.ACHInterbank.Application.Exceptions;
using Cfa.ACHInterbank.Application.AuthLogs.Dtos;
using Cfa.ACHInterbank.Application.AuthLogs.Interfaces;
using Cfa.ACHInterbank.Application.Features;
using Cfa.ACHInterbank.Application.Services.Authentication.Interfaces;
using Cfa.ACHInterbank.Application.Services.Authentication.Models;
using Microsoft.AspNetCore.Authorization;
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
    /// Pendiente de documentación.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] IAuthService authService,
        [FromServices] IAuthLogsService authLogsService,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);

        await authLogsService.AddAsync(new AuthLogCreate
        {
            Username = result.Username ?? request.Username ?? string.Empty,
            Success = result.Success,
            FailureReason = result.Success ? null : result.Message,
            IpAddress = GetClientIpAddress(),
            UserAgent = Request.Headers.UserAgent.ToString()
        }, cancellationToken);

        if (!result.Success)
        {
            return StatusCode(StatusCodes.Status401Unauthorized, ResponseApiService.Response(StatusCodes.Status401Unauthorized, result.Message));
        }

        return Ok(ResponseApiService.Response(StatusCodes.Status200OK, result));
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
    /// Pendiente de documentación.
    /// </summary>

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, [FromServices] IAuthService authService, CancellationToken cancellationToken)
    {
        var result = await authService.RequestPasswordResetAsync(request, cancellationToken);
        var statusCode = result.Success ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest;
        return StatusCode(statusCode, ResponseApiService.Response(statusCode, result.Message, result.Message));
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, [FromServices] IAuthService authService, CancellationToken cancellationToken)
    {
        var result = await authService.ResetPasswordAsync(request, cancellationToken);
        var statusCode = result.Success ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest;
        return StatusCode(statusCode, ResponseApiService.Response(statusCode, result.Message, result.Message));
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpPost("refresh")]
    [Authorize]
    public async Task<IActionResult> RefreshSession([FromServices] IAuthService authService, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue("uid");

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return StatusCode(StatusCodes.Status401Unauthorized, ResponseApiService.Response(StatusCodes.Status401Unauthorized, "Sesión inválida"));
        }

        var result = await authService.RefreshSessionAsync(userId, cancellationToken);

        if (!result.Success)
        {
            return StatusCode(StatusCodes.Status401Unauthorized, ResponseApiService.Response(StatusCodes.Status401Unauthorized, result.Message));
        }

        return Ok(ResponseApiService.Response(StatusCodes.Status200OK, result));
    }
}
