namespace Cfa.ACHInterbank.Domain.Models.Configurations;

public sealed class CertificateManagementOptions
{
    public const string SectionName = "CertificateManagement";

    public int ExpirationWarningDays { get; set; } = 30;
}
