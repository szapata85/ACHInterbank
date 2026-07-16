using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;

namespace Cfa.ACHInterbank.Domain.Models.Configurations;

public sealed class DigitalEnvelopeCertificateBootstrapOptions
{
    public const string SectionName = "DigitalEnvelope:CertificateBootstrap";

    public bool Enabled { get; set; }
    public string DirectoryPath { get; set; } = "/app/certificates/uat";
    public string PublicCertificateFileName { get; set; } = "ACHcolombia.cer";
    public string PrivateCertificateFileName { get; set; } = "CFA.pfx";
    public string? PfxPassword { get; set; }
    public int ClearingHouseId { get; set; } = 1;
    public string ClearingHouseCode { get; set; } = "ACHCOL";
    public string ClearingHouseName { get; set; } = "ACH Colombia";
    public string ClearingHouseOriginCode { get; set; } = "000101006";
    public CertificateEnvironment Environment { get; set; } = CertificateEnvironment.Test;
}
