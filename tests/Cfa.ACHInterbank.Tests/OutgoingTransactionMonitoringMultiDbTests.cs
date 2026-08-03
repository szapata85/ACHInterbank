using Cfa.ACHInterbank.Application.OutgoingTransactionMonitoring;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.OutgoingTransactionMonitoring;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Cfa.ACHInterbank.Tests;

public sealed class OutgoingTransactionMonitoringMultiDbTests
{
    private const string RequiredVariable = "RUN_OUTGOING_MONITOR_MULTIDB";

    [Fact]
    [Trait("Category", "OutgoingMonitorMultiDb")]
    [Trait("Provider", "SqlServer")]
    public Task QueryAndDetail_RunAgainstSqlServer() => RunAsync(DatabaseProvider.SqlServer);

    [Fact]
    [Trait("Category", "OutgoingMonitorMultiDb")]
    [Trait("Provider", "PostgreSql")]
    public Task QueryAndDetail_RunAgainstPostgreSql() => RunAsync(DatabaseProvider.PostgreSql);

    [Fact]
    [Trait("Category", "LocalRuntimeFixture")]
    public async Task SeedDeterministicLocalRuntimeFixture_WhenExplicitlyEnabled()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("SEED_OUTGOING_MONITOR_RUNTIME_FIXTURE"), "true", StringComparison.OrdinalIgnoreCase))
            return;
        var connectionString = Environment.GetEnvironmentVariable("OUTGOING_MONITOR_RUNTIME_SQLSERVER_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Falta OUTGOING_MONITOR_RUNTIME_SQLSERVER_CONNECTION_STRING.");
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsAssembly("Cfa.ACHInterbank.Persistence.Migrations.SqlServer"))
            .Options;
        await using var context = new AchDbContext(options);
        var id = await SeedAsync(context);
        (await context.AchTransactions.AnyAsync(item => item.Id == id)).Should().BeTrue();
    }

    private static async Task RunAsync(DatabaseProvider provider)
    {
        EnsureConfiguration(provider);
        await using var fixture = await DatabaseFixture.CreateAsync(provider);
        await using var context = fixture.CreateContext();
        await context.Database.MigrateAsync();
        var transactionId = await SeedAsync(context);
        var persisted = await context.AchTransactions.AsNoTracking()
            .Where(item => item.Id == transactionId)
            .Select(item => new { item.CreatedAt, item.Direction, item.ClassificationStatus })
            .SingleAsync();
        persisted.Direction.Should().Be(AchTransactionDirection.Outgoing);
        persisted.ClassificationStatus.Should().Be(AchTransactionClassificationStatus.Determined);
        var service = new OutgoingTransactionMonitoringQueryService(context, new OutgoingTransactionMonitoringStatusPolicy(),
            new FixedTimeProvider(persisted.CreatedAt.AddHours(1)));

        var page = await service.SearchAsync(new OutgoingTransactionMonitoringQuery
        {
            FromUtc = persisted.CreatedAt.AddDays(-1),
            ToUtc = persisted.CreatedAt.AddDays(1),
            PageSize = 10
        });

        page.TotalItems.Should().Be(1, "solo la clasificación persistida de salida determinada pertenece al monitor");
        page.Items.Should().ContainSingle();
        page.Items[0].Id.Should().Be(transactionId);
        page.Items[0].MaskedDestinationAccount.Should().Be("******7890");
        page.Items[0].InitialResultDisplayName.Should().Be("Aceptada");
        page.Items[0].SubsequentSituationDisplayName.Should().Be("Devuelta posteriormente");
        page.Items[0].FileName.Should().Be("SALIDA.002");
        page.Items[0].FileVersion.Should().Be(2);
        page.Items[0].FileLifecycleStatusDisplayName.Should().Be("Protegido; sin evidencia de transmisión");

        var detail = await service.GetDetailAsync(transactionId, includeTechnicalDetail: false);
        detail.Should().NotBeNull();
        detail!.Files.Should().HaveCount(2);
        detail.Files.Select(item => item.Version).Should().Equal(1, 2);
        detail.Files.Should().OnlyContain(item => !item.HasTransmissionEvidence);
        detail.Timeline.Should().Contain(item => item.StageDisplayName == "Aceptación");
        detail.Timeline.Should().Contain(item => item.StageDisplayName == "Devolución");
        detail.TechnicalDetail.Should().BeNull();
    }

    private static async Task<int> SeedAsync(AchDbContext context)
    {
        var existing = await context.AchTransactions.AsNoTracking()
            .Where(item => item.TransactionExternalId == "MON2-OUT-001")
            .Select(item => (int?)item.Id)
            .SingleOrDefaultAsync();
        if (existing.HasValue) return existing.Value;

        var now = new DateTimeOffset(2026, 8, 2, 10, 0, 0, TimeSpan.Zero);
        var configuration = new ClearingHouseConfig { TimeZoneId = "America/Bogota" };
        context.Add(configuration);
        await context.SaveChangesAsync();
        var house = new ClearingHouse { Name = "Cámara de prueba", Code = "MON2", OriginCode = "MON2", ClearingHouseId = configuration.Id };
        var source = Institution("CFA local", true, "00001", "001");
        var destination = Institution("Entidad destino de prueba", false, "00002", "002");
        context.AddRange(house, source, destination);
        await context.SaveChangesAsync();
        var cycle = new AchCycle
        {
            Id = "MON2-CYCLE-20260802",
            CycleName = "Ciclo monitor 2",
            ProcessingDate = new DateTime(2026, 8, 2),
            StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(12), CutoffTime = TimeSpan.FromHours(11),
            ClearingHouseId = house.Id, OperationalStatus = AchCycleOperationalStatus.Open
        };
        var batch = new AchBatch
        {
            AchCycleId = cycle.Id, ServiceClassCode = "220", CompanyName = "CFA",
            CompanyIdentification = "MONITOR", OriginOrOdfi = "00000001", EffectiveEntryDate = new DateTime(2026, 8, 2),
            BatchSequenceNumber = 1, CompanyEntryDescriptionId = 1
        };
        context.AddRange(cycle, batch);
        await context.SaveChangesAsync();

        var outgoing = Transaction("MON2-OUT-001", "123456789012345", AchTransactionDirection.Outgoing,
            AchTransactionClassificationStatus.Determined, source.Id, destination.Id, cycle.Id, batch.Id, now);
        var historical = Transaction("MON2-OLD-001", "123456789012346", AchTransactionDirection.Unknown,
            AchTransactionClassificationStatus.Unknown, source.Id, destination.Id, cycle.Id, batch.Id, now.AddMinutes(1));
        context.AchTransactions.AddRange(outgoing, historical);
        await context.SaveChangesAsync();

        context.AchTransactionStateEvents.AddRange(
            new AchTransactionStateEvent
            {
                AchTransactionId = outgoing.Id, FromState = AchTransferStateEnum.Pending, ToState = AchTransferStateEnum.AppliedTacitly,
                Source = AchStateEventSourceEnum.Epr, OccurredAtUtc = new DateTime(2026, 8, 2, 10, 10, 0, DateTimeKind.Utc)
            },
            new AchTransactionStateEvent
            {
                AchTransactionId = outgoing.Id, FromState = AchTransferStateEnum.AppliedTacitly, ToState = AchTransferStateEnum.ReturnedByEpr,
                Source = AchStateEventSourceEnum.Epr, ReasonCode = "R01", ResolvedReasonDescription = "Fondos insuficientes en el contexto de prueba",
                OccurredAtUtc = new DateTime(2026, 8, 2, 11, 0, 0, DateTimeKind.Utc)
            });
        var file1 = File(cycle.Id, house.Id, "SALIDA.001", 1, new DateTime(2026, 8, 2, 10, 20, 0, DateTimeKind.Utc));
        var file2 = File(cycle.Id, house.Id, "SALIDA.002", 2, new DateTime(2026, 8, 2, 10, 30, 0, DateTimeKind.Utc));
        context.AchFileExports.AddRange(file1, file2);
        await context.SaveChangesAsync();
        context.AchFileExportTransactions.AddRange(
            new AchFileExportTransaction { AchFileExportId = file1.Id, AchTransactionId = outgoing.Id, AchCycleId = cycle.Id, AchBatchId = batch.Id, FileSequence = 1, TraceNumber = outgoing.TraceNumber, Amount = outgoing.Amount, IncludedAtUtc = file1.GeneratedAtUtc },
            new AchFileExportTransaction { AchFileExportId = file2.Id, AchTransactionId = outgoing.Id, AchCycleId = cycle.Id, AchBatchId = batch.Id, FileSequence = 1, TraceNumber = outgoing.TraceNumber, Amount = outgoing.Amount, IncludedAtUtc = file2.GeneratedAtUtc });
        await context.SaveChangesAsync();
        return outgoing.Id;
    }

    private static FinancialInstitution Institution(string name, bool source, string routing, string transit)
    {
        var institution = new FinancialInstitution { Name = name, IsDefaultSource = source, RoutingNumber = routing, TransitCode = transit, Status = FinancialInstitutionStatus.Active };
        institution.CalculateCheckDigit();
        return institution;
    }

    private static AchTransaction Transaction(string externalId, string trace, AchTransactionDirection direction,
        AchTransactionClassificationStatus classification, int sourceId, int destinationId, string cycleId, int batchId, DateTimeOffset createdAt)
        => new()
        {
            Amount = 125000.50m, TransactionExternalId = externalId, Reference = externalId, Type = TransactionTypeEnum.Credit,
            TransactionCode = "22", ServiceClassCode = "220", CompanyEntryDescriptionId = 1, CompanyName = "CFA", CompanyIdentification = "MONITOR",
            OriginatingDFI = "00000001", ReceivingDFI = "00000002", TraceNumber = trace, TraceSequenceNumber = 1,
            EffectiveEntryDate = new DateTime(2026, 8, 2), Direction = direction, Origin = AchTransactionOrigin.Cfa,
            MonetaryIntegrationRoute = direction == AchTransactionDirection.Outgoing ? AchMonetaryIntegrationRoute.ProcContrapartidas : AchMonetaryIntegrationRoute.ManualReview,
            ClassificationStatus = classification, SourceInstitutionWasDefaultAtCreation = true, ClassifiedAtUtc = classification == AchTransactionClassificationStatus.Determined ? createdAt.UtcDateTime : null,
            ClassificationVersion = classification == AchTransactionClassificationStatus.Determined ? 1 : 0, State = AchTransferStateEnum.ReturnedByEpr,
            StateChangedAtUtc = createdAt.UtcDateTime, SourceAccountNumber = "0000001111", DestinationAccountNumber = "1234567890",
            SourceInstitutionId = sourceId, DestinationInstitutionId = destinationId, AchCycleId = cycleId, AchBatchId = batchId,
            DiscretionaryData = string.Empty, CreatedAt = createdAt, UpdatedAt = createdAt
        };

    private static AchFileExport File(string cycleId, int houseId, string name, int version, DateTime generated)
        => new()
        {
            AchCycleId = cycleId, ClearingHouseId = houseId, ExportKind = "OUT", FileName = name,
            TotalRecords = 1, TotalTransactions = 1, IsEncrypted = true, GeneratedAtUtc = generated, Version = version,
            LifecycleStatus = AchFileExportLifecycleStatus.Protected
        };

    private static void EnsureConfiguration(DatabaseProvider provider)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(RequiredVariable), "true", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{RequiredVariable}=true es obligatorio; la prueba no admite omisiones.");
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionVariable(provider))))
            throw new InvalidOperationException($"Falta {ConnectionVariable(provider)}.");
    }

    private static string ConnectionVariable(DatabaseProvider provider) => provider == DatabaseProvider.SqlServer
        ? "OUTGOING_MONITOR_SQLSERVER_CONNECTION_STRING" : "OUTGOING_MONITOR_POSTGRES_CONNECTION_STRING";

    private enum DatabaseProvider { SqlServer, PostgreSql }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }

    private sealed class DatabaseFixture : IAsyncDisposable
    {
        private readonly string _databaseName;
        private readonly string _connectionString;
        private readonly string _adminConnectionString;
        private DatabaseFixture(DatabaseProvider provider, string databaseName, string connectionString, string adminConnectionString)
        { Provider = provider; _databaseName = databaseName; _connectionString = connectionString; _adminConnectionString = adminConnectionString; }
        public DatabaseProvider Provider { get; }

        public static async Task<DatabaseFixture> CreateAsync(DatabaseProvider provider)
        {
            var baseConnection = Environment.GetEnvironmentVariable(ConnectionVariable(provider))!;
            var databaseName = $"ach_monitor_{Guid.NewGuid():N}";
            if (provider == DatabaseProvider.SqlServer)
            {
                var admin = new SqlConnectionStringBuilder(baseConnection) { InitialCatalog = "master" };
                await using var connection = new SqlConnection(admin.ConnectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand(); command.CommandText = $"CREATE DATABASE [{databaseName}]"; await command.ExecuteNonQueryAsync();
                var target = new SqlConnectionStringBuilder(baseConnection) { InitialCatalog = databaseName };
                return new DatabaseFixture(provider, databaseName, target.ConnectionString, admin.ConnectionString);
            }
            var postgresAdmin = new NpgsqlConnectionStringBuilder(baseConnection) { Database = "postgres" };
            await using (var connection = new NpgsqlConnection(postgresAdmin.ConnectionString))
            { await connection.OpenAsync(); await using var command = connection.CreateCommand(); command.CommandText = $"CREATE DATABASE \"{databaseName}\""; await command.ExecuteNonQueryAsync(); }
            var targetPostgres = new NpgsqlConnectionStringBuilder(baseConnection) { Database = databaseName };
            return new DatabaseFixture(provider, databaseName, targetPostgres.ConnectionString, postgresAdmin.ConnectionString);
        }

        public AchDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AchDbContext>();
            if (Provider == DatabaseProvider.SqlServer) options.UseSqlServer(_connectionString, sql => sql.MigrationsAssembly("Cfa.ACHInterbank.Persistence.Migrations.SqlServer"));
            else options.UseNpgsql(_connectionString);
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            return new AchDbContext(options.Options);
        }

        public async ValueTask DisposeAsync()
        {
            if (Provider == DatabaseProvider.SqlServer)
            {
                await using var connection = new SqlConnection(_adminConnectionString); await connection.OpenAsync();
                await using var command = connection.CreateCommand(); command.CommandText = $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]"; await command.ExecuteNonQueryAsync(); return;
            }
            await using var postgres = new NpgsqlConnection(_adminConnectionString); await postgres.OpenAsync();
            await using var terminate = postgres.CreateCommand(); terminate.CommandText = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname=@database AND pid<>pg_backend_pid()"; terminate.Parameters.AddWithValue("database", _databaseName); await terminate.ExecuteNonQueryAsync();
            await using var drop = postgres.CreateCommand(); drop.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\""; await drop.ExecuteNonQueryAsync();
        }
    }
}
