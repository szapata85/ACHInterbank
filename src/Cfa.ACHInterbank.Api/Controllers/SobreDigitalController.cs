//using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers
{
    [ApiController] // Es una buena práctica agregar este atributo para los controladores de API.
    [Route("[controller]")] // Define la ruta base para el controlador.
    public class SobreDigitalController : ControllerBase // Se recomienda heredar de ControllerBase para APIs.
    {
        private readonly ICryptoServiceScoped _crypto;

        public SobreDigitalController(ICryptoServiceScoped crypto) => _crypto = crypto;
        /// <summary>
        /// Pendiente de documentación.
        /// </summary>

        public record EncryptResponse(DigitalEnvelopeModel Envelope);
        /// <summary>
        /// Pendiente de documentación.
        /// </summary>
        public record DecryptRequest(DigitalEnvelopeModel Envelope);
        /// <summary>
        /// Pendiente de documentación.
        /// </summary>
        public record DecryptResponse(string Base64Plaintext);
        /// <summary>
        /// Pendiente de documentación.
        /// </summary>

        [HttpPost("encrypt")]
        public async Task<IActionResult> Encrypt(IFormFile file)
        {
            // Valida que el archivo exista.
            if (file == null || file.Length == 0)
            {
                return BadRequest("No se ha proporcionado ningún archivo.");
            }

            // Convierte el IFormFile a un array de bytes.
            byte[] data;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                data = memoryStream.ToArray();
            }

            byte[] fileBytes = await _crypto.CreateEnvelopeAsync(data, file.FileName);

            string fileName = $"{file.FileName}.ENV";
            string contentType = "text/plain"; // cambia según el tipo de archivo

            // Devuelve el archivo como respuesta
            return File(fileBytes, contentType, fileName);
        }
        /// <summary>
        /// Pendiente de documentación.
        /// </summary>

        [HttpPost("decrypt")]
        public async Task<ActionResult<DecryptResponse>> Decrypt(IFormFile file)
        {
            //var plain = await _crypto.OpenEnvelopeAsync(req.Envelope, ct);
            //return Ok(new DecryptResponse(Convert.ToBase64String(plain)));
            // Valida que el archivo exista.
            if (file == null || file.Length == 0)
            {
                return BadRequest("No se ha proporcionado ningún archivo.");
            }

            // Convierte el IFormFile a un array de bytes.
            byte[] data;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                data = memoryStream.ToArray();
            }

            //byte[] fileBytes = await _crypto.CreateEnvelopeAsync(data, file.FileName);
            byte[] fileBytes = await _crypto.OpenEnvelopeAsync(data, file.FileName);

            string fileName = file.FileName.Replace(".ENV", null);
            string contentType = "text/plain"; // cambia según el tipo de archivo

            // Devuelve el archivo como respuesta
            return File(fileBytes, contentType, fileName);
        }
        /// <summary>
        /// Pendiente de documentación.
        /// </summary>

        [HttpPost("testRSA")]
        public void testRSA([FromServices] IRsaKeyProvider _rsaKeyService)
        {
            var resultcert = _rsaKeyService.ObtenerCertificate("CertCrypt");
        }


            //IRsaKeyProviderSingleton
        }
}
