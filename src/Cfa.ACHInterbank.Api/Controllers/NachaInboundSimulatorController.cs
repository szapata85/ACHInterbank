using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Authorize(Policy = FineGrainedPermissions.TestTools.Access)]
[Route("api/uat/nacha-inbound-simulator")]
public sealed class NachaInboundSimulatorController : ControllerBase
{
    private readonly INachaInboundSimulationService _service;
    private readonly IAuthorizationService _authorizationService;

    public NachaInboundSimulatorController(
        INachaInboundSimulationService service,
        IAuthorizationService authorizationService)
    {
        _service = service;
        _authorizationService = authorizationService;
    }

    [HttpGet("prenotifications/contract")]
    [Authorize(Policy = P1Policies.NachaSimulatorRead)]
    [EndpointDescription("I01: contrato versionado de resultados de prenotificaciones originadas. Ambos resultados siguen inhabilitados operacionalmente.")]
    [ProducesResponseType(typeof(PrenotificationSimulationContractCapabilitiesResponse), StatusCodes.Status200OK)]
    public ActionResult<PrenotificationSimulationContractCapabilitiesResponse> PrenotificationContract()
        => Ok(PrenotificationSimulationContract.Describe());

    [HttpPost("prenotifications/validate-contract")]
    [Authorize(Policy = P1Policies.NachaSimulatorRead)]
    [EndpointDescription("I01: valida solo la forma del contrato. ShapeValid NO significa elegible; no consulta BD, no genera archivos ni cambia estados.")]
    [ProducesResponseType(typeof(PrenotificationSimulationContractValidationResponse), StatusCodes.Status200OK)]
    public ActionResult<PrenotificationSimulationContractValidationResponse> ValidatePrenotificationContract(
        [FromBody] PrenotificationSimulationContractRequest request)
        => Ok(PrenotificationSimulationContract.Validate(request));

    [HttpGet("prenotifications/originated")]
    [Authorize(Policy = P1Policies.NachaSimulatorRead)]
    [EndpointDescription("I02: prenotificaciones realmente originadas, con evidencia de transmision, intento y motivos de no elegibilidad. Solo lectura.")]
    [ProducesResponseType(typeof(PrenotificationCandidatePage), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PrenotificationCandidatePage>> OriginatedPrenotifications(
        [FromQuery] PrenotificationCandidateQuery query, CancellationToken ct)
    {
        try { return Ok(await _service.ListOriginatedPrenotificationsAsync(query, ct)); }
        catch (ArgumentException ex) { return BadRequest(ToProblem(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ToProblem(ex.Message)); }
    }

    [HttpPost("prenotifications/eligibility-preview")]
    [Authorize(Policy = P1Policies.NachaSimulatorRead)]
    [EndpointDescription("I02: preflight de camara, banco, ciclo, transmision, intento, duplicados y plazo. BusinessEligible no habilita ejecucion ni reserva idempotencia.")]
    [ProducesResponseType(typeof(PrenotificationBusinessEligibilityPreviewResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PrenotificationBusinessEligibilityPreviewResponse>> PrenotificationEligibilityPreview(
        [FromBody] PrenotificationSimulationContractRequest request, CancellationToken ct)
        => Ok(await _service.PreviewPrenotificationAsync(request, ct));

    
    [HttpPost("prenotifications/tacit-acceptance/preview")]
    [Authorize(Policy = P1Policies.NachaSimulatorRead)]
    [EndpointDescription("I05: evalua sin efectos el vencimiento con reloj virtual opcional. No modifica transacciones, Quartz ni crea archivos NACHA-M.")]
    [ProducesResponseType(typeof(PrenotificationTacitAcceptancePreviewResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PrenotificationTacitAcceptancePreviewResponse>> PreviewTacitAcceptance(
        [FromBody] PrenotificationTacitAcceptancePreviewRequest request, CancellationToken ct)
        => Ok(await _service.PreviewTacitAcceptanceAsync(request, ct));

    [HttpPost("prenotifications/tacit-acceptance/apply-due")]
    [Authorize(Policy = P1Policies.NachaSimulatorGenerateDifferential)]
    [EndpointDescription("I05: UAT/local exclusivamente. Aplica mediante el motor canonico SOLO vencimientos reales elegibles, sin adelantar fechas ni crear NACHA-M; exige autorizacion de simulacion diferencial.")]
    [ProducesResponseType(typeof(PrenotificationTacitAcceptanceApplyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PrenotificationTacitAcceptanceApplyResponse>> ApplyDueTacitAcceptance(
        [FromBody] PrenotificationSimulationContractRequest request, CancellationToken ct)
    {
        try { return Ok(await _service.ApplyDueTacitAcceptanceAsync(request, User?.Identity?.Name ?? "uat-local", ct)); }
        catch (ArgumentException ex) { return BadRequest(ToProblem(ex.Message)); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(ToProblem(ex.Message)); }
    }

    [HttpPost("prenotifications/generate-return")]
    [Authorize(Policy = P1Policies.NachaSimulatorGenerateDifferential)]
    [EndpointDescription("I03: genera solo un archivo de devolucion NACHA-M de prenotificaciones SALIENTES verificadas. UAT/local, con idempotencia en base de datos, sin autoimportar ni cambiar estados. NoReturnByDeadline no genera archivo (I05).")]
    [ProducesResponseType(typeof(GenerateNachaInboundSimulationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<GenerateNachaInboundSimulationResponse>> GeneratePrenotificationReturn(
        [FromBody] PrenotificationSimulationContractRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _service.GeneratePrenotificationReturnAsync(request, User?.Identity?.Name ?? "uat-local", ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex) { return BadRequest(ToProblem(ex.Message)); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(ToProblem(ex.Message)); }
    }

    [HttpPost("generate")]
    [EndpointDescription("Genera un archivo NACHA-M de entrada simulado para UAT/local. El simulador solo genera y permite descargar el archivo: no llama NachaUpload, no importa automaticamente, no crea transacciones de entrada, no cambia estados y no transmite a camaras externas. El usuario debe cargar manualmente el archivo generado por la SPA mediante el flujo real NachaUpload. Ejemplos vigentes: ACH Colombia IncomingCredit, CENIT IncomingDebit, ACH Colombia IncomingCreditReturn. El escenario legado continua bloqueado; IncomingPrenotificationReturn se genera exclusivamente por prenotifications/generate-return bajo contrato I03. Errores esperados: 400 request invalido; 401/403 autenticacion/autorizacion; 422 simulador deshabilitado, regla no configurada, referencia inexistente o causal requerida; 500 error no controlado.")]
    [ProducesResponseType(typeof(GenerateNachaInboundSimulationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GenerateNachaInboundSimulationResponse>> Generate([FromBody] GenerateNachaInboundSimulationRequest request, CancellationToken ct)
    {
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
        return Ok(await _service.ListAsync(ct));
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
        try
        {
            var file = await _service.GetFileAsync(id, ct);
            return file is null ? NotFound() : File(file.Value.Content, file.Value.ContentType, file.Value.FileName);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(ToProblem(ex.Message));
        }
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
        return Ok(await _service.PreviewAsync(request, ct));
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

    [HttpGet("available-cycles")]
    [Authorize(Policy = P1Policies.NachaSimulatorRead)]
    [ProducesResponseType(typeof(IReadOnlyList<AvailableInboundCycleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AvailableInboundCycleDto>>> AvailableCycles(
        [FromQuery] AvailableInboundCycleQuery query,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.ClearingHouseCode))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "CLEARING_HOUSE_REQUIRED",
                Detail = "Debe seleccionar una cámara.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(await _service.ListAvailableCyclesAsync(query, ct));
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
