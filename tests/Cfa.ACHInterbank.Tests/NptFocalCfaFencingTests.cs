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

/// <summary>Relational SQLite CAS tests; provider-specific SQL Server/Postgres
/// concurrency certification is a separate infrastructure gate.</summary>
public sealed class NptFocalCfaFencingTests
{
    [Fact]
    public async Task TwoInstancesCannotClaimSameMonetaryQueue()
    {
        await using var fixture = await Fixture.CreateAsync();
        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ =>
        {
            await using var db = fixture.Open();
            var queue = await db.IncomingNachaDispatchQueue.SingleAsync();
            return await Pipeline(db).TryClaimAsync(queue, DateTime.UtcNow, CancellationToken.None);
        }));
        Assert.Single(results.Where(x => x));
        await using var check = fixture.Open();
        var q = await check.IncomingNachaDispatchQueue.SingleAsync();
        Assert.Equal(IncomingNachaDispatchQueueStatus.Dispatching, q.QueueStatus);
        Assert.Equal(1, q.AttemptCount);
        Assert.False(string.IsNullOrWhiteSpace(q.DispatchClaimToken));
    }

    [Fact]
    public async Task ExpiredTransportIntent_RemainsQuarantinedAfterLateSuccess()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using var workerDb = fixture.Open();
        var queue = await workerDb.IncomingNachaDispatchQueue.SingleAsync();
        var worker = Pipeline(workerDb);
        var oldTime = DateTime.UtcNow.AddMinutes(-40);
        Assert.True(await worker.TryClaimAsync(queue, oldTime, CancellationToken.None));
        // Simulate a transport intent recorded before a process crash.
        await workerDb.IncomingNachaDispatchQueue
            .Where(x => x.Id == queue.Id)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.TransportStartedAtUtc, oldTime));
        queue.TransportStartedAtUtc = oldTime;
        await using (var watchdogDb = fixture.Open())
            await Pipeline(watchdogDb).RecoverExpiredClaimsAsync(DateTime.UtcNow, CancellationToken.None);
        queue.QueueStatus = IncomingNachaDispatchQueueStatus.Confirmed; // stale worker
        Assert.False(await worker.FinalizeClaimAsync(queue, null, CancellationToken.None));
        await using var check = fixture.Open();
        var persisted = await check.IncomingNachaDispatchQueue.AsNoTracking().SingleAsync();
        Assert.Equal(IncomingNachaDispatchQueueStatus.Blocked, persisted.QueueStatus);
        Assert.True(persisted.RequiresReconciliation);
        Assert.Equal("NPT_TRANSPORT_OUTCOME_UNKNOWN", persisted.LastErrorCode);
        Assert.True(await check.IncomingNachaProcessingEvents.CountAsync() >= 2);
        Assert.False(await Pipeline(check).TryClaimAsync(persisted, DateTime.UtcNow, CancellationToken.None));
    }

    [Fact]
    public async Task ValidOwnerFinishesUnderFenceAndCannotFinishTwice()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using var workerDb = fixture.Open();
        var queue = await workerDb.IncomingNachaDispatchQueue.SingleAsync();
        var worker = Pipeline(workerDb);
        Assert.True(await worker.TryClaimAsync(queue, DateTime.UtcNow, CancellationToken.None));
        queue.QueueStatus = IncomingNachaDispatchQueueStatus.Confirmed;
        Assert.True(await worker.FinalizeClaimAsync(queue, null, CancellationToken.None));
        Assert.False(await worker.FinalizeClaimAsync(queue, null, CancellationToken.None));
    }

    private static IncomingNachaPostProcessingPipeline Pipeline(AchDbContext db)
        => new(db, Mock.Of<IProcTransaccionesRequestMapper>(),
            Mock.Of<IProcTransaccionesResponseParser>(), Mock.Of<IWscfaachSoapClient>(),
            new IncomingNachaDispatchResilienceOptions(), new ProcTransaccionesDispatchOptions(),
            null, null, null, null, null, null, null, TimeProvider.System, null,
            Mock.Of<ICycleCalendarGuard>(), Mock.Of<IOperationalCycleWindowResolver>(), null);

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string _path;
        private readonly DbContextOptions<AchDbContext> _options;
        private Fixture(string path)
        {
            _path = path;
            // Missing unrelated FK seeds are deliberate: this test isolates the
            // DB fencing contract, NOT full NACHA ingestion.
            _options = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite($"Data Source={path};Foreign Keys=False;Default Timeout=30").Options;
        }
        public AchDbContext Open() => new(_options);
        public static async Task<Fixture> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"npt-cfa-{Guid.NewGuid():N}.sqlite");
            var fixture = new Fixture(path);
            await using var setup = fixture.Open();
            await setup.Database.EnsureCreatedAsync();
            await setup.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
            setup.IncomingNachaDispatchQueue.Add(new IncomingNachaDispatchQueue
            {
                Id = Guid.NewGuid(), IncomingNachaFileIngestionId = Guid.NewGuid(),
                IncomingNachaEntryClassificationId = Guid.NewGuid(),
                IncomingNachaTransactionLinkId = Guid.NewGuid(),
                AchTransactionId = 901, AchCycleId = "CYCLE", ClearingHouseId = 1,
                SourceKind = IncomingNachaDispatchSourceKinds.CycleBound,
                OperationalDate = DateTime.UtcNow.Date,
                IdempotencyDispatchKey = "NPT-FENCE-001", QueueStatus = IncomingNachaDispatchQueueStatus.Queued
            });
            await setup.SaveChangesAsync();
            return fixture;
        }
        public ValueTask DisposeAsync()
        {
            try { File.Delete(_path); File.Delete(_path + "-wal"); File.Delete(_path + "-shm"); }
            catch (IOException) { }
            return ValueTask.CompletedTask;
        }
    }
}
