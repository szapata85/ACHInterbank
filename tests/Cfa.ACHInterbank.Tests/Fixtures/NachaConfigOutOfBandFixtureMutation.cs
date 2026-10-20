using Cfa.ACHInterbank.Persistence.DataBase;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Cfa.ACHInterbank.Tests;

// Deliberate corruption of a disposable SQLite fixture for snapshot isolation and fail-closed tests.
// Product writes still go through AchDbContext.SaveChanges and its publication guard.
internal static class NachaConfigOutOfBandFixtureMutation
{
    public static async Task ApplyAsync(AchDbContext context)
    {
        if (!context.Database.IsSqlite())
            throw new InvalidOperationException("OUT_OF_BAND_FIXTURE_REQUIRES_SQLITE");

        context.ChangeTracker.DetectChanges();
        var entries = context.ChangeTracker.Entries()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();
        foreach (var entry in entries)
        {
            var table = entry.Metadata.GetTableName()
                ?? throw new InvalidOperationException("OUT_OF_BAND_FIXTURE_TABLE_MISSING");
            if (!(table.StartsWith("Cfg", StringComparison.Ordinal)
                  || table.StartsWith("HistConfig", StringComparison.Ordinal)))
                throw new InvalidOperationException("OUT_OF_BAND_FIXTURE_CONFIG_ONLY");

            var store = StoreObjectIdentifier.Table(table, entry.Metadata.GetSchema());
            var key = entry.Metadata.FindPrimaryKey()?.Properties.Single()
                ?? throw new InvalidOperationException("OUT_OF_BAND_FIXTURE_KEY_MISSING");
            var keyColumn = Quote(key.GetColumnName(store)!);
            var keyValue = entry.Property(key.Name).CurrentValue
                ?? throw new InvalidOperationException("OUT_OF_BAND_FIXTURE_KEY_VALUE_MISSING");
            var tableSql = Quote(table);

            if (entry.State == EntityState.Deleted)
            {
                await context.Database.ExecuteSqlAsync(FormattableStringFactory.Create(
                    $"DELETE FROM {tableSql} WHERE {keyColumn} = {{0}}", keyValue));
                continue;
            }

            var properties = entry.Properties
                .Where(property => !property.Metadata.IsPrimaryKey())
                .Where(property => entry.State == EntityState.Added || property.IsModified)
                .Where(property => property.Metadata.GetColumnName(store) is not null)
                .ToList();
            if (properties.Count == 0)
                continue;

            var columns = properties.Select(property => Quote(property.Metadata.GetColumnName(store)!)).ToArray();
            var values = properties.Select(property => property.CurrentValue).ToArray();
            if (entry.State == EntityState.Added)
            {
                var parameters = Enumerable.Range(0, values.Length).Select(index => $"{{{index}}}");
                await context.Database.ExecuteSqlAsync(FormattableStringFactory.Create(
                    $"INSERT INTO {tableSql} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", parameters)})",
                    values));
            }
            else
            {
                var assignments = columns.Select((column, index) => $"{column} = {{{index}}}");
                await context.Database.ExecuteSqlAsync(FormattableStringFactory.Create(
                    $"UPDATE {tableSql} SET {string.Join(", ", assignments)} WHERE {keyColumn} = {{{values.Length}}}",
                    [.. values, keyValue]));
            }
        }
        context.ChangeTracker.Clear();
    }

    private static string Quote(string identifier)
        => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}
