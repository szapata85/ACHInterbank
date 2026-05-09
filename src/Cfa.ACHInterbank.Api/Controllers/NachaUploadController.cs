using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Validators.NachaValidator;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class NachaUploadController : Controller
    {
        private readonly IIncomingNachaIngestionAppService _ingestionService;
        private readonly AchDbContext _context;
        private readonly ILogger<NachaUploadController> _logger;

        public NachaUploadController(
            IIncomingNachaIngestionAppService ingestionService,
            AchDbContext context,
            ILogger<NachaUploadController> logger)
        {
            _ingestionService = ingestionService;
            _context = context;
            _logger = logger;
        }
        /// <summary>
        /// Endpoint de la API ACH Interbank.
        /// </summary>

        private const long MaxUploadSizeBytes = 10 * 1024 * 1024; // 10 MB
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".ach", ".nacha", ".txt"
        };
        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "text/plain", "application/octet-stream"
        };

        [HttpPost("upload")]
        [Authorize(Policy = P1Policies.NachaUpload)]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxUploadSizeBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadSizeBytes)]
        public async Task<IActionResult> UploadNachaFile([FromForm] NachaUploadRequest request, CancellationToken ct)
        {
            var traceId = HttpContext.TraceIdentifier;
            var file = request.File;
            if (file == null || file.Length == 0)
            {
                return BadRequest(new NachaUploadResponseDto
                {
                    Success = false,
                    Partial = false,
                    Message = "Archivo inválido.",
                    Errors = ["Debe enviar un archivo NACHA válido."],
                    TraceId = traceId
                });
            }

            if (file.Length > MaxUploadSizeBytes)
            {
                return BadRequest(new NachaUploadResponseDto
                {
                    Success = false,
                    Partial = false,
                    Message = "Archivo excede el tamaño permitido.",
                    Errors = [$"Tamaño máximo permitido: {MaxUploadSizeBytes / (1024 * 1024)} MB."],
                    TraceId = traceId
                });
            }

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(extension))
            {
                return BadRequest(new NachaUploadResponseDto
                {
                    Success = false,
                    Partial = false,
                    Message = "Extensión de archivo no permitida.",
                    Errors = ["Extensiones permitidas: .ach, .nacha, .txt"],
                    TraceId = traceId
                });
            }

            if (!string.IsNullOrWhiteSpace(file.ContentType) && !AllowedContentTypes.Contains(file.ContentType))
            {
                return BadRequest(new NachaUploadResponseDto
                {
                    Success = false,
                    Partial = false,
                    Message = "Tipo MIME no permitido.",
                    Errors = ["Tipos MIME permitidos: text/plain, application/octet-stream"],
                    TraceId = traceId
                });
            }

            try
            {
                await using var stream = file.OpenReadStream();
                var result = await _ingestionService.IngestAsync(new IncomingNachaIngestionRequest
                {
                    FileStream = stream,
                    FileName = file.FileName,
                    ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                    RequestedBy = User?.Identity?.Name ?? "usuario-api",
                    CorrelationId = traceId,
                    ForceReprocess = request.ForceReprocess,
                    ParentIngestionId = request.ParentIngestionId
                }, ct);

                var response = new NachaUploadResponseDto
                {
                    Success = result.IngestionStatus == Domain.Models.ACH.IncomingNachaIngestionStatus.Completado,
                    Partial = result.ErrorCount > 0 || result.CycleResolutionStatus is Domain.Models.ACH.IncomingNachaCycleResolutionStatus.Ambiguo or Domain.Models.ACH.IncomingNachaCycleResolutionStatus.NoResuelto,
                    Message = result.IngestionStatus switch
                    {
                        Domain.Models.ACH.IncomingNachaIngestionStatus.Duplicado => "Archivo duplicado detectado.",
                        Domain.Models.ACH.IncomingNachaIngestionStatus.Bloqueado => "Archivo bloqueado por ambigüedad de ciclo.",
                        Domain.Models.ACH.IncomingNachaIngestionStatus.PendienteResolucion => "Archivo pendiente de resolución de ciclo.",
                        Domain.Models.ACH.IncomingNachaIngestionStatus.Completado => "Archivo procesado correctamente.",
                        _ => "Archivo recibido."
                    },
                    Errors = result.Errors,
                    TraceId = traceId,
                    IngestionId = result.IngestionId,
                    IngestionStatus = result.IngestionStatus.ToString(),
                    CycleResolutionStatus = result.CycleResolutionStatus.ToString(),
                    ParsingStatus = result.ParsingStatus.ToString(),
                    DetectedClearingHouseId = result.DetectedClearingHouseId,
                    ResolvedClearingHouseId = result.ResolvedClearingHouseId,
                    ResolvedAchCycleId = result.ResolvedAchCycleId,
                    OperationalDate = result.OperationalDate,
                    TotalBatches = result.TotalBatches,
                    TotalEntries = result.TotalEntries,
                    TotalAddendas = result.TotalAddendas
                };

                if (result.IngestionStatus == Domain.Models.ACH.IncomingNachaIngestionStatus.Bloqueado)
                {
                    return UnprocessableEntity(response);
                }

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validación de archivo NACHA falló {FileName}", file.FileName);
                return BadRequest(new NachaUploadResponseDto
                {
                    Success = false,
                    Partial = false,
                    Message = "No fue posible validar el archivo.",
                    Errors = [ex.Message],
                    TraceId = traceId
                });
            }
            catch (OperationCanceledException)
            {
                return StatusCode(StatusCodes.Status408RequestTimeout, new NachaUploadResponseDto
                {
                    Success = false,
                    Partial = false,
                    Message = "La carga del archivo fue cancelada.",
                    Errors = ["La operación fue cancelada o agotó el tiempo de espera."],
                    TraceId = traceId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar archivo NACHA-M {FileName}", file.FileName);
                return StatusCode(StatusCodes.Status500InternalServerError, new NachaUploadResponseDto
                {
                    Success = false,
                    Partial = false,
                    Message = "No fue posible procesar el archivo.",
                    Errors = ["Error interno del servidor."],
                    TraceId = traceId
                });
            }
        }

        [HttpGet("records")]
        [Authorize(Policy = P1Policies.NachaRead)]
        public async Task<ActionResult<IReadOnlyList<NachaUploadRecordResponse>>> GetUploadedRecords(
            [FromQuery] string? immediateOrigin,
            [FromQuery] string? immediateDestination,
            [FromQuery] string? referenceCode,
            [FromQuery] string? achCycleId,
            [FromQuery] DateTime? fileCreationDate,
            CancellationToken ct)
        {
            var query = _context.NachaHeaders
                .AsNoTracking()
                .Include(h => h.ClearingHouse)
                .Include(h => h.AchCycle)
                .Include(h => h.EntryDetails)
                .Include(h => h.Batches)
                .Include(h => h.AddendaRecords)
                .Include(h => h.BatchControls)
                .Include(h => h.FileControls)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(immediateOrigin))
            {
                string value = immediateOrigin.Trim();
                query = query.Where(h => h.ImmediateOrigin != null && h.ImmediateOrigin.Contains(value));
            }

            if (!string.IsNullOrWhiteSpace(immediateDestination))
            {
                string value = immediateDestination.Trim();
                query = query.Where(h => h.ImmediateDestination != null && h.ImmediateDestination.Contains(value));
            }

            if (!string.IsNullOrWhiteSpace(referenceCode))
            {
                string value = referenceCode.Trim();
                query = query.Where(h => h.ReferenceCode != null && h.ReferenceCode.Contains(value));
            }

            if (!string.IsNullOrWhiteSpace(achCycleId))
            {
                string value = achCycleId.Trim();
                query = query.Where(h => h.AchCycleId == value);
            }

            if (fileCreationDate.HasValue)
            {
                string target = fileCreationDate.Value.ToString("yyyyMMdd");
                query = query.Where(h => h.FileCreationDate == target);
            }

            var result = await query
                .OrderByDescending(h => h.FileCreationDate)
                .ThenByDescending(h => h.FileCreationTime)
                .Select(h => new NachaUploadRecordResponse
                {
                    NachaId = h.NachaID,
                    ImmediateOrigin = h.ImmediateOrigin,
                    ImmediateDestination = h.ImmediateDestination,
                    ImmediateOriginName = h.ImmediateOriginName,
                    ImmediateDestinationName = h.ImmediateDestinationName,
                    ReferenceCode = h.ReferenceCode,
                    FileCreationDate = h.FileCreationDate,
                    FileCreationTime = h.FileCreationTime,
                    AchCycleId = h.AchCycleId,
                    AchCycleName = h.AchCycle != null ? h.AchCycle.CycleName : null,
                    ClearingHouseName = h.ClearingHouse != null ? h.ClearingHouse.Name : null,
                    TotalEntries = h.EntryDetails != null ? h.EntryDetails.Count : 0,
                    TotalAddendas = h.AddendaRecords != null ? h.AddendaRecords.Count : 0,
                    TotalBatches = h.Batches != null ? h.Batches.Count : 0,
                    TotalAmount = h.EntryDetails != null
                        ? h.EntryDetails.Sum(e => e.Amount ?? 0)
                        : 0,
                    TotalDebitAmount = h.FileControls != null
                        ? h.FileControls.Sum(fc => fc.TotalDebitAmount)
                        : 0,
                    TotalCreditAmount = h.FileControls != null
                        ? h.FileControls.Sum(fc => fc.TotalCreditAmount)
                        : 0
                })
                .ToListAsync(ct);

            return Ok(result);
        }
    }

    public class NachaUploadRequest
    {
        public IFormFile File { get; set; } = null!;
        public bool ForceReprocess { get; set; }
        public Guid? ParentIngestionId { get; set; }
    }

    public class NachaUploadResponseDto
    {
        public bool Success { get; set; }
        public bool Partial { get; set; }
        public string Message { get; set; } = string.Empty;
        public IReadOnlyList<string> Errors { get; set; } = [];
        public IReadOnlyList<NachaValidationFailure> OperatorReturns { get; set; } = [];
        public string TraceId { get; set; } = string.Empty;
        public Guid? IngestionId { get; set; }
        public string IngestionStatus { get; set; } = string.Empty;
        public string CycleResolutionStatus { get; set; } = string.Empty;
        public string ParsingStatus { get; set; } = string.Empty;
        public int? DetectedClearingHouseId { get; set; }
        public int? ResolvedClearingHouseId { get; set; }
        public string? ResolvedAchCycleId { get; set; }
        public DateTime? OperationalDate { get; set; }
        public int TotalBatches { get; set; }
        public int TotalEntries { get; set; }
        public int TotalAddendas { get; set; }
    }

    public class NachaUploadRecordResponse
    {
        public string? NachaId { get; set; }
        public string? ImmediateOrigin { get; set; }
        public string? ImmediateDestination { get; set; }
        public string? ImmediateOriginName { get; set; }
        public string? ImmediateDestinationName { get; set; }
        public string? ReferenceCode { get; set; }
        public string? FileCreationDate { get; set; }
        public string? FileCreationTime { get; set; }
        public string? AchCycleId { get; set; }
        public string? AchCycleName { get; set; }
        public string? ClearingHouseName { get; set; }
        public int TotalEntries { get; set; }
        public int TotalAddendas { get; set; }
        public int TotalBatches { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalDebitAmount { get; set; }
        public decimal TotalCreditAmount { get; set; }
    }
}
