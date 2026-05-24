using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class BatchNumberSequenceStore(
    AchDbContext context,
    ILogger<BatchNumberSequenceStore>? logger = null) : IBatchNumberSequenceStore
{
    private const int MaxAttempts = 3;

    public async Task<BatchNumberRangeReservation> ReserveRangeAsync(
        BatchNumberSequenceScope scope,
        int count,
        CancellationToken ct = default)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "count must be > 0");
        }

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var strategy = context.Database.CreateExecutionStrategy();
                return await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await context.Database.BeginTransactionAsync(ct);
                    var sequence = await context.BatchNumberSequences
                        .SingleOrDefaultAsync(x =>
                            x.ClearingHouseId == scope.ClearingHouseId
                            && x.OriginatingDfi == scope.OriginatingDfi
                            && x.ProcessingDate == scope.ProcessingDate
                            && x.PolicyCode == scope.PolicyCode, ct);

                    var wasCreated = false;
                    var previous = 0;
                    var start = 1;
                    var end = count;

                    if (sequence is null)
                    {
                        wasCreated = true;
                        sequence = new BatchNumberSequence
                        {
                            ClearingHouseId = scope.ClearingHouseId,
                            OriginatingDfi = scope.OriginatingDfi,
                            ProcessingDate = scope.ProcessingDate,
                            PolicyCode = scope.PolicyCode,
                            LastAssignedValue = count,
                            RowVersion = RandomNumberGenerator.GetBytes(8)
                        };

                        context.BatchNumberSequences.Add(sequence);
                    }
                    else
                    {
                        previous = sequence.LastAssignedValue;
                        start = previous + 1;
                        end = previous + count;
                        sequence.LastAssignedValue = end;
                        sequence.RowVersion = RandomNumberGenerator.GetBytes(8);
                    }

                    await context.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);

                    return new BatchNumberRangeReservation(
                        Scope: scope,
                        PreviousValue: previous,
                        StartValue: start,
                        EndValue: end,
                        WasCreated: wasCreated,
                        ReservedCount: count,
                        AttemptCount: attempt);
                });
            }
            catch (DbUpdateException ex) when (attempt < MaxAttempts && LooksLikeUniqueViolation(ex))
            {
                logger?.LogWarning(ex,
                    "BatchNumberSequence unique race. Retry {Attempt}/{MaxAttempts} for scope {Scope}.",
                    attempt,
                    MaxAttempts,
                    scope.ToScopeKey());
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < MaxAttempts)
            {
                logger?.LogWarning(ex,
                    "BatchNumberSequence concurrency retry {Attempt}/{MaxAttempts} for scope {Scope}.",
                    attempt,
                    MaxAttempts,
                    scope.ToScopeKey());
            }
        }

        throw new InvalidOperationException($"Could not reserve batch number sequence for scope {scope.ToScopeKey()}.");
    }

    private static bool LooksLikeUniqueViolation(DbUpdateException ex)
    {
        var text = ex.InnerException?.Message ?? ex.Message;
        return text.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
               || text.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
               || text.Contains("23505", StringComparison.OrdinalIgnoreCase);
    }
}
