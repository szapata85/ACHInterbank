using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

[Scoped]
public class SqlServerExternalFileNameSequenceService : IExternalFileNameSequenceProvider
{
    private readonly AchDbContext _context;

    public SqlServerExternalFileNameSequenceService(AchDbContext context)
    {
        _context = context;
    }

    public bool CanHandle(string? providerName)
        => providerName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true;

    public async Task<int> ReserveNextSequenceAsync(ExternalFileNameContext context, CancellationToken ct = default)
    {
        int next;
        try
        {
            next = await ExecuteReservationAsync(context, ct);
        }
        catch (SqlException ex) when (ex.Number == 51036)
        {
            throw new InvalidOperationException(
                ExternalFileNameSupport.BuildDailySequenceExhaustedMessage(context),
                ex);
        }

        if (next > ExternalFileNameSupport.ResolveDailySequenceMaximum(context))
        {
            throw new InvalidOperationException(ExternalFileNameSupport.BuildDailySequenceExhaustedMessage(context));
        }

        return next;
    }

    protected virtual async Task<int> ExecuteReservationAsync(ExternalFileNameContext context, CancellationToken ct)
    {
        var connection = _context.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
        {
            await connection.OpenAsync(ct);
        }

        var ambientTransaction = _context.Database.CurrentTransaction?.GetDbTransaction();
        DbTransaction? ownedTransaction = null;
        try
        {
            ownedTransaction = ambientTransaction is null
                ? await connection.BeginTransactionAsync(IsolationLevel.Serializable, ct)
                : null;

            await using var command = connection.CreateCommand();
            command.Transaction = ambientTransaction ?? ownedTransaction;
            command.CommandText = """
                DECLARE @next int;

                UPDATE dbo.ExternalFileSequences WITH (UPDLOCK, HOLDLOCK)
                SET LastValue = LastValue + 1,
                    UpdatedAtUtc = @updatedAtUtc,
                    RowVersion = CONVERT(varbinary(8), LastValue + 1),
                    @next = LastValue + 1
                WHERE ClearingHouseId = @clearingHouseId
                  AND ScopeCode = @scopeCode
                  AND SequenceDate = @sequenceDate
                  AND LastValue < @maxValue;

                IF @@ROWCOUNT = 0
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM dbo.ExternalFileSequences WITH (UPDLOCK, HOLDLOCK)
                        WHERE ClearingHouseId = @clearingHouseId
                          AND ScopeCode = @scopeCode
                          AND SequenceDate = @sequenceDate)
                    BEGIN
                        THROW 51036, 'ACH_EXTERNAL_SEQUENCE_LIMIT', 1;
                    END;

                    INSERT INTO dbo.ExternalFileSequences
                        (ClearingHouseId, ScopeCode, SequenceDate, LastValue, UpdatedAtUtc, RowVersion)
                    VALUES
                        (@clearingHouseId, @scopeCode, @sequenceDate, 1, @updatedAtUtc, 0x01);
                    SET @next = 1;
                END;

                SELECT @next;
                """;
            AddParameter(command, "@clearingHouseId", context.ClearingHouseId);
            AddParameter(command, "@scopeCode", ExternalFileNameSupport.GetSequenceScopeCode(context));
            AddParameter(command, "@sequenceDate",
                context.OperationalTimeSnapshot?.OperationalDate.ToDateTime(TimeOnly.MinValue)
                ?? context.ProcessingDate.Date);
            AddParameter(command, "@maxValue", ExternalFileNameSupport.ResolveDailySequenceMaximum(context));
            AddParameter(command, "@updatedAtUtc",
                context.OperationalTimeSnapshot?.CapturedAtUtc ?? DateTime.UtcNow);

            var scalar = await command.ExecuteScalarAsync(ct);
            if (scalar is null || scalar is DBNull)
            {
                throw new InvalidOperationException("SQL Server no devolvió el consecutivo reservado para el nombre externo.");
            }

            if (ownedTransaction is not null)
            {
                await ownedTransaction.CommitAsync(ct);
            }

            return Convert.ToInt32(scalar);
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

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
