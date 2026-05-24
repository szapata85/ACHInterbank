using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/uat/nacha-inbound-simulator")]
public sealed class NachaInboundSimulatorController : ControllerBase
{
    private readonly INachaInboundSimulationService _service;

    public NachaInboundSimulatorController(INachaInboundSimulationService service)
    {
        _service = service;
    }

    [HttpPost("generate")]
    [Authorize(Policy = P1Policies.NachaGenerate)]
    [EndpointDescription("Genera un archivo NACHA-M de entrada simulado para UAT/local. El simulador solo genera y permite descargar el archivo: no llama NachaUpload, no importa automaticamente, no crea transacciones de entrada, no cambia estados y no transmite a camaras externas. El usuario debe cargar manualmente el archivo generado por la SPA mediante el flujo real NachaUpload. Ejemplos: ACH Colombia IncomingCredit, CENIT IncomingPrenotificationResponse, ACH Colombia IncomingCreditReturn. Errores esperados: 400 request invalido; 401/403 autenticacion/autorizacion; 422 simulador deshabilitado, regla no configurada, referencia inexistente o causal requerida; 500 error no controlado.")]
    [ProducesResponseType(typeof(GenerateNachaInboundSimulationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GenerateNachaInboundSimulationResponse>> Generate([FromBody] GenerateNachaInboundSimulationRequest request, CancellationToken ct)
    {
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
    [Authorize(Policy = P1Policies.NachaRead)]
    [EndpointDescription("Lista simulaciones NACHA-M de entrada generadas para UAT/local. Consulta solo metadata; no importa archivos ni modifica estados.")]
    [ProducesResponseType(typeof(IReadOnlyList<NachaInboundSimulationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<NachaInboundSimulationDto>>> List(CancellationToken ct)
        => Ok(await _service.ListAsync(ct));

    [HttpGet("{id:int}")]
    [Authorize(Policy = P1Policies.NachaRead)]
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
    [Authorize(Policy = P1Policies.NachaRead)]
    [EndpointDescription("Descarga el archivo NACHA-M simulado. La descarga no lo importa ni llama NachaUpload; el usuario debe cargarlo manualmente en la SPA.")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFile(int id, CancellationToken ct)
    {
        var file = await _service.GetFileAsync(id, ct);
        return file is null ? NotFound() : File(file.Value.Content, file.Value.ContentType, file.Value.FileName);
    }

    [HttpGet("{id:int}/evidence")]
    [Authorize(Policy = P1Policies.NachaRead)]
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
    [Authorize(Policy = P1Policies.NachaRead)]
    [EndpointDescription("Evalua si una solicitud del simulador NACHA-M de entrada es elegible. No genera archivo, no importa, no crea transacciones y no cambia estados.")]
    [ProducesResponseType(typeof(InboundSimulationEligibilityPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InboundSimulationEligibilityPreviewResponse>> Preview([FromBody] InboundSimulationEligibilityPreviewRequest request, CancellationToken ct)
        => Ok(await _service.PreviewAsync(request, ct));

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
