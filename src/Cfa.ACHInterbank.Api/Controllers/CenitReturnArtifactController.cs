using System.Security.Claims;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

/// <summary>I05-CENIT: archivo local de revisión, sin endpoint de transporte ni nombre externo.</summary>
[ApiController]
[Authorize]
[Route("api/cenit/devoluciones-automaticas/artefactos")]
public sealed class CenitReturnArtifactController(ICenitReturnArtifactService artifacts) : ControllerBase
{
    [HttpGet("{preparationId:guid}")]
    [Authorize(Policy = P1Policies.ConfigRead)]
    public async Task<ActionResult<CenitReturnArtifactMetadata>> Get(Guid preparationId,
        CancellationToken ct = default)
    {
        try { return Ok(await artifacts.GetMetadataAsync(preparationId, ct)); }
        catch (CenitReturnArtifactConflictException ex) { return Conflict(Error(ex.Message)); }
    }

    [HttpPost("{preparationId:guid}/generar-local")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<IActionResult> GenerateLocal(Guid preparationId, CancellationToken ct)
    {
        var user = HttpContext?.User;
        var actor = user?.FindFirst("uid")?.Value
            ?? user?.FindFirst("sub")?.Value
            ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (user?.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(actor))
            return Forbid();
        try { return LocalFile(await artifacts.GenerateAsync(preparationId, actor, ct)); }
        catch (ArgumentException ex) { return BadRequest(Error(ex.Message, 400)); }
        catch (CenitReturnArtifactConflictException ex) { return Conflict(Error(ex.Message)); }
    }

    [HttpGet("{preparationId:guid}/descargar-local")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    public async Task<IActionResult> DownloadLocal(Guid preparationId, CancellationToken ct)
    {
        try { return LocalFile(await artifacts.ReadLocalAsync(preparationId, ct)); }
        catch (CenitReturnArtifactConflictException ex) { return Conflict(Error(ex.Message)); }
    }

    private IActionResult LocalFile(CenitReturnArtifactResult artifact)
    {
        Response.Headers.CacheControl = "no-store, private";
        Response.Headers.Pragma = "no-cache";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        // Nombre inequívocamente interno, prohibido como nombre de transmisión.
        return File(artifact.Content, "application/octet-stream", artifact.InternalFileName);
    }
    private static ProblemDetails Error(string message, int status = 409) =>
        new() { Title = "Operación I05-CENIT no permitida", Detail = message, Status = status };
}
