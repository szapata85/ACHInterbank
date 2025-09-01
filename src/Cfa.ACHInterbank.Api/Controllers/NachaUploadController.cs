using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Validators.NachaValidator;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NachaUploadController : Controller
    {
        private readonly INachaParserService _parserService;

        public NachaUploadController(INachaParserService parserService)
        {
            _parserService = parserService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadNachaFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Archivo inválido.");

            using var stream = file.OpenReadStream();
            await _parserService.ParseAndSaveAsync(stream);

            return Ok("Archivo procesado y guardado.");
        }
    }
}
