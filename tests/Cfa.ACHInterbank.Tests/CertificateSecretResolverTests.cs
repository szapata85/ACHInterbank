using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.Net.Http;
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

    [Fact]
    public async Task OpenBaoProvider_ShouldFail_WhenSecretRefSchemeInvalid()
    {
        var provider = new OpenBaoCertificateSecretProvider(
            new StaticHttpClientFactory(new HttpClient(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)))),
            Options.Create(new OpenBaoOptions { Enabled = true, ApiToken = "token", BaseUrl = "http://openbao:8200" }));

        var result = await provider.ResolveAsync(new CertificateSecretResolutionRequest(
            2001,
            CertificatePurpose.OutboundSigning,
            CertificateStorageMode.OpenBaoReference,
            "vault://certificates/test/ch-1/outboundsigning/v1",
            "test"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("OPENBAO_SECRETREF_INVALID");
    }

    [Fact]
    public async Task OpenBaoProvider_ShouldFail_WhenBackendUnavailable()
    {
        var provider = new OpenBaoCertificateSecretProvider(
            new StaticHttpClientFactory(new HttpClient(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)))),
            Options.Create(new OpenBaoOptions { Enabled = true, ApiToken = "token", BaseUrl = "http://openbao:8200" }));

        var result = await provider.ResolveAsync(new CertificateSecretResolutionRequest(
            2002,
            CertificatePurpose.OutboundSigning,
            CertificateStorageMode.OpenBaoReference,
            "openbao://certificates/test/ch-1/outboundsigning/v1",
            "test"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("OPENBAO_READ_FAILED");
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

    private sealed class StaticHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public StaticHttpClientFactory(HttpClient client) => _client = client;
        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class StubHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _factory;
        public StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> factory) => _factory = factory;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_factory(request));
    }
}
