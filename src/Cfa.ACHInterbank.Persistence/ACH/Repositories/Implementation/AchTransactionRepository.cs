using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Data.SqlClient;
using Npgsql;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace Cfa.ACHInterbank.Persistence.ACH.Repositories.Implementation;

[Scoped]
public class AchTransactionRepository : IAchTransactionRepository
{
    private readonly AchDbContext _context;

    public AchTransactionRepository(AchDbContext context)
    {
        _context = context;
    }

    private const int MaximumTraceSequence = 6_999_999;

    public async Task<int> AllocateNextTraceSequenceAsync(
        DateOnly processingDate,
        string traceOriginatingDfi,
        DateTime allocatedAtUtc,
        CancellationToken ct = default)
    {
        var providerName = _context.Database.ProviderName ?? string.Empty;
        if (providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            return await AllocateSqlServerAsync(processingDate, traceOriginatingDfi, allocatedAtUtc, ct);
        }

        if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase)
            || providerName.Contains("Postgre", StringComparison.OrdinalIgnoreCase))
        {
            return await AllocatePostgresAsync(processingDate, traceOriginatingDfi, allocatedAtUtc, ct);
        }

        if (providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            return await AllocateSqliteAsync(processingDate, traceOriginatingDfi, allocatedAtUtc, ct);
        }

        throw new NotSupportedException($"El proveedor '{providerName}' no soporta la asignación atómica de trazas ACH.");
    }

    private Task<int> AllocatePostgresAsync(DateOnly processingDate, string traceOriginatingDfi, DateTime allocatedAtUtc, CancellationToken ct)
        => ExecuteAllocationCommandAsync(
            """
            INSERT INTO "AchTransactionTraceSequences"
                ("OriginatingDfi", "SequenceDate", "LastAssignedValue", "UpdatedAtUtc")
            VALUES (@originatingDfi, @sequenceDate, 1, @allocatedAtUtc)
            ON CONFLICT ("OriginatingDfi", "SequenceDate")
            DO UPDATE SET
                "LastAssignedValue" = "AchTransactionTraceSequences"."LastAssignedValue" + 1,
                "UpdatedAtUtc" = @allocatedAtUtc
            WHERE "AchTransactionTraceSequences"."LastAssignedValue" < @maximumSequence
            RETURNING "LastAssignedValue";
            """,
            processingDate,
            traceOriginatingDfi,
            allocatedAtUtc,
            ownsSerializableTransaction: false,
            useDedicatedConnection: true,
            ct);

    private async Task<int> AllocateSqlServerAsync(DateOnly processingDate, string traceOriginatingDfi, DateTime allocatedAtUtc, CancellationToken ct)
    {
        const string sql = """
            DECLARE @next int;

            UPDATE dbo.AchTransactionTraceSequences WITH (UPDLOCK, HOLDLOCK)
            SET @next = LastAssignedValue + 1,
                LastAssignedValue = LastAssignedValue + 1,
                UpdatedAtUtc = @allocatedAtUtc
            WHERE OriginatingDfi = @originatingDfi
              AND SequenceDate = @sequenceDate
              AND LastAssignedValue < @maximumSequence;

            IF @@ROWCOUNT = 0
            BEGIN
                IF EXISTS (
                    SELECT 1
                    FROM dbo.AchTransactionTraceSequences WITH (UPDLOCK, HOLDLOCK)
                    WHERE OriginatingDfi = @originatingDfi
                      AND SequenceDate = @sequenceDate)
                BEGIN
                    THROW 51038, 'ACH_TRANSACTION_TRACE_SEQUENCE_LIMIT', 1;
                END;

                INSERT INTO dbo.AchTransactionTraceSequences
                    (OriginatingDfi, SequenceDate, LastAssignedValue, UpdatedAtUtc)
                VALUES (@originatingDfi, @sequenceDate, 1, @allocatedAtUtc);
                SET @next = 1;
            END;

            SELECT @next;
            """;

        try
        {
            return await ExecuteAllocationCommandAsync(
                sql,
                processingDate.ToDateTime(TimeOnly.MinValue),
                traceOriginatingDfi,
                allocatedAtUtc,
                ownsSerializableTransaction: true,
                useDedicatedConnection: true,
                ct);
        }
        catch (SqlException ex) when (ex.Number == 51038)
        {
            throw SequenceLimit(ex);
        }
    }

    private Task<int> AllocateSqliteAsync(DateOnly processingDate, string traceOriginatingDfi, DateTime allocatedAtUtc, CancellationToken ct)
        => ExecuteAllocationCommandAsync(
            """
            INSERT INTO "AchTransactionTraceSequences"
                ("OriginatingDfi", "SequenceDate", "LastAssignedValue", "UpdatedAtUtc")
            VALUES (@originatingDfi, @sequenceDate, 1, @allocatedAtUtc)
            ON CONFLICT ("OriginatingDfi", "SequenceDate")
            DO UPDATE SET
                "LastAssignedValue" = "LastAssignedValue" + 1,
                "UpdatedAtUtc" = @allocatedAtUtc
            WHERE "LastAssignedValue" < @maximumSequence
            RETURNING "LastAssignedValue";
            """,
            processingDate.ToString("yyyy-MM-dd"),
            traceOriginatingDfi,
            allocatedAtUtc,
            ownsSerializableTransaction: false,
            useDedicatedConnection: false,
            ct);

    private async Task<int> ExecuteAllocationCommandAsync(
        string sql,
        object processingDate,
        string traceOriginatingDfi,
        DateTime allocatedAtUtc,
        bool ownsSerializableTransaction,
        bool useDedicatedConnection,
        CancellationToken ct)
    {
        var contextConnection = _context.Database.GetDbConnection();
        var connection = useDedicatedConnection
            ? CreateDedicatedConnection(contextConnection)
            : contextConnection;
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
        {
            await connection.OpenAsync(ct);
        }

        var ambientTransaction = useDedicatedConnection
            ? null
            : _context.Database.CurrentTransaction?.GetDbTransaction();
        DbTransaction? ownedTransaction = null;
        var committed = false;
        try
        {
            if (ambientTransaction is null && ownsSerializableTransaction)
            {
                ownedTransaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            }

            await using var command = connection.CreateCommand();
            command.Transaction = ambientTransaction ?? ownedTransaction;
            command.CommandText = sql;
            AddParameter(command, "@originatingDfi", traceOriginatingDfi);
            AddParameter(command, "@sequenceDate", processingDate);
            AddParameter(command, "@allocatedAtUtc", allocatedAtUtc);
            AddParameter(command, "@maximumSequence", MaximumTraceSequence);

            var value = await command.ExecuteScalarAsync(ct);
            if (value is null or DBNull)
            {
                throw SequenceLimit();
            }

            if (ownedTransaction is not null)
            {
                await ownedTransaction.CommitAsync(ct);
                committed = true;
            }

            return Convert.ToInt32(value);
        }
        catch
        {
            if (ownedTransaction is not null && !committed && ownedTransaction.Connection is not null)
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

            if (useDedicatedConnection)
            {
                await connection.DisposeAsync();
            }
        }
    }

    private static DbConnection CreateDedicatedConnection(DbConnection contextConnection)
        => contextConnection switch
        {
            SqlConnection sqlServer => (SqlConnection)((ICloneable)sqlServer).Clone(),
            NpgsqlConnection postgres => (NpgsqlConnection)((ICloneable)postgres).Clone(),
            _ => throw new NotSupportedException(
                $"El proveedor '{contextConnection.GetType().Name}' no soporta una conexión dedicada para asignar trazas ACH.")
        };

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static InvalidOperationException SequenceLimit(Exception? inner = null)
        => new(
            "Error Fatal ID 7: el consecutivo diario excede el máximo permitido (6999999). El rango 7000001-9999999 está reservado para PSE.",
            inner);

    public Task AddAsync(AchTransaction transaction, CancellationToken ct = default)
    {
        _context.AchTransactions.Add(transaction);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<(TransactionTypeEnum Type, decimal Sum)>> GetTotalsByBatchAsync(AchBatch batch, CancellationToken ct = default)
    {
        var persisted = batch.Id > 0
            ? await _context.AchTransactions
                .AsNoTracking()
                .Where(t => t.AchBatchId == batch.Id)
                .Select(t => new { t.Id, t.Type, t.Amount })
                .ToListAsync(ct)
            : [];

        var tracked = _context.AchTransactions.Local
            .Where(t => ReferenceEquals(t.AchBatch, batch) || (batch.Id > 0 && t.AchBatchId == batch.Id))
            .Select(t => new { t.Id, t.Type, t.Amount, InstanceId = RuntimeHelpers.GetHashCode(t) })
            .ToList();

        return persisted
            .Select(t => new { Key = $"db:{t.Id}", t.Type, t.Amount })
            .Concat(tracked.Select(t => new
            {
                Key = t.Id > 0 ? $"db:{t.Id}" : $"mem:{t.InstanceId}",
                t.Type,
                t.Amount
            }))
            .GroupBy(t => t.Key)
            .Select(g => g.First())
            .GroupBy(t => t.Type)
            .Select(g => (Type: g.Key, Sum: g.Sum(x => x.Amount)))
            .ToList();
    }

    public async Task<IReadOnlyList<TransactionTypeEnum>> GetTypesByBatchAsync(AchBatch batch, CancellationToken ct = default)
    {
        var persistedTypes = batch.Id > 0
            ? await _context.AchTransactions
                .AsNoTracking()
                .Where(t => t.AchBatchId == batch.Id)
                .Select(t => new { t.Id, t.Type })
                .ToListAsync(ct)
            : [];

        var trackedTypes = _context.AchTransactions.Local
            .Where(t => ReferenceEquals(t.AchBatch, batch) || (batch.Id > 0 && t.AchBatchId == batch.Id))
            .Select(t => new { t.Id, t.Type, InstanceId = RuntimeHelpers.GetHashCode(t) })
            .ToList();

        return persistedTypes
            .Select(t => new { Key = $"db:{t.Id}", t.Type })
            .Concat(trackedTypes.Select(t => new
            {
                Key = t.Id > 0 ? $"db:{t.Id}" : $"mem:{t.InstanceId}",
                t.Type
            }))
            .GroupBy(t => t.Key)
            .Select(g => g.First().Type)
            .ToList();
    }
}
