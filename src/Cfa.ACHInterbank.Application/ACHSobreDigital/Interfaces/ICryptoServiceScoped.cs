using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;

namespace Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;

public interface ICryptoServiceScoped
{
    Task<byte[]> CreateEnvelopeAsync(byte[] contenidoBytes, string FileName);
    Task<byte[]> OpenEnvelopeAsync(byte[] contenidoBytes, string FileName);

    // Firma/verificación opcional sobre el sobre
    //Task<string> SignEnvelopeAsync(byte[] contenidoBytes);
    //Task<bool> VerifyEnvelopeAsync(DigitalEnvelopeModel envelope, CancellationToken ct = default);
}
