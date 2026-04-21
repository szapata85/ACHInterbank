using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

[Scoped]
public class PostgresExternalFileNameSequenceService : IExternalFileNameSequenceProvider
{
    private const string ScopeCode = "ACH_EXTERNAL_NAME";
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

        if (ExternalFileNameSupport.IsAch(context) && next > 36)
        {
            throw new InvalidOperationException("Regla ACH HARD BLOCK: máximo 36 archivos diarios por participante.");
        }

        return next;
    }

    protected virtual async Task<int> ExecuteUpsertAsync(ExternalFileNameContext context, CancellationToken ct)
    {
        var connection = (NpgsqlConnection)_context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO "ExternalFileSequences"
                ("ClearingHouseId","ScopeCode","SequenceDate","LastValue","UpdatedAtUtc","RowVersion")
            VALUES
                (@clearingHouseId,@scopeCode,@sequenceDate,1,timezone('utc', now()),decode('01','hex'))
            ON CONFLICT ("ClearingHouseId","ScopeCode","SequenceDate")
            DO UPDATE SET
                "LastValue" = "ExternalFileSequences"."LastValue" + 1,
                "UpdatedAtUtc" = timezone('utc', now()),
                "RowVersion" = decode('01','hex')
            RETURNING "LastValue";
            """;

        cmd.Parameters.AddWithValue("@clearingHouseId", context.ClearingHouseId);
        cmd.Parameters.AddWithValue("@scopeCode", ScopeCode);
        cmd.Parameters.AddWithValue("@sequenceDate", DateOnly.FromDateTime(context.ProcessingDate.Date));

        var scalar = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(scalar);
    }
}
