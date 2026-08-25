using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Repositories.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using Moq;
using Npgsql;

namespace Cfa.ACHInterbank.Tests;

public sealed class OutboundReturnMultiDbTests
{
    private const string RequiredVariable = "RUN_OUTBOUND_RETURN_MULTIDB";
    private static readonly DateTimeOffset FixedNow = new(2026, 8, 8, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    [Trait("Category", "OutboundReturnMultiDb")]
    [Trait("Provider", "SqlServer")]
    public Task RaceMatrix_RunsAgainstSqlServer() => RunRaceMatrixAsync(DatabaseProvider.SqlServer);

    [Fact]
    [Trait("Category", "OutboundReturnMultiDb")]
    [Trait("Provider", "PostgreSql")]
    public Task RaceMatrix_RunsAgainstPostgreSql() => RunRaceMatrixAsync(DatabaseProvider.PostgreSql);

    [Fact]
    [Trait("Category", "OutboundReturnMultiDb")]
    [Trait("Provider", "SqlServer")]
    public Task MigrationUpDown_RunsAgainstSqlServer() => RunMigrationRoundTripAsync(DatabaseProvider.SqlServer);

    [Fact]
    [Trait("Category", "OutboundReturnMultiDb")]
    [Trait("Provider", "PostgreSql")]
    public Task MigrationUpDown_RunsAgainstPostgreSql() => RunMigrationRoundTripAsync(DatabaseProvider.PostgreSql);

    private static async Task RunRaceMatrixAsync(DatabaseProvider provider)
    {
        EnsureConfiguration(provider);
        await using var fixture = await DatabaseFixture.CreateAsync(provider);
        await fixture.InitializeAsync();

        // RACE A: misma transacción y misma causal; el gate solo alinea los nodos y no serializa.
        var raceA = await fixture.SeedScenarioAsync("RACE-A", new DateTime(2026, 8, 8), "81000001", 1);
        var raceABefore = await fixture.CaptureEvidenceAsync();
        var raceAResults = await RunConcurrentAsync(
            fixture,
            Request(raceA, (0, "R01")),
            Request(raceA, (0, "R01")));
        AssertWinnerLoser(raceAResults);
        await fixture.AssertScenarioAsync(raceA, expectedReturns: 1, expectedEvents: 1, expectedRegistries: 1);
        await fixture.AssertEvidenceDeltaAsync(raceABefore, generationAudits: 1, registries: 1);

        // RACE B: causales diferentes no eluden el único ReturnOut permitido por V35 6.6.1.
        var raceB = await fixture.SeedScenarioAsync("RACE-B", new DateTime(2026, 8, 9), "81000002", 1);
        var raceBBefore = await fixture.CaptureEvidenceAsync();
        var raceBResults = await RunConcurrentAsync(
            fixture,
            Request(raceB, (0, "R01")),
            Request(raceB, (0, "R02")));
        AssertWinnerLoser(raceBResults);
        await fixture.AssertScenarioAsync(raceB, expectedReturns: 1, expectedEvents: 1, expectedRegistries: 1);
        await fixture.AssertEvidenceDeltaAsync(raceBBefore, generationAudits: 1, registries: 1);

        // RACE C: dos transacciones, mismo participante y fecha; ambas ganan con trace distinto.
        var raceC = await fixture.SeedScenarioAsync("RACE-C", new DateTime(2026, 8, 10), "81000003", 2);
        var raceCBefore = await fixture.CaptureEvidenceAsync();
        var raceCResults = await RunConcurrentAsync(
            fixture,
            Request(raceC, (0, "R01")),
            Request(raceC, (1, "R02")));
        raceCResults.Should().OnlyContain(result => result.Response != null && result.Error == null);
        await fixture.AssertScenarioAsync(raceC, expectedReturns: 2, expectedEvents: 2, expectedRegistries: 2);
        await fixture.AssertEvidenceDeltaAsync(raceCBefore, generationAudits: 2, registries: 2);
        await using (var assertion = fixture.CreateContext())
        {
            var traces = await assertion.AchReturnsGenerated
                .Where(row => raceC.TransactionIds.Contains(row.OriginalTransactionId))
                .Select(row => row.NewSequenceNumber)
                .ToListAsync();
            traces.Should().OnlyHaveUniqueItems();
            traces.Should().OnlyContain(trace => trace.StartsWith(raceC.ParticipantDfi, StringComparison.Ordinal));
        }

        // RACE D: lotes solapados; un archivo completo gana y el perdedor no deja su transacción exclusiva.
        var raceD = await fixture.SeedScenarioAsync("RACE-D", new DateTime(2026, 8, 11), "81000004", 3);
        var raceDBefore = await fixture.CaptureEvidenceAsync();
        var raceDResults = await RunConcurrentAsync(
            fixture,
            Request(raceD, (0, "R01"), (1, "R02")),
            Request(raceD, (1, "R02"), (2, "R03")));
        AssertWinnerLoser(raceDResults);
        await fixture.AssertScenarioAsync(raceD, expectedReturns: 2, expectedEvents: 2, expectedRegistries: 1);
        await fixture.AssertEvidenceDeltaAsync(raceDBefore, generationAudits: 1, registries: 1);

        // RACE E: retry después del winner confirmado.
        var raceEBefore = await fixture.CaptureEvidenceAsync();
        await using (var context = fixture.CreateContext())
        {
            var service = fixture.CreateService(context, new IndependentGateLockService());
            var retry = () => service.GenerateReturnsFileAsync(Request(raceA, (0, "R01")));
            await Assert.ThrowsAsync<AchReturnAlreadyGeneratedException>(retry);
        }
        await fixture.AssertScenarioAsync(raceA, expectedReturns: 1, expectedEvents: 1, expectedRegistries: 1);
        await fixture.AssertEvidenceDeltaAsync(raceEBefore, generationAudits: 0, registries: 0);

        // RACE F: falla después del claim y antes del lifecycle.
        var raceF = await fixture.SeedScenarioAsync("RACE-F", new DateTime(2026, 8, 12), "81000005", 1);
        var raceFBefore = await fixture.CaptureEvidenceAsync();
        await using (var context = fixture.CreateContext())
        {
            var builder = new Mock<INachaFileBuilder>(MockBehavior.Strict);
            builder.Setup(x => x.BuildReturnOutAsync(It.IsAny<NachaReturnOutBuildRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("RACE_F_AFTER_DB_CLAIM"));
            var service = fixture.CreateService(context, new IndependentGateLockService(), builder: builder.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateReturnsFileAsync(Request(raceF, (0, "R01"))));
        }
        await fixture.AssertScenarioAsync(raceF, expectedReturns: 0, expectedEvents: 0, expectedRegistries: 0);
        await fixture.AssertEvidenceDeltaAsync(raceFBefore, generationAudits: 0, registries: 0);
        await fixture.AssertNoTraceCounterAsync(raceF);

        // RACE G: falla de state transition después de naming y generation audit; todo revierte.
        var raceG = await fixture.SeedScenarioAsync("RACE-G", new DateTime(2026, 8, 13), "81000006", 1);
        var raceGBefore = await fixture.CaptureEvidenceAsync();
        await using (var context = fixture.CreateContext())
        {
            var transition = new Mock<IAchStateTransitionService>(MockBehavior.Strict);
            transition.Setup(x => x.TransitionAsync(It.IsAny<AchStateTransitionRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("RACE_G_STATE_TRANSITION_FAILURE"));
            var service = fixture.CreateService(context, new IndependentGateLockService(), transition: transition.Object);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateReturnsFileAsync(Request(raceG, (0, "R01"))));
        }
        await fixture.AssertScenarioAsync(raceG, expectedReturns: 0, expectedEvents: 0, expectedRegistries: 0);
        await fixture.AssertEvidenceDeltaAsync(raceGBefore, generationAudits: 0, registries: 0);
        await fixture.AssertNoTraceCounterAsync(raceG);

        // RACE H: prenotificación retorna monto cero dentro de la misma unidad atómica.
        var raceH = await fixture.SeedScenarioAsync("RACE-H", new DateTime(2026, 8, 14), "81000007", 1, prenotification: true);
        var raceHBefore = await fixture.CaptureEvidenceAsync();
        await using (var context = fixture.CreateContext())
        {
            var service = fixture.CreateService(context, new IndependentGateLockService());
            var response = await service.GenerateReturnsFileAsync(Request(raceH, (0, "R01")));
            response.TotalReturns.Should().Be(1);
        }
        await fixture.AssertScenarioAsync(raceH, expectedReturns: 1, expectedEvents: 1, expectedRegistries: 1);
        await fixture.AssertEvidenceDeltaAsync(raceHBefore, generationAudits: 1, registries: 1);
        await using (var assertion = fixture.CreateContext())
        {
            (await assertion.AchReturnsGenerated.SingleAsync(row => row.OriginalTransactionId == raceH.TransactionIds[0]))
                .Amount.Should().Be(0m);
        }

        // RACE I: CENIT usa la misma garantía DB-first en SQL Server y PostgreSQL.
        var raceI = await fixture.SeedCenitScenarioAsync("RACE-I", new DateTime(2026, 8, 15), "81000008");
        var raceIResults = await RunConcurrentAsync(
            fixture,
            Request(raceI, (0, "R02")),
            Request(raceI, (0, "R02")));
        AssertWinnerLoser(raceIResults);
        await fixture.AssertScenarioAsync(raceI, expectedReturns: 1, expectedEvents: 1, expectedRegistries: 1);
        await fixture.AssertCenitRorPersistenceAsync(raceI);

        // RACE J: callbacks terminales CENIT concurrentes conservan un único resultado efectivo.
        var transport = await fixture.SeedCenitTransportExportAsync(raceI);
        var callbackGate = new AsyncStartGate(2);
        async Task<AchOutboundReturnResultProcessingResult> ProcessResult(
            string eventId,
            AchOutboundReturnOutcome outcome,
            string code)
        {
            await callbackGate.ArriveAsync(CancellationToken.None);
            await using var callbackContext = fixture.CreateContext();
            var processor = new AchOutboundReturnResultProcessor(
                callbackContext,
                new AchFileTransmissionEvidenceRecorder(callbackContext));
            return await processor.ProcessAsync(new AchOutboundReturnResultRequest(
                eventId,
                transport.FileName,
                transport.Reference,
                outcome,
                code,
                FixedNow.UtcDateTime));
        }
        var callbackResults = await Task.WhenAll(
            Task.Run(() => ProcessResult("CENIT-RACE-J-ACK", AchOutboundReturnOutcome.Accepted, "ACCEPTED")),
            Task.Run(() => ProcessResult("CENIT-RACE-J-REJECT", AchOutboundReturnOutcome.Rejected, "REJECTED")));
        callbackResults.Should().ContainSingle(result => result.Applied);
        callbackResults.Should().ContainSingle(result => result.RequiresManualReview && !result.Applied);
        await fixture.AssertCenitTransportConflictAsync(transport);
    }

    private static async Task RunMigrationRoundTripAsync(DatabaseProvider provider)
    {
        EnsureConfiguration(provider);
        await using var fixture = await DatabaseFixture.CreateAsync(provider);
        await fixture.InitializeAsync();

        var scenario = await fixture.SeedScenarioAsync("MIGRATION", new DateTime(2026, 8, 15), "82000001", 1);
        await using (var context = fixture.CreateContext())
        {
            var response = await fixture.CreateService(
                    context,
                    new IndependentGateLockService(),
                    builder: ReturnOutNachaFileBuilderFactory.Create())
                .GenerateReturnsFileAsync(Request(scenario, (0, "R01")));
            response.TotalReturns.Should().Be(1);
        }

        await using (var context = fixture.CreateContext())
        {
            var migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync(PreviousMigration(provider));
        }
        await fixture.AssertOldReturnIndexAsync();

        await using (var context = fixture.CreateContext())
        {
            await context.Database.MigrateAsync();
            (await context.AchReturnsGenerated.CountAsync(row => row.OriginalTransactionId == scenario.TransactionIds[0]))
                .Should().Be(1);
            var counter = await context.AchReturnTraceSequences.SingleAsync(row =>
                row.ParticipantDfi == scenario.ParticipantDfi && row.SequenceDate == DateOnly.FromDateTime(scenario.ProcessingDate));
            counter.LastAssignedValue.Should().Be(1);
        }
        await fixture.AssertNewReturnIndexesAsync();

        // La migración debe fallar cerrado y conservar evidencia si el índice histórico
        // permitía dos devoluciones ordinarias para la misma transacción.
        await using var incompatibleFixture = await DatabaseFixture.CreateAsync(provider);
        await incompatibleFixture.InitializeAsync();
        var incompatibleScenario = await incompatibleFixture.SeedScenarioAsync(
            "MIGRATION-DUPLICATE",
            new DateTime(2026, 8, 16),
            "82000002",
            1);
        await using (var context = incompatibleFixture.CreateContext())
        {
            await incompatibleFixture.CreateService(
                    context,
                    new IndependentGateLockService(),
                    builder: ReturnOutNachaFileBuilderFactory.Create())
                .GenerateReturnsFileAsync(Request(incompatibleScenario, (0, "R01")));
            await context.GetService<IMigrator>().MigrateAsync(PreviousMigration(provider));
        }
        await incompatibleFixture.InsertHistoricalDuplicateReturnAsync(incompatibleScenario.TransactionIds[0]);
        await using (var context = incompatibleFixture.CreateContext())
        {
            await Assert.ThrowsAnyAsync<Exception>(() => context.Database.MigrateAsync());
        }
        await incompatibleFixture.AssertHistoricalReturnCountAsync(incompatibleScenario.TransactionIds[0], 2);
        await incompatibleFixture.AssertOldReturnIndexAsync();
    }

    private static GenerateReturnsFileRequest Request(Scenario scenario, params (int Index, string Reason)[] items)
        => new(
            scenario.CycleId,
            items.Select(item => new ReturnSelectionItemDto(scenario.TransactionIds[item.Index], item.Reason)).ToArray(),
            scenario.ReturnCycleId);

    private static async Task<GenerationAttempt[]> RunConcurrentAsync(
        DatabaseFixture fixture,
        GenerateReturnsFileRequest requestA,
        GenerateReturnsFileRequest requestB)
    {
        var gate = new AsyncStartGate(2);
        async Task<GenerationAttempt> Execute(GenerateReturnsFileRequest request)
        {
            await using var context = fixture.CreateContext();
            var service = fixture.CreateService(context, new IndependentGateLockService(gate));
            try
            {
                return new GenerationAttempt(await service.GenerateReturnsFileAsync(request), null);
            }
            catch (Exception ex)
            {
                return new GenerationAttempt(null, ex);
            }
        }

        return await Task.WhenAll(Task.Run(() => Execute(requestA)), Task.Run(() => Execute(requestB)));
    }

    private static void AssertWinnerLoser(IReadOnlyCollection<GenerationAttempt> results)
    {
        var diagnostic = string.Join(" | ", results.Select(result => result.Error is null
            ? "SUCCESS"
            : ExceptionDiagnostic(result.Error)));
        results.Count(result => result.Response is not null && result.Error is null).Should().Be(1, diagnostic);
        results.Count(result => result.Response is null && result.Error is AchReturnAlreadyGeneratedException).Should().Be(1, diagnostic);
    }

    private static string ExceptionDiagnostic(Exception exception)
    {
        var messages = new List<string>();
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            messages.Add($"{current.GetType().Name}: {current.Message}");
        }
        return string.Join(" -> ", messages);
    }

    private static void EnsureConfiguration(DatabaseProvider provider)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(RequiredVariable), "true", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{RequiredVariable}=true es obligatorio; OutboundReturnMultiDb no admite omisiones.");
        }

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionVariable(provider))))
        {
            throw new InvalidOperationException($"Falta {ConnectionVariable(provider)}.");
        }
    }

    private static string ConnectionVariable(DatabaseProvider provider) => provider == DatabaseProvider.SqlServer
        ? "OUTBOUND_RETURN_SQLSERVER_CONNECTION_STRING"
        : "OUTBOUND_RETURN_POSTGRES_CONNECTION_STRING";

    private static string PreviousMigration(DatabaseProvider provider) => provider == DatabaseProvider.SqlServer
        ? "20260806203451_SchedulerEnterpriseTasks"
        : "20260806203546_SchedulerEnterpriseTasks";

    private enum DatabaseProvider { SqlServer, PostgreSql }

    private sealed record Scenario(
        string CycleId,
        DateTime ProcessingDate,
        string ParticipantDfi,
        int[] TransactionIds,
        string? ReturnCycleId = null);
    private sealed record GenerationAttempt(GenerateReturnsFileResponse? Response, Exception? Error);
    private sealed record Evidence(int GenerationAudits, int Registries);
    private sealed record TransportExport(int Id, string FileName, string Reference);

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => FixedNow;
    }

    private sealed class AlwaysEligibleService : IAchReturnEligibilityService
    {
        public Task<AchReturnEligibilityResult> EvaluateOutgoingReturnAsync(AchReturnEligibilityRequest request, CancellationToken ct = default)
            => Task.FromResult(new AchReturnEligibilityResult(true, request.ReturnReasonCode.Trim().ToUpperInvariant(), 1, "Debit", "Pending", []));

        public Task<AchReturnEligibilityResult> EvaluateIncomingReturnAsync(AchReturnEligibilityRequest request, CancellationToken ct = default)
            => EvaluateOutgoingReturnAsync(request, ct);
    }

    private sealed class IndependentGateLockService(AsyncStartGate? gate = null) : IAchReturnGenerationLockService
    {
        public async Task<IAsyncDisposable> AcquireAsync(IReadOnlyCollection<int> transactionIds, CancellationToken cancellationToken)
        {
            if (gate is not null)
            {
                await gate.ArriveAsync(cancellationToken);
            }
            return NoopAsyncDisposable.Instance;
        }
    }

    private sealed class AsyncStartGate(int participants)
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _remaining = participants;

        public async Task ArriveAsync(CancellationToken ct)
        {
            if (Interlocked.Decrement(ref _remaining) == 0)
            {
                _release.TrySetResult();
            }
            await _release.Task.WaitAsync(ct);
        }
    }

    private sealed class NoopAsyncDisposable : IAsyncDisposable
    {
        public static readonly NoopAsyncDisposable Instance = new();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class DatabaseFixture : IAsyncDisposable
    {
        private readonly string _databaseName;
        private readonly string _connectionString;
        private readonly string _adminConnectionString;
        private int _clearingHouseId;
        private int _cenitClearingHouseId;
        private int _sourceInstitutionId;
        private int _destinationInstitutionId;
        private int _companyEntryDescriptionId;

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
            var databaseName = $"ach_returnout_{Guid.NewGuid():N}";
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

        public AchDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AchDbContext>();
            if (Provider == DatabaseProvider.SqlServer)
            {
                options.UseSqlServer(_connectionString, sql => sql.MigrationsAssembly("Cfa.ACHInterbank.Persistence.Migrations.SqlServer"));
            }
            else
            {
                options.UseNpgsql(_connectionString);
            }
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            return new AchDbContext(options.Options, timeProvider: new FixedTimeProvider());
        }

        public async Task InitializeAsync()
        {
            await using var context = CreateContext();
            await context.Database.MigrateAsync();
            await new ClearingHouseConfigSeeder(context).SeedAsync();
            await EnsureOperationalCatalogAsync(context);
            await new NachaConfigOfficialProfilesSeeder(context).SeedAsync();
            await new NachaFileNamingRuleSeeder(context).SeedAsync();
        }

        public AchReturnsService CreateService(
            AchDbContext context,
            IAchReturnGenerationLockService generationLock,
            INachaFileBuilder? builder = null,
            IAchStateTransitionService? transition = null)
            => new(
                context,
                new FixedTimeProvider(),
                Mock.Of<IAchRegulatoryCatalogService>(),
                new AlwaysEligibleService(),
                generationLock,
                externalFileNamePolicy: BuildExternalFileNamePolicy(context),
                stateTransitionService: transition ?? new AchStateTransitionService(context),
                nachaFileBuilder: builder ?? BuildOptionCBuilder(context),
                returnTraceSequenceService: new AchReturnTraceSequenceService(context),
                cenitReturnPolicy: new CenitIncomingReturnPolicy());

        public async Task<Scenario> SeedScenarioAsync(
            string suffix,
            DateTime processingDate,
            string participantDfi,
            int count,
            bool prenotification = false)
        {
            await using var context = CreateContext();
            var cycleId = $"OUT-RET-{suffix}";
            var cycle = new AchCycle
            {
                Id = cycleId,
                CycleName = cycleId,
                ProcessingDate = DateTime.SpecifyKind(processingDate, DateTimeKind.Utc),
                StartTime = TimeSpan.FromHours(8),
                EndTime = TimeSpan.FromHours(17),
                CutoffTime = TimeSpan.FromHours(16),
                ClearingHouseId = _clearingHouseId
            };
            context.AchCycles.Add(cycle);
            await context.SaveChangesAsync();

            var batch = new AchBatch
            {
                AchCycleId = cycleId,
                ServiceClassCode = "225",
                CompanyName = "ORIGINADOR SINT",
                CompanyIdentification = "900000001",
                CompanyEntryDescription = "PAGOS",
                CompanyEntryDescriptionId = _companyEntryDescriptionId,
                OriginOrOdfi = participantDfi,
                EffectiveEntryDate = processingDate,
                BatchSequenceNumber = 1
            };
            context.AchBatches.Add(batch);
            await context.SaveChangesAsync();

            var rows = Enumerable.Range(1, count).Select(index => new AchTransaction
            {
                Amount = prenotification ? 0m : 100m + index,
                TransactionExternalId = $"OUT-RET-{suffix}-{index}",
                Reference = $"REF-{suffix}-{index}",
                Type = TransactionTypeEnum.Debit,
                TransactionCode = prenotification ? "28" : "27",
                ServiceClassCode = "225",
                CompanyEntryDescriptionId = _companyEntryDescriptionId,
                CompanyName = "ORIGINADOR SINT",
                CompanyIdentification = "900000001",
                OriginatingDFI = "91000001",
                ReceivingDFI = participantDfi,
                TraceNumber = $"91000001{index:0000000}",
                TraceSequenceNumber = index,
                EffectiveEntryDate = processingDate,
                AddendaRecordIndicator = true,
                IsPrenotification = prenotification,
                State = AchTransferStateEnum.Pending,
                StateChangedAtUtc = processingDate,
                SourceAccountNumber = "0000001111",
                DestinationAccountNumber = "0000002222",
                RecipientIdNumber = "100000001",
                DiscretionaryData = string.Empty,
                SourceInstitutionId = _sourceInstitutionId,
                DestinationInstitutionId = _destinationInstitutionId,
                AchCycleId = cycleId,
                AchBatchId = batch.Id
            }).ToArray();
            context.AchTransactions.AddRange(rows);
            await context.SaveChangesAsync();
            return new Scenario(cycleId, processingDate.Date, participantDfi, rows.Select(row => row.Id).ToArray());
        }

        public async Task<Scenario> SeedCenitScenarioAsync(
            string suffix,
            DateTime processingDate,
            string participantDfi)
        {
            await using var context = CreateContext();
            var originalCycleId = $"CENIT-{suffix}-C1";
            var returnCycleId = $"CENIT-{suffix}-C2";
            for (var number = 1; number <= 4; number++)
            {
                context.AchCycles.Add(new AchCycle
                {
                    Id = $"CENIT-{suffix}-C{number}",
                    CycleName = $"Ciclo {number}",
                    ProcessingDate = DateTime.SpecifyKind(processingDate, DateTimeKind.Utc),
                    StartTime = TimeSpan.FromHours(7 + number),
                    EndTime = TimeSpan.FromHours(8 + number),
                    CutoffTime = TimeSpan.FromHours(8 + number),
                    ClearingHouseId = _cenitClearingHouseId
                });
            }
            await context.SaveChangesAsync();

            var batch = new AchBatch
            {
                AchCycleId = originalCycleId,
                ServiceClassCode = "225",
                CompanyName = "ORIGINADOR CENIT",
                CompanyIdentification = "900000001",
                CompanyEntryDescription = "PAGOS",
                CompanyEntryDescriptionId = _companyEntryDescriptionId,
                OriginOrOdfi = "91000001",
                EffectiveEntryDate = processingDate,
                BatchSequenceNumber = 1
            };
            context.AchBatches.Add(batch);
            await context.SaveChangesAsync();

            var transaction = new AchTransaction
            {
                Amount = 100m,
                TransactionExternalId = $"CENIT-{suffix}-1",
                Reference = $"REF-CENIT-{suffix}-1",
                Type = TransactionTypeEnum.Debit,
                TransactionCode = "27",
                ServiceClassCode = "225",
                CompanyEntryDescriptionId = _companyEntryDescriptionId,
                CompanyName = "ORIGINADOR CENIT",
                CompanyIdentification = "900000001",
                OriginatingDFI = "91000001",
                ReceivingDFI = participantDfi,
                TraceNumber = "910000010000001",
                TraceSequenceNumber = 1,
                EffectiveEntryDate = processingDate,
                AddendaRecordIndicator = true,
                State = AchTransferStateEnum.Pending,
                StateChangedAtUtc = processingDate,
                SourceAccountNumber = "0000001111",
                DestinationAccountNumber = "0000002222",
                RecipientIdNumber = "100000001",
                DiscretionaryData = string.Empty,
                SourceInstitutionId = _sourceInstitutionId,
                DestinationInstitutionId = _destinationInstitutionId,
                AchCycleId = originalCycleId,
                AchBatchId = batch.Id
            };
            context.AchTransactions.Add(transaction);
            await context.SaveChangesAsync();

            var ingestionId = Guid.NewGuid();
            var headerId = $"CENIT-{suffix}-{Guid.NewGuid():N}";
            var ingestion = new IncomingNachaFileIngestion
            {
                Id = ingestionId,
                FileName = "1234567.001.20260815.1",
                FileHashSha256 = new string('A', 64),
                UploadedBy = "multidb-test",
                CorrelationId = $"cenit-{suffix}"
            };
            var header = new NachaHeader
            {
                NachaID = headerId,
                ImmediateDestination = "0987654321",
                ImmediateOrigin = "0123456789",
                ImmediateDestinationName = "CFA",
                ImmediateOriginName = "CENIT",
                IncomingNachaFileIngestionId = ingestionId,
                ClearingHouseId = _cenitClearingHouseId,
                AchCycleId = returnCycleId
            };
            var rawBatch = new BatchHeader
            {
                NachaID = headerId,
                BatchNumber = 1,
                StandardEntryClassCode = "PPD",
                ServiceClassCode = "225"
            };
            var entry = new EntryDetail
            {
                NachaID = headerId,
                BatchHeader = rawBatch,
                BatchNumber = 1,
                TransactionCode = "27",
                SequenceNumber = transaction.TraceNumber,
                ReceivingParticipantEntityCode = participantDfi,
                AccountNumber = transaction.DestinationAccountNumber,
                Amount = transaction.Amount,
                NachaHeader = header
            };
            var addenda = new AddendaRecord
            {
                NachaID = headerId,
                EntryDetail = entry,
                CodeTypeAddendumRecord = "99",
                ReturnReasonCode = "R02",
                OriginalTraceNumber = transaction.TraceNumber,
                IdUserOrig = participantDfi,
                NewTraceNumber = $"{participantDfi}0000002"
            };
            context.IncomingNachaFileIngestions.Add(ingestion);
            context.NachaHeaders.Add(header);
            context.BatchHeaders.Add(rawBatch);
            context.EntryDetails.Add(entry);
            context.AddendaRecords.Add(addenda);
            context.IncomingNachaTransactionLinks.Add(new IncomingNachaTransactionLink
            {
                IncomingNachaFileIngestionId = ingestionId,
                EntryDetail = entry,
                AddendaRecord = addenda,
                AchTransactionId = transaction.Id,
                LinkType = IncomingNachaLinkType.ExactTrace15,
                ConfidenceScore = 1m,
                LinkedBy = "multidb-test",
                IsFinal = true
            });
            await context.SaveChangesAsync();

            return new Scenario(originalCycleId, processingDate.Date, participantDfi, [transaction.Id], returnCycleId);
        }

        public async Task<TransportExport> SeedCenitTransportExportAsync(Scenario scenario)
        {
            await using var context = CreateContext();
            var export = new AchFileExport
            {
                AchCycleId = scenario.ReturnCycleId!,
                ClearingHouseId = _cenitClearingHouseId,
                ExportKind = "RETURN",
                FileName = $"CENIT-{Provider}-RACE-J.ENV",
                TotalRecords = 10,
                TotalTransactions = 1,
                IsEncrypted = true,
                GeneratedAtUtc = FixedNow.UtcDateTime.AddMinutes(-2),
                LifecycleStatus = AchFileExportLifecycleStatus.Transmitted,
                TransmissionReference = $"CFA-MFT-HANDOFF:CENIT-{Provider}-RACE-J",
                TransmittedAtUtc = FixedNow.UtcDateTime.AddMinutes(-1)
            };
            context.AchFileExports.Add(export);
            await context.SaveChangesAsync();
            return new TransportExport(export.Id, export.FileName, export.TransmissionReference);
        }

        public async Task AssertCenitTransportConflictAsync(TransportExport transport)
        {
            await using var context = CreateContext();
            var export = await context.AchFileExports.AsNoTracking().SingleAsync(item => item.Id == transport.Id);
            export.LifecycleStatus.Should().BeOneOf(AchFileExportLifecycleStatus.Accepted, AchFileExportLifecycleStatus.Rejected);
            (await context.AchFileTransportResults.CountAsync(item => item.AchFileExportId == transport.Id)).Should().Be(2);
            (await context.AchFileTransportResults.CountAsync(item => item.AchFileExportId == transport.Id && item.Applied)).Should().Be(1);
            (await context.AchFileTransportResults.CountAsync(item => item.AchFileExportId == transport.Id && item.RequiresManualReview)).Should().Be(1);
        }

        public async Task AssertCenitRorPersistenceAsync(Scenario scenario)
        {
            var originalId = scenario.TransactionIds.Single();
            var targetCycleId = scenario.CycleId.EndsWith("-C1", StringComparison.Ordinal)
                ? $"{scenario.CycleId[..^1]}4"
                : throw new InvalidOperationException("CENIT_ROR_TEST_TARGET_CYCLE_UNRESOLVED");
            int parentGeneratedId;
            long parentEventId;
            await using (var context = CreateContext())
            {
                parentGeneratedId = await context.AchReturnsGenerated.Where(x => x.OriginalTransactionId == originalId).Select(x => x.Id).SingleAsync();
                parentEventId = await context.AchTransactionStateEvents.Where(x => x.AchTransactionId == originalId).Select(x => x.Id).SingleAsync();
            }
            var when = DateTime.SpecifyKind(scenario.ProcessingDate.AddHours(14), DateTimeKind.Utc);

            async Task<CenitReturnOfReturnResult> Outbound(DateTime requestedAt)
            {
                await using var context = CreateContext();
                return await CreateRorService(context).CreateOutgoingAsync(new(parentEventId, "R60", targetCycleId, requestedAt));
            }
            var outbound = await Task.WhenAll(Outbound(when), Outbound(when.AddMinutes(1)));
            outbound.Should().ContainSingle(x => x.IsSuccessful && !x.WasDuplicate);
            outbound.Should().ContainSingle(x => x.WasDuplicate);

            CenitReturnOfReturnInRequest inboundRequest;
            await using (var context = CreateContext())
            {
                var parent = await context.AchReturnsGenerated.AsNoTracking().SingleAsync(x => x.Id == parentGeneratedId);
                inboundRequest = new(
                    parent.Id,
                    originalId,
                    targetCycleId,
                    "R61",
                    "21",
                    $"{scenario.ParticipantDfi}6000001",
                    parent.OriginalSequenceNumber,
                    parent.OriginatorEntityCode,
                    parent.NewSequenceNumber,
                    parent.SequenceDate.DayOfYear.ToString("D3"),
                    parent.ReturnReasonCode.TrimStart('R'),
                    parent.Amount,
                    when.AddMinutes(2),
                    $"multidb-ror:{Provider}:{originalId}");
            }
            async Task<CenitReturnOfReturnResult> Inbound(DateTime receivedAt)
            {
                await using var context = CreateContext();
                return await CreateRorService(context).IngestIncomingAsync(inboundRequest with { ReceivedAtUtc = receivedAt });
            }
            var inbound = await Task.WhenAll(Inbound(when.AddMinutes(2)), Inbound(when.AddMinutes(3)));
            inbound.Should().ContainSingle(x => x.IsSuccessful && !x.WasDuplicate);
            inbound.Should().ContainSingle(x => x.WasDuplicate);

            await using (var context = CreateContext())
            {
                var flows = await context.ReturnOfReturnFlows.AsNoTracking().Where(x => x.OriginalTransactionId == originalId).ToListAsync();
                flows.Should().HaveCount(2);
                var outgoingFlow = flows.Should().ContainSingle(x => x.Direction == "Out" && x.ParentIncomingReturnStateEventId == parentEventId).Subject;
                flows.Should().ContainSingle(x => x.Direction == "In" && x.ParentOutgoingReturnGeneratedId == parentGeneratedId);
                var ror = await context.AchTransactions.AsNoTracking().SingleAsync(x => x.Id == outgoingFlow.ReturnOfReturnTransactionId);
                ror.TraceNumber.Should().Be("910000010000002");
                ror.TraceSequenceNumber.Should().Be(2);
                ror.EffectiveEntryDate.Should().Be(scenario.ProcessingDate);
                ror.OriginalTraceRef.Should().Be("910000010000001");
                var transactionCounter = await context.AchTransactionTraceSequences.AsNoTracking().SingleAsync(x =>
                    x.OriginatingDfi == "91000001" && x.SequenceDate == DateOnly.FromDateTime(ror.EffectiveEntryDate));
                transactionCounter.LastAssignedValue.Should().Be(2);
            }

            CenitReturnOfReturnService CreateRorService(AchDbContext context)
            {
                var regulatory = new Mock<IAchRegulatoryCatalogService>();
                regulatory.Setup(x => x.ValidateReturnOfReturnAsync(
                        It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                        It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((true, null, true));
                return new CenitReturnOfReturnService(
                    context,
                    regulatory.Object,
                    new AchTransactionRepository(context),
                    new AchReturnGenerationLockService(),
                    new OperationalCalendarService(context));
            }
        }

        public async Task<Evidence> CaptureEvidenceAsync()
        {
            await using var context = CreateContext();
            return new Evidence(
                await context.HistConfigChanges.CountAsync(row => row.EntityName == "NachaFileBuilder" && row.ChangeType == "GENERATION_TRACE"),
                await context.ExternalFileNameRegistry.CountAsync());
        }

        public async Task AssertEvidenceDeltaAsync(Evidence before, int generationAudits, int registries)
        {
            var after = await CaptureEvidenceAsync();
            (after.GenerationAudits - before.GenerationAudits).Should().Be(generationAudits);
            (after.Registries - before.Registries).Should().Be(registries);
        }

        public async Task AssertScenarioAsync(Scenario scenario, int expectedReturns, int expectedEvents, int expectedRegistries)
        {
            await using var context = CreateContext();
            var generated = await context.AchReturnsGenerated
                .Where(row => scenario.TransactionIds.Contains(row.OriginalTransactionId))
                .ToListAsync();
            generated.Should().HaveCount(expectedReturns);
            generated.Select(row => row.OriginalTransactionId).Should().OnlyHaveUniqueItems();
            generated.Select(row => row.NewSequenceNumber).Should().OnlyHaveUniqueItems();
            if (generated.Count > 0)
            {
                generated.Should().OnlyContain(row => row.SequenceDate == DateOnly.FromDateTime(scenario.ProcessingDate));
            }
            (await context.AchTransactionStateEvents.CountAsync(row => scenario.TransactionIds.Contains(row.AchTransactionId)))
                .Should().Be(expectedEvents);
            (await context.ExternalFileNameRegistry.CountAsync(row => row.CycleId == (scenario.ReturnCycleId ?? scenario.CycleId)))
                .Should().Be(expectedRegistries);
            var states = await context.AchTransactions
                .Where(row => scenario.TransactionIds.Contains(row.Id))
                .Select(row => row.State)
                .ToListAsync();
            states.Count(state => state == AchTransferStateEnum.ReturnedByEpr).Should().Be(expectedReturns);
            states.Count(state => state == AchTransferStateEnum.Pending).Should().Be(scenario.TransactionIds.Length - expectedReturns);
        }

        public async Task AssertNoTraceCounterAsync(Scenario scenario)
        {
            await using var context = CreateContext();
            (await context.AchReturnTraceSequences.AnyAsync(row =>
                row.ParticipantDfi == scenario.ParticipantDfi
                && row.SequenceDate == DateOnly.FromDateTime(scenario.ProcessingDate))).Should().BeFalse();
        }

        public async Task AssertOldReturnIndexAsync()
        {
            var indexes = await LoadReturnIndexNamesAsync();
            indexes.Should().Contain("UX_AchReturnGenerated_OriginalTransaction_Reason_Cycle");
            indexes.Should().NotContain("UX_AchReturnGenerated_OriginalTransaction");
            indexes.Should().NotContain("UX_AchReturnGenerated_SequenceDate_Trace");
        }

        public async Task AssertNewReturnIndexesAsync()
        {
            var indexes = await LoadReturnIndexNamesAsync();
            indexes.Should().Contain("UX_AchReturnGenerated_OriginalTransaction");
            indexes.Should().Contain("UX_AchReturnGenerated_SequenceDate_Trace");
            indexes.Should().NotContain("UX_AchReturnGenerated_OriginalTransaction_Reason_Cycle");
        }

        private async Task<HashSet<string>> LoadReturnIndexNamesAsync()
        {
            await using var connection = Provider == DatabaseProvider.SqlServer
                ? new SqlConnection(_connectionString)
                : new NpgsqlConnection(_connectionString) as System.Data.Common.DbConnection;
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = Provider == DatabaseProvider.SqlServer
                ? "SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.AchReturnsGenerated') AND name IS NOT NULL"
                : "SELECT indexname FROM pg_indexes WHERE schemaname = current_schema() AND tablename = 'AchReturnsGenerated'";
            var result = new HashSet<string>(StringComparer.Ordinal);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) result.Add(reader.GetString(0));
            return result;
        }

        public async Task InsertHistoricalDuplicateReturnAsync(int transactionId)
        {
            await using var connection = Provider == DatabaseProvider.SqlServer
                ? new SqlConnection(_connectionString)
                : new NpgsqlConnection(_connectionString) as System.Data.Common.DbConnection;
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = Provider == DatabaseProvider.SqlServer
                ? """
                    INSERT INTO dbo.AchReturnsGenerated
                        (OriginalTransactionId, ReturnCycleId, ReturnReasonCode, Amount,
                         NewSequenceNumber, OriginalSequenceNumber, ReceiverEntityCode,
                         OriginatorEntityCode, FileName, GeneratedAtUtc)
                    SELECT OriginalTransactionId, ReturnCycleId, 'R02', Amount,
                           LEFT(NewSequenceNumber, 8) + '6999999', OriginalSequenceNumber,
                           ReceiverEntityCode, OriginatorEntityCode, FileName, GeneratedAtUtc
                    FROM dbo.AchReturnsGenerated
                    WHERE OriginalTransactionId = @transactionId;
                    """
                : """
                    INSERT INTO "AchReturnsGenerated"
                        ("OriginalTransactionId", "ReturnCycleId", "ReturnReasonCode", "Amount",
                         "NewSequenceNumber", "OriginalSequenceNumber", "ReceiverEntityCode",
                         "OriginatorEntityCode", "FileName", "GeneratedAtUtc")
                    SELECT "OriginalTransactionId", "ReturnCycleId", 'R02', "Amount",
                           LEFT("NewSequenceNumber", 8) || '6999999', "OriginalSequenceNumber",
                           "ReceiverEntityCode", "OriginatorEntityCode", "FileName", "GeneratedAtUtc"
                    FROM "AchReturnsGenerated"
                    WHERE "OriginalTransactionId" = @transactionId;
                    """;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@transactionId";
            parameter.Value = transactionId;
            command.Parameters.Add(parameter);
            (await command.ExecuteNonQueryAsync()).Should().Be(1);
        }

        public async Task AssertHistoricalReturnCountAsync(int transactionId, int expected)
        {
            await using var connection = Provider == DatabaseProvider.SqlServer
                ? new SqlConnection(_connectionString)
                : new NpgsqlConnection(_connectionString) as System.Data.Common.DbConnection;
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = Provider == DatabaseProvider.SqlServer
                ? "SELECT COUNT(*) FROM dbo.AchReturnsGenerated WHERE OriginalTransactionId = @transactionId"
                : "SELECT COUNT(*) FROM \"AchReturnsGenerated\" WHERE \"OriginalTransactionId\" = @transactionId";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@transactionId";
            parameter.Value = transactionId;
            command.Parameters.Add(parameter);
            Convert.ToInt32(await command.ExecuteScalarAsync()).Should().Be(expected);
        }

        private async Task EnsureOperationalCatalogAsync(AchDbContext context)
        {
            var config = await context.ClearingHouseConfigs.OrderBy(row => row.Id).FirstAsync();
            var ach = await context.ClearingHouses.SingleOrDefaultAsync(row => row.Code == "ACH");
            if (ach is null)
            {
                ach = new ClearingHouse
                {
                    Name = "ACH Colombia",
                    Code = "ACH",
                    OriginCode = "000101006",
                    ClearingHouseId = config.Id,
                    IsActive = true
                };
                context.ClearingHouses.Add(ach);
            }
            var cenit = await context.ClearingHouses.SingleOrDefaultAsync(row => row.Code == "CENIT");
            if (cenit is null)
            {
                cenit = new ClearingHouse
                {
                    Name = "CENIT",
                    Code = "CENIT",
                    OriginCode = "000128300",
                    ClearingHouseId = config.Id,
                    IsActive = true
                };
                context.ClearingHouses.Add(cenit);
            }

            var source = await context.FinancialInstitutions.SingleOrDefaultAsync(row => row.IsDefaultSource);
            if (source is null)
            {
                source = Institution("CFA fuente sintética", true, "10001", "101");
                context.FinancialInstitutions.Add(source);
            }
            var destination = await context.FinancialInstitutions.FirstOrDefaultAsync(row => !row.IsDefaultSource);
            if (destination is null)
            {
                destination = Institution("Destino sintético", false, "10002", "102");
                context.FinancialInstitutions.Add(destination);
            }
            await context.SaveChangesAsync();

            _clearingHouseId = ach.Id;
            _cenitClearingHouseId = cenit.Id;
            _sourceInstitutionId = source.Id;
            _destinationInstitutionId = destination.Id;
            _companyEntryDescriptionId = await context.CompanyEntryDescriptionCatalogs.Select(row => row.Id).FirstAsync();
        }

        private IExternalFileNamePolicy BuildExternalFileNamePolicy(AchDbContext context)
        {
            var resolver = new ExternalFileNameSequenceProviderResolver(
            [
                new SqlServerExternalFileNameSequenceService(context),
                new PostgresExternalFileNameSequenceService(context),
                new EfGenericExternalFileNameSequenceService(context)
            ]);
            var sequence = new ExternalFileNameSequenceService(context, resolver);
            var identifier = new NachaFileIdentifierMapService(context);
            var duplicate = new ExternalFileDuplicateGuard(context);
            var correlation = new ExternalFileNameCorrelationService(identifier);
            var builder = new ExternalFileNameBuilder(sequence, identifier, new NachaFileNamingRuleService(context));
            return new ExternalFileNamePolicy(
                builder,
                new ExternalFileNameValidator(duplicate, correlation, identifier),
                correlation,
                new ExternalFileNameAuditService(context),
                duplicate);
        }

        private static INachaFileBuilder BuildOptionCBuilder(AchDbContext context)
        {
            return new NachaFileBuilder(
                context,
                Mock.Of<IBankHoliday>(),
                Mock.Of<INachaDataLoader>(),
                Mock.Of<INachaTransactionValidationService>(),
                Mock.Of<INachaFixedWidthRecordRenderer>(),
                Mock.Of<INachaRecordDataProvider>(),
                Mock.Of<INachaSemanticValidator>(),
                configResolver: new NachaConfigResolver(context),
                generationOptions: Options.Create(new NachaGenerationOptions
                {
                    Mode = "TABLE_DRIVEN",
                    ExecutionScope = "DEVELOPMENT"
                }));
        }

        private static FinancialInstitution Institution(string name, bool source, string routing, string transit)
        {
            var institution = new FinancialInstitution
            {
                Name = name,
                IsDefaultSource = source,
                RoutingNumber = routing,
                TransitCode = transit,
                Status = FinancialInstitutionStatus.Active
            };
            institution.CalculateCheckDigit();
            return institution;
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
}
