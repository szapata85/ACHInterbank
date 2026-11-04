using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;

/// <summary>
/// APN-I12: stable, id-keyset pages of all potentially eligible pending prenotifications.
/// A null persisted deadline means "not calculated", not "never evaluate": the job
/// still recalculates it and applies all transmission/response safeguards before changing state.
/// Each page is materialized before the job issues other queries on the same DbContext.
/// </summary>
public static class PrenotificationCandidateScan
{
    public const string CursorParameterKey = "ApnI12LastExaminedTransactionId";

    public static int ParseCursor(string? value) =>
        int.TryParse(value, out var cursor) && cursor > 0 ? cursor : 0;

    public static Task<List<int>> LoadPageAsync(
        AchDbContext db, DateTimeOffset now, int afterId, int maxId, int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageSize <= 0 || afterId < 0 || maxId < 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Invalid candidate scan bounds.");
        if (afterId >= maxId) return Task.FromResult(new List<int>());

        var canonical = db.PrenotificationValidationAttempts.AsNoTracking()
            .Where(x => x.AchTransactionId.HasValue
                        && x.AchTransaction!.IsPrenotification
                        && x.AchTransaction.State == AchTransferStateEnum.Pending
                        && x.Status == PrenotificationValidationAttemptStatus.AwaitingResponse
                        && (!x.ResponseDeadlineAtUtc.HasValue || x.ResponseDeadlineAtUtc.Value <= now)
                        && x.ThirdPartyAccountValidation.PolicyRule != null
                        && x.ThirdPartyAccountValidation.PolicyRule.TacitAcceptanceEnabled)
            .Select(x => x.AchTransactionId!.Value);

        var legacy = db.AchTransactions.AsNoTracking()
            .Where(t => t.IsPrenotification
                        && t.State == AchTransferStateEnum.Pending
                        && !db.PrenotificationValidationAttempts.Any(a => a.AchTransactionId == t.Id))
            .Select(t => t.Id);

        // UNION deduplicates IDs even if a validation has inconsistent attempts.
        // Keyset order is stable when another candidate is accepted in the same run;
        // offset pagination would skip IDs after the accepted rows disappear.
        return canonical.Union(legacy)
            .Where(id => id > afterId && id <= maxId)
            .OrderBy(id => id)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}
