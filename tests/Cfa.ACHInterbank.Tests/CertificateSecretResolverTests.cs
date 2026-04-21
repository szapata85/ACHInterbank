using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Tests;

public class CertificateSecretResolverTests
{
    [Fact]
    public async Task SecretResolver_ShouldResolveInMemoryPrivateCertificate_ForTestSecretRef()
    {
        var store = new InMemoryCertificateSecretStore();
        var cert = CreateSelfSignedCertificate();
        var pfx = cert.Export(X509ContentType.Pkcs12, "test-pass");
        store.Set("inmem://cm/private/signing", pfx, "test-pass");

        var inMemoryProvider = new InMemoryCertificateSecretProvider(
            store,
            new TestHostEnvironment("Development"),
            Options.Create(new CertificateSecretResolverOptions { EnableInMemoryProvider = true }));

        var resolver = new CertificateSecretResolver(
            new StaticCertificateSecretProviderResolver(inMemoryProvider),
            Options.Create(new CertificateSecretResolverOptions()));

        var result = await resolver.ResolveAsync(new CertificateSecretResolutionRequest(
            1001,
            CertificatePurpose.OutboundSigning,
            CertificateStorageMode.ExternalSecretReference,
            "inmem://cm/private/signing",
            "test"));

        result.Success.Should().BeTrue();
        result.Material.Should().NotBeNull();
        result.Material!.HasPrivateKey.Should().BeTrue();
        result.ProviderType.Should().Be(CertificateSecretProviderType.InMemory);
    }

    [Fact]
    public async Task SecretResolver_ShouldFail_WhenProviderNotConfigured()
    {
        var provider = new KeyVaultCertificateSecretProvider(Options.Create(new CertificateSecretResolverOptions
        {
            EnableKeyVaultProvider = false
        }));

        var resolver = new CertificateSecretResolver(
            new StaticCertificateSecretProviderResolver(provider),
            Options.Create(new CertificateSecretResolverOptions()));

        var result = await resolver.ResolveAsync(new CertificateSecretResolutionRequest(
            1002,
            CertificatePurpose.InboundDecryption,
            CertificateStorageMode.KeyVaultReference,
            "kv://secret/cert-01",
            "test"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("KEYVAULT_PROVIDER_DISABLED");
    }

    [Fact]
    public async Task SecretResolver_ShouldMaskSecretRef_InLogsAndResults()
    {
        var provider = new ExternalSecretReferenceCertificateProvider(Options.Create(new CertificateSecretResolverOptions
        {
            EnableExternalSecretReferenceProvider = true
        }));

        var resolver = new CertificateSecretResolver(
            new StaticCertificateSecretProviderResolver(provider),
            Options.Create(new CertificateSecretResolverOptions()));

        var result = await resolver.ResolveAsync(new CertificateSecretResolutionRequest(
            1003,
            CertificatePurpose.OutboundSigning,
            CertificateStorageMode.ExternalSecretReference,
            "vault://very/long/secret/reference/123456789",
            "test"));

        result.Success.Should().BeFalse();
        result.SecretRefMasked.Should().StartWith("****");
        result.SecretRefMasked.Should().NotContain("vault://very/long/secret/reference");
    }

    private static X509Certificate2 CreateSelfSignedCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=SecretResolverTest", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
    }

    private sealed class StaticCertificateSecretProviderResolver : ICertificateSecretProviderResolver
    {
        private readonly ICertificateSecretProvider _provider;

        public StaticCertificateSecretProviderResolver(ICertificateSecretProvider provider)
        {
            _provider = provider;
        }

        public ICertificateSecretProvider Resolve(CertificateStorageMode storageMode) => _provider;
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
