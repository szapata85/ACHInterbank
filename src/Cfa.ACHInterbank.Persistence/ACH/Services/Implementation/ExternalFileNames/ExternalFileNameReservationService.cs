using System.Data;
using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.ACH.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

[Scoped]
public sealed class ExternalFileNameReservationService : IExternalFileNameReservationService
{
    private const int MaxAttempts = 4;
    private readonly AchDbContext _context;
    private readonly IExternalFileNameSequenceService _sequenceService;

    public ExternalFileNameReservationService(
        AchDbContext context,
        IExternalFileNameSequenceService sequenceService)
    {
        _context = context;
        _sequenceService = sequenceService;
    }

    public async Task<ExternalFileNameReservationResult> ReserveAsync(
        ExternalFileNameContext context,
        string requestFingerprint,
        CancellationToken ct = default)
    {
        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(
            () => ReserveWithinExecutionStrategyAsync(context, requestFingerprint, ct));
    }

    private async Task<ExternalFileNameReservationResult> ReserveWithinExecutionStrategyAsync(
        ExternalFileNameContext context,
        string requestFingerprint,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(context.IdempotencyKey))
        {
            throw new InvalidOperationException("ACH_EXTERNAL_FILENAME_IDEMPOTENCY_KEY_REQUIRED: la solicitud no tiene clave idempotente.");
        }

        var idempotencyHash = Hash(context.IdempotencyKey);
        var fingerprintHash = Hash(requestFingerprint);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var existing = await FindExistingAsync(context.ClearingHouseId, idempotencyHash, ct);
            if (existing is not null)
            {
                return ValidateAndMap(existing, fingerprintHash, wasReused: true);
            }

            IDbContextTransaction? transaction = null;
            ExternalFileNameReservation? added = null;
            try
            {
                if (_context.Database.CurrentTransaction is null)
                {
                    transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                }

                existing = await FindExistingAsync(context.ClearingHouseId, idempotencyHash, ct);
                if (existing is not null)
                {
                    if (transaction is not null)
                    {
                        await transaction.CommitAsync(ct);
                    }

                    return ValidateAndMap(existing, fingerprintHash, wasReused: true);
                }

                var sequence = await _sequenceService.ReserveNextSequenceAsync(context, ct);
                var now = context.OperationalTimeSnapshot?.CapturedAtUtc ?? DateTime.UtcNow;
                added = new ExternalFileNameReservation
                {
                    ClearingHouseId = context.ClearingHouseId,
                    ScopeCode = ExternalFileNameSupport.GetSequenceScopeCode(context),
                    OperationalDate = DateOnly.FromDateTime(context.ProcessingDate),
                    IdempotencyKeyHash = idempotencyHash,
                    RequestFingerprintHash = fingerprintHash,
                    Sequence = sequence,
                    Status = "Reserved",
                    ReservedAtUtc = now,
                    LastAccessedAtUtc = now,
                    CreatedBy = NormalizeActor(context.RequestedBy),
                    RowVersion = [1]
                };
                _context.ExternalFileNameReservations.Add(added);
                await _context.SaveChangesAsync(ct);

                if (transaction is not null)
                {
                    await transaction.CommitAsync(ct);
                }

                return ValidateAndMap(added, fingerprintHash, wasReused: false);
            }
            catch (Exception ex) when (attempt < MaxAttempts && IsTransientReservationConflict(ex))
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }

                if (added is not null)
                {
                    _context.Entry(added).State = EntityState.Detached;
                }

                foreach (var sequenceEntry in _context.ChangeTracker.Entries<ExternalFileSequence>().ToList())
                {
                    sequenceEntry.State = EntityState.Detached;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(15 * attempt), ct);
            }
            finally
            {
                if (transaction is not null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        var winner = await FindExistingAsync(context.ClearingHouseId, idempotencyHash, ct);
        if (winner is not null)
        {
            return ValidateAndMap(winner, fingerprintHash, wasReused: true);
        }

        throw new InvalidOperationException("ACH_EXTERNAL_FILENAME_RESERVATION_CONFLICT: no fue posible resolver la reserva concurrente.");
    }

    public async Task CompleteAsync(
        long reservationId,
        string externalFileName,
        char? fileIdModifier,
        CancellationToken ct = default)
    {
        var reservation = await _context.ExternalFileNameReservations.SingleAsync(x => x.Id == reservationId, ct);
        if (string.Equals(reservation.Status, "Completed", StringComparison.Ordinal))
        {
            if (!string.Equals(reservation.ExternalFileName, externalFileName, StringComparison.Ordinal)
                || reservation.FileIdModifier != fileIdModifier?.ToString())
            {
                throw new InvalidOperationException("ACH_EXTERNAL_FILENAME_IDEMPOTENCY_MISMATCH: la reserva completada no coincide con el resultado recalculado.");
            }

            return;
        }

        var now = reservation.ReservedAtUtc;
        reservation.ExternalFileName = externalFileName;
        reservation.FileIdModifier = fileIdModifier?.ToString();
        reservation.Status = "Completed";
        reservation.CompletedAtUtc = now;
        reservation.LastAccessedAtUtc = now;
        reservation.RowVersion = BitConverter.GetBytes(now.Ticks);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.Entry(reservation).State = EntityState.Detached;
            var winner = await _context.ExternalFileNameReservations.AsNoTracking()
                .SingleAsync(x => x.Id == reservationId, ct);
            if (string.Equals(winner.Status, "Completed", StringComparison.Ordinal)
                && string.Equals(winner.ExternalFileName, externalFileName, StringComparison.Ordinal)
                && winner.FileIdModifier == fileIdModifier?.ToString())
            {
                return;
            }

            throw new InvalidOperationException(
                "ACH_EXTERNAL_FILENAME_COMPLETION_CONFLICT: la reserva fue completada con un resultado diferente.");
        }
    }

    private Task<ExternalFileNameReservation?> FindExistingAsync(int clearingHouseId, string idempotencyHash, CancellationToken ct)
        => _context.ExternalFileNameReservations
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.ClearingHouseId == clearingHouseId
                                    && x.IdempotencyKeyHash == idempotencyHash, ct);

    private static ExternalFileNameReservationResult ValidateAndMap(
        ExternalFileNameReservation reservation,
        string fingerprintHash,
        bool wasReused)
    {
        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(reservation.RequestFingerprintHash),
                Convert.FromHexString(fingerprintHash)))
        {
            throw new InvalidOperationException("ACH_EXTERNAL_FILENAME_IDEMPOTENCY_MISMATCH: la misma clave fue utilizada para una solicitud lógica diferente.");
        }

        return new ExternalFileNameReservationResult(
            reservation.Id,
            reservation.Sequence,
            wasReused,
            reservation.IdempotencyKeyHash,
            reservation.RequestFingerprintHash,
            reservation.ExternalFileName,
            string.IsNullOrWhiteSpace(reservation.FileIdModifier) ? null : reservation.FileIdModifier[0]);
    }

    private static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static bool IsTransientReservationConflict(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is DbUpdateConcurrencyException)
            {
                return true;
            }

            var sqlState = current.GetType().GetProperty("SqlState")?.GetValue(current)?.ToString();
            if (sqlState is "23505" or "40001" or "40P01")
            {
                return true;
            }

            if (current.GetType().GetProperty("Number")?.GetValue(current) is int number
                && number is 1205 or 2601 or 2627)
            {
                return true;
            }

            if (current.GetType().GetProperty("SqliteErrorCode")?.GetValue(current) is int sqliteCode
                && sqliteCode is 5 or 6 or 19)
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeActor(string? actor)
        => string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim()[..Math.Min(actor.Trim().Length, 120)];
}
