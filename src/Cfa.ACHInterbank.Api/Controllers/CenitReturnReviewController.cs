using System.Security.Claims;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

/// <summary>I04-CENIT: decisiones humanas, sin NACHA-M, generación ni transmisión.</summary>
[ApiController]
[Authorize]
[Route("api/cenit/devoluciones-automaticas/revisiones")]
public sealed class CenitReturnReviewController(ICenitReturnReviewService review,
    ICenitPassiveReturnDetector detector) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = P1Policies.ConfigRead)]
    public async Task<ActionResult<IReadOnlyList<CenitReturnReviewItem>>> Get(
        [FromQuery] int maxItems = 100, CancellationToken ct = default)
    {
        if (maxItems is < 1 or > 100)
            return BadRequest(new ProblemDetails { Title = "Límite inválido", Status = 400 });
        return Ok(await review.ListAsync(maxItems, ct));
    }

    /// <summary>Excepciones I02 solo para consulta humana. Jamás crea reservas o aprobaciones.</summary>
    [HttpGet("excepciones")]
    [Authorize(Policy = P1Policies.ConfigRead)]
    public async Task<ActionResult<CenitPassiveReturnDetection>> GetExceptions(
        [FromQuery] int maxTransactions = 100, CancellationToken ct = default)
    {
        if (maxTransactions is < 1 or > 100)
            return BadRequest(new ProblemDetails { Title = "Límite inválido", Status = 400 });
        var findings = await detector.DetectAsync(maxTransactions, ct);
        // Conservar LimitedWindow: no sugerir que se consultó todo el histórico.
        return Ok(findings with { Findings = findings.Findings.Where(x => x.Classification is
            "REVISION_CAUSAL" or "REVISION_SOLICITUD" or "REVISION_PRENOTIFICACION").ToArray() });
    }

    [HttpPost("{preparationId:guid}/decision")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<ActionResult<CenitReturnReviewItem>> Decide(
        Guid preparationId, [FromBody] CenitReturnReviewDecision decision, CancellationToken ct)
    {
        // Identidad exclusivamente del principal autenticado, no de campos editables del cliente.
        var user = HttpContext?.User;
        var actor = user?.FindFirst("uid")?.Value
            ?? user?.FindFirst("sub")?.Value
            ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (user?.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(actor))
            return Forbid();
        try { return Ok(await review.DecideAsync(preparationId, decision, actor, ct)); }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails { Title = "Decisión inválida", Detail = ex.Message, Status = 400 });
        }
        catch (CenitReturnReviewConflictException ex)
        {
            return Conflict(new ProblemDetails { Title = "No se puede resolver la preparación",
                Detail = ex.Message, Status = 409 });
        }
    }
}
