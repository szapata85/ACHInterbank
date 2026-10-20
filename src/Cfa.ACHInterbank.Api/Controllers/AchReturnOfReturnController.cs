using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("ach-returns/return-of-return")]
[Authorize]
public class AchReturnOfReturnController(
    IAchReturnOfReturnEligibilityService eligibilityService,
    IAchReturnOfReturnFileGenerationService generationService,
    ICenitReturnOfReturnService? cenitReturnOfReturnService = null) : ControllerBase
{
    /// <summary>
    /// Registra una devolución de devolución saliente para un retorno CENIT previamente identificado.
    /// </summary>
    /// <param name="request">Identificador del evento de devolución recibido, causal, ciclo de retorno y metadatos opcionales del solicitante.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Crear devolución de devolución saliente CENIT")]
    [EndpointDescription("Valida que ParentIncomingReturnStateEventId sea mayor que cero y que ReasonCode y ReturnCycleId tengan valor. Si el servicio CENIT de devolución de devolución está disponible, delega la creación y responde 200 cuando el resultado es exitoso; un resultado no exitoso o la ausencia del servicio se responde como 409.")]
    [HttpPost("cenit/out")]
    [Authorize(Policy = P0Policies.ReturnsGenerateFile)]
    [ProducesResponseType(typeof(CenitReturnOfReturnResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Devolución de devolución saliente creada con resultado exitoso.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "Faltan datos obligatorios para solicitar la devolución de devolución.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "El servicio CENIT de devolución de devolución no está disponible o la creación no terminó exitosamente.")]
    public async Task<IActionResult> CreateCenitOut([FromBody] CreateCenitReturnOfReturnOutRequest request, CancellationToken ct)
    {
        if (request is null || request.ParentIncomingReturnStateEventId <= 0 || string.IsNullOrWhiteSpace(request.ReasonCode) || string.IsNullOrWhiteSpace(request.ReturnCycleId))
            return BadRequest(new { message = "parentIncomingReturnStateEventId, reasonCode y returnCycleId son obligatorios." });
        if (cenitReturnOfReturnService is null)
            return Conflict(new { message = "Servicio CENIT ROR no disponible." });

        var result = await cenitReturnOfReturnService.CreateOutgoingAsync(new(
            request.ParentIncomingReturnStateEventId,
            request.ReasonCode,
            request.ReturnCycleId,
            DateTime.UtcNow,
            request.RequestedBy,
            request.Source), ct);
        return result.IsSuccessful ? Ok(result) : Conflict(result);
    }
    /// <summary>
    /// Evalúa la elegibilidad de una devolución de devolución.
    /// </summary>
    /// <param name="request">Transacción de devolución origen, nueva causal y metadatos opcionales del solicitante.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Evaluar devolución de devolución")]
    [EndpointDescription("Requiere un SourceReturnTransactionId mayor que cero y una NewReturnReasonCode no vacía. La evaluación siempre devuelve 200 cuando la solicitud es estructuralmente válida; IsEligible y Failures expresan el resultado funcional sin convertir una ineligibilidad en error HTTP.")]
    [HttpPost("evaluate")]
    [Authorize(Policy = P0Policies.ReturnsRead)]
    [ProducesResponseType(typeof(AchReturnOfReturnEligibilityResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Resultado de elegibilidad de la devolución de devolución, incluyendo fallas funcionales cuando no es elegible.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La solicitud no contiene una transacción origen válida o una nueva causal.")]
    public async Task<IActionResult> Evaluate([FromBody] EvaluateReturnOfReturnRequest request, CancellationToken ct)
    {
        if (request is null || request.SourceReturnTransactionId <= 0 || string.IsNullOrWhiteSpace(request.NewReturnReasonCode))
        {
            return BadRequest(new { message = "sourceReturnTransactionId y newReturnReasonCode son obligatorios." });
        }

        var result = await eligibilityService.EvaluateAsync(
            new AchReturnOfReturnEligibilityRequest(
                request.SourceReturnTransactionId,
                request.NewReturnReasonCode,
                DateTime.UtcNow,
                request.RequestedBy,
                request.Source),
            ct);

        return Ok(result);
    }

    /// <summary>
    /// Genera el archivo interno de auditoría para flujos de devolución de devolución.
    /// </summary>
    /// <param name="request">Identificadores de flujo y metadatos opcionales del solicitante.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Generar archivo de auditoría de devolución de devolución")]
    [EndpointDescription("Requiere al menos un FlowId. Delega la generación al servicio de devolución de devolución y devuelve el contenido generado como text/plain. Si el servicio no produce contenido y nombre de archivo válidos, responde 409 con las fallas y los flowIds informados por el resultado de generación.")]
    [HttpPost("generate-audit-file")]
    [Authorize(Policy = P0Policies.ReturnsGenerateFile)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Archivo interno de auditoría generado y devuelto como descarga text/plain.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La solicitud no contiene flowIds para generar el archivo.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "El servicio no pudo generar un archivo de auditoría utilizable para los flujos solicitados.")]
    public async Task<IActionResult> GenerateAuditFile([FromBody] GenerateReturnOfReturnAuditFileRequest request, CancellationToken ct)
    {
        if (request is null || request.FlowIds is null || request.FlowIds.Count == 0)
        {
            return BadRequest(new { message = "Debe enviar al menos un flowId." });
        }

        var result = await generationService.GenerateAsync(
            new AchReturnOfReturnFileGenerationRequest(
                request.FlowIds,
                DateTime.UtcNow,
                request.RequestedBy,
                request.Source),
            ct);

        if (!result.IsGenerated || result.Content is null || string.IsNullOrWhiteSpace(result.FileName))
        {
            return Conflict(new
            {
                message = "No fue posible generar el archivo de auditoría Return Of Return.",
                failures = result.Failures,
                flowIds = result.FlowIds
            });
        }

        return File(result.Content, "text/plain", result.FileName);
    }

    /// <summary>
    /// Genera el archivo NACHA de devolución de devolución para los flujos seleccionados.
    /// </summary>
    /// <param name="request">Identificadores de flujo y metadatos opcionales del solicitante.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Generar archivo NACHA de devolución de devolución")]
    [EndpointDescription("Requiere al menos un FlowId. Delega la generación NACHA al servicio actual y devuelve el contenido generado como text/plain. Si el servicio informa que no se generó archivo, no entrega contenido o no produce nombre de archivo, responde 409 con las fallas y los flowIds del resultado; esta documentación no agrega reglas de cámara distintas de las ejecutadas por el servicio.")]
    [HttpPost("generate-nacha-file")]
    [Authorize(Policy = P0Policies.ReturnsGenerateFile)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Archivo NACHA de devolución de devolución generado y devuelto como descarga text/plain.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La solicitud no contiene flowIds para generar el archivo.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "El servicio no pudo generar un archivo NACHA utilizable para los flujos solicitados.")]
    public async Task<IActionResult> GenerateNachaFile([FromBody] GenerateReturnOfReturnAuditFileRequest request, CancellationToken ct)
    {
        if (request is null || request.FlowIds is null || request.FlowIds.Count == 0)
        {
            return BadRequest(new { message = "Debe enviar al menos un flowId." });
        }

        var result = await generationService.GenerateNachaAsync(
            new AchReturnOfReturnFileGenerationRequest(
                request.FlowIds,
                DateTime.UtcNow,
                request.RequestedBy,
                request.Source),
            ct);

        if (!result.IsGenerated || result.Content is null || string.IsNullOrWhiteSpace(result.FileName))
        {
            return Conflict(new
            {
                message = "No fue posible generar el archivo NACHA de devolución de devolución.",
                failures = result.Failures,
                flowIds = result.FlowIds
            });
        }

        return File(result.Content, "text/plain", result.FileName);
    }
}

/// <summary>Solicitud pública para evaluar una devolución de devolución.</summary>
/// <param name="SourceReturnTransactionId">Identificador interno de la transacción de devolución que origina la evaluación; la acción requiere un valor mayor que cero.</param>
/// <param name="NewReturnReasonCode">Nueva causal propuesta para la devolución de devolución; la acción requiere un valor no vacío.</param>
/// <param name="RequestedBy">Identidad operativa opcional informada por el cliente y trasladada al servicio de elegibilidad.</param>
/// <param name="Source">Origen opcional informado por el cliente y trasladado al servicio de elegibilidad.</param>
public sealed record EvaluateReturnOfReturnRequest(
    int SourceReturnTransactionId,
    string NewReturnReasonCode,
    string? RequestedBy = null,
    string? Source = null);

/// <summary>Solicitud pública para generar archivos a partir de flujos de devolución de devolución.</summary>
/// <param name="FlowIds">Identificadores internos de los flujos a generar; debe contener al menos un elemento.</param>
/// <param name="RequestedBy">Identidad operativa opcional informada por el cliente y trasladada al servicio de generación.</param>
/// <param name="Source">Origen opcional informado por el cliente y trasladado al servicio de generación.</param>
public sealed record GenerateReturnOfReturnAuditFileRequest(
    IReadOnlyCollection<int> FlowIds,
    string? RequestedBy = null,
    string? Source = null);

/// <summary>Solicitud pública para crear una devolución de devolución saliente del flujo CENIT implementado.</summary>
/// <param name="ParentIncomingReturnStateEventId">Identificador del evento de estado de la devolución entrante padre; debe ser mayor que cero.</param>
/// <param name="ReasonCode">Causal solicitada; la acción requiere un valor no vacío.</param>
/// <param name="ReturnCycleId">Identificador del ciclo de retorno; la acción requiere un valor no vacío.</param>
/// <param name="RequestedBy">Identidad operativa opcional informada por el cliente.</param>
/// <param name="Source">Origen opcional informado por el cliente.</param>
public sealed record CreateCenitReturnOfReturnOutRequest(
    long ParentIncomingReturnStateEventId,
    string ReasonCode,
    string ReturnCycleId,
    string? RequestedBy = null,
    string? Source = null);
