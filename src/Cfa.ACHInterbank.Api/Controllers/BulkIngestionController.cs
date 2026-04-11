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
    private readonly ILogger<BulkIngestionController> _logger;

    public BulkIngestionController(
        IAchBulkFileIngestionService bulkFileIngestionService,
        IAchBulkBatchQueryService queryService,
        IAchBulkBatchRetryService retryService,
        ILogger<BulkIngestionController> logger)
    {
        _bulkFileIngestionService = bulkFileIngestionService;
        _queryService = queryService;
        _retryService = retryService;
        _logger = logger;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadSizeBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadSizeBytes)]
    [ProducesResponseType(typeof(BulkFileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

    [HttpGet("{batchId:guid}")]
    [ProducesResponseType(typeof(BulkBatchStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatch(Guid batchId, CancellationToken ct)
    {
        var batch = await _queryService.GetBatchAsync(batchId, ct);
        return batch is null
            ? NotFound(new { message = $"No existe el lote {batchId}." })
            : Ok(batch);
    }

    [HttpGet("{batchId:guid}/items")]
    [ProducesResponseType(typeof(BulkBatchItemsPageDto), StatusCodes.Status200OK)]
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

    [HttpGet("{batchId:guid}/summary")]
    [ProducesResponseType(typeof(BulkBatchProcessingSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBatchSummary(Guid batchId, CancellationToken ct)
    {
        var summary = await _queryService.GetBatchSummaryAsync(batchId, ct);
        return summary is null
            ? NotFound(new { message = $"No existe el lote {batchId}." })
            : Ok(summary);
    }

    [HttpPost("{batchId:guid}/retry")]
    [ProducesResponseType(typeof(RetryBatchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
}

public sealed class BulkFileUploadForm
{
    public IFormFile File { get; set; } = null!;
    public string? BatchReference { get; set; }
    public string? ClientRequestId { get; set; }
}
