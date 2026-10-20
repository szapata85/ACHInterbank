using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public sealed class AchColombiaManagedMftQueryProjectionComponent(AchDbContext context)
{
    public async Task<IReadOnlyList<AchManagedFileTransferSummary>> QueryAsync(
        AchManagedFileTransferQuery query,
        CancellationToken ct = default)
    {
        var items = context.AchManagedFileTransfers.AsNoTracking()
            .Where(x => x.ClearingHouse.Code == AchColombiaManagedMftImplementationSupport.ClearingHouseCode);
        if (query.From.HasValue) items = items.Where(x => x.OperationalDate >= query.From.Value.Date);
        if (query.To.HasValue) items = items.Where(x => x.OperationalDate < query.To.Value.Date.AddDays(1));
        if (query.Direction.HasValue) items = items.Where(x => x.Direction == query.Direction);
        if (query.Status.HasValue) items = items.Where(x => x.Status == query.Status);
        if (!string.IsNullOrWhiteSpace(query.CycleId)) items = items.Where(x => x.AchCycleId == query.CycleId);
        if (query.ExecutionOrigin.HasValue) items = items.Where(x => x.ExecutionOrigin == query.ExecutionOrigin);
        if (!string.IsNullOrWhiteSpace(query.FileName))
        {
            var fileName = query.FileName.Trim().ToUpperInvariant();
            items = items.Where(x => x.PhysicalFileName.ToUpper().Contains(fileName));
        }
        if (query.TransferId.HasValue) items = items.Where(x => x.Id == query.TransferId);
        if (query.Archived.HasValue) items = items.Where(x => (x.ArchivedAtUtc != null) == query.Archived);

        var pageSize = Math.Clamp(query.PageSize, 1, 500);
        var pageNumber = Math.Clamp(query.PageNumber, 1, int.MaxValue / pageSize);
        var page = await items
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.PhysicalFileName,
                x.Direction,
                x.OperationalDate,
                x.AchCycleId,
                x.Status,
                x.ExecutionOrigin,
                x.AttemptCount,
                x.UpdatedAt,
                Archived = x.ArchivedAtUtc != null,
                Retired = x.RetiredAtUtc != null
            })
            .ToListAsync(ct);

        return page.Select(x => new AchManagedFileTransferSummary(
            x.Id,
            x.PhysicalFileName,
            x.Direction,
            x.OperationalDate,
            x.AchCycleId,
            x.Status,
            x.ExecutionOrigin,
            x.AttemptCount,
            x.UpdatedAt.UtcDateTime,
            x.Archived,
            x.Retired)
        {
            StatusLabel = AchManagedFileTransferPresentation.StatusLabel(x.Direction, x.Status),
            ExternalConfirmationStatus = AchManagedFileTransferPresentation.ExternalConfirmationStatus(x.Direction, x.Status),
            ExternalConfirmationMessage = AchManagedFileTransferPresentation.ExternalConfirmationMessage(x.Direction, x.Status)
        }).ToArray();
    }

    public async Task<AchManagedFileTransferDetail?> GetAsync(Guid transferId, CancellationToken ct = default)
    {
        var transfer = await context.AchManagedFileTransfers.AsNoTracking()
            .Include(x => x.Events)
            .SingleOrDefaultAsync(
                x => x.Id == transferId
                    && x.ClearingHouse.Code == AchColombiaManagedMftImplementationSupport.ClearingHouseCode,
                ct);
        return transfer is null ? null : await MapAsync(transfer, ct);
    }

    internal async Task<AchManagedFileTransferDetail> MapAsync(AchManagedFileTransfer x, CancellationToken ct)
    {
        var retrySettings = await context.AchManagedFileTransferConfigurations.AsNoTracking()
            .Where(c => c.ClearingHouseId == x.ClearingHouseId)
            .Select(c => new { c.MaximumRetries, c.ProfileEnabled, c.ManualOutboundAllowed })
            .SingleOrDefaultAsync(ct);
        var configuration = retrySettings is null
            ? new AchManagedFileTransferConfiguration()
            : new AchManagedFileTransferConfiguration
            {
                MaximumRetries = retrySettings.MaximumRetries,
                ProfileEnabled = retrySettings.ProfileEnabled,
                ManualOutboundAllowed = retrySettings.ManualOutboundAllowed
            };
        var transactions = x.AchFileExportId.HasValue
            ? await context.AchFileExportTransactions.AsNoTracking()
                .Where(m => m.AchFileExportId == x.AchFileExportId)
                .OrderBy(m => m.FileSequence)
                .ThenBy(m => m.AchTransactionId)
                .Select(m => m.AchTransactionId)
                .ToArrayAsync(ct)
            : [];

        return new AchManagedFileTransferDetail(
            x.Id,
            x.PhysicalFileName,
            x.Direction,
            x.OperationalDate,
            x.AchCycleId,
            x.Status,
            x.ExecutionOrigin,
            x.FileSize,
            x.ContentSha256,
            x.AttemptCount,
            x.CreatedAtUtc,
            x.TransferredAtUtc,
            x.ProcessedAtUtc,
            x.LastError,
            x.ArchivedAtUtc != null,
            x.ArchivedAtUtc,
            x.RetiredAtUtc != null,
            x.RetiredAtUtc,
            x.RetirementReason,
            x.CorrectedFromTransferId,
            x.Events.OrderBy(e => e.OccurredAtUtc).ThenBy(e => e.Id)
                .Select(AchColombiaManagedMftImplementationSupport.MapEvent)
                .ToArray())
        {
            LastAttemptAtUtc = x.LastAttemptAtUtc,
            LastErrorCode = x.LastErrorCode,
            CorrelationId = x.CorrelationId,
            AchFileExportId = x.AchFileExportId,
            IncomingNachaFileIngestionId = x.IncomingNachaFileIngestionId,
            TransactionIds = transactions,
            ContentAvailable = x.RetainedContent is not null,
            CanRetry = x.Direction == AchManagedFileDirection.Outbound
                && x.RetainedContent is not null
                && x.Status is AchManagedFileTransferStatus.RetryPending or AchManagedFileTransferStatus.Uncertain
                && x.AttemptCount <= configuration.MaximumRetries
                && configuration.ProfileEnabled
                && AchColombiaManagedMftImplementationSupport.IsEnabled(
                    configuration,
                    AchManagedFileDirection.Outbound,
                    AchManagedFileExecutionOrigin.Manual),
            CanReprocess = x.Direction == AchManagedFileDirection.Inbound
                && x.IncomingNachaFileIngestionId.HasValue
                && x.RetainedContent is not null
                && x.Status is AchManagedFileTransferStatus.Rejected or AchManagedFileTransferStatus.Failed,
            CanArchive = !x.RetiredAtUtc.HasValue,
            CanRetire = x.Status != AchManagedFileTransferStatus.InProgress && !x.RetiredAtUtc.HasValue,
            StatusLabel = AchManagedFileTransferPresentation.StatusLabel(x.Direction, x.Status),
            ExternalConfirmationStatus = AchManagedFileTransferPresentation.ExternalConfirmationStatus(x.Direction, x.Status),
            ExternalConfirmationMessage = AchManagedFileTransferPresentation.ExternalConfirmationMessage(x.Direction, x.Status),
            SanitizedActiveReference = AchColombiaManagedMftImplementationSupport.SanitizeStorageReference(x.ActiveStorageReference),
            SanitizedArchiveReference = AchColombiaManagedMftImplementationSupport.SanitizeStorageReference(x.ArchiveReference)
        };
    }
}
