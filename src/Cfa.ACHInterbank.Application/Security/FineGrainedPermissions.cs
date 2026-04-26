namespace Cfa.ACHInterbank.Application.Security;

public static class FineGrainedPermissions
{
    public const string CanGenerateNacha = "CanGenerateNacha";
    public const string CanGenerateEncryptedNacha = "CanGenerateEncryptedNacha";
    public const string CanManualEncryptEnvelope = "CanManualEncryptEnvelope";
    public const string CanManualDecryptEnvelope = "CanManualDecryptEnvelope";
    public const string CanDownloadPlainNacha = "CanDownloadPlainNacha";
    public const string CanDownloadEnvelope = "CanDownloadEnvelope";
    public const string CanViewNachaSecurityAudit = "CanViewNachaSecurityAudit";
    public const string CanManageCertificates = "CanManageCertificates";
    public const string CanRunInteroperabilityHarness = "CanRunInteroperabilityHarness";
    public const string CanViewPaymentRailCapabilityRegistry = "CanViewPaymentRailCapabilityRegistry";
}
