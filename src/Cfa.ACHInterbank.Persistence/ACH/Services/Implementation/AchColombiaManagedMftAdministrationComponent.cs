using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public sealed class AchColombiaManagedMftAdministrationComponent(
    AchDbContext context,
    IOptions<AchColombiaManagedMftOptions> options)
{
    internal async Task<AchManagedFileTransferConfiguration> GetOrCreateConfigurationEntityAsync(CancellationToken ct)
    {
        var chamber = await context.ClearingHouses.SingleAsync(
            x => x.Code == AchColombiaManagedMftImplementationSupport.ClearingHouseCode, ct);
        var configuration = await context.AchManagedFileTransferConfigurations
            .SingleOrDefaultAsync(x => x.ClearingHouseId == chamber.Id, ct);
        if (configuration is not null) return configuration;

        var defaults = options.Value;
        configuration = new AchManagedFileTransferConfiguration
        {
            ClearingHouseId = chamber.Id,
            ProfileEnabled = defaults.Enabled,
            OutboundLocation = defaults.OutboundPath,
            InboundLocation = defaults.InboundPath,
            ArchiveLocation = defaults.ArchivePath
        };
        context.Add(configuration);
        await context.SaveChangesAsync(ct);
        return configuration;
    }

    public async Task<AchManagedFileTransferConfigurationDto> GetConfigurationAsync(CancellationToken ct = default)
        => Map(await GetOrCreateConfigurationEntityAsync(ct));

    public async Task<AchManagedFileTransferConfigurationDto> UpdateConfigurationAsync(
        AchManagedFileTransferConfigurationDto value,
        string actor,
        CancellationToken ct = default)
    {
        if (value.MaximumRetries is < 0 or > 20 || value.RetentionDays is < 1 or > 3650)
            throw new ArgumentException("Configuración operativa fuera de rango.");

        var entity = await GetOrCreateConfigurationEntityAsync(ct);
        if (entity.ConcurrencyToken != value.ConcurrencyToken)
            throw new DbUpdateConcurrencyException("ACHCOL_MFT_CONFIGURATION_CONFLICT");

        entity.AutomaticOutboundEnabled = value.AutomaticOutboundEnabled;
        entity.AutomaticInboundEnabled = value.AutomaticInboundEnabled;
        entity.ManualOutboundAllowed = value.ManualOutboundAllowed;
        entity.ManualInboundAllowed = value.ManualInboundAllowed;
        entity.MaximumRetries = value.MaximumRetries;
        entity.RetentionDays = value.RetentionDays;
        entity.OutboundLocation = AchColombiaManagedMftImplementationSupport.LimitRequired(value.OutboundLocation, 120);
        entity.InboundLocation = AchColombiaManagedMftImplementationSupport.LimitRequired(value.InboundLocation, 120);
        entity.ArchiveLocation = AchColombiaManagedMftImplementationSupport.LimitRequired(value.ArchiveLocation, 120);
        entity.ConcurrencyToken = Guid.NewGuid();
        await context.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<AchManagedMftAdministrationDto> GetAdministrationAsync(CancellationToken ct = default)
        => MapAdministration(await GetOrCreateConfigurationEntityAsync(ct));

    public async Task<AchManagedMftAdministrationDto> UpdateAdministrationAsync(
        UpdateAchManagedMftAdministrationRequest value,
        string actor,
        CancellationToken ct = default)
    {
        if (value.MaximumRetries is < 0 or > 20 || value.RetryDelaySeconds is < 0 or > 86400 || value.RetentionDays is < 1 or > 3650)
            throw new ArgumentException("Configuración operativa fuera de rango.");
        if (value.Port is < 1 or > 65535) throw new ArgumentException("Puerto inválido.");
        if (value.SftpTimeoutSeconds is < 1 or > 300) throw new ArgumentException("ACHCOL_MFT_SFTP_TIMEOUT_INVALID");
        if (value.ProfileEnabled
            && (string.IsNullOrWhiteSpace(value.OutboundLocation)
                || string.IsNullOrWhiteSpace(value.InboundLocation)
                || string.IsNullOrWhiteSpace(value.ArchiveLocation)))
        {
            throw new ArgumentException("Las rutas operativas son obligatorias para un perfil habilitado.");
        }

        var entity = await GetOrCreateConfigurationEntityAsync(ct);
        if (entity.ConcurrencyToken != value.ConcurrencyToken)
            throw new DbUpdateConcurrencyException("ACHCOL_MFT_CONFIGURATION_CONFLICT");

        var (provider, protocol) = AchColombiaManagedMftImplementationSupport.NormalizeTransport(value.Provider, value.Protocol);
        var endpoint = AchColombiaManagedMftImplementationSupport.Optional(value.Endpoint, 300);
        var principal = AchColombiaManagedMftImplementationSupport.Optional(value.Principal, 160);
        var fingerprint = string.IsNullOrWhiteSpace(value.SftpHostKeyFingerprint)
            ? null
            : AchColombiaSftpHostKeyFingerprint.NormalizeRequired(value.SftpHostKeyFingerprint);

        if (provider == "SftpEndpoint" && value.ProfileEnabled)
        {
            if (endpoint is null || value.Port is null || principal is null)
                throw new ArgumentException("ACHCOL_MFT_SFTP_ENDPOINT_INCOMPLETE");
            if (fingerprint is null) throw new ArgumentException("ACHCOL_MFT_SFTP_HOST_KEY_FINGERPRINT_REQUIRED");
            if (string.IsNullOrWhiteSpace(entity.ProtectedCredential)
                || !string.Equals(entity.CredentialType, "Password", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("ACHCOL_MFT_SFTP_PASSWORD_CREDENTIAL_REQUIRED");
            }
        }

        entity.ProfileName = AchColombiaManagedMftImplementationSupport.LimitRequired(value.ProfileName, 120);
        entity.Provider = provider;
        entity.Protocol = protocol;
        entity.ProfileEnabled = value.ProfileEnabled;
        entity.Endpoint = endpoint;
        entity.Port = value.Port;
        entity.Principal = principal;
        entity.SftpHostKeyFingerprint = fingerprint;
        entity.SftpTimeoutSeconds = value.SftpTimeoutSeconds;
        entity.AutomaticOutboundEnabled = value.AutomaticOutboundEnabled;
        entity.AutomaticInboundEnabled = value.AutomaticInboundEnabled;
        entity.ManualOutboundAllowed = value.ManualOutboundAllowed;
        entity.ManualInboundAllowed = value.ManualInboundAllowed;
        entity.MaximumRetries = value.MaximumRetries;
        entity.RetryDelaySeconds = value.RetryDelaySeconds;
        entity.RetentionDays = value.RetentionDays;
        entity.OutboundLocation = AchColombiaManagedMftImplementationSupport.LimitRequired(value.OutboundLocation, 120);
        entity.InboundLocation = AchColombiaManagedMftImplementationSupport.LimitRequired(value.InboundLocation, 120);
        entity.ArchiveLocation = AchColombiaManagedMftImplementationSupport.LimitRequired(value.ArchiveLocation, 120);
        entity.ConcurrencyToken = Guid.NewGuid();
        await context.SaveChangesAsync(ct);
        return MapAdministration(entity);
    }

    internal static AchManagedMftAdministrationDto MapAdministration(AchManagedFileTransferConfiguration x)
        => new(
            x.ProfileName,
            x.Provider,
            x.Protocol,
            x.ProfileEnabled,
            x.Endpoint,
            x.Port,
            x.Principal,
            x.AutomaticOutboundEnabled,
            x.AutomaticInboundEnabled,
            x.ManualOutboundAllowed,
            x.ManualInboundAllowed,
            x.MaximumRetries,
            x.RetryDelaySeconds,
            x.RetentionDays,
            x.OutboundLocation,
            x.InboundLocation,
            x.ArchiveLocation,
            !string.IsNullOrWhiteSpace(x.ProtectedCredential),
            x.CredentialType,
            x.CredentialUpdatedAtUtc,
            x.ConcurrencyToken)
        {
            SftpHostKeyFingerprint = x.SftpHostKeyFingerprint,
            SftpTimeoutSeconds = x.SftpTimeoutSeconds
        };

    private static AchManagedFileTransferConfigurationDto Map(AchManagedFileTransferConfiguration x)
        => new(
            x.AutomaticOutboundEnabled,
            x.AutomaticInboundEnabled,
            x.ManualOutboundAllowed,
            x.ManualInboundAllowed,
            x.MaximumRetries,
            x.RetentionDays,
            x.OutboundLocation,
            x.InboundLocation,
            x.ArchiveLocation,
            x.ConcurrencyToken);
}
