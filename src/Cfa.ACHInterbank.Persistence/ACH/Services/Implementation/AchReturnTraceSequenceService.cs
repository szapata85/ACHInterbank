using System.Data;
using System.Data.Common;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class AchReturnTraceSequenceService(AchDbContext context) : IAchReturnTraceSequenceService
{
    private const int MaximumSequence = 6_999_999;

    public async Task<AchReturnTraceRange> ReserveRangeAsync(
        string participantDfi,
        DateOnly sequenceDate,
        int count,
        DateTime capturedAtUtc,
        CancellationToken ct = default)
    {
        if (participantDfi.Length != 8 || participantDfi.Any(ch => !char.IsDigit(ch)))
        {
            throw new InvalidOperationException("RETURN_TRACE_PARTICIPANT_INVALID: el participante generador debe tener 8 dígitos.");
        }

        if (count is <= 0 or > MaximumSequence)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        var providerName = context.Database.ProviderName ?? string.Empty;
        if (providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            return await ReserveSqlServerAsync(participantDfi, sequenceDate, count, capturedAtUtc, ct);
        }

        if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase)
            || providerName.Contains("Postgre", StringComparison.OrdinalIgnoreCase))
        {
            return await ReservePostgresAsync(participantDfi, sequenceDate, count, capturedAtUtc, ct);
        }

        if (providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            return await ReserveSqliteAsync(participantDfi, sequenceDate, count, capturedAtUtc, ct);
        }

        return await ReserveNonRelationalAsync(participantDfi, sequenceDate, count, capturedAtUtc, ct);
    }

    private async Task<AchReturnTraceRange> ReserveSqlServerAsync(
        string participantDfi,
        DateOnly sequenceDate,
        int count,
        DateTime capturedAtUtc,
        CancellationToken ct)
    {
        const string sql = """
            DECLARE @start int;
            DECLARE @end int;

            UPDATE dbo.AchReturnTraceSequences WITH (UPDLOCK, HOLDLOCK)
            SET @start = LastAssignedValue + 1,
                @end = LastAssignedValue + @count,
                LastAssignedValue = LastAssignedValue + @count,
                UpdatedAtUtc = @capturedAtUtc
            WHERE ParticipantDfi = @participantDfi
              AND SequenceDate = @sequenceDate
              AND LastAssignedValue <= @maximumSequence - @count;

            IF @@ROWCOUNT = 0
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM dbo.AchReturnTraceSequences WITH (UPDLOCK, HOLDLOCK)
                    WHERE ParticipantDfi = @participantDfi AND SequenceDate = @sequenceDate)
                BEGIN
                    THROW 51037, 'ACH_RETURN_TRACE_SEQUENCE_LIMIT', 1;
                END;

                INSERT INTO dbo.AchReturnTraceSequences
                    (ParticipantDfi, SequenceDate, LastAssignedValue, UpdatedAtUtc)
                VALUES (@participantDfi, @sequenceDate, @count, @capturedAtUtc);
                SET @start = 1;
                SET @end = @count;
            END;

            SELECT @start, @end;
            """;

        try
        {
            return await ExecuteRangeCommandAsync(
                sql,
                participantDfi,
                sequenceDate.ToDateTime(TimeOnly.MinValue),
                count,
                capturedAtUtc,
                ownsSerializableTransaction: true,
                ct);
        }
        catch (SqlException ex) when (ex.Number == 51037)
        {
            throw SequenceLimit(ex);
        }
    }

    private async Task<AchReturnTraceRange> ReservePostgresAsync(
        string participantDfi,
        DateOnly sequenceDate,
        int count,
        DateTime capturedAtUtc,
        CancellationToken ct)
    {
        const string sql = """
            INSERT INTO "AchReturnTraceSequences"
                ("ParticipantDfi", "SequenceDate", "LastAssignedValue", "UpdatedAtUtc")
            VALUES (@participantDfi, @sequenceDate, @count, @capturedAtUtc)
            ON CONFLICT ("ParticipantDfi", "SequenceDate")
            DO UPDATE SET
                "LastAssignedValue" = "AchReturnTraceSequences"."LastAssignedValue" + @count,
                "UpdatedAtUtc" = @capturedAtUtc
            WHERE "AchReturnTraceSequences"."LastAssignedValue" <= @maximumSequence - @count
            RETURNING "LastAssignedValue" - @count + 1, "LastAssignedValue";
            """;

        return await ExecuteRangeCommandAsync(
            sql,
            participantDfi,
            sequenceDate,
            count,
            capturedAtUtc,
            ownsSerializableTransaction: false,
            ct);
    }

    private async Task<AchReturnTraceRange> ReserveSqliteAsync(
        string participantDfi,
        DateOnly sequenceDate,
        int count,
        DateTime capturedAtUtc,
        CancellationToken ct)
    {
        const string sql = """
            INSERT INTO "AchReturnTraceSequences"
                ("ParticipantDfi", "SequenceDate", "LastAssignedValue", "UpdatedAtUtc")
            VALUES (@participantDfi, @sequenceDate, @count, @capturedAtUtc)
            ON CONFLICT ("ParticipantDfi", "SequenceDate")
            DO UPDATE SET
                "LastAssignedValue" = "LastAssignedValue" + @count,
                "UpdatedAtUtc" = @capturedAtUtc
            WHERE "LastAssignedValue" <= @maximumSequence - @count
            RETURNING "LastAssignedValue" - @count + 1, "LastAssignedValue";
            """;

        return await ExecuteRangeCommandAsync(
            sql,
            participantDfi,
            sequenceDate.ToString("yyyy-MM-dd"),
            count,
            capturedAtUtc,
            ownsSerializableTransaction: false,
            ct);
    }

    private async Task<AchReturnTraceRange> ExecuteRangeCommandAsync(
        string sql,
        string participantDfi,
        object sequenceDate,
        int count,
        DateTime capturedAtUtc,
        bool ownsSerializableTransaction,
        CancellationToken ct)
    {
        var connection = context.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
        {
            await connection.OpenAsync(ct);
        }

        var ambientTransaction = context.Database.CurrentTransaction?.GetDbTransaction();
        DbTransaction? ownedTransaction = null;
        try
        {
            if (ambientTransaction is null && ownsSerializableTransaction)
            {
                ownedTransaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            }

            await using var command = connection.CreateCommand();
            command.Transaction = ambientTransaction ?? ownedTransaction;
            command.CommandText = sql;
            AddParameter(command, "@participantDfi", participantDfi);
            AddParameter(command, "@sequenceDate", sequenceDate);
            AddParameter(command, "@count", count);
            AddParameter(command, "@maximumSequence", MaximumSequence);
            AddParameter(command, "@capturedAtUtc", capturedAtUtc);

            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
            {
                throw SequenceLimit();
            }

            var range = new AchReturnTraceRange(reader.GetInt32(0), reader.GetInt32(1));
            if (ownedTransaction is not null)
            {
                await ownedTransaction.CommitAsync(ct);
            }

            return range;
        }
        catch
        {
            if (ownedTransaction is not null)
            {
                await ownedTransaction.RollbackAsync(CancellationToken.None);
            }

            throw;
        }
        finally
        {
            if (ownedTransaction is not null)
            {
                await ownedTransaction.DisposeAsync();
            }

            if (openedHere)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task<AchReturnTraceRange> ReserveNonRelationalAsync(
        string participantDfi,
        DateOnly sequenceDate,
        int count,
        DateTime capturedAtUtc,
        CancellationToken ct)
    {
        var row = await context.AchReturnTraceSequences.SingleOrDefaultAsync(
            x => x.ParticipantDfi == participantDfi && x.SequenceDate == sequenceDate,
            ct);
        var start = (row?.LastAssignedValue ?? 0) + 1;
        var end = start + count - 1;
        if (end > MaximumSequence)
        {
            throw SequenceLimit();
        }

        if (row is null)
        {
            context.AchReturnTraceSequences.Add(new AchReturnTraceSequence
            {
                ParticipantDfi = participantDfi,
                SequenceDate = sequenceDate,
                LastAssignedValue = end,
                UpdatedAtUtc = capturedAtUtc
            });
        }
        else
        {
            row.LastAssignedValue = end;
            row.UpdatedAtUtc = capturedAtUtc;
        }

        return new AchReturnTraceRange(start, end);
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static InvalidOperationException SequenceLimit(Exception? inner = null)
        => new(
            "RETURN_TRACE_SEQUENCE_EXHAUSTED: se agotó el consecutivo diario de 7 posiciones para el participante generador.",
            inner);
}
