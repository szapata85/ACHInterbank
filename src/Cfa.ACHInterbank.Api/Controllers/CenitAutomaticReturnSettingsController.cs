using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Api.Controllers;

/// <summary>Consulta y cambio autorizado del modo propio de devoluciones automáticas CENIT.</summary>
[ApiController]
[Authorize]
[Route("api/cenit/devoluciones-automaticas/configuracion")]
public sealed class CenitAutomaticReturnSettingsController(
    ICenitAutomaticReturnSettingsService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = P1Policies.ConfigRead)]
    public async Task<ActionResult<CenitAutomaticReturnSettings>> Get(CancellationToken ct)
        => Ok(await service.GetAsync(ct));

    [HttpPut]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<ActionResult<CenitAutomaticReturnSettings>> Update(
        [FromBody] UpdateCenitAutomaticReturnSettings request, CancellationToken ct)
    {
        try
        {
            return Ok(await service.UpdateAsync(request, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Modo inválido", Detail = ex.Message, Status = 400
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Configuración modificada por otro usuario",
                Detail = "Consulte el modo vigente de CENIT y vuelva a intentar.",
                Status = 409
            });
        }
    }
}
