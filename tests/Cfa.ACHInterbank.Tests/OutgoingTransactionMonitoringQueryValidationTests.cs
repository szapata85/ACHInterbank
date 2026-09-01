using Cfa.ACHInterbank.Application.OutgoingTransactionMonitoring;
using Cfa.ACHInterbank.Persistence.ACH.OutgoingTransactionMonitoring;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
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

    [Fact]
    public async Task GetDetailAsync_UsesOnlyExactFileExportMembership()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var context = fixture.Context;
        var configuration = new ClearingHouseConfig { TimeZoneId = "America/Bogota" };
        context.Add(configuration);
        await context.SaveChangesAsync();
        var house = new ClearingHouse { Name = "Cámara", Code = "MEM", OriginCode = "MEM", ClearingHouseId = configuration.Id };
        var source = Institution("CFA", true, "00001", "001");
        var destination = Institution("Destino", false, "00002", "002");
        context.AddRange(house, source, destination);
        await context.SaveChangesAsync();
        var cycle = new AchCycle { Id = "MEM-C1", CycleName = "C1", ProcessingDate = new DateTime(2026, 8, 2), StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(12), CutoffTime = TimeSpan.FromHours(11), ClearingHouseId = house.Id, OperationalStatus = AchCycleOperationalStatus.Open };
        var batch = new AchBatch { AchCycleId = cycle.Id, ServiceClassCode = "220", CompanyName = "CFA", CompanyIdentification = "MEM", OriginOrOdfi = "00000001", EffectiveEntryDate = cycle.ProcessingDate, BatchSequenceNumber = 1, CompanyEntryDescriptionId = 1 };
        context.AddRange(cycle, batch);
        await context.SaveChangesAsync();
        var t1 = Transaction("T1", "900000000000001", source.Id, destination.Id, cycle.Id, batch.Id);
        var t2 = Transaction("T2", "900000000000002", source.Id, destination.Id, cycle.Id, batch.Id);
        var t3 = Transaction("T3", "900000000000003", source.Id, destination.Id, cycle.Id, batch.Id);
        context.AddRange(t1, t2, t3);
        await context.SaveChangesAsync();
        var fileA = File(cycle.Id, house.Id, "File A", 1);
        var fileB = File(cycle.Id, house.Id, "File B", 2);
        context.AddRange(fileA, fileB);
        await context.SaveChangesAsync();
        context.AchFileExportTransactions.AddRange(
            Membership(fileA.Id, t1, cycle.Id, batch.Id),
            Membership(fileB.Id, t2, cycle.Id, batch.Id));
        await context.SaveChangesAsync();
        var service = new OutgoingTransactionMonitoringQueryService(context, new OutgoingTransactionMonitoringStatusPolicy());

        (await service.GetDetailAsync(t1.Id, false))!.Files.Select(file => file.FileName).Should().Equal("File A");
        (await service.GetDetailAsync(t2.Id, false))!.Files.Select(file => file.FileName).Should().Equal("File B");
        (await service.GetDetailAsync(t3.Id, false))!.Files.Should().BeEmpty();
    }

    private static FinancialInstitution Institution(string name, bool source, string routing, string transit)
    {
        var institution = new FinancialInstitution { Name = name, IsDefaultSource = source, RoutingNumber = routing, TransitCode = transit, Status = FinancialInstitutionStatus.Active };
        institution.CalculateCheckDigit();
        return institution;
    }

    private static AchTransaction Transaction(string externalId, string traceNumber, int sourceId, int destinationId, string cycleId, int batchId)
        => new()
        {
            Amount = 100m, TransactionExternalId = externalId, Reference = externalId, Type = TransactionTypeEnum.Credit, TransactionCode = "22", ServiceClassCode = "220", CompanyEntryDescriptionId = 1, CompanyName = "CFA", CompanyIdentification = "MEM", OriginatingDFI = "00000001", ReceivingDFI = "00000002", TraceNumber = traceNumber, TraceSequenceNumber = 1, EffectiveEntryDate = new DateTime(2026, 8, 2), Direction = AchTransactionDirection.Outgoing, Origin = AchTransactionOrigin.Cfa, MonetaryIntegrationRoute = AchMonetaryIntegrationRoute.ProcContrapartidas, ClassificationStatus = AchTransactionClassificationStatus.Determined, SourceInstitutionWasDefaultAtCreation = true, ClassifiedAtUtc = new DateTime(2026, 8, 2), ClassificationVersion = 1, State = AchTransferStateEnum.Pending, StateChangedAtUtc = new DateTime(2026, 8, 2), SourceAccountNumber = "0000001111", DestinationAccountNumber = "0000002222", SourceInstitutionId = sourceId, DestinationInstitutionId = destinationId, AchCycleId = cycleId, AchBatchId = batchId, DiscretionaryData = string.Empty, CreatedAt = new DateTimeOffset(2026, 8, 2, 8, 0, 0, TimeSpan.Zero), UpdatedAt = new DateTimeOffset(2026, 8, 2, 8, 0, 0, TimeSpan.Zero)
        };

    private static AchFileExport File(string cycleId, int houseId, string fileName, int version)
        => new() { AchCycleId = cycleId, ClearingHouseId = houseId, ExportKind = "OUT", FileName = fileName, TotalRecords = 1, TotalTransactions = 1, IsEncrypted = true, GeneratedAtUtc = new DateTime(2026, 8, 2, 9, version, 0, DateTimeKind.Utc), Version = version, LifecycleStatus = AchFileExportLifecycleStatus.Generated };

    private static AchFileExportTransaction Membership(int fileId, AchTransaction transaction, string cycleId, int batchId)
        => new() { AchFileExportId = fileId, AchTransactionId = transaction.Id, AchCycleId = cycleId, AchBatchId = batchId, FileSequence = 1, TraceNumber = transaction.TraceNumber, Amount = transaction.Amount, IncludedAtUtc = new DateTime(2026, 8, 2, 9, fileId, 0, DateTimeKind.Utc) };

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
