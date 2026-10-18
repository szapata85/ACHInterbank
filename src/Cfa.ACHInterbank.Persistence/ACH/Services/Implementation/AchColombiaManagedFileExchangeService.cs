using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Compatibility composition facade for the R6A capability contracts. R6B implementation ownership lives in
/// dedicated workflow/query/operations/administration components; production consumers still resolve the same
/// scoped facade instance through the R6A aliases.
/// </summary>
[Scoped]
public sealed class AchColombiaManagedFileExchangeService(
    AchColombiaManagedMftOutboundWorkflow outboundWorkflow,
    AchColombiaManagedMftInboundWorkflow inboundWorkflow,
    AchColombiaManagedMftRetryReprocessComponent retryReprocess,
    AchColombiaManagedMftLifecycleComponent lifecycle,
    AchColombiaManagedMftQueryProjectionComponent queryProjection,
    AchColombiaManagedMftAdministrationComponent administration,
    AchColombiaManagedMftCredentialComponent credentials,
    IAchColombiaManagedMftConnectionTester connectionTester) : IAchColombiaManagedFileExchangeService
{
    public Task<AchManagedFileExecutionResult> ExecuteOutboundAsync(
        string cycleId,
        AchManagedFileExecutionOrigin origin,
        string actor,
        string idempotencyKey,
        CancellationToken ct = default,
        Guid? correctedFromTransferId = null)
        => outboundWorkflow.ExecuteAsync(cycleId, origin, actor, idempotencyKey, ct, correctedFromTransferId);

    public Task<AchManagedFileExecutionResult> ExecuteInboundAsync(
        AchManagedFileExecutionOrigin origin,
        string actor,
        string idempotencyKey,
        CancellationToken ct = default)
        => inboundWorkflow.ExecuteAsync(origin, actor, idempotencyKey, ct);

    public Task<AchManagedFileTransferDetail> RetryAsync(
        Guid transferId,
        string actor,
        string idempotencyKey,
        CancellationToken ct = default)
        => retryReprocess.RetryAsync(transferId, actor, idempotencyKey, ct);

    public Task<AchManagedFileTransferDetail> ReprocessAsync(
        Guid transferId,
        string actor,
        CancellationToken ct = default)
        => retryReprocess.ReprocessAsync(transferId, actor, ct);

    public Task<AchManagedFileTransferDetail> ArchiveAsync(
        Guid transferId,
        string actor,
        CancellationToken ct = default)
        => lifecycle.ArchiveAsync(transferId, actor, ct);

    public Task<AchManagedFileTransferDetail> RetireAsync(
        Guid transferId,
        string actor,
        string reason,
        CancellationToken ct = default)
        => lifecycle.RetireAsync(transferId, actor, reason, ct);

    public Task<IReadOnlyList<AchManagedFileTransferSummary>> QueryAsync(
        AchManagedFileTransferQuery query,
        CancellationToken ct = default)
        => queryProjection.QueryAsync(query, ct);

    public Task<AchManagedFileTransferDetail?> GetAsync(Guid transferId, CancellationToken ct = default)
        => queryProjection.GetAsync(transferId, ct);

    public Task<AchManagedFileDownload?> DownloadAsync(
        Guid transferId,
        string actor,
        CancellationToken ct = default)
        => lifecycle.DownloadAsync(transferId, actor, ct);

    public Task<AchManagedFileTransferConfigurationDto> GetConfigurationAsync(CancellationToken ct = default)
        => administration.GetConfigurationAsync(ct);

    public Task<AchManagedFileTransferConfigurationDto> UpdateConfigurationAsync(
        AchManagedFileTransferConfigurationDto configuration,
        string actor,
        CancellationToken ct = default)
        => administration.UpdateConfigurationAsync(configuration, actor, ct);

    public Task<AchManagedMftAdministrationDto> GetAdministrationAsync(CancellationToken ct = default)
        => administration.GetAdministrationAsync(ct);

    public Task<AchManagedMftAdministrationDto> UpdateAdministrationAsync(
        UpdateAchManagedMftAdministrationRequest request,
        string actor,
        CancellationToken ct = default)
        => administration.UpdateAdministrationAsync(request, actor, ct);

    public Task<AchManagedMftAdministrationDto> SetCredentialAsync(
        SetAchManagedMftCredentialRequest request,
        string actor,
        CancellationToken ct = default)
        => credentials.SetCredentialAsync(request, actor, ct);

    public Task<AchManagedMftConnectionTestResult> TestConnectionAsync(CancellationToken ct = default)
        => connectionTester.TestAsync(ct);
}
