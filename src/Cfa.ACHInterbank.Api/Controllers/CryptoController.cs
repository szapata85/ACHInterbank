//using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers
{
    [ApiController] // Es una buena práctica agregar este atributo para los controladores de API.
    [Route("api/[controller]")] // Define la ruta base para el controlador.
    public class CryptoController : ControllerBase // Se recomienda heredar de ControllerBase para APIs.
    {
        //private readonly ICryptoServiceScoped _crypto;

        //public CryptoController(ICryptoServiceScoped crypto) => _crypto = crypto;

        public record EncryptResponse(DigitalEnvelopeModel Envelope);
        public record DecryptRequest(DigitalEnvelopeModel Envelope);
        public record DecryptResponse(string Base64Plaintext);

        [HttpPost("encrypt")]
        public async Task<ActionResult<EncryptResponse>> Encrypt(IFormFile file, [FromForm] Dictionary<string, string>? aad, CancellationToken ct)
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
                await file.CopyToAsync(memoryStream, ct);
                data = memoryStream.ToArray();
            }

            //var envelope = await _crypto.CreateEnvelopeAsync(data, aad, ct);
            //return Ok(new EncryptResponse(envelope));
            return Ok();
        }

        [HttpPost("decrypt")]
        public async Task<ActionResult<DecryptResponse>> Decrypt([FromBody] DecryptRequest req, CancellationToken ct)
        {
            //var plain = await _crypto.OpenEnvelopeAsync(req.Envelope, ct);
            //return Ok(new DecryptResponse(Convert.ToBase64String(plain)));
            return Ok();
        }

        [HttpPost("testRSA")]
        public void testRSA([FromServices] IRsaKeyProviderSingleton _rsaKeyService)
        {
            var resultcert = _rsaKeyService.ObtenerCertificate("CertCrypt");
        }


            //IRsaKeyProviderSingleton
        }
}