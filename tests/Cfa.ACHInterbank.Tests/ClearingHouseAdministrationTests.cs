using Cfa.ACHInterbank.Application.ACH.Models;
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

        var request = new UpdateClearingHouseRequest { Code = "OTRARED", Name = "Otro nombre", OriginCode = "900", TimeZoneId = "America/Bogota", HolidayStrategy = "Colombian" };
        await Assert.ThrowsAsync<ClearingHouseConflictException>(() => scope.Service.UpdateAsync(created.Id, request));
        Assert.Equal("NUEVARED", (await scope.Service.GetByIdAsync(created.Id))!.Code);
    }

    private static CreateClearingHouseRequest Request(string code) => new()
    {
        Code = code, Name = "Nueva Red de Pruebas", OriginCode = "900", TimeZoneId = "America/Bogota", HolidayStrategy = "Colombian"
    };

    private sealed class TestScope : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AchDbContext Context { get; }
        public ClearingHouseService Service { get; }
        private TestScope(SqliteConnection connection, AchDbContext context) { _connection = connection; Context = context; Service = new(context); }
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
