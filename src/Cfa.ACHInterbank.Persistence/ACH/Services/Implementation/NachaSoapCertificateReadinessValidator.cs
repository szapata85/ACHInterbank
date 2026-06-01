using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaSoapCertificateReadinessValidator : INachaSoapCertificateReadinessValidator
{
    public NachaSoapCertificateCheckResult Validate(
        NachaSoapEndpointDescriptor endpoint,
        NachaSoapUatControlOptions options)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(options);

        var requiresCertificate = endpoint.RequiresClientCertificate || options.RequireCertificateValidation;
        var hasThumbprint = !string.IsNullOrWhiteSpace(endpoint.CertificateThumbprint);
        var hasStoreLocation = !string.IsNullOrWhiteSpace(endpoint.CertificateStoreName)
                               && !string.IsNullOrWhiteSpace(endpoint.CertificateStoreLocation);
        var errors = new List<string>();

        if (requiresCertificate && (!hasThumbprint || !hasStoreLocation))
        {
            errors.Add("Metadata de certificado requerida no configurada.");
        }

        return new NachaSoapCertificateCheckResult
        {
            RequiresClientCertificate = requiresCertificate,
            HasThumbprint = hasThumbprint,
            HasStoreLocation = hasStoreLocation,
            CertificateAvailable = hasThumbprint && hasStoreLocation,
            PrivateKeyAccessible = false,
            IsBlocked = errors.Count > 0,
            BlockReason = string.Join(" ", errors),
            SanitizedThumbprint = SanitizeThumbprint(endpoint.CertificateThumbprint),
            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Phase"] = "6B.5",
                ["StoreAccess"] = "NotAccessed",
                ["RealCertificateStoreAccess"] = "false"
            }
        };
    }

    internal static string SanitizeThumbprint(string thumbprint)
    {
        if (string.IsNullOrWhiteSpace(thumbprint))
        {
            return string.Empty;
        }

        var cleaned = thumbprint.Replace(" ", string.Empty, StringComparison.Ordinal);
        return cleaned.Length <= 8 ? "***" : $"{cleaned[..4]}***{cleaned[^4..]}";
    }
}
