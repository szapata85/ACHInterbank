using Cfa.ACHInterbank.Persistence;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class PostgresDataProtectionStartupTests
{
    [FinancialIntegrityFact(FinancialPersistenceMigrationTests.PersistenceProvider.PostgreSql)]
    [Trait("Category", "FinancialIntegrity")]
    public async Task EmptySchema_WithMigrationsEnabled_ShouldInitializeKeyRingBeforeHostStarts()
    {
        const string connectionVariable = "FINANCIAL_INTEGRITY_POSTGRES_CONNECTION_STRING";
        var baseConnectionString = Environment.GetEnvironmentVariable(connectionVariable)!;
        var schemaName = $"data_protection_startup_{Guid.NewGuid():N}";
        var schemaConnectionString = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            SearchPath = schemaName
        }.ConnectionString;

        await CreateSchemaAsync(baseConnectionString, schemaName);
        try
        {
            (await TableExistsAsync(schemaConnectionString)).Should().BeFalse();

            using var host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Database:Provider"] = "Postgres",
                        ["Database:ApplyMigrations"] = "true",
                        ["Database:ApplySeed"] = "false",
                        ["ConnectionStrings:PostgresConnection"] = schemaConnectionString,
                        ["Quartz:JobStore:Mode"] = "RAM",
                        ["DigitalEnvelope:CertificateBootstrap:Enabled"] = "false"
                    }))
                .ConfigureServices((context, services) => services.AddPersistence(context.Configuration))
                .Build();

            await host.StartAsync();

            await using var scope = host.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AchDbContext>();
            (await dbContext.DataProtectionKeys.AsNoTracking().CountAsync()).Should().BeGreaterThan(0);

            await host.StopAsync();
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await DropSchemaAsync(baseConnectionString, schemaName);
        }
    }

    private static async Task<bool> TableExistsAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT to_regclass('\"DataProtectionKeys\"') IS NOT NULL";
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private static async Task CreateSchemaAsync(string connectionString, string schemaName)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE SCHEMA \"{schemaName}\"";
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DropSchemaAsync(string connectionString, string schemaName)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP SCHEMA IF EXISTS \"{schemaName}\" CASCADE";
        await command.ExecuteNonQueryAsync();
    }
}
