namespace Cfa.ACHInterbank.Domain.Models.Configurations;

public class DigitalEnvelopeSignatureValidationOptions
{
    public bool EnableSignatureValidation { get; set; } = true;
    public bool FailCloseOnInvalidSignature { get; set; } = true;
    public bool FailWhenSignerCertificateMissing { get; set; } = true;
    public bool FailWhenSignerCertificateExpired { get; set; } = true;
    public bool ValidateSignerCertificateThumbprint { get; set; } = false;
    public bool ValidateSignerCertificateChain { get; set; } = false;
    public bool AllowLegacyUnsignedEnvelope { get; set; } = false;
    public bool LogSignatureValidationDetails { get; set; } = true;
    public bool AuditInvalidSignature { get; set; } = true;
    public string Environment { get; set; } = "Test";
}
