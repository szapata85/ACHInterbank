using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/ach/nacha/soap-uat-console")]
[Authorize]
public sealed class NachaSoapUatConsoleController : ControllerBase
{
    private readonly INachaSoapUatConsoleReadModelService _service;
    private readonly NachaInboundSimulatorOptions _options;
    private readonly IHostEnvironment _environment;

    public NachaSoapUatConsoleController(
        INachaSoapUatConsoleReadModelService service,
        IOptions<NachaInboundSimulatorOptions> options,
        IHostEnvironment environment)
    {
        _service = service;
        _options = options.Value;
        _environment = environment;
    }

    [HttpGet("dashboard")]
    [Authorize(Policy = P1Policies.NachaSimulatorRead)]
    [ProducesResponseType(typeof(NachaSoapUatConsoleDashboardReadModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        => IsAvailable()
            ? Ok(await _service.GetDashboardAsync(cancellationToken))
            : FeatureNotFound();

    [HttpGet("candidates")]
    [Authorize(Policy = P1Policies.NachaSimulatorRead)]
    [ProducesResponseType(typeof(IReadOnlyList<NachaSoapUatCandidateReadModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCandidates(CancellationToken cancellationToken)
        => IsAvailable()
            ? Ok(await _service.GetCandidatesAsync(cancellationToken))
            : FeatureNotFound();

    [HttpGet("candidates/{correlationId}")]
    [Authorize(Policy = P1Policies.NachaSimulatorRead)]
    [ProducesResponseType(typeof(NachaSoapUatCandidateReadModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCandidate(string correlationId, CancellationToken cancellationToken)
    {
        if (!IsAvailable())
        {
            return FeatureNotFound();
        }

        var candidate = await _service.GetCandidateAsync(correlationId, cancellationToken);
        return candidate is null
            ? NotFound(new { errorCode = "SOAP_UAT_CANDIDATE_NOT_FOUND", message = "Candidato SOAP/UAT no encontrado." })
            : Ok(candidate);
    }

    [HttpGet("audit")]
    [Authorize(Policy = P1Policies.NachaSimulatorRead)]
    [ProducesResponseType(typeof(IReadOnlyList<NachaSoapUatAuditReadModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAudit(CancellationToken cancellationToken)
        => IsAvailable()
            ? Ok(await _service.GetAuditAsync(cancellationToken))
            : FeatureNotFound();

    private bool IsAvailable()
        => !_environment.IsProduction() && _options.IsUatLike();

    private static NotFoundObjectResult FeatureNotFound()
        => new(new ProblemDetails
        {
            Title = "UAT_FEATURE_UNAVAILABLE",
            Detail = "La consola SOAP/UAT no está disponible en este ambiente.",
            Status = StatusCodes.Status404NotFound
        });
}
