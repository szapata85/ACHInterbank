using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.ACH.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

[Scoped]
public class ExternalFileNameSequenceService : IExternalFileNameSequenceService
{
    private readonly AchDbContext _context;

    public ExternalFileNameSequenceService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<int> ReserveNextSequenceAsync(ExternalFileNameContext context, CancellationToken ct = default)
    {
        if (_context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
        {
            return await ReserveWithPostgresUpsertAsync(context, ct);
        }

        var date = DateOnly.FromDateTime(context.ProcessingDate.Date);
        const string scopeCode = "ACH_EXTERNAL_NAME";

        var row = await _context.ExternalFileSequences
            .SingleOrDefaultAsync(x => x.ClearingHouseId == context.ClearingHouseId
                                    && x.ScopeCode == scopeCode
                                    && x.SequenceDate == date, ct);

        if (row is null)
        {
            row = new ExternalFileSequence
            {
                ClearingHouseId = context.ClearingHouseId,
                ScopeCode = scopeCode,
                SequenceDate = date,
                LastValue = 0,
                UpdatedAtUtc = DateTime.UtcNow,
                RowVersion = [1]
            };
            _context.ExternalFileSequences.Add(row);
        }

        row.LastValue += 1;
        row.UpdatedAtUtc = DateTime.UtcNow;
        row.RowVersion = [(byte)(row.LastValue % 255 == 0 ? 1 : row.LastValue % 255)];

        if (ExternalFileNameSupport.IsAch(context) && row.LastValue > 36)
        {
            throw new InvalidOperationException("Regla ACH HARD BLOCK: máximo 36 archivos diarios por participante.");
        }

        await _context.SaveChangesAsync(ct);
        return row.LastValue;
    }

    private async Task<int> ReserveWithPostgresUpsertAsync(ExternalFileNameContext context, CancellationToken ct)
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
        cmd.Parameters.AddWithValue("@scopeCode", "ACH_EXTERNAL_NAME");
        cmd.Parameters.AddWithValue("@sequenceDate", DateOnly.FromDateTime(context.ProcessingDate.Date));

        var scalar = await cmd.ExecuteScalarAsync(ct);
        var next = Convert.ToInt32(scalar);

        if (ExternalFileNameSupport.IsAch(context) && next > 36)
        {
            throw new InvalidOperationException("Regla ACH HARD BLOCK: máximo 36 archivos diarios por participante.");
        }

        return next;
    }
}
