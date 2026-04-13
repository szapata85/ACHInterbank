using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/integrations")]
[Authorize]
public class IntegrationCatalogController : ControllerBase
{
    private readonly IIntegrationCatalogService _catalogService;

    public IntegrationCatalogController(IIntegrationCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet("methods")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> GetMethods(CancellationToken ct)
        => Ok(await _catalogService.GetMethodsAsync(ct));

    [HttpGet("methods/{methodId:int}/parameters")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> GetParametersByMethod(int methodId, CancellationToken ct)
        => Ok(await _catalogService.GetMethodParametersAsync(methodId, ct));

    [HttpGet("source-catalog")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> GetSourceCatalog([FromQuery] int? methodId, CancellationToken ct)
        => Ok(await _catalogService.GetSourceCatalogAsync(methodId, ct));

    [HttpGet("transformations")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> GetTransformations(CancellationToken ct)
        => Ok(await _catalogService.GetTransformationsAsync(ct));
}
