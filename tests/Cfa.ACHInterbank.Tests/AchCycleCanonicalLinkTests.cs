using AutoMapper;
using Cfa.ACHInterbank.Application.Configuration;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchCycleCanonicalLinkTests
{
    private static readonly DateTime ProcessingDate = new(2026, 7, 22);

    [Fact]
    public async Task Create_UsesExplicitCanonicalConfiguration_AndRejectsInvalidAssignments()
    {
        await using var fixture = CreateFixture();
        await using var context = fixture.Context;
        var ids = await SeedBaseAsync(context);
        var service = CreateService(context);

        var created = await service.CreateAsync(Request(ids.PrimaryConfigId));
        Assert.Equal(ids.PrimaryConfigId, created.ClearingHouseCycleConfigId);
        Assert.Equal("Ciclo canónico", created.CycleName);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(Request(ids.OtherHouseConfigId, suffix: "-OTHER")));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(Request(ids.InactiveConfigId, suffix: "-INACTIVE")));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(Request(ids.FutureConfigId, suffix: "-FUTURE")));
        Assert.Single(await context.AchCycles.ToListAsync());
    }

    [Fact]
    public async Task Create_LegacyRequest_DoesNotDependExclusivelyOnName_AndRejectsAmbiguity()
    {
        await using var fixture = CreateFixture();
        await using var context = fixture.Context;
        var ids = await SeedBaseAsync(context);
        var service = CreateService(context);

        var request = Request(null);
        request.CycleName = " nombre visible cambiado ";
        var created = await service.CreateAsync(request);
        Assert.Equal(ids.PrimaryConfigId, created.ClearingHouseCycleConfigId);

        context.ClearingHouseCycleConfigs.Add(new ClearingHouseCycleConfig
        {
            ClearingHouseId = ids.PrimaryHouseId,
            CycleName = "CICLO CANÓNICO",
            StartTime = TimeSpan.FromHours(8),
            EndTime = TimeSpan.FromHours(10),
            CutoffTime = TimeSpan.FromHours(9),
            EffectiveFrom = ProcessingDate.AddDays(-10),
            IsActive = true
        });
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(Request(null, suffix: "-AMBIGUOUS")));
    }

    [Fact]
    public async Task Update_RevalidatesDateAndHouse_AndBlocksConfigurationChangeWithTransactions()
    {
        await using var fixture = CreateFixture();
        await using var context = fixture.Context;
        var ids = await SeedBaseAsync(context);
        var service = CreateService(context);
        var created = await service.CreateAsync(Request(ids.PrimaryConfigId));
        await AddTransactionAsync(context, created.Id, ids.PrimaryHouseId, 25.50m);
        var originalAmount = await context.AchTransactions.Select(x => x.Amount).SingleAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(created.Id, Request(ids.AlternateConfigId)));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(created.Id, Request(ids.PrimaryConfigId, processingDate: ProcessingDate.AddYears(2))));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(created.Id, Request(ids.PrimaryConfigId, clearingHouseId: ids.OtherHouseId)));

        context.ChangeTracker.Clear();
        var persisted = await context.AchCycles.AsNoTracking().SingleAsync(x => x.Id == created.Id);
        Assert.Equal(ids.PrimaryConfigId, persisted.ClearingHouseCycleConfigId);
        Assert.Equal(ids.PrimaryHouseId, persisted.ClearingHouseId);
        Assert.Equal(ProcessingDate, persisted.ProcessingDate);
        Assert.Equal(originalAmount, await context.AchTransactions.Select(x => x.Amount).SingleAsync());
    }

    [Fact]
    public async Task Repair_LinksUniqueHistoricalCycle_AndIsIdempotent()
    {
        await using var fixture = CreateFixture();
        await using var context = fixture.Context;
        var ids = await SeedBaseAsync(context);
        var historical = HistoricalCycle("HIST-UNIQUE", ids.PrimaryHouseId, "Nombre histórico");
        context.AchCycles.Add(historical);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var first = await service.RepairConfigurationLinksAsync();
        Assert.True(first.Completed);
        Assert.Equal(1, first.RepairedCount);
        Assert.Equal(ids.PrimaryConfigId, await context.AchCycles.Where(x => x.Id == historical.Id)
            .Select(x => x.ClearingHouseCycleConfigId).SingleAsync());

        var second = await service.RepairConfigurationLinksAsync();
        Assert.True(second.Completed);
        Assert.Equal(0, second.RepairedCount);
    }

    [Fact]
    public async Task Repair_DoesNotModifyAmbiguousOrUnmatchedCycles()
    {
        await using var fixture = CreateFixture();
        await using var context = fixture.Context;
        var ids = await SeedBaseAsync(context);
        context.ClearingHouseCycleConfigs.Add(new ClearingHouseCycleConfig
        {
            ClearingHouseId = ids.PrimaryHouseId,
            CycleName = "Ciclo canónico",
            StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(10), CutoffTime = TimeSpan.FromHours(9),
            EffectiveFrom = ProcessingDate.AddDays(-20), IsActive = false
        });
        context.AchCycles.AddRange(
            HistoricalCycle("HIST-AMBIGUOUS", ids.PrimaryHouseId, "Ciclo canónico"),
            HistoricalCycle("HIST-NOMATCH", ids.PrimaryHouseId, "Sin configuración", TimeSpan.FromHours(18)));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.RepairConfigurationLinksAsync();
        Assert.False(result.Completed);
        Assert.Equal(1, result.AmbiguousCount);
        Assert.Equal(1, result.UnmatchedCount);
        Assert.All(await context.AchCycles.AsNoTracking().ToListAsync(), x => Assert.Null(x.ClearingHouseCycleConfigId));
    }

    private static AchCycleRequest Request(
        int? configId,
        string suffix = "",
        DateTime? processingDate = null,
        int clearingHouseId = 1) => new()
    {
        ClearingHouseId = clearingHouseId,
        ClearingHouseCycleConfigId = configId,
        CycleName = $"Ciclo canónico{suffix}",
        ProcessingDate = processingDate ?? ProcessingDate,
        StartTime = TimeSpan.FromHours(8),
        EndTime = TimeSpan.FromHours(10),
        CutoffTime = TimeSpan.FromHours(9),
        RescheduleOnHoliday = false
    };

    private static AchCycle HistoricalCycle(string id, int houseId, string name, TimeSpan? start = null) => new()
    {
        Id = id,
        ClearingHouseId = houseId,
        CycleName = name,
        ProcessingDate = ProcessingDate,
        StartTime = start ?? TimeSpan.FromHours(8),
        EndTime = (start ?? TimeSpan.FromHours(8)).Add(TimeSpan.FromHours(2)),
        CutoffTime = (start ?? TimeSpan.FromHours(8)).Add(TimeSpan.FromHours(1)),
        ClearingHouseCycleConfigId = null
    };

    private static async Task AddTransactionAsync(AchDbContext context, string cycleId, int houseId, decimal amount)
    {
        context.AchBatches.Add(new AchBatch
        {
            Id = 900,
            AchCycleId = cycleId,
            CompanyName = "PRUEBA",
            CompanyIdentification = "900000000",
            CompanyEntryDescription = "PRUEBA",
            CompanyEntryDescriptionId = 1,
            OriginOrOdfi = "00001283",
            EffectiveEntryDate = ProcessingDate,
            BatchSequenceNumber = 1
        });
        context.AchTransactions.Add(new AchTransaction
        {
            Id = 900,
            Reference = "CANONICAL-900",
            TransactionExternalId = "CANONICAL-900",
            Type = TransactionTypeEnum.Credit,
            Amount = amount,
            State = AchTransferStateEnum.Pending,
            AchCycleId = cycleId,
            AchBatchId = 900,
            CompanyEntryDescriptionId = 1,
            CompanyName = "PRUEBA",
            CompanyIdentification = "900000000",
            OriginatingDFI = "00001283",
            ReceivingDFI = "99999900",
            TraceNumber = "000012830000900",
            SourceInstitutionId = 1,
            DestinationInstitutionId = 2,
            SourceAccountNumber = "100",
            DestinationAccountNumber = "200",
            RecipientIdNumber = "900000001",
            EffectiveEntryDate = ProcessingDate,
            StateChangedAtUtc = DateTime.SpecifyKind(ProcessingDate, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();
    }

    private static async Task<SeedIds> SeedBaseAsync(AchDbContext context)
    {
        context.ClearingHouseConfigs.AddRange(
            new ClearingHouseConfig { Id = 1, ClearingHouseId = 1, HolidayStrategy = "Colombian" },
            new ClearingHouseConfig { Id = 2, ClearingHouseId = 2, HolidayStrategy = "Colombian" });
        context.ClearingHouses.AddRange(
            new ClearingHouse { Id = 1, ClearingHouseId = 1, Code = "ACHCOL", Name = "ACH Colombia", OriginCode = "ACH" },
            new ClearingHouse { Id = 2, ClearingHouseId = 2, Code = "CENIT", Name = "CENIT", OriginCode = "CEN" });
        var origin = new FinancialInstitution { Id = 1, Name = "Origen", RoutingNumber = "00001", TransitCode = "001", IsDefaultSource = true };
        var destination = new FinancialInstitution { Id = 2, Name = "Destino", RoutingNumber = "00002", TransitCode = "002" };
        origin.CalculateCheckDigit();
        destination.CalculateCheckDigit();
        context.FinancialInstitutions.AddRange(origin, destination);
        context.ClearingHouseCycleConfigs.AddRange(
            Config(11, 1, "Ciclo canónico", true, ProcessingDate.AddDays(-30), ProcessingDate.AddDays(30), 8),
            Config(12, 1, "Alterno", true, ProcessingDate.AddDays(-30), ProcessingDate.AddDays(30), 12),
            Config(13, 2, "Otra cámara", true, ProcessingDate.AddDays(-30), ProcessingDate.AddDays(30), 8),
            Config(14, 1, "Inactivo", false, ProcessingDate.AddDays(-30), ProcessingDate.AddDays(30), 16),
            Config(15, 1, "Futuro", true, ProcessingDate.AddDays(10), null, 8));
        await context.SaveChangesAsync();
        return new SeedIds(1, 2, 11, 12, 13, 14, 15);
    }

    private static ClearingHouseCycleConfig Config(int id, int houseId, string name, bool active, DateTime from, DateTime? to, int start) => new()
    {
        Id = id, ClearingHouseId = houseId, CycleName = name, IsActive = active,
        EffectiveFrom = from, EffectiveTo = to, StartTime = TimeSpan.FromHours(start),
        EndTime = TimeSpan.FromHours(start + 2), CutoffTime = TimeSpan.FromHours(start + 1)
    };

    private static AchCycleAppService CreateService(AchDbContext context)
    {
        MapperBootstrapper.Configure(NullLoggerFactory.Instance);
        return new AchCycleAppService(context, MapperBootstrapper.Instance);
    }

    private static Fixture CreateFixture()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options);
        context.Database.EnsureCreated();
        return new Fixture(connection, context);
    }

    private sealed record SeedIds(
        int PrimaryHouseId,
        int OtherHouseId,
        int PrimaryConfigId,
        int AlternateConfigId,
        int OtherHouseConfigId,
        int InactiveConfigId,
        int FutureConfigId);

    private sealed class Fixture(SqliteConnection connection, AchDbContext context) : IAsyncDisposable
    {
        public AchDbContext Context { get; } = context;
        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
