using Cfa.ACHInterbank.Application.ACH.Responses.Reprocessing;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchResponseReprocessDispatcherMultiDbTests
{
    private const string RequiredVariable = "JOB4_REQUIRE_DATABASES";

    [Fact]
    [Trait("Category", "Job4ReprocessMultiDb")]
    [Trait("Provider", "SqlServer")]
    public Task DispatcherCertification_RunsAgainstSqlServer()
        => RunCertificationAsync(DatabaseProvider.SqlServer);

    [Fact]
    [Trait("Category", "Job4ReprocessMultiDb")]
    [Trait("Provider", "PostgreSql")]
    public Task DispatcherCertification_RunsAgainstPostgreSql()
        => RunCertificationAsync(DatabaseProvider.PostgreSql);

    private static async Task RunCertificationAsync(DatabaseProvider provider)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(RequiredVariable), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        EnsureRequiredConfiguration(provider);
        await using var fixture = await DatabaseFixture.CreateAsync(provider);
        await VerifyMigrationLifecycleAsync(fixture);
        await VerifyClaimHeartbeatAndSingleExecutionAsync(fixture);
        await VerifyLostOwnershipAndRecoveryAsync(fixture);
        await VerifyTerminalResultsAsync(fixture);
    }

    private static async Task VerifyMigrationLifecycleAsync(DatabaseFixture fixture)
    {
        await using var context = fixture.CreateContext();
        var migrations = context.Database.GetMigrations().ToList();
        var migration = migrations.Single(x => x.EndsWith("_Job41ReprocessDispatcher", StringComparison.Ordinal));
        var previous = migrations[migrations.IndexOf(migration) - 1];
        var migrator = context.Database.GetService<IMigrator>();

        await migrator.MigrateAsync(previous);
        await context.Database.ExecuteSqlRawAsync(fixture.Provider == DatabaseProvider.SqlServer
            ? "CREATE TABLE [Job4PreservedData] ([Id] int NOT NULL PRIMARY KEY, [Value] nvarchar(20) NOT NULL); INSERT INTO [Job4PreservedData] VALUES (1, N'preserved');"
            : "CREATE TABLE \"Job4PreservedData\" (\"Id\" integer PRIMARY KEY, \"Value\" varchar(20) NOT NULL); INSERT INTO \"Job4PreservedData\" VALUES (1, 'preserved');");

        await migrator.MigrateAsync(migration);
        Assert.Equal(9, await CountJob41ColumnsAsync(context, fixture.Provider));
        Assert.Equal(1, await CountAcquisitionIndexAsync(context, fixture.Provider));

        await migrator.MigrateAsync(previous);
        Assert.Equal(1, await ScalarAsync(context, fixture.Provider == DatabaseProvider.SqlServer
            ? "SELECT COUNT(*) FROM [Job4PreservedData] WHERE [Value] = N'preserved'"
            : "SELECT COUNT(*) FROM \"Job4PreservedData\" WHERE \"Value\" = 'preserved'"));
        Assert.Equal(0, await CountJob41ColumnsAsync(context, fixture.Provider));

        await migrator.MigrateAsync(migration);
        Assert.Equal(9, await CountJob41ColumnsAsync(context, fixture.Provider));
        Assert.Contains(migration, await context.Database.GetAppliedMigrationsAsync());
    }

    private static async Task VerifyClaimHeartbeatAndSingleExecutionAsync(DatabaseFixture fixture)
    {
        await using (var seed = fixture.CreateContext())
        {
            var seededResponse = NewResponse(AchResponseProcessingStatus.PendienteReproceso);
            seed.AchResponses.Add(seededResponse);
            seed.AchResponseReprocessAttempts.Add(NewAttempt(seededResponse.Id));
            await seed.SaveChangesAsync();
        }

        var pipeline = new ControlledPipeline();
        await using var services = BuildServices(fixture, pipeline);
        await using var scopeA = services.CreateAsyncScope();
        await using var scopeB = services.CreateAsyncScope();
        var dispatcherA = scopeA.ServiceProvider.GetRequiredService<IAchResponseReprocessDispatcher>();
        var dispatcherB = scopeB.ServiceProvider.GetRequiredService<IAchResponseReprocessDispatcher>();
        var lease = TimeSpan.FromSeconds(6);

        var first = dispatcherA.DispatchAsync(10, lease, "instance-A");
        await pipeline.WaitUntilStartedAsync();
        var claimed = await LoadOnlyAttemptAsync(fixture);
        var initialHeartbeat = claimed.LastHeartbeatAtUtc;
        var initialLease = claimed.LeaseExpiresAtUtc;

        var afterHeartbeat = await WaitForHeartbeatAsync(fixture, initialHeartbeat);
        Assert.True(afterHeartbeat.LastHeartbeatAtUtc > initialHeartbeat);
        Assert.True(afterHeartbeat.LeaseExpiresAtUtc > initialLease);

        var second = await dispatcherB.DispatchAsync(10, lease, "instance-B");
        Assert.Equal(0, second.Claimed);
        pipeline.Release(AchResponseReprocessResultCode.Completed);
        var firstResult = await first;

        Assert.Equal(1, firstResult.Claimed);
        Assert.Equal(1, firstResult.Completed);
        Assert.Equal(1, pipeline.InvocationCount);
        await using var verify = fixture.CreateContext();
        var attempt = await verify.AchResponseReprocessAttempts.SingleAsync();
        var response = await verify.AchResponses.SingleAsync();
        Assert.Equal(AchResponseReprocessAttemptStatuses.Completed, attempt.Status);
        Assert.Equal("instance-A", attempt.ClaimedBy);
        Assert.Equal(AchResponseReprocessResultCode.Completed.ToString(), attempt.ResultCode);
        Assert.Equal(AchResponseProcessingStatus.Reprocesada, response.EstadoProcesamiento);
        Assert.Equal(1, await verify.AchResponseAudits.CountAsync(x => x.Action == "ReprocessClaimed"));
        Assert.Equal(1, await verify.AchResponseAudits.CountAsync(x => x.Action == "ReprocessCompleted"));
        Assert.Equal(0, response.DuplicateReceiptCount);
        Assert.Equal(1, await verify.AchResponses.CountAsync());
        Assert.Equal(0, await verify.AchResponseNotificationAttempts.CountAsync());
    }

    private static async Task VerifyLostOwnershipAndRecoveryAsync(DatabaseFixture fixture)
    {
        await ClearResponsesAsync(fixture);
        Guid responseId;
        long attemptId;
        await using (var seed = fixture.CreateContext())
        {
            var response = NewResponse(AchResponseProcessingStatus.PendienteReproceso);
            seed.AchResponses.Add(response);
            var attempt = NewAttempt(response.Id);
            seed.AchResponseReprocessAttempts.Add(attempt);
            await seed.SaveChangesAsync();
            responseId = response.Id;
            attemptId = attempt.Id;
        }

        var blocked = new ControlledPipeline();
        await using (var services = BuildServices(fixture, blocked))
        await using (var scope = services.CreateAsyncScope())
        {
            var oldWorker = scope.ServiceProvider.GetRequiredService<IAchResponseReprocessDispatcher>();
            var oldRun = oldWorker.DispatchAsync(1, TimeSpan.FromMilliseconds(750), "instance-old");
            await blocked.WaitUntilStartedAsync();

            await using (var steal = fixture.CreateContext())
            {
                await steal.AchResponseReprocessAttempts.Where(x => x.Id == attemptId)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.ClaimedBy, "instance-current")
                        .SetProperty(x => x.Version, Guid.NewGuid())
                        .SetProperty(x => x.LeaseExpiresAtUtc, DateTime.UtcNow.AddMilliseconds(450)));
            }
            await Task.Delay(320);
            blocked.Release(AchResponseReprocessResultCode.Completed);
            var lost = await oldRun;
            Assert.Equal(0, lost.Completed);
            Assert.Equal(1, lost.Skipped);
        }

        await using (var expire = fixture.CreateContext())
        {
            await expire.AchResponseReprocessAttempts.Where(x => x.Id == attemptId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LeaseExpiresAtUtc, DateTime.UtcNow.AddMilliseconds(-10)));
        }

        var recoveryPipeline = ControlledPipeline.Immediate(AchResponseReprocessResultCode.AlreadyApplied);
        await using (var services = BuildServices(fixture, recoveryPipeline))
        await using (var scope = services.CreateAsyncScope())
        {
            var recovery = await scope.ServiceProvider.GetRequiredService<IAchResponseReprocessDispatcher>()
                .DispatchAsync(1, TimeSpan.FromSeconds(2), "instance-recovery");
            Assert.Equal(1, recovery.Claimed);
            Assert.Equal(1, recovery.Completed);
        }

        await using var verify = fixture.CreateContext();
        var finalAttempt = await verify.AchResponseReprocessAttempts.SingleAsync(x => x.Id == attemptId);
        Assert.Equal(AchResponseReprocessAttemptStatuses.Completed, finalAttempt.Status);
        Assert.Equal("instance-recovery", finalAttempt.ClaimedBy);
        Assert.Equal(AchResponseReprocessResultCode.AlreadyApplied.ToString(), finalAttempt.ResultCode);
        Assert.Equal(1, await verify.AchResponseAudits.CountAsync(x => x.AchResponseId == responseId && x.Action == "ReprocessLeaseRecovered"));
        Assert.Equal(1, await verify.AchResponseAudits.CountAsync(x => x.AchResponseId == responseId && x.Action == "ReprocessAlreadyApplied"));
    }

    private static async Task VerifyTerminalResultsAsync(DatabaseFixture fixture)
    {
        await VerifyTerminalAsync(fixture, AchResponseReprocessResultCode.MissingOperationalData,
            AchResponseReprocessAttemptStatuses.FailedFunctional, AchResponseProcessingStatus.RequiereRevisionManual);
        await VerifyTerminalAsync(fixture, AchResponseReprocessResultCode.TechnicalFailure,
            AchResponseReprocessAttemptStatuses.FailedTechnical, AchResponseProcessingStatus.ErrorTecnico);
    }

    private static async Task VerifyTerminalAsync(DatabaseFixture fixture, AchResponseReprocessResultCode resultCode,
        string expectedAttemptStatus, AchResponseProcessingStatus expectedResponseStatus)
    {
        await ClearResponsesAsync(fixture);
        await using (var seed = fixture.CreateContext())
        {
            var response = NewResponse(AchResponseProcessingStatus.PendienteReproceso);
            seed.AchResponses.Add(response);
            seed.AchResponseReprocessAttempts.Add(NewAttempt(response.Id));
            await seed.SaveChangesAsync();
        }
        var pipeline = ControlledPipeline.Immediate(resultCode);
        await using var services = BuildServices(fixture, pipeline);
        await using var scope = services.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<IAchResponseReprocessDispatcher>()
            .DispatchAsync(1, TimeSpan.FromSeconds(2), $"instance-{resultCode}");
        Assert.Equal(1, result.Claimed);
        await using var verify = fixture.CreateContext();
        Assert.Equal(expectedAttemptStatus, await verify.AchResponseReprocessAttempts.Select(x => x.Status).SingleAsync());
        Assert.Equal(expectedResponseStatus, await verify.AchResponses.Select(x => x.EstadoProcesamiento).SingleAsync());
        Assert.Equal(resultCode.ToString(), await verify.AchResponseReprocessAttempts.Select(x => x.ResultCode).SingleAsync());
    }

    private static ServiceProvider BuildServices(DatabaseFixture fixture, ControlledPipeline pipeline)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AchDbContext>(options => fixture.Configure(options), ServiceLifetime.Scoped);
        services.AddSingleton(pipeline);
        services.AddScoped<IAchResponseReprocessPipeline>(sp => sp.GetRequiredService<ControlledPipeline>());
        services.AddScoped<IAchResponseReprocessDispatcher, AchResponseReprocessDispatcher>();
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static async Task<AchResponseReprocessAttempt> LoadOnlyAttemptAsync(DatabaseFixture fixture)
    {
        await using var context = fixture.CreateContext();
        return await context.AchResponseReprocessAttempts.AsNoTracking().SingleAsync();
    }

    private static async Task<AchResponseReprocessAttempt> WaitForHeartbeatAsync(
        DatabaseFixture fixture, DateTime? initialHeartbeat)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        AchResponseReprocessAttempt current;
        do
        {
            await Task.Delay(100);
            current = await LoadOnlyAttemptAsync(fixture);
            if (current.LastHeartbeatAtUtc > initialHeartbeat) return current;
        } while (DateTime.UtcNow < deadline);
        return current;
    }

    private static async Task ClearResponsesAsync(DatabaseFixture fixture)
    {
        await using var context = fixture.CreateContext();
        await context.AchResponseAudits.ExecuteDeleteAsync();
        await context.AchResponseReprocessAttempts.ExecuteDeleteAsync();
        await context.AchResponseNotificationAttempts.ExecuteDeleteAsync();
        await context.AchResponses.ExecuteDeleteAsync();
    }

    private static AchResponse NewResponse(AchResponseProcessingStatus status) => new()
    {
        Id = Guid.NewGuid(),
        TipoRespuesta = TipoRespuestaAch.Transaccion,
        IdTransaccion = "job4-synthetic",
        CodigoCamaraCompensacion = "ACHCOL",
        CodigoEstadoExterno = "R01",
        HashIdempotencia = Guid.NewGuid().ToString("N"),
        CanonicalPayloadHash = Guid.NewGuid().ToString("N"),
        OperationalDate = DateTime.UtcNow.Date,
        CorrelationId = $"job4-{Guid.NewGuid():N}",
        FechaRecepcion = DateTime.UtcNow,
        FechaCreacion = DateTime.UtcNow,
        EstadoProcesamiento = status,
        Version = Guid.NewGuid()
    };

    private static AchResponseReprocessAttempt NewAttempt(Guid responseId) => new()
    {
        AchResponseId = responseId,
        AttemptNumber = 1,
        Status = AchResponseReprocessAttemptStatuses.Pending,
        RequestedBy = "job4-certification",
        Reason = "Certificación sintética JOB 4",
        CorrelationId = $"job4-attempt-{Guid.NewGuid():N}",
        RequestedAtUtc = DateTime.UtcNow,
        CommandId = Guid.NewGuid(),
        Version = Guid.NewGuid()
    };

    private static Task<int> CountJob41ColumnsAsync(AchDbContext context, DatabaseProvider provider)
        => ScalarAsync(context, provider == DatabaseProvider.SqlServer
            ? "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AchResponseReprocessAttempts' AND COLUMN_NAME IN ('ClaimedBy','ClaimedAtUtc','LeaseExpiresAtUtc','StartedAtUtc','LastHeartbeatAtUtc','ResultCode','ErrorType','ErrorDetailSanitized','Version')"
            : "SELECT COUNT(*) FROM information_schema.columns WHERE table_name='AchResponseReprocessAttempts' AND column_name IN ('ClaimedBy','ClaimedAtUtc','LeaseExpiresAtUtc','StartedAtUtc','LastHeartbeatAtUtc','ResultCode','ErrorType','ErrorDetailSanitized','Version')");

    private static Task<int> CountAcquisitionIndexAsync(AchDbContext context, DatabaseProvider provider)
        => ScalarAsync(context, provider == DatabaseProvider.SqlServer
            ? "SELECT COUNT(*) FROM sys.indexes WHERE name='IX_AchResponseReprocessAttempts_Status_LeaseExpiresAtUtc_RequestedAtUtc_Id'"
            : "SELECT COUNT(*) FROM pg_indexes WHERE indexname='IX_AchResponseReprocessAttempts_Status_LeaseExpiresAtUtc_RequestedAtUtc_Id'");

    private static async Task<int> ScalarAsync(AchDbContext context, string sql)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static void EnsureRequiredConfiguration(DatabaseProvider provider)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(RequiredVariable), "true", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{RequiredVariable}=true es obligatorio; la suite no admite omisiones.");
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionVariable(provider))))
            throw new InvalidOperationException($"Falta {ConnectionVariable(provider)}.");
    }

    private static string ConnectionVariable(DatabaseProvider provider)
        => provider == DatabaseProvider.SqlServer
            ? "JOB4_SQLSERVER_CONNECTION_STRING"
            : "JOB4_POSTGRES_CONNECTION_STRING";

    private enum DatabaseProvider { SqlServer, PostgreSql }

    private sealed class DatabaseFixture : IAsyncDisposable
    {
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

        public DatabaseProvider Provider { get; }

        public static async Task<DatabaseFixture> CreateAsync(DatabaseProvider provider)
        {
            var baseConnection = Environment.GetEnvironmentVariable(ConnectionVariable(provider))!;
            var databaseName = $"ach_job4_{Guid.NewGuid():N}";
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
            var targetPostgres = new NpgsqlConnectionStringBuilder(baseConnection) { Database = databaseName };
            return new DatabaseFixture(provider, databaseName, targetPostgres.ConnectionString, postgresAdmin.ConnectionString);
        }

        public void Configure(DbContextOptionsBuilder options)
        {
            if (Provider == DatabaseProvider.SqlServer)
                options.UseSqlServer(_connectionString, sql =>
                {
                    sql.MigrationsAssembly("Cfa.ACHInterbank.Persistence.Migrations.SqlServer");
                    sql.EnableRetryOnFailure();
                });
            else
                options.UseNpgsql(_connectionString, npgsql => npgsql.EnableRetryOnFailure());
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        }

        public AchDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AchDbContext>();
            Configure(options);
            return new AchDbContext(options.Options);
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
            terminate.CommandText = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname=@database AND pid<>pg_backend_pid()";
            terminate.Parameters.AddWithValue("database", _databaseName);
            await terminate.ExecuteNonQueryAsync();
            await using var drop = postgres.CreateCommand();
            drop.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\"";
            await drop.ExecuteNonQueryAsync();
        }
    }

    private sealed class ControlledPipeline : IAchResponseReprocessPipeline
    {
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<AchResponseReprocessExecutionResult> _result =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _invocationCount;

        public int InvocationCount => Volatile.Read(ref _invocationCount);

        public static ControlledPipeline Immediate(AchResponseReprocessResultCode code)
        {
            var pipeline = new ControlledPipeline();
            pipeline.Release(code);
            return pipeline;
        }

        public async Task<AchResponseReprocessExecutionResult> ExecuteAsync(Guid responseId, long attemptId,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _invocationCount);
            _started.TrySetResult();
            return await _result.Task.WaitAsync(cancellationToken);
        }

        public Task WaitUntilStartedAsync() => _started.Task.WaitAsync(TimeSpan.FromSeconds(15));

        public void Release(AchResponseReprocessResultCode code)
            => _result.TrySetResult(new AchResponseReprocessExecutionResult(code, $"controlled:{code}",
                code == AchResponseReprocessResultCode.TechnicalFailure ? "technical failure sanitized" : null));
    }
}
