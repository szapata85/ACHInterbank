using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public sealed class AchColombiaManagedMftRetryReprocessComponent(
    AchDbContext context,
    IAchColombiaManagedMftAdapterResolver mftAdapterResolver,
    AchColombiaManagedMftAdministrationComponent administration,
    AchColombiaManagedMftOutboundWorkflow outboundWorkflow,
    AchColombiaManagedMftInboundWorkflow inboundWorkflow,
    AchColombiaManagedMftQueryProjectionComponent queryProjection)
{
    public async Task<AchManagedFileTransferDetail> RetryAsync(
        Guid transferId,
        string actor,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        AchColombiaManagedMftImplementationSupport.ValidateCommand(actor, idempotencyKey);
        var transfer = await AchColombiaManagedMftImplementationSupport.RequiredAsync(context, transferId, ct);
        if (transfer.Direction != AchManagedFileDirection.Outbound
            || transfer.Status is not (AchManagedFileTransferStatus.RetryPending or AchManagedFileTransferStatus.Uncertain))
        {
            throw new InvalidOperationException("ACHCOL_MFT_RETRY_NOT_ALLOWED");
        }
        if (transfer.RetainedContent is null)
            throw new InvalidOperationException("ACHCOL_MFT_CONTENT_NOT_RETAINED");

        var configuration = await administration.GetOrCreateConfigurationEntityAsync(ct);
        if (transfer.AttemptCount > configuration.MaximumRetries)
            throw new InvalidOperationException("ACHCOL_MFT_RETRIES_EXHAUSTED");
        if (!configuration.ProfileEnabled)
            throw new InvalidOperationException("ACHCOL_MFT_DISABLED");
        if (!AchColombiaManagedMftImplementationSupport.IsEnabled(
                configuration,
                AchManagedFileDirection.Outbound,
                AchManagedFileExecutionOrigin.Manual))
        {
            throw new InvalidOperationException("ACHCOL_MFT_RETRY_NOT_ALLOWED");
        }

        var mftAdapter = await mftAdapterResolver.ResolveAsync(ct);
        await outboundWorkflow.HandoffAsync(
            transfer,
            AchManagedFileExecutionOrigin.Manual,
            actor,
            mftAdapter,
            ct);
        return await queryProjection.MapAsync(
            await AchColombiaManagedMftImplementationSupport.RequiredAsync(context, transferId, ct),
            ct);
    }

    public async Task<AchManagedFileTransferDetail> ReprocessAsync(
        Guid transferId,
        string actor,
        CancellationToken ct = default)
    {
        var transfer = await AchColombiaManagedMftImplementationSupport.RequiredAsync(context, transferId, ct);
        if (transfer.Direction != AchManagedFileDirection.Inbound
            || transfer.IncomingNachaFileIngestionId is null
            || transfer.RetainedContent is null
            || transfer.Status is not (AchManagedFileTransferStatus.Rejected or AchManagedFileTransferStatus.Failed))
        {
            throw new InvalidOperationException("ACHCOL_MFT_REPROCESS_NOT_ALLOWED");
        }

        await inboundWorkflow.ProcessInboundAsync(
            transfer,
            transfer.RetainedContent,
            AchManagedFileExecutionOrigin.Manual,
            actor,
            true,
            transfer.IncomingNachaFileIngestionId,
            ct);
        await context.SaveChangesAsync(ct);
        return await queryProjection.MapAsync(transfer, ct);
    }
}
