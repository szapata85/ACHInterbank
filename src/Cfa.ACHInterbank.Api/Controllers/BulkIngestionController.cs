using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/transactions/bulk-ingestion")]
[Authorize(Policy = "CanManageAch")]
public class BulkIngestionController : ControllerBase
{
    private const long MaxUploadSizeBytes = 20 * 1024 * 1024;

    private readonly IAchBulkFileIngestionService _bulkFileIngestionService;
    private readonly IAchBulkBatchQueryService _queryService;
    private readonly IAchBulkBatchRetryService _retryService;
    private readonly IBulkIngestionLifecycleService _lifecycleService;
    private readonly ILogger<BulkIngestionController> _logger;

    public BulkIngestionController(
        IAchBulkFileIngestionService bulkFileIngestionService,
        IAchBulkBatchQueryService queryService,
        IAchBulkBatchRetryService retryService,
        IBulkIngestionLifecycleService lifecycleService,
        ILogger<BulkIngestionController> logger)
    {
        _bulkFileIngestionService = bulkFileIngestionService;
        _queryService = queryService;
        _retryService = retryService;
        _lifecycleService = lifecycleService;
        _logger = logger;
    }

    [EndpointSummary("Carga masiva de archivo para procesamiento por lotes")]
    [EndpointDescription("Qué acción ejecuta: recibe un archivo de carga masiva, valida estructura básica y registra el lote para procesamiento. Quién lo usa: operación ACH, soporte de integración y equipos de backoffice con ventanas autorizadas. Permiso requerido: CanManageAch. Tipo: acción operativa con impacto en carga masiva, lote y transacciones derivadas. Auditoría/trazabilidad: debe registrar usuario solicitante, nombre de archivo, referencia de lote y clientRequestId para correlación. Riesgos: cargar archivo equivocado o fuera de ventana puede introducir errores operativos masivos. Errores esperados: 400 por archivo inválido/tamaño/formato; 401 no autenticado; 403 no autorizado; 500 error no controlado. Relación con NACHA-M/transacciones: habilita ingreso de datos de alto volumen que alimentan procesamiento ACH y conciliación posterior. Precaución operacional: validar origen, versión y corte operativo antes de subir. Advertencia: no modifica reglas regulatorias ni justifica alteración manual de archivos originales fuera de flujo controlado.")]
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadSizeBytes)]
    [ProducesResponseType(typeof(BulkFileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Upload([FromForm] BulkFileUploadForm request, CancellationToken ct)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest(new { message = "Debe adjuntar un archivo de lote." });
        }

        if (request.File.Length > MaxUploadSizeBytes)
        {
            return BadRequest(new { message = $"El archivo supera el tamaño máximo permitido de {MaxUploadSizeBytes / (1024 * 1024)} MB." });
        }

        try
        {
            await using var stream = request.File.OpenReadStream();
            var response = await _bulkFileIngestionService.UploadAndParseAsync(
                stream,
                request.File.FileName,
                request.File.ContentType,
                new BulkFileUploadRequest
                {
                    BatchReference = request.BatchReference,
                    ClientRequestId = request.ClientRequestId,
                    RequestedBy = User?.Identity?.Name
                },
                ct);

            return Ok(response);
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning(ex, "Formato de archivo de lote no soportado.");
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Error de validación estructural de archivo masivo.");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar lote masivo.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error interno del servidor." });
        }
    }

    [EndpointSummary("Consulta de estado general de lote masivo")]
    [EndpointDescription("Qué consulta: retorna el estado consolidado de un lote de carga masiva por identificador. Quién lo usa: operación, soporte y auditoría para seguimiento de avance y resultado. Permiso requerido: CanManageAch. Tipo: consulta de seguimiento sin mutación de datos. Impacto operacional: permite confirmar si el lote avanza, falla o requiere intervención. Auditoría/trazabilidad: la consulta queda registrada y se correlaciona con eventos de procesamiento. Riesgos: interpretar estado sin revisar detalle puede generar acciones prematuras. Errores esperados: 401 no autenticado; 403 no autorizado; 404 lote no encontrado; 500 error no controlado. Relación con carga masiva/lotes: punto principal de tracking del ciclo de lote. Precaución operacional: confirmar batchId y ventana de ejecución antes de escalar incidentes.")]
    [HttpGet("{batchId:guid}")]
    [ProducesResponseType(typeof(BulkBatchStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBatch(Guid batchId, CancellationToken ct)
    {
        var batch = await _queryService.GetBatchAsync(batchId, ct);
        return batch is null
            ? NotFound(new { message = $"No existe el lote {batchId}." })
            : Ok(batch);
    }

    [EndpointSummary("Consulta paginada de ítems del lote masivo")]
    [EndpointDescription("Qué consulta: lista ítems del lote con paginación y filtro por estado para diagnóstico detallado. Quién lo usa: soporte técnico, operación ACH y control de calidad de carga. Permiso requerido: CanManageAch. Tipo: consulta de seguimiento. Impacto operacional: facilita aislar transacciones con error o pendientes para definir reintento/cancelación. Auditoría/trazabilidad: deja evidencia de revisión por ítem y estado. Riesgos: filtros o paginación incorrecta pueden ocultar ítems críticos. Errores esperados: 400 solicitud inválida; 401 no autenticado; 403 no autorizado; 404 lote no encontrado; 500 error no controlado. Relación con carga masiva/transacciones: descompone el lote en unidades operativas para soporte y conciliación. Precaución operacional: revisar total de páginas y estado filtrado antes de decidir acciones operativas.")]
    [HttpGet("{batchId:guid}/items")]
    [ProducesResponseType(typeof(BulkBatchItemsPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBatchItems(
        Guid batchId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] BulkIngestionItemStatusEnum? status = null,
        CancellationToken ct = default)
    {
        var batch = await _queryService.GetBatchAsync(batchId, ct);
        if (batch is null)
        {
            return NotFound(new { message = $"No existe el lote {batchId}." });
        }

        var result = await _queryService.GetBatchItemsAsync(batchId, page, pageSize, status, ct);
        return Ok(result);
    }

    [EndpointSummary("Resumen de procesamiento y errores del lote")]
    [EndpointDescription("Qué consulta: entrega métricas agregadas de procesamiento del lote, incluyendo conteos de éxito, error y pendientes. Quién lo usa: operación, auditoría operativa y tecnología para evaluar salud de la carga. Permiso requerido: CanManageAch. Tipo: consulta de observabilidad. Impacto operacional: soporta decisiones de retry o cancelación sin modificar estado por sí misma. Auditoría/trazabilidad: consolida evidencia para reportes de incidente y cumplimiento. Riesgos: usar resumen sin contraste con ítems puede ocultar errores de alto impacto. Errores esperados: 401 no autenticado; 403 no autorizado; 404 lote no encontrado; 500 error no controlado. Relación con lotes/NACHA-M/transacciones: resume comportamiento de la carga masiva que alimenta flujos ACH. Precaución operacional: correlacionar con detalle del lote antes de ejecutar acciones manuales.")]
    [HttpGet("{batchId:guid}/summary")]
    [ProducesResponseType(typeof(BulkBatchProcessingSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBatchSummary(Guid batchId, CancellationToken ct)
    {
        var summary = await _queryService.GetBatchSummaryAsync(batchId, ct);
        return summary is null
            ? NotFound(new { message = $"No existe el lote {batchId}." })
            : Ok(summary);
    }

    [EndpointSummary("Reintento operativo de lote o subconjunto de ítems")]
    [EndpointDescription("Qué acción ejecuta: solicita reintento de procesamiento para el lote según parámetros del request. Quién lo usa: operación autorizada y soporte senior durante recuperación de fallas. Permiso requerido: CanManageAch. Tipo: acción operativa con impacto directo en estado de lote/ítems y en la secuencia de intentos. Auditoría/trazabilidad: debe registrar usuario ejecutor, criterio de reintento y resultado del disparo. Riesgos: reintentar sin análisis puede duplicar cargas o aumentar backlog. Errores esperados: 400 solicitud inválida o estado no permitido; 401 no autenticado; 403 no autorizado; 404 lote no encontrado; 409 estado inconsistente cuando aplique; 500 error no controlado. Relación con carga masiva/transacciones: habilita recuperación controlada de procesamiento masivo. Precaución operacional: validar idempotencia, ventana y causa raíz antes de reintentar. Advertencia: no autoriza modificación manual de archivos originales ni de reglas regulatorias.")]
    [HttpPost("{batchId:guid}/retry")]
    [ProducesResponseType(typeof(RetryBatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Retry(Guid batchId, [FromBody] RetryBatchRequest request, CancellationToken ct)
    {
        try
        {
            var response = await _retryService.RetryAsync(
                batchId,
                request,
                triggeredBy: User?.Identity?.Name ?? "system",
                ct);
            return Ok(response);
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

    [EndpointSummary("Solicitud operativa de cancelación de lote masivo")]
    [EndpointDescription("Qué acción ejecuta: solicita cancelación del lote cuando su estado permite detener procesamiento pendiente. Quién lo usa: operación ACH y líderes de soporte con criterio de contención. Permiso requerido: CanManageAch. Tipo: acción operativa con impacto en continuidad del lote y procesamiento de transacciones pendientes. Auditoría/trazabilidad: debe quedar evidencia de usuario, momento y motivo de cancelación. Riesgos: cancelar lote incorrecto puede afectar SLA y compromisos de procesamiento. Errores esperados: 400 estado no cancelable; 401 no autenticado; 403 no autorizado; 404 lote no encontrado; 409 estado inconsistente cuando aplique; 500 error no controlado. Relación con carga masiva/lote: controla detención de ejecución para mitigación de incidentes. Precaución operacional: confirmar alcance de impacto y comunicación a áreas dependientes antes de cancelar. Advertencia: no altera archivos de origen ni reglas regulatorias fuera de flujo controlado.")]
    [HttpPost("{batchId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Cancel(Guid batchId, CancellationToken ct)
    {
        var cancelled = await _lifecycleService.RequestCancellationAsync(
            batchId,
            User?.Identity?.Name ?? "system",
            ct);

        if (!cancelled)
        {
            var batch = await _queryService.GetBatchAsync(batchId, ct);
            if (batch is null)
            {
                return NotFound(new { message = $"No existe el lote {batchId}." });
            }

            return BadRequest(new { message = "El lote no se puede cancelar en su estado actual." });
        }

        return Ok(new
        {
            batchId,
            cancelled = true,
            message = "Cancelación solicitada correctamente."
        });
    }
}

public sealed class BulkFileUploadForm
{
    public IFormFile File { get; set; } = null!;
    public string? BatchReference { get; set; }
    public string? ClientRequestId { get; set; }
}
