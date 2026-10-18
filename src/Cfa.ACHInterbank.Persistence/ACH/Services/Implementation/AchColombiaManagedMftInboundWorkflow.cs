using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public sealed class AchColombiaManagedMftInboundWorkflow(
    AchDbContext context,
    IIncomingNachaIngestionAppService ingestionService,
    IAchColombiaManagedMftAdapterResolver mftAdapterResolver,
    AchColombiaManagedMftAdministrationComponent administration,
    AchColombiaManagedMftLifecycleComponent lifecycle,
    IOperationalCycleWindowResolver windowResolver,
    TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<AchManagedFileExecutionResult> ExecuteAsync(
        AchManagedFileExecutionOrigin origin,
        string actor,
        string idempotencyKey,
        CancellationToken ct = default)
    {
        AchColombiaManagedMftImplementationSupport.ValidateCommand(actor, idempotencyKey);
        var configuration = await administration.GetOrCreateConfigurationEntityAsync(ct);
        if (!AchColombiaManagedMftImplementationSupport.IsEnabled(configuration, AchManagedFileDirection.Inbound, origin))
            return new AchManagedFileExecutionResult(0, 0, 0, []);
        if (!configuration.ProfileEnabled) throw new InvalidOperationException("ACHCOL_MFT_DISABLED");

        var chamberId = configuration.ClearingHouseId;
        var mftAdapter = await mftAdapterResolver.ResolveAsync(ct);
        var artifacts = await mftAdapter.PickupInboundAsync(ct);
        var ids = new List<Guid>();
        var succeeded = 0;
        var failed = 0;
        var considered = 0;

        foreach (var artifact in artifacts)
        {
            ct.ThrowIfCancellationRequested();

            // Los adapters productivos sólo reclaman sobres finales OUT/RET.
            // Esta defensa conserva el contrato si una implementación futura lo viola.
            if (!AchColombiaManagedMftImplementationSupport.IsAuthorizedInboundEnvelopeName(artifact.FileName))
                continue;

            considered++;
            var duplicate = await context.AchManagedFileTransfers.Include(x => x.Events)
                .SingleOrDefaultAsync(x => x.Direction == AchManagedFileDirection.Inbound
                    && x.ContentSha256 == artifact.ContentSha256
                    && x.FileSize == artifact.Content.LongLength, ct);

            if (duplicate is not null)
            {
                if (AchColombiaManagedMftImplementationSupport.HasPersistedInboundResult(duplicate)
                    && duplicate.ArchivedAtUtc is null
                    && string.Equals(duplicate.ActiveStorageReference, artifact.ClaimReference, StringComparison.Ordinal))
                {
                    AchColombiaManagedMftImplementationSupport.AddEvent(
                        duplicate,
                        "InboundRecovery",
                        "Started",
                        "Se reanudó el archivado de una recepción con resultado persistido.",
                        origin,
                        actor);
                    duplicate.ConcurrencyToken = Guid.NewGuid();
                    await context.SaveChangesAsync(ct);
                    await RegisterInboundWindowObservationAsync(duplicate, artifact, origin, actor, ct);
                    if (!await lifecycle.TryArchiveInboundAfterPersistedResultAsync(duplicate, artifact, mftAdapter, origin, actor, ct))
                    {
                        failed++;
                    }
                    else if (duplicate.Status is AchManagedFileTransferStatus.Processed or AchManagedFileTransferStatus.Duplicate)
                    {
                        succeeded++;
                    }
                    else
                    {
                        failed++;
                    }

                    ids.Add(duplicate.Id);
                    continue;
                }

                if (duplicate.IncomingNachaFileIngestionId is null
                    && duplicate.Status is AchManagedFileTransferStatus.Received
                        or AchManagedFileTransferStatus.InProgress
                        or AchManagedFileTransferStatus.RetryPending)
                {
                    if (!IsInboundRecoveryDue(duplicate, configuration))
                    {
                        ids.Add(duplicate.Id);
                        continue;
                    }

                    if (!await TryAcquireInboundRecoveryAsync(duplicate, ct))
                    {
                        ids.Add(duplicate.Id);
                        continue;
                    }

                    AchColombiaManagedMftImplementationSupport.AddEvent(
                        duplicate,
                        "InboundRecovery",
                        "Started",
                        "Se reanudó una recepción interrumpida.",
                        origin,
                        actor);
                    await context.SaveChangesAsync(ct);

                    try
                    {
                        await ProcessInboundAsync(
                            duplicate,
                            duplicate.RetainedContent ?? artifact.Content,
                            origin,
                            actor,
                            false,
                            null,
                            ct);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch
                    {
                        await MarkInboundRecoveryPendingAsync(duplicate, origin, actor, ct);
                        failed++;
                        ids.Add(duplicate.Id);
                        continue;
                    }

                    await RegisterInboundWindowObservationAsync(duplicate, artifact, origin, actor, ct);
                    if (!await lifecycle.TryArchiveInboundAfterPersistedResultAsync(duplicate, artifact, mftAdapter, origin, actor, ct))
                        failed++;
                    else if (duplicate.Status is AchManagedFileTransferStatus.Processed or AchManagedFileTransferStatus.Duplicate)
                        succeeded++;
                    else
                        failed++;
                }
                else
                {
                    AchColombiaManagedMftImplementationSupport.AddEvent(
                        duplicate,
                        "DuplicateDetected",
                        "Ignored",
                        $"Contenido repetido recibido como {artifact.FileName}.",
                        origin,
                        actor);
                    AchColombiaManagedMftImplementationSupport.AddEvent(
                        duplicate,
                        "Duplicate",
                        "Duplicate",
                        "Archivo duplicado identificado; no se creó un efecto funcional adicional.",
                        origin,
                        actor);
                    duplicate.ConcurrencyToken = Guid.NewGuid();
                    await context.SaveChangesAsync(ct);
                    if (!await lifecycle.TryArchiveInboundAfterPersistedResultAsync(duplicate, artifact, mftAdapter, origin, actor, ct))
                        failed++;
                }

                ids.Add(duplicate.Id);
                continue;
            }

            var sameName = await context.AchManagedFileTransfers.AnyAsync(
                x => x.Direction == AchManagedFileDirection.Inbound && x.PhysicalFileName == artifact.FileName,
                ct);
            var transfer = AchColombiaManagedMftImplementationSupport.NewTransfer(
                chamberId,
                AchManagedFileDirection.Inbound,
                artifact.FileName,
                artifact.Content,
                _timeProvider.GetUtcNow().UtcDateTime.Date,
                null,
                origin,
                actor,
                AchColombiaManagedMftImplementationSupport.Limit($"{idempotencyKey}:{artifact.ContentSha256}", 160));
            transfer.Status = sameName ? AchManagedFileTransferStatus.Rejected : AchManagedFileTransferStatus.Received;
            transfer.ActiveStorageReference = artifact.ClaimReference;
            AchColombiaManagedMftImplementationSupport.AddEvent(
                transfer,
                "ReceivedFromMftBoundary",
                "Succeeded",
                "Archivo recibido desde la frontera CFA.",
                origin,
                actor);
            AchColombiaManagedMftImplementationSupport.AddEvent(
                transfer,
                "InboundClaimed",
                "Succeeded",
                "Archivo recibido y reclamado de forma exclusiva en la frontera CFA.",
                origin,
                actor);
            context.AchManagedFileTransfers.Add(transfer);

            try
            {
                await context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                context.ChangeTracker.Clear();
                var winner = await context.AchManagedFileTransfers.Include(x => x.Events)
                    .SingleOrDefaultAsync(x => x.Direction == AchManagedFileDirection.Inbound
                        && x.ContentSha256 == artifact.ContentSha256
                        && x.FileSize == artifact.Content.LongLength, ct);
                if (winner is null) throw;

                // Otra instancia ganó la identidad funcional. No se archiva un claim
                // mientras la ganadora aún no haya persistido un resultado terminal.
                if (AchColombiaManagedMftImplementationSupport.HasPersistedInboundResult(winner))
                {
                    AchColombiaManagedMftImplementationSupport.AddEvent(
                        winner,
                        "DuplicateDetected",
                        "Ignored",
                        $"Recepción concurrente descartada: {artifact.FileName}.",
                        origin,
                        actor);
                    AchColombiaManagedMftImplementationSupport.AddEvent(
                        winner,
                        "Duplicate",
                        "Duplicate",
                        "Archivo duplicado identificado; no se creó un efecto funcional adicional.",
                        origin,
                        actor);
                    winner.ConcurrencyToken = Guid.NewGuid();
                    await context.SaveChangesAsync(ct);
                    await lifecycle.TryArchiveInboundAfterPersistedResultAsync(winner, artifact, mftAdapter, origin, actor, ct);
                }

                ids.Add(winner.Id);
                continue;
            }

            ids.Add(transfer.Id);
            if (sameName)
            {
                transfer.LastErrorCode = "ACHCOL_MFT_SAME_NAME_DIFFERENT_CONTENT";
                transfer.LastError = "El nombre ya fue recibido con contenido diferente.";
                AchColombiaManagedMftImplementationSupport.AddEvent(
                    transfer,
                    "InboundRejected",
                    "Rejected",
                    transfer.LastError,
                    origin,
                    actor);
                AchColombiaManagedMftImplementationSupport.AddEvent(
                    transfer,
                    "Rejected",
                    "Rejected",
                    "Archivo rechazado durante el procesamiento.",
                    origin,
                    actor);
                transfer.ConcurrencyToken = Guid.NewGuid();
                await context.SaveChangesAsync(ct);
                failed++;
                await lifecycle.TryArchiveInboundAfterPersistedResultAsync(transfer, artifact, mftAdapter, origin, actor, ct);
                continue;
            }

            try
            {
                await ProcessInboundAsync(transfer, artifact.Content, origin, actor, false, null, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                await MarkInboundRecoveryPendingAsync(transfer, origin, actor, ct);
                failed++;
                continue;
            }

            await RegisterInboundWindowObservationAsync(transfer, artifact, origin, actor, ct);
            if (!await lifecycle.TryArchiveInboundAfterPersistedResultAsync(transfer, artifact, mftAdapter, origin, actor, ct))
                failed++;
            else if (transfer.Status is AchManagedFileTransferStatus.Processed or AchManagedFileTransferStatus.Duplicate)
                succeeded++;
            else
                failed++;
        }

        return new AchManagedFileExecutionResult(considered, succeeded, failed, ids);
    }

    internal async Task ProcessInboundAsync(
        AchManagedFileTransfer transfer,
        byte[] content,
        AchManagedFileExecutionOrigin origin,
        string actor,
        bool reprocess,
        Guid? parentId,
        CancellationToken ct)
    {
        transfer.AttemptCount++;
        transfer.LastAttemptAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        transfer.Status = AchManagedFileTransferStatus.InProgress;
        AchColombiaManagedMftImplementationSupport.AddEvent(
            transfer,
            reprocess ? "ReprocessStarted" : "InboundProcessingStarted",
            "Started",
            "Procesamiento NACHA-M iniciado.",
            origin,
            actor);
        await context.SaveChangesAsync(ct);

        await using var stream = new MemoryStream(content, false);
        var result = await ingestionService.IngestAsync(new IncomingNachaIngestionRequest
        {
            FileStream = stream,
            FileName = transfer.PhysicalFileName,
            ContentType = "application/octet-stream",
            RequestedBy = actor,
            CorrelationId = transfer.CorrelationId,
            RequestedClearingHouseId = transfer.ClearingHouseId,
            ForceReprocess = reprocess,
            ParentIngestionId = parentId
        }, ct);

        transfer.IncomingNachaFileIngestionId = result.IngestionId;
        transfer.AchCycleId = result.ResolvedAchCycleId;
        transfer.OperationalDate = result.OperationalDate?.Date ?? transfer.OperationalDate;
        transfer.Status = result.IngestionStatus == IncomingNachaIngestionStatus.Completado
            ? AchManagedFileTransferStatus.Processed
            : result.IngestionStatus == IncomingNachaIngestionStatus.Duplicado
                ? AchManagedFileTransferStatus.Duplicate
                : result.ParsingStatus == IncomingNachaParsingStatus.FallidoReprocesable
                    ? AchManagedFileTransferStatus.RetryPending
                    : AchManagedFileTransferStatus.Rejected;
        transfer.ProcessedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        transfer.LastError = result.Errors.Count == 0
            ? null
            : AchColombiaManagedMftImplementationSupport.Limit(string.Join(" | ", result.Errors), 1000);
        transfer.LastErrorCode = transfer.Status is AchManagedFileTransferStatus.Processed or AchManagedFileTransferStatus.Duplicate
            ? null
            : "ACHCOL_INBOUND_REJECTED";
        transfer.ConcurrencyToken = Guid.NewGuid();

        AchColombiaManagedMftImplementationSupport.AddEvent(
            transfer,
            reprocess ? "ReprocessFinished" : "InboundProcessingFinished",
            transfer.Status.ToString(),
            transfer.LastError ?? "Procesamiento completado.",
            origin,
            actor);

        switch (transfer.Status)
        {
            case AchManagedFileTransferStatus.Processed:
                AchColombiaManagedMftImplementationSupport.AddEvent(transfer, "Processed", "Processed", "Archivo procesado.", origin, actor);
                break;
            case AchManagedFileTransferStatus.Duplicate:
                AchColombiaManagedMftImplementationSupport.AddEvent(
                    transfer,
                    "Duplicate",
                    "Duplicate",
                    "Archivo duplicado identificado; no se creó un efecto funcional adicional.",
                    origin,
                    actor);
                break;
            case AchManagedFileTransferStatus.RetryPending:
                AchColombiaManagedMftImplementationSupport.AddEvent(
                    transfer,
                    "RetryPending",
                    "RetryPending",
                    "El procesamiento quedó pendiente de reintento.",
                    origin,
                    actor);
                break;
            case AchManagedFileTransferStatus.Rejected:
                AchColombiaManagedMftImplementationSupport.AddEvent(
                    transfer,
                    "Rejected",
                    "Rejected",
                    "Archivo rechazado durante el procesamiento.",
                    origin,
                    actor);
                break;
            case AchManagedFileTransferStatus.Failed:
                AchColombiaManagedMftImplementationSupport.AddEvent(
                    transfer,
                    "Failed",
                    "Failed",
                    "El procesamiento falló.",
                    origin,
                    actor);
                break;
        }

        // El resultado funcional debe quedar persistido antes de mover el claim
        // a archivo. Esto permite recuperación segura si el proceso cae después.
        await context.SaveChangesAsync(ct);
    }

    private async Task RegisterInboundWindowObservationAsync(
        AchManagedFileTransfer transfer,
        AchManagedMftArtifact artifact,
        AchManagedFileExecutionOrigin origin,
        string actor,
        CancellationToken ct)
    {
        if (origin != AchManagedFileExecutionOrigin.Automatic
            || string.IsNullOrWhiteSpace(transfer.AchCycleId)
            || !artifact.BoundaryLastWriteTimeUtc.HasValue
            || transfer.Events.Any(x => x.EventType == "InboundOutsideExpectedWindow"))
        {
            return;
        }

        var cycle = await context.AchCycles.AsNoTracking()
            .Include(x => x.ClearingHouse)
                .ThenInclude(x => x!.ClearingHouseConfig)
            .SingleOrDefaultAsync(x => x.Id == transfer.AchCycleId
                && x.ClearingHouse!.Code == AchColombiaManagedMftImplementationSupport.ClearingHouseCode, ct);
        if (cycle?.OutputReleaseStartTime is null || cycle.OutputReleaseEndTime is null)
            return;

        var timeZoneId = cycle.ClearingHouse?.ClearingHouseConfig?.TimeZoneId;
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return;

        var observedUtc = DateTime.SpecifyKind(artifact.BoundaryLastWriteTimeUtc.Value, DateTimeKind.Utc);
        var observedInstant = new DateTimeOffset(observedUtc, TimeSpan.Zero);
        var window = windowResolver.Resolve(
            cycle.ProcessingDate,
            cycle.OutputReleaseStartTime.Value,
            cycle.OutputReleaseEndTime.Value,
            timeZoneId,
            observedInstant);
        if (window.Status == OperationalCycleWindowStatus.Inside)
            return;

        AchColombiaManagedMftImplementationSupport.AddEvent(
            transfer,
            "InboundOutsideExpectedWindow",
            "Observed",
            "La marca de tiempo observada en la frontera CFA está fuera de la ventana esperada del ciclo.",
            origin,
            actor);
        transfer.ConcurrencyToken = Guid.NewGuid();
        await context.SaveChangesAsync(ct);
    }

    private async Task MarkInboundRecoveryPendingAsync(
        AchManagedFileTransfer transfer,
        AchManagedFileExecutionOrigin origin,
        string actor,
        CancellationToken ct)
    {
        transfer.Status = AchManagedFileTransferStatus.RetryPending;
        transfer.LastErrorCode = "ACHCOL_INBOUND_RECOVERY_PENDING";
        transfer.LastError = "El procesamiento se interrumpió y quedó pendiente de recuperación.";
        transfer.ConcurrencyToken = Guid.NewGuid();
        AchColombiaManagedMftImplementationSupport.AddEvent(
            transfer,
            "InboundProcessingFailed",
            "Failed",
            "No se pudo procesar el archivo.",
            origin,
            actor);
        AchColombiaManagedMftImplementationSupport.AddEvent(
            transfer,
            "InboundRecoveryPending",
            "RetryPending",
            "El archivo quedó pendiente de recuperación.",
            origin,
            actor);
        await context.SaveChangesAsync(ct);
    }

    private bool IsInboundRecoveryDue(
        AchManagedFileTransfer transfer,
        AchManagedFileTransferConfiguration configuration)
    {
        var retryDelay = TimeSpan.FromSeconds(Math.Max(1, configuration.RetryDelaySeconds));
        var cutoff = _timeProvider.GetUtcNow().UtcDateTime.Subtract(retryDelay);
        return transfer.Status switch
        {
            AchManagedFileTransferStatus.Received => transfer.CreatedAtUtc <= cutoff,
            AchManagedFileTransferStatus.InProgress => transfer.LastAttemptAtUtc.HasValue && transfer.LastAttemptAtUtc.Value <= cutoff,
            AchManagedFileTransferStatus.RetryPending => !transfer.LastAttemptAtUtc.HasValue || transfer.LastAttemptAtUtc.Value <= cutoff,
            _ => false
        };
    }

    private async Task<bool> TryAcquireInboundRecoveryAsync(AchManagedFileTransfer transfer, CancellationToken ct)
    {
        transfer.ConcurrencyToken = Guid.NewGuid();
        try
        {
            await context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            context.ChangeTracker.Clear();
            return false;
        }
    }
}
