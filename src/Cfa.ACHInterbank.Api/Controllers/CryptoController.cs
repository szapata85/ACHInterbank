using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers
{
    public class CryptoController : Controller
    {
        private readonly ICryptoServiceScoped _crypto;

        public CryptoController(ICryptoServiceScoped crypto) => _crypto = crypto;

        public record EncryptRequest(string Base64Plaintext, Dictionary<string, string>? Aad);
        public record EncryptResponse(DigitalEnvelopeModel Envelope);
        public record DecryptRequest(DigitalEnvelopeModel Envelope);
        public record DecryptResponse(string Base64Plaintext);

        [HttpPost("encrypt")]
        public async Task<ActionResult<EncryptResponse>> Encrypt([FromBody] EncryptRequest req, CancellationToken ct)
        {
            var data = Convert.FromBase64String(req.Base64Plaintext);
            var envelope = await _crypto.CreateEnvelopeAsync(data, req.Aad, ct);
            return Ok(new EncryptResponse(envelope));
        }

        [HttpPost("decrypt")]
        public async Task<ActionResult<DecryptResponse>> Decrypt([FromBody] DecryptRequest req, CancellationToken ct)
        {
            var plain = await _crypto.OpenEnvelopeAsync(req.Envelope, ct);
            return Ok(new DecryptResponse(Convert.ToBase64String(plain)));
        }
    }
}
