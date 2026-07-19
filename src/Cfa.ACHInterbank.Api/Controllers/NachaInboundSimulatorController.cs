using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/uat/nacha-inbound-simulator")]
public sealed class NachaInboundSimulatorController : ControllerBase
{
    private readonly INachaInboundSimulationService _service;
    private readonly NachaInboundSimulatorOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly IAuthorizationService _authorizationService;

    public NachaInboundSimulatorController(
        INachaInboundSimulationService service,
        IOptions<NachaInboundSimulatorOptions> options,
        IHostEnvironment environment,
        IAuthorizationService authorizationService)
    {
        _service = service;
        _options = options.Value;
        _environment = environment;
        _authorizationService = authorizationService;
    }

    [HttpPost("generate")]
    [EndpointDescription("Genera un archivo NACHA-M de entrada simulado para UAT/local. El simulador solo genera y permite descargar el archivo: no llama NachaUpload, no importa automaticamente, no crea transacciones de entrada, no cambia estados y no transmite a camaras externas. El usuario debe cargar manualmente el archivo generado por la SPA mediante el flujo real NachaUpload. Ejemplos: ACH Colombia IncomingCredit, CENIT IncomingPrenotificationResponse, ACH Colombia IncomingCreditReturn. Errores esperados: 400 request invalido; 401/403 autenticacion/autorizacion; 422 simulador deshabilitado, regla no configurada, referencia inexistente o causal requerida; 500 error no controlado.")]
    [ProducesResponseType(typeof(GenerateNachaInboundSimulationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GenerateNachaInboundSimulationResponse>> Generate([FromBody] GenerateNachaInboundSimulationRequest request, CancellationToken ct)
    {
        var unavailable = UnavailableOutsideAuthorizedEnvironment();
        if (unavailable is not null)
        {
            return unavailable;
        }

        var requiredPolicy = request.SimulationMode == NachaSimulationMode.DifferentialResponses
            ? P1Policies.NachaSimulatorGenerateDifferential
            : P1Policies.NachaSimulatorGenerateIncoming;
        if (!(await _authorizationService.AuthorizeAsync(User, requiredPolicy)).Succeeded)
        {
            return Forbid();
        }

        try
        {
            var result = await _service.GenerateAsync(request, User?.Identity?.Name ?? "uat-local", ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(ToProblem(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ToProblem(ex.Message));
        }
    }

    [HttpGet]
    [Authorize(Policy = P1Policies.NachaSimulatorRead)]
    [EndpointDescription("Lista simulaciones NACHA-M de entrada generadas para UAT/local. Consulta solo metadata; no importa archivos ni modifica estados.")]
    [ProducesResponseType(typeof(IReadOnlyList<NachaInboundSimulationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<NachaInboundSimulationDto>>> List(CancellationToken ct)
    {
        var unavailable = UnavailableOutsideAuthorizedEnvironment();
        return unavailable ?? Ok(await _service.ListAsync(ct));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = P1Policies.NachaSimulatorRead)]
    [EndpointDescription("Obtiene una simulacion NACHA-M de entrada por id. Consulta read-only; no importa archivos ni cambia estados.")]
    [ProducesResponseType(typeof(NachaInboundSimulationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NachaInboundSimulationDto>> GetById(int id, CancellationToken ct)
    {
        var unavailable = UnavailableOutsideAuthorizedEnvironment();
        if (unavailable is not null)
        {
            return unavailable;
        }

        var simulation = await _service.GetAsync(id, ct);
        return simulation is null ? NotFound() : Ok(simulation);
    }

    [HttpGet("{id:int}/file")]
    [Authorize(Policy = P1Policies.NachaSimulatorDownload)]
    [EndpointDescription("Descarga el archivo NACHA-M simulado. La descarga no lo importa ni llama NachaUpload; el usuario debe cargarlo manualmente en la SPA.")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFile(int id, CancellationToken ct)
    {
        var unavailable = UnavailableOutsideAuthorizedEnvironment();
        if (unavailable is not null)
        {
            return unavailable;
        }

        var file = await _service.GetFileAsync(id, ct);
        return file is null ? NotFound() : File(file.Value.Content, file.Value.ContentType, file.Value.FileName);
    }

    [HttpGet("{id:int}/evidence")]
    [Authorize(Policy = P1Policies.NachaSimulatorRead)]
    [EndpointDescription("Devuelve metadata/evidencia de la simulacion: hash SHA256, conteos, generatedOnly=true, autoImported=false, uploadRequired=true y externalTransmission=false.")]
    [ProducesResponseType(typeof(NachaInboundSimulationMetadataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NachaInboundSimulationMetadataDto>> Evidence(int id, CancellationToken ct)
    {
        var unavailable = UnavailableOutsideAuthorizedEnvironment();
        if (unavailable is not null)
        {
            return unavailable;
        }

        var evidence = await _service.GetEvidenceAsync(id, ct);
        return evidence is null ? NotFound() : Ok(evidence);
    }

    [HttpPost("eligibility-preview")]
    [Authorize(Policy = P1Policies.NachaSimulatorRead)]
    [EndpointDescription("Evalua si una solicitud del simulador NACHA-M de entrada es elegible. No genera archivo, no importa, no crea transacciones y no cambia estados.")]
    [ProducesResponseType(typeof(InboundSimulationEligibilityPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InboundSimulationEligibilityPreviewResponse>> Preview([FromBody] InboundSimulationEligibilityPreviewRequest request, CancellationToken ct)
    {
        var unavailable = UnavailableOutsideAuthorizedEnvironment();
        return unavailable ?? Ok(await _service.PreviewAsync(request, ct));
    }

    [HttpGet("eligible-differential-transactions")]
    [Authorize(Policy = P1Policies.NachaSimulatorRead)]
    [ProducesResponseType(typeof(DifferentialResponseTransactionPage), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DifferentialResponseTransactionPage>> EligibleDifferentialTransactions(
        [FromQuery] DifferentialResponseTransactionQuery query,
        CancellationToken ct)
    {
        var unavailable = UnavailableOutsideAuthorizedEnvironment();
        if (unavailable is not null)
        {
            return unavailable;
        }

        if (string.IsNullOrWhiteSpace(query.ClearingHouseCode))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "CLEARING_HOUSE_REQUIRED",
                Detail = "Debe seleccionar una cámara.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(await _service.ListEligibleDifferentialTransactionsAsync(query, ct));
    }

    private ActionResult? UnavailableOutsideAuthorizedEnvironment()
    {
        if (!_environment.IsProduction() && _options.IsUatLike())
        {
            return null;
        }

        return NotFound(new ProblemDetails
        {
            Title = "UAT_FEATURE_UNAVAILABLE",
            Detail = "El simulador NACHA-M no está disponible en este ambiente.",
            Status = StatusCodes.Status404NotFound
        });
    }

    private static ProblemDetails ToProblem(string message)
    {
        var split = message.Split(':', 2);
        return new ProblemDetails
        {
            Title = split.Length == 2 ? split[0] : "SIMULATOR_ERROR",
            Detail = split.Length == 2 ? split[1].Trim() : message,
            Status = StatusCodes.Status422UnprocessableEntity
        };
    }
}
