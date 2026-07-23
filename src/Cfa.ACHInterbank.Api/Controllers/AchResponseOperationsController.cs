using System.Security.Claims;
using Cfa.ACHInterbank.Api.Contracts.AchResponses;
using Cfa.ACHInterbank.Application.ACH.Responses.Operations;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Authorize]
public sealed class AchResponseOperationsController : ControllerBase
{
    private readonly IAchResponseOperationsService _service;
    public AchResponseOperationsController(IAchResponseOperationsService service) => _service = service;

    [HttpGet("api/ach/response-status-mappings/{id:int}")]
    [Authorize(Policy = P1Policies.NachaRead)]
    public async Task<IActionResult> GetMapping(int id, CancellationToken ct)
    {
        var item = await _service.GetMappingAsync(id, ct);
        return item is null ? NotFound(Problem("Mapping no encontrado.", 404)) : Ok(item);
    }

    [HttpPost("api/ach/response-status-mappings")]
    [Authorize(Policy = P1Policies.NachaGenerate)]
    public Task<IActionResult> CreateMapping([FromBody] AchResponseMappingWriteRequest request, CancellationToken ct)
        => Execute(async () =>
        {
            var correlation = Correlation(null);
            var created = await _service.CreateMappingAsync(Map(request), Actor(), correlation, ct);
            return CreatedAtAction(nameof(GetMapping), new { id = created.Id }, created);
        });

    [HttpPut("api/ach/response-status-mappings/{id:int}")]
    [Authorize(Policy = P1Policies.NachaGenerate)]
    public Task<IActionResult> UpdateMapping(int id, [FromBody] AchResponseMappingWriteRequest request, CancellationToken ct)
        => Execute(async () => Ok(await _service.UpdateMappingAsync(id, Map(request), Actor(), Correlation(null), ct)));

    [HttpPost("api/ach/response-status-mappings/{id:int}/activate")]
    [Authorize(Policy = P1Policies.NachaGenerate)]
    public Task<IActionResult> ActivateMapping(int id, [FromBody] VersionedReasonRequest request, CancellationToken ct)
        => Execute(async () => Ok(await _service.SetMappingActiveAsync(id, true, request.ExpectedVersion,
            request.Reason, Actor(), Correlation(request.CorrelationId), ct)));

    [HttpPost("api/ach/response-status-mappings/{id:int}/deactivate")]
    [Authorize(Policy = P1Policies.NachaGenerate)]
    public Task<IActionResult> DeactivateMapping(int id, [FromBody] VersionedReasonRequest request, CancellationToken ct)
        => Execute(async () => Ok(await _service.SetMappingActiveAsync(id, false, request.ExpectedVersion,
            request.Reason, Actor(), Correlation(request.CorrelationId), ct)));

    [HttpGet("api/ach/response-status-mappings/{id:int}/audit")]
    [Authorize(Policy = P1Policies.NachaRead)]
    public async Task<IActionResult> MappingAudit(int id, CancellationToken ct)
        => Ok(await _service.GetAuditAsync("AchResponseStatusMapping", id.ToString(), ct));

    [HttpGet("api/ach/responses/{id:guid}/audit")]
    [Authorize(Policy = P1Policies.NachaRead)]
    public async Task<IActionResult> ResponseAudit(Guid id, CancellationToken ct)
        => Ok(await _service.GetAuditAsync("AchResponse", id.ToString(), ct));

    [HttpGet("api/ach/responses/orphans")]
    [Authorize(Policy = P1Policies.NachaRead)]
    public async Task<IActionResult> ListOrphans([FromQuery] int? clearingHouseId, [FromQuery] string? status, CancellationToken ct)
        => Ok(await _service.ListOrphansAsync(clearingHouseId, status, ct));

    [HttpPost("api/ach/responses/{id:guid}/orphan")]
    [Authorize(Policy = P1Policies.NachaGenerate)]
    public Task<IActionResult> CreateOrphan(Guid id, [FromBody] CreateOrphanRequest request, CancellationToken ct)
        => Execute(async () => Ok(await _service.CreateOrphanAsync(id, request.Reason, request.CandidateReferences,
            Actor(), Correlation(request.CorrelationId), ct)));

    [HttpPost("api/ach/responses/orphans/{id:guid}/review/start")]
    [Authorize(Policy = P1Policies.NachaGenerate)]
    public Task<IActionResult> BeginReview(Guid id, [FromBody] VersionedReasonRequest request, CancellationToken ct)
        => Execute(async () => Ok(await _service.BeginReviewAsync(id, request.ExpectedVersion, request.Reason,
            Actor(), Correlation(request.CorrelationId), ct)));

    [HttpPost("api/ach/responses/orphans/{id:guid}/resolve")]
    [Authorize(Policy = P1Policies.NachaGenerate)]
    public Task<IActionResult> ResolveOrphan(Guid id, [FromBody] ResolveOrphanRequest request, CancellationToken ct)
        => Execute(async () => Ok(await _service.ResolveOrphanAsync(id,
            new ManualResolutionCommand(request.ExpectedVersion, request.Reason, request.FunctionalReference,
                request.Reject, Correlation(request.CorrelationId)), Actor(), ct)));

    [HttpPost("api/ach/responses/{id:guid}/reprocess")]
    [Authorize(Policy = P1Policies.NachaGenerate)]
    public Task<IActionResult> Reprocess(Guid id, [FromBody] ReprocessResponseRequest request, CancellationToken ct)
        => Execute(async () => Accepted(await _service.RequestReprocessAsync(id,
            new ReprocessCommand(request.CommandId, request.ExpectedVersion, request.Reason,
                Correlation(request.CorrelationId)), Actor(), ct)));

    [HttpGet("api/ach/responses/{id:guid}/reprocess-attempts")]
    [Authorize(Policy = P1Policies.NachaRead)]
    public async Task<IActionResult> ReprocessAttempts(Guid id, CancellationToken ct)
        => Ok(await _service.ListReprocessAttemptsAsync(id, ct));

    [HttpGet("api/ach/responses/{id:guid}/reprocess-attempts/{attemptId:long}")]
    [Authorize(Policy = P1Policies.NachaRead)]
    public async Task<IActionResult> ReprocessAttempt(Guid id, long attemptId, CancellationToken ct)
    {
        var item = await _service.GetReprocessAttemptAsync(id, attemptId, ct);
        return item is null ? NotFound(Problem("Intento de reproceso no encontrado.", 404)) : Ok(item);
    }

    private async Task<IActionResult> Execute(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (AchResponseNotFoundException ex) { return NotFound(Problem(ex.Message, 404)); }
        catch (AchResponseConflictException ex)
        {
            var problem = Problem(ex.Message, 409);
            if (ex.CurrentVersion.HasValue) problem.Extensions["currentVersion"] = ex.CurrentVersion.Value;
            return Conflict(problem);
        }
        catch (AchResponseOperationException ex) { return BadRequest(Problem(ex.Message, 400)); }
        catch (ArgumentException ex) { return BadRequest(Problem(ex.Message, 400)); }
        catch (InvalidOperationException ex) { return Conflict(Problem(ex.Message, 409)); }
    }

    private string Actor() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "authenticated-user";
    private string Correlation(string? supplied) => !string.IsNullOrWhiteSpace(supplied)
        ? supplied.Trim()
        : Request.Headers.TryGetValue("X-Correlation-ID", out var header) && !string.IsNullOrWhiteSpace(header)
            ? header.ToString().Trim() : Guid.NewGuid().ToString("N");
    private static ProblemDetails Problem(string detail, int status) => new()
        { Title = status == 409 ? "Conflicto de concurrencia" : "Operación de respuestas ACH", Detail = detail, Status = status };
    private static AchResponseMappingCommand Map(AchResponseMappingWriteRequest x)
        => new(x.ClearingHouseId, x.ResponseType, x.ExternalCode, x.ExternalCause, x.InternalStatusId,
            x.ExternalServiceStatusId, x.InternalStatusName, x.NormalizedCause, x.NormalizedDescription,
            x.RequiresCause, x.AllowsNotification, x.Priority, x.EffectiveFrom, x.EffectiveTo,
            x.IsActive, x.ExpectedVersion, x.Reason);
}
