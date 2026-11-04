using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

/// <summary>I03-CENIT: reserva bajo petición autorizada; sin aprobar, generar ni enviar.</summary>
[ApiController]
[Authorize]
[Route("api/cenit/devoluciones-automaticas/preparaciones")]
public sealed class CenitReturnPreparationController(ICenitReturnPreparationService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = P1Policies.ConfigRead)]
    public async Task<ActionResult<IReadOnlyList<CenitReturnPreparationItem>>> Get(
        [FromQuery] int maxItems = 100, CancellationToken ct = default)
    {
        if (maxItems is < 1 or > 100)
            return BadRequest(new ProblemDetails { Title = "Límite inválido", Status = 400 });
        return Ok(await service.ListAsync(maxItems, ct));
    }

    [HttpPost]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<ActionResult<CenitReturnPreparationItem>> Prepare(
        [FromBody] PrepareCenitReturnRequest request, CancellationToken ct)
    {
        try { return Ok(await service.PrepareAsync(request, ct)); }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails { Title = "Solicitud inválida", Detail = ex.Message, Status = 400 });
        }
        catch (CenitReturnPreparationConflictException ex)
        {
            return Conflict(new ProblemDetails { Title = "No se pudo preparar la devolución",
                Detail = ex.Message, Status = 409 });
        }
    }
}
