using Cfa.ACHInterbank.Application.Exceptions;
using Cfa.ACHInterbank.Application.Features;
using Cfa.ACHInterbank.Application.Services.Authentication.Interfaces;
using Cfa.ACHInterbank.Application.Services.Authentication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("[controller]")]
[TypeFilter(typeof(ExceptionManager))]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, [FromServices] IAuthService authService, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);

        if (!result.Success)
        {
            return StatusCode(StatusCodes.Status401Unauthorized, ResponseApiService.Response(StatusCodes.Status401Unauthorized, result.Message));
        }

        return Ok(ResponseApiService.Response(StatusCodes.Status200OK, result));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, [FromServices] IAuthService authService, CancellationToken cancellationToken)
    {
        var result = await authService.RequestPasswordResetAsync(request, cancellationToken);
        var statusCode = result.Success ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest;
        return StatusCode(statusCode, ResponseApiService.Response(statusCode, result.Message, result.Message));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, [FromServices] IAuthService authService, CancellationToken cancellationToken)
    {
        var result = await authService.ResetPasswordAsync(request, cancellationToken);
        var statusCode = result.Success ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest;
        return StatusCode(statusCode, ResponseApiService.Response(statusCode, result.Message, result.Message));
    }

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
