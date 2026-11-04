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

/// <summary>Regresión I01-CENIT: configuración, aislamiento, autorización declarativa, auditoría y concurrencia.</summary>
public sealed class CenitAutomaticReturnSettingsI01Tests
{
    [Fact]
    public async Task InitiallyDisabled_AndReadNeverReportsConnectedProcessing()
    {
        await using var db = NewContext();
        await SeedAsync(db);
        var result = await new CenitAutomaticReturnSettingsService(db).GetAsync();
        Assert.Equal(CenitAutomaticReturnSettingsService.Disabled, result.Mode);
        Assert.False(result.ProcessingConnected);
    }

    [Fact]
    public async Task EveryTransitionPersistsWithAudit_AndDoesNotAffectAchColombia()
    {
        var options = NewOptions();
        await using (var db = new AchDbContext(options))
        {
            await SeedAsync(db);
            var ach = new AchColombiaAutomaticReturnSettingsService(db);
            await ach.UpdateAsync(new(AchColombiaAutomaticReturnSettingsService.Automatic));
        }

        var mode = CenitAutomaticReturnSettingsService.Disabled;
        foreach (var next in new[]
        {
            CenitAutomaticReturnSettingsService.WithApproval,
            CenitAutomaticReturnSettingsService.Automatic,
            CenitAutomaticReturnSettingsService.Disabled
        })
        {
            await using (var db = new AchDbContext(options))
            {
                var service = new CenitAutomaticReturnSettingsService(db);
                Assert.Equal(mode, (await service.GetAsync()).Mode);
                var result = await service.UpdateAsync(new(next, mode));
                Assert.Equal(next, result.Mode);
                Assert.False(result.ProcessingConnected);
                Assert.Equal(AchColombiaAutomaticReturnSettingsService.Automatic,
                    (await new AchColombiaAutomaticReturnSettingsService(db).GetAsync()).Mode);
                Assert.Equal(AchColombiaAutomaticReturnSettingsService.Automatic,
                    (await db.ClearingHouseConfigs.SingleAsync(c => c.ClearingHouseId == 1)).AutomaticReturnMode);
                Assert.Contains(db.AuditLogs, log =>
                    log.EntityName == nameof(ClearingHouseConfig)
                    && log.ChangedFields != null
                    && log.ChangedFields.Contains(nameof(ClearingHouseConfig.AutomaticReturnMode))
                    && log.BeforeJson != null && log.AfterJson != null);
            }
            mode = next;
        }
        await using (var db = new AchDbContext(options))
        {
            Assert.Equal(CenitAutomaticReturnSettingsService.Disabled,
                (await new CenitAutomaticReturnSettingsService(db).GetAsync()).Mode);
            Assert.Equal(3, await db.AuditLogs.CountAsync(log =>
                log.EntityName == nameof(ClearingHouseConfig)
                && log.EntityId == "102"
                && log.ChangedFields != null
                && log.ChangedFields.Contains(nameof(ClearingHouseConfig.AutomaticReturnMode))));
        }
    }

    [Theory]
    [InlineData("", "DESACTIVADO")]
    [InlineData("automatico", "DESACTIVADO")]
    [InlineData("MANUAL", "DESACTIVADO")]
    [InlineData("AUTOMATICO_CENIT", "DESACTIVADO")]
    [InlineData("AUTOMATICO", "invalid")]
    [InlineData("AUTOMATICO", "")]
    public async Task InvalidModesAndExpectedModesAreRejectedWithoutChanges(string mode, string expected)
    {
        await using var db = NewContext();
        await SeedAsync(db);
        var service = new CenitAutomaticReturnSettingsService(db);
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync(new(mode, expected)));
        Assert.Equal(CenitAutomaticReturnSettingsService.Disabled, (await service.GetAsync()).Mode);
        Assert.DoesNotContain(db.AuditLogs, log => log.ChangedFields != null &&
            log.ChangedFields.Contains(nameof(ClearingHouseConfig.AutomaticReturnMode)));
    }

    [Fact]
    public async Task StaleExpectedModeYieldsConflict_WithoutOverwritingOtherChanges()
    {
        await using var db = NewContext();
        await SeedAsync(db);
        var service = new CenitAutomaticReturnSettingsService(db);
        await service.UpdateAsync(new(CenitAutomaticReturnSettingsService.WithApproval,
            CenitAutomaticReturnSettingsService.Disabled));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            service.UpdateAsync(new(CenitAutomaticReturnSettingsService.Automatic,
                CenitAutomaticReturnSettingsService.Disabled)));
        Assert.Equal(CenitAutomaticReturnSettingsService.WithApproval, (await service.GetAsync()).Mode);
    }

    [Fact]
    public async Task ConcurrentTrackedWritesFailByEntityConcurrencyToken()
    {
        var opts = NewOptions();
        await using (var db = new AchDbContext(opts)) await SeedAsync(db);
        await using var first = new AchDbContext(opts);
        await using var second = new AchDbContext(opts);
        var firstConfig = await first.ClearingHouseConfigs.SingleAsync(c => c.ClearingHouseId == 2);
        var secondConfig = await second.ClearingHouseConfigs.SingleAsync(c => c.ClearingHouseId == 2);
        firstConfig.AutomaticReturnMode = CenitAutomaticReturnSettingsService.WithApproval;
        await first.SaveChangesAsync();
        secondConfig.AutomaticReturnMode = CenitAutomaticReturnSettingsService.Automatic;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        await using var verify = new AchDbContext(opts);
        Assert.Equal(CenitAutomaticReturnSettingsService.WithApproval,
            (await verify.ClearingHouseConfigs.SingleAsync(c => c.ClearingHouseId == 2)).AutomaticReturnMode);
    }

    [Fact]
    public async Task NoOpDoesNotEmitModeChangeAudit()
    {
        await using var db = NewContext();
        await SeedAsync(db);
        var initial = await db.AuditLogs.CountAsync();
        var service = new CenitAutomaticReturnSettingsService(db);
        var result = await service.UpdateAsync(new(CenitAutomaticReturnSettingsService.Disabled,
            CenitAutomaticReturnSettingsService.Disabled));
        Assert.False(result.ProcessingConnected);
        Assert.Equal(initial, await db.AuditLogs.CountAsync());
    }

    [Fact]
    public async Task MissingCenitConfigFailsClosed()
    {
        await using var db = NewContext();
        await SeedAsync(db);
        db.ClearingHouseConfigs.Remove(await db.ClearingHouseConfigs.SingleAsync(c => c.ClearingHouseId == 2));
        await db.SaveChangesAsync();
        var service = new CenitAutomaticReturnSettingsService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(
            new(CenitAutomaticReturnSettingsService.Automatic, CenitAutomaticReturnSettingsService.Disabled)));
        Assert.Equal(CenitAutomaticReturnSettingsService.Disabled,
            (await db.ClearingHouseConfigs.SingleAsync(c => c.ClearingHouseId == 1)).AutomaticReturnMode);
    }

    [Fact]
    public async Task MissingOrSharedConfigurationFailsClosed_WithoutTouchingAchColombia()
    {
        await using var db = NewContext();
        await SeedAsync(db);
        (await db.ClearingHouses.SingleAsync(x => x.Code == RegulatoryCycleScheduleCatalog.CenitCode))
            .ClearingHouseId = 101; // referencia cruzada potencialmente peligrosa
        await db.SaveChangesAsync();
        var service = new CenitAutomaticReturnSettingsService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(new(CenitAutomaticReturnSettingsService.Automatic,
                CenitAutomaticReturnSettingsService.Disabled)));
        Assert.Equal(CenitAutomaticReturnSettingsService.Disabled,
            (await db.ClearingHouseConfigs.SingleAsync(x => x.ClearingHouseId == 1)).AutomaticReturnMode);
    }

    [Fact]
    public async Task ChangingCenitDoesNotChangeAchMode_EvenWhenAchIsDisabled()
    {
        await using var db = NewContext();
        await SeedAsync(db);
        var service = new CenitAutomaticReturnSettingsService(db);
        await service.UpdateAsync(new(CenitAutomaticReturnSettingsService.Automatic,
            CenitAutomaticReturnSettingsService.Disabled));
        Assert.Equal(CenitAutomaticReturnSettingsService.Automatic, (await service.GetAsync()).Mode);
        Assert.False((await service.GetAsync()).ProcessingConnected);
        Assert.Equal(AchColombiaAutomaticReturnSettingsService.Disabled,
            (await new AchColombiaAutomaticReturnSettingsService(db).GetAsync()).Mode);
    }

    [Fact]
    public async Task ControllerRejectsInvalidAndStaleChangesWithHttpSemantics()
    {
        await using var db = NewContext();
        await SeedAsync(db);
        var controller = new CenitAutomaticReturnSettingsController(new CenitAutomaticReturnSettingsService(db));
        var invalid = await controller.Update(new("desactivado", "DESACTIVADO"), default);
        Assert.IsType<BadRequestObjectResult>(invalid.Result);
        await controller.Update(new("CON_APROBACION", "DESACTIVADO"), default);
        var stale = await controller.Update(new("AUTOMATICO", "DESACTIVADO"), default);
        Assert.IsType<ConflictObjectResult>(stale.Result);
        Assert.IsType<OkObjectResult>((await controller.Get(default)).Result);
    }

    [Fact]
    public void ReadAndWriteRequireExistingConfigPolicies()
    {
        var controller = typeof(CenitAutomaticReturnSettingsController);
        Assert.NotNull(Attribute.GetCustomAttribute(controller, typeof(AuthorizeAttribute)));
        Assert.Equal(P1Policies.ConfigRead,
            controller.GetMethod(nameof(CenitAutomaticReturnSettingsController.Get))!
                .GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .Cast<AuthorizeAttribute>().Single().Policy);
        Assert.Equal(P1Policies.ConfigManage,
            controller.GetMethod(nameof(CenitAutomaticReturnSettingsController.Update))!
                .GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .Cast<AuthorizeAttribute>().Single().Policy);
        Assert.Equal("api/cenit/devoluciones-automaticas/configuracion",
            controller.GetCustomAttributes(typeof(RouteAttribute), false)
                .Cast<RouteAttribute>().Single().Template);
    }

    private static DbContextOptions<AchDbContext> NewOptions() =>
        new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase($"cenit-config-i01-{Guid.NewGuid():N}").Options;

    private static AchDbContext NewContext() => new(NewOptions());

    private static async Task SeedAsync(AchDbContext db)
    {
        db.ClearingHouseConfigs.AddRange(
            new ClearingHouseConfig { Id = 101, ClearingHouseId = 1, TimeZoneId = "America/Bogota" },
            new ClearingHouseConfig { Id = 102, ClearingHouseId = 2, TimeZoneId = "America/Bogota" });
        db.ClearingHouses.AddRange(
            new ClearingHouse
            {
                Id = 1, ClearingHouseId = 101, Code = RegulatoryCycleScheduleCatalog.AchColombiaCode,
                Name = "ACH Colombia", OriginCode = "TEST1"
            },
            new ClearingHouse
            {
                Id = 2, ClearingHouseId = 102, Code = RegulatoryCycleScheduleCatalog.CenitCode,
                Name = "CENIT", OriginCode = "TEST2"
            });
        await db.SaveChangesAsync();
    }
}
