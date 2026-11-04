using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColombiaAutomaticReturnSettingsI01Tests
{
    [Fact]
    public async Task DefaultsAreDisabled_AndCenitIsIsolated()
    {
        await using var db = NewContext();
        await SeedAsync(db);
        var service = new AchColombiaAutomaticReturnSettingsService(db);

        var initial = await service.GetAsync();
        Assert.Equal(AchColombiaAutomaticReturnSettingsService.Disabled, initial.Mode);
        Assert.False(initial.ProcessingConnected);

        var updated = await service.UpdateAsync(new(AchColombiaAutomaticReturnSettingsService.WithApproval));
        Assert.Equal(AchColombiaAutomaticReturnSettingsService.WithApproval, updated.Mode);
        Assert.False(updated.ProcessingConnected);

        Assert.Equal(AchColombiaAutomaticReturnSettingsService.Disabled,
            (await db.ClearingHouseConfigs.AsNoTracking().SingleAsync(x => x.ClearingHouseId == 2)).AutomaticReturnMode);
        Assert.Equal(1, await db.ClearingHouseConfigs.CountAsync(x =>
            x.AutomaticReturnMode == AchColombiaAutomaticReturnSettingsService.WithApproval));
        Assert.Contains(db.AuditLogs, row => row.EntityName == nameof(ClearingHouseConfig)
            && row.ChangedFields != null && row.ChangedFields.Contains(nameof(ClearingHouseConfig.AutomaticReturnMode))
            && row.BeforeJson != null && row.AfterJson != null);
    }

    [Fact]
    public async Task SettingsPersistAcrossContexts_AndCanBeDeactivated()
    {
        var database = $"achcol-automatic-returns-{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(database).Options;
        await using (var db = new AchDbContext(options))
        {
            await SeedAsync(db);
            var service = new AchColombiaAutomaticReturnSettingsService(db);
            await service.UpdateAsync(new(AchColombiaAutomaticReturnSettingsService.Automatic));
        }

        await using (var db = new AchDbContext(options))
        {
            var service = new AchColombiaAutomaticReturnSettingsService(db);
            Assert.Equal(AchColombiaAutomaticReturnSettingsService.Automatic, (await service.GetAsync()).Mode);
            await service.UpdateAsync(new(AchColombiaAutomaticReturnSettingsService.Disabled));
        }

        await using (var db = new AchDbContext(options))
        {
            var service = new AchColombiaAutomaticReturnSettingsService(db);
            Assert.Equal(AchColombiaAutomaticReturnSettingsService.Disabled, (await service.GetAsync()).Mode);
            Assert.Equal(AchColombiaAutomaticReturnSettingsService.Disabled,
                (await db.ClearingHouseConfigs.SingleAsync(x => x.ClearingHouseId == 2)).AutomaticReturnMode);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("automatico")]
    [InlineData("MANUAL")]
    [InlineData("AUTOMATICO_CENIT")]
    public async Task InvalidModeDoesNotModifyPersistedValue(string mode)
    {
        await using var db = NewContext();
        await SeedAsync(db);
        var service = new AchColombiaAutomaticReturnSettingsService(db);
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(new(mode)));
        Assert.Equal(AchColombiaAutomaticReturnSettingsService.Disabled, (await service.GetAsync()).Mode);
    }

    [Fact]
    public async Task MissingOwnConfigFailsClosedWithoutChangingOtherChamber()
    {
        await using var db = NewContext();
        await SeedAsync(db);
        (await db.ClearingHouses.SingleAsync(x => x.Id == 1)).ClearingHouseId = 102;
        await db.SaveChangesAsync();
        var service = new AchColombiaAutomaticReturnSettingsService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(new(AchColombiaAutomaticReturnSettingsService.Automatic)));
        Assert.Equal(AchColombiaAutomaticReturnSettingsService.Disabled,
            (await db.ClearingHouseConfigs.SingleAsync(x => x.ClearingHouseId == 2)).AutomaticReturnMode);
    }

    [Fact]
    public void ControllerRequiresExistingConfigurationPermissions()
    {
        var controller = typeof(AchColombiaAutomaticReturnSettingsController);
        Assert.NotNull(Attribute.GetCustomAttribute(controller, typeof(AuthorizeAttribute)));
        Assert.Equal(P1Policies.ConfigRead,
            controller.GetMethod(nameof(AchColombiaAutomaticReturnSettingsController.Get))!
                .GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .Cast<AuthorizeAttribute>().Single().Policy);
        Assert.Equal(P1Policies.ConfigManage,
            controller.GetMethod(nameof(AchColombiaAutomaticReturnSettingsController.Update))!
                .GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .Cast<AuthorizeAttribute>().Single().Policy);
        Assert.Equal("api/ach-colombia/devoluciones-automaticas/configuracion",
            controller.GetCustomAttributes(typeof(RouteAttribute), false)
                .Cast<RouteAttribute>().Single().Template);
    }

    private static AchDbContext NewContext() => new(
        new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task SeedAsync(AchDbContext db)
    {
        db.ClearingHouseConfigs.AddRange(
            new ClearingHouseConfig { Id = 101, ClearingHouseId = 1, TimeZoneId = "America/Bogota" },
            new ClearingHouseConfig { Id = 102, ClearingHouseId = 2, TimeZoneId = "America/Bogota" });
        db.ClearingHouses.AddRange(
            new ClearingHouse { Id = 1, ClearingHouseId = 101, Code = RegulatoryCycleScheduleCatalog.AchColombiaCode, Name = "ACH Colombia", OriginCode = "TEST1" },
            new ClearingHouse { Id = 2, ClearingHouseId = 102, Code = RegulatoryCycleScheduleCatalog.CenitCode, Name = "CENIT", OriginCode = "TEST2" });
        await db.SaveChangesAsync();
    }
}
