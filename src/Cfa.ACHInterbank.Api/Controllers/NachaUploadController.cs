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
        public async Task<IActionResult> UploadNachaFile([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Archivo inválido.");

            try
            {
                using var stream = file.OpenReadStream();
                await _parserService.ParseAndSaveAsync(stream, file.FileName);

                return Ok("Archivo procesado y guardado.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar archivo NACHA-M {FileName}", file.FileName);
                return StatusCode(StatusCodes.Status500InternalServerError, "No fue posible procesar el archivo.");
            }
        }
    }
}
