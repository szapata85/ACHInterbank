using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("incoming-nacha-command-center")]
[Authorize(Policy = "CanReadAch")]
public class IncomingNachaCommandCenterController : ControllerBase
{
    private static IActionResult MapInvalidOperation(InvalidOperationException ex)
        => new ObjectResult(new { message = ex.Message }) { StatusCode = StatusCodes.Status409Conflict };
    private readonly IIncomingNachaCommandCenterService _service;

    public IncomingNachaCommandCenterController(IIncomingNachaCommandCenterService service)
    {
        _service = service;
    }

    [EndpointSummary("GET observability/summary: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'observability/summary'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
    [HttpGet("observability/summary")]
    public async Task<IActionResult> GetObservabilitySummary([FromQuery] int windowHours = 24, CancellationToken ct = default)
        => Ok(await _service.GetObservabilitySummaryAsync(windowHours, ct));

    [EndpointSummary("GET ingestions: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'ingestions'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
    [HttpGet("ingestions")]
    public async Task<IActionResult> GetIngestions([FromQuery] IncomingNachaIngestionQuery query, CancellationToken ct)
        => Ok(await _service.GetIngestionsAsync(query, ct));

    [EndpointSummary("GET ingestions/{ingestionId:guid}: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'ingestions/{ingestionId:guid}'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
    [HttpGet("ingestions/{ingestionId:guid}")]
    public async Task<IActionResult> GetIngestionDetail(Guid ingestionId, CancellationToken ct)
    {
        var result = await _service.GetIngestionDetailAsync(ingestionId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [EndpointSummary("GET queue: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'queue'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
    [HttpGet("queue")]
    public async Task<IActionResult> GetQueue([FromQuery] IncomingNachaQueueQuery query, CancellationToken ct)
        => Ok(await _service.GetQueueAsync(query, ct));

    [EndpointSummary("GET queue/{queueId:guid}: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'queue/{queueId:guid}'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
    [HttpGet("queue/{queueId:guid}")]
    public async Task<IActionResult> GetQueueDetail(Guid queueId, CancellationToken ct)
    {
        var result = await _service.GetQueueDetailAsync(queueId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [EndpointSummary("POST queue/{queueId:guid}/retry: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'queue/{queueId:guid}/retry'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: modifica información. Genera auditoría: sí, mediante los servicios de operación/auditoría cuando aplica al flujo.")]
    [HttpPost("queue/{queueId:guid}/retry")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> RetryManual(Guid queueId, [FromBody] IncomingNachaManualActionRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.RetryManualAsync(queueId, request, User?.Identity?.Name ?? "ops.user", ct));
        }
        catch (InvalidOperationException ex)
        {
            return MapInvalidOperation(ex);
        }
    }

    [EndpointSummary("POST queue/{queueId:guid}/unblock: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'queue/{queueId:guid}/unblock'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: modifica información. Genera auditoría: sí, mediante los servicios de operación/auditoría cuando aplica al flujo.")]
    [HttpPost("queue/{queueId:guid}/unblock")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> UnblockManual(Guid queueId, [FromBody] IncomingNachaManualActionRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.UnblockManualAsync(queueId, request, User?.Identity?.Name ?? "ops.user", ct));
        }
        catch (InvalidOperationException ex)
        {
            return MapInvalidOperation(ex);
        }
    }

    [EndpointSummary("POST queue/{queueId:guid}/requeue: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'queue/{queueId:guid}/requeue'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: modifica información. Genera auditoría: sí, mediante los servicios de operación/auditoría cuando aplica al flujo.")]
    [HttpPost("queue/{queueId:guid}/requeue")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> RequeueManual(Guid queueId, [FromBody] IncomingNachaManualActionRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.RequeueManualAsync(queueId, request, User?.Identity?.Name ?? "ops.user", ct));
        }
        catch (InvalidOperationException ex)
        {
            return MapInvalidOperation(ex);
        }
    }

    [EndpointSummary("POST queue/{queueId:guid}/mark-failed-final: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'queue/{queueId:guid}/mark-failed-final'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: modifica información. Genera auditoría: sí, mediante los servicios de operación/auditoría cuando aplica al flujo.")]
    [HttpPost("queue/{queueId:guid}/mark-failed-final")]
    [Authorize(Policy = "CanManageAch")]
    public async Task<IActionResult> MarkFailedFinal(Guid queueId, [FromBody] IncomingNachaManualActionRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.MarkFailedFinalManualAsync(queueId, request, User?.Identity?.Name ?? "ops.user", ct));
        }
        catch (InvalidOperationException ex)
        {
            return MapInvalidOperation(ex);
        }
    }
}
