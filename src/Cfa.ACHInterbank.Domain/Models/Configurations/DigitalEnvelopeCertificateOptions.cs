namespace Cfa.ACHInterbank.Domain.Models.Configurations;

public class DigitalEnvelopeCertificateOptions
{
    public bool UseCertificateManagement { get; set; } = false;
    public bool AllowLegacyCertificateFallback { get; set; } = true;
    public bool FailIfCertificateManagementUnavailable { get; set; } = false;
    public string Environment { get; set; } = "Test";
    public int DefaultClearingHouseId { get; set; } = 1;
    public bool PreferActiveCertificateManagementVersion { get; set; } = true;
    public bool LogCertificateSource { get; set; } = true;
}
