using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/ach/nacha/soap-uat-console")]
[Authorize]
public sealed class NachaSoapUatConsoleController : ControllerBase
{
    private readonly INachaSoapUatConsoleReadModelService _service;

    public NachaSoapUatConsoleController(INachaSoapUatConsoleReadModelService service)
    {
        _service = service;
    }

    [HttpGet("dashboard")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(NachaSoapUatConsoleDashboardReadModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        => Ok(await _service.GetDashboardAsync(cancellationToken));

    [HttpGet("candidates")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(IReadOnlyList<NachaSoapUatCandidateReadModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCandidates(CancellationToken cancellationToken)
        => Ok(await _service.GetCandidatesAsync(cancellationToken));

    [HttpGet("candidates/{correlationId}")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(NachaSoapUatCandidateReadModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCandidate(string correlationId, CancellationToken cancellationToken)
    {
        var candidate = await _service.GetCandidateAsync(correlationId, cancellationToken);
        return candidate is null
            ? NotFound(new { errorCode = "SOAP_UAT_CANDIDATE_NOT_FOUND", message = "Candidato SOAP/UAT no encontrado." })
            : Ok(candidate);
    }

    [HttpGet("audit")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(IReadOnlyList<NachaSoapUatAuditReadModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAudit(CancellationToken cancellationToken)
        => Ok(await _service.GetAuditAsync(cancellationToken));
}
