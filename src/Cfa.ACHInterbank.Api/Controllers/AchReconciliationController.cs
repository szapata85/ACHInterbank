using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Api.Contracts.AchResponses;
using Cfa.ACHInterbank.Application.ACH.Responses.Operations;
using Cfa.ACHInterbank.Application.Security;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/ach/reconciliation")]
[Authorize]
public sealed class AchReconciliationController : ControllerBase
{
    private readonly IAchReconciliationReadModelService _service;
    private readonly IAchResponseOperationsService? _operations;

    public AchReconciliationController(IAchReconciliationReadModelService service, IAchResponseOperationsService? operations = null)
    {
        _service = service;
        _operations = operations;
    }

    [HttpGet("exceptions")]
    [Authorize(Policy = P1Policies.NachaRead)]
    public async Task<IActionResult> GetExceptions([FromQuery] int? clearingHouseId, [FromQuery] string? status,
        CancellationToken cancellationToken)
        => _operations is null ? StatusCode(501) : Ok(await _operations.ListReconciliationCasesAsync(clearingHouseId, status, cancellationToken));

    [HttpGet("exceptions/{id:guid}")]
    [Authorize(Policy = P1Policies.NachaRead)]
    public async Task<IActionResult> GetException(Guid id, CancellationToken cancellationToken)
    {
        if (_operations is null) return StatusCode(501);
        var item = (await _operations.ListReconciliationCasesAsync(null, null, cancellationToken)).SingleOrDefault(x => x.Id == id);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("exceptions/{id:guid}/resolve")]
    [Authorize(Policy = P1Policies.NachaGenerate)]
    public async Task<IActionResult> ResolveException(Guid id, [FromBody] ResolveReconciliationRequest request,
        CancellationToken cancellationToken)
    {
        if (_operations is null) return StatusCode(501);
        try
        {
            var correlation = string.IsNullOrWhiteSpace(request.CorrelationId) ? Guid.NewGuid().ToString("N") : request.CorrelationId.Trim();
            var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "authenticated-user";
            return Ok(await _operations.ResolveReconciliationCaseAsync(id,
                new ReconciliationResolutionCommand(request.ExpectedVersion, request.Resolution, request.Reason, correlation), actor, cancellationToken));
        }
        catch (AchResponseNotFoundException ex) { return NotFound(new ProblemDetails { Status = 404, Title = "Conciliación", Detail = ex.Message }); }
        catch (AchResponseConflictException ex)
        {
            var problem = new ProblemDetails { Status = 409, Title = "Conflicto de concurrencia", Detail = ex.Message };
            if (ex.CurrentVersion.HasValue) problem.Extensions["currentVersion"] = ex.CurrentVersion.Value;
            return Conflict(problem);
        }
        catch (AchResponseOperationException ex) { return BadRequest(new ProblemDetails { Status = 400, Title = "Conciliación", Detail = ex.Message }); }
    }

    [HttpGet("dashboard")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(AchReconciliationDashboardReadModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        => Ok(await _service.GetDashboardAsync(cancellationToken));

    [HttpGet("items")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(IReadOnlyList<AchReconciliationItemReadModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetItems(CancellationToken cancellationToken)
        => Ok(await _service.GetItemsAsync(cancellationToken));

    [HttpGet("items/{reconciliationId}")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(AchReconciliationDetailReadModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetItem(string reconciliationId, CancellationToken cancellationToken)
    {
        var detail = await _service.GetItemAsync(reconciliationId, cancellationToken);
        return detail is null
            ? NotFound(new { errorCode = "ACH_RECONCILIATION_ITEM_NOT_FOUND", message = "Item de conciliacion no encontrado." })
            : Ok(detail);
    }

    [HttpGet("items/by-correlation/{correlationId}")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(AchReconciliationDetailReadModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetItemByCorrelation(string correlationId, CancellationToken cancellationToken)
    {
        var detail = await _service.GetItemByCorrelationAsync(correlationId, cancellationToken);
        return detail is null
            ? NotFound(new { errorCode = "ACH_RECONCILIATION_CORRELATION_NOT_FOUND", message = "CorrelationId no encontrado." })
            : Ok(detail);
    }
}
