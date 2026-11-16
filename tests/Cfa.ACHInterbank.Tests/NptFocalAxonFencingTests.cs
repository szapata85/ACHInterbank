using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

/// <summary>SQLite integration: real relational ExecuteUpdate and audit transactions,
/// not a mocked CAS. Cross-provider SQL Server/Postgres remains an infra test.</summary>
public sealed class NptFocalAxonFencingTests
{
    [Fact]
    public async Task TwoWorkersCompete_ForOneAttempt_OnlyOneWins()
    {
        await using var fixture = await Fixture.CreateAsync();
        var now = DateTime.UtcNow;
        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(async i =>
        {
            await using var db = fixture.Open();
            return await new AxonNotificationClaimStore(db).TryClaimAsync(
                71, $"worker-{i}", now, CancellationToken.None);
        }));
        Assert.Equal(1, results.Count(x => x));
        await using var check = fixture.Open();
        Assert.Equal(AchResponseNotificationStatus.InFlight,
            (await check.AchResponseNotificationAttempts.SingleAsync(x => x.Id == 71)).EstadoNotificacion);
    }

    [Fact]
    public async Task ExpiredLeaseQuarantinesParent_AndOldWorkerCannotFinalize()
    {
        await using var fixture = await Fixture.CreateAsync();
        var t0 = DateTime.UtcNow.AddHours(-1);
        await using (var worker = fixture.Open())
        {
            var store = new AxonNotificationClaimStore(worker);
            Assert.True(await store.TryClaimAsync(71, "old-token", t0, CancellationToken.None));
            Assert.True(await store.MarkTransportIntentAsync(71, "old-token", t0.AddSeconds(1), CancellationToken.None));
        }
        await using (var watchdog = fixture.Open())
            Assert.Equal(1, await new AxonNotificationClaimStore(watchdog)
                .QuarantineExpiredAsync(t0.AddMinutes(31), CancellationToken.None));
        await using (var oldWorker = fixture.Open())
        {
            var store = new AxonNotificationClaimStore(oldWorker);
            Assert.False(await store.TryFinalizeAsync(71, fixture.ResponseId, "old-token",
                Success, t0.AddMinutes(31), CancellationToken.None));
            await store.RecordLateEvidenceAsync(71, fixture.ResponseId, "old-token",
                "LATE_SOAP_SUCCESS", "<mock>ok</mock>", CancellationToken.None);
        }
        await using var check = fixture.Open();
        Assert.Equal(AchResponseNotificationStatus.RequiereRevisionManual,
            (await check.AchResponseNotificationAttempts.SingleAsync(x => x.Id == 71)).EstadoNotificacion);
        Assert.Equal(AchResponseProcessingStatus.RequiereRevisionManual,
            (await check.AchResponses.SingleAsync(x => x.Id == fixture.ResponseId)).EstadoProcesamiento);
        Assert.Equal(2, await check.AchResponseAudits.CountAsync(x => x.AchResponseId == fixture.ResponseId));
        Assert.False(await new AxonNotificationClaimStore(check).TryClaimAsync(71,
            "replay", t0.AddMinutes(32), CancellationToken.None));
    }

    [Fact]
    public async Task PreflightCannotOverrideConcurrentInFlightOrManualQuarantine()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using (var ownerDb = fixture.Open())
            Assert.True(await new AxonNotificationClaimStore(ownerDb)
                .TryClaimAsync(71, "winner", DateTime.UtcNow, CancellationToken.None));
        await using (var delayedPreflightDb = fixture.Open())
        {
            Assert.False(await new AxonNotificationClaimStore(delayedPreflightDb)
                .TryFinishPreflightAsync(71, fixture.ResponseId,
                    new AxonNotificationCompletion(AchResponseNotificationStatus.ErrorFuncional,
                        AchResponseProcessingStatus.ErrorFuncional, true, "INVALID", null, null, null, null),
                    DateTime.UtcNow, CancellationToken.None));
        }
        await using var check = fixture.Open();
        Assert.Equal(AchResponseNotificationStatus.InFlight,
            (await check.AchResponseNotificationAttempts.SingleAsync(x => x.Id == 71)).EstadoNotificacion);
        Assert.Equal(AchResponseProcessingStatus.Homologada,
            (await check.AchResponses.SingleAsync(x => x.Id == fixture.ResponseId)).EstadoProcesamiento);
    }

    [Fact]
    public async Task UnexpiredOwnedClaim_CompletesResponseAndAttemptTogether()
    {
        await using var fixture = await Fixture.CreateAsync();
        var t0 = DateTime.UtcNow;
        await using (var worker = fixture.Open())
        {
            var store = new AxonNotificationClaimStore(worker);
            Assert.True(await store.TryClaimAsync(71, "owner", t0, CancellationToken.None));
            Assert.True(await store.MarkTransportIntentAsync(71, "owner", t0, CancellationToken.None));
            Assert.True(await store.TryFinalizeAsync(71, fixture.ResponseId, "owner",
                Success, t0.AddSeconds(1), CancellationToken.None));
        }
        await using var check = fixture.Open();
        Assert.Equal(AchResponseNotificationStatus.Exitosa,
            (await check.AchResponseNotificationAttempts.SingleAsync(x => x.Id == 71)).EstadoNotificacion);
        Assert.Equal(AchResponseProcessingStatus.Notificada,
            (await check.AchResponses.SingleAsync(x => x.Id == fixture.ResponseId)).EstadoProcesamiento);
        Assert.Single(await check.AchResponseAudits.Where(x => x.AchResponseId == fixture.ResponseId).ToListAsync());
    }

    private static readonly AxonNotificationCompletion Success = new(
        AchResponseNotificationStatus.Exitosa, AchResponseProcessingStatus.Notificada,
        false, null, null, null, "{}", "{\"existeError\":false}");

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly string _path;
        private readonly DbContextOptions<AchDbContext> _options;
        public Guid ResponseId { get; } = Guid.NewGuid();
        private Fixture(string path)
        {
            _path = path;
            _options = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite($"Data Source={path};Default Timeout=30").Options;
        }
        public AchDbContext Open() => new(_options);
        public static async Task<Fixture> CreateAsync()
        {
            var path = Path.Combine(Path.GetTempPath(), $"npt-axon-{Guid.NewGuid():N}.sqlite");
            var f = new Fixture(path);
            await using var setup = f.Open();
            await setup.Database.EnsureCreatedAsync();
            await setup.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
            setup.AchResponses.Add(new AchResponse
            {
                Id = f.ResponseId, IdTransaccion = "PRENOTE-001", CodigoCamaraCompensacion = "ACH",
                CodigoEstadoExterno = "00", TipoRespuesta = TipoRespuestaAch.Prenota,
                EstadoProcesamiento = AchResponseProcessingStatus.Homologada,
                HashIdempotencia = Guid.NewGuid().ToString("N"),
                CanonicalPayloadHash = Guid.NewGuid().ToString("N"),
                OperationalDate = DateTime.UtcNow.Date, Version = Guid.NewGuid(),
                FechaCreacion = DateTime.UtcNow, FechaRecepcion = DateTime.UtcNow
            });
            setup.AchResponseNotificationAttempts.Add(new AchResponseNotificationAttempt
            {
                Id = 71, AchResponseId = f.ResponseId, NumeroIntento = 1,
                EstadoNotificacion = AchResponseNotificationStatus.Pendiente,
                NombreCanal = "ACH", IdTransaccion = "PRENOTE-001",
                FechaCreacion = DateTime.UtcNow
            });
            await setup.SaveChangesAsync();
            return f;
        }
        public ValueTask DisposeAsync()
        {
            try { File.Delete(_path); File.Delete(_path + "-wal"); File.Delete(_path + "-shm"); }
            catch (IOException) { /* test scratch file; cleanup on OS restart */ }
            return ValueTask.CompletedTask;
        }
    }
}
