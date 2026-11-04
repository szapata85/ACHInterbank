using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Api.Controllers;

/// <summary>Administración de la configuración de devoluciones automáticas solo para ACH Colombia.</summary>
[ApiController]
[Authorize]
[Route("api/ach-colombia/devoluciones-automaticas/configuracion")]
public sealed class AchColombiaAutomaticReturnSettingsController(
    IAchColombiaAutomaticReturnSettingsService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = P1Policies.ConfigRead)]
    public async Task<ActionResult<AchColombiaAutomaticReturnSettings>> Get(CancellationToken ct)
        => Ok(await service.GetAsync(ct));

    [HttpPut]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<ActionResult<AchColombiaAutomaticReturnSettings>> Update(
        [FromBody] UpdateAchColombiaAutomaticReturnSettings request, CancellationToken ct)
    {
        try
        {
            return Ok(await service.UpdateAsync(request, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails { Title = "Modo inválido", Detail = ex.Message, Status = 400 });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Configuración modificada por otro usuario",
                Detail = "Consulte la configuración vigente y vuelva a intentar.",
                Status = 409
            });
        }
    }
}
