using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACHSobreDigital.ManagedDigitalEnvelope;
using Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class AchColombiaManagedFileExchangeService(
    AchDbContext context,
    INachaFileBuilder nachaBuilder,
    IExternalFileNamePolicy externalFileNamePolicy,
    IOperationalTimeSnapshotProvider operationalTimeProvider,
    INachaExportDigitalEnvelopeService digitalEnvelope,
    IAchFileExportAuditService exportAudit,
    IIncomingNachaIngestionAppService ingestionService,
    IAchColombiaManagedMftAdapter mftAdapter,
    IEncryptionService? encryption = null,
    IOptions<AchColombiaManagedMftOptions>? options = null) : IAchColombiaManagedFileExchangeService
{
    private const string ClearingHouseCode = "ACHCOL";

    public async Task<AchManagedFileExecutionResult> ExecuteOutboundAsync(
        string cycleId, AchManagedFileExecutionOrigin origin, string actor, string idempotencyKey, CancellationToken ct = default,
        Guid? correctedFromTransferId = null)
    {
        ValidateCommand(actor, idempotencyKey);
        var configuration = await GetOrCreateConfigurationEntityAsync(ct);
        if (!IsEnabled(configuration, AchManagedFileDirection.Outbound, origin)) return new(0, 0, 0, []);
        if (!configuration.ProfileEnabled) throw new InvalidOperationException("ACHCOL_MFT_DISABLED");

        var cycle = await context.AchCycles.AsNoTracking().Include(x => x.ClearingHouse)
            .SingleOrDefaultAsync(x => x.Id == cycleId && x.ClearingHouse!.Code == ClearingHouseCode, ct)
            ?? throw new InvalidOperationException("ACHCOL_CYCLE_NOT_FOUND");
        AchManagedFileTransfer? predecessor = null;
        if (correctedFromTransferId.HasValue)
        {
            predecessor = await context.AchManagedFileTransfers.AsNoTracking().SingleOrDefaultAsync(x =>
                x.Id == correctedFromTransferId && x.Direction == AchManagedFileDirection.Outbound && x.AchCycleId == cycleId, ct)
                ?? throw new InvalidOperationException("ACHCOL_MFT_CORRECTION_SOURCE_NOT_FOUND");
            if (predecessor.Status is not (AchManagedFileTransferStatus.Failed or AchManagedFileTransferStatus.Rejected or AchManagedFileTransferStatus.Retired))
                throw new InvalidOperationException("ACHCOL_MFT_CORRECTION_NOT_ALLOWED");
        }
        var existing = await context.AchManagedFileTransfers.Include(x => x.Events)
            .Where(x => x.Direction == AchManagedFileDirection.Outbound && x.AchCycleId == cycleId
                && (correctedFromTransferId == null ? x.Status != AchManagedFileTransferStatus.Retired : x.CorrectedFromTransferId == correctedFromTransferId))
            .OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(ct);
        if (existing is not null)
        {
            if (existing.Status is AchManagedFileTransferStatus.RetryPending or AchManagedFileTransferStatus.Uncertain or AchManagedFileTransferStatus.InProgress
                && existing.RetainedContent is not null
                && existing.AttemptCount <= configuration.MaximumRetries)
            {
                return await HandoffAsync(existing, actor, ct);
            }
            return new(1, existing.Status is AchManagedFileTransferStatus.Transferred ? 1 : 0, existing.Status is AchManagedFileTransferStatus.Failed ? 1 : 0, [existing.Id]);
        }

        var built = await nachaBuilder.BuildNachaFileArtifactByCycleAsync(cycleId, ct);
        if (string.IsNullOrWhiteSpace(built.Content)) return new(0, 0, 0, []);
        var internalName = $"NACHA_{cycle.Id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.tmp";
        var snapshot = operationalTimeProvider.GetOrCreate($"ACHCOL-MFT:{cycle.Id}", DateOnly.FromDateTime(cycle.ProcessingDate), TimeOnly.FromTimeSpan(cycle.CutoffTime));
        var nameResult = await externalFileNamePolicy.GenerateExternalNameAsync(new ExternalFileNameContext
        {
            ClearingHouseId = cycle.ClearingHouseId,
            ClearingHouseCode = cycle.ClearingHouse!.Code,
            ClearingHouseOriginCode = cycle.ClearingHouse.OriginCode,
            CycleId = cycle.Id,
            CycleName = cycle.CycleName,
            CycleNumber = ResolveCycleNumber(cycle.CycleName),
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
        if (nameResult.Validation.IsHardBlocked) throw new InvalidOperationException("ACHCOL_MFT_FILENAME_POLICY_REJECTED");
        var normalized = NormalizeHeaderIdentifier(built.Content, nameResult.Components.FileIdModifier);
        var plain = Encoding.ASCII.GetBytes(normalized);
        ManagedDigitalEnvelopeResult envelope;
        try
        {
            envelope = await digitalEnvelope.EncryptAsync(cycle.ClearingHouseId, nameResult.ExternalFileName, plain, actor, ct);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plain);
        }
        var hash = Convert.ToHexString(SHA256.HashData(envelope.Content));

        await exportAudit.RecordGeneratedFileAsync(cycle.Id, cycle.ClearingHouseId, "NACHA", envelope.FileName,
            normalized.Length / 106, built.AchTransactionIds.Count, true, built.AchTransactionIds, hash, ct);
        var exportId = await context.AchFileExports.Where(x => x.AchCycleId == cycle.Id && x.FileName == envelope.FileName && x.IsEncrypted)
            .Select(x => x.Id).SingleAsync(ct);
        var transfer = NewTransfer(cycle.ClearingHouseId, AchManagedFileDirection.Outbound, envelope.FileName, envelope.Content,
            cycle.ProcessingDate.Date, cycle.Id, origin, actor, Limit(idempotencyKey, 160));
        transfer.AchFileExportId = exportId;
        transfer.CorrectedFromTransferId = predecessor?.Id;
        transfer.Status = AchManagedFileTransferStatus.InProgress;
        transfer.ProcessingStartedAtUtc = DateTime.UtcNow;
        AddEvent(transfer, "OutboundPrepared", "Succeeded", "Archivo oficial preparado para entrega.", origin, actor);
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
            return new(1, winner.Status == AchManagedFileTransferStatus.Transferred ? 1 : 0,
                winner.Status is AchManagedFileTransferStatus.Failed or AchManagedFileTransferStatus.Rejected ? 1 : 0, [winner.Id]);
        }
        return await HandoffAsync(transfer, actor, ct);
    }

    public async Task<AchManagedFileExecutionResult> ExecuteInboundAsync(
        AchManagedFileExecutionOrigin origin, string actor, string idempotencyKey, CancellationToken ct = default)
    {
        ValidateCommand(actor, idempotencyKey);
        var configuration = await GetOrCreateConfigurationEntityAsync(ct);
        if (!IsEnabled(configuration, AchManagedFileDirection.Inbound, origin)) return new(0, 0, 0, []);
        if (!configuration.ProfileEnabled) throw new InvalidOperationException("ACHCOL_MFT_DISABLED");
        var chamberId = configuration.ClearingHouseId;
        var artifacts = await mftAdapter.PickupInboundAsync(ct);
        var ids = new List<Guid>();
        var succeeded = 0;
        var failed = 0;
        foreach (var artifact in artifacts)
        {
            var duplicate = await context.AchManagedFileTransfers.Include(x => x.Events)
                .SingleOrDefaultAsync(x => x.Direction == AchManagedFileDirection.Inbound && x.ContentSha256 == artifact.ContentSha256 && x.FileSize == artifact.Content.LongLength, ct);
            if (duplicate is not null)
            {
                if (duplicate.IncomingNachaFileIngestionId is null
                    && duplicate.Status is AchManagedFileTransferStatus.Received or AchManagedFileTransferStatus.InProgress or AchManagedFileTransferStatus.RetryPending)
                {
                    AddEvent(duplicate, "InboundRecovery", "Started", "Se reanudó una recepción interrumpida.", origin, actor);
                    await ProcessInboundAsync(duplicate, duplicate.RetainedContent ?? artifact.Content, actor, false, null, ct);
                    if (duplicate.Status == AchManagedFileTransferStatus.Processed) succeeded++; else failed++;
                }
                else
                {
                    AddEvent(duplicate, "DuplicateDetected", "Ignored", $"Contenido repetido recibido como {artifact.FileName}.", origin, actor);
                }
                duplicate.ConcurrencyToken = Guid.NewGuid();
                duplicate.ArchiveReference ??= await mftAdapter.ArchiveInboundAsync(artifact, ct);
                duplicate.ArchivedAtUtc ??= DateTime.UtcNow;
                ids.Add(duplicate.Id);
                await context.SaveChangesAsync(ct);
                continue;
            }

            var sameName = await context.AchManagedFileTransfers.AnyAsync(x => x.Direction == AchManagedFileDirection.Inbound && x.PhysicalFileName == artifact.FileName, ct);
            var transfer = NewTransfer(chamberId, AchManagedFileDirection.Inbound, artifact.FileName, artifact.Content,
                DateTime.UtcNow.Date, null, origin, actor, Limit($"{idempotencyKey}:{artifact.ContentSha256}", 160));
            transfer.Status = sameName ? AchManagedFileTransferStatus.Rejected : AchManagedFileTransferStatus.Received;
            transfer.ActiveStorageReference = artifact.ClaimReference;
            AddEvent(transfer, "InboundClaimed", "Succeeded", "Archivo recibido y reclamado de forma exclusiva.", origin, actor);
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
                        && x.ContentSha256 == artifact.ContentSha256 && x.FileSize == artifact.Content.LongLength, ct);
                if (winner is null) throw;
                AddEvent(winner, "DuplicateDetected", "Ignored", $"Recepción concurrente descartada: {artifact.FileName}.", origin, actor);
                winner.ArchiveReference ??= await mftAdapter.ArchiveInboundAsync(artifact, ct);
                winner.ArchivedAtUtc ??= DateTime.UtcNow;
                winner.ConcurrencyToken = Guid.NewGuid();
                await context.SaveChangesAsync(ct);
                ids.Add(winner.Id);
                continue;
            }
            ids.Add(transfer.Id);
            if (sameName)
            {
                transfer.LastErrorCode = "ACHCOL_MFT_SAME_NAME_DIFFERENT_CONTENT";
                transfer.LastError = "El nombre ya fue recibido con contenido diferente.";
                AddEvent(transfer, "InboundRejected", "Rejected", transfer.LastError, origin, actor);
                failed++;
            }
            else
            {
                await ProcessInboundAsync(transfer, artifact.Content, actor, false, null, ct);
                if (transfer.Status == AchManagedFileTransferStatus.Processed) succeeded++; else failed++;
            }
            transfer.ArchiveReference = await mftAdapter.ArchiveInboundAsync(artifact, ct);
            transfer.ArchivedAtUtc = DateTime.UtcNow;
            transfer.ActiveStorageReference = null;
            AddEvent(transfer, "Archived", "Succeeded", "Archivo retirado del área de recepción y conservado en archivo.", origin, actor);
            transfer.ConcurrencyToken = Guid.NewGuid();
            await context.SaveChangesAsync(ct);
        }
        return new(artifacts.Count, succeeded, failed, ids);
    }

    public async Task<AchManagedFileTransferDetail> RetryAsync(Guid transferId, string actor, string idempotencyKey, CancellationToken ct = default)
    {
        ValidateCommand(actor, idempotencyKey);
        var transfer = await RequiredAsync(transferId, ct);
        if (transfer.Direction != AchManagedFileDirection.Outbound || transfer.Status is not (AchManagedFileTransferStatus.RetryPending or AchManagedFileTransferStatus.Uncertain))
            throw new InvalidOperationException("ACHCOL_MFT_RETRY_NOT_ALLOWED");
        if (transfer.RetainedContent is null) throw new InvalidOperationException("ACHCOL_MFT_CONTENT_NOT_RETAINED");
        var configuration = await GetOrCreateConfigurationEntityAsync(ct);
        if (transfer.AttemptCount > configuration.MaximumRetries) throw new InvalidOperationException("ACHCOL_MFT_RETRIES_EXHAUSTED");
        await HandoffAsync(transfer, actor, ct);
        return Map(await RequiredAsync(transferId, ct));
    }

    public async Task<AchManagedFileTransferDetail> ReprocessAsync(Guid transferId, string actor, CancellationToken ct = default)
    {
        var transfer = await RequiredAsync(transferId, ct);
        if (transfer.Direction != AchManagedFileDirection.Inbound || transfer.IncomingNachaFileIngestionId is null || transfer.RetainedContent is null || transfer.Status is not (AchManagedFileTransferStatus.Rejected or AchManagedFileTransferStatus.Failed))
            throw new InvalidOperationException("ACHCOL_MFT_REPROCESS_NOT_ALLOWED");
        await ProcessInboundAsync(transfer, transfer.RetainedContent, actor, true, transfer.IncomingNachaFileIngestionId, ct);
        await context.SaveChangesAsync(ct);
        return Map(transfer);
    }

    public async Task<AchManagedFileTransferDetail> ArchiveAsync(Guid transferId, string actor, CancellationToken ct = default)
    {
        var transfer = await RequiredAsync(transferId, ct);
        if (transfer.RetiredAtUtc.HasValue) throw new InvalidOperationException("ACHCOL_MFT_ARCHIVE_NOT_ALLOWED");
        transfer.ArchivedAtUtc ??= DateTime.UtcNow;
        transfer.ArchiveReference ??= $"retained:{transfer.Id:N}";
        AddEvent(transfer, "Archived", "Succeeded", "Contenido conservado en el archivo operativo.", AchManagedFileExecutionOrigin.Manual, actor);
        transfer.ConcurrencyToken = Guid.NewGuid();
        await context.SaveChangesAsync(ct);
        return Map(transfer);
    }

    public async Task<AchManagedFileTransferDetail> RetireAsync(Guid transferId, string actor, string reason, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("La razón de retiro es obligatoria.", nameof(reason));
        var transfer = await RequiredAsync(transferId, ct);
        if (transfer.Status == AchManagedFileTransferStatus.InProgress) throw new InvalidOperationException("ACHCOL_MFT_RETIRE_NOT_ALLOWED");
        transfer.RetainedContent = null;
        transfer.ActiveStorageReference = null;
        transfer.Status = AchManagedFileTransferStatus.Retired;
        transfer.RetiredAtUtc = DateTime.UtcNow;
        transfer.RetiredBy = actor;
        transfer.RetirementReason = Limit(reason, 500);
        transfer.ConcurrencyToken = Guid.NewGuid();
        AddEvent(transfer, "Retired", "Succeeded", "Archivo retirado del almacenamiento activo; historial conservado.", AchManagedFileExecutionOrigin.Manual, actor);
        await context.SaveChangesAsync(ct);
        return Map(transfer);
    }

    public async Task<IReadOnlyList<AchManagedFileTransferSummary>> QueryAsync(AchManagedFileTransferQuery query, CancellationToken ct = default)
    {
        var items = context.AchManagedFileTransfers.AsNoTracking().Where(x => x.ClearingHouse.Code == ClearingHouseCode);
        if (query.From.HasValue) items = items.Where(x => x.OperationalDate >= query.From.Value.Date);
        if (query.To.HasValue) items = items.Where(x => x.OperationalDate < query.To.Value.Date.AddDays(1));
        if (query.Direction.HasValue) items = items.Where(x => x.Direction == query.Direction);
        if (query.Status.HasValue) items = items.Where(x => x.Status == query.Status);
        if (!string.IsNullOrWhiteSpace(query.CycleId)) items = items.Where(x => x.AchCycleId == query.CycleId);
        if (query.ExecutionOrigin.HasValue) items = items.Where(x => x.ExecutionOrigin == query.ExecutionOrigin);
        return await items.OrderByDescending(x => x.CreatedAtUtc).Take(500)
            .Select(x => new AchManagedFileTransferSummary(x.Id, x.PhysicalFileName, x.Direction, x.OperationalDate, x.AchCycleId, x.Status, x.ExecutionOrigin, x.AttemptCount, x.UpdatedAt.UtcDateTime, x.ArchivedAtUtc != null, x.RetiredAtUtc != null)).ToListAsync(ct);
    }

    public async Task<AchManagedFileTransferDetail?> GetAsync(Guid transferId, CancellationToken ct = default)
    {
        var transfer = await context.AchManagedFileTransfers.AsNoTracking().Include(x => x.Events)
            .SingleOrDefaultAsync(x => x.Id == transferId && x.ClearingHouse.Code == ClearingHouseCode, ct);
        return transfer is null ? null : Map(transfer);
    }

    public async Task<AchManagedFileDownload?> DownloadAsync(Guid transferId, string actor, CancellationToken ct = default)
    {
        var transfer = await RequiredAsync(transferId, ct);
        if (transfer.RetainedContent is null) return null;
        AddEvent(transfer, "Downloaded", "Succeeded", "Archivo descargado por un operador autorizado.", AchManagedFileExecutionOrigin.Manual, actor);
        await context.SaveChangesAsync(ct);
        return new(transfer.PhysicalFileName, "application/octet-stream", transfer.RetainedContent);
    }

    public async Task<AchManagedFileTransferConfigurationDto> GetConfigurationAsync(CancellationToken ct = default)
        => Map(await GetOrCreateConfigurationEntityAsync(ct));

    public async Task<AchManagedFileTransferConfigurationDto> UpdateConfigurationAsync(AchManagedFileTransferConfigurationDto value, string actor, CancellationToken ct = default)
    {
        if (value.MaximumRetries is < 0 or > 20 || value.RetentionDays is < 1 or > 3650) throw new ArgumentException("Configuración operativa fuera de rango.");
        var entity = await GetOrCreateConfigurationEntityAsync(ct);
        if (entity.ConcurrencyToken != value.ConcurrencyToken) throw new DbUpdateConcurrencyException("ACHCOL_MFT_CONFIGURATION_CONFLICT");
        entity.AutomaticOutboundEnabled = value.AutomaticOutboundEnabled;
        entity.AutomaticInboundEnabled = value.AutomaticInboundEnabled;
        entity.ManualOutboundAllowed = value.ManualOutboundAllowed;
        entity.ManualInboundAllowed = value.ManualInboundAllowed;
        entity.MaximumRetries = value.MaximumRetries;
        entity.RetentionDays = value.RetentionDays;
        entity.OutboundLocation = LimitRequired(value.OutboundLocation, 120);
        entity.InboundLocation = LimitRequired(value.InboundLocation, 120);
        entity.ArchiveLocation = LimitRequired(value.ArchiveLocation, 120);
        entity.ConcurrencyToken = Guid.NewGuid();
        await context.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<AchManagedMftAdministrationDto> GetAdministrationAsync(CancellationToken ct = default)
        => MapAdministration(await GetOrCreateConfigurationEntityAsync(ct));

    public async Task<AchManagedMftAdministrationDto> UpdateAdministrationAsync(UpdateAchManagedMftAdministrationRequest value, string actor, CancellationToken ct = default)
    {
        if (value.MaximumRetries is < 0 or > 20 || value.RetryDelaySeconds is < 0 or > 86400 || value.RetentionDays is < 1 or > 3650)
            throw new ArgumentException("Configuración operativa fuera de rango.");
        if (value.Port is < 1 or > 65535) throw new ArgumentException("Puerto inválido.");
        if (value.ProfileEnabled && (string.IsNullOrWhiteSpace(value.OutboundLocation) || string.IsNullOrWhiteSpace(value.InboundLocation) || string.IsNullOrWhiteSpace(value.ArchiveLocation)))
            throw new ArgumentException("Las rutas operativas son obligatorias para un perfil habilitado.");
        var entity = await GetOrCreateConfigurationEntityAsync(ct);
        if (entity.ConcurrencyToken != value.ConcurrencyToken) throw new DbUpdateConcurrencyException("ACHCOL_MFT_CONFIGURATION_CONFLICT");
        entity.ProfileName = LimitRequired(value.ProfileName, 120);
        entity.Provider = LimitRequired(value.Provider, 60);
        entity.Protocol = LimitRequired(value.Protocol, 40);
        entity.ProfileEnabled = value.ProfileEnabled;
        entity.Endpoint = Optional(value.Endpoint, 300);
        entity.Port = value.Port;
        entity.Principal = Optional(value.Principal, 160);
        entity.AutomaticOutboundEnabled = value.AutomaticOutboundEnabled;
        entity.AutomaticInboundEnabled = value.AutomaticInboundEnabled;
        entity.ManualOutboundAllowed = value.ManualOutboundAllowed;
        entity.ManualInboundAllowed = value.ManualInboundAllowed;
        entity.MaximumRetries = value.MaximumRetries;
        entity.RetryDelaySeconds = value.RetryDelaySeconds;
        entity.RetentionDays = value.RetentionDays;
        entity.OutboundLocation = LimitRequired(value.OutboundLocation, 120);
        entity.InboundLocation = LimitRequired(value.InboundLocation, 120);
        entity.ArchiveLocation = LimitRequired(value.ArchiveLocation, 120);
        entity.ConcurrencyToken = Guid.NewGuid();
        await context.SaveChangesAsync(ct);
        return MapAdministration(entity);
    }

    public async Task<AchManagedMftAdministrationDto> SetCredentialAsync(SetAchManagedMftCredentialRequest value, string actor, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(value.CredentialType) || string.IsNullOrWhiteSpace(value.Secret)) throw new ArgumentException("La credencial es obligatoria.");
        var entity = await GetOrCreateConfigurationEntityAsync(ct);
        entity.CredentialType = LimitRequired(value.CredentialType, 40);
        entity.ProtectedCredential = (encryption ?? throw new InvalidOperationException("ACHCOL_MFT_CREDENTIAL_PROTECTION_NOT_CONFIGURED")).Encrypt(value.Secret);
        entity.CredentialUpdatedAtUtc = DateTime.UtcNow;
        entity.ConcurrencyToken = Guid.NewGuid();
        await context.SaveChangesAsync(ct);
        return MapAdministration(entity);
    }

    private async Task<AchManagedFileExecutionResult> HandoffAsync(AchManagedFileTransfer transfer, string actor, CancellationToken ct)
    {
        transfer.AttemptCount++;
        transfer.LastAttemptAtUtc = DateTime.UtcNow;
        transfer.Status = AchManagedFileTransferStatus.InProgress;
        AddEvent(transfer, "OutboundAttempt", "Started", $"Intento {transfer.AttemptCount} de entrega iniciado.", transfer.ExecutionOrigin, actor);
        await context.SaveChangesAsync(ct);
        var result = await mftAdapter.HandoffOutboundAsync(transfer.PhysicalFileName, transfer.RetainedContent!, transfer.ContentSha256, ct);
        transfer.LastErrorCode = result.Succeeded ? null : result.Code;
        transfer.LastError = result.Succeeded ? null : result.Message;
        transfer.ActiveStorageReference = result.StorageReference;
        transfer.Status = result.Succeeded ? AchManagedFileTransferStatus.Transferred : result.Uncertain ? AchManagedFileTransferStatus.Uncertain : result.Retryable ? AchManagedFileTransferStatus.RetryPending : AchManagedFileTransferStatus.Failed;
        transfer.TransferredAtUtc = result.Succeeded ? DateTime.UtcNow : null;
        if (result.Succeeded)
        {
            transfer.ArchivedAtUtc ??= DateTime.UtcNow;
            transfer.ArchiveReference ??= $"retained:{transfer.Id:N}";
            AddEvent(transfer, "Archived", "Succeeded", "Contenido conservado en el archivo operativo.", transfer.ExecutionOrigin, actor);
        }
        transfer.ConcurrencyToken = Guid.NewGuid();
        AddEvent(transfer, "OutboundAttempt", result.Succeeded ? "Succeeded" : result.Uncertain ? "Uncertain" : "Failed", result.Message, transfer.ExecutionOrigin, actor);
        await context.SaveChangesAsync(ct);
        return new(1, result.Succeeded ? 1 : 0, result.Succeeded ? 0 : 1, [transfer.Id]);
    }

    private async Task ProcessInboundAsync(AchManagedFileTransfer transfer, byte[] content, string actor, bool reprocess, Guid? parentId, CancellationToken ct)
    {
        transfer.AttemptCount++;
        transfer.LastAttemptAtUtc = DateTime.UtcNow;
        transfer.Status = AchManagedFileTransferStatus.InProgress;
        AddEvent(transfer, reprocess ? "ReprocessStarted" : "InboundProcessingStarted", "Started", "Procesamiento NACHA-M iniciado.", transfer.ExecutionOrigin, actor);
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
        transfer.Status = result.IngestionStatus == IncomingNachaIngestionStatus.Completado ? AchManagedFileTransferStatus.Processed
            : result.IngestionStatus == IncomingNachaIngestionStatus.Duplicado ? AchManagedFileTransferStatus.Duplicate
            : result.ParsingStatus == IncomingNachaParsingStatus.FallidoReprocesable ? AchManagedFileTransferStatus.RetryPending
            : AchManagedFileTransferStatus.Rejected;
        transfer.ProcessedAtUtc = DateTime.UtcNow;
        transfer.LastError = result.Errors.Count == 0 ? null : Limit(string.Join(" | ", result.Errors), 1000);
        transfer.LastErrorCode = transfer.Status is AchManagedFileTransferStatus.Processed or AchManagedFileTransferStatus.Duplicate ? null : "ACHCOL_INBOUND_REJECTED";
        transfer.ConcurrencyToken = Guid.NewGuid();
        AddEvent(transfer, reprocess ? "ReprocessFinished" : "InboundProcessingFinished", transfer.Status.ToString(), transfer.LastError ?? "Procesamiento completado.", transfer.ExecutionOrigin, actor);
    }

    private async Task<AchManagedFileTransferConfiguration> GetOrCreateConfigurationEntityAsync(CancellationToken ct)
    {
        var chamber = await context.ClearingHouses.SingleAsync(x => x.Code == ClearingHouseCode, ct);
        var configuration = await context.AchManagedFileTransferConfigurations.SingleOrDefaultAsync(x => x.ClearingHouseId == chamber.Id, ct);
        if (configuration is not null) return configuration;
        var defaults = options?.Value ?? new AchColombiaManagedMftOptions();
        configuration = new AchManagedFileTransferConfiguration
        {
            ClearingHouseId = chamber.Id, ProfileEnabled = defaults.Enabled,
            OutboundLocation = defaults.OutboundPath, InboundLocation = defaults.InboundPath, ArchiveLocation = defaults.ArchivePath
        };
        context.Add(configuration);
        await context.SaveChangesAsync(ct);
        return configuration;
    }

    private async Task<AchManagedFileTransfer> RequiredAsync(Guid id, CancellationToken ct)
        => await context.AchManagedFileTransfers.Include(x => x.Events).SingleOrDefaultAsync(x => x.Id == id && x.ClearingHouse.Code == ClearingHouseCode, ct)
           ?? throw new KeyNotFoundException("ACHCOL_MFT_TRANSFER_NOT_FOUND");

    private static AchManagedFileTransfer NewTransfer(int chamberId, AchManagedFileDirection direction, string fileName, byte[] content,
        DateTime operationalDate, string? cycleId, AchManagedFileExecutionOrigin origin, string actor, string idempotencyKey)
        => new()
        {
            ClearingHouseId = chamberId, Direction = direction, LogicalFileIdentity = $"{direction}:{Convert.ToHexString(SHA256.HashData(content))}",
            PhysicalFileName = Path.GetFileName(fileName), ContentSha256 = Convert.ToHexString(SHA256.HashData(content)), FileSize = content.LongLength,
            RetainedContent = content.ToArray(), OperationalDate = operationalDate, AchCycleId = cycleId, ExecutionOrigin = origin,
            OperatorIdentity = origin == AchManagedFileExecutionOrigin.Manual ? actor : null, CorrelationId = Guid.NewGuid().ToString("N"), IdempotencyKey = idempotencyKey
        };

    private static void AddEvent(AchManagedFileTransfer transfer, string type, string result, string message, AchManagedFileExecutionOrigin origin, string actor)
        => transfer.Events.Add(new() { EventType = type, Result = result, Message = Limit(message, 1000), ExecutionOrigin = origin, Actor = Limit(actor, 160) });
    private static bool IsEnabled(AchManagedFileTransferConfiguration c, AchManagedFileDirection d, AchManagedFileExecutionOrigin o)
        => (d, o) switch { (AchManagedFileDirection.Outbound, AchManagedFileExecutionOrigin.Automatic) => c.AutomaticOutboundEnabled, (AchManagedFileDirection.Inbound, AchManagedFileExecutionOrigin.Automatic) => c.AutomaticInboundEnabled, (AchManagedFileDirection.Outbound, _) => c.ManualOutboundAllowed, _ => c.ManualInboundAllowed };
    private static void ValidateCommand(string actor, string key) { ArgumentException.ThrowIfNullOrWhiteSpace(actor); ArgumentException.ThrowIfNullOrWhiteSpace(key); }
    private static string LimitRequired(string value, int max) { ArgumentException.ThrowIfNullOrWhiteSpace(value); return Limit(value, max); }
    private static string Limit(string value, int max) => value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static string? Optional(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : Limit(value, max);
    private static int ResolveCycleNumber(string value)
    {
        var matches = Regex.Matches(value ?? string.Empty, @"(?<!\d)(\d+)(?!\d)");
        if (matches.Count != 1 || !int.TryParse(matches[0].Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number <= 0) throw new InvalidOperationException("ACHCOL_CYCLE_NUMBER_INVALID");
        return number;
    }
    private static string NormalizeHeaderIdentifier(string content, char? identifier)
    {
        if (!identifier.HasValue || content.Length < 36) return content;
        var chars = content.ToCharArray(); chars[35] = identifier.Value; return new string(chars);
    }
    private static AchManagedFileTransferDetail Map(AchManagedFileTransfer x) => new(x.Id, x.PhysicalFileName, x.Direction, x.OperationalDate, x.AchCycleId, x.Status, x.ExecutionOrigin, x.FileSize, x.ContentSha256, x.AttemptCount, x.CreatedAtUtc, x.TransferredAtUtc, x.ProcessedAtUtc, x.LastError, x.ArchivedAtUtc != null, x.ArchivedAtUtc, x.RetiredAtUtc != null, x.RetiredAtUtc, x.RetirementReason, x.CorrectedFromTransferId, x.Events.OrderBy(e => e.OccurredAtUtc).Select(e => new AchManagedFileTransferEventDto(e.Id, e.OccurredAtUtc, e.EventType, e.Result, e.Message, e.ExecutionOrigin, e.Actor)).ToArray());
    private static AchManagedFileTransferConfigurationDto Map(AchManagedFileTransferConfiguration x) => new(x.AutomaticOutboundEnabled, x.AutomaticInboundEnabled, x.ManualOutboundAllowed, x.ManualInboundAllowed, x.MaximumRetries, x.RetentionDays, x.OutboundLocation, x.InboundLocation, x.ArchiveLocation, x.ConcurrencyToken);
    private static AchManagedMftAdministrationDto MapAdministration(AchManagedFileTransferConfiguration x) => new(x.ProfileName, x.Provider, x.Protocol, x.ProfileEnabled, x.Endpoint, x.Port, x.Principal, x.AutomaticOutboundEnabled, x.AutomaticInboundEnabled, x.ManualOutboundAllowed, x.ManualInboundAllowed, x.MaximumRetries, x.RetryDelaySeconds, x.RetentionDays, x.OutboundLocation, x.InboundLocation, x.ArchiveLocation, !string.IsNullOrWhiteSpace(x.ProtectedCredential), x.CredentialType, x.CredentialUpdatedAtUtc, x.ConcurrencyToken);
}
