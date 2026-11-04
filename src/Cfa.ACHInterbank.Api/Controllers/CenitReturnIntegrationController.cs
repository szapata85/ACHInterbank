using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

/// <summary>
/// I06-CENIT: conciliación diagnóstica sin transporte, mutaciones ni acuses
/// atribuibles al artefacto local NO_TRANSMITIR de I05.
/// </summary>
[ApiController]
[Authorize]
[Route("api/cenit/devoluciones-automaticas/conciliacion")]
public sealed class CenitReturnIntegrationController(ICenitReturnIntegrationReadService service)
    : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = P1Policies.ConfigRead)]
    public async Task<ActionResult<CenitReturnIntegrationPage>> List(
        [FromQuery] int maxItems = 100, CancellationToken ct = default)
    {
        if (maxItems is < 1 or > 100)
            return BadRequest(new ProblemDetails { Title = "Límite inválido", Status = 400 });
        Response.Headers.CacheControl = "no-store, private";
        return Ok(await service.ListAsync(maxItems, ct));
    }

    [HttpGet("{preparationId:guid}")]
    [Authorize(Policy = P1Policies.ConfigRead)]
    public async Task<ActionResult<CenitReturnIntegrationDiagnostic>> Get(
        Guid preparationId, CancellationToken ct = default)
    {
        if (preparationId == Guid.Empty) return BadRequest();
        Response.Headers.CacheControl = "no-store, private";
        var result = await service.GetAsync(preparationId, ct);
        return result is null ? NotFound() : Ok(result);
    }
}
