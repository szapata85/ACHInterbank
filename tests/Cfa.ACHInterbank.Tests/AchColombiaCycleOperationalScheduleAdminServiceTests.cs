using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColombiaCycleOperationalScheduleAdminServiceTests
{
    [Fact]
    public async Task Crud_PersistsReadsUpdatesUsesOptimisticConcurrencyAndDeactivates()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 731, 1731, RegulatoryCycleScheduleCatalog.AchColombiaCode);
        AddCycle(context, 731, 2701, "ACHCOL-HISTORICAL", "Ciclo 1", isActive: true);
        await context.SaveChangesAsync();
        var sut = new AchColombiaCycleOperationalScheduleAdminService(context, Mock.Of<IAchColombiaOperationalScheduleSynchronizer>());

        var created = await sut.CreateAsync(new(
            ClearingHouseId: 731,
            ClearingHouseCycleConfigId: 2701,
            AutomaticOutboundEnabled: true,
            AutomaticOutboundStartTime: new TimeSpan(6, 30, 0),
            AutomaticOutboundLatestStartTime: null,
            OutboundExecutionStrategy: AchColombiaOutboundExecutionStrategy.ExactTime,
            EffectiveTimeZoneId: RegulatoryCycleScheduleCatalog.BogotaTimeZoneId));

        Assert.Equal(731, created.ClearingHouseId);
        Assert.Equal(2701, created.ClearingHouseCycleConfigId);
        Assert.Equal(new TimeSpan(6, 30, 0), created.AutomaticOutboundStartTime);
        Assert.True(created.IsActive);
        Assert.False(string.IsNullOrWhiteSpace(created.RowVersion));
        Assert.NotEqual(default, created.CreatedAt);
        Assert.NotEqual(default, created.UpdatedAt);

        var persisted = await context.AchColombiaCycleOperationalSchedules.AsNoTracking().SingleAsync();
        Assert.Equal(731, persisted.ClearingHouseId);
        Assert.Equal(2701, persisted.ClearingHouseCycleConfigId);
        Assert.Equal(16, persisted.RowVersion.Length);

        var detail = await sut.GetByCycleAsync(2701);
        Assert.NotNull(detail);
        Assert.Equal(created.Id, detail!.Id);

        var updated = await sut.UpdateAsync(created.Id, new(
            ClearingHouseId: 731,
            ClearingHouseCycleConfigId: 2701,
            AutomaticOutboundEnabled: true,
            AutomaticOutboundStartTime: new TimeSpan(6, 35, 0),
            AutomaticOutboundLatestStartTime: new TimeSpan(6, 50, 0),
            OutboundExecutionStrategy: AchColombiaOutboundExecutionStrategy.Window,
            EffectiveTimeZoneId: RegulatoryCycleScheduleCatalog.BogotaTimeZoneId,
            IsActive: true,
            ExpectedRowVersion: created.RowVersion));

        Assert.NotNull(updated);
        Assert.Equal(new TimeSpan(6, 35, 0), updated!.AutomaticOutboundStartTime);
        Assert.NotEqual(created.RowVersion, updated.RowVersion);

        await Assert.ThrowsAsync<AchColombiaCycleOperationalScheduleConcurrencyException>(() =>
            sut.UpdateAsync(created.Id, new(
                731,
                2701,
                true,
                new TimeSpan(6, 40, 0),
                new TimeSpan(6, 50, 0),
                AchColombiaOutboundExecutionStrategy.Window,
                RegulatoryCycleScheduleCatalog.BogotaTimeZoneId,
                true,
                created.RowVersion)));

        var deactivated = await sut.DeactivateAsync(
            created.Id,
            new(updated.RowVersion));
        Assert.NotNull(deactivated);
        Assert.False(deactivated!.IsActive);
    }

    [Fact]
    public async Task Create_RejectsDuplicateNonAchColombiaAndCycleFromAnotherHouse()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 731, 1731, RegulatoryCycleScheduleCatalog.AchColombiaCode);
        SeedHouse(context, 947, 1947, RegulatoryCycleScheduleCatalog.CenitCode);
        AddCycle(context, 731, 2701, "ACHCOL-HISTORICAL", "Ciclo 1", isActive: true);
        AddCycle(context, 947, 3701, "CENIT-HISTORICAL", "Ciclo 1", isActive: true);
        await context.SaveChangesAsync();
        var sut = new AchColombiaCycleOperationalScheduleAdminService(context, Mock.Of<IAchColombiaOperationalScheduleSynchronizer>());

        await sut.CreateAsync(ValidRequest(731, 2701));

        await Assert.ThrowsAsync<AchColombiaCycleOperationalScheduleValidationException>(() =>
            sut.CreateAsync(ValidRequest(731, 2701)));

        var nonAch = await Assert.ThrowsAsync<AchColombiaCycleOperationalScheduleValidationException>(() =>
            sut.CreateAsync(ValidRequest(947, 3701)));
        Assert.Contains("ACH Colombia", nonAch.Message, StringComparison.OrdinalIgnoreCase);

        var crossHouse = await Assert.ThrowsAsync<AchColombiaCycleOperationalScheduleValidationException>(() =>
            sut.CreateAsync(ValidRequest(731, 3701)));
        Assert.Contains("no pertenece", crossHouse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Validation_RejectsInvalidRangeExactTimeWithoutStartAndInvalidTimeZone()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 731, 1731, RegulatoryCycleScheduleCatalog.AchColombiaCode);
        AddCycle(context, 731, 2701, "ACHCOL-HISTORICAL", "Ciclo 1", isActive: true);
        await context.SaveChangesAsync();
        var sut = new AchColombiaCycleOperationalScheduleAdminService(context, Mock.Of<IAchColombiaOperationalScheduleSynchronizer>());

        var range = await Assert.ThrowsAsync<AchColombiaCycleOperationalScheduleValidationException>(() =>
            sut.CreateAsync(new(
                731, 2701, true,
                new TimeSpan(17, 55, 0),
                new TimeSpan(17, 50, 0),
                AchColombiaOutboundExecutionStrategy.Window,
                RegulatoryCycleScheduleCatalog.BogotaTimeZoneId)));
        Assert.Contains("posterior", range.Message, StringComparison.OrdinalIgnoreCase);

        var exact = await Assert.ThrowsAsync<AchColombiaCycleOperationalScheduleValidationException>(() =>
            sut.CreateAsync(new(
                731, 2701, true,
                null, null,
                AchColombiaOutboundExecutionStrategy.ExactTime,
                RegulatoryCycleScheduleCatalog.BogotaTimeZoneId)));
        Assert.Contains("ExactTime", exact.Message, StringComparison.OrdinalIgnoreCase);

        var zone = await Assert.ThrowsAsync<AchColombiaCycleOperationalScheduleValidationException>(() =>
            sut.CreateAsync(new(
                731, 2701, true,
                new TimeSpan(6, 30, 0), null,
                AchColombiaOutboundExecutionStrategy.ExactTime,
                "Not/A_Real_Time_Zone")));
        Assert.Contains("zona horaria", zone.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OperationalModel_RepresentsKnownC1AndC7Times_AllowsC2WithoutInventingTime_AndPreservesRegulation()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 731, 1731, RegulatoryCycleScheduleCatalog.AchColombiaCode);
        for (var cycleNumber = 1; cycleNumber <= 7; cycleNumber++)
        {
            AddCycle(
                context,
                731,
                7100 + cycleNumber,
                RegulatoryCycleScheduleCatalog.AchColombiaSevenCycleCandidateVersion,
                $"Ciclo {cycleNumber}",
                isActive: false);
        }
        await context.SaveChangesAsync();
        var sut = new AchColombiaCycleOperationalScheduleAdminService(context, Mock.Of<IAchColombiaOperationalScheduleSynchronizer>());

        var c1 = await sut.CreateAsync(new(
            731, 7101, true,
            new TimeSpan(6, 30, 0), null,
            AchColombiaOutboundExecutionStrategy.ExactTime,
            RegulatoryCycleScheduleCatalog.BogotaTimeZoneId,
            IsActive: false));

        var pendingC2ToC6 = new List<AchColombiaCycleOperationalScheduleDto>();
        for (var cycleNumber = 2; cycleNumber <= 6; cycleNumber++)
        {
            pendingC2ToC6.Add(await sut.CreateAsync(new(
                731, 7100 + cycleNumber, false,
                null, null,
                AchColombiaOutboundExecutionStrategy.Window,
                RegulatoryCycleScheduleCatalog.BogotaTimeZoneId,
                IsActive: false)));
        }

        var c7 = await sut.CreateAsync(new(
            731, 7107, true,
            null, new TimeSpan(17, 50, 0),
            AchColombiaOutboundExecutionStrategy.Window,
            RegulatoryCycleScheduleCatalog.BogotaTimeZoneId,
            IsActive: false));

        Assert.Equal(new TimeSpan(6, 30, 0), c1.AutomaticOutboundStartTime);
        Assert.All(pendingC2ToC6, schedule =>
        {
            Assert.False(schedule.AutomaticOutboundEnabled);
            Assert.Null(schedule.AutomaticOutboundStartTime);
            Assert.Null(schedule.AutomaticOutboundLatestStartTime);
            Assert.False(schedule.IsActive);
        });
        Assert.Equal(new TimeSpan(17, 50, 0), c7.AutomaticOutboundLatestStartTime);
        Assert.False(c1.IsActive);
        Assert.False(c7.IsActive);

        var candidate = RegulatoryCycleScheduleCatalog.AchColombiaSevenCycleCandidate;
        Assert.False(candidate.IsActivatable);
        Assert.Null(candidate.EffectiveFrom);
        Assert.Equal(new TimeSpan(7, 0, 0), candidate.Cycles.Single(x => x.CycleNumber == 1).CutoffTime);
        Assert.Equal(new TimeSpan(18, 0, 0), candidate.Cycles.Single(x => x.CycleNumber == 7).CutoffTime);
        Assert.Null(candidate.Cycles.Single(x => x.CycleNumber == 4).StartTime);
    }

    [Fact]
    public async Task ActiveSchedule_CannotBeCreatedForInactiveRegulatoryCycleConfig()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 731, 1731, RegulatoryCycleScheduleCatalog.AchColombiaCode);
        AddCycle(context, 731, 7107, RegulatoryCycleScheduleCatalog.AchColombiaSevenCycleCandidateVersion, "Ciclo 7", isActive: false);
        await context.SaveChangesAsync();
        var sut = new AchColombiaCycleOperationalScheduleAdminService(context, Mock.Of<IAchColombiaOperationalScheduleSynchronizer>());

        var error = await Assert.ThrowsAsync<AchColombiaCycleOperationalScheduleValidationException>(() =>
            sut.CreateAsync(new(
                731, 7107, true,
                null, new TimeSpan(17, 50, 0),
                AchColombiaOutboundExecutionStrategy.Window,
                RegulatoryCycleScheduleCatalog.BogotaTimeZoneId,
                IsActive: true)));

        Assert.Contains("inactiva", error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task CreateUpdateDeactivate_ShouldPersistFirst_ThenRequestOutboundResynchronization()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 731, 1731, RegulatoryCycleScheduleCatalog.AchColombiaCode);
        AddCycle(context, 731, 2701, "ACHCOL-HISTORICAL", "Ciclo 1", isActive: true);
        await context.SaveChangesAsync();

        var synchronizer = new Mock<IAchColombiaOperationalScheduleSynchronizer>(MockBehavior.Strict);
        var syncInvocation = 0;
        synchronizer.Setup(x => x.SynchronizeOutboundAsync(It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                syncInvocation++;
                var persisted = context.AchColombiaCycleOperationalSchedules.AsNoTracking().Single();
                if (syncInvocation == 1)
                {
                    Assert.Equal(new TimeSpan(6, 30, 0), persisted.AutomaticOutboundStartTime);
                    Assert.True(persisted.IsActive);
                }
                else if (syncInvocation == 2)
                {
                    Assert.Equal(new TimeSpan(6, 35, 0), persisted.AutomaticOutboundStartTime);
                    Assert.True(persisted.IsActive);
                }
                else
                {
                    Assert.False(persisted.IsActive);
                }
            })
            .Returns(Task.CompletedTask);
        var sut = new AchColombiaCycleOperationalScheduleAdminService(context, synchronizer.Object);

        var created = await sut.CreateAsync(new(
            731,
            2701,
            true,
            new TimeSpan(6, 30, 0),
            null,
            AchColombiaOutboundExecutionStrategy.ExactTime,
            RegulatoryCycleScheduleCatalog.BogotaTimeZoneId));

        var updated = await sut.UpdateAsync(created.Id, new(
            731,
            2701,
            true,
            new TimeSpan(6, 35, 0),
            new TimeSpan(6, 50, 0),
            AchColombiaOutboundExecutionStrategy.Window,
            RegulatoryCycleScheduleCatalog.BogotaTimeZoneId,
            true,
            created.RowVersion));
        Assert.NotNull(updated);

        await sut.DeactivateAsync(created.Id, new(updated!.RowVersion));

        synchronizer.Verify(x => x.SynchronizeOutboundAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task AdministrationReadModel_UsesLatestPersistedPolicyAndProjectsRegulatoryWindowReadOnly()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 731, 1731, RegulatoryCycleScheduleCatalog.AchColombiaCode);

        AddCycle(context, 731, 2701, "ACHCOL-HISTORICAL", "Ciclo 1", isActive: true);
        var historical = context.ClearingHouseCycleConfigs.Local.Single(x => x.Id == 2701);
        historical.EffectiveFrom = new DateTime(2020, 1, 1);
        historical.StartTime = new TimeSpan(7, 1, 0);
        historical.CutoffTime = new TimeSpan(9, 0, 0);

        AddCycle(context, 731, 7101, "ACHCOL-PREP", "Ciclo 1", isActive: false);
        AddCycle(context, 731, 7107, "ACHCOL-PREP", "Ciclo 7", isActive: false);
        foreach (var cycle in context.ClearingHouseCycleConfigs.Local.Where(x => x.PolicyVersion == "ACHCOL-PREP"))
        {
            cycle.EffectiveFrom = new DateTime(2099, 1, 1);
        }
        var c1Config = context.ClearingHouseCycleConfigs.Local.Single(x => x.Id == 7101);
        c1Config.StartTime = new TimeSpan(19, 1, 0);
        c1Config.CutoffTime = new TimeSpan(7, 0, 0);
        var c7Config = context.ClearingHouseCycleConfigs.Local.Single(x => x.Id == 7107);
        c7Config.StartTime = new TimeSpan(16, 1, 0);
        c7Config.CutoffTime = new TimeSpan(18, 0, 0);
        await context.SaveChangesAsync();

        var sut = new AchColombiaCycleOperationalScheduleAdminService(context, Mock.Of<IAchColombiaOperationalScheduleSynchronizer>());
        await sut.CreateAsync(new(
            731, 7101, true, new TimeSpan(6, 30, 0), null,
            AchColombiaOutboundExecutionStrategy.ExactTime,
            RegulatoryCycleScheduleCatalog.BogotaTimeZoneId,
            IsActive: false));
        await sut.CreateAsync(new(
            731, 7107, true, null, new TimeSpan(17, 50, 0),
            AchColombiaOutboundExecutionStrategy.Window,
            RegulatoryCycleScheduleCatalog.BogotaTimeZoneId,
            IsActive: false));

        var rows = await sut.GetAdministrationAsync();

        Assert.Equal(2, rows.Count);
        Assert.All(rows, row => Assert.Equal("ACHCOL-PREP", row.PolicyVersion));
        var c1 = rows.Single(x => x.CycleName == "Ciclo 1");
        var c7 = rows.Single(x => x.CycleName == "Ciclo 7");
        Assert.Equal(new TimeSpan(19, 1, 0), c1.RegulatoryStartTime);
        Assert.Equal(new TimeSpan(7, 0, 0), c1.RegulatoryCutoffTime);
        Assert.Equal(new TimeSpan(6, 30, 0), c1.AutomaticOutboundStartTime);
        Assert.Equal(new TimeSpan(18, 0, 0), c7.RegulatoryCutoffTime);
        Assert.Equal(new TimeSpan(17, 50, 0), c7.AutomaticOutboundLatestStartTime);
        Assert.False(c1.RegulatoryCycleActive);
        Assert.False(c7.RegulatoryCycleActive);
    }

    [Fact]
    public async Task EfModel_HasRequiredForeignKeysUniqueCycleConstraintAuditAndConcurrency()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);

        var entity = context.Model.FindEntityType(typeof(AchColombiaCycleOperationalSchedule));
        Assert.NotNull(entity);
        Assert.True(entity!.FindProperty(nameof(AchColombiaCycleOperationalSchedule.RowVersion))!.IsConcurrencyToken);
        Assert.NotNull(entity.FindProperty(nameof(AchColombiaCycleOperationalSchedule.CreatedAt)));
        Assert.NotNull(entity.FindProperty(nameof(AchColombiaCycleOperationalSchedule.UpdatedAt)));
        Assert.Equal(2, entity.GetForeignKeys().Count());

        var unique = entity.GetIndexes().Single(index => index.GetDatabaseName() == "UX_AchColombiaCycleOperationalSchedules_House_Cycle");
        Assert.True(unique.IsUnique);
        Assert.Equal(
            new[] { nameof(AchColombiaCycleOperationalSchedule.ClearingHouseId), nameof(AchColombiaCycleOperationalSchedule.ClearingHouseCycleConfigId) },
            unique.Properties.Select(property => property.Name));
    }

    private static CreateAchColombiaCycleOperationalScheduleRequest ValidRequest(int houseId, int cycleConfigId)
        => new(
            houseId,
            cycleConfigId,
            true,
            new TimeSpan(6, 30, 0),
            null,
            AchColombiaOutboundExecutionStrategy.ExactTime,
            RegulatoryCycleScheduleCatalog.BogotaTimeZoneId);

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    private static AchDbContext CreateContext(SqliteConnection connection)
    {
        var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options);
        context.Database.EnsureCreated();
        return context;
    }

    private static void SeedHouse(AchDbContext context, int houseId, int configId, string code)
    {
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = configId,
            ClearingHouseId = houseId,
            TimeZoneId = RegulatoryCycleScheduleCatalog.BogotaTimeZoneId,
            HolidayStrategy = "Colombian"
        });
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = houseId,
            ClearingHouseId = configId,
            Code = code,
            Name = code,
            OriginCode = $"{houseId:000000000}",
            IsActive = true
        });
    }

    private static void AddCycle(
        AchDbContext context,
        int houseId,
        int id,
        string policyVersion,
        string cycleName,
        bool isActive)
    {
        context.ClearingHouseCycleConfigs.Add(new ClearingHouseCycleConfig
        {
            Id = id,
            ClearingHouseId = houseId,
            PolicyVersion = policyVersion,
            CycleName = cycleName,
            StartTime = new TimeSpan(0, 0, 0),
            EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = new TimeSpan(23, 59, 0),
            OutputReleaseTime = new TimeSpan(23, 59, 0),
            EffectiveFrom = RegulatoryCycleScheduleCatalog.HistoricalBootstrapEffectiveFrom,
            IsActive = isActive
        });
    }
}
