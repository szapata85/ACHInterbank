using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACHSobreDigital.ManagedDigitalEnvelope;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public sealed class AchColombiaManagedMftOutboundWorkflow(
    AchDbContext context,
    INachaFileBuilder nachaBuilder,
    IExternalFileNamePolicy externalFileNamePolicy,
    IOperationalTimeSnapshotProvider operationalTimeProvider,
    INachaExportDigitalEnvelopeService digitalEnvelope,
    IAchFileExportAuditService exportAudit,
    IAchColombiaManagedMftAdapterResolver mftAdapterResolver,
    AchColombiaManagedMftAdministrationComponent administration,
    IOperationalCycleWindowResolver windowResolver)
{
    public async Task<AchManagedFileExecutionResult> ExecuteAsync(
        string cycleId,
        AchManagedFileExecutionOrigin origin,
        string actor,
        string idempotencyKey,
        CancellationToken ct = default,
        Guid? correctedFromTransferId = null)
    {
        AchColombiaManagedMftImplementationSupport.ValidateCommand(actor, idempotencyKey);
        var configuration = await administration.GetOrCreateConfigurationEntityAsync(ct);
        if (!AchColombiaManagedMftImplementationSupport.IsEnabled(configuration, AchManagedFileDirection.Outbound, origin))
            return new AchManagedFileExecutionResult(0, 0, 0, []);
        if (!configuration.ProfileEnabled) throw new InvalidOperationException("ACHCOL_MFT_DISABLED");

        var mftAdapter = await mftAdapterResolver.ResolveAsync(ct);
        var cycle = await context.AchCycles.AsNoTracking()
            .Include(x => x.ClearingHouse)
                .ThenInclude(x => x!.ClearingHouseConfig)
            .SingleOrDefaultAsync(
                x => x.Id == cycleId
                    && x.ClearingHouse!.Code == AchColombiaManagedMftImplementationSupport.ClearingHouseCode,
                ct)
            ?? throw new InvalidOperationException("ACHCOL_CYCLE_NOT_FOUND");

        if (origin == AchManagedFileExecutionOrigin.Automatic)
            EnsureAutomaticOutboundInsideSendingWindow(cycle);

        AchManagedFileTransfer? predecessor = null;
        if (correctedFromTransferId.HasValue)
        {
            predecessor = await context.AchManagedFileTransfers.AsNoTracking().SingleOrDefaultAsync(
                    x => x.Id == correctedFromTransferId
                        && x.Direction == AchManagedFileDirection.Outbound
                        && x.AchCycleId == cycleId,
                    ct)
                ?? throw new InvalidOperationException("ACHCOL_MFT_CORRECTION_SOURCE_NOT_FOUND");
            if (predecessor.Status is not (
                    AchManagedFileTransferStatus.Failed
                    or AchManagedFileTransferStatus.Rejected
                    or AchManagedFileTransferStatus.Retired))
            {
                throw new InvalidOperationException("ACHCOL_MFT_CORRECTION_NOT_ALLOWED");
            }
        }

        var existing = await context.AchManagedFileTransfers.Include(x => x.Events)
            .Where(x => x.Direction == AchManagedFileDirection.Outbound
                && x.AchCycleId == cycleId
                && (correctedFromTransferId == null
                    ? x.Status != AchManagedFileTransferStatus.Retired
                    : x.CorrectedFromTransferId == correctedFromTransferId))
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
        if (existing is not null)
        {
            if (existing.Status is AchManagedFileTransferStatus.RetryPending
                    or AchManagedFileTransferStatus.Uncertain
                    or AchManagedFileTransferStatus.InProgress
                && existing.RetainedContent is not null
                && existing.AttemptCount <= configuration.MaximumRetries)
            {
                return await HandoffAsync(existing, origin, actor, mftAdapter, ct);
            }

            return new AchManagedFileExecutionResult(
                1,
                existing.Status is AchManagedFileTransferStatus.Transferred ? 1 : 0,
                existing.Status is AchManagedFileTransferStatus.Failed ? 1 : 0,
                [existing.Id]);
        }

        var built = await nachaBuilder.BuildNachaFileArtifactByCycleAsync(cycleId, ct);
        if (string.IsNullOrWhiteSpace(built.Content)) return new AchManagedFileExecutionResult(0, 0, 0, []);

        var internalName = $"NACHA_{cycle.Id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.tmp";
        var snapshot = operationalTimeProvider.GetOrCreate(
            $"ACHCOL-MFT:{cycle.Id}",
            DateOnly.FromDateTime(cycle.ProcessingDate),
            TimeOnly.FromTimeSpan(cycle.CutoffTime));
        var nameResult = await externalFileNamePolicy.GenerateExternalNameAsync(new ExternalFileNameContext
        {
            ClearingHouseId = cycle.ClearingHouseId,
            ClearingHouseCode = cycle.ClearingHouse!.Code,
            ClearingHouseOriginCode = cycle.ClearingHouse.OriginCode,
            CycleId = cycle.Id,
            CycleName = cycle.CycleName,
            CycleNumber = AchColombiaManagedMftImplementationSupport.ResolveCycleNumber(cycle.CycleName),
            ProcessingDate = snapshot.BogotaTimestamp,
            OperationalTimeSnapshot = snapshot,
            IdempotencyKey = $"NACHA_OUT|CH:{cycle.ClearingHouseId}|CYCLE:{cycle.Id}|CORRECTED_FROM:{correctedFromTransferId?.ToString("N") ?? "NONE"}",
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound,
            InternalFileName = internalName,
            NachaContent = built.Content,
            RequestedBy = actor
        }, ct);
        if (nameResult.Validation.IsHardBlocked)
            throw new InvalidOperationException("ACHCOL_MFT_FILENAME_POLICY_REJECTED");

        var normalized = AchColombiaManagedMftImplementationSupport.NormalizeHeaderIdentifier(
            built.Content,
            nameResult.Components.FileIdModifier);
        var plain = Encoding.ASCII.GetBytes(normalized);
        ManagedDigitalEnvelopeResult envelope;
        try
        {
            envelope = await digitalEnvelope.EncryptAsync(
                cycle.ClearingHouseId,
                nameResult.ExternalFileName,
                plain,
                actor,
                ct);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plain);
        }

        var hash = Convert.ToHexString(SHA256.HashData(envelope.Content));
        await exportAudit.RecordGeneratedFileAsync(
            cycle.Id,
            cycle.ClearingHouseId,
            "NACHA",
            envelope.FileName,
            normalized.Length / 106,
            built.AchTransactionIds.Count,
            true,
            built.AchTransactionIds,
            hash,
            ct);
        var exportId = await context.AchFileExports
            .Where(x => x.AchCycleId == cycle.Id && x.FileName == envelope.FileName && x.IsEncrypted)
            .Select(x => x.Id)
            .SingleAsync(ct);

        var transfer = AchColombiaManagedMftImplementationSupport.NewTransfer(
            cycle.ClearingHouseId,
            AchManagedFileDirection.Outbound,
            envelope.FileName,
            envelope.Content,
            cycle.ProcessingDate.Date,
            cycle.Id,
            origin,
            actor,
            AchColombiaManagedMftImplementationSupport.Limit(idempotencyKey, 160));
        transfer.AchFileExportId = exportId;
        transfer.CorrectedFromTransferId = predecessor?.Id;
        transfer.Status = AchManagedFileTransferStatus.InProgress;
        transfer.ProcessingStartedAtUtc = DateTime.UtcNow;
        AchColombiaManagedMftImplementationSupport.AddEvent(
            transfer, "Generated", "Succeeded", "Archivo NACHA-M generado.", origin, actor);
        AchColombiaManagedMftImplementationSupport.AddEvent(
            transfer, "Encrypted", "Succeeded", "Sobre digital cifrado generado.", origin, actor);
        AchColombiaManagedMftImplementationSupport.AddEvent(
            transfer, "OutboundPrepared", "Succeeded", "Archivo oficial preparado para publicación en la frontera CFA.", origin, actor);
        context.AchManagedFileTransfers.Add(transfer);

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            context.ChangeTracker.Clear();
            var winner = await context.AchManagedFileTransfers.AsNoTracking()
                .SingleOrDefaultAsync(x => x.AchFileExportId == exportId, ct);
            if (winner is null) throw;
            return new AchManagedFileExecutionResult(
                1,
                winner.Status == AchManagedFileTransferStatus.Transferred ? 1 : 0,
                winner.Status is AchManagedFileTransferStatus.Failed or AchManagedFileTransferStatus.Rejected ? 1 : 0,
                [winner.Id]);
        }

        return await HandoffAsync(transfer, origin, actor, mftAdapter, ct);
    }

    internal async Task<AchManagedFileExecutionResult> HandoffAsync(
        AchManagedFileTransfer transfer,
        AchManagedFileExecutionOrigin origin,
        string actor,
        IAchColombiaManagedMftAdapter mftAdapter,
        CancellationToken ct)
    {
        transfer.AttemptCount++;
        transfer.LastAttemptAtUtc = DateTime.UtcNow;
        transfer.Status = AchManagedFileTransferStatus.InProgress;
        AchColombiaManagedMftImplementationSupport.AddEvent(
            transfer,
            "PublicationStarted",
            "Started",
            $"Intento {transfer.AttemptCount} de publicación en la frontera CFA iniciado.",
            origin,
            actor);
        AchColombiaManagedMftImplementationSupport.AddEvent(
            transfer,
            "OutboundAttempt",
            "Started",
            $"Intento {transfer.AttemptCount} de publicación iniciado.",
            origin,
            actor);
        await context.SaveChangesAsync(ct);

        var result = await mftAdapter.HandoffOutboundAsync(
            transfer.PhysicalFileName,
            transfer.RetainedContent!,
            transfer.ContentSha256,
            ct);
        transfer.LastErrorCode = result.Succeeded ? null : result.Code;
        transfer.LastError = result.Succeeded ? null : result.Message;
        transfer.ActiveStorageReference = result.StorageReference;
        transfer.Status = result.Succeeded
            ? AchManagedFileTransferStatus.Transferred
            : result.Uncertain
                ? AchManagedFileTransferStatus.Uncertain
                : result.Retryable
                    ? AchManagedFileTransferStatus.RetryPending
                    : AchManagedFileTransferStatus.Failed;
        transfer.TransferredAtUtc = result.Succeeded ? DateTime.UtcNow : null;
        if (result.Succeeded)
        {
            transfer.ArchivedAtUtc ??= DateTime.UtcNow;
            transfer.ArchiveReference ??= $"retained:{transfer.Id:N}";
        }
        transfer.ConcurrencyToken = Guid.NewGuid();

        if (result.Succeeded)
        {
            AchColombiaManagedMftImplementationSupport.AddEvent(
                transfer,
                "PublishedToMftBoundary",
                "Succeeded",
                "Archivo publicado en la frontera CFA; la recepción por ACH Colombia no está confirmada.",
                origin,
                actor);
            AchColombiaManagedMftImplementationSupport.AddEvent(
                transfer,
                "Archived",
                "Succeeded",
                "Contenido conservado en el archivo operativo.",
                origin,
                actor);
        }
        else if (result.Uncertain)
        {
            AchColombiaManagedMftImplementationSupport.AddEvent(
                transfer,
                "PublicationUncertain",
                "Uncertain",
                "No fue posible confirmar la publicación en la frontera CFA.",
                origin,
                actor);
        }
        else
        {
            AchColombiaManagedMftImplementationSupport.AddEvent(
                transfer,
                "PublicationFailed",
                "Failed",
                "No se pudo publicar el archivo en la frontera CFA.",
                origin,
                actor);
            if (result.Retryable)
            {
                AchColombiaManagedMftImplementationSupport.AddEvent(
                    transfer,
                    "RetryPending",
                    "RetryPending",
                    "La publicación quedó pendiente de reintento.",
                    origin,
                    actor);
            }
        }

        AchColombiaManagedMftImplementationSupport.AddEvent(
            transfer,
            "OutboundAttempt",
            result.Succeeded ? "Succeeded" : result.Uncertain ? "Uncertain" : "Failed",
            result.Message,
            origin,
            actor);
        await context.SaveChangesAsync(ct);
        return new AchManagedFileExecutionResult(1, result.Succeeded ? 1 : 0, result.Succeeded ? 0 : 1, [transfer.Id]);
    }

    private void EnsureAutomaticOutboundInsideSendingWindow(AchCycle cycle)
    {
        var timeZoneId = cycle.ClearingHouse?.ClearingHouseConfig?.TimeZoneId;
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new InvalidOperationException("ACHCOL_OPERATIONAL_TIMEZONE_NOT_CONFIGURED");

        var now = operationalTimeProvider.CaptureNow();
        var currentInstant = new DateTimeOffset(
            DateTime.SpecifyKind(now.CapturedAtUtc, DateTimeKind.Utc),
            TimeSpan.Zero);
        var window = windowResolver.Resolve(
            cycle.ProcessingDate,
            cycle.StartTime,
            cycle.CutoffTime,
            timeZoneId,
            currentInstant);
        if (!window.IsInside)
            throw new InvalidOperationException("ACHCOL_MFT_OUTBOUND_OUTSIDE_CYCLE_WINDOW");
    }
}
