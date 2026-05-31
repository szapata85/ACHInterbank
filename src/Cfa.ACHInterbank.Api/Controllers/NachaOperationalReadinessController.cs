using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/ach/nacha/operational")]
[Authorize]
public sealed class NachaOperationalReadinessController : ControllerBase
{
    private readonly INachaOperationalReadModelService _service;

    public NachaOperationalReadinessController(INachaOperationalReadModelService service)
    {
        _service = service;
    }

    [HttpGet("dashboard")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(NachaOperationalDashboardReadModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        => Ok(await _service.GetDashboardAsync(cancellationToken));

    [HttpGet("summary")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(NachaOperationalSummaryReadModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
        => Ok(await _service.GetSummaryAsync(cancellationToken));

    [HttpGet("files")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(IReadOnlyList<NachaOperationalFileReadModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFiles(CancellationToken cancellationToken)
        => Ok(await _service.GetFilesAsync(cancellationToken));

    [HttpGet("files/{fileId}")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(NachaOperationalFileDetailReadModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFileDetail(string fileId, CancellationToken cancellationToken)
    {
        var detail = await _service.GetFileDetailAsync(fileId, cancellationToken);
        return detail is null
            ? NotFound(new { errorCode = "NACHA_FILE_NOT_FOUND", message = "Archivo NACHA-M no encontrado o no persistido." })
            : Ok(detail);
    }

    [HttpGet("decisions")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(IReadOnlyList<NachaOperationalDecisionReadModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDecisions(CancellationToken cancellationToken)
        => Ok(await _service.GetDecisionsAsync(cancellationToken));

    [HttpGet("soap-readiness")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(IReadOnlyList<NachaSoapReadinessReadModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSoapReadiness(CancellationToken cancellationToken)
        => Ok(await _service.GetSoapReadinessAsync(cancellationToken));

    [HttpGet("audit")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(IReadOnlyList<NachaOperationalAuditReadModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAudit(CancellationToken cancellationToken)
        => Ok(await _service.GetAuditAsync(cancellationToken));
}
