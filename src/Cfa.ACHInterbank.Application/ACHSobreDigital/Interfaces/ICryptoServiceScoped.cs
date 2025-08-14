using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;

namespace Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;

public interface ICryptoServiceScoped
{
    Task<byte[]> CreateEnvelopeAsync(byte[] contenidoBytes);
    Task<byte[]> OpenEnvelopeAsync(byte[] contenidoBytes);

    // Firma/verificación opcional sobre el sobre
    //Task<string> SignEnvelopeAsync(DigitalEnvelopeModel envelope, CancellationToken ct = default);
    //Task<bool> VerifyEnvelopeAsync(DigitalEnvelopeModel envelope, CancellationToken ct = default);
}
