using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Validators.NachaValidator;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NachaUploadController : Controller
    {
        private readonly INachaParserService _parserService;
        private readonly AchDbContext _context;
        private readonly ILogger<NachaUploadController> _logger;

        public NachaUploadController(
            INachaParserService parserService,
            AchDbContext context,
            ILogger<NachaUploadController> logger)
        {
            _parserService = parserService;
            _context = context;
            _logger = logger;
        }
        /// <summary>
        /// Pendiente de documentación.
        /// </summary>

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadNachaFile([FromForm] NachaUploadRequest request)
        {
            var file = request.File;
            if (file == null || file.Length == 0)
                return BadRequest("Archivo inválido.");

            try
            {
                using var stream = file.OpenReadStream();
                var failures = await _parserService.ParseAndSaveAsync(stream, file.FileName);

                if (failures.Count > 0)
                {
                    return Ok(new
                    {
                        message = "Archivo procesado con devoluciones por operador.",
                        operatorReturns = failures
                    });
                }

                return Ok("Archivo procesado y guardado.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar archivo NACHA-M {FileName}", file.FileName);
                return StatusCode(StatusCodes.Status500InternalServerError, "No fue posible procesar el archivo.");
            }
        }

        [HttpGet("records")]
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
