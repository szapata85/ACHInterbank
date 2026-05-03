using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Application.ACH.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("incoming-nacha-command-center")]
[Authorize]
public class IncomingNachaCommandCenterController : ControllerBase
{
    private static IActionResult MapInvalidOperation(InvalidOperationException ex)
        => new ObjectResult(new { message = ex.Message }) { StatusCode = StatusCodes.Status409Conflict };
    private readonly IIncomingNachaCommandCenterService _service;

    public IncomingNachaCommandCenterController(IIncomingNachaCommandCenterService service)
    {
        _service = service;
    }

    [EndpointSummary("Panel consolidado de observabilidad de inbound NACHA-M")]
    [EndpointDescription("Qué consulta: consolida indicadores de ingesta, cola de despacho, reintentos, bloqueos y fallas finales en la ventana solicitada. Quién lo usa: operación ACH, soporte de incidentes, auditoría operativa y tecnología para diagnóstico temprano. Permiso requerido: CanReadAch. Tipo: consulta (solo lectura) sin ejecución de acciones manuales. Impacto operacional: orienta priorización de incidentes y ventanas de atención, pero no cambia estados ni altera cola. Auditoría/trazabilidad: la consulta queda en trazas de acceso y se correlaciona con eventos previos del command center. Riesgos: una ventana horaria incorrecta puede ocultar backlog crítico. Errores esperados: 400 por windowHours inválido; 401 no autenticado; 403 no autorizado; 500 error no controlado. Relación NACHA-M inbound: provee visibilidad del tramo entrante antes de su continuidad ACH/CENIT. Advertencia: no modifica archivos originales NACHA-M ni reglas regulatorias; cualquier ajuste requiere flujo controlado.")]
    [HttpGet("observability/summary")]
    [Authorize(Policy = P1Policies.CommandCenterRead)]
    [ProducesResponseType(typeof(IncomingNachaObservabilitySummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetObservabilitySummary([FromQuery] int windowHours = 24, CancellationToken ct = default)
        => Ok(await _service.GetObservabilitySummaryAsync(windowHours, ct));

    [EndpointSummary("Consulta paginada de ingestas inbound NACHA-M")]
    [EndpointDescription("Qué consulta: lista ingestas entrantes con filtros de estado, fechas y correlación para seguimiento operativo. Quién lo usa: operación, soporte técnico y auditoría de continuidad que revisan volumen y progreso de procesamiento. Permiso requerido: CanReadAch. Tipo: consulta (solo lectura). Impacto operacional: habilita triage de incidentes y selección de casos a investigar, sin alterar la ingesta ni la cola. Auditoría/trazabilidad: registra consumo para evidencia de monitoreo y control. Riesgos: filtros incompletos pueden excluir casos críticos o generar diagnóstico parcial. Errores esperados: 400 solicitud inválida; 401 no autenticado; 403 no autorizado; 500 error no controlado. Relación NACHA-M inbound: cubre la etapa de recepción y clasificación previa a acciones manuales sobre dispatch queue. Advertencia: no ejecuta cutover ni modifica contenido de archivos originales o reglas regulatorias.")]
    [HttpGet("ingestions")]
    [Authorize(Policy = P1Policies.CommandCenterRead)]
    [ProducesResponseType(typeof(IncomingNachaPageResult<IncomingNachaIngestionListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetIngestions([FromQuery] IncomingNachaIngestionQuery query, CancellationToken ct)
        => Ok(await _service.GetIngestionsAsync(query, ct));

    [EndpointSummary("Detalle técnico y operativo de una ingesta inbound")]
    [EndpointDescription("Qué consulta: retorna el detalle integral de una ingestión específica, incluyendo estado de procesamiento y contexto asociado. Quién lo usa: soporte de segundo nivel, operación ACH y auditoría técnica para reconstrucción de casos. Permiso requerido: CanReadAch. Tipo: consulta (solo lectura). Impacto operacional: apoya decisiones de remediación sobre cola y reintentos, sin mutar la ingestión. Auditoría/trazabilidad: referencia identificadores de correlación y eventos históricos para evidencia de investigación. Riesgos: usar ingestionId incorrecto puede derivar en diagnóstico sobre un caso no relacionado. Errores esperados: 404 no encontrado; 401 no autenticado; 403 no autorizado; 500 error no controlado. Relación NACHA-M inbound: permite traza de extremo a extremo del archivo entrante y su progreso hacia ACH/CENIT. Advertencia: no altera archivos originales ni aplica cambios regulatorios fuera de flujo controlado.")]
    [HttpGet("ingestions/{ingestionId:guid}")]
    [Authorize(Policy = P1Policies.CommandCenterRead)]
    [ProducesResponseType(typeof(IncomingNachaIngestionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetIngestionDetail(Guid ingestionId, CancellationToken ct)
    {
        var result = await _service.GetIngestionDetailAsync(ingestionId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [EndpointSummary("Vista operativa de dispatch queue inbound NACHA-M")]
    [EndpointDescription("Qué consulta: lista elementos de cola con estado de despacho, prioridad, intentos y errores para control operativo. Quién lo usa: operación ACH, soporte de turnos y tecnología durante incidentes de procesamiento. Permiso requerido: CanReadAch. Tipo: consulta (solo lectura). Impacto operacional: permite decidir si corresponde retry, unblock, requeue o mark-failed-final, sin ejecutar acciones por sí misma. Auditoría/trazabilidad: deja evidencia de consulta previa a intervención manual. Riesgos: interpretar mal la máquina de estados puede gatillar acciones incorrectas en pasos posteriores. Errores esperados: 400 solicitud inválida; 401 no autenticado; 403 no autorizado; 500 error no controlado. Relación NACHA-M inbound: refleja el estado del tránsito de mensajes entrantes hacia ejecución operativa ACH/CENIT. Advertencia: no modifica archivos originales ni reglas regulatorias, solo expone lectura de estado.")]
    [HttpGet("queue")]
    [Authorize(Policy = P1Policies.CommandCenterRead)]
    [ProducesResponseType(typeof(IncomingNachaPageResult<IncomingNachaQueueListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetQueue([FromQuery] IncomingNachaQueueQuery query, CancellationToken ct)
        => Ok(await _service.GetQueueAsync(query, ct));

    [EndpointSummary("Detalle de item de dispatch queue con acciones permitidas")]
    [EndpointDescription("Qué consulta: obtiene detalle del item de cola, eventos y acciones manuales permitidas por estado actual. Quién lo usa: operación de contingencia, soporte técnico y auditoría de ejecución manual. Permiso requerido: CanReadAch. Tipo: consulta (solo lectura). Impacto operacional: prepara decisión informada antes de ejecutar acciones manuales, sin cambiar estado del item. Auditoría/trazabilidad: facilita evidencia de debido proceso al revisar contexto previo a intervenir. Riesgos: omitir esta consulta antes de accionar puede producir duplicidad de intentos o transición indebida. Errores esperados: 404 no encontrado; 401 no autenticado; 403 no autorizado; 500 error no controlado. Relación NACHA-M inbound: conecta la trazabilidad del archivo entrante con su unidad de trabajo en cola. Advertencia: no altera archivos NACHA-M ni reglas regulatorias sin flujo controlado.")]
    [HttpGet("queue/{queueId:guid}")]
    [Authorize(Policy = P1Policies.CommandCenterRead)]
    [ProducesResponseType(typeof(IncomingNachaQueueDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetQueueDetail(Guid queueId, CancellationToken ct)
    {
        var result = await _service.GetQueueDetailAsync(queueId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [EndpointSummary("Acción manual de retry sobre item de dispatch queue")]
    [EndpointDescription("Qué acción ejecuta: solicita retry manual de un item en cola cuando existe condición transitoria resuelta. Quién lo usa: operación autorizada, soporte senior y continuidad operacional. Permiso requerido: CanManageAch. Tipo: acción manual con impacto operacional sobre estado de procesamiento e intento de reenvío. Entidad afectada: dispatch queue (attemptCount, next attempt, estado y eventos). Auditoría/trazabilidad: debe quedar evidencia de usuario, motivo, estado previo y estado resultante. Riesgos: retry sin validación puede provocar reproceso duplicado o presión sobre integraciones externas. Errores esperados: 400 solicitud inválida; 401 no autenticado; 403 no autorizado; 404 queueId no encontrado; 409 transición no permitida/estado inconsistente; 500 error no controlado. Relación NACHA-M inbound: recupera procesamiento de mensajes entrantes sin modificar archivo fuente. Advertencia: no cambia reglas regulatorias ni contenido original NACHA-M; solo ejecuta transición controlada del item.")]
    [HttpPost("queue/{queueId:guid}/retry")]
    [Authorize(Policy = P1Policies.CommandCenterRetry)]
    [ProducesResponseType(typeof(IncomingNachaManualActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    [EndpointSummary("Acción manual de unblock sobre item bloqueado")]
    [EndpointDescription("Qué acción ejecuta: desbloquea manualmente un item para que vuelva a flujo operativo permitido por la máquina de estados. Quién lo usa: operación ACH senior y soporte de incidentes con autorización de cambio. Permiso requerido: CanManageAch. Tipo: acción manual con impacto operacional sobre estado de bloqueo/desbloqueo. Entidad afectada: dispatch queue y trazabilidad de transición. Auditoría/trazabilidad: debe registrar usuario ejecutor, motivo de desbloqueo y evidencia de aprobación. Riesgos: desbloquear sin remediar causa raíz puede reintroducir fallas repetitivas o incumplir controles. Errores esperados: 400 solicitud inválida; 401 no autenticado; 403 no autorizado; 404 queueId no encontrado; 409 transición no permitida/estado inconsistente; 500 error no controlado. Relación NACHA-M inbound: reanuda procesamiento de trabajo asociado a ingestión entrante. Advertencia: no modifica archivo original ni normativa; aplica únicamente transición manual controlada del item.")]
    [HttpPost("queue/{queueId:guid}/unblock")]
    [Authorize(Policy = P1Policies.CommandCenterUnblock)]
    [ProducesResponseType(typeof(IncomingNachaManualActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    [EndpointSummary("Acción manual de requeue para reproceso controlado")]
    [EndpointDescription("Qué acción ejecuta: reencola manualmente el item para reproceso desde cola bajo condiciones operativas autorizadas. Quién lo usa: operación ACH, soporte técnico y continuidad en ventanas de recuperación. Permiso requerido: CanManageAch. Tipo: acción manual con impacto operacional sobre estado de cola y prioridad de atención. Entidad afectada: dispatch queue, estado de procesamiento y secuencia de intentos. Auditoría/trazabilidad: debe conservar motivo, usuario y transición aplicada para revisión posterior. Riesgos: requeue indiscriminado puede incrementar backlog, afectar SLA o generar competencia de recursos. Errores esperados: 400 solicitud inválida; 401 no autenticado; 403 no autorizado; 404 queueId no encontrado; 409 transición no permitida/estado inconsistente; 500 error no controlado. Relación NACHA-M inbound: permite recuperación operativa sin alterar el archivo entrante ni su evidencia original. Advertencia: no altera reglas regulatorias ni habilita cambios fuera de flujo controlado.")]
    [HttpPost("queue/{queueId:guid}/requeue")]
    [Authorize(Policy = P1Policies.CommandCenterRequeue)]
    [ProducesResponseType(typeof(IncomingNachaManualActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    [EndpointSummary("Acción manual mark-failed-final para cierre operativo")]
    [EndpointDescription("Qué acción ejecuta: marca el item como falla final cuando no existe ruta segura de recuperación y debe cerrarse el caso. Quién lo usa: liderazgo de operación, soporte senior y control operacional con autorización formal. Permiso requerido: CanManageAch. Tipo: acción manual con impacto operacional de cierre definitivo del estado de procesamiento. Entidad afectada: dispatch queue, estado final y trazabilidad de decisión. Auditoría/trazabilidad: requiere registro explícito de motivo, aprobadores y evidencia asociada al cierre. Riesgos: cierre prematuro puede omitir recuperación viable y afectar conciliación o investigación posterior. Errores esperados: 400 solicitud inválida; 401 no autenticado; 403 no autorizado; 404 queueId no encontrado; 409 transición no permitida/estado inconsistente; 500 error no controlado. Relación NACHA-M inbound: cierra controladamente incidentes de items derivados de ingesta entrante, sin modificar archivo original. Advertencia: no altera reglas regulatorias ni sustituye proceso formal de gestión de incidentes.")]
    [HttpPost("queue/{queueId:guid}/mark-failed-final")]
    [Authorize(Policy = P1Policies.CommandCenterMarkFailedFinal)]
    [ProducesResponseType(typeof(IncomingNachaManualActionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
