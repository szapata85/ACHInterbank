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

    [EndpointSummary("Resumen operativo de ingestión NACHA entrante")]
    [EndpointDescription("Qué hace: consolida indicadores de observabilidad de ingestas, cola, reintentos y fallas finales en una ventana horaria. Cuándo se usa: en monitoreo operativo y mesas de soporte para detectar degradación. Perfil consumidor: operación ACH, NOC y soporte productivo. Permiso requerido: CanReadAch. Tipo de operación: solo consulta. Genera auditoría: sí, de forma indirecta por trazas de acceso y eventos previos del flujo. Riesgos operativos: tomar decisiones con una ventana horaria inadecuada puede ocultar incidentes. Errores esperados: 400 por parámetros inválidos; 401/403 por autorización. Relación ACH/CENIT/NACHA-M: supervisa el tramo NACHA-M entrante antes de su enrutamiento CENIT/ACH. Precauciones para desarrollo u operación: validar windowHours y correlacionar con bitácora antes de ejecutar acciones manuales.")]
    [HttpGet("observability/summary")]
    public async Task<IActionResult> GetObservabilitySummary([FromQuery] int windowHours = 24, CancellationToken ct = default)
        => Ok(await _service.GetObservabilitySummaryAsync(windowHours, ct));

    [EndpointSummary("Consulta paginada de ingestas NACHA entrantes")]
    [EndpointDescription("Qué hace: lista ingestas con filtros del command center. Cuándo se usa: al analizar volumen, estados y tiempos de procesamiento. Perfil consumidor: operación ACH y analistas de soporte. Permiso requerido: CanReadAch. Tipo de operación: solo consulta. Genera auditoría: sí, vía registro de acceso. Riesgos operativos: filtros incorrectos pueden omitir casos críticos. Errores esperados: 400 por filtros inválidos; 401/403 por autorización. Relación ACH/CENIT/NACHA-M: cubre ciclo de recepción NACHA-M previo a despacho interno. Precauciones para desarrollo u operación: usar filtros de fecha/estado consistentes en incidentes.")]
    [HttpGet("ingestions")]
    public async Task<IActionResult> GetIngestions([FromQuery] IncomingNachaIngestionQuery query, CancellationToken ct)
        => Ok(await _service.GetIngestionsAsync(query, ct));

    [EndpointSummary("Detalle de una ingestión NACHA")]
    [EndpointDescription("Qué hace: obtiene el detalle técnico y de estado de una ingestión puntual. Cuándo se usa: durante investigación de una ingestión específica. Perfil consumidor: soporte técnico ACH. Permiso requerido: CanReadAch. Tipo de operación: solo consulta. Genera auditoría: sí, por trazas de acceso. Riesgos operativos: consultar un identificador incorrecto puede inducir diagnósticos erróneos. Errores esperados: 404 si no existe la ingestión; 401/403 por autorización. Relación ACH/CENIT/NACHA-M: permite trazar recepción NACHA-M extremo a extremo. Precauciones para desarrollo u operación: confirmar correlation id antes de escalar.")]
    [HttpGet("ingestions/{ingestionId:guid}")]
    public async Task<IActionResult> GetIngestionDetail(Guid ingestionId, CancellationToken ct)
    {
        var result = await _service.GetIngestionDetailAsync(ingestionId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [EndpointSummary("Consulta de cola operativa de NACHA entrante")]
    [EndpointDescription("Qué hace: lista elementos pendientes/bloqueados en cola de procesamiento. Cuándo se usa: cuando hay represamiento o incumplimiento de SLA. Perfil consumidor: operadores ACH. Permiso requerido: CanReadAch. Tipo de operación: solo consulta. Genera auditoría: sí, por bitácoras del sistema. Riesgos operativos: interpretar mal estados de cola puede provocar acciones manuales innecesarias. Errores esperados: 400 por filtros inválidos; 401/403 por autorización. Relación ACH/CENIT/NACHA-M: gestiona tránsito NACHA-M hacia procesamiento ACH/CENIT. Precauciones para desarrollo u operación: revisar estado de máquina antes de intervenir manualmente.")]
    [HttpGet("queue")]
    public async Task<IActionResult> GetQueue([FromQuery] IncomingNachaQueueQuery query, CancellationToken ct)
        => Ok(await _service.GetQueueAsync(query, ct));

    [EndpointSummary("Detalle de item en cola NACHA")]
    [EndpointDescription("Qué hace: muestra datos completos y capacidades de acción para un item de cola. Cuándo se usa: antes de ejecutar reintento, desbloqueo o reencolado. Perfil consumidor: operador ACH con rol de soporte. Permiso requerido: CanReadAch. Tipo de operación: solo consulta. Genera auditoría: sí, por trazas de consulta. Riesgos operativos: actuar sin revisar detalle puede duplicar procesamiento. Errores esperados: 404 si el item no existe; 401/403 por autorización. Relación ACH/CENIT/NACHA-M: trazabilidad de eventos NACHA-M en cola de procesamiento. Precauciones para desarrollo u operación: verificar estado y restricciones de transición.")]
    [HttpGet("queue/{queueId:guid}")]
    public async Task<IActionResult> GetQueueDetail(Guid queueId, CancellationToken ct)
    {
        var result = await _service.GetQueueDetailAsync(queueId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [EndpointSummary("Reintento manual de item en cola")]
    [EndpointDescription("Qué hace: dispara un reintento manual del procesamiento de un item en cola. Cuándo se usa: tras resolver causa transitoria de falla. Perfil consumidor: operadores ACH con funciones de contingencia. Permiso requerido: CanManageAch. Tipo de operación: modifica información. Genera auditoría: sí, explícita en auditoría de acciones manuales. Riesgos operativos: reintentos indebidos pueden generar reprocesos o duplicidad. Errores esperados: 409 por transición inválida; 400 por solicitud inválida; 401/403 por autorización. Relación ACH/CENIT/NACHA-M: mantiene continuidad del flujo NACHA-M ante fallas temporales. Precauciones para desarrollo u operación: documentar causa del reintento y validar idempotencia.")]
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

    [EndpointSummary("Desbloqueo manual de item en cola")]
    [EndpointDescription("Qué hace: libera un item bloqueado para continuar procesamiento. Cuándo se usa: cuando el bloqueo ya fue analizado y aprobado. Perfil consumidor: operación ACH senior. Permiso requerido: CanManageAch. Tipo de operación: modifica información. Genera auditoría: sí, explícita en auditoría. Riesgos operativos: desbloqueos sin análisis pueden reactivar errores de seguridad/negocio. Errores esperados: 409 por estado incompatible; 400 por solicitud inválida; 401/403. Relación ACH/CENIT/NACHA-M: habilita continuidad del canal NACHA-M. Precauciones para desarrollo u operación: registrar motivo y evidencia operativa en ticket.")]
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

    [EndpointSummary("Reencolado manual de item")]
    [EndpointDescription("Qué hace: envía nuevamente un item a la cola para reproceso controlado. Cuándo se usa: en remediaciones operativas con ventana vigente. Perfil consumidor: operación ACH. Permiso requerido: CanManageAch. Tipo de operación: modifica información. Genera auditoría: sí, explícita. Riesgos operativos: reencolar sin control puede saturar cola y degradar SLA. Errores esperados: 409 por transición inválida; 400; 401/403. Relación ACH/CENIT/NACHA-M: recuperación de transacciones NACHA-M no completadas. Precauciones para desarrollo u operación: coordinar con capacidad de procesamiento antes del reencolado.")]
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

    [EndpointSummary("Marcar falla final de item")]
    [EndpointDescription("Qué hace: declara el item como falla final para cerrar su ciclo operativo. Cuándo se usa: cuando no existe ruta segura de recuperación. Perfil consumidor: operación ACH y liderazgo de soporte. Permiso requerido: CanManageAch. Tipo de operación: modifica información. Genera auditoría: sí, crítica para auditoría. Riesgos operativos: cerrar prematuramente un caso puede perder trazabilidad de recuperación. Errores esperados: 409 por transición inválida; 400; 401/403. Relación ACH/CENIT/NACHA-M: cierre controlado de incidentes NACHA-M en command center. Precauciones para desarrollo u operación: aprobar con doble validación operativa antes de ejecutar.")]
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
