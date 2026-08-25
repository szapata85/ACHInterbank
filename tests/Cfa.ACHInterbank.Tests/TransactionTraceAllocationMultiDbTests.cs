using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Repositories.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Cfa.ACHInterbank.Tests;

public sealed class TransactionTraceAllocationMultiDbTests
{
    private const int AttemptCount = 96;
    private const int ExternalSequenceBaseline = 500;
    private const string OriginatingDfi = "87654321";

    [Theory]
    [InlineData(DatabaseProvider.PostgreSql)]
    [InlineData(DatabaseProvider.SqlServer)]
    public async Task ConcurrentIndependentContexts_AllocateAndPersistDistinctDailyTraces(DatabaseProvider provider)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionVariable(provider))))
        {
            if (string.Equals(Environment.GetEnvironmentVariable("REQUIRE_TRANSACTION_TRACE_MULTI_DB_TESTS"), "true", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Missing {ConnectionVariable(provider)}.");
            }

            return;
        }

        await using var fixture = await DatabaseFixture.CreateAsync(provider);
        var sequenceDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var seeded = await fixture.MigrateAndSeedAsync(sequenceDate);

        await using (var secondStartup = fixture.CreateContext())
        {
            await secondStartup.Database.MigrateAsync();
        }

        await using (var externalInsert = fixture.CreateContext())
        {
            var externalTransaction = CreateTransaction(seeded, sequenceDate, -1, ExternalSequenceBaseline);
            externalTransaction.TransactionExternalId = $"EXTERNAL-TRACE-{Guid.NewGuid():N}";
            externalTransaction.Reference = "EXTERNAL-TRACE-BASELINE";
            externalInsert.AchTransactions.Add(externalTransaction);
            await externalInsert.SaveChangesAsync();
        }

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = Enumerable.Range(0, AttemptCount)
            .Select(index => PersistOneAsync(fixture, seeded, sequenceDate, index, start.Task))
            .ToArray();

        start.SetResult();
        var results = await Task.WhenAll(attempts);
        Assert.All(results, result => Assert.Null(result.Error));
        Assert.Equal(AttemptCount, results.Count(result => result.Error is null));

        await using var verification = fixture.CreateContext();
        var persisted = await verification.AchTransactions
            .AsNoTracking()
            .Where(transaction => transaction.TransactionExternalId.StartsWith("OPS-GAP-005-"))
            .Select(transaction => new { transaction.TraceNumber, transaction.TraceSequenceNumber })
            .ToListAsync();

        Assert.Equal(AttemptCount, persisted.Count);
        Assert.Equal(AttemptCount, persisted.Select(transaction => transaction.TraceNumber).Distinct().Count());
        Assert.DoesNotContain(
            persisted.GroupBy(transaction => transaction.TraceNumber),
            group => group.Count() > 1);
        Assert.Equal(
            Enumerable.Range(ExternalSequenceBaseline + 1, AttemptCount),
            persisted.Select(transaction => transaction.TraceSequenceNumber).Order());

        var allocatorState = await verification.AchTransactionTraceSequences
            .AsNoTracking()
            .SingleAsync(row => row.OriginatingDfi == OriginatingDfi && row.SequenceDate == sequenceDate);
        Assert.Equal(ExternalSequenceBaseline + AttemptCount, allocatorState.LastAssignedValue);

        var duplicate = CreateTransaction(seeded, sequenceDate, AttemptCount + 1, persisted[0].TraceSequenceNumber);
        duplicate.TraceNumber = persisted[0].TraceNumber;
        verification.AchTransactions.Add(duplicate);
        await Assert.ThrowsAsync<DbUpdateException>(() => verification.SaveChangesAsync());
    }

    [Fact]
    public void ProductionAllocator_DoesNotContainCheckThenInsertOrMaxPlusOne()
    {
        var repositoryRoot = FindRepositoryRoot();
        var repositorySource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "Cfa.ACHInterbank.Persistence",
            "ACH",
            "Repositories",
            "Implementation",
            "AchTransactionRepository.cs"));
        var persisterSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "Cfa.ACHInterbank.Persistence",
            "ACH",
            "Services",
            "Implementation",
            "TransactionPersister.cs"));
        var postgresMigration = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "Cfa.ACHInterbank.Persistence",
            "DataBase",
            "Migrations",
            "Postgres",
            "20260824214431_AtomicTransactionTraceAllocation.cs"));
        var sqlServerMigration = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "Cfa.ACHInterbank.Persistence.Migrations.SqlServer",
            "DataBase",
            "Migrations",
            "SqlServer",
            "20260824214552_AtomicTransactionTraceAllocation.cs"));

        Assert.DoesNotContain("GetMaxTraceSequenceAsync", repositorySource, StringComparison.Ordinal);
        Assert.DoesNotContain("ExistsTraceSequenceAsync", repositorySource, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxAsync", repositorySource, StringComparison.Ordinal);
        Assert.DoesNotContain("GetMaxTraceSequenceAsync", persisterSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ExistsTraceSequenceAsync", persisterSource, StringComparison.Ordinal);
        Assert.Contains("AllocateNextTraceSequenceAsync", persisterSource, StringComparison.Ordinal);
        Assert.Contains("TR_AchTransactions_SyncTraceSequence", postgresMigration, StringComparison.Ordinal);
        Assert.Contains("TR_AchTransactions_SyncTraceSequence", sqlServerMigration, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TwoApiProcesses_SharingOneDatabase_PersistOneHundredDistinctTraces()
    {
        var api1 = Environment.GetEnvironmentVariable("TRANSACTION_TRACE_API_1");
        var api2 = Environment.GetEnvironmentVariable("TRANSACTION_TRACE_API_2");
        var username = Environment.GetEnvironmentVariable("TRANSACTION_TRACE_API_USERNAME");
        var password = Environment.GetEnvironmentVariable("TRANSACTION_TRACE_API_PASSWORD");
        var provider = Environment.GetEnvironmentVariable("TRANSACTION_TRACE_RUNTIME_PROVIDER");
        var connectionString = Environment.GetEnvironmentVariable("TRANSACTION_TRACE_RUNTIME_CONNECTION_STRING");
        if (new[] { api1, api2, username, password, provider, connectionString }.Any(string.IsNullOrWhiteSpace))
        {
            if (string.Equals(Environment.GetEnvironmentVariable("REQUIRE_TRANSACTION_TRACE_MULTI_INSTANCE"), "true", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Multi-instance transaction trace runtime configuration is incomplete.");
            }

            return;
        }

        using var client1 = new HttpClient { BaseAddress = new Uri(api1!) };
        using var client2 = new HttpClient { BaseAddress = new Uri(api2!) };
        var token = await LoginAsync(client1, username!, password!);
        client1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await using var lookup = CreateRuntimeContext(provider!, connectionString!);
        var destinationInstitutionId = await lookup.FinancialInstitutions
            .Where(institution => !institution.IsDefaultSource)
            .Select(institution => institution.Id)
            .FirstAsync();
        var descriptionId = await lookup.CompanyEntryDescriptionCatalogs
            .Where(description => description.IsActive)
            .Select(description => description.Id)
            .FirstAsync();

        const int attempts = 100;
        var runId = Guid.NewGuid().ToString("N");
        var warmup = await PostTransactionAsync(
            client1,
            $"WARMUP-{runId}",
            999,
            destinationInstitutionId,
            descriptionId,
            Task.CompletedTask);
        Assert.Equal(HttpStatusCode.Created, warmup.StatusCode);

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requests = Enumerable.Range(0, attempts)
            .Select(index => PostTransactionAsync(
                index % 2 == 0 ? client1 : client2,
                runId,
                index,
                destinationInstitutionId,
                descriptionId,
                start.Task))
            .ToArray();

        start.SetResult();
        var statuses = await Task.WhenAll(requests);
        var successes = statuses.Count(result => result.StatusCode == HttpStatusCode.Created);
        var failures = attempts - successes;
        var expectedExternalIds = Enumerable.Range(0, attempts)
            .Select(index => $"OPS-GAP-005-RUNTIME-{runId}-{index:D3}")
            .ToArray();

        await using var verification = CreateRuntimeContext(provider!, connectionString!);
        var persisted = await verification.AchTransactions
            .AsNoTracking()
            .Where(transaction => expectedExternalIds.Contains(transaction.TransactionExternalId))
            .Select(transaction => transaction.TraceNumber)
            .ToListAsync();
        var distinct = persisted.Distinct().Count();
        var duplicates = persisted.Count - distinct;

        Console.WriteLine(
            $"[OPS-GAP-005] instances=2 attempts={attempts} successes={successes} failures={failures} persisted={persisted.Count} distinct={distinct} duplicates={duplicates}");

        Assert.Equal(attempts, statuses.Length);
        var statusSummary = string.Join(", ", statuses
            .GroupBy(result => result.StatusCode)
            .OrderBy(group => group.Key)
            .Select(group => $"{(int)group.Key}:{group.Count()}"));
        var firstFailure = statuses.FirstOrDefault(result => result.StatusCode != HttpStatusCode.Created);
        Assert.True(
            attempts == successes,
            $"Expected {attempts} created responses but received {successes}. Statuses: {statusSummary}. First failure: {firstFailure?.Body}");
        Assert.Equal(attempts, persisted.Count);
        Assert.Equal(attempts, distinct);
        Assert.Equal(0, duplicates);
    }

    private static async Task<AttemptResult> PersistOneAsync(
        DatabaseFixture fixture,
        SeededReferences seeded,
        DateOnly sequenceDate,
        int index,
        Task start)
    {
            await start;
            try
            {
                await using var context = fixture.CreateContext();
                var repository = new AchTransactionRepository(context);
                var sequence = await repository.AllocateNextTraceSequenceAsync(sequenceDate, OriginatingDfi, DateTime.UtcNow);
                await using var transaction = await context.Database.BeginTransactionAsync();
                context.AchTransactions.Add(CreateTransaction(seeded, sequenceDate, index, sequence));
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return new AttemptResult(sequence, null);
        }
        catch (Exception ex)
        {
            return new AttemptResult(null, ex);
        }
    }

    private static async Task<string> LoginAsync(HttpClient client, string username, string password)
    {
        using var response = await client.PostAsJsonAsync("Auth/login", new { username, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var content = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(content);
        return FindStringProperty(document.RootElement, "token")
            ?? throw new InvalidOperationException("Login response did not contain a token.");
    }

    private static async Task<HttpAttemptResult> PostTransactionAsync(
        HttpClient client,
        string runId,
        int index,
        int destinationInstitutionId,
        int descriptionId,
        Task start)
    {
        await start;
        using var response = await client.PostAsJsonAsync("api/transactions", new
        {
            amount = 0m,
            transactionExternalId = $"OPS-GAP-005-RUNTIME-{runId}-{index:D3}",
            reference = $"OPS005-{runId[..8]}-{index:D3}",
            type = TransactionTypeEnum.Prenotification,
            accountType = AccountTypeEnum.Checking,
            isPrenotification = true,
            destinationInstitutionId,
            sourceAccountNumber = $"100000{index:D6}",
            destinationAccountNumber = $"200000{index:D6}",
            companyName = "OPS GAP 005",
            companyIdentification = "TEST",
            companyEntryDescriptionId = descriptionId
        });
        var body = await response.Content.ReadAsStringAsync();
        return new HttpAttemptResult(response.StatusCode, body.Length <= 500 ? body : body[..500]);
    }

    private static AchDbContext CreateRuntimeContext(string provider, string connectionString)
    {
        var options = new DbContextOptionsBuilder<AchDbContext>();
        if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly("Cfa.ACHInterbank.Persistence.Migrations.SqlServer"));
        }
        else
        {
            options.UseNpgsql(connectionString);
        }

        return new AchDbContext(options.Options);
    }

    private static string? FindStringProperty(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase)
                    && property.Value.ValueKind == JsonValueKind.String)
                {
                    return property.Value.GetString();
                }

                var nested = FindStringProperty(property.Value, propertyName);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindStringProperty(item, propertyName);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static AchTransaction CreateTransaction(SeededReferences seeded, DateOnly sequenceDate, int index, int sequence)
        => new()
        {
            Amount = 1m,
            TransactionExternalId = $"OPS-GAP-005-{index:D4}-{Guid.NewGuid():N}",
            Reference = $"TRACE-{index:D4}",
            Type = TransactionTypeEnum.Credit,
            TransactionCode = "22",
            ServiceClassCode = "220",
            CompanyEntryDescriptionId = seeded.CompanyEntryDescriptionId,
            CompanyName = "OPS GAP 005",
            CompanyIdentification = "TEST",
            OriginatingDFI = OriginatingDfi,
            ReceivingDFI = "12345678",
            TraceNumber = $"{OriginatingDfi}{sequence:D7}",
            TraceSequenceNumber = sequence,
            EffectiveEntryDate = sequenceDate.ToDateTime(TimeOnly.MinValue),
            AddendaRecordIndicator = false,
            SourceAccountNumber = "SOURCE",
            DestinationAccountNumber = "DESTINATION",
            SourceInstitutionId = seeded.SourceInstitutionId,
            DestinationInstitutionId = seeded.DestinationInstitutionId,
            AchCycleId = seeded.CycleId,
            AchBatchId = seeded.BatchId
        };

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ACHInterbank.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("ACHInterbank repository root was not found.");
    }

    private static string ConnectionVariable(DatabaseProvider provider)
        => provider == DatabaseProvider.SqlServer
            ? "TRANSACTION_TRACE_SQLSERVER_CONNECTION_STRING"
            : "TRANSACTION_TRACE_POSTGRES_CONNECTION_STRING";

    public enum DatabaseProvider { PostgreSql, SqlServer }

    private sealed record AttemptResult(int? Sequence, Exception? Error);
    private sealed record HttpAttemptResult(HttpStatusCode StatusCode, string Body);
    private sealed record SeededReferences(int SourceInstitutionId, int DestinationInstitutionId, int CompanyEntryDescriptionId, string CycleId, int BatchId);

    private sealed class DatabaseFixture : IAsyncDisposable
    {
        private const string SqlServerMigrationsAssembly = "Cfa.ACHInterbank.Persistence.Migrations.SqlServer";
        private readonly string _databaseName;
        private readonly string _connectionString;
        private readonly string _adminConnectionString;

        private DatabaseFixture(DatabaseProvider provider, string databaseName, string connectionString, string adminConnectionString)
        {
            Provider = provider;
            _databaseName = databaseName;
            _connectionString = connectionString;
            _adminConnectionString = adminConnectionString;
        }

        private DatabaseProvider Provider { get; }

        public static async Task<DatabaseFixture> CreateAsync(DatabaseProvider provider)
        {
            var baseConnection = Environment.GetEnvironmentVariable(ConnectionVariable(provider))!;
            var databaseName = $"ach_trace_{Guid.NewGuid():N}";
            if (provider == DatabaseProvider.SqlServer)
            {
                var admin = new SqlConnectionStringBuilder(baseConnection) { InitialCatalog = "master" };
                await using var connection = new SqlConnection(admin.ConnectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"CREATE DATABASE [{databaseName}]";
                await command.ExecuteNonQueryAsync();
                var target = new SqlConnectionStringBuilder(baseConnection) { InitialCatalog = databaseName };
                return new DatabaseFixture(provider, databaseName, target.ConnectionString, admin.ConnectionString);
            }

            var postgresAdmin = new NpgsqlConnectionStringBuilder(baseConnection) { Database = "postgres" };
            await using (var connection = new NpgsqlConnection(postgresAdmin.ConnectionString))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
                await command.ExecuteNonQueryAsync();
            }

            var postgresTarget = new NpgsqlConnectionStringBuilder(baseConnection) { Database = databaseName };
            return new DatabaseFixture(provider, databaseName, postgresTarget.ConnectionString, postgresAdmin.ConnectionString);
        }

        public AchDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AchDbContext>();
            if (Provider == DatabaseProvider.SqlServer)
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

        public async Task<SeededReferences> MigrateAndSeedAsync(DateOnly sequenceDate)
        {
            await using var context = CreateContext();
            await context.Database.MigrateAsync();

            var config = new ClearingHouseConfig { HolidayStrategy = "Test", TimeZoneId = "America/Bogota" };
            var clearingHouse = new ClearingHouse
            {
                Name = "OPS GAP 005",
                Code = "OPS005",
                OriginCode = "TEST",
                ClearingHouseConfig = config
            };
            var source = new FinancialInstitution
            {
                Name = "OPS Source",
                IsDefaultSource = true,
                RoutingNumber = "8765432",
                TransitCode = "1",
                Status = FinancialInstitutionStatus.Active
            };
            source.CalculateCheckDigit();
            var destination = new FinancialInstitution
            {
                Name = "OPS Destination",
                RoutingNumber = "1234567",
                TransitCode = "8",
                Status = FinancialInstitutionStatus.Active
            };
            destination.CalculateCheckDigit();

            context.ClearingHouseConfigs.Add(config);
            context.ClearingHouses.Add(clearingHouse);
            context.FinancialInstitutions.AddRange(source, destination);
            await context.SaveChangesAsync();

            var descriptionId = await context.CompanyEntryDescriptionCatalogs
                .Where(description => description.IsActive)
                .Select(description => description.Id)
                .FirstAsync();
            var cycle = new AchCycle
            {
                Id = $"OPS005-{Guid.NewGuid():N}",
                CycleName = $"OPS005-{Guid.NewGuid():N}",
                ProcessingDate = sequenceDate.ToDateTime(TimeOnly.MinValue),
                StartTime = TimeSpan.Zero,
                EndTime = new TimeSpan(23, 59, 59),
                CutoffTime = new TimeSpan(23, 59, 59),
                ClearingHouseId = clearingHouse.Id
            };
            var batch = new AchBatch
            {
                AchCycle = cycle,
                ServiceClassCode = "220",
                CompanyName = "OPS GAP 005",
                CompanyIdentification = "TEST",
                CompanyEntryDescription = "PAGOS",
                CompanyEntryDescriptionId = descriptionId,
                OriginOrOdfi = OriginatingDfi,
                EffectiveEntryDate = sequenceDate.ToDateTime(TimeOnly.MinValue),
                BatchSequenceNumber = 1
            };
            context.AchBatches.Add(batch);
            await context.SaveChangesAsync();

            return new SeededReferences(source.Id, destination.Id, descriptionId, cycle.Id, batch.Id);
        }

        public async ValueTask DisposeAsync()
        {
            if (Provider == DatabaseProvider.SqlServer)
            {
                await using var connection = new SqlConnection(_adminConnectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]";
                await command.ExecuteNonQueryAsync();
                return;
            }

            await using var postgres = new NpgsqlConnection(_adminConnectionString);
            await postgres.OpenAsync();
            await using var terminate = postgres.CreateCommand();
            terminate.CommandText = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @database AND pid <> pg_backend_pid()";
            terminate.Parameters.AddWithValue("database", _databaseName);
            await terminate.ExecuteNonQueryAsync();
            await using var drop = postgres.CreateCommand();
            drop.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\"";
            await drop.ExecuteNonQueryAsync();
        }
    }
}
