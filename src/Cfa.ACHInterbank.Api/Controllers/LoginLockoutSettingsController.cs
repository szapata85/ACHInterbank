using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/users/login-lockout")]
[Authorize]
public class LoginLockoutSettingsController : ControllerBase
{
    private readonly ILoginLockoutSettingsService _service;

    public LoginLockoutSettingsController(ILoginLockoutSettingsService service)
    {
        _service = service;
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet]
    public async Task<ActionResult<LoginLockoutSettingsDto>> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await _service.GetAsync(cancellationToken);
        return Ok(settings);
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpPut]
    public async Task<ActionResult<LoginLockoutSettingsDto>> SaveAsync(
        [FromBody] LoginLockoutSettingsDto request,
        CancellationToken cancellationToken)
    {
        var settings = await _service.SaveAsync(request, cancellationToken);
        return Ok(settings);
    }
}
