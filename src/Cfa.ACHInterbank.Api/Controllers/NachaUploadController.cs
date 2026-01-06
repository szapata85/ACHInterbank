using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Validators.NachaValidator;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NachaUploadController : Controller
    {
        private readonly INachaParserService _parserService;
        private readonly ILogger<NachaUploadController> _logger;

        public NachaUploadController(INachaParserService parserService, ILogger<NachaUploadController> logger)
        {
            _parserService = parserService;
            _logger = logger;
        }

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
    }

    public class NachaUploadRequest
    {
        public IFormFile File { get; set; } = null!;
    }
}
