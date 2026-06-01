using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/ach/reconciliation")]
[Authorize]
public sealed class AchReconciliationController : ControllerBase
{
    private readonly IAchReconciliationReadModelService _service;

    public AchReconciliationController(IAchReconciliationReadModelService service)
    {
        _service = service;
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
