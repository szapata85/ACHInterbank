using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests.Mapping;

public class DailyResetBatchNumberGeneratorTests
{
    [Fact]
    public async Task AssignBatchNumbersAsync_NewScope_ShouldStartAt1()
    {
        await using var harness = await CreateSqliteHarnessAsync();
        var sut = new DailyResetBatchNumberGenerator(new BatchNumberSequenceStore(harness.Context));
        var batches = new List<AchBatch> { new() { Id = 1, OriginOrOdfi = "11111111" } };

        var result = await sut.AssignBatchNumbersAsync(batches, "ACH", new DateTime(2026, 4, 19, 10, 0, 0, DateTimeKind.Utc));

        result.BatchNumberByBatchId[1].Should().Be(1);
        result.ScopeTrace.Should().ContainSingle();
        result.ScopeTrace[0].PreviousValue.Should().Be(0);
        result.ScopeTrace[0].AssignedValue.Should().Be(1);
        result.ScopeTrace[0].WasCreated.Should().BeTrue();
    }

    [Fact]
    public async Task AssignBatchNumbersAsync_ExistingScope_ShouldIncrement()
    {
        await using var harness = await CreateSqliteHarnessAsync();
        var store = new BatchNumberSequenceStore(harness.Context);
        var sut = new DailyResetBatchNumberGenerator(store);

        await sut.AssignBatchNumbersAsync([new AchBatch { Id = 1, OriginOrOdfi = "11111111" }], "ACH", new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc));
        var second = await sut.AssignBatchNumbersAsync([new AchBatch { Id = 2, OriginOrOdfi = "11111111" }], "ACH", new DateTime(2026, 4, 19, 2, 0, 0, DateTimeKind.Utc));

        second.BatchNumberByBatchId[2].Should().Be(2);
        second.ScopeTrace[0].PreviousValue.Should().Be(1);
        second.ScopeTrace[0].AssignedValue.Should().Be(2);
        second.ScopeTrace[0].WasCreated.Should().BeFalse();
    }

    [Fact]
    public async Task AssignBatchNumbersAsync_DifferentDay_ShouldReset()
    {
        await using var harness = await CreateSqliteHarnessAsync();
        var sut = new DailyResetBatchNumberGenerator(new BatchNumberSequenceStore(harness.Context));

        var day1 = await sut.AssignBatchNumbersAsync([new AchBatch { Id = 1, OriginOrOdfi = "11111111" }], "ACH", new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc));
        var day2 = await sut.AssignBatchNumbersAsync([new AchBatch { Id = 2, OriginOrOdfi = "11111111" }], "ACH", new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc));

        day1.BatchNumberByBatchId[1].Should().Be(1);
        day2.BatchNumberByBatchId[2].Should().Be(1);
    }

    [Fact]
    public async Task AssignBatchNumbersAsync_ShouldSeparateByChamberAndOriginatingDfi()
    {
        await using var harness = await CreateSqliteHarnessAsync();
        var sut = new DailyResetBatchNumberGenerator(new BatchNumberSequenceStore(harness.Context));

        var ach = await sut.AssignBatchNumbersAsync([new AchBatch { Id = 1, OriginOrOdfi = "11111111" }], "ACH", new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc));
        var cenit = await sut.AssignBatchNumbersAsync([new AchBatch { Id = 2, OriginOrOdfi = "11111111" }], "CENIT", new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc));
        var otherOdfi = await sut.AssignBatchNumbersAsync([new AchBatch { Id = 3, OriginOrOdfi = "22222222" }], "ACH", new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc));

        ach.BatchNumberByBatchId[1].Should().Be(1);
        cenit.BatchNumberByBatchId[2].Should().Be(1);
        otherOdfi.BatchNumberByBatchId[3].Should().Be(1);
        harness.Context.BatchNumberSequences.Count().Should().Be(3);
    }

    [Fact]
    public async Task ReserveRangeAsync_ConcurrentCreate_ShouldAllocateUniqueNumbers()
    {
        await using var harness = await CreateSqliteHarnessAsync();

        var scope = new BatchNumberSequenceScope("DAILY_RESET_BY_CHAMBER_DATE_ORIGINATING_DFI", "ACH", "11111111", new DateOnly(2026, 4, 19));
        var tasks = Enumerable.Range(0, 5).Select(async _ =>
        {
            await using var context = CreateContext(harness.Connection);
            var store = new BatchNumberSequenceStore(context);
            var result = await store.ReserveRangeAsync(scope, 1);
            return result.StartValue;
        });

        var values = await Task.WhenAll(tasks);
        values.OrderBy(x => x).Should().Equal([1, 2, 3, 4, 5]);
    }

    private static async Task<SqliteHarness> CreateSqliteHarnessAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        return new SqliteHarness(connection, context);
    }

    private static AchDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        return new AchDbContext(options);
    }

    private sealed class SqliteHarness(SqliteConnection connection, AchDbContext context) : IAsyncDisposable
    {
        public SqliteConnection Connection { get; } = connection;
        public AchDbContext Context { get; } = context;

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
