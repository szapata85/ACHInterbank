using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using System.Data.Common;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchIncomingReturnDbFirstIdempotencyTests
{
    private const string SqlServerMigrationsAssembly = "Cfa.ACHInterbank.Persistence.Migrations.SqlServer";

    [Fact]
    public void ReturnIdentity_IsStableAcrossTransportMetadata_AndIncludesCausal()
    {
        var first = AchIncomingEventIdentityPolicy.BuildReturnKey(7001, 10, "123456780000001", "r01");
        var replay = AchIncomingEventIdentityPolicy.BuildReturnKey(7001, 10, " 123456780000001 ", " R01 ");
        var differentCause = AchIncomingEventIdentityPolicy.BuildReturnKey(7001, 10, "123456780000001", "R02");

        Assert.Equal(first, replay);
        Assert.NotEqual(first, differentCause);
    }

    [Fact]
    public async Task ReturnStateEvent_UniqueIdentity_IsEnforcedByRelationalDatabase()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = Options(connection);

        await using (var setup = new AchDbContext(options))
        {
            setup.AuditEnabled = false;
            await setup.Database.EnsureCreatedAsync();
            Seed(setup);
        }

        var key = AchIncomingEventIdentityPolicy.BuildReturnKey(7001, 10, "123456780000001", "R01");
        await using var context = new AchDbContext(options) { AuditEnabled = false };
        context.AchTransactionStateEvents.AddRange(
            Event(key),
            Event(key));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        await using var verify = new AchDbContext(options);
        Assert.Equal(0, await verify.AchTransactionStateEvents.CountAsync());
    }

    [Fact]
    public async Task SameReturn_ThroughTwoIndependentDbContexts_AppliesOnceAndReplaysIdempotently()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = Options(connection);

        await using (var setup = new AchDbContext(options))
        {
            setup.AuditEnabled = false;
            await setup.Database.EnsureCreatedAsync();
            Seed(setup);
        }

        var key = AchIncomingEventIdentityPolicy.BuildReturnKey(7001, 10, "123456780000001", "R01");
        await using (var firstContext = new AchDbContext(options) { AuditEnabled = false })
        {
            var first = await new AchStateTransitionService(firstContext).TransitionAsync(
                new AchStateTransitionRequest(10, AchTransferStateEnum.ReturnedByEpr, AchStateEventSourceEnum.Epr,
                    "R01", "{}", "123456780000001", DateTime.UtcNow, key, 7001));
            Assert.True(first.Applied);
        }

        await using (var secondContext = new AchDbContext(options) { AuditEnabled = false })
        {
            var replay = await new AchStateTransitionService(secondContext).TransitionAsync(
                new AchStateTransitionRequest(10, AchTransferStateEnum.ReturnedByEpr, AchStateEventSourceEnum.Epr,
                    "R01", "{}", "123456780000001", DateTime.UtcNow.AddHours(1), key, 7001));
            Assert.True(replay.WasDuplicate);
            Assert.False(replay.Applied);
        }

        await using var verify = new AchDbContext(options);
        Assert.Equal(1, await verify.AchTransactionStateEvents.CountAsync(x => x.AchTransactionId == 10));
        Assert.Equal(AchTransferStateEnum.ReturnedByEpr, await verify.AchTransactions.Where(x => x.Id == 10).Select(x => x.State).SingleAsync());
        Assert.Equal("R01", await verify.AchTransactions.Where(x => x.Id == 10).Select(x => x.ReturnReasonCode).SingleAsync());
    }

    [FinancialIntegrityFact(FinancialPersistenceMigrationTests.PersistenceProvider.SqlServer)]
    [Trait("Category", "IncomingReturnDbFirst")]
    public Task SameReturn_ConcurrentIndependentContexts_IsIdempotent_OnSqlServer()
        => AssertConcurrentProviderScenarioAsync(ProviderKind.SqlServer);

    [FinancialIntegrityFact(FinancialPersistenceMigrationTests.PersistenceProvider.PostgreSql)]
    [Trait("Category", "IncomingReturnDbFirst")]
    public Task SameReturn_ConcurrentIndependentContexts_IsIdempotent_OnPostgreSql()
        => AssertConcurrentProviderScenarioAsync(ProviderKind.PostgreSql);

    private static async Task AssertConcurrentProviderScenarioAsync(ProviderKind provider)
    {
        await using var fixture = await ProviderDatabaseFixture.CreateAsync(provider);
        (int TransactionId, int ClearingHouseId) seeded;
        await using (var setup = fixture.CreateContext())
        {
            setup.AuditEnabled = false;
            await setup.Database.MigrateAsync();
            seeded = await SeedProviderAsync(setup);
        }

        Assert.True(await fixture.HasIdempotencyIndexAsync());
        var key = AchIncomingEventIdentityPolicy.BuildReturnKey(seeded.ClearingHouseId, seeded.TransactionId, "123456780000001", "R01");
        var gate = new IdempotencyReadGate();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));

        var attempts = Enumerable.Range(0, 2)
            .Select(_ => Task.Run(async () =>
            {
                await using var context = fixture.CreateContext(gate);
                context.AuditEnabled = false;
                return await new AchStateTransitionService(context).TransitionAsync(
                    new AchStateTransitionRequest(
                        seeded.TransactionId,
                        AchTransferStateEnum.ReturnedByEpr,
                        AchStateEventSourceEnum.Epr,
                        "R01",
                        "{}",
                        "123456780000001",
                        DateTime.UtcNow,
                        key,
                        seeded.ClearingHouseId),
                    timeout.Token);
            }, timeout.Token))
            .ToArray();

        await gate.WaitForBothReadsAsync(timeout.Token);
        var results = await Task.WhenAll(attempts);

        Assert.Single(results, x => x.Applied);
        Assert.Single(results, x => x.WasDuplicate);

        await using var verify = fixture.CreateContext();
        Assert.Equal(1, await verify.AchTransactionStateEvents.CountAsync(x => x.IdempotencyKey == key));
        var stateEvent = await verify.AchTransactionStateEvents.SingleAsync(x => x.IdempotencyKey == key);
        Assert.Equal("R01", stateEvent.ReasonCode);
        var transaction = await verify.AchTransactions.SingleAsync(x => x.Id == seeded.TransactionId);
        Assert.Equal(AchTransferStateEnum.ReturnedByEpr, transaction.State);
        Assert.Equal("R01", transaction.ReturnReasonCode);
    }

    private static DbContextOptions<AchDbContext> Options(SqliteConnection connection)
        => new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;

    private static void Seed(AchDbContext context)
    {
        context.ClearingHouses.Add(new ClearingHouse { Id = 7001, ClearingHouseId = 1, Code = "CENIT", Name = "Test", OriginCode = "000101006" });
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, ClearingHouseId = 7001, PaymentRailCode = "CENIT" });
        var source = new FinancialInstitution { Id = 1, Name = "Source", RoutingNumber = "00000", TransitCode = "000" };
        source.CalculateCheckDigit();
        var destination = new FinancialInstitution { Id = 2, Name = "Destination", RoutingNumber = "00000", TransitCode = "001" };
        destination.CalculateCheckDigit();
        context.FinancialInstitutions.AddRange(source, destination);
        context.AchCycles.Add(new AchCycle { Id = "C1", CycleName = "C1", ProcessingDate = new DateTime(2026, 5, 16), CutoffTime = new TimeSpan(8, 0, 0), ClearingHouseId = 7001 });
        context.AchBatches.Add(new AchBatch { Id = 1, AchCycleId = "C1", CompanyEntryDescriptionId = 1, EffectiveEntryDate = new DateTime(2026, 5, 16) });
        context.AchTransactions.Add(new AchTransaction
        {
            Id = 10,
            TraceNumber = "123456780000001",
            AchCycleId = "C1",
            Type = TransactionTypeEnum.Credit,
            State = AchTransferStateEnum.Pending,
            EffectiveEntryDate = new DateTime(2026, 5, 16),
            TransactionCode = "22",
            ReceivingDFI = "12345678",
            OriginatingDFI = "12345678",
            Amount = 100,
            Reference = "R",
            SourceAccountNumber = "1",
            DestinationAccountNumber = "2",
            OriginalTraceRef = "ALT000000000010",
            RecipientIdNumber = "RID-001",
            SourceInstitutionId = 1,
            DestinationInstitutionId = 2,
            AchBatchId = 1
        });
        context.SaveChanges();
    }

    private static AchTransactionStateEvent Event(string key)
        => new()
        {
            AchTransactionId = 10,
            FromState = AchTransferStateEnum.Pending,
            ToState = AchTransferStateEnum.ReturnedByEpr,
            Source = AchStateEventSourceEnum.Epr,
            ReasonCode = "R01",
            IdempotencyKey = key,
            OccurredAtUtc = DateTime.UtcNow
        };

    private static async Task<(int TransactionId, int ClearingHouseId)> SeedProviderAsync(AchDbContext context)
    {
        var configuration = new ClearingHouseConfig { PaymentRailCode = "CENIT" };
        context.ClearingHouseConfigs.Add(configuration);
        await context.SaveChangesAsync();

        var clearingHouse = new ClearingHouse { ClearingHouseId = configuration.Id, Code = "CENIT", Name = "Test", OriginCode = "000101006" };
        context.ClearingHouses.Add(clearingHouse);
        await context.SaveChangesAsync();

        var source = new FinancialInstitution { Name = "Source", RoutingNumber = "00000", TransitCode = "000" };
        source.CalculateCheckDigit();
        var destination = new FinancialInstitution { Name = "Destination", RoutingNumber = "00000", TransitCode = "001" };
        destination.CalculateCheckDigit();
        context.FinancialInstitutions.AddRange(source, destination);
        await context.SaveChangesAsync();

        var cycle = new AchCycle { Id = $"C{Guid.NewGuid():N}", CycleName = "C1", ProcessingDate = new DateTime(2026, 5, 16), CutoffTime = new TimeSpan(8, 0, 0), ClearingHouseId = clearingHouse.Id };
        context.AchCycles.Add(cycle);
        await context.SaveChangesAsync();

        var batch = new AchBatch { AchCycleId = cycle.Id, CompanyEntryDescriptionId = 1, EffectiveEntryDate = new DateTime(2026, 5, 16) };
        context.AchBatches.Add(batch);
        await context.SaveChangesAsync();

        var transaction = new AchTransaction
        {
            TraceNumber = "123456780000001",
            AchCycleId = cycle.Id,
            Type = TransactionTypeEnum.Credit,
            State = AchTransferStateEnum.Pending,
            EffectiveEntryDate = new DateTime(2026, 5, 16),
            TransactionCode = "22",
            ReceivingDFI = "12345678",
            OriginatingDFI = "12345678",
            Amount = 100,
            Reference = "R",
            SourceAccountNumber = "1",
            DestinationAccountNumber = "2",
            OriginalTraceRef = "ALT000000000010",
            RecipientIdNumber = "RID-001",
            SourceInstitutionId = source.Id,
            DestinationInstitutionId = destination.Id,
            AchBatchId = batch.Id
        };
        context.AchTransactions.Add(transaction);
        await context.SaveChangesAsync();
        return (transaction.Id, clearingHouse.Id);
    }

    private enum ProviderKind
    {
        SqlServer,
        PostgreSql
    }

    private sealed class IdempotencyReadGate : DbCommandInterceptor
    {
        private readonly TaskCompletionSource _bothReadsReached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _reads;

        public async Task WaitForBothReadsAsync(CancellationToken ct)
            => await _bothReadsReached.Task.WaitAsync(ct);

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("AchTransactionStateEvents", StringComparison.OrdinalIgnoreCase)
                && command.CommandText.Contains("IdempotencyKey", StringComparison.OrdinalIgnoreCase)
                && command.CommandText.Contains("SELECT", StringComparison.OrdinalIgnoreCase)
                && Interlocked.Increment(ref _reads) <= 2)
            {
                if (Volatile.Read(ref _reads) == 2)
                {
                    _bothReadsReached.TrySetResult();
                }

                await _bothReadsReached.Task.WaitAsync(cancellationToken);
            }

            return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }

    private sealed class ProviderDatabaseFixture : IAsyncDisposable
    {
        private readonly string _connectionString;
        private readonly string _adminConnectionString;
        private readonly string? _databaseName;
        private readonly string? _schemaName;

        private ProviderDatabaseFixture(ProviderKind provider, string connectionString, string adminConnectionString, string? databaseName, string? schemaName)
        {
            Provider = provider;
            _connectionString = connectionString;
            _adminConnectionString = adminConnectionString;
            _databaseName = databaseName;
            _schemaName = schemaName;
        }

        public ProviderKind Provider { get; }

        public static async Task<ProviderDatabaseFixture> CreateAsync(ProviderKind provider)
        {
            var settingName = provider == ProviderKind.SqlServer
                ? "FINANCIAL_INTEGRITY_SQLSERVER_CONNECTION_STRING"
                : "FINANCIAL_INTEGRITY_POSTGRES_CONNECTION_STRING";
            var baseConnectionString = Environment.GetEnvironmentVariable(settingName)
                ?? throw new InvalidOperationException($"Missing {settingName}.");

            if (provider == ProviderKind.SqlServer)
            {
                var builder = new SqlConnectionStringBuilder(baseConnectionString) { InitialCatalog = "master" };
                var databaseName = $"achinterbank_incoming_return_{Guid.NewGuid():N}";
                await using var connection = new SqlConnection(builder.ConnectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"CREATE DATABASE [{databaseName}]";
                await command.ExecuteNonQueryAsync();
                builder.InitialCatalog = databaseName;
                return new ProviderDatabaseFixture(provider, builder.ConnectionString, new SqlConnectionStringBuilder(baseConnectionString) { InitialCatalog = "master" }.ConnectionString, databaseName, null);
            }

            var postgresBuilder = new NpgsqlConnectionStringBuilder(baseConnectionString);
            var schemaName = $"incoming_return_{Guid.NewGuid():N}";
            await using (var connection = new NpgsqlConnection(postgresBuilder.ConnectionString))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"CREATE SCHEMA \"{schemaName}\"";
                await command.ExecuteNonQueryAsync();
            }

            postgresBuilder.SearchPath = schemaName;
            return new ProviderDatabaseFixture(provider, postgresBuilder.ConnectionString, baseConnectionString, null, schemaName);
        }

        public AchDbContext CreateContext(params IInterceptor[] interceptors)
        {
            var options = new DbContextOptionsBuilder<AchDbContext>();
            if (Provider == ProviderKind.SqlServer)
            {
                options.UseSqlServer(_connectionString, sql => sql.MigrationsAssembly(SqlServerMigrationsAssembly));
            }
            else
            {
                options.UseNpgsql(_connectionString);
            }

            if (interceptors.Length > 0)
            {
                options.AddInterceptors(interceptors);
            }

            return new AchDbContext(options.Options);
        }

        public async Task<bool> HasIdempotencyIndexAsync()
        {
            await using var context = CreateContext();
            await using var connection = context.Database.GetDbConnection();
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = Provider == ProviderKind.SqlServer
                ? "SELECT CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_AchTransactionStateEvents_IdempotencyKey') THEN 1 ELSE 0 END"
                : "SELECT CASE WHEN EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = current_schema() AND indexname = 'UX_AchTransactionStateEvents_IdempotencyKey') THEN 1 ELSE 0 END";
            return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
        }

        public async ValueTask DisposeAsync()
        {
            if (Provider == ProviderKind.SqlServer)
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
            await using var drop = postgres.CreateCommand();
            drop.CommandText = $"DROP SCHEMA IF EXISTS \"{_schemaName}\" CASCADE";
            await drop.ExecuteNonQueryAsync();
        }
    }
}
