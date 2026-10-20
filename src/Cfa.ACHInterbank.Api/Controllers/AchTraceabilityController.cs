using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/ach-traceability")]
[Authorize]
public class AchTraceabilityController : ControllerBase
{
    private readonly IAchTraceabilityService _traceabilityService;

    public AchTraceabilityController(IAchTraceabilityService traceabilityService)
    {
        _traceabilityService = traceabilityService;
    }

    /// <summary>
    /// Certifica una transacción mediante la transición SOL02 implementada por el servicio de trazabilidad.
    /// </summary>
    /// <param name="transactionId">Identificador interno de la transacción que se desea certificar.</param>
    /// <param name="request">Referencia y notas opcionales que se almacenan en el payload del evento de estado.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Certificar transacción con SOL02")]
    [EndpointDescription("Mueve la transacción indicada al estado Certified mediante una transición con fuente Claims y código de razón SOL02. CertificationReference y Notes son opcionales y se conservan en el payload del evento de estado. Devuelve 404 si la transacción no existe y 400 cuando la transición a Certified no está permitida desde su estado actual.")]
    [HttpPost("sol02/{transactionId:int}/certify")]
    [Authorize(Policy = P0Policies.TraceabilityCertifySol02)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Certificación SOL02 aplicada; la respuesta informa el identificador, estado y fecha del cambio de estado.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La transición de la transacción al estado Certified no está permitida desde su estado actual.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una transacción con el identificador indicado.")]
    public async Task<IActionResult> CertifyWithSol02(
        int transactionId,
        [FromBody] Sol02CertificationRequest request,
        CancellationToken ct)
    {
        try
        {
            var transaction = await _traceabilityService.CertifySol02Async(
                transactionId,
                request.CertificationReference,
                request.Notes,
                ct);

            return Ok(new
            {
                message = "Certificación SOL02 aplicada.",
                transactionId = transaction.Id,
                transaction.State,
                transaction.StateChangedAtUtc
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Consulta la trazabilidad persistida de una transacción ACHInterbank.
    /// </summary>
    /// <param name="transactionId">Identificador interno de la transacción consultada.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar trazabilidad de una transacción")]
    [EndpointDescription("Devuelve los datos operativos de la transacción, su ciclo y cámara, instituciones de origen y destino, estado actual, el archivo NACHA más reciente del ciclo cuando exista, el último archivo de devolución asociado y los eventos de cambio de estado ordenados por fecha de creación. Devuelve 404 si la transacción no existe.")]
    [HttpGet("transactions/{transactionId:int}")]
    [Authorize(Policy = P0Policies.TraceabilityRead)]
    [ProducesResponseType(typeof(AchTraceabilityDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Detalle de trazabilidad de la transacción y su historial de eventos de estado.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una transacción con el identificador indicado.")]
    public async Task<IActionResult> GetTransactionTraceability(int transactionId, CancellationToken ct)
    {
        var traceability = await _traceabilityService.GetTransactionTraceabilityAsync(transactionId, ct);
        if (traceability is null)
        {
            return NotFound(new { message = $"No existe la transacción ACH {transactionId}." });
        }

        return Ok(traceability);
    }

    /// <summary>
    /// Consulta un reporte de trazabilidad usando filtros opcionales sobre el estado persistido de las transacciones.
    /// </summary>
    /// <param name="fromUtc">Límite inferior inclusivo de StateChangedAtUtc. El servicio normaliza el valor mediante ToUniversalTime antes de filtrar.</param>
    /// <param name="toUtc">Límite superior inclusivo de StateChangedAtUtc. El servicio normaliza el valor mediante ToUniversalTime antes de filtrar.</param>
    /// <param name="state">Estado actual de la transacción usado como filtro exacto.</param>
    /// <param name="achCycleId">Uno o varios identificadores de ciclo separados por comas. Los valores vacíos se ignoran.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar reporte de trazabilidad")]
    [EndpointDescription("Devuelve filas de trazabilidad ordenadas por StateChangedAtUtc descendente. Permite filtrar de forma opcional e inclusiva por fecha de cambio de estado, por estado actual y por uno o varios identificadores de ciclo separados por comas. Para cada ciclo informa el archivo NACHA más reciente disponible cuando existe.")]
    [HttpGet("report")]
    [Authorize(Policy = P0Policies.TraceabilityRead)]
    [ProducesResponseType(typeof(IReadOnlyList<AchTraceabilityReportRowDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Filas de trazabilidad que cumplen los filtros solicitados, ordenadas por el cambio de estado más reciente.")]
    public async Task<IActionResult> GetTraceabilityReport(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] AchTransferStateEnum? state,
        [FromQuery] string? achCycleId,
        CancellationToken ct)
    {
        var report = await _traceabilityService.GetTraceabilityReportAsync(fromUtc, toUtc, state, achCycleId, ct);
        return Ok(report);
    }
}

/// <summary>
/// Datos opcionales asociados a una certificación SOL02 de una transacción.
/// </summary>
public class Sol02CertificationRequest
{
    /// <summary>
    /// Referencia opcional de certificación que se conserva en el payload JSON del evento de estado.
    /// </summary>
    public string? CertificationReference { get; set; }

    /// <summary>
    /// Notas opcionales que se conservan en el payload JSON del evento de estado.
    /// </summary>
    public string? Notes { get; set; }
}
