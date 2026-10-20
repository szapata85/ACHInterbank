using Cfa.ACHInterbank.Api.OpenApi;
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

        [EndpointSummary("Cargar archivo NACHA-M para ingestión")]
        [EndpointDescription("Recibe multipart/form-data con File, ClearingHouseId, ForceReprocess y ParentIngestionId. El archivo debe ser no vacío y de hasta 10 MB; se aceptan nombres CENIT con el patrón implementado o extensiones .ach, .nacha, .txt, .OUT, .RET y .env. Los archivos .env requieren ClearingHouseId positivo. Cuando el archivo informa MIME type, solo se aceptan text/plain o application/octet-stream; si viene vacío se remite al servicio como application/octet-stream. Devuelve 200 al completar/aceptar la ingesta, 422 cuando el resultado queda Bloqueado o Fallido, 400 para validaciones controladas, 408 para cancelación, 500 para errores capturados y el límite de request puede producir 413 antes de completar la acción.")]
        [HttpPost("upload")]
        [Authorize(Policy = P1Policies.NachaUpload)]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(MaxUploadSizeBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadSizeBytes)]
        [ProducesResponseType(typeof(NachaUploadResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(NachaUploadResponseDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(NachaUploadResponseDto), StatusCodes.Status408RequestTimeout)]
        [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
        [ProducesResponseType(typeof(NachaUploadResponseDto), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(NachaUploadResponseDto), StatusCodes.Status500InternalServerError)]
        [OpenApiResponseDescription(StatusCodes.Status200OK, "Resultado de la ingesta NACHA-M cuando el servicio no deja el archivo en estado Bloqueado ni Fallido.")]
        [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "El archivo/formulario no supera una validación explícita del controller o el servicio lanza ArgumentException; el body es NachaUploadResponseDto.")]
        [OpenApiResponseDescription(StatusCodes.Status408RequestTimeout, "La operación fue cancelada o agotó el tiempo de espera; el body es NachaUploadResponseDto.")]
        [OpenApiResponseDescription(StatusCodes.Status413PayloadTooLarge, "La solicitud multipart supera el límite de 10 MB aplicado por RequestSizeLimit/RequestFormLimits antes de completar el procesamiento de la acción.")]
        [OpenApiResponseDescription(StatusCodes.Status422UnprocessableEntity, "El servicio de ingestión devolvió estado Bloqueado o Fallido; el body es NachaUploadResponseDto.")]
        [OpenApiResponseDescription(StatusCodes.Status500InternalServerError, "Una excepción no controlada por las validaciones específicas fue capturada por la acción; el body es NachaUploadResponseDto.")]
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

        /// <summary>Consulta los encabezados NACHA-M persistidos aplicando filtros opcionales.</summary>
        /// <param name="immediateOrigin">Filtro opcional aplicado por coincidencia parcial sobre el origen inmediato persistido.</param>
        /// <param name="immediateDestination">Filtro opcional aplicado por coincidencia parcial sobre el destino inmediato persistido.</param>
        /// <param name="referenceCode">Filtro opcional aplicado por coincidencia parcial sobre el código de referencia persistido.</param>
        /// <param name="achCycleId">Filtro opcional por coincidencia exacta con el identificador de ciclo ACH persistido.</param>
        /// <param name="fileCreationDate">Fecha opcional usada para comparar el campo de fecha de creación del archivo en formato <c>yyyyMMdd</c>.</param>
        /// <param name="ct">Token de cancelación de la solicitud.</param>
        [EndpointSummary("Consultar archivos NACHA-M cargados")]
        [EndpointDescription("Consulta los encabezados NACHA persistidos y permite filtrar por immediateOrigin, immediateDestination, referenceCode, achCycleId y fileCreationDate. La respuesta agrega por archivo los conteos de lotes, entradas y adendas, además de totales monetarios persistidos; la acción no expone descarga de archivo ni recibe body.")]
        [HttpGet("records")]
        [Authorize(Policy = P1Policies.NachaRead)]
        [ProducesResponseType(typeof(IReadOnlyList<NachaUploadRecordResponse>), StatusCodes.Status200OK)]
        [OpenApiResponseDescription(StatusCodes.Status200OK, "Colección de archivos NACHA-M persistidos que cumplen los filtros solicitados, con conteos y totales agregados.")]
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

    /// <summary>Formulario multipart utilizado para cargar un archivo NACHA-M.</summary>
    public class NachaUploadRequest
    {
        /// <summary>Archivo recibido en el campo multipart <c>File</c>.</summary>
        public IFormFile File { get; set; } = null!;
        /// <summary>Identificador opcional de la cámara solicitada; es obligatorio y positivo cuando el nombre termina en <c>.env</c>.</summary>
        public int? ClearingHouseId { get; set; }
        /// <summary>Indica si la solicitud pide reprocesar de forma controlada una ingesta previa.</summary>
        public bool ForceReprocess { get; set; }
        /// <summary>Identificador opcional de la ingesta padre asociado al reproceso.</summary>
        public Guid? ParentIngestionId { get; set; }
    }

    /// <summary>Resultado contractual de la carga e ingestión de un archivo NACHA-M.</summary>
    public class NachaUploadResponseDto
    {
        /// <summary>Indica si la ingesta terminó en estado Completado.</summary>
        public bool Success { get; set; }
        /// <summary>Indica si el resultado contiene errores o una resolución de ciclo ambigua/no resuelta.</summary>
        public bool Partial { get; set; }
        /// <summary>Mensaje funcional derivado del estado de ingestión.</summary>
        public string Message { get; set; } = string.Empty;
        /// <summary>Errores devueltos por la ingesta o generados por la acción.</summary>
        public IReadOnlyList<string> Errors { get; set; } = [];
        /// <summary>Validaciones/devoluciones de operador incluidas por el contrato cuando existan.</summary>
        public IReadOnlyList<NachaValidationFailure> OperatorReturns { get; set; } = [];
        /// <summary>Identificador de trazabilidad de la petición HTTP.</summary>
        public string TraceId { get; set; } = string.Empty;
        /// <summary>Identificador persistido de la ingesta cuando fue creado.</summary>
        public Guid? IngestionId { get; set; }
        /// <summary>Nombre de archivo registrado por el servicio de ingestión.</summary>
        public string OriginalFileName { get; set; } = string.Empty;
        /// <summary>Hash de archivo devuelto por el servicio de ingestión.</summary>
        public string FileHash { get; set; } = string.Empty;
        /// <summary>Identificador de correlación registrado para la ingesta.</summary>
        public string CorrelationId { get; set; } = string.Empty;
        /// <summary>Estado de ingestión convertido a texto.</summary>
        public string IngestionStatus { get; set; } = string.Empty;
        /// <summary>Estado de resolución de ciclo convertido a texto.</summary>
        public string CycleResolutionStatus { get; set; } = string.Empty;
        /// <summary>Estado de parsing convertido a texto.</summary>
        public string ParsingStatus { get; set; } = string.Empty;
        /// <summary>Identificador de cámara detectado durante la ingestión cuando existe.</summary>
        public int? DetectedClearingHouseId { get; set; }
        /// <summary>Identificador de cámara finalmente resuelto cuando existe.</summary>
        public int? ResolvedClearingHouseId { get; set; }
        /// <summary>Identificador del ciclo ACH resuelto cuando existe.</summary>
        public string? ResolvedAchCycleId { get; set; }
        /// <summary>Fecha operativa resuelta para el archivo cuando existe.</summary>
        public DateTime? OperationalDate { get; set; }
        /// <summary>Estado de selección de perfil NACHA-M cuando el servicio lo informa.</summary>
        public string? ProfileSelectionStatus { get; set; }
        /// <summary>Código del perfil NACHA-M seleccionado cuando existe.</summary>
        public string? SelectedProfileCode { get; set; }
        /// <summary>Versión del perfil NACHA-M seleccionado cuando existe.</summary>
        public string? SelectedProfileVersion { get; set; }
        /// <summary>Total de lotes contabilizados por la ingesta.</summary>
        public int TotalBatches { get; set; }
        /// <summary>Total de entradas contabilizadas por la ingesta.</summary>
        public int TotalEntries { get; set; }
        /// <summary>Total de adendas contabilizadas por la ingesta.</summary>
        public int TotalAddendas { get; set; }
        /// <summary>Incidencia operacional de admisión cuando el servicio la informa.</summary>
        public IncomingNachaAdmissionIssue? Error { get; set; }
    }

    /// <summary>Resumen persistido de un archivo NACHA-M cargado.</summary>
    public class NachaUploadRecordResponse
    {
        /// <summary>Identificador NACHA persistido.</summary>
        public string? NachaId { get; set; }
        /// <summary>Immediate Origin registrado en el encabezado.</summary>
        public string? ImmediateOrigin { get; set; }
        /// <summary>Immediate Destination registrado en el encabezado.</summary>
        public string? ImmediateDestination { get; set; }
        /// <summary>Nombre del originador inmediato registrado.</summary>
        public string? ImmediateOriginName { get; set; }
        /// <summary>Nombre del destino inmediato registrado.</summary>
        public string? ImmediateDestinationName { get; set; }
        /// <summary>Código de referencia registrado en el encabezado.</summary>
        public string? ReferenceCode { get; set; }
        /// <summary>Fecha de creación del archivo tal como fue persistida.</summary>
        public string? FileCreationDate { get; set; }
        /// <summary>Hora de creación del archivo tal como fue persistida.</summary>
        public string? FileCreationTime { get; set; }
        /// <summary>Identificador del ciclo ACH relacionado cuando existe.</summary>
        public string? AchCycleId { get; set; }
        /// <summary>Nombre del ciclo ACH relacionado cuando existe.</summary>
        public string? AchCycleName { get; set; }
        /// <summary>Nombre de la cámara relacionada cuando existe.</summary>
        public string? ClearingHouseName { get; set; }
        /// <summary>Total de registros de detalle asociados.</summary>
        public int TotalEntries { get; set; }
        /// <summary>Total de adendas asociadas.</summary>
        public int TotalAddendas { get; set; }
        /// <summary>Total de lotes asociados.</summary>
        public int TotalBatches { get; set; }
        /// <summary>Suma de importes de los registros de detalle asociados.</summary>
        public decimal TotalAmount { get; set; }
        /// <summary>Total débito persistido en los controles de archivo asociados.</summary>
        public decimal TotalDebitAmount { get; set; }
        /// <summary>Total crédito persistido en los controles de archivo asociados.</summary>
        public decimal TotalCreditAmount { get; set; }
    }
}
