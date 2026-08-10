using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class AchOutboundReturnResultProcessor(
    AchDbContext context,
    IAchFileTransmissionEvidenceRecorder evidenceRecorder) : IAchOutboundReturnResultProcessor
{
    public async Task<AchOutboundReturnResultProcessingResult> ProcessAsync(
        AchOutboundReturnResultRequest request,
        CancellationToken ct = default)
    {
        Validate(request);
        var eventId = Trim(request.ExternalEventId, 128);
        var fileName = Path.GetFileName(request.FileName.Trim());
        var reference = Trim(request.TransmissionReference, 120);
        var resultCode = Trim(request.ResultCode, 60);
        var identity = ComputeIdentity(reference, fileName, request.Outcome, resultCode);

        var duplicate = await context.AchFileTransportResults
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ExternalEventId == eventId || x.FunctionalIdentityHash == identity, ct);
        if (duplicate is not null)
        {
            return Map(duplicate, true, await ResolveLifecycleAsync(duplicate.AchFileExportId, ct));
        }

        var candidates = await context.AchFileExports
            .Where(x => x.IsEncrypted
                        && x.ExportKind == "RETURN"
                        && x.FileName == fileName
                        && x.TransmissionReference == reference)
            .ToListAsync(ct);
        var correlation = candidates.Count switch
        {
            0 => AchResponseCorrelationStatus.NotFound,
            1 => AchResponseCorrelationStatus.Matched,
            _ => AchResponseCorrelationStatus.Ambiguous
        };
        var export = candidates.Count == 1 ? candidates[0] : null;
        var now = DateTime.UtcNow;
        var result = new AchFileTransportResult
        {
            Id = Guid.NewGuid(),
            AchFileExportId = export?.Id,
            ExternalEventId = eventId,
            FunctionalIdentityHash = identity,
            FileName = fileName,
            TransmissionReference = reference,
            Outcome = request.Outcome,
            ResultCode = resultCode,
            ResultSummary = Trim(request.ResultSummary ?? string.Empty, 500),
            OccurredAtUtc = request.OccurredAtUtc,
            ReceivedAtUtc = now,
            CorrelationStatus = correlation,
            RequiresManualReview = correlation != AchResponseCorrelationStatus.Matched
                                   || request.Outcome == AchOutboundReturnOutcome.Unknown
        };

        await using var transaction = context.Database.IsRelational()
            ? await context.Database.BeginTransactionAsync(ct)
            : null;
        try
        {
            if (export is not null && request.Outcome != AchOutboundReturnOutcome.Unknown)
            {
                var target = request.Outcome switch
                {
                    AchOutboundReturnOutcome.Acknowledged => AchFileExportLifecycleStatus.Acknowledged,
                    AchOutboundReturnOutcome.Accepted => AchFileExportLifecycleStatus.Accepted,
                    AchOutboundReturnOutcome.Rejected => AchFileExportLifecycleStatus.Rejected,
                    _ => throw new InvalidOperationException("Resultado Return Out no soportado.")
                };

                if (export.LifecycleStatus is AchFileExportLifecycleStatus.Accepted or AchFileExportLifecycleStatus.Rejected
                    && export.LifecycleStatus != target)
                {
                    result.CorrelationStatus = AchResponseCorrelationStatus.ManualReviewRequired;
                    result.RequiresManualReview = true;
                }
                else if (export.LifecycleStatus == target)
                {
                    result.ProcessedAtUtc = now;
                }
                else
                {
                    await evidenceRecorder.RecordAsync(new AchFileTransmissionEvidence(
                        export.Id,
                        target,
                        reference,
                        request.OccurredAtUtc,
                        resultCode), ct);
                    result.Applied = true;
                    result.ProcessedAtUtc = now;
                }
            }

            context.AchFileTransportResults.Add(result);
            await context.SaveChangesAsync(ct);
            if (transaction is not null)
            {
                await transaction.CommitAsync(ct);
            }
        }
        catch (DbUpdateException)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            context.ChangeTracker.Clear();
            var winner = await context.AchFileTransportResults
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ExternalEventId == eventId || x.FunctionalIdentityHash == identity, CancellationToken.None);
            if (winner is not null)
            {
                return Map(winner, true, await ResolveLifecycleAsync(winner.AchFileExportId, CancellationToken.None));
            }
            throw;
        }

        var lifecycle = export is null ? null : await ResolveLifecycleAsync(export.Id, ct);
        return Map(result, false, lifecycle);
    }

    private async Task<AchFileExportLifecycleStatus?> ResolveLifecycleAsync(int? exportId, CancellationToken ct)
        => exportId.HasValue
            ? await context.AchFileExports.AsNoTracking()
                .Where(x => x.Id == exportId.Value)
                .Select(x => (AchFileExportLifecycleStatus?)x.LifecycleStatus)
                .SingleOrDefaultAsync(ct)
            : null;

    private static AchOutboundReturnResultProcessingResult Map(
        AchFileTransportResult result,
        bool wasDuplicate,
        AchFileExportLifecycleStatus? lifecycle)
        => new(
            result.Id,
            wasDuplicate,
            result.CorrelationStatus,
            result.AchFileExportId,
            lifecycle,
            result.Applied,
            result.RequiresManualReview,
            result.ResultCode);

    private static void Validate(AchOutboundReturnResultRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ExternalEventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TransmissionReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ResultCode);
        if (!string.Equals(Path.GetFileName(request.FileName.Trim()), request.FileName.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("El nombre del artefacto de resultado no es válido.", nameof(request));
        }
        if (request.OccurredAtUtc == default)
        {
            throw new ArgumentException("El resultado requiere fecha de ocurrencia.", nameof(request));
        }
    }

    private static string ComputeIdentity(
        string reference,
        string fileName,
        AchOutboundReturnOutcome outcome,
        string resultCode)
    {
        var canonical = $"{reference}|{fileName}|{outcome}|{resultCode}".ToUpperInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static string Trim(string value, int maxLength)
    {
        var trimmed = value.Trim();
        return trimmed[..Math.Min(trimmed.Length, maxLength)];
    }
}
