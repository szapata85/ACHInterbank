namespace Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;

public enum DigitalEnvelopeCertificateType
{
    EncryptionPublic = 1,
    SigningKeyPair = 2
}

public class DigitalEnvelopeCertificate
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DigitalEnvelopeCertificateType Type { get; set; }
    public byte[] RawData { get; set; } = Array.Empty<byte>();
    public string? Password { get; set; }
    public bool HasPrivateKey { get; set; }
    public string? Subject { get; set; }
    public string? Issuer { get; set; }
    public string? Thumbprint { get; set; }
    public DateTime? NotBefore { get; set; }
    public DateTime? NotAfter { get; set; }
    public DateTime UploadedAt { get; set; }
}
