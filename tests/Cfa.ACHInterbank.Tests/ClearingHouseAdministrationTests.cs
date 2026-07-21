using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class ClearingHouseAdministrationTests
{
    [Fact]
    public async Task Create_NormalizesAndRejectsCaseInsensitiveDuplicate()
    {
        await using var scope = await TestScope.CreateAsync();
        var created = await scope.Service.CreateAsync(Request("  nuevared  "));

        Assert.Equal("NUEVARED", created.Code);
        Assert.False(created.IsActive);
        Assert.Equal(3, await scope.Context.ClearingHouses.CountAsync());
        await Assert.ThrowsAsync<ClearingHouseConflictException>(() => scope.Service.CreateAsync(Request("nuevared")));
    }

    [Fact]
    public async Task Activation_RequiresCurrentCycle_AndOperationalQueryHonorsStatus()
    {
        await using var scope = await TestScope.CreateAsync();
        var created = await scope.Service.CreateAsync(Request("NUEVARED"));
        var error = await Assert.ThrowsAsync<ClearingHouseValidationException>(() => scope.Service.ChangeStatusAsync(created.Id, true));
        Assert.Contains(error.MissingRequirements, x => x.Contains("ciclo", StringComparison.OrdinalIgnoreCase));

        scope.Context.ClearingHouseCycleConfigs.Add(new ClearingHouseCycleConfig
        {
            ClearingHouseId = created.Id, CycleName = "Ciclo propio", IsActive = true,
            StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(17), CutoffTime = TimeSpan.FromHours(16),
            EffectiveFrom = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(-1), DateTimeKind.Utc)
        });
        await scope.Context.SaveChangesAsync();

        var active = await scope.Service.ChangeStatusAsync(created.Id, true);
        Assert.True(active.IsActive);
        Assert.Contains(await scope.Service.GetOperationalAsync(), x => x.Id == created.Id);
        await scope.Service.ChangeStatusAsync(created.Id, false);
        Assert.DoesNotContain(await scope.Service.GetOperationalAsync(), x => x.Id == created.Id);
        Assert.NotNull(await scope.Service.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task Readiness_RequiresRegisteredPaymentRail_AndUnknownIsRejected()
    {
        await using var scope = await TestScope.CreateAsync();
        var withoutRail = await scope.Service.CreateAsync(Request("NUEVARED", paymentRailCode: null));

        Assert.False(withoutRail.IsReady);
        Assert.Contains("Estrategia operativa registrada", withoutRail.MissingRequirements);
        var activation = await Assert.ThrowsAsync<ClearingHouseValidationException>(
            () => scope.Service.ChangeStatusAsync(withoutRail.Id, true));
        Assert.Contains("Estrategia operativa registrada", activation.MissingRequirements);

        var unknown = Request("OTRARED", "NO_REGISTRADA");
        var validation = await Assert.ThrowsAsync<ClearingHouseValidationException>(
            () => scope.Service.CreateAsync(unknown));
        Assert.Contains(validation.MissingRequirements, x => x.Contains("no está registrada", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RegisteredPaymentRail_WithCurrentCycle_IsReadyAndOperationalOnlyWhileActive()
    {
        await using var scope = await TestScope.CreateAsync();
        var created = await scope.Service.CreateAsync(Request("NUEVARED", PaymentRailCodes.Cenit));
        scope.Context.ClearingHouseCycleConfigs.Add(CurrentCycle(created.Id));
        await scope.Context.SaveChangesAsync();

        var readiness = await scope.Service.GetReadinessAsync(created.Id);
        Assert.True(readiness.IsReady);
        var active = await scope.Service.ChangeStatusAsync(created.Id, true);
        Assert.Equal(PaymentRailCodes.Cenit, active.PaymentRailCode);
        Assert.Contains(await scope.Service.GetOperationalAsync(), x => x.Id == created.Id);

        await scope.Service.ChangeStatusAsync(created.Id, false);
        Assert.DoesNotContain(await scope.Service.GetOperationalAsync(), x => x.Id == created.Id);
    }

    [Fact]
    public async Task PaymentRailOptions_ExcludeUnknown_AndUpdateIsAudited()
    {
        await using var scope = await TestScope.CreateAsync();
        Assert.Equal([PaymentRailCodes.AchColombia, PaymentRailCodes.Cenit],
            scope.Service.GetPaymentRailOptions().Select(x => x.Code).OrderBy(x => x).ToArray());

        var created = await scope.Service.CreateAsync(Request("NUEVARED", PaymentRailCodes.AchColombia));
        var before = created.UpdatedAt;
        await Task.Delay(2);
        var updated = await scope.Service.UpdateAsync(created.Id, new UpdateClearingHouseRequest
        {
            Code = created.Code,
            Name = created.Name,
            OriginCode = created.OriginCode,
            TimeZoneId = created.TimeZoneId,
            HolidayStrategy = created.HolidayStrategy!,
            PaymentRailCode = PaymentRailCodes.Cenit,
            ExpectedUpdatedAt = before
        });

        Assert.Equal(PaymentRailCodes.Cenit, updated.PaymentRailCode);
        Assert.True(updated.UpdatedAt > before);
    }

    [Fact]
    public async Task Update_BlocksCodeChangeAfterCycleAndKeepsIdentity()
    {
        await using var scope = await TestScope.CreateAsync();
        var created = await scope.Service.CreateAsync(Request("NUEVARED"));
        scope.Context.ClearingHouseCycleConfigs.Add(new ClearingHouseCycleConfig
        {
            ClearingHouseId = created.Id, CycleName = "Ciclo propio", IsActive = true,
            StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(17), CutoffTime = TimeSpan.FromHours(16),
            EffectiveFrom = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc)
        });
        await scope.Context.SaveChangesAsync();

        var request = new UpdateClearingHouseRequest { Code = "OTRARED", Name = "Otro nombre", OriginCode = "900", TimeZoneId = "America/Bogota", HolidayStrategy = "Colombian", PaymentRailCode = PaymentRailCodes.AchColombia };
        await Assert.ThrowsAsync<ClearingHouseConflictException>(() => scope.Service.UpdateAsync(created.Id, request));
        Assert.Equal("NUEVARED", (await scope.Service.GetByIdAsync(created.Id))!.Code);
    }

    private static CreateClearingHouseRequest Request(string code, string? paymentRailCode = PaymentRailCodes.AchColombia) => new()
    {
        Code = code, Name = "Nueva Red de Pruebas", OriginCode = "900", TimeZoneId = "America/Bogota", HolidayStrategy = "Colombian", PaymentRailCode = paymentRailCode
    };

    private static ClearingHouseCycleConfig CurrentCycle(int clearingHouseId) => new()
    {
        ClearingHouseId = clearingHouseId,
        CycleName = "Ciclo propio",
        IsActive = true,
        StartTime = TimeSpan.FromHours(8),
        EndTime = TimeSpan.FromHours(17),
        CutoffTime = TimeSpan.FromHours(16),
        EffectiveFrom = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(-1), DateTimeKind.Utc)
    };

    private sealed class TestScope : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AchDbContext Context { get; }
        public ClearingHouseService Service { get; }
        private TestScope(SqliteConnection connection, AchDbContext context)
        {
            _connection = connection;
            Context = context;
            IPaymentRailOperationalStrategy[] strategies =
            [
                new AchColombiaPaymentRailOperationalStrategy(),
                new CenitPaymentRailOperationalStrategy(),
                new UnknownPaymentRailOperationalStrategy()
            ];
            Service = new(context, strategies);
        }
        public static async Task<TestScope> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:"); await connection.OpenAsync();
            var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options);
            await context.Database.EnsureCreatedAsync();
            context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, ClearingHouseId = 1, HolidayStrategy = "Colombian", TimeZoneId = "America/Bogota" });
            context.ClearingHouses.AddRange(
                new ClearingHouse { Id = 1, Code = "ACHCOL", Name = "ACH Colombia", OriginCode = "1", IsActive = true, ClearingHouseId = 1 },
                new ClearingHouse { Id = 2, Code = "CENIT", Name = "CENIT", OriginCode = "2", IsActive = true, ClearingHouseId = 1 });
            await context.SaveChangesAsync();
            return new TestScope(connection, context);
        }
        public async ValueTask DisposeAsync() { await Context.DisposeAsync(); await _connection.DisposeAsync(); }
    }
}
