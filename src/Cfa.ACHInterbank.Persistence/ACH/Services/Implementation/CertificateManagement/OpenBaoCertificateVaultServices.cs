using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;

internal sealed record OpenBaoKvV2WriteRequest(OpenBaoKvV2WritePayload Data);
internal sealed record OpenBaoKvV2WritePayload(string Pkcs12Base64, string Password);
internal sealed record OpenBaoKvV2ReadResponse(OpenBaoKvV2ReadData? Data);
internal sealed record OpenBaoKvV2ReadData(OpenBaoKvV2SecretData? Data);
internal sealed record OpenBaoKvV2SecretData(string? Pkcs12Base64, string? Password);

[Scoped]
public sealed class OpenBaoCertificatePrivateMaterialStore : ICertificatePrivateMaterialStore
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenBaoOptions _options;

    public OpenBaoCertificatePrivateMaterialStore(
        IHttpClientFactory httpClientFactory,
        IOptions<OpenBaoOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value ?? new OpenBaoOptions();
    }

    public async Task<CertificatePrivateMaterialStoreResult> StorePkcs12Async(
        CertificatePrivateMaterialStoreRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            throw new InvalidOperationException("OpenBao está deshabilitado para este entorno.");
        var token = ResolveToken();
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("OpenBao token no configurado.");

        var environment = request.Environment.Trim().ToLowerInvariant();
        var purpose = request.Purpose.Trim().ToLowerInvariant().Replace('_', '-');
        var path = $"{_options.CertificatesPrefix.Trim('/')}/{environment}/ch-{request.ClearingHouseId}/{purpose}/v{request.Version}";

        using var client = _httpClientFactory.CreateClient(nameof(OpenBaoCertificatePrivateMaterialStore));
        client.BaseAddress = new Uri(_options.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds));
        client.DefaultRequestHeaders.Add("X-Vault-Token", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var writeBody = new OpenBaoKvV2WriteRequest(new OpenBaoKvV2WritePayload(Convert.ToBase64String(request.RawPkcs12), request.Password));
        var response = await client.PostAsJsonAsync($"/v1/{_options.KvMount.Trim('/')}/data/{path}", writeBody, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"OpenBao write falló: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
        }

        var secretRef = $"openbao://{path}";
        return new CertificatePrivateMaterialStoreResult(secretRef, CertificateSecretMasker.Mask(secretRef), "openbao");
    }

    private string ResolveToken()
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiToken))
            return _options.ApiToken;

        if (!string.IsNullOrWhiteSpace(_options.ApiTokenFilePath) && File.Exists(_options.ApiTokenFilePath))
            return File.ReadAllText(_options.ApiTokenFilePath).Trim();

        return string.Empty;
    }
}

[Scoped]
public class OpenBaoCertificateSecretProvider : ICertificateSecretProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenBaoOptions _options;

    public OpenBaoCertificateSecretProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<OpenBaoOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value ?? new OpenBaoOptions();
    }

    public CertificateSecretProviderType ProviderType => CertificateSecretProviderType.OpenBao;
    public bool Supports(CertificateStorageMode storageMode) => storageMode == CertificateStorageMode.OpenBaoReference;

    public async Task<CertificateSecretResolutionResult> ResolveAsync(CertificateSecretResolutionRequest request, CancellationToken cancellationToken = default)
    {
        var masked = CertificateSecretMasker.Mask(request.SecretRef);
        var token = ResolveToken();
        if (!_options.Enabled)
            return new CertificateSecretResolutionResult(false, ProviderType, null, "OPENBAO_DISABLED", "OpenBao está deshabilitado.", masked);
        if (string.IsNullOrWhiteSpace(token))
            return new CertificateSecretResolutionResult(false, ProviderType, null, "OPENBAO_TOKEN_MISSING", "OpenBao token no configurado.", masked);
        if (!request.SecretRef.StartsWith("openbao://", StringComparison.OrdinalIgnoreCase))
            return new CertificateSecretResolutionResult(false, ProviderType, null, "OPENBAO_SECRETREF_INVALID", "SecretRef no usa esquema openbao://.", masked);

        var path = request.SecretRef["openbao://".Length..].Trim('/');
        using var client = _httpClientFactory.CreateClient(nameof(OpenBaoCertificateSecretProvider));
        client.BaseAddress = new Uri(_options.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds));
        client.DefaultRequestHeaders.Add("X-Vault-Token", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await client.GetAsync($"/v1/{_options.KvMount.Trim('/')}/data/{path}", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new CertificateSecretResolutionResult(false, ProviderType, null, "OPENBAO_READ_FAILED", $"OpenBao read falló ({(int)response.StatusCode}).", masked);

        var payload = await response.Content.ReadFromJsonAsync<OpenBaoKvV2ReadResponse>(cancellationToken: cancellationToken);
        var pfxBase64 = payload?.Data?.Data?.Pkcs12Base64;
        if (string.IsNullOrWhiteSpace(pfxBase64))
            return new CertificateSecretResolutionResult(false, ProviderType, null, "OPENBAO_SECRET_EMPTY", "Secreto OpenBao vacío.", masked);

        try
        {
            var raw = Convert.FromBase64String(pfxBase64);
            var cert = X509CertificateLoader.LoadPkcs12(raw, payload?.Data?.Data?.Password, X509KeyStorageFlags.EphemeralKeySet);
            var resolved = new CertificateSecretMaterial(cert, cert.Thumbprint ?? string.Empty, cert.SerialNumber, cert.Subject, cert.HasPrivateKey);
            return new CertificateSecretResolutionResult(true, ProviderType, resolved, null, null, masked);
        }
        catch
        {
            return new CertificateSecretResolutionResult(false, ProviderType, null, "OPENBAO_SECRET_INVALID", "PKCS#12 inválido en OpenBao.", masked);
        }
    }

    private string ResolveToken()
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiToken))
            return _options.ApiToken;

        if (!string.IsNullOrWhiteSpace(_options.ApiTokenFilePath) && File.Exists(_options.ApiTokenFilePath))
            return File.ReadAllText(_options.ApiTokenFilePath).Trim();

        return string.Empty;
    }
}
