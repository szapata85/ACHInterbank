using Cfa.ACHInterbank.Application.OutgoingTransactionMonitoring;
using Cfa.ACHInterbank.Persistence.ACH.OutgoingTransactionMonitoring;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class OutgoingTransactionMonitoringQueryValidationTests
{
    [Fact]
    public async Task SearchAsync_TranslatesReadProjectionAndReturnsServerPagination()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var service = new OutgoingTransactionMonitoringQueryService(
            fixture.Context,
            new OutgoingTransactionMonitoringStatusPolicy(),
            new FixedTimeProvider(new DateTimeOffset(2026, 8, 2, 12, 0, 0, TimeSpan.Zero)));

        var result = await service.SearchAsync(new OutgoingTransactionMonitoringQuery());

        result.Items.Should().BeEmpty();
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(25);
        result.TotalItems.Should().Be(0);
        result.TotalPages.Should().Be(0);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task SearchAsync_RejectsRangesLongerThanNinetyDays()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var service = new OutgoingTransactionMonitoringQueryService(fixture.Context, new OutgoingTransactionMonitoringStatusPolicy());

        var action = () => service.SearchAsync(new OutgoingTransactionMonitoringQuery
        {
            FromUtc = DateTimeOffset.UtcNow.AddDays(-91),
            ToUtc = DateTimeOffset.UtcNow
        });

        var exception = await action.Should().ThrowAsync<OutgoingTransactionMonitoringException>();
        exception.Which.Code.Should().Be("OUTGOING_MONITOR_INVALID_DATE_RANGE");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(24)]
    [InlineData(101)]
    public async Task SearchAsync_RejectsArbitraryPageSizes(int pageSize)
    {
        await using var fixture = await TestFixture.CreateAsync();
        var service = new OutgoingTransactionMonitoringQueryService(fixture.Context, new OutgoingTransactionMonitoringStatusPolicy());

        var action = () => service.SearchAsync(new OutgoingTransactionMonitoringQuery { PageSize = pageSize });

        var exception = await action.Should().ThrowAsync<OutgoingTransactionMonitoringException>();
        exception.Which.Code.Should().Be("OUTGOING_MONITOR_PAGE_SIZE_EXCEEDED");
    }

    [Fact]
    public async Task GetDetailAsync_ReturnsNotFoundOutsideConfirmedOutgoingScope()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var service = new OutgoingTransactionMonitoringQueryService(fixture.Context, new OutgoingTransactionMonitoringStatusPolicy());

        var result = await service.GetDetailAsync(999, includeTechnicalDetail: true);

        result.Should().BeNull();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        private TestFixture(AchDbContext context) => Context = context;

        public AchDbContext Context { get; }

        public static async Task<TestFixture> CreateAsync()
        {
            var options = new DbContextOptionsBuilder<AchDbContext>()
                .UseInMemoryDatabase($"outgoing-monitor-{Guid.NewGuid():N}")
                .Options;
            var context = new AchDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return new TestFixture(context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
        }
    }
}
