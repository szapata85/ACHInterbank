using Cfa.ACHInterbank.Persistence;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class SqlServerDataProtectionStartupTests
{
    [FinancialIntegrityFact(FinancialPersistenceMigrationTests.PersistenceProvider.SqlServer)]
    [Trait("Category", "FinancialIntegrity")]
    public async Task EmptyDatabase_WithMigrationsEnabled_ShouldInitializeKeyRingBeforeHostStarts()
    {
        const string connectionVariable = "FINANCIAL_INTEGRITY_SQLSERVER_CONNECTION_STRING";
        var baseConnectionString = Environment.GetEnvironmentVariable(connectionVariable)!;
        var databaseName = $"achinterbank_data_protection_{Guid.NewGuid():N}";
        var adminBuilder = new SqlConnectionStringBuilder(baseConnectionString) { InitialCatalog = "master" };
        var databaseBuilder = new SqlConnectionStringBuilder(baseConnectionString) { InitialCatalog = databaseName };

        await CreateDatabaseAsync(adminBuilder.ConnectionString, databaseName);
        try
        {
            using var host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Database:Provider"] = "SqlServer",
                        ["Database:ApplyMigrations"] = "true",
                        ["Database:ApplySeed"] = "false",
                        ["ConnectionStrings:SqlConnection"] = databaseBuilder.ConnectionString,
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
            SqlConnection.ClearAllPools();
            await DropDatabaseAsync(adminBuilder.ConnectionString, databaseName);
        }
    }

    private static async Task CreateDatabaseAsync(string connectionString, string databaseName)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE [{databaseName}]";
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DropDatabaseAsync(string connectionString, string databaseName)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]";
        await command.ExecuteNonQueryAsync();
    }
}
