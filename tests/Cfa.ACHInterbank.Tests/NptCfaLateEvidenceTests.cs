using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

/// <summary>
/// NPT-CFA-LATE-01: integration-level relational SQLite tests with an actual
/// persisted execution and its unique (EntryDetailId, AttemptNumber) index.
/// SQL Server and PostgreSQL certification is still required separately.
/// </summary>
public sealed class NptCfaLateEvidenceTests
{
    private const string LateXml = "<Envelope><Body>late-bank-response</Body></Envelope>";

    [Fact]
    public async Task PersistedAttempt_RejectsDuplicateExecution_ButLateResultUsesExistingEventTable()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using (var collision = fixture.Open())
        {
            var attempt = await collision.IncomingNachaIntegrationExecution.AsNoTracking().SingleAsync();
            collision.IncomingNachaIntegrationExecution.Add(new IncomingNachaIntegrationExecution
            {
                DispatchQueueId = attempt.DispatchQueueId,
                EntryDetailId = attempt.EntryDetailId,
                AttemptNumber = attempt.AttemptNumber,
                ClearingHouseId = attempt.ClearingHouseId,
                CorrelationId = "duplicate-must-fail", ExecutionMode = "Live",
                RequestHash = "duplicate", StartedAtUtc = DateTime.UtcNow
            });
            await Assert.ThrowsAsync<DbUpdateException>(() => collision.SaveChangesAsync());
        }

        await WriteLateAsync(fixture);
        await using var verify = fixture.Open();
        Assert.Equal(1, await verify.IncomingNachaIntegrationExecution.CountAsync());
        var archived = await verify.IncomingNachaProcessingEvents.SingleAsync();
        using var json = JsonDocument.Parse(archived.EvidenceJson);
        Assert.Equal(LateXml, json.RootElement.GetProperty("responsePayloadXml").GetString());
        Assert.Equal("CFA-LATE-CORR", json.RootElement.GetProperty("correlationId").GetString());
        Assert.Equal("ACCEPTED-LATE", json.RootElement.GetProperty("responseCode").GetString());
        Assert.Equal("ORIGINAL_REQUEST", json.RootElement.GetProperty("requestPayloadXml").GetString());
        Assert.Equal(1, json.RootElement.GetProperty("attemptNumber").GetInt32());
        Assert.Equal("RevisionManual", archived.EventStatus);
        await AssertQuarantineAndOriginalAttemptAsync(verify);
    }

    [Fact]
    public async Task EarlierSoapEvidence_IsNeverOverwrittenByAConflictingLateResponse()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using (var setup = fixture.Open())
        {
            var original = await setup.IncomingNachaIntegrationExecution.SingleAsync();
            original.ResponsePayloadXml = "<Envelope>original-evidence</Envelope>";
            original.ResponseCode = "ORIGINAL_CODE";
            await setup.SaveChangesAsync();
        }
        await WriteLateAsync(fixture);
        await using var verify = fixture.Open();
        var persisted = await verify.IncomingNachaIntegrationExecution.AsNoTracking().SingleAsync();
        Assert.Equal("<Envelope>original-evidence</Envelope>", persisted.ResponsePayloadXml);
        Assert.Equal("ORIGINAL_CODE", persisted.ResponseCode);
        var archived = await verify.IncomingNachaProcessingEvents.SingleAsync();
        using var json = JsonDocument.Parse(archived.EvidenceJson);
        Assert.Equal(LateXml, json.RootElement.GetProperty("responsePayloadXml").GetString());
        Assert.Equal(1, await verify.IncomingNachaIntegrationExecution.CountAsync());
        var queue = await verify.IncomingNachaDispatchQueue.AsNoTracking().SingleAsync();
        Assert.True(queue.RequiresReconciliation);
        Assert.Equal(IncomingNachaDispatchQueueStatus.Blocked, queue.QueueStatus);
    }

    [Fact]
    public async Task RepeatedIdenticalLateResult_IsIdempotent_AndDoesNotChangeReconciliation()
    {
        await using var fixture = await Fixture.CreateAsync();
        await WriteLateAsync(fixture, finishedAtUtc: new DateTime(2026, 11, 15, 23, 0, 0, DateTimeKind.Utc));
        await WriteLateAsync(fixture, finishedAtUtc: new DateTime(2026, 11, 15, 23, 1, 0, DateTimeKind.Utc));
        await WriteLateAsync(fixture, finishedAtUtc: new DateTime(2026, 11, 15, 23, 2, 0, DateTimeKind.Utc));

        await using var verify = fixture.Open();
        Assert.Equal(1, await verify.IncomingNachaProcessingEvents.CountAsync());
        Assert.Equal(1, await verify.IncomingNachaIntegrationExecution.CountAsync());
        await AssertQuarantineAndOriginalAttemptAsync(verify);
    }

    [Fact]
    public async Task ConcurrentLateWorkers_ArchiveOnce_WithOneOriginalAttempt()
    {
        await using var fixture = await Fixture.CreateAsync();
        var starts = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var workers = Enumerable.Range(0, 3).Select(async _ =>
        {
            await using var worker = fixture.Open();
            var queue = await worker.IncomingNachaDispatchQueue.SingleAsync();
            var execution = await worker.IncomingNachaIntegrationExecution.SingleAsync();
            ApplyLateResult(queue, execution, LateXml);
            await starts.Task;
            Assert.False(await Pipeline(worker).FinalizeClaimAsync(queue, execution, CancellationToken.None));
        }).ToArray();
        starts.SetResult(true);
        await Task.WhenAll(workers);

        await using var verify = fixture.Open();
        Assert.Equal(1, await verify.IncomingNachaProcessingEvents.CountAsync());
        Assert.Equal(1, await verify.IncomingNachaIntegrationExecution.CountAsync());
        await AssertQuarantineAndOriginalAttemptAsync(verify);
    }

    [Fact]
    public async Task DistinctLateBankOutcomes_OnSameAttempt_RetainBothWithoutOverwriting()
    {
        await using var fixture = await Fixture.CreateAsync();
        await WriteLateAsync(fixture, LateXml);
        await WriteLateAsync(fixture, "<Envelope><Body>different-bank-outcome</Body></Envelope>");

        await using var verify = fixture.Open();
        var events = await verify.IncomingNachaProcessingEvents.AsNoTracking().ToListAsync();
        Assert.Equal(2, events.Count);
        Assert.Equal(2, events.Select(x => x.Id).Distinct().Count());
        Assert.All(events, x => Assert.Equal("NptLateOutcomeFenced", x.EventType));
        Assert.Equal(1, await verify.IncomingNachaIntegrationExecution.CountAsync());
        await AssertQuarantineAndOriginalAttemptAsync(verify);
    }

    [Fact]
    public async Task ArchiveInsertFailure_RollsBackEvidence_LeavesAttemptAndQuarantineIntact()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using (var setup = fixture.Open())
        {
            await setup.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER fail_late_evidence BEFORE INSERT ON IncomingNachaProcessingEvents
                WHEN NEW.EventType = 'NptLateOutcomeFenced'
                BEGIN SELECT RAISE(ABORT, 'injected event persistence failure'); END;
                """);
        }

        await using (var worker = fixture.Open())
        {
            var queue = await worker.IncomingNachaDispatchQueue.SingleAsync();
            var execution = await worker.IncomingNachaIntegrationExecution.SingleAsync();
            ApplyLateResult(queue, execution, LateXml);
            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await Pipeline(worker).FinalizeClaimAsync(queue, execution, CancellationToken.None);
            });
        }

        await using var verify = fixture.Open();
        Assert.Equal(0, await verify.IncomingNachaProcessingEvents.CountAsync());
        Assert.Equal(1, await verify.IncomingNachaIntegrationExecution.CountAsync());
        await AssertQuarantineAndOriginalAttemptAsync(verify);
    }

    private static async Task WriteLateAsync(Fixture fixture, string xml = LateXml, DateTime? finishedAtUtc = null)
    {
        await using var worker = fixture.Open();
        var queue = await worker.IncomingNachaDispatchQueue.SingleAsync();
        var execution = await worker.IncomingNachaIntegrationExecution.SingleAsync();
        ApplyLateResult(queue, execution, xml, finishedAtUtc);
        Assert.False(await Pipeline(worker).FinalizeClaimAsync(queue, execution, CancellationToken.None));
    }

    private static void ApplyLateResult(IncomingNachaDispatchQueue queue,
        IncomingNachaIntegrationExecution execution, string xml, DateTime? finishedAtUtc = null)
    {
        // Old worker observed success, but database is already quarantined by watchdog.
        queue.QueueStatus = IncomingNachaDispatchQueueStatus.Confirmed;
        queue.RequiresReconciliation = false;
        execution.ResponsePayloadXml = xml;
        execution.ResponseHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(xml)));
        execution.ResponseCode = "ACCEPTED-LATE";
        execution.SoapResponseCode = "ACCEPTED-LATE";
        execution.SoapResponseDescription = "not a bank success assertion";
        execution.ResponseMessage = "not a bank success assertion";
        execution.FinishedAtUtc = finishedAtUtc ?? new DateTime(2026, 11, 15, 23, 0, 0, DateTimeKind.Utc);
    }

    private static async Task AssertQuarantineAndOriginalAttemptAsync(AchDbContext db)
    {
        var queue = await db.IncomingNachaDispatchQueue.AsNoTracking().SingleAsync();
        Assert.Equal(IncomingNachaDispatchQueueStatus.Blocked, queue.QueueStatus);
        Assert.True(queue.RequiresReconciliation);
        Assert.Equal("NPT_TRANSPORT_OUTCOME_UNKNOWN", queue.LastErrorCode);
        Assert.False(await Pipeline(db).TryClaimAsync(queue, DateTime.UtcNow, CancellationToken.None));
        var original = await db.IncomingNachaIntegrationExecution.AsNoTracking().SingleAsync();
        Assert.Equal("", original.ResponsePayloadXml);
        Assert.Equal("ORIGINAL_REQUEST", original.RequestPayloadXml);
        Assert.Equal("CFA-LATE-CORR", original.CorrelationId);
        Assert.Equal(1, original.AttemptNumber);
    }

    private static IncomingNachaPostProcessingPipeline Pipeline(AchDbContext db)
        => new(db, Mock.Of<IProcTransaccionesRequestMapper>(),
            Mock.Of<IProcTransaccionesResponseParser>(), new Mock<IWscfaachSoapClient>(MockBehavior.Strict).Object,
            new IncomingNachaDispatchResilienceOptions(), new ProcTransaccionesDispatchOptions(),
            null, null, null, null, null, null, null, TimeProvider.System, null,
            Mock.Of<ICycleCalendarGuard>(), Mock.Of<IOperationalCycleWindowResolver>(), null);

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string _dbFile;
        private readonly DbContextOptions<AchDbContext> _options;
        private Fixture(string dbFile)
        {
            _dbFile = dbFile;
            // FK seeds outside the queue/execution/event graph are omitted on purpose.
            _options = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite($"Data Source={dbFile};Foreign Keys=False;Default Timeout=30").Options;
        }
        public AchDbContext Open() => new(_options);

        public static async Task<Fixture> CreateAsync()
        {
            var f = new Fixture(Path.Combine(Path.GetTempPath(), $"npt-cfa-late-{Guid.NewGuid():N}.sqlite"));
            await using var setup = f.Open();
            await setup.Database.EnsureCreatedAsync();
            await setup.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
            var queue = new IncomingNachaDispatchQueue
            {
                Id = Guid.NewGuid(), IncomingNachaFileIngestionId = Guid.NewGuid(),
                IncomingNachaEntryClassificationId = Guid.NewGuid(),
                IncomingNachaTransactionLinkId = Guid.NewGuid(),
                AchTransactionId = 901, AchCycleId = "CYCLE", ClearingHouseId = 1,
                SourceKind = IncomingNachaDispatchSourceKinds.CycleBound,
                OperationalDate = DateTime.UtcNow.Date,
                IdempotencyDispatchKey = "NPT-CFA-LATE-01",
                QueueStatus = IncomingNachaDispatchQueueStatus.Blocked,
                DispatchClaimToken = "stale-dispatch-token",
                DispatchLeaseExpiresAtUtc = DateTime.UtcNow.AddMinutes(-20),
                TransportStartedAtUtc = DateTime.UtcNow.AddMinutes(-30),
                LastErrorCode = "NPT_TRANSPORT_OUTCOME_UNKNOWN",
                RequiresReconciliation = true,
                AttemptCount = 1
            };
            setup.IncomingNachaDispatchQueue.Add(queue);
            setup.IncomingNachaIntegrationExecution.Add(new IncomingNachaIntegrationExecution
            {
                Id = Guid.NewGuid(), DispatchQueueId = queue.Id,
                EntryDetailId = 4511, AttemptNumber = 1,
                ClearingHouseId = queue.ClearingHouseId,
                MethodName = "Proc_Transacciones", SoapMethodName = "Proc_Transacciones",
                CorrelationId = "CFA-LATE-CORR", ExecutionMode = "Live",
                RequestHash = "existing-request-hash", RequestPayloadXml = "ORIGINAL_REQUEST",
                StartedAtUtc = DateTime.UtcNow.AddMinutes(-30)
            });
            await setup.SaveChangesAsync();
            return f;
        }
        public ValueTask DisposeAsync()
        {
            try { File.Delete(_dbFile); File.Delete(_dbFile + "-wal"); File.Delete(_dbFile + "-shm"); }
            catch (IOException) { }
            return ValueTask.CompletedTask;
        }
    }
}
