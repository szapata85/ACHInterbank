using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

/// <summary>I02-CENIT: solo consulta, nunca prepara, genera ni transmite devoluciones.</summary>
[ApiController]
[Authorize]
[Route("api/cenit/devoluciones-automaticas/deteccion")]
public sealed class CenitPassiveReturnDetectionController(ICenitPassiveReturnDetector detector) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = P1Policies.ConfigRead)]
    public async Task<ActionResult<CenitPassiveReturnDetection>> Get(
        [FromQuery] int maxTransactions = 100, CancellationToken ct = default)
    {
        if (maxTransactions is < 1 or > 100)
            return BadRequest(new ProblemDetails
            {
                Title = "Límite de consulta inválido",
                Detail = "El número de transacciones debe estar entre 1 y 100.", Status = 400
            });
        return Ok(await detector.DetectAsync(maxTransactions, ct));
    }
}
