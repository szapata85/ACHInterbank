using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Validators.NachaValidator;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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
        // CENIT inbound files retain their complete four-segment external name.
        // Do not normalize this to an .ach fixture name or derive it from a batch.
        private static readonly Regex OfficialNachaNamePattern = new(@"^\d{7}\.\d{3}\.\d{8}\.\d+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".ach", ".nacha", ".txt", ".OUT", ".RET", ".env"
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

            var fileName = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(fileName);
            var isOfficialName = OfficialNachaNamePattern.IsMatch(fileName);
            if (!isOfficialName && !AllowedExtensions.Contains(extension))
            {
                return BadRequest(new NachaUploadResponseDto
                {
                    Success = false,
                    Partial = false,
                    Message = "Extensión de archivo no permitida.",
                    Errors = ["Nombres CENIT permitidos: RRRRTTT.ZZZ.YYYYMMDD.N; ACH Colombia: .OUT; devoluciones controladas: .RET; fixtures internos: .ach, .nacha, .txt."],
                    TraceId = traceId
                });
            }

            if (extension.Equals(".env", StringComparison.OrdinalIgnoreCase)
                && (!request.ClearingHouseId.HasValue || request.ClearingHouseId.Value <= 0))
            {
                return BadRequest(new NachaUploadResponseDto
                {
                    Success = false,
                    Partial = false,
                    Message = "Debe seleccionar la cámara del sobre digital.",
                    Errors = ["ClearingHouseId es obligatorio para archivos .env."],
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
                    FileName = fileName,
                    ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                    RequestedBy = User?.Identity?.Name ?? "usuario-api",
                    CorrelationId = traceId,
                    RequestedClearingHouseId = request.ClearingHouseId,
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
                        Domain.Models.ACH.IncomingNachaIngestionStatus.Bloqueado when result.ProfileSelectionStatus.HasValue
                            => "Archivo bloqueado por selección de perfil NACHA-M.",
                        Domain.Models.ACH.IncomingNachaIngestionStatus.Bloqueado => "Archivo bloqueado por ambigüedad de ciclo.",
                        Domain.Models.ACH.IncomingNachaIngestionStatus.PendienteResolucion => "Archivo pendiente de resolución de ciclo.",
                        Domain.Models.ACH.IncomingNachaIngestionStatus.Completado => "Archivo procesado correctamente.",
                        Domain.Models.ACH.IncomingNachaIngestionStatus.Fallido => "No fue posible procesar el archivo.",
                        _ => "Archivo recibido."
                    },
                    Errors = result.Errors,
                    TraceId = traceId,
                    IngestionId = result.IngestionId,
                    OriginalFileName = result.OriginalFileName,
                    FileHash = result.FileHash,
                    CorrelationId = result.CorrelationId,
                    IngestionStatus = result.IngestionStatus.ToString(),
                    CycleResolutionStatus = result.CycleResolutionStatus.ToString(),
                    ParsingStatus = result.ParsingStatus.ToString(),
                    DetectedClearingHouseId = result.DetectedClearingHouseId,
                    ResolvedClearingHouseId = result.ResolvedClearingHouseId,
                    ResolvedAchCycleId = result.ResolvedAchCycleId,
                    OperationalDate = result.OperationalDate,
                    ProfileSelectionStatus = result.ProfileSelectionStatus?.ToString(),
                    SelectedProfileCode = result.SelectedProfileCode,
                    SelectedProfileVersion = result.SelectedProfileVersion,
                    TotalBatches = result.TotalBatches,
                    TotalEntries = result.TotalEntries,
                    TotalAddendas = result.TotalAddendas,
                    Error = result.OperationalIssue
                };

                if (result.IngestionStatus is Domain.Models.ACH.IncomingNachaIngestionStatus.Bloqueado
                    or Domain.Models.ACH.IncomingNachaIngestionStatus.Fallido)
                {
                    return UnprocessableEntity(response);
                }

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                var incidentId = Guid.NewGuid().ToString("N");
                _logger.LogWarning(
                    "NACHA_UPLOAD_VALIDATION_FAILED|Incident={Incident}|ExceptionType={ExceptionType}",
                    incidentId,
                    ex.GetType().Name);
                return BadRequest(new NachaUploadResponseDto
                {
                    Success = false,
                    Partial = false,
                    Message = "No fue posible validar el archivo.",
                    Errors = [$"NACHA_UPLOAD_VALIDATION_FAILED;Incident={incidentId}"],
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
                var incidentId = Guid.NewGuid().ToString("N");
                _logger.LogError(
                    "NACHA_UPLOAD_PROCESSING_FAILED|Incident={Incident}|ExceptionType={ExceptionType}",
                    incidentId,
                    ex.GetType().Name);
                return StatusCode(StatusCodes.Status500InternalServerError, new NachaUploadResponseDto
                {
                    Success = false,
                    Partial = false,
                    Message = "No fue posible procesar el archivo.",
                    Errors = [$"NACHA_UPLOAD_PROCESSING_FAILED;Incident={incidentId}"],
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
                    TotalEntries = _context.EntryDetails.Count(e => e.NachaID == h.NachaID),
                    TotalAddendas = _context.AddendaRecords.Count(a => a.NachaID == h.NachaID),
                    TotalBatches = _context.BatchHeaders.Count(b => b.NachaID == h.NachaID),
                    TotalAmount = _context.EntryDetails
                        .Where(e => e.NachaID == h.NachaID)
                        .Sum(e => e.Amount ?? 0),
                    TotalDebitAmount = _context.FileControls
                        .Where(fc => fc.NachaID == h.NachaID)
                        .Sum(fc => fc.TotalDebitAmount),
                    TotalCreditAmount = _context.FileControls
                        .Where(fc => fc.NachaID == h.NachaID)
                        .Sum(fc => fc.TotalCreditAmount)
                })
                .ToListAsync(ct);

            return Ok(result);
        }
    }

    public class NachaUploadRequest
    {
        public IFormFile File { get; set; } = null!;
        public int? ClearingHouseId { get; set; }
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
        public string OriginalFileName { get; set; } = string.Empty;
        public string FileHash { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string IngestionStatus { get; set; } = string.Empty;
        public string CycleResolutionStatus { get; set; } = string.Empty;
        public string ParsingStatus { get; set; } = string.Empty;
        public int? DetectedClearingHouseId { get; set; }
        public int? ResolvedClearingHouseId { get; set; }
        public string? ResolvedAchCycleId { get; set; }
        public DateTime? OperationalDate { get; set; }
        public string? ProfileSelectionStatus { get; set; }
        public string? SelectedProfileCode { get; set; }
        public string? SelectedProfileVersion { get; set; }
        public int TotalBatches { get; set; }
        public int TotalEntries { get; set; }
        public int TotalAddendas { get; set; }
        public IncomingNachaAdmissionIssue? Error { get; set; }
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
