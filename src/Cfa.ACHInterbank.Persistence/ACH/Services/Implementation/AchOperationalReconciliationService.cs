using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public sealed class AchOperationalReconciliationService : IAchOperationalReconciliationService
{
    private readonly AchDbContext _db;
    private readonly TimeProvider _timeProvider;

    public AchOperationalReconciliationService(AchDbContext db, TimeProvider? timeProvider = null)
    {
        _db = db;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task<AchOperationalReconciliationSnapshot?> GetLatestAsync(
        int clearingHouseId,
        DateOnly operationalDate,
        string achCycleId,
        CancellationToken ct = default)
        => _db.AchOperationalReconciliationSnapshots
            .AsNoTracking()
            .Include(x => x.Differences)
            .Where(x => x.ClearingHouseId == clearingHouseId
                && x.OperationalDate == operationalDate
                && x.AchCycleId == achCycleId)
            .OrderByDescending(x => x.Revision)
            .FirstOrDefaultAsync(ct);

    public async Task<AchOperationalReconciliationResult> ReconcileAsync(
        AchOperationalReconciliationRequest request,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AchCycleId);

        var cycle = await _db.AchCycles.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.AchCycleId && x.ClearingHouseId == request.ClearingHouseId, ct)
            ?? throw new InvalidOperationException("The reconciliation cycle does not belong to the requested clearing house.");
        if (DateOnly.FromDateTime(cycle.ProcessingDate) != request.OperationalDate)
        {
            throw new InvalidOperationException("The operational date does not match the persisted cycle.");
        }

        var transactions = await _db.AchTransactions.AsNoTracking()
            .Where(x => x.AchCycleId == request.AchCycleId)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);
        var defaultInstitutionIds = await _db.FinancialInstitutions.AsNoTracking()
            .Where(x => x.IsDefaultSource)
            .Select(x => x.Id)
            .ToListAsync(ct);

        var sent = transactions.Where(x => x.Direction == AchTransactionDirection.Outgoing).ToList();
        var received = transactions.Where(x => x.Direction == AchTransactionDirection.Incoming
            && x.Type != TransactionTypeEnum.Return).ToList();
        var applied = received.Where(x => x.State is AchTransferStateEnum.AppliedTacitly or AchTransferStateEnum.Certified).ToList();
        var participantReturns = received.Where(x => x.State == AchTransferStateEnum.ReturnedByEpr).ToList();
        var operatorReturns = transactions.Where(x => x.State == AchTransferStateEnum.ReturnedByOperator).ToList();
        var internalNetPosition = CalculateInternalNetPosition(transactions, defaultInstitutionIds);
        var fingerprint = BuildFingerprint(request, transactions, internalNetPosition);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var existing = await _db.AchOperationalReconciliationSnapshots
                .AsNoTracking()
                .Include(x => x.Differences)
                .SingleOrDefaultAsync(x => x.ClearingHouseId == request.ClearingHouseId
                    && x.OperationalDate == request.OperationalDate
                    && x.AchCycleId == request.AchCycleId
                    && x.SourceFingerprint == fingerprint, ct);
            if (existing is not null)
            {
                return new AchOperationalReconciliationResult(existing, true);
            }

            var latestRevision = await _db.AchOperationalReconciliationSnapshots
                .Where(x => x.ClearingHouseId == request.ClearingHouseId
                    && x.OperationalDate == request.OperationalDate
                    && x.AchCycleId == request.AchCycleId)
                .Select(x => (int?)x.Revision)
                .MaxAsync(ct) ?? 0;
            var calculatedAt = _timeProvider.GetUtcNow();
            var snapshot = new AchOperationalReconciliationSnapshot
            {
                Id = Guid.NewGuid(),
                ClearingHouseId = request.ClearingHouseId,
                OperationalDate = request.OperationalDate,
                AchCycleId = request.AchCycleId,
                Revision = latestRevision + 1,
                SourceFingerprint = fingerprint,
                SentCount = sent.Count,
                SentAmount = sent.Sum(x => x.Amount),
                ReceivedCount = received.Count,
                ReceivedAmount = received.Sum(x => x.Amount),
                AppliedCount = applied.Count,
                AppliedAmount = applied.Sum(x => x.Amount),
                ParticipantReturnCount = participantReturns.Count,
                ParticipantReturnAmount = participantReturns.Sum(x => x.Amount),
                OperatorReturnCount = operatorReturns.Count,
                OperatorReturnAmount = operatorReturns.Sum(x => x.Amount),
                InternalExpectedNetPosition = internalNetPosition,
                ExternalEvidenceReference = Normalize(request.ExternalEvidence?.EvidenceReference),
                ExternalSentCount = request.ExternalEvidence?.SentCount,
                ExternalSentAmount = request.ExternalEvidence?.SentAmount,
                ExternalReceivedCount = request.ExternalEvidence?.ReceivedCount,
                ExternalReceivedAmount = request.ExternalEvidence?.ReceivedAmount,
                ExternalNetPosition = request.ExternalEvidence?.NetPosition,
                ExternalEvidenceRecordedAt = request.ExternalEvidence?.RecordedAt,
                CalculatedAt = calculatedAt,
                CalculatedBy = Normalize(request.CalculatedBy) ?? "system",
                Version = Guid.NewGuid()
            };

            AddDifferences(snapshot, request.ExternalEvidence, calculatedAt);
            snapshot.Status = snapshot.Differences.Count > 0
                ? AchOperationalReconciliationStatus.Differences
                : request.ExternalEvidence?.IsComplete == true && snapshot.InternalExpectedNetPosition.HasValue
                    ? AchOperationalReconciliationStatus.Balanced
                    : AchOperationalReconciliationStatus.PendingExternalEvidence;

            _db.AchOperationalReconciliationSnapshots.Add(snapshot);
            try
            {
                await _db.SaveChangesAsync(ct);
                return new AchOperationalReconciliationResult(snapshot, false);
            }
            catch (DbUpdateException ex) when (RelationalDatabaseExceptionClassifier.IsUniqueViolation(ex))
            {
                _db.ChangeTracker.Clear();
                if (attempt == 2)
                {
                    throw;
                }
            }
        }

        throw new InvalidOperationException("The reconciliation revision could not be persisted.");
    }

    private static decimal? CalculateInternalNetPosition(
        IReadOnlyList<AchTransaction> transactions,
        IReadOnlyList<int> defaultInstitutionIds)
    {
        if (defaultInstitutionIds.Count != 1)
        {
            return null;
        }

        var institutionId = defaultInstitutionIds[0];
        var compensable = transactions.Where(x => x.State is not (AchTransferStateEnum.ReturnedByOperator or AchTransferStateEnum.ReturnedByEpr));
        return compensable.Sum(x => (x.DestinationInstitutionId == institutionId ? x.Amount : 0m)
            - (x.SourceInstitutionId == institutionId ? x.Amount : 0m));
    }

    private static void AddDifferences(
        AchOperationalReconciliationSnapshot snapshot,
        AchOperationalReconciliationExternalEvidence? evidence,
        DateTimeOffset detectedAt)
    {
        AddDifference(snapshot, AchOperationalReconciliationDifferenceCategory.ReceivedApplicationInvariant,
            snapshot.ReceivedCount, snapshot.AppliedCount + snapshot.ParticipantReturnCount,
            "ACH-Colombia-V35:2.6.2", detectedAt);

        if (evidence?.IsComplete != true)
        {
            return;
        }

        AddDifference(snapshot, AchOperationalReconciliationDifferenceCategory.ExternalSentCount,
            snapshot.SentCount - snapshot.OperatorReturnCount, evidence.SentCount, evidence.EvidenceReference!, detectedAt);
        AddDifference(snapshot, AchOperationalReconciliationDifferenceCategory.ExternalSentAmount,
            snapshot.SentAmount - snapshot.OperatorReturnAmount, evidence.SentAmount, evidence.EvidenceReference!, detectedAt);
        AddDifference(snapshot, AchOperationalReconciliationDifferenceCategory.ExternalReceivedCount,
            snapshot.ReceivedCount, evidence.ReceivedCount, evidence.EvidenceReference!, detectedAt);
        AddDifference(snapshot, AchOperationalReconciliationDifferenceCategory.ExternalReceivedAmount,
            snapshot.ReceivedAmount, evidence.ReceivedAmount, evidence.EvidenceReference!, detectedAt);
        if (snapshot.InternalExpectedNetPosition.HasValue)
        {
            AddDifference(snapshot, AchOperationalReconciliationDifferenceCategory.ExternalNetPosition,
                snapshot.InternalExpectedNetPosition, evidence.NetPosition, evidence.EvidenceReference!, detectedAt);
        }
    }

    private static void AddDifference(
        AchOperationalReconciliationSnapshot snapshot,
        AchOperationalReconciliationDifferenceCategory category,
        decimal? internalValue,
        decimal? externalValue,
        string evidenceSource,
        DateTimeOffset detectedAt)
    {
        if (internalValue == externalValue)
        {
            return;
        }

        snapshot.Differences.Add(new AchOperationalReconciliationDifference
        {
            Id = Guid.NewGuid(),
            SnapshotId = snapshot.Id,
            Category = category,
            InternalValue = internalValue,
            ExternalValue = externalValue,
            Delta = internalValue.HasValue && externalValue.HasValue ? internalValue.Value - externalValue.Value : null,
            EvidenceSource = evidenceSource,
            DetectedAt = detectedAt
        });
    }

    private static string BuildFingerprint(
        AchOperationalReconciliationRequest request,
        IReadOnlyList<AchTransaction> transactions,
        decimal? internalNetPosition)
    {
        var source = new StringBuilder()
            .Append(request.ClearingHouseId).Append('|')
            .Append(request.OperationalDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append('|')
            .Append(request.AchCycleId).Append('|')
            .Append(internalNetPosition?.ToString(CultureInfo.InvariantCulture) ?? "NA").Append('|');
        foreach (var tx in transactions)
        {
            source.Append(tx.Id).Append(':').Append(tx.Amount.ToString(CultureInfo.InvariantCulture)).Append(':')
                .Append((int)tx.Direction).Append(':').Append((int)tx.Type).Append(':').Append((int)tx.State).Append(':')
                .Append(tx.SourceInstitutionId).Append(':').Append(tx.DestinationInstitutionId).Append(';');
        }

        var evidence = request.ExternalEvidence;
        source.Append('|').Append(Normalize(evidence?.EvidenceReference) ?? "NA")
            .Append('|').Append(evidence?.SentCount?.ToString(CultureInfo.InvariantCulture) ?? "NA")
            .Append('|').Append(evidence?.SentAmount?.ToString(CultureInfo.InvariantCulture) ?? "NA")
            .Append('|').Append(evidence?.ReceivedCount?.ToString(CultureInfo.InvariantCulture) ?? "NA")
            .Append('|').Append(evidence?.ReceivedAmount?.ToString(CultureInfo.InvariantCulture) ?? "NA")
            .Append('|').Append(evidence?.NetPosition?.ToString(CultureInfo.InvariantCulture) ?? "NA")
            .Append('|').Append(evidence?.RecordedAt?.ToString("O", CultureInfo.InvariantCulture) ?? "NA");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source.ToString())));
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
