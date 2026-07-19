using System.Data;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public enum FinancialIntegrityMissingConnectionOutcome
{
    Configured,
    LocalSkip,
    RequiredFailure
}

public static class FinancialIntegrityTestConfiguration
{
    public const string RequireDatabasesVariable = "FINANCIAL_INTEGRITY_REQUIRE_DATABASES";

    public static bool IsRequired(string? value)
        => value is not null
            && (value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("1", StringComparison.OrdinalIgnoreCase)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase));

    public static string VariableName(FinancialPersistenceMigrationTests.PersistenceProvider provider)
        => provider == FinancialPersistenceMigrationTests.PersistenceProvider.SqlServer
            ? "FINANCIAL_INTEGRITY_SQLSERVER_CONNECTION_STRING"
            : "FINANCIAL_INTEGRITY_POSTGRES_CONNECTION_STRING";

    public static FinancialIntegrityMissingConnectionOutcome Evaluate(string? connectionString, bool required)
        => string.IsNullOrWhiteSpace(connectionString)
            ? required ? FinancialIntegrityMissingConnectionOutcome.RequiredFailure : FinancialIntegrityMissingConnectionOutcome.LocalSkip
            : FinancialIntegrityMissingConnectionOutcome.Configured;

    public static string MissingConnectionMessage(FinancialPersistenceMigrationTests.PersistenceProvider provider)
        => $"FinancialIntegrity requires a real {provider} database connection. Set {VariableName(provider)}; "
            + $"{RequireDatabasesVariable}=true makes this missing connection a CI failure.";

    public static void EnsureConnectionIsAvailable(FinancialPersistenceMigrationTests.PersistenceProvider provider)
    {
        var variableName = VariableName(provider);
        var connectionString = Environment.GetEnvironmentVariable(variableName);
        if (Evaluate(connectionString, IsRequired(Environment.GetEnvironmentVariable(RequireDatabasesVariable)))
            == FinancialIntegrityMissingConnectionOutcome.RequiredFailure)
        {
            throw new InvalidOperationException(MissingConnectionMessage(provider));
        }
    }
}

public sealed class FinancialIntegrityFactAttribute : FactAttribute
{
    public FinancialIntegrityFactAttribute(FinancialPersistenceMigrationTests.PersistenceProvider provider)
    {
        if (!FinancialIntegrityTestConfiguration.IsRequired(Environment.GetEnvironmentVariable(FinancialIntegrityTestConfiguration.RequireDatabasesVariable))
            && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(FinancialIntegrityTestConfiguration.VariableName(provider))))
        {
            Skip = $"Local FinancialIntegrity run omitted: set {FinancialIntegrityTestConfiguration.VariableName(provider)} to execute {provider}.";
        }
    }
}

public class FinancialPersistenceMigrationTests
{
    [FinancialIntegrityFact(PersistenceProvider.SqlServer)]
    [Trait("Category", "FinancialIntegrity")]
    public Task EnforceFinancialIntegrityMigration_ShouldPreserveExistingDataAndRollback_SqlServer()
        => EnforceFinancialIntegrityMigration_ShouldPreserveExistingDataAndRollback(PersistenceProvider.SqlServer);

    [FinancialIntegrityFact(PersistenceProvider.PostgreSql)]
    [Trait("Category", "FinancialIntegrity")]
    public Task EnforceFinancialIntegrityMigration_ShouldPreserveExistingDataAndRollback_PostgreSql()
        => EnforceFinancialIntegrityMigration_ShouldPreserveExistingDataAndRollback(PersistenceProvider.PostgreSql);

    private static async Task EnforceFinancialIntegrityMigration_ShouldPreserveExistingDataAndRollback(PersistenceProvider provider)
    {
        await using var fixture = await MigrationFixture.CreateAsync(provider);
        FinancialIntegrityTestConfiguration.EnsureConnectionIsAvailable(provider);

        await using var context = fixture.CreateContext();
        var migrator = context.Database.GetService<IMigrator>();
        var enforcementMigrationId = context.Database.GetMigrations().Single(id => id.EndsWith("_EnforceFinancialIntegrity", StringComparison.Ordinal));
        var migrations = context.Database.GetMigrations().ToList();
        var previousMigrationId = migrations[migrations.IndexOf(enforcementMigrationId) - 1];
        var targetMigrationId = migrations.Last();

        await migrator.MigrateAsync(previousMigrationId);
        FinancialIntegrityEvidence.Record(provider, "previous-migration");
        var expected = await SeedExistingDataAsync(context);

        await migrator.MigrateAsync(targetMigrationId);
        FinancialIntegrityEvidence.Record(provider, "up");
        context.ChangeTracker.Clear();

        await AssertSnapshotAsync(context, expected);
        await AssertUpSchemaAsync(context, provider);
        FinancialIntegrityEvidence.Record(provider, "invariance");

        await migrator.MigrateAsync(previousMigrationId);
        FinancialIntegrityEvidence.Record(provider, "rollback");
        context.ChangeTracker.Clear();

        await AssertSnapshotAsync(context, expected);
        await AssertRollbackSchemaAsync(context, provider);
    }

    [FinancialIntegrityFact(PersistenceProvider.SqlServer)]
    [Trait("Category", "FinancialIntegrity")]
    public Task EnforceFinancialIntegrityMigration_ShouldRejectExistingAmountsOutsideTheDefinedScale_SqlServer()
        => EnforceFinancialIntegrityMigration_ShouldRejectExistingAmountsOutsideTheDefinedScale(PersistenceProvider.SqlServer);

    [FinancialIntegrityFact(PersistenceProvider.PostgreSql)]
    [Trait("Category", "FinancialIntegrity")]
    public Task EnforceFinancialIntegrityMigration_ShouldRejectExistingAmountsOutsideTheDefinedScale_PostgreSql()
        => EnforceFinancialIntegrityMigration_ShouldRejectExistingAmountsOutsideTheDefinedScale(PersistenceProvider.PostgreSql);

    private static async Task EnforceFinancialIntegrityMigration_ShouldRejectExistingAmountsOutsideTheDefinedScale(PersistenceProvider provider)
    {
        await using var fixture = await MigrationFixture.CreateAsync(provider);
        FinancialIntegrityTestConfiguration.EnsureConnectionIsAvailable(provider);

        await using var context = fixture.CreateContext();
        var migrator = context.Database.GetService<IMigrator>();
        var enforcementMigrationId = context.Database.GetMigrations().Single(id => id.EndsWith("_EnforceFinancialIntegrity", StringComparison.Ordinal));
        var migrations = context.Database.GetMigrations().ToList();
        var previousMigrationId = migrations[migrations.IndexOf(enforcementMigrationId) - 1];
        var targetMigrationId = migrations.Last();

        await migrator.MigrateAsync(previousMigrationId);
        if (provider == PersistenceProvider.SqlServer)
        {
            context.EntryDetails.Add(new EntryDetail { Amount = 1.001m, BatchNumber = 1 });
            await context.SaveChangesAsync();
            await context.Database.ExecuteSqlRawAsync("UPDATE [EntryDetails] SET [Amount] = CAST(1.001 AS money)");
        }
        else
        {
            context.AchBatches.Add(new AchBatch { TotalDebitAmount = 1.001m, TotalCreditAmount = 0m });
            await context.SaveChangesAsync();
            await context.Database.ExecuteSqlRawAsync("UPDATE \"AchBatches\" SET \"TotalDebitAmount\" = 1.001");
        }

        await Assert.ThrowsAnyAsync<Exception>(() => migrator.MigrateAsync(targetMigrationId));
        FinancialIntegrityEvidence.Record(provider, "out-of-scale-rejected");
        Assert.DoesNotContain(enforcementMigrationId, await context.Database.GetAppliedMigrationsAsync());
    }

    private static async Task<FinancialSnapshot> SeedExistingDataAsync(AchDbContext context)
    {
        var clearingHouseConfig = new ClearingHouseConfig
        {
            ClearingHouseId = 9001,
            HolidayStrategy = "Test"
        };
        context.ClearingHouseConfigs.Add(clearingHouseConfig);
        await context.SaveChangesAsync();

        var clearingHouse = new ClearingHouse
        {
            Name = "Clearing House Test",
            Code = "TEST",
            OriginCode = "000101006",
            ClearingHouseId = clearingHouseConfig.Id
        };
        var sourceInstitution = CreateInstitution("Source Test", "1234567", "9");
        var destinationInstitution = CreateInstitution("Destination Test", "8765432", "3");
        context.AddRange(clearingHouse, sourceInstitution, destinationInstitution);
        await context.SaveChangesAsync();

        var cycle = new AchCycle
        {
            Id = "FIN-INT",
            CycleName = "Financial integrity",
            ProcessingDate = new DateTime(2026, 7, 18),
            CutoffTime = new TimeSpan(12, 0, 0),
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(16, 0, 0),
            ClearingHouseId = clearingHouse.Id
        };
        var batch = new AchBatch
        {
            AchCycleId = cycle.Id,
            CompanyName = "TEST",
            CompanyIdentification = "900000001",
            CompanyEntryDescription = "PAGOS",
            CompanyEntryDescriptionId = 1,
            OriginOrOdfi = "12345678",
            EffectiveEntryDate = cycle.ProcessingDate,
            BatchSequenceNumber = 1,
            TotalDebitAmount = 0m,
            TotalCreditAmount = 9_999_999_999_999_999.99m
        };
        context.AddRange(cycle, batch);
        await context.SaveChangesAsync();

        var transaction = new AchTransaction
        {
            Amount = 9_999_999_999_999_999.99m,
            TransactionExternalId = "financial-integrity-transaction",
            Reference = "FIN-INT",
            Type = TransactionTypeEnum.Credit,
            TransactionCode = "22",
            ServiceClassCode = "220",
            CompanyEntryDescriptionId = 1,
            CompanyName = "TEST",
            CompanyIdentification = "900000001",
            OriginatingDFI = "12345678",
            ReceivingDFI = "87654321",
            TraceNumber = "123456780000001",
            TraceSequenceNumber = 1,
            EffectiveEntryDate = cycle.ProcessingDate,
            State = AchTransferStateEnum.Pending,
            StateChangedAtUtc = DateTime.UtcNow,
            ContrapartidasResponseCode = string.Empty,
            ReturnReasonCode = string.Empty,
            OriginalTraceRef = string.Empty,
            RecipientIdNumber = string.Empty,
            DiscretionaryData = string.Empty,
            SourceAccountNumber = "1",
            DestinationAccountNumber = "2",
            SourceInstitutionId = sourceInstitution.Id,
            DestinationInstitutionId = destinationInstitution.Id,
            AchCycleId = cycle.Id,
            AchBatchId = batch.Id
        };
        var entry = new EntryDetail
        {
            Amount = 922_337_203_685_477.58m,
            TransactionCode = "22",
            ReceivingParticipantEntityCode = "12345678",
            BatchNumber = 1
        };
        var batchControl = new BatchControl
        {
            TotalDebitAmount = 0m,
            TotalCreditAmount = 922_337_203_685_477.58m,
            BatchNumber = "1"
        };
        var fileControl = new FileControl
        {
            BatchCount = 1,
            BlockCount = 1,
            EntryAddendaCount = 1,
            EntryHash = 1,
            TotalDebitAmount = 0m,
            TotalCreditAmount = 922_337_203_685_477.58m
        };
        var ingestion = new IncomingNachaFileIngestion
        {
            FileName = "financial-integrity.out",
            FileHashSha256 = new string('a', 64),
            FileSize = 1,
            ContentType = "text/plain",
            UploadedAtUtc = DateTime.UtcNow,
            UploadedBy = "test",
            CorrelationId = "financial-integrity",
            Notes = string.Empty
        };
        context.AddRange(transaction, entry, batchControl, fileControl, ingestion);
        await context.SaveChangesAsync();

        var events = Enumerable.Range(1, 3)
            .Select(index => new IncomingNachaProcessingEvent
            {
                IncomingNachaFileIngestionId = ingestion.Id,
                EventType = "Integrity",
                EventStatus = "Persisted",
                Message = $"Event {index}",
                EvidenceJson = "{}",
                OccurredAtUtc = DateTime.UtcNow.AddMinutes(index),
                RaisedBy = "test"
            })
            .ToList();
        context.IncomingNachaProcessingEvents.AddRange(events);
        await context.SaveChangesAsync();

        return new FinancialSnapshot(
            ingestion.Id,
            events.Select(@event => @event.Id).Order().ToArray(),
            new Dictionary<string, decimal>
            {
                ["AchBatch.TotalDebitAmount"] = batch.TotalDebitAmount,
                ["AchBatch.TotalCreditAmount"] = batch.TotalCreditAmount,
                ["AchTransaction.Amount"] = transaction.Amount,
                ["EntryDetail.Amount"] = entry.Amount!.Value,
                ["BatchControl.TotalDebitAmount"] = batchControl.TotalDebitAmount,
                ["BatchControl.TotalCreditAmount"] = batchControl.TotalCreditAmount,
                ["FileControl.TotalDebitAmount"] = fileControl.TotalDebitAmount,
                ["FileControl.TotalCreditAmount"] = fileControl.TotalCreditAmount
            });
    }

    private static async Task AssertSnapshotAsync(AchDbContext context, FinancialSnapshot expected)
    {
        var eventIds = await context.IncomingNachaProcessingEvents
            .Where(@event => @event.IncomingNachaFileIngestionId == expected.IngestionId)
            .Select(@event => @event.Id)
            .Order()
            .ToArrayAsync();

        Assert.True(expected.EventIds.ToHashSet().SetEquals(eventIds));

        var batch = await context.AchBatches.SingleAsync();
        var transaction = await context.AchTransactions.SingleAsync();
        var entry = await context.EntryDetails.SingleAsync();
        var batchControl = await context.BatchControls.SingleAsync();
        var fileControl = await context.FileControls.SingleAsync();
        var actual = new Dictionary<string, decimal>
        {
            ["AchBatch.TotalDebitAmount"] = batch.TotalDebitAmount,
            ["AchBatch.TotalCreditAmount"] = batch.TotalCreditAmount,
            ["AchTransaction.Amount"] = transaction.Amount,
            ["EntryDetail.Amount"] = entry.Amount!.Value,
            ["BatchControl.TotalDebitAmount"] = batchControl.TotalDebitAmount,
            ["BatchControl.TotalCreditAmount"] = batchControl.TotalCreditAmount,
            ["FileControl.TotalDebitAmount"] = fileControl.TotalDebitAmount,
            ["FileControl.TotalCreditAmount"] = fileControl.TotalCreditAmount
        };

        Assert.Equal(expected.Amounts.OrderBy(pair => pair.Key), actual.OrderBy(pair => pair.Key));
        Assert.Equal(expected.Amounts.Values.Sum(), actual.Values.Sum());
    }

    private static async Task AssertUpSchemaAsync(AchDbContext context, PersistenceProvider provider)
    {
        Assert.False(await ColumnExistsAsync(context, provider, "IncomingNachaProcessingEvents", "IncomingNachaFileIngestionId1"));
        var expectedType = provider == PersistenceProvider.SqlServer ? "decimal(18,2)" : "numeric(18,2)";
        foreach (var (table, column) in MonetaryColumns)
        {
            Assert.Equal(expectedType, await GetColumnTypeAsync(context, provider, table, column));
        }
    }

    private static async Task AssertRollbackSchemaAsync(AchDbContext context, PersistenceProvider provider)
    {
        Assert.True(await ColumnExistsAsync(context, provider, "IncomingNachaProcessingEvents", "IncomingNachaFileIngestionId1"));
        foreach (var (table, column) in LegacyMoneyColumns)
        {
            Assert.Equal("money", await GetColumnTypeAsync(context, provider, table, column));
        }
    }

    private static readonly (string Table, string Column)[] MonetaryColumns =
    [
        ("AchBatches", "TotalDebitAmount"),
        ("AchBatches", "TotalCreditAmount"),
        ("AchTransactions", "Amount"),
        ("EntryDetails", "Amount"),
        ("BatchControls", "TotalDebitAmount"),
        ("BatchControls", "TotalCreditAmount"),
        ("FileControls", "TotalDebitAmount"),
        ("FileControls", "TotalCreditAmount")
    ];

    private static readonly (string Table, string Column)[] LegacyMoneyColumns =
    [
        ("EntryDetails", "Amount"),
        ("BatchControls", "TotalDebitAmount"),
        ("BatchControls", "TotalCreditAmount"),
        ("FileControls", "TotalDebitAmount"),
        ("FileControls", "TotalCreditAmount")
    ];

    private static async Task<bool> ColumnExistsAsync(AchDbContext context, PersistenceProvider provider, string table, string column)
        => await ExecuteScalarAsync<int>(context, provider,
            provider == PersistenceProvider.SqlServer
                ? "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @table AND COLUMN_NAME = @column"
                : "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = @table AND column_name = @column",
            table,
            column) > 0;

    private static async Task<string> GetColumnTypeAsync(AchDbContext context, PersistenceProvider provider, string table, string column)
    {
        var type = await ExecuteScalarAsync<string>(context, provider,
            provider == PersistenceProvider.SqlServer
                ? "SELECT CASE WHEN DATA_TYPE IN ('decimal', 'numeric') THEN DATA_TYPE + '(' + CAST(NUMERIC_PRECISION AS varchar(10)) + ',' + CAST(NUMERIC_SCALE AS varchar(10)) + ')' ELSE DATA_TYPE END FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @table AND COLUMN_NAME = @column"
                : "SELECT CASE WHEN data_type IN ('numeric', 'decimal') THEN data_type || '(' || numeric_precision || ',' || numeric_scale || ')' ELSE data_type END FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = @table AND column_name = @column",
            table,
            column);

        return type.ToLowerInvariant();
    }

    private static async Task<T> ExecuteScalarAsync<T>(AchDbContext context, PersistenceProvider provider, string sql, string table, string column)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var tableParameter = command.CreateParameter();
        tableParameter.ParameterName = "@table";
        tableParameter.Value = table;
        command.Parameters.Add(tableParameter);
        var columnParameter = command.CreateParameter();
        columnParameter.ParameterName = "@column";
        columnParameter.Value = column;
        command.Parameters.Add(columnParameter);

        return (T)Convert.ChangeType((await command.ExecuteScalarAsync())!, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static FinancialInstitution CreateInstitution(string name, string routingNumber, string transitCode)
    {
        var institution = new FinancialInstitution
        {
            Name = name,
            RoutingNumber = routingNumber,
            TransitCode = transitCode
        };
        institution.CalculateCheckDigit();
        return institution;
    }

    private sealed record FinancialSnapshot(Guid IngestionId, Guid[] EventIds, IReadOnlyDictionary<string, decimal> Amounts);

    public enum PersistenceProvider
    {
        SqlServer,
        PostgreSql
    }

    private sealed class MigrationFixture : IAsyncDisposable
    {
        private const string SqlServerMigrationsAssembly = "Cfa.ACHInterbank.Persistence.Migrations.SqlServer";
        private readonly string? _databaseName;
        private readonly string? _schemaName;
        private readonly string _connectionString;
        private readonly string _adminConnectionString;

        private MigrationFixture(PersistenceProvider provider, string connectionString, string adminConnectionString, string? databaseName, string? schemaName)
        {
            Provider = provider;
            _connectionString = connectionString;
            _adminConnectionString = adminConnectionString;
            _databaseName = databaseName;
            _schemaName = schemaName;
        }

        public PersistenceProvider Provider { get; }

        public static async Task<MigrationFixture> CreateAsync(PersistenceProvider provider)
        {
            var settingName = provider == PersistenceProvider.SqlServer
                ? "FINANCIAL_INTEGRITY_SQLSERVER_CONNECTION_STRING"
                : "FINANCIAL_INTEGRITY_POSTGRES_CONNECTION_STRING";
            var baseConnectionString = Environment.GetEnvironmentVariable(settingName);
            if (string.IsNullOrWhiteSpace(baseConnectionString))
            {
                throw new InvalidOperationException(FinancialIntegrityTestConfiguration.MissingConnectionMessage(provider));
            }

            if (provider == PersistenceProvider.SqlServer)
            {
                var builder = new SqlConnectionStringBuilder(baseConnectionString) { InitialCatalog = "master" };
                var databaseName = $"achinterbank_financial_integrity_{Guid.NewGuid():N}";
                await using var connection = new SqlConnection(builder.ConnectionString);
                await connection.OpenAsync();
                FinancialIntegrityEvidence.Record(provider, "connection-opened");
                await using var command = connection.CreateCommand();
                command.CommandText = $"CREATE DATABASE [{databaseName}]";
                await command.ExecuteNonQueryAsync();
                FinancialIntegrityEvidence.Record(provider, "isolated-database-created");
                builder.InitialCatalog = databaseName;
                return new MigrationFixture(provider, builder.ConnectionString, new SqlConnectionStringBuilder(baseConnectionString) { InitialCatalog = "master" }.ConnectionString, databaseName, null);
            }

            var postgresBuilder = new NpgsqlConnectionStringBuilder(baseConnectionString);
            var schemaName = $"financial_integrity_{Guid.NewGuid():N}";
            await using (var connection = new NpgsqlConnection(postgresBuilder.ConnectionString))
            {
                await connection.OpenAsync();
                FinancialIntegrityEvidence.Record(provider, "connection-opened");
                await using var command = connection.CreateCommand();
                command.CommandText = $"CREATE SCHEMA \"{schemaName}\"";
                await command.ExecuteNonQueryAsync();
                FinancialIntegrityEvidence.Record(provider, "isolated-schema-created");
            }

            postgresBuilder.SearchPath = schemaName;
            return new MigrationFixture(provider, postgresBuilder.ConnectionString, baseConnectionString, null, schemaName);
        }

        public AchDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AchDbContext>();
            if (Provider == PersistenceProvider.SqlServer)
            {
                options.UseSqlServer(_connectionString, sql => sql.MigrationsAssembly(SqlServerMigrationsAssembly));
            }
            else
            {
                options.UseNpgsql(_connectionString);
            }

            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            return new AchDbContext(options.Options);
        }

        public async ValueTask DisposeAsync()
        {
            if (Provider == PersistenceProvider.SqlServer)
            {
                await using var connection = new SqlConnection(_adminConnectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]";
                await command.ExecuteNonQueryAsync();
                FinancialIntegrityEvidence.Record(Provider, "cleanup");
                return;
            }

            await using var postgresConnection = new NpgsqlConnection(_adminConnectionString);
            await postgresConnection.OpenAsync();
            await using var postgresCommand = postgresConnection.CreateCommand();
            postgresCommand.CommandText = $"DROP SCHEMA IF EXISTS \"{_schemaName}\" CASCADE";
            await postgresCommand.ExecuteNonQueryAsync();
            FinancialIntegrityEvidence.Record(Provider, "cleanup");
        }
    }
}

[Trait("Category", "FinancialIntegrity")]
public sealed class FinancialIntegrityTestConfigurationTests
{
    [Fact]
    public void RequiredMode_WithMissingSqlServerConnection_FailsExplicitly()
        => Assert.Equal(
            FinancialIntegrityMissingConnectionOutcome.RequiredFailure,
            FinancialIntegrityTestConfiguration.Evaluate(null, required: true));

    [Fact]
    public void RequiredMode_WithMissingPostgreSqlConnection_FailsExplicitly()
        => Assert.Equal(
            FinancialIntegrityMissingConnectionOutcome.RequiredFailure,
            FinancialIntegrityTestConfiguration.Evaluate("", required: true));

    [Fact]
    public void LocalMode_WithMissingConnection_IsAnExplicitSkip()
        => Assert.Equal(
            FinancialIntegrityMissingConnectionOutcome.LocalSkip,
            FinancialIntegrityTestConfiguration.Evaluate(null, required: false));

    [Fact]
    public void ConfiguredConnection_RequiresRealExecution()
        => Assert.Equal(
            FinancialIntegrityMissingConnectionOutcome.Configured,
            FinancialIntegrityTestConfiguration.Evaluate("Host=localhost;Database=synthetic", required: true));
}

internal static class FinancialIntegrityEvidence
{
    private static readonly object Gate = new();

    public static void Record(FinancialPersistenceMigrationTests.PersistenceProvider provider, string stage)
    {
        var path = Environment.GetEnvironmentVariable("FINANCIAL_INTEGRITY_EVIDENCE_PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        lock (Gate)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.AppendAllText(path, $"provider={provider};stage={stage}{Environment.NewLine}");
        }
    }
}
