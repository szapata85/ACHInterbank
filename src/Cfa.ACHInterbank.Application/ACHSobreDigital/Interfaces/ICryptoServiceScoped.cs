using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;

namespace Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;

public interface ICryptoServiceScoped
{
    Task<DigitalEnvelopeModel> CreateEnvelopeAsync(byte[] plaintext, IDictionary<string, string>? aad = null, CancellationToken ct = default);
    Task<byte[]> OpenEnvelopeAsync(DigitalEnvelopeModel envelope, CancellationToken ct = default);

    // Firma/verificación opcional sobre el sobre
    //Task<string> SignEnvelopeAsync(DigitalEnvelopeModel envelope, CancellationToken ct = default);
    //Task<bool> VerifyEnvelopeAsync(DigitalEnvelopeModel envelope, CancellationToken ct = default);
}
