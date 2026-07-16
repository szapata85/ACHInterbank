using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;

public sealed class CertificateBootstrapHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DigitalEnvelopeCertificateBootstrapOptions _options;
    private readonly ILogger<CertificateBootstrapHostedService> _logger;
    private int _clearingHouseId;

    public CertificateBootstrapHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<DigitalEnvelopeCertificateBootstrapOptions> options,
        ILogger<CertificateBootstrapHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _clearingHouseId = _options.ClearingHouseId;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Certificate bootstrap is disabled.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.DirectoryPath) || !Directory.Exists(_options.DirectoryPath))
        {
            _logger.LogWarning("Certificate bootstrap directory is unavailable. Directory={Directory}", _options.DirectoryPath);
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AchDbContext>();
        if (!await context.Database.CanConnectAsync(cancellationToken))
        {
            _logger.LogWarning("Certificate bootstrap skipped because the database is unavailable.");
            return;
        }

        var loadService = scope.ServiceProvider.GetRequiredService<ICertificateLoadService>();
        var activationService = scope.ServiceProvider.GetRequiredService<ICertificateActivationService>();
        _clearingHouseId = await EnsureClearingHouseAsync(context, cancellationToken);

        await BootstrapPublicAsync(context, loadService, activationService, cancellationToken);
        await BootstrapPrivateAsync(
            context,
            loadService,
            activationService,
            "CFA-OUTBOUND-SIGNING",
            "CFA - Firma de salida",
            CertificatePurpose.OutboundSigning,
            cancellationToken);
        await BootstrapPrivateAsync(
            context,
            loadService,
            activationService,
            "CFA-INBOUND-DECRYPTION",
            "CFA - Descifrado de entrada",
            CertificatePurpose.InboundDecryption,
            cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task BootstrapPublicAsync(
        AchDbContext context,
        ICertificateLoadService loadService,
        ICertificateActivationService activationService,
        CancellationToken cancellationToken)
    {
        var path = SafeCombine(_options.DirectoryPath, _options.PublicCertificateFileName);
        if (!File.Exists(path))
        {
            _logger.LogWarning("Public certificate bootstrap file is unavailable. FileName={FileName}", _options.PublicCertificateFileName);
            return;
        }

        var raw = await File.ReadAllBytesAsync(path, cancellationToken);
        try
        {
            using var certificate = X509CertificateLoader.LoadCertificate(raw);
            var version = await FindExistingAsync(
                context,
                certificate,
                CertificatePurpose.OutboundEncryption,
                CertificateHolderType.ClearingHouse,
                cancellationToken);
            if (version is null)
            {
                var created = await loadService.LoadPublicCertificateAsync(
                    new LoadPublicCertificateRequest(
                        "ACHCOL-OUTBOUND-ENCRYPTION",
                        "ACH Colombia - Cifrado de salida",
                        _clearingHouseId,
                        _options.Environment,
                        CertificatePurpose.OutboundEncryption,
                        CertificateHolderType.ClearingHouse,
                        raw,
                        "certificate-bootstrap",
                        Path.GetFileName(path)),
                    cancellationToken);
                version = await context.DigitalCertificateVersions.FirstAsync(x => x.Id == created.Id, cancellationToken);
            }

            await EnsureActiveAsync(version, activationService, cancellationToken);
            _logger.LogInformation(
                "Public certificate bootstrap ready. CertificateVersionId={CertificateVersionId} Thumbprint={Thumbprint}",
                version.Id,
                MaskThumbprint(version.Thumbprint));
        }
        catch (Exception ex) when (ex is CryptographicException or CertificateValidationException or CertificateConflictException)
        {
            _logger.LogWarning("Public certificate bootstrap failed validation. ErrorType={ErrorType}", ex.GetType().Name);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(raw);
        }
    }

    private async Task BootstrapPrivateAsync(
        AchDbContext context,
        ICertificateLoadService loadService,
        ICertificateActivationService activationService,
        string code,
        string displayName,
        CertificatePurpose purpose,
        CancellationToken cancellationToken)
    {
        var path = SafeCombine(_options.DirectoryPath, _options.PrivateCertificateFileName);
        if (!File.Exists(path))
        {
            _logger.LogWarning("Private certificate bootstrap file is unavailable. FileName={FileName} Purpose={Purpose}", _options.PrivateCertificateFileName, purpose);
            return;
        }

        if (string.IsNullOrEmpty(_options.PfxPassword))
        {
            var existingWithoutPassword = await context.DigitalCertificateVersions
                .FirstOrDefaultAsync(x => x.ClearingHouseId == _clearingHouseId
                                          && x.Environment == _options.Environment
                                          && x.Purpose == purpose
                                          && x.HolderType == CertificateHolderType.Participant
                                          && x.HasPrivateKey,
                    cancellationToken);
            if (existingWithoutPassword is not null)
            {
                await EnsureActiveAsync(existingWithoutPassword, activationService, cancellationToken);
                _logger.LogInformation(
                    "Existing private certificate bootstrap record retained. CertificateVersionId={CertificateVersionId} Purpose={Purpose} Thumbprint={Thumbprint}",
                    existingWithoutPassword.Id,
                    purpose,
                    MaskThumbprint(existingWithoutPassword.Thumbprint));
                return;
            }

            _logger.LogWarning(
                "Private certificate bootstrap skipped because the PFX password secret is unavailable. Purpose={Purpose}",
                purpose);
            return;
        }

        var raw = await File.ReadAllBytesAsync(path, cancellationToken);
        try
        {
            using var certificate = X509CertificateLoader.LoadPkcs12(
                raw,
                _options.PfxPassword,
                X509KeyStorageFlags.EphemeralKeySet);
            var version = await FindExistingAsync(
                context,
                certificate,
                purpose,
                CertificateHolderType.Participant,
                cancellationToken);
            if (version is null)
            {
                var created = await loadService.RegisterPrivateCertificateAsync(
                    new RegisterPrivateCertificateRequest(
                        code,
                        displayName,
                        _clearingHouseId,
                        _options.Environment,
                        purpose,
                        CertificateHolderType.Participant,
                        raw,
                        _options.PfxPassword,
                        "certificate-bootstrap",
                        CertificateStorageMode.DatabaseEncrypted,
                        null,
                        Path.GetFileName(path)),
                    cancellationToken);
                version = await context.DigitalCertificateVersions.FirstAsync(x => x.Id == created.Id, cancellationToken);
            }

            await EnsureActiveAsync(version, activationService, cancellationToken);
            _logger.LogInformation(
                "Private certificate bootstrap ready. CertificateVersionId={CertificateVersionId} Purpose={Purpose} Thumbprint={Thumbprint}",
                version.Id,
                purpose,
                MaskThumbprint(version.Thumbprint));
        }
        catch (Exception ex) when (ex is CryptographicException or CertificateValidationException or CertificateConflictException)
        {
            _logger.LogWarning(
                "Private certificate bootstrap failed validation. Purpose={Purpose} ErrorType={ErrorType}",
                purpose,
                ex.GetType().Name);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(raw);
        }
    }

    private async Task<DigitalCertificateVersion?> FindExistingAsync(
        AchDbContext context,
        X509Certificate2 certificate,
        CertificatePurpose purpose,
        CertificateHolderType holderType,
        CancellationToken cancellationToken)
    {
        var fingerprint = Convert.ToHexString(SHA256.HashData(certificate.RawData));
        var thumbprint = certificate.Thumbprint ?? string.Empty;
        return await context.DigitalCertificateVersions
            .FirstOrDefaultAsync(x => x.ClearingHouseId == _clearingHouseId
                                      && x.Environment == _options.Environment
                                      && x.Purpose == purpose
                                      && x.HolderType == holderType
                                      && (x.FingerprintSha256 == fingerprint || x.Thumbprint == thumbprint),
                cancellationToken);
    }

    private async Task<int> EnsureClearingHouseAsync(AchDbContext context, CancellationToken cancellationToken)
    {
        var code = _options.ClearingHouseCode.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Certificate bootstrap clearing-house code is required.");

        var existing = await context.ClearingHouses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
        if (existing is not null) return existing.Id;

        var config = await context.ClearingHouseConfigs
                         .FirstOrDefaultAsync(x => x.Id == _options.ClearingHouseId, cancellationToken)
                     ?? await context.ClearingHouseConfigs.FirstOrDefaultAsync(cancellationToken);
        if (config is null)
        {
            config = new ClearingHouseConfig
            {
                ClearingHouseId = _options.ClearingHouseId,
                HolidayStrategy = "Colombian"
            };
            context.ClearingHouseConfigs.Add(config);
            await context.SaveChangesAsync(cancellationToken);
        }

        var clearingHouse = new ClearingHouse
        {
            Code = code,
            Name = _options.ClearingHouseName.Trim(),
            OriginCode = _options.ClearingHouseOriginCode.Trim(),
            ClearingHouseId = config.Id
        };
        context.ClearingHouses.Add(clearingHouse);
        await context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Certificate bootstrap created missing clearing-house reference. ClearingHouseId={ClearingHouseId} Code={Code}",
            clearingHouse.Id,
            clearingHouse.Code);
        return clearingHouse.Id;
    }

    private static async Task EnsureActiveAsync(
        DigitalCertificateVersion version,
        ICertificateActivationService activationService,
        CancellationToken cancellationToken)
    {
        if (version.Status == CertificateStatus.Active) return;
        await activationService.ActivateVersionAsync(
            new ActivateCertificateVersionRequest(version.Id, "certificate-bootstrap"),
            cancellationToken);
    }

    private static string SafeCombine(string directory, string fileName)
    {
        var safeFileName = Path.GetFileName(fileName);
        if (!string.Equals(safeFileName, fileName, StringComparison.Ordinal))
            throw new InvalidOperationException("Certificate bootstrap file name must not contain a path.");
        return Path.Combine(directory, safeFileName);
    }

    private static string MaskThumbprint(string thumbprint)
        => string.IsNullOrWhiteSpace(thumbprint) || thumbprint.Length <= 12
            ? "****"
            : $"{thumbprint[..6]}...{thumbprint[^6..]}";
}
