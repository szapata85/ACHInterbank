using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/users/soap-integrations")]
[Authorize]
public class SoapIntegrationSettingsController : ControllerBase
{
    private readonly ISoapIntegrationSettingsService _service;

    public SoapIntegrationSettingsController(ISoapIntegrationSettingsService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<SoapIntegrationSettingsDto>> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await _service.GetAsync(cancellationToken);
        return Ok(settings);
    }

    [HttpPut]
    public async Task<ActionResult<SoapIntegrationSettingsDto>> SaveAsync(
        [FromBody] SoapIntegrationSettingsDto request,
        CancellationToken cancellationToken)
    {
        var settings = await _service.SaveAsync(request, cancellationToken);
        return Ok(settings);
    }
}
