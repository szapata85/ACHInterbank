using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class ExternalFileNameReservationTests
{
    [Fact]
    public async Task SameLogicalRequest_ReusesSequenceAndReservation()
    {
        await using var harness = await Harness.CreateAsync();
        var service = CreateService(harness.Context);
        var context = CreateContext("logical-request-001");

        var first = await service.ReserveAsync(context, "fingerprint-v1");
        await service.CompleteAsync(first.ReservationId, "1234567.001.1", 'A');
        var retry = await service.ReserveAsync(context, "fingerprint-v1");

        Assert.Equal(first.ReservationId, retry.ReservationId);
        Assert.Equal(1, retry.Sequence);
        Assert.True(retry.WasReused);
        Assert.Equal(1, await harness.Context.ExternalFileNameReservations.CountAsync());
        Assert.Equal(1, (await harness.Context.ExternalFileSequences.SingleAsync()).LastValue);
    }

    [Fact]
    public async Task DifferentLogicalRequests_ReceiveDifferentSequences()
    {
        await using var harness = await Harness.CreateAsync();
        var service = CreateService(harness.Context);

        var first = await service.ReserveAsync(CreateContext("logical-request-001"), "fingerprint-001");
        var second = await service.ReserveAsync(CreateContext("logical-request-002"), "fingerprint-002");

        Assert.Equal(1, first.Sequence);
        Assert.Equal(2, second.Sequence);
        Assert.NotEqual(first.ReservationId, second.ReservationId);
    }

    [Fact]
    public async Task SameKeyWithDifferentFingerprint_FailsClosed()
    {
        await using var harness = await Harness.CreateAsync();
        var service = CreateService(harness.Context);
        var context = CreateContext("logical-request-001");
        await service.ReserveAsync(context, "fingerprint-001");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ReserveAsync(context, "fingerprint-modified"));

        Assert.Contains("IDEMPOTENCY_MISMATCH", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, (await harness.Context.ExternalFileSequences.SingleAsync()).LastValue);
    }

    [Fact]
    public async Task Rollback_DoesNotConsumeSequenceOrReservation()
    {
        await using var harness = await Harness.CreateAsync();
        await using (var transaction = await harness.Context.Database.BeginTransactionAsync())
        {
            var service = CreateService(harness.Context);
            var reserved = await service.ReserveAsync(CreateContext("rolled-back-request"), "fingerprint-rollback");
            Assert.Equal(1, reserved.Sequence);
            await transaction.RollbackAsync();
        }

        harness.Context.ChangeTracker.Clear();
        var retryService = CreateService(harness.Context);
        var retry = await retryService.ReserveAsync(CreateContext("retry-after-rollback"), "fingerprint-retry");

        Assert.Equal(1, retry.Sequence);
        Assert.Equal(1, await harness.Context.ExternalFileNameReservations.CountAsync());
    }

    [Fact]
    public async Task SequenceLimit_FailsWithoutPersistingThirtySeventhValue()
    {
        await using var harness = await Harness.CreateAsync();
        var provider = new EfGenericExternalFileNameSequenceService(harness.Context);
        for (var i = 1; i <= 36; i++)
        {
            Assert.Equal(i, await provider.ReserveNextSequenceAsync(CreateContext($"request-{i:D2}")));
        }

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.ReserveNextSequenceAsync(CreateContext("request-37")));

        Assert.Equal(36, (await harness.Context.ExternalFileSequences.SingleAsync()).LastValue);
    }

    private static ExternalFileNameReservationService CreateService(AchDbContext context)
    {
        var generic = new EfGenericExternalFileNameSequenceService(context);
        var resolver = new ExternalFileNameSequenceProviderResolver([generic]);
        var sequence = new ExternalFileNameSequenceService(context, resolver);
        return new ExternalFileNameReservationService(context, sequence);
    }

    private static ExternalFileNameContext CreateContext(string idempotencyKey) => new()
    {
        ClearingHouseId = 1,
        ClearingHouseCode = "ACHCOL",
        ClearingHouseOriginCode = "1234567",
        CycleNumber = 1,
        ProcessingDate = new DateTime(2026, 7, 16),
        OperationalTimeSnapshot = new OperationalTimeSnapshot(
            new DateTime(2026, 7, 16, 13, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 16, 8, 0, 0),
            new DateOnly(2026, 7, 16),
            "America/Bogota"),
        IdempotencyKey = idempotencyKey,
        ExternalFileType = ExternalFileType.NachaOut,
        Flow = ExternalFileFlow.Originacion,
        Direction = ExternalFileDirection.Outbound,
        RequestedBy = "synthetic-test"
    };

    private sealed class Harness(SqliteConnection connection, AchDbContext context) : IAsyncDisposable
    {
        public AchDbContext Context { get; } = context;

        public static async Task<Harness> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;
            var context = new AchDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return new Harness(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
