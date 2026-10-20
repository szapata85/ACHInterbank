using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;

internal static class CertificateSecretMasker
{
    public static string Mask(string? secretRef)
    {
        if (string.IsNullOrWhiteSpace(secretRef))
        {
            return "****";
        }

        return secretRef.Length <= 6 ? "****" : $"****{secretRef[^6..]}";
    }
}

[Singleton]
public class InMemoryCertificateSecretStore : IInMemoryCertificateSecretStore
{
    private readonly ConcurrentDictionary<string, (byte[] RawPkcs12, string? Password)> _materials = new(StringComparer.Ordinal);

    public void Set(string secretRef, byte[] rawPkcs12, string? password)
    {
        _materials[secretRef] = (rawPkcs12, password);
    }

    public bool TryGet(string secretRef, out (byte[] RawPkcs12, string? Password) material)
    {
        return _materials.TryGetValue(secretRef, out material);
    }
}

[Scoped]
public class InMemoryCertificateSecretProvider : ICertificateSecretProvider
{
    private readonly IInMemoryCertificateSecretStore _store;
    private readonly IHostEnvironment _environment;
    private readonly CertificateSecretResolverOptions _options;

    public InMemoryCertificateSecretProvider(
        IInMemoryCertificateSecretStore store,
        IHostEnvironment environment,
        IOptions<CertificateSecretResolverOptions> options)
    {
        _store = store;
        _environment = environment;
        _options = options.Value ?? new CertificateSecretResolverOptions();
    }

    public CertificateSecretProviderType ProviderType => CertificateSecretProviderType.InMemory;
    public bool Supports(CertificateStorageMode storageMode) => storageMode == CertificateStorageMode.ExternalSecretReference;

    public Task<CertificateSecretResolutionResult> ResolveAsync(CertificateSecretResolutionRequest request, CancellationToken cancellationToken = default)
    {
        var masked = CertificateSecretMasker.Mask(request.SecretRef);
        if (!_options.EnableInMemoryProvider)
        {
            return Task.FromResult(new CertificateSecretResolutionResult(false, ProviderType, null, "INMEMORY_PROVIDER_DISABLED", "InMemory secret provider está deshabilitado.", masked));
        }

        if (_options.DisableInMemoryProviderInProduction && _environment.IsProduction())
        {
            return Task.FromResult(new CertificateSecretResolutionResult(false, ProviderType, null, "INMEMORY_PROVIDER_NOT_ALLOWED", "InMemory secret provider no permitido en producción.", masked));
        }

        if (!_store.TryGet(request.SecretRef, out var material))
        {
            return Task.FromResult(new CertificateSecretResolutionResult(false, ProviderType, null, "SECRET_REF_NOT_FOUND", "SecretRef no existe en InMemory provider.", masked));
        }

        try
        {
            var cert = X509CertificateLoader.LoadPkcs12(material.RawPkcs12, material.Password, X509KeyStorageFlags.EphemeralKeySet);
            var resolved = new CertificateSecretMaterial(cert, cert.Thumbprint ?? string.Empty, cert.SerialNumber, cert.Subject, cert.HasPrivateKey);
            return Task.FromResult(new CertificateSecretResolutionResult(true, ProviderType, resolved, null, null, masked));
        }
        catch
        {
            return Task.FromResult(new CertificateSecretResolutionResult(false, ProviderType, null, "SECRET_MATERIAL_INVALID", "El material secreto no es un PKCS#12 válido.", masked));
        }
    }
}

[Scoped]
public class DatabaseEncryptedCertificateSecretProvider : ICertificateSecretProvider
{
    private readonly AchDbContext _context;
    private readonly ICertificatePrivateMaterialProtector _protector;

    public DatabaseEncryptedCertificateSecretProvider(
        AchDbContext context,
        ICertificatePrivateMaterialProtector protector)
    {
        _context = context;
        _protector = protector;
    }

    public CertificateSecretProviderType ProviderType => CertificateSecretProviderType.DatabaseEncrypted;
    public bool Supports(CertificateStorageMode storageMode) => storageMode == CertificateStorageMode.DatabaseEncrypted;

    public async Task<CertificateSecretResolutionResult> ResolveAsync(
        CertificateSecretResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        var version = await _context.DigitalCertificateVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.CertificateVersionId, cancellationToken);
        var masked = CertificateSecretMasker.Mask(request.SecretRef);

        if (version?.EncryptedPrivateMaterial is not { Length: > 0 })
        {
            return new CertificateSecretResolutionResult(false, ProviderType, null, "ENCRYPTED_MATERIAL_NOT_FOUND", "No existe material privado protegido para la versión.", masked);
        }

        try
        {
            var certificate = _protector.Unprotect(version.EncryptedPrivateMaterial);
            if (!certificate.HasPrivateKey
                || !string.Equals(certificate.Thumbprint, version.Thumbprint, StringComparison.OrdinalIgnoreCase))
            {
                certificate.Dispose();
                return new CertificateSecretResolutionResult(false, ProviderType, null, "ENCRYPTED_MATERIAL_MISMATCH", "El material protegido no corresponde a la versión registrada.", masked);
            }

            var material = new CertificateSecretMaterial(
                certificate,
                certificate.Thumbprint ?? string.Empty,
                certificate.SerialNumber,
                certificate.Subject,
                true);
            return new CertificateSecretResolutionResult(true, ProviderType, material, null, null, masked);
        }
        catch (CryptographicException)
        {
            return new CertificateSecretResolutionResult(false, ProviderType, null, "ENCRYPTED_MATERIAL_INVALID", "No fue posible abrir el material privado protegido.", masked);
        }
    }
}

[Scoped]
public class ExternalSecretReferenceCertificateProvider : ICertificateSecretProvider
{
    private readonly CertificateSecretResolverOptions _options;

    public ExternalSecretReferenceCertificateProvider(IOptions<CertificateSecretResolverOptions> options)
    {
        _options = options.Value ?? new CertificateSecretResolverOptions();
    }

    public CertificateSecretProviderType ProviderType => CertificateSecretProviderType.ExternalSecretReference;
    public bool Supports(CertificateStorageMode storageMode) => storageMode == CertificateStorageMode.ExternalSecretReference;

    public Task<CertificateSecretResolutionResult> ResolveAsync(CertificateSecretResolutionRequest request, CancellationToken cancellationToken = default)
    {
        var masked = CertificateSecretMasker.Mask(request.SecretRef);
        if (!_options.EnableExternalSecretReferenceProvider)
        {
            return Task.FromResult(new CertificateSecretResolutionResult(false, ProviderType, null, "SECRET_PROVIDER_DISABLED", "ExternalSecretReference provider deshabilitado.", masked));
        }

        return Task.FromResult(new CertificateSecretResolutionResult(false, ProviderType, null, "SECRET_PROVIDER_NOT_CONFIGURED", "No hay backend de secretos externo configurado.", masked));
    }
}

[Scoped]
public class KeyVaultCertificateSecretProvider : ICertificateSecretProvider
{
    private readonly CertificateSecretResolverOptions _options;

    public KeyVaultCertificateSecretProvider(IOptions<CertificateSecretResolverOptions> options)
    {
        _options = options.Value ?? new CertificateSecretResolverOptions();
    }

    public CertificateSecretProviderType ProviderType => CertificateSecretProviderType.KeyVault;
    public bool Supports(CertificateStorageMode storageMode) => storageMode == CertificateStorageMode.KeyVaultReference;

    public Task<CertificateSecretResolutionResult> ResolveAsync(CertificateSecretResolutionRequest request, CancellationToken cancellationToken = default)
    {
        var masked = CertificateSecretMasker.Mask(request.SecretRef);
        if (!_options.EnableKeyVaultProvider)
        {
            return Task.FromResult(new CertificateSecretResolutionResult(false, ProviderType, null, "KEYVAULT_PROVIDER_DISABLED", "KeyVault provider deshabilitado.", masked));
        }

        return Task.FromResult(new CertificateSecretResolutionResult(false, ProviderType, null, "KEYVAULT_NOT_CONFIGURED", "KeyVault provider no está configurado en este entorno.", masked));
    }
}

[Scoped]
public class HsmCertificateSecretProvider : ICertificateSecretProvider
{
    private readonly CertificateSecretResolverOptions _options;

    public HsmCertificateSecretProvider(IOptions<CertificateSecretResolverOptions> options)
    {
        _options = options.Value ?? new CertificateSecretResolverOptions();
    }

    public CertificateSecretProviderType ProviderType => CertificateSecretProviderType.Hsm;
    public bool Supports(CertificateStorageMode storageMode) => storageMode == CertificateStorageMode.HsmReference;

    public Task<CertificateSecretResolutionResult> ResolveAsync(CertificateSecretResolutionRequest request, CancellationToken cancellationToken = default)
    {
        var masked = CertificateSecretMasker.Mask(request.SecretRef);
        if (!_options.EnableHsmProvider)
        {
            return Task.FromResult(new CertificateSecretResolutionResult(false, ProviderType, null, "HSM_PROVIDER_DISABLED", "HSM provider deshabilitado.", masked));
        }

        return Task.FromResult(new CertificateSecretResolutionResult(false, ProviderType, null, "HSM_NOT_CONFIGURED", "HSM provider no está configurado en este entorno.", masked));
    }
}

[Scoped]
public class CertificateSecretProviderResolver : ICertificateSecretProviderResolver
{
    private readonly IReadOnlyList<ICertificateSecretProvider> _providers;

    public CertificateSecretProviderResolver(IEnumerable<ICertificateSecretProvider> providers)
    {
        _providers = providers.ToList();
    }

    public ICertificateSecretProvider Resolve(CertificateStorageMode storageMode)
    {
        return _providers.FirstOrDefault(p => p.Supports(storageMode))
               ?? throw new InvalidOperationException($"No provider registrado para storage mode {storageMode}.");
    }
}

[Scoped]
public class CertificateSecretResolver : ICertificateSecretResolver
{
    private readonly ICertificateSecretProviderResolver _providerResolver;
    private readonly CertificateSecretResolverOptions _options;

    public CertificateSecretResolver(
        ICertificateSecretProviderResolver providerResolver,
        IOptions<CertificateSecretResolverOptions> options)
    {
        _providerResolver = providerResolver;
        _options = options.Value ?? new CertificateSecretResolverOptions();
    }

    public async Task<CertificateSecretResolutionResult> ResolveAsync(CertificateSecretResolutionRequest request, CancellationToken cancellationToken = default)
    {
        var provider = _providerResolver.Resolve(request.StorageMode);
        var result = await provider.ResolveAsync(request, cancellationToken);

        if (!result.Success && _options.FailIfSecretProviderUnavailable)
        {
            return result with
            {
                ErrorCode = result.ErrorCode ?? "SECRET_RESOLUTION_FAILED",
                ErrorMessage = result.ErrorMessage ?? "No fue posible resolver el secreto requerido."
            };
        }

        return result;
    }
}
