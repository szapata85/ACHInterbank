using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

[Scoped]
public class PostgresExternalFileNameSequenceService : IExternalFileNameSequenceProvider
{
    private readonly AchDbContext _context;

    public PostgresExternalFileNameSequenceService(AchDbContext context)
    {
        _context = context;
    }

    public bool CanHandle(string? providerName)
        => providerName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true
           || providerName?.Contains("Postgre", StringComparison.OrdinalIgnoreCase) == true;

    public async Task<int> ReserveNextSequenceAsync(ExternalFileNameContext context, CancellationToken ct = default)
    {
        var next = await ExecuteUpsertAsync(context, ct);

        if ((ExternalFileNameSupport.IsAchColombiaNachaOut(context) || ExternalFileNameSupport.IsReturnOut(context)) && next > 36)
        {
            throw new InvalidOperationException("Regla ACH HARD BLOCK: máximo 36 archivos diarios por participante.");
        }

        return next;
    }

    protected virtual async Task<int> ExecuteUpsertAsync(ExternalFileNameContext context, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)_context.Database.GetDbConnection();
        var openedHere = connection.State != System.Data.ConnectionState.Open;
        if (openedHere)
        {
            await connection.OpenAsync(ct);
        }

        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction() as NpgsqlTransaction;
            cmd.CommandText = """
                INSERT INTO "ExternalFileSequences"
                    ("ClearingHouseId","ScopeCode","SequenceDate","LastValue","UpdatedAtUtc","RowVersion")
                VALUES
                    (@clearingHouseId,@scopeCode,@sequenceDate,1,@updatedAtUtc,decode('01','hex'))
                ON CONFLICT ("ClearingHouseId","ScopeCode","SequenceDate")
                DO UPDATE SET
                    "LastValue" = "ExternalFileSequences"."LastValue" + 1,
                    "UpdatedAtUtc" = @updatedAtUtc,
                    "RowVersion" = decode('01','hex')
                WHERE "ExternalFileSequences"."LastValue" < @maxValue
                RETURNING "LastValue";
                """;

            cmd.Parameters.AddWithValue("@clearingHouseId", context.ClearingHouseId);
            cmd.Parameters.AddWithValue("@scopeCode", ExternalFileNameSupport.GetSequenceScopeCode(context));
            cmd.Parameters.AddWithValue("@sequenceDate",
                context.OperationalTimeSnapshot?.OperationalDate
                ?? DateOnly.FromDateTime(context.ProcessingDate.Date));
            cmd.Parameters.AddWithValue("@maxValue",
                ExternalFileNameSupport.IsAchColombiaNachaOut(context) || ExternalFileNameSupport.IsReturnOut(context)
                    ? 36
                    : int.MaxValue);
            cmd.Parameters.AddWithValue("@updatedAtUtc",
                context.OperationalTimeSnapshot?.CapturedAtUtc ?? DateTime.UtcNow);

            var scalar = await cmd.ExecuteScalarAsync(ct);
            if (scalar is null || scalar is DBNull)
            {
                throw new InvalidOperationException("Regla ACH HARD BLOCK: máximo 36 archivos diarios por participante.");
            }

            return Convert.ToInt32(scalar);
        }
        finally
        {
            if (openedHere)
            {
                await connection.CloseAsync();
            }
        }
    }
}
