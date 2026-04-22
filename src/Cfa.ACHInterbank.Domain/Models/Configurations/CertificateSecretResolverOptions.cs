namespace Cfa.ACHInterbank.Domain.Models.Configurations;

public class CertificateSecretResolverOptions
{
    public bool EnableInMemoryProvider { get; set; } = true;
    public bool EnableExternalSecretReferenceProvider { get; set; } = true;
    public bool EnableKeyVaultProvider { get; set; } = false;
    public bool EnableHsmProvider { get; set; } = false;
    public bool FailIfSecretProviderUnavailable { get; set; } = false;
    public bool MaskSecretRefInLogs { get; set; } = true;
    public bool DisableInMemoryProviderInProduction { get; set; } = true;
}
