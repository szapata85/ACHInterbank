using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/transactions/bulk-ingestion")]
[Authorize]
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

    /// <summary>
    /// Carga un archivo de transacciones para ingestión masiva.
    /// </summary>
    /// <param name="request">Formulario multipart con el archivo y las referencias opcionales del lote.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Cargar archivo de transacciones masivas")]
    [EndpointDescription("Recibe multipart/form-data con File obligatorio y un tamaño máximo de 20 MB. El código admite JSON, CSV y Excel (.xlsx/.xlsm). El archivo se parsea y cada registro se valida estructuralmente; los registros inválidos se conservan con estado StructuralError. Si existe al menos un registro válido, el lote se encola y se despacha su procesamiento; cuando hay registros pero ninguno es válido, el lote queda Failed. Si ClientRequestId está informado y ya existe un lote con el mismo ClientRequestId y el mismo hash SHA-256 del archivo, se devuelve ese lote existente. ClientRequestId pertenece a este flujo y no corresponde al header Idempotency-Key.")]
    [HttpPost("upload")]
    [Authorize(Policy = P1Policies.BulkIngestionUpload)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadSizeBytes)]
    [ProducesResponseType(typeof(BulkFileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Lote registrado o lote existente devuelto por coincidencia de ClientRequestId y hash del archivo.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "El archivo falta, está vacío, supera el tamaño admitido, usa un formato no soportado o produce una validación estructural capturada.")]
    [OpenApiResponseDescription(StatusCodes.Status500InternalServerError, "La acción capturó un error interno inesperado al registrar el lote.")]
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

    /// <summary>
    /// Consulta el estado consolidado de un lote de ingestión masiva.
    /// </summary>
    /// <param name="batchId">Identificador del lote devuelto por la carga.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar estado de un lote")]
    [EndpointDescription("Devuelve el estado consolidado y los contadores persistidos del lote indicado. Si el lote no existe, responde 404 con el cuerpo actual de mensaje simple.")]
    [HttpGet("{batchId:guid}")]
    [Authorize(Policy = P1Policies.BulkIngestionRead)]
    [ProducesResponseType(typeof(BulkBatchStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Estado consolidado actual del lote.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe un lote con el identificador indicado.")]
    public async Task<IActionResult> GetBatch(Guid batchId, CancellationToken ct)
    {
        var batch = await _queryService.GetBatchAsync(batchId, ct);
        return batch is null
            ? NotFound(new { message = $"No existe el lote {batchId}." })
            : Ok(batch);
    }

    /// <summary>
    /// Consulta los ítems de un lote con paginación y filtro opcional por estado.
    /// </summary>
    /// <param name="batchId">Identificador del lote consultado.</param>
    /// <param name="page">Página solicitada. El valor por defecto es 1 y cualquier valor menor se normaliza a 1.</param>
    /// <param name="pageSize">Cantidad de ítems por página. El valor por defecto es 100 y se limita al rango de 1 a 500.</param>
    /// <param name="status">Estado opcional usado para filtrar los ítems del lote.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar ítems de un lote")]
    [EndpointDescription("Devuelve los ítems del lote ordenados por ItemIndex. page usa 1 por defecto y se normaliza a un mínimo de 1; pageSize usa 100 por defecto y se limita al rango 1..500. status es un filtro opcional exacto por estado. Si el lote no existe, responde 404.")]
    [HttpGet("{batchId:guid}/items")]
    [Authorize(Policy = P1Policies.BulkIngestionRead)]
    [ProducesResponseType(typeof(BulkBatchItemsPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Página de ítems del lote después de aplicar la normalización de paginación y el filtro opcional por estado.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe un lote con el identificador indicado.")]
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

    /// <summary>
    /// Consulta el resumen consolidado y el historial de intentos de procesamiento de un lote.
    /// </summary>
    /// <param name="batchId">Identificador del lote consultado.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar resumen de procesamiento")]
    [EndpointDescription("Devuelve el estado consolidado del lote junto con su historial de intentos de procesamiento, ordenado del intento más reciente al más antiguo. Si el lote no existe, responde 404.")]
    [HttpGet("{batchId:guid}/summary")]
    [Authorize(Policy = P1Policies.BulkIngestionRead)]
    [ProducesResponseType(typeof(BulkBatchProcessingSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Resumen del lote con estado consolidado e historial de intentos de procesamiento.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe un lote con el identificador indicado.")]
    public async Task<IActionResult> GetBatchSummary(Guid batchId, CancellationToken ct)
    {
        var summary = await _queryService.GetBatchSummaryAsync(batchId, ct);
        return summary is null
            ? NotFound(new { message = $"No existe el lote {batchId}." })
            : Ok(summary);
    }

    /// <summary>
    /// Reintenta el procesamiento de los ítems elegibles de un lote.
    /// </summary>
    /// <param name="batchId">Identificador del lote que se desea reintentar.</param>
    /// <param name="request">Alcance del reintento: FailedOnly o Full.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Reintentar procesamiento de un lote")]
    [EndpointDescription("Solicita un nuevo intento de procesamiento. Scope=FailedOnly selecciona únicamente ítems en ProcessingError; Scope=Full selecciona todos los ítems excepto StructuralError. Los ítems elegibles vuelven a Ready, se crea un nuevo intento y se despacha el procesamiento. El máximo actual es de 3 reintentos. La operación rechaza lotes con procesamiento en curso, lotes que alcanzaron el máximo o alcances sin ítems elegibles. StructuralError no se corrige automáticamente mediante este reintento.")]
    [HttpPost("{batchId:guid}/retry")]
    [Authorize(Policy = P1Policies.BulkIngestionRetry)]
    [ProducesResponseType(typeof(RetryBatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Reintento creado y procesamiento despachado para los ítems elegibles.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "El lote no es reintentable en su estado actual, alcanzó el máximo de 3 reintentos o no tiene ítems elegibles para el alcance seleccionado.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe un lote con el identificador indicado.")]
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

    /// <summary>
    /// Cancela un lote cuando su estado actual lo permite.
    /// </summary>
    /// <param name="batchId">Identificador del lote que se desea cancelar.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Cancelar lote de ingestión")]
    [EndpointDescription("Cuando el lote existe y no está Completed, Failed ni Cancelled, el servicio actual marca su estado como Cancelled, registra el mensaje de cancelación y establece ProcessingFinishedAtUtc. Este comportamiento no demuestra una cancelación física de un job, worker o backplane que ya esté en ejecución.")]
    [HttpPost("{batchId:guid}/cancel")]
    [Authorize(Policy = P1Policies.BulkIngestionCancel)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "La cancelación fue aceptada y el servicio marcó el lote como Cancelled.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "El lote existe, pero su estado actual no permite cancelarlo.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe un lote con el identificador indicado.")]
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

/// <summary>
/// Formulario multipart para cargar un archivo de ingestión masiva.
/// </summary>
public sealed class BulkFileUploadForm
{
    /// <summary>Archivo obligatorio de la carga. Debe contener datos, no superar 20 MB y usar JSON, CSV o Excel (.xlsx/.xlsm).</summary>
    public IFormFile File { get; set; } = null!;

    /// <summary>Referencia opcional del lote. Si se omite o llega en blanco, el servicio genera una referencia para el lote.</summary>
    public string? BatchReference { get; set; }

    /// <summary>Identificador opcional usado junto con el hash SHA-256 del archivo para devolver un lote previamente registrado. No corresponde al header Idempotency-Key.</summary>
    public string? ClientRequestId { get; set; }
}
