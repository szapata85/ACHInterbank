using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("nacha-config")]
[Authorize]
public sealed class NachaConfigProfilesController : ControllerBase
{
    private readonly INachaConfigProfileQueryService _query;
    private readonly INachaConfigProfileCommandService _command;
    private readonly INachaConfigValidationService _validation;
    private readonly INachaConfigPublicationService _publication;
    private readonly INachaConfigHistoryService _history;
    private readonly INachaConfigPreviewService _preview;

    public NachaConfigProfilesController(
        INachaConfigProfileQueryService query,
        INachaConfigProfileCommandService command,
        INachaConfigValidationService validation,
        INachaConfigPublicationService publication,
        INachaConfigHistoryService history,
        INachaConfigPreviewService preview)
    {
        _query = query;
        _command = command;
        _validation = validation;
        _publication = publication;
        _history = history;
        _preview = preview;
    }

    [HttpGet("perfiles")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<ActionResult<IReadOnlyList<NachaConfigProfileListItemDto>>> GetProfiles(CancellationToken ct)
        => Ok(await _query.GetProfilesAsync(ct));

    [HttpGet("perfiles/{id:int}")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<ActionResult<NachaConfigProfileDetailDto>> GetProfile(int id, CancellationToken ct)
    {
        var detail = await _query.GetProfileDetailAsync(id, ct);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost("perfiles")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<ActionResult<NachaConfigProfileDetailDto>> CreateDraft([FromBody] NachaConfigCreateDraftRequest request, CancellationToken ct)
    {
        var actor = User?.Identity?.Name ?? "system";
        var created = await _command.CreateDraftAsync(request, actor, ct);
        return CreatedAtAction(nameof(GetProfile), new { id = created.Id }, created);
    }

    [HttpPut("perfiles/{id:int}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<ActionResult<NachaConfigProfileDetailDto>> UpdateDraft(int id, [FromBody] NachaConfigUpdateProfileRequest request, CancellationToken ct)
    {
        var actor = User?.Identity?.Name ?? "system";
        var updated = await _command.UpdateDraftAsync(id, request, actor, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpPost("perfiles/{id:int}/clonar")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<ActionResult<NachaConfigProfileDetailDto>> Clone(int id, [FromBody] NachaConfigCloneProfileRequest request, CancellationToken ct)
    {
        var actor = User?.Identity?.Name ?? "system";
        var cloned = await _command.CloneProfileAsync(id, request, actor, ct);
        return cloned is null ? NotFound() : Ok(cloned);
    }

    [HttpPost("perfiles/{id:int}/validar")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<ActionResult<NachaConfigValidationResultDto>> Validate(int id, CancellationToken ct)
        => Ok(await _validation.ValidateBeforePublishAsync(id, ct));

    [HttpPost("perfiles/{id:int}/publicar")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<ActionResult<NachaConfigPublicationResultDto>> Publish(int id, CancellationToken ct)
    {
        var actor = User?.Identity?.Name ?? "system";
        var result = await _publication.PublishAsync(id, actor, ct);
        return result.Publicado ? Ok(result) : BadRequest(result);
    }

    [HttpPost("perfiles/{id:int}/inactivar")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Inactivate(int id, CancellationToken ct)
    {
        var actor = User?.Identity?.Name ?? "system";
        return await _command.InactivateProfileAsync(id, actor, ct) ? Ok() : NotFound();
    }

    [HttpPost("perfiles/{id:int}/archivar")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> Archive(int id, CancellationToken ct)
    {
        var actor = User?.Identity?.Name ?? "system";
        return await _command.ArchiveProfileAsync(id, actor, ct) ? Ok() : NotFound();
    }

    [HttpGet("perfiles/{id:int}/historial")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<ActionResult<IReadOnlyList<NachaConfigHistoryItemDto>>> History(int id, CancellationToken ct)
        => Ok(await _history.GetHistoryAsync(id, ct));

    [HttpGet("perfiles/{id:int}/snapshots")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<ActionResult<IReadOnlyList<NachaConfigSnapshotItemDto>>> Snapshots(int id, CancellationToken ct)
        => Ok(await _history.GetSnapshotsAsync(id, ct));

    [HttpPut("perfiles/{id:int}/records/secuencia")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> UpdateSequence(int id, [FromBody] IReadOnlyList<NachaConfigProfileRecordSequenceDto> request, CancellationToken ct)
    {
        var actor = User?.Identity?.Name ?? "system";
        return await _command.UpdateRecordSequenceAsync(id, request, actor, ct) ? Ok() : NotFound();
    }

    [HttpPut("perfiles/{id:int}/variantes/{variantId:int}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> UpdateVariant(int id, int variantId, [FromBody] NachaConfigLayoutVariantEditDto request, CancellationToken ct)
    {
        var actor = User?.Identity?.Name ?? "system";
        return await _command.UpdateLayoutVariantAsync(id, variantId, request, actor, ct) ? Ok() : NotFound();
    }

    [HttpPut("perfiles/{id:int}/fields/{fieldId:int}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> UpdateField(int id, int fieldId, [FromBody] NachaConfigLayoutFieldEditDto request, CancellationToken ct)
    {
        var actor = User?.Identity?.Name ?? "system";
        return await _command.UpdateLayoutFieldAsync(id, fieldId, request, actor, ct) ? Ok() : NotFound();
    }

    [HttpPut("perfiles/{id:int}/rules/{ruleId:int}")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> UpdateRule(int id, int ruleId, [FromBody] NachaConfigFieldRuleEditDto request, CancellationToken ct)
    {
        var actor = User?.Identity?.Name ?? "system";
        return await _command.UpdateFieldRuleAsync(id, ruleId, request, actor, ct) ? Ok() : NotFound();
    }

    [HttpPost("resolver-preview")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<ActionResult<NachaConfigResolverPreviewResultDto>> ResolverPreview([FromBody] NachaConfigResolverPreviewRequest request, CancellationToken ct)
        => Ok(await _preview.PreviewResolverAsync(request, ct));
}
