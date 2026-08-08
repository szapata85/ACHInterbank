using Microsoft.Data.SqlClient;
using Npgsql;

namespace Cfa.ACHInterbank.Persistence.DataBase;

public static class RelationalDatabaseExceptionClassifier
{
    public static bool IsUniqueViolation(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException { Number: 2601 or 2627 })
            {
                return true;
            }

            if (current is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return true;
            }

            var sqliteCode = current.GetType().GetProperty("SqliteErrorCode")?.GetValue(current);
            if (sqliteCode is 19)
            {
                return true;
            }
        }

        return false;
    }
}
