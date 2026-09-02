using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ach-colombia/file-exchange")]
public sealed class AchColombiaFileExchangeController(IAchColombiaManagedFileExchangeService service) : ControllerBase
{
    [HttpGet("transfers")]
    [Authorize(Policy = "CanReadAch")]
    public Task<IReadOnlyList<AchManagedFileTransferSummary>> Query(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] AchManagedFileDirection? direction,
        [FromQuery] AchManagedFileTransferStatus? status, [FromQuery] string? cycleId,
        [FromQuery] AchManagedFileExecutionOrigin? executionOrigin, CancellationToken ct)
        => service.QueryAsync(new(from, to, direction, status, cycleId, executionOrigin), ct);

    [HttpGet("transfers/{id:guid}")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<ActionResult<AchManagedFileTransferDetail>> Get(Guid id, CancellationToken ct)
        => await service.GetAsync(id, ct) is { } result ? Ok(result) : NotFound();

    [HttpGet("transfers/{id:guid}/download")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var result = await service.DownloadAsync(id, Actor(), ct);
        return result is null ? NotFound() : File(result.Content, result.ContentType, result.FileName);
    }

    [HttpPost("outbound/execute")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<ActionResult<AchManagedFileExecutionResult>> ExecuteOutbound(ExecuteOutboundRequest request, CancellationToken ct)
        => Ok(await service.ExecuteOutboundAsync(request.CycleId, AchManagedFileExecutionOrigin.Manual, Actor(), Idempotency(), ct, request.CorrectedFromTransferId));

    [HttpPost("inbound/execute")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<ActionResult<AchManagedFileExecutionResult>> ExecuteInbound(CancellationToken ct)
        => Ok(await service.ExecuteInboundAsync(AchManagedFileExecutionOrigin.Manual, Actor(), Idempotency(), ct));

    [HttpPost("transfers/{id:guid}/retry")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<ActionResult<AchManagedFileTransferDetail>> Retry(Guid id, CancellationToken ct)
        => Ok(await service.RetryAsync(id, Actor(), Idempotency(), ct));

    [HttpPost("transfers/{id:guid}/reprocess")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<ActionResult<AchManagedFileTransferDetail>> Reprocess(Guid id, CancellationToken ct)
        => Ok(await service.ReprocessAsync(id, Actor(), ct));

    [HttpPost("transfers/{id:guid}/archive")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<ActionResult<AchManagedFileTransferDetail>> Archive(Guid id, CancellationToken ct)
        => Ok(await service.ArchiveAsync(id, Actor(), ct));

    [HttpPost("transfers/{id:guid}/retire")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<ActionResult<AchManagedFileTransferDetail>> Retire(Guid id, RetireRequest request, CancellationToken ct)
        => Ok(await service.RetireAsync(id, Actor(), request.Reason, ct));

    [HttpGet("configuration")]
    [Authorize(Policy = "CanReadAch")]
    public Task<AchManagedFileTransferConfigurationDto> GetConfiguration(CancellationToken ct)
        => service.GetConfigurationAsync(ct);

    [HttpPut("configuration")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<ActionResult<AchManagedFileTransferConfigurationDto>> UpdateConfiguration(
        AchManagedFileTransferConfigurationDto request, CancellationToken ct)
    {
        try { return Ok(await service.UpdateConfigurationAsync(request, Actor(), ct)); }
        catch (DbUpdateConcurrencyException) { return Conflict(new ProblemDetails { Status = 409, Title = "La configuración cambió", Detail = "Actualice la pantalla y vuelva a intentar." }); }
    }

    [HttpGet("administration")]
    [Authorize(Policy = "CanReadAch")]
    public Task<AchManagedMftAdministrationDto> GetAdministration(CancellationToken ct)
        => service.GetAdministrationAsync(ct);

    [HttpPut("administration")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<ActionResult<AchManagedMftAdministrationDto>> UpdateAdministration(UpdateAchManagedMftAdministrationRequest request, CancellationToken ct)
    {
        try { return Ok(await service.UpdateAdministrationAsync(request, Actor(), ct)); }
        catch (DbUpdateConcurrencyException) { return Conflict(new ProblemDetails { Status = 409, Title = "La configuración cambió", Detail = "Actualice la pantalla y vuelva a intentar." }); }
        catch (ArgumentException ex) { return BadRequest(new ProblemDetails { Status = 400, Title = "Configuración inválida", Detail = ex.Message }); }
    }

    [HttpPut("administration/credential")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<ActionResult<AchManagedMftAdministrationDto>> SetCredential(SetAchManagedMftCredentialRequest request, CancellationToken ct)
    {
        try { return Ok(await service.SetCredentialAsync(request, Actor(), ct)); }
        catch (ArgumentException ex) { return BadRequest(new ProblemDetails { Status = 400, Title = "Credencial inválida", Detail = ex.Message }); }
    }

    private string Actor() => User.Identity?.Name ?? "usuario-api";
    private string Idempotency() => Request.Headers.TryGetValue("Idempotency-Key", out var value) && !string.IsNullOrWhiteSpace(value)
        ? value.ToString() : $"manual:{HttpContext.TraceIdentifier}";

    public sealed record ExecuteOutboundRequest(string CycleId, Guid? CorrectedFromTransferId = null);
    public sealed record RetireRequest(string Reason);
}
