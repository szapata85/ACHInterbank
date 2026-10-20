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
        var targetMigrationId = enforcementMigrationId;

        await migrator.MigrateAsync(previousMigrationId);
        FinancialIntegrityEvidence.Record(provider, "previous-migration");
        Assert.False(await ColumnExistsAsync(context, provider, "AuditLog", "CorrelationId"));
        var expected = await SeedExistingDataAsync(context, provider);

        await migrator.MigrateAsync(targetMigrationId);
        FinancialIntegrityEvidence.Record(provider, "up");
        context.ChangeTracker.Clear();

        await AssertSnapshotAsync(context, provider, expected);
        await AssertUpSchemaAsync(context, provider);
        FinancialIntegrityEvidence.Record(provider, "invariance");

        await migrator.MigrateAsync(previousMigrationId);
        FinancialIntegrityEvidence.Record(provider, "rollback");
        context.ChangeTracker.Clear();

        await AssertSnapshotAsync(context, provider, expected);
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
        var targetMigrationId = enforcementMigrationId;

        await migrator.MigrateAsync(previousMigrationId);
        Assert.False(await ColumnExistsAsync(context, provider, "AuditLog", "CorrelationId"));
        var historical = new HistoricalFinancialIntegritySeed(context, provider);
        if (provider == PersistenceProvider.SqlServer)
        {
            await historical.InsertAsync("EntryDetails", new() { ["Amount"] = 1.001m, ["BatchNumber"] = 1 });
        }
        else
        {
            await historical.InsertAsync("AchBatches", new()
            {
                ["ServiceClassCode"] = "220",
                ["CompanyName"] = "TEST",
                ["CompanyIdentification"] = "900000001",
                ["CompanyEntryDescription"] = "PAGOS",
                ["CompanyEntryDescriptionId"] = 1,
                ["OriginOrOdfi"] = "12345678",
                ["EffectiveEntryDate"] = new DateTime(2026, 7, 18),
                ["BatchSequenceNumber"] = 1,
                ["TotalDebitAmount"] = 1.001m,
                ["TotalCreditAmount"] = 0m,
                ["CreatedAt"] = DateTimeOffset.UtcNow,
                ["UpdatedAt"] = DateTimeOffset.UtcNow
            });
        }

        await Assert.ThrowsAnyAsync<Exception>(() => migrator.MigrateAsync(targetMigrationId));
        FinancialIntegrityEvidence.Record(provider, "out-of-scale-rejected");
        Assert.DoesNotContain(enforcementMigrationId, await context.Database.GetAppliedMigrationsAsync());
        Assert.True(await ColumnExistsAsync(context, provider, "IncomingNachaProcessingEvents", "IncomingNachaFileIngestionId1"));
    }

    [FinancialIntegrityFact(PersistenceProvider.SqlServer)]
    [Trait("Category", "IncomingNachaTraceabilityMigration")]
    public Task IncomingNachaTraceabilityMigration_ShouldBackfillAndRollback_SqlServer()
        => IncomingNachaTraceabilityMigration_ShouldBackfillAndRollback(PersistenceProvider.SqlServer);

    [FinancialIntegrityFact(PersistenceProvider.PostgreSql)]
    [Trait("Category", "IncomingNachaTraceabilityMigration")]
    public Task IncomingNachaTraceabilityMigration_ShouldBackfillAndRollback_PostgreSql()
        => IncomingNachaTraceabilityMigration_ShouldBackfillAndRollback(PersistenceProvider.PostgreSql);

    private static async Task IncomingNachaTraceabilityMigration_ShouldBackfillAndRollback(PersistenceProvider provider)
    {
        await using var fixture = await MigrationFixture.CreateAsync(provider);
        FinancialIntegrityTestConfiguration.EnsureConnectionIsAvailable(provider);
        await using var context = fixture.CreateContext();
        var migrator = context.Database.GetService<IMigrator>();
        var migrations = context.Database.GetMigrations().ToList();
        var targetMigrationId = migrations.Single(id => id.EndsWith("_IncomingNachaTraceabilityCore", StringComparison.Ordinal));
        var previousMigrationId = migrations[migrations.IndexOf(targetMigrationId) - 1];

        await migrator.MigrateAsync(previousMigrationId);
        var historical = new HistoricalFinancialIntegritySeed(context, provider);
        var receivedAt = new DateTimeOffset(2026, 7, 31, 14, 0, 0, TimeSpan.Zero);
        var ingestionId = Guid.NewGuid();
        const string nachaId = "traceability-migration-fixture";
        const decimal amount = 922_337_203_685_477.58m;

        await historical.InsertAsync("IncomingNachaFileIngestions", new()
        {
            ["Id"] = ingestionId, ["FileName"] = "0001283.001.20260731.1.OUT",
            ["FileHashSha256"] = new string('b', 64), ["FileSize"] = 106L,
            ["ContentType"] = "text/plain", ["UploadedAtUtc"] = receivedAt.UtcDateTime,
            ["ReceivedAtUtc"] = receivedAt.UtcDateTime, ["UploadedBy"] = "migration-test",
            ["ReceivedBy"] = "migration-test", ["IngestionStatus"] = "Completado",
            ["CycleResolutionStatus"] = "ResueltoConfirmado", ["ParsingStatus"] = "Exitoso",
            ["ResolutionEvidenceJson"] = "{}", ["CorrelationId"] = "traceability-migration",
            ["IsReprocess"] = false, ["Notes"] = "fixture controlado", ["WarningsJson"] = "[]",
            ["CreatedAt"] = receivedAt, ["UpdatedAt"] = receivedAt
        });
        await historical.InsertAsync("NachaHeaders", new()
        {
            ["NachaID"] = nachaId, ["FileCreationDate"] = "260731", ["CycleNumber"] = 1,
            ["IncomingNachaFileIngestionId"] = ingestionId
        });
        var batchId = await historical.InsertIdentityAsync("BatchHeaders", new()
        {
            ["BatchNumber"] = 1, ["NachaID"] = nachaId, ["ServiceClassCode"] = "220"
        }, "BatchID");
        var entryId = await historical.InsertIdentityAsync("EntryDetails", new()
        {
            ["TransactionCode"] = "22", ["Amount"] = amount,
            ["SequenceNumber"] = "000000010000001", ["NachaID"] = nachaId, ["BatchNumber"] = 1
        }, "EntryDetailID");
        await historical.InsertAsync("AddendaRecords", new()
        {
            ["CodeTypeAddendumRecord"] = "05", ["AddendumSequence"] = "0001",
            ["EntryDetailSequenceNumber"] = "0000001", ["NachaID"] = nachaId
        });
        await historical.InsertAsync("BatchControls", new()
        {
            ["EntryAddendaCount"] = 1, ["EntryHash"] = 1L, ["TotalDebitAmount"] = 0m,
            ["TotalCreditAmount"] = amount, ["BatchNumber"] = "1", ["NachaID"] = nachaId
        });
        await historical.InsertAsync("FileControls", new()
        {
            ["BatchCount"] = 1, ["BlockCount"] = 1, ["EntryAddendaCount"] = 1,
            ["EntryHash"] = 1L, ["TotalDebitAmount"] = 0m, ["TotalCreditAmount"] = amount,
            ["NachaID"] = nachaId
        });

        await migrator.MigrateAsync(targetMigrationId);
        FinancialIntegrityEvidence.Record(provider, "traceability-up");

        Assert.Equal(batchId, await ReadScalarAsync<int>(context, provider,
            $"SELECT {Quote(provider, "BatchHeaderId")} FROM {Quote(provider, "EntryDetails")} WHERE {Quote(provider, "EntryDetailID")} = @value", entryId));
        Assert.Equal(entryId, await ReadScalarAsync<int>(context, provider,
            $"SELECT {Quote(provider, "EntryDetailId")} FROM {Quote(provider, "AddendaRecords")} WHERE {Quote(provider, "NachaID")} = @value", nachaId));
        Assert.Equal("Persisted", await ReadScalarAsync<string>(context, provider,
            $"SELECT {Quote(provider, "Stage")} FROM {Quote(provider, "IncomingNachaFileIngestions")} WHERE {Quote(provider, "Id")} = @value", ingestionId));
        Assert.Equal(".OUT", await ReadScalarAsync<string>(context, provider,
            $"SELECT {Quote(provider, "FileExtension")} FROM {Quote(provider, "IncomingNachaFileIngestions")} WHERE {Quote(provider, "Id")} = @value", ingestionId));
        Assert.Equal(receivedAt, await ReadScalarAsync<DateTimeOffset>(context, provider,
            $"SELECT {Quote(provider, "CreatedAt")} FROM {Quote(provider, "EntryDetails")} WHERE {Quote(provider, "EntryDetailID")} = @value", entryId));
        Assert.Equal(provider == PersistenceProvider.SqlServer ? "decimal(18,2)" : "numeric(18,2)",
            await GetColumnTypeAsync(context, provider, "EntryDetails", "Amount"));
        Assert.True(await IsColumnNullableAsync(context, provider, "IncomingNachaIntegrationExecution", "EntryDetailId"));
        var attemptIndexName = provider == PersistenceProvider.SqlServer
            ? "IX_IncomingNachaIntegrationExecution_EntryDetailId_AttemptNumber"
            : "IX_IncomingNachaIntegrationExecution_EntryDetailId_AttemptNumb~";
        Assert.True(await DatabaseObjectExistsAsync(context, provider, "index", attemptIndexName));
        Assert.True(await DatabaseObjectExistsAsync(context, provider, "foreign-key", "FK_IncomingNachaIntegrationExecution_EntryDetails_EntryDetailId"));

        await migrator.MigrateAsync(previousMigrationId);
        FinancialIntegrityEvidence.Record(provider, "traceability-rollback");
        Assert.False(await ColumnExistsAsync(context, provider, "EntryDetails", "BatchHeaderId"));
        Assert.False(await ColumnExistsAsync(context, provider, "EntryDetails", "CreatedAt"));
        Assert.False(await ColumnExistsAsync(context, provider, "AddendaRecords", "EntryDetailId"));
        Assert.Equal(amount, await ReadScalarAsync<decimal>(context, provider,
            $"SELECT {Quote(provider, "Amount")} FROM {Quote(provider, "EntryDetails")} WHERE {Quote(provider, "EntryDetailID")} = @value", entryId));
    }

    private static async Task<FinancialSnapshot> SeedExistingDataAsync(AchDbContext context, PersistenceProvider provider)
    {
        var historical = new HistoricalFinancialIntegritySeed(context, provider);
        var now = DateTimeOffset.UtcNow;
        var processingDate = new DateTime(2026, 7, 18);
        var clearingHouseConfigId = await historical.InsertIdentityAsync("ClearingHouseConfigs", new() { ["ClearingHouseId"] = 9001, ["HolidayStrategy"] = "Test" });
        var clearingHouseId = await historical.InsertIdentityAsync("ClearingHouses", new() { ["Name"] = "Clearing House Test", ["Code"] = "TEST", ["OriginCode"] = "000101006", ["ClearingHouseId"] = clearingHouseConfigId });
        var sourceInstitutionId = await historical.InsertIdentityAsync("FinancialInstitutions", InstitutionValues("Source Test", "1234567", "9", now));
        var destinationInstitutionId = await historical.InsertIdentityAsync("FinancialInstitutions", InstitutionValues("Destination Test", "8765432", "3", now));
        await historical.InsertAsync("AchCycles", new() { ["Id"] = "FIN-INT", ["CycleName"] = "Financial integrity", ["ProcessingDate"] = processingDate, ["CutoffTime"] = new TimeSpan(12, 0, 0), ["StartTime"] = new TimeSpan(8, 0, 0), ["EndTime"] = new TimeSpan(16, 0, 0), ["RescheduleOnHoliday"] = false, ["ClearingHouseId"] = clearingHouseId, ["CreatedAt"] = now, ["UpdatedAt"] = now });
        var batchId = await historical.InsertIdentityAsync("AchBatches", new() { ["AchCycleId"] = "FIN-INT", ["ServiceClassCode"] = "220", ["CompanyName"] = "TEST", ["CompanyIdentification"] = "900000001", ["CompanyEntryDescription"] = "PAGOS", ["CompanyEntryDescriptionId"] = 1, ["OriginOrOdfi"] = "12345678", ["EffectiveEntryDate"] = processingDate, ["BatchSequenceNumber"] = 1, ["TotalDebitAmount"] = 0m, ["TotalCreditAmount"] = 9_999_999_999_999_999.99m, ["CreatedAt"] = now, ["UpdatedAt"] = now });
        await historical.InsertAsync("AchTransactions", new() { ["Amount"] = 9_999_999_999_999_999.99m, ["TransactionExternalId"] = "financial-integrity-transaction", ["Reference"] = "FIN-INT", ["Type"] = "Credit", ["TransactionCode"] = "22", ["ServiceClassCode"] = "220", ["CompanyEntryDescriptionId"] = 1, ["CompanyName"] = "TEST", ["CompanyIdentification"] = "900000001", ["OriginatingDFI"] = "12345678", ["ReceivingDFI"] = "87654321", ["TraceNumber"] = "123456780000001", ["TraceSequenceNumber"] = 1, ["EffectiveEntryDate"] = processingDate, ["AddendaRecordIndicator"] = false, ["IsPrenotification"] = false, ["State"] = "Pending", ["StateChangedAtUtc"] = now.UtcDateTime, ["ContrapartidasResponseCode"] = "", ["ReturnReasonCode"] = "", ["OriginalTraceRef"] = "", ["RecipientIdNumber"] = "", ["DiscretionaryData"] = "", ["SourceAccountNumber"] = "1", ["DestinationAccountNumber"] = "2", ["SourceInstitutionId"] = sourceInstitutionId, ["DestinationInstitutionId"] = destinationInstitutionId, ["AchCycleId"] = "FIN-INT", ["AchBatchId"] = batchId, ["CreatedAt"] = now, ["UpdatedAt"] = now });
        await historical.InsertAsync("EntryDetails", new() { ["Amount"] = 922_337_203_685_477.58m, ["TransactionCode"] = "22", ["ReceivingParticipantEntityCode"] = "12345678", ["BatchNumber"] = 1 });
        await historical.InsertAsync("BatchControls", new() { ["TotalDebitAmount"] = 0m, ["TotalCreditAmount"] = 922_337_203_685_477.58m, ["BatchNumber"] = "1" });
        await historical.InsertAsync("FileControls", new() { ["BatchCount"] = 1, ["BlockCount"] = 1, ["EntryAddendaCount"] = 1, ["EntryHash"] = 1L, ["TotalDebitAmount"] = 0m, ["TotalCreditAmount"] = 922_337_203_685_477.58m });
        var ingestionId = Guid.NewGuid();
        await historical.InsertAsync("IncomingNachaFileIngestions", new() { ["Id"] = ingestionId, ["FileName"] = "financial-integrity.out", ["FileHashSha256"] = new string('a', 64), ["FileSize"] = 1L, ["ContentType"] = "text/plain", ["UploadedAtUtc"] = now.UtcDateTime, ["UploadedBy"] = "test", ["IngestionStatus"] = "Recibido", ["CycleResolutionStatus"] = "NoIntentado", ["ParsingStatus"] = "NoEjecutado", ["ResolutionEvidenceJson"] = "{}", ["CorrelationId"] = "financial-integrity", ["IsReprocess"] = false, ["Notes"] = "", ["WarningsJson"] = "[]", ["CreatedAt"] = now, ["UpdatedAt"] = now });
        var eventIds = Enumerable.Range(1, 3).Select(_ => Guid.NewGuid()).ToArray();
        for (var index = 0; index < eventIds.Length; index++)
        {
            await historical.InsertAsync("IncomingNachaProcessingEvents", new() { ["Id"] = eventIds[index], ["IncomingNachaFileIngestionId"] = ingestionId, ["EventType"] = "Integrity", ["EventStatus"] = "Persisted", ["Message"] = $"Event {index + 1}", ["EvidenceJson"] = "{}", ["OccurredAtUtc"] = now.UtcDateTime.AddMinutes(index + 1), ["RaisedBy"] = "test", ["CreatedAt"] = now, ["UpdatedAt"] = now });
        }

        return new FinancialSnapshot(
            ingestionId,
            eventIds.Order().ToArray(),
            new Dictionary<string, decimal>
            {
                ["AchBatch.TotalDebitAmount"] = 0m,
                ["AchBatch.TotalCreditAmount"] = 9_999_999_999_999_999.99m,
                ["AchTransaction.Amount"] = 9_999_999_999_999_999.99m,
                ["EntryDetail.Amount"] = 922_337_203_685_477.58m,
                ["BatchControl.TotalDebitAmount"] = 0m,
                ["BatchControl.TotalCreditAmount"] = 922_337_203_685_477.58m,
                ["FileControl.TotalDebitAmount"] = 0m,
                ["FileControl.TotalCreditAmount"] = 922_337_203_685_477.58m
            });
    }

    private static async Task AssertSnapshotAsync(
        AchDbContext context,
        PersistenceProvider provider,
        FinancialSnapshot expected)
    {
        var reader = new HistoricalFinancialSnapshotReader(context, provider);
        var eventIds = await reader.ReadEventIdsAsync(expected.IngestionId);

        Assert.True(expected.EventIds.ToHashSet().SetEquals(eventIds));

        var actual = new Dictionary<string, decimal>
        {
            ["AchBatch.TotalDebitAmount"] = await reader.ReadDecimalAsync("AchBatches", "TotalDebitAmount"),
            ["AchBatch.TotalCreditAmount"] = await reader.ReadDecimalAsync("AchBatches", "TotalCreditAmount"),
            ["AchTransaction.Amount"] = await reader.ReadDecimalAsync("AchTransactions", "Amount"),
            ["EntryDetail.Amount"] = await reader.ReadDecimalAsync("EntryDetails", "Amount"),
            ["BatchControl.TotalDebitAmount"] = await reader.ReadDecimalAsync("BatchControls", "TotalDebitAmount"),
            ["BatchControl.TotalCreditAmount"] = await reader.ReadDecimalAsync("BatchControls", "TotalCreditAmount"),
            ["FileControl.TotalDebitAmount"] = await reader.ReadDecimalAsync("FileControls", "TotalDebitAmount"),
            ["FileControl.TotalCreditAmount"] = await reader.ReadDecimalAsync("FileControls", "TotalCreditAmount")
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

    private static async Task<T> ReadScalarAsync<T>(AchDbContext context, PersistenceProvider provider, string sql, object value)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@value";
        parameter.Value = value;
        command.Parameters.Add(parameter);
        var result = await command.ExecuteScalarAsync();
        Assert.NotNull(result);
        if (typeof(T) == typeof(DateTimeOffset))
        {
            var timestamp = result is DateTimeOffset offset
                ? offset
                : new DateTimeOffset(DateTime.SpecifyKind((DateTime)result, DateTimeKind.Utc));
            return (T)(object)timestamp;
        }
        return (T)Convert.ChangeType(result, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<bool> IsColumnNullableAsync(AchDbContext context, PersistenceProvider provider, string table, string column)
        => (await ExecuteScalarAsync<string>(context, provider,
            provider == PersistenceProvider.SqlServer
                ? "SELECT IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @table AND COLUMN_NAME = @column"
                : "SELECT is_nullable FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = @table AND column_name = @column",
            table, column)).Equals("YES", StringComparison.OrdinalIgnoreCase);

    private static async Task<bool> DatabaseObjectExistsAsync(AchDbContext context, PersistenceProvider provider, string objectType, string name)
    {
        var sql = (provider, objectType) switch
        {
            (PersistenceProvider.SqlServer, "index") => "SELECT COUNT(*) FROM sys.indexes WHERE name = @table",
            (PersistenceProvider.SqlServer, _) => "SELECT COUNT(*) FROM sys.foreign_keys WHERE name = @table",
            (PersistenceProvider.PostgreSql, "index") => "SELECT COUNT(*) FROM pg_indexes WHERE schemaname = current_schema() AND indexname = @table",
            _ => "SELECT COUNT(*) FROM information_schema.table_constraints WHERE constraint_schema = current_schema() AND constraint_type = 'FOREIGN KEY' AND constraint_name = @table"
        };
        return await ExecuteScalarAsync<int>(context, provider, sql, name, string.Empty) > 0;
    }

    private static string Quote(PersistenceProvider provider, string identifier)
        => provider == PersistenceProvider.SqlServer ? $"[{identifier}]" : $"\"{identifier}\"";

    private static Dictionary<string, object?> InstitutionValues(string name, string routingNumber, string transitCode, DateTimeOffset now)
        => new()
        {
            ["Name"] = name,
            ["IsDefaultSource"] = false,
            ["RoutingNumber"] = routingNumber,
            ["TransitCode"] = transitCode,
            ["CheckDigit"] = Cfa.ACHInterbank.Domain.Helpers.DigitoChequeoHelper.CalcularDigitoChequeo($"{routingNumber}{transitCode}"),
            ["Status"] = 1,
            ["CreatedAt"] = now,
            ["UpdatedAt"] = now
        };

    private sealed class HistoricalFinancialIntegritySeed
    {
        private readonly AchDbContext _context;
        private readonly PersistenceProvider _provider;
        private readonly Dictionary<string, HashSet<string>> _columns = new(StringComparer.Ordinal);

        public HistoricalFinancialIntegritySeed(AchDbContext context, PersistenceProvider provider)
        {
            _context = context;
            _provider = provider;
        }

        public async Task InsertAsync(string table, Dictionary<string, object?> values)
            => _ = await InsertCoreAsync(table, values, returnIdentity: false);

        public async Task<int> InsertIdentityAsync(string table, Dictionary<string, object?> values, string identityColumn = "Id")
        {
            var result = await InsertCoreAsync(table, values, returnIdentity: true, identityColumn);
            return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
        }

        private async Task<object?> InsertCoreAsync(string table, Dictionary<string, object?> values, bool returnIdentity, string identityColumn = "Id")
        {
            var availableColumns = await GetColumnsAsync(table);
            var selected = values.Where(pair => availableColumns.Contains(pair.Key)).ToArray();
            if (selected.Length == 0)
            {
                throw new InvalidOperationException($"Historical seed did not find compatible columns for {table}.");
            }

            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await using var command = connection.CreateCommand();
            var tableName = Quote(table);
            var columns = string.Join(", ", selected.Select(pair => Quote(pair.Key)));
            var parameters = string.Join(", ", selected.Select((_, index) => $"@p{index}"));
            command.CommandText = returnIdentity
                ? _provider == PersistenceProvider.SqlServer
                    ? $"INSERT INTO {tableName} ({columns}) VALUES ({parameters}); SELECT CAST(SCOPE_IDENTITY() AS int);"
                    : $"INSERT INTO {tableName} ({columns}) VALUES ({parameters}) RETURNING {Quote(identityColumn)};"
                : $"INSERT INTO {tableName} ({columns}) VALUES ({parameters});";

            for (var index = 0; index < selected.Length; index++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@p{index}";
                parameter.Value = selected[index].Value ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }

            return returnIdentity
                ? await command.ExecuteScalarAsync()
                : await command.ExecuteNonQueryAsync();
        }

        private async Task<HashSet<string>> GetColumnsAsync(string table)
        {
            if (_columns.TryGetValue(table, out var cached))
            {
                return cached;
            }

            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await using var command = connection.CreateCommand();
            command.CommandText = _provider == PersistenceProvider.SqlServer
                ? "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @table"
                : "SELECT column_name FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = @table";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@table";
            parameter.Value = table;
            command.Parameters.Add(parameter);

            var columns = new HashSet<string>(StringComparer.Ordinal);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(0));
            }

            _columns.Add(table, columns);
            return columns;
        }

        private string Quote(string identifier)
            => _provider == PersistenceProvider.SqlServer ? $"[{identifier}]" : $"\"{identifier}\"";
    }

    private sealed class HistoricalFinancialSnapshotReader
    {
        private readonly AchDbContext _context;
        private readonly PersistenceProvider _provider;

        public HistoricalFinancialSnapshotReader(AchDbContext context, PersistenceProvider provider)
        {
            _context = context;
            _provider = provider;
        }

        public async Task<Guid[]> ReadEventIdsAsync(Guid ingestionId)
        {
            var connection = await OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT {Quote("Id")} FROM {Quote("IncomingNachaProcessingEvents")} "
                + $"WHERE {Quote("IncomingNachaFileIngestionId")} = @ingestionId ORDER BY {Quote("Id")}";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@ingestionId";
            parameter.Value = ingestionId;
            command.Parameters.Add(parameter);

            var values = new List<Guid>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                values.Add(reader.GetGuid(0));
            }

            return values.ToArray();
        }

        public async Task<decimal> ReadDecimalAsync(string table, string column)
        {
            var connection = await OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT {Quote(column)} FROM {Quote(table)}";
            var value = await command.ExecuteScalarAsync();
            Assert.NotNull(value);
            return Convert.ToDecimal(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        private async Task<System.Data.Common.DbConnection> OpenConnectionAsync()
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            return connection;
        }

        private string Quote(string identifier)
            => _provider == PersistenceProvider.SqlServer ? $"[{identifier}]" : $"\"{identifier}\"";
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
