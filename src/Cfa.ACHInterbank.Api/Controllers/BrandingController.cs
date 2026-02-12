using Cfa.ACHInterbank.Application.Branding.Dtos;
using Cfa.ACHInterbank.Application.Branding.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/users/branding")]
public class BrandingController : ControllerBase
{
    private readonly IBrandingSettingsService _service;

    public BrandingController(IBrandingSettingsService service)
    {
        _service = service;
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<BrandingSettingsDto>> GetBrandingAsync(CancellationToken cancellationToken)
    {
        var branding = await _service.GetAsync(cancellationToken);
        return Ok(branding);
    }

    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>
    [HttpPut]
    // El sitio público también consume la identidad visual y esta instancia no usa JWT,
    // por lo que el endpoint debe estar disponible sin autenticación.
    [AllowAnonymous]
    public async Task<ActionResult<BrandingSettingsDto>> SaveBrandingAsync(
        [FromBody] BrandingSettingsDto request,
        CancellationToken cancellationToken)
    {
        var branding = await _service.SaveAsync(request, cancellationToken);
        return Ok(branding);
    }
}
