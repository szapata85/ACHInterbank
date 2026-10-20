using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchColombiaManagedMftAdapter
{
    bool Enabled { get; }
    Task<AchManagedMftResult> HandoffOutboundAsync(string fileName, byte[] content, string contentSha256, CancellationToken ct = default);
    Task<IReadOnlyList<AchManagedMftArtifact>> PickupInboundAsync(CancellationToken ct = default);
    Task<string> ArchiveInboundAsync(AchManagedMftArtifact artifact, CancellationToken ct = default);
}

public interface IAchColombiaManagedMftConfigurationProvider
{
    Task<AchManagedMftEffectiveConfiguration> GetEffectiveAsync(CancellationToken ct = default);
}

public interface IAchColombiaManagedMftAdapterResolver
{
    Task<IAchColombiaManagedMftAdapter> ResolveAsync(CancellationToken ct = default);
}

public interface IAchColombiaManagedMftConnectionTester
{
    Task<AchManagedMftConnectionTestResult> TestAsync(CancellationToken ct = default);
}

public interface IAchColombiaManagedMftExecutionService
{
    Task<AchManagedFileExecutionResult> ExecuteOutboundAsync(string cycleId, AchManagedFileExecutionOrigin origin, string actor, string idempotencyKey,
        CancellationToken ct = default, Guid? correctedFromTransferId = null);
    Task<AchManagedFileExecutionResult> ExecuteInboundAsync(AchManagedFileExecutionOrigin origin, string actor, string idempotencyKey, CancellationToken ct = default);
}

public interface IAchColombiaManagedMftTransferReadService
{
    Task<IReadOnlyList<AchManagedFileTransferSummary>> QueryAsync(AchManagedFileTransferQuery query, CancellationToken ct = default);
    Task<AchManagedFileTransferDetail?> GetAsync(Guid transferId, CancellationToken ct = default);
}

public interface IAchColombiaManagedMftTransferOperationsService
{
    Task<AchManagedFileTransferDetail> RetryAsync(Guid transferId, string actor, string idempotencyKey, CancellationToken ct = default);
    Task<AchManagedFileTransferDetail> ReprocessAsync(Guid transferId, string actor, CancellationToken ct = default);
    Task<AchManagedFileTransferDetail> ArchiveAsync(Guid transferId, string actor, CancellationToken ct = default);
    Task<AchManagedFileTransferDetail> RetireAsync(Guid transferId, string actor, string reason, CancellationToken ct = default);
    Task<AchManagedFileDownload?> DownloadAsync(Guid transferId, string actor, CancellationToken ct = default);
    Task<AchManagedMftConnectionTestResult> TestConnectionAsync(CancellationToken ct = default);
}

public interface IAchColombiaManagedMftAdministrationService
{
    Task<AchManagedFileTransferConfigurationDto> GetConfigurationAsync(CancellationToken ct = default);
    Task<AchManagedFileTransferConfigurationDto> UpdateConfigurationAsync(AchManagedFileTransferConfigurationDto configuration, string actor, CancellationToken ct = default);
    Task<AchManagedMftAdministrationDto> GetAdministrationAsync(CancellationToken ct = default);
    Task<AchManagedMftAdministrationDto> UpdateAdministrationAsync(UpdateAchManagedMftAdministrationRequest request, string actor, CancellationToken ct = default);
}

public interface IAchColombiaManagedMftCredentialAdministrationService
{
    Task<AchManagedMftAdministrationDto> SetCredentialAsync(SetAchManagedMftCredentialRequest request, string actor, CancellationToken ct = default);
}

/// <summary>
/// Compatibility facade retained during the incremental R6 refactor. New consumers must depend on the
/// smallest capability contract they actually use.
/// </summary>
public interface IAchColombiaManagedFileExchangeService :
    IAchColombiaManagedMftExecutionService,
    IAchColombiaManagedMftTransferReadService,
    IAchColombiaManagedMftTransferOperationsService,
    IAchColombiaManagedMftAdministrationService,
    IAchColombiaManagedMftCredentialAdministrationService
{
}
