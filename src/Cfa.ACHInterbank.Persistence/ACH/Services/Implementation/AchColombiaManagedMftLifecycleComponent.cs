using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public sealed class AchColombiaManagedMftLifecycleComponent(
    AchDbContext context,
    AchColombiaManagedMftQueryProjectionComponent queryProjection)
{
    public async Task<AchManagedFileTransferDetail> ArchiveAsync(
        Guid transferId,
        string actor,
        CancellationToken ct = default)
    {
        var transfer = await AchColombiaManagedMftImplementationSupport.RequiredAsync(context, transferId, ct);
        if (transfer.RetiredAtUtc.HasValue) throw new InvalidOperationException("ACHCOL_MFT_ARCHIVE_NOT_ALLOWED");
        transfer.ArchivedAtUtc ??= DateTime.UtcNow;
        transfer.ArchiveReference ??= $"retained:{transfer.Id:N}";
        AchColombiaManagedMftImplementationSupport.AddEvent(
            transfer,
            "Archived",
            "Succeeded",
            "Contenido conservado en el archivo operativo.",
            AchManagedFileExecutionOrigin.Manual,
            actor);
        transfer.ConcurrencyToken = Guid.NewGuid();
        await context.SaveChangesAsync(ct);
        return await queryProjection.MapAsync(transfer, ct);
    }

    public async Task<AchManagedFileTransferDetail> RetireAsync(
        Guid transferId,
        string actor,
        string reason,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("La razón de retiro es obligatoria.", nameof(reason));

        var transfer = await AchColombiaManagedMftImplementationSupport.RequiredAsync(context, transferId, ct);
        if (transfer.Status == AchManagedFileTransferStatus.InProgress)
            throw new InvalidOperationException("ACHCOL_MFT_RETIRE_NOT_ALLOWED");

        transfer.RetainedContent = null;
        transfer.ActiveStorageReference = null;
        transfer.Status = AchManagedFileTransferStatus.Retired;
        transfer.RetiredAtUtc = DateTime.UtcNow;
        transfer.RetiredBy = actor;
        transfer.RetirementReason = AchColombiaManagedMftImplementationSupport.Limit(reason, 500);
        transfer.ConcurrencyToken = Guid.NewGuid();
        AchColombiaManagedMftImplementationSupport.AddEvent(
            transfer,
            "Retired",
            "Succeeded",
            "Archivo retirado del almacenamiento activo; historial conservado.",
            AchManagedFileExecutionOrigin.Manual,
            actor);
        await context.SaveChangesAsync(ct);
        return await queryProjection.MapAsync(transfer, ct);
    }

    public async Task<AchManagedFileDownload?> DownloadAsync(
        Guid transferId,
        string actor,
        CancellationToken ct = default)
    {
        var transfer = await AchColombiaManagedMftImplementationSupport.RequiredAsync(context, transferId, ct);
        if (transfer.RetainedContent is null) return null;
        AchColombiaManagedMftImplementationSupport.AddEvent(
            transfer,
            "Downloaded",
            "Succeeded",
            "Archivo descargado por un operador autorizado.",
            AchManagedFileExecutionOrigin.Manual,
            actor);
        await context.SaveChangesAsync(ct);
        return new AchManagedFileDownload(transfer.PhysicalFileName, "application/octet-stream", transfer.RetainedContent);
    }

    internal async Task<bool> TryArchiveInboundAfterPersistedResultAsync(
        AchManagedFileTransfer transfer,
        AchManagedMftArtifact artifact,
        IAchColombiaManagedMftAdapter mftAdapter,
        AchManagedFileExecutionOrigin origin,
        string actor,
        CancellationToken ct)
    {
        try
        {
            await ArchiveInboundAfterPersistedResultAsync(transfer, artifact, mftAdapter, origin, actor, ct);
            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            AchColombiaManagedMftImplementationSupport.AddEvent(
                transfer,
                "InboundArchivePending",
                "RetryPending",
                "El resultado fue guardado, pero el archivo quedó pendiente de recuperación para archivado.",
                origin,
                actor);
            transfer.ConcurrencyToken = Guid.NewGuid();
            await context.SaveChangesAsync(ct);
            return false;
        }
    }

    private async Task ArchiveInboundAfterPersistedResultAsync(
        AchManagedFileTransfer transfer,
        AchManagedMftArtifact artifact,
        IAchColombiaManagedMftAdapter mftAdapter,
        AchManagedFileExecutionOrigin origin,
        string actor,
        CancellationToken ct)
    {
        if (!AchColombiaManagedMftImplementationSupport.HasPersistedInboundResult(transfer))
            throw new InvalidOperationException("ACHCOL_MFT_INBOUND_RESULT_NOT_PERSISTED");

        var archiveReference = await mftAdapter.ArchiveInboundAsync(artifact, ct);
        transfer.ArchiveReference ??= archiveReference;
        transfer.ArchivedAtUtc ??= DateTime.UtcNow;
        transfer.ActiveStorageReference = null;
        AchColombiaManagedMftImplementationSupport.AddEvent(
            transfer,
            "Archived",
            "Succeeded",
            "Archivo retirado del área de recepción y conservado en archivo.",
            origin,
            actor);
        transfer.ConcurrencyToken = Guid.NewGuid();
        await context.SaveChangesAsync(ct);
    }
}
