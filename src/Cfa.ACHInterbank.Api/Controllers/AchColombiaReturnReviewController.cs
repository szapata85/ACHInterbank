using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

/// <summary>I04-ACH/I05-ACH: revisión y generación explícita; no transmite .RET.</summary>
[ApiController]
[Authorize]
[Route("api/ach-colombia/devoluciones-automaticas/candidatas")]
public sealed class AchColombiaReturnReviewController(IAchColombiaReturnReviewService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = P0Policies.ReturnsRead)]
    public async Task<ActionResult<IReadOnlyList<AchColombiaReturnReviewItem>>> Get(
        [FromQuery] int limit = 50, CancellationToken ct = default)
        => Ok(await service.ListAsync(Math.Clamp(limit, 1, 100), ct));

    /// <summary>Genera un archivo .RET a partir de una candidata aprobada de ACH Colombia.
    /// No realiza transmisión automática. Usa el generador y controles de duplicidad existentes.</summary>
    [HttpPost("{candidateId:long}/generar-archivo")]
    [Authorize(Policy = P0Policies.ReturnsGenerateFile)]
    public async Task<IActionResult> GenerateApprovedFile(long candidateId, CancellationToken ct)
    {
        if (candidateId <= 0)
            return BadRequest(new ProblemDetails
            {
                Title = "Candidata no válida", Detail = "Selecciona una candidata válida.", Status = 400
            });
        try
        {
            var file = await service.GenerateApprovedFileAsync(candidateId, ct);
            // La respuesta JSON conserva el nombre oficial y evita que el navegador
            // lo reemplace cuando la SPA descarga el archivo de una sola candidata.
            return Ok(file);
        }
        catch (AchReturnAlreadyGeneratedException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Devolución ya generada", Detail = ex.Message, Status = 409
            });
        }
        catch (AchColombiaReturnReviewConflictException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "No se pudo generar el archivo", Detail = ex.Message, Status = 409
            });
        }
    }

    /// <summary>Recupera un .RET previamente generado, sin regenerar secuencias ni transmitir.</summary>
    [HttpGet("{candidateId:long}/archivo")]
    [Authorize(Policy = P0Policies.ReturnsGenerateFile)]
    public async Task<IActionResult> RecoverGeneratedFile(long candidateId, CancellationToken ct)
    {
        try
        {
            var result = await service.RecoverGeneratedFileAsync(candidateId, ct);
            return Ok(result);
        }
        catch (AchColombiaReturnReviewConflictException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Archivo no disponible", Detail = ex.Message, Status = 409
            });
        }
    }

    [HttpPost("{candidateId:long}/decision")]
    [Authorize(Policy = P0Policies.ReturnsGenerateFile)]
    public async Task<ActionResult<AchColombiaReturnReviewResult>> Decide(
        long candidateId, [FromBody] AchColombiaReturnReviewDecision decision, CancellationToken ct)
    {
        if (candidateId <= 0 || decision is null || decision.Action is not ("APROBAR" or "DESCARTAR"))
            return BadRequest(new ProblemDetails
            {
                Title = "Decisión no válida",
                Detail = "Selecciona aprobar o descartar una candidata válida.", Status = 400
            });

        try { return Ok(await service.DecideAsync(candidateId, decision, ct)); }
        catch (AchColombiaReturnReviewConflictException ex)
        {
            return Conflict(new ProblemDetails
            {
                Title = "No se pudo resolver la candidata", Detail = ex.Message, Status = 409
            });
        }
    }
}
