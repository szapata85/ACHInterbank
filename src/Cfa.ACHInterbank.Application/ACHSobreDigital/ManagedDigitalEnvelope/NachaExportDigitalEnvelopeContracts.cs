namespace Cfa.ACHInterbank.Application.ACHSobreDigital.ManagedDigitalEnvelope;

public interface INachaExportDigitalEnvelopeService
{
    Task<ManagedDigitalEnvelopeResult> EncryptAsync(
        int clearingHouseId,
        string fileName,
        byte[] content,
        string actor,
        CancellationToken cancellationToken = default);
}
