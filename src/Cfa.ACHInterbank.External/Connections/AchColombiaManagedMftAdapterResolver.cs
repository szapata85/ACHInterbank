using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.External.Connections;

public sealed class AchColombiaManagedMftAdapterResolver(
    IAchColombiaManagedMftConfigurationProvider configurationProvider,
    AchColombiaManagedMftFolderAdapter managedFolder,
    AchColombiaSftpMftAdapter sftpEndpoint) : IAchColombiaManagedMftAdapterResolver
{
    public async Task<IAchColombiaManagedMftAdapter> ResolveAsync(CancellationToken ct = default)
    {
        var configuration = await configurationProvider.GetEffectiveAsync(ct);
        if (string.Equals(configuration.Provider, "ManagedFolder", StringComparison.OrdinalIgnoreCase)
            && string.Equals(configuration.Protocol, "ManagedFile", StringComparison.OrdinalIgnoreCase))
            return managedFolder;
        if (string.Equals(configuration.Provider, "SftpEndpoint", StringComparison.OrdinalIgnoreCase)
            && string.Equals(configuration.Protocol, "Sftp", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(configuration.Endpoint) || configuration.Port is not (>= 1 and <= 65535)
                || string.IsNullOrWhiteSpace(configuration.Principal)
                || !string.Equals(configuration.CredentialType, "Password", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(configuration.ProtectedCredential)
                || configuration.SftpTimeoutSeconds is < 1 or > 300)
                throw new InvalidOperationException("ACHCOL_MFT_SFTP_CONFIGURATION_INCOMPLETE");
            try
            {
                _ = AchColombiaSftpHostKeyFingerprint.NormalizeRequired(configuration.SftpHostKeyFingerprint);
            }
            catch (ArgumentException)
            {
                throw new InvalidOperationException("ACHCOL_MFT_SFTP_CONFIGURATION_INCOMPLETE");
            }
            return sftpEndpoint;
        }
        throw new InvalidOperationException("ACHCOL_MFT_TRANSPORT_UNSUPPORTED");
    }
}

/// <summary>
/// Compatibility adapter for callers that still depend directly on the port. New orchestration should
/// resolve once per logical operation through <see cref="IAchColombiaManagedMftAdapterResolver"/>.
/// </summary>
public sealed class AchColombiaManagedMftDispatchAdapter(IAchColombiaManagedMftAdapterResolver resolver)
    : IAchColombiaManagedMftAdapter
{
    public bool Enabled => true;

    public async Task<AchManagedMftResult> HandoffOutboundAsync(string fileName, byte[] content, string contentSha256, CancellationToken ct = default)
        => await (await resolver.ResolveAsync(ct)).HandoffOutboundAsync(fileName, content, contentSha256, ct);

    public async Task<IReadOnlyList<AchManagedMftArtifact>> PickupInboundAsync(CancellationToken ct = default)
        => await (await resolver.ResolveAsync(ct)).PickupInboundAsync(ct);

    public async Task<string> ArchiveInboundAsync(AchManagedMftArtifact artifact, CancellationToken ct = default)
        => await (await resolver.ResolveAsync(ct)).ArchiveInboundAsync(artifact, ct);
}
