using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class IntegrationResponseCatalogTests
{
    [Fact]
    public async Task Bootstrapper_SeedsR96ByMethod_AndIsIdempotent()
    {
        await using var fixture = await Fixture.CreateAsync();
        var bootstrapper = new IntegrationCatalogBootstrapper(fixture.Context);

        await bootstrapper.EnsureAsync();
        await bootstrapper.EnsureAsync();

        var codes = await fixture.Context.IntegrationResponseCodes
            .Include(x => x.Method)
            .Where(x => x.Source == IntegrationResponseCategory.CoreSoapResponse && x.Code == "R96")
            .OrderBy(x => x.Method.Code)
            .ToListAsync();

        Assert.Equal(2, codes.Count);
        Assert.Contains(codes, x => x.Method.Code == "WSCFAACH.Proc_Contrapartidas"
            && x.Description == "Débito aplicado correctamente");
        Assert.Contains(codes, x => x.Method.Code == "WSCFAACH.Proc_Transacciones"
            && x.Description == "Crédito aplicado correctamente");
        Assert.All(codes, x =>
        {
            Assert.Equal(IntegrationResponseBusinessStatus.Success, x.BusinessStatus);
            Assert.False(x.RetryAllowed);
            Assert.False(x.RequiresManualReview);
            Assert.True(x.IsActive);
        });
    }

    [Fact]
    public async Task Bootstrapper_UpdatesExistingR96_WithoutReplacingItsId()
    {
        await using var fixture = await Fixture.CreateAsync();
        var bootstrapper = new IntegrationCatalogBootstrapper(fixture.Context);
        await bootstrapper.EnsureAsync();
        var existing = await fixture.Context.IntegrationResponseCodes
            .Include(x => x.Method)
            .SingleAsync(x => x.Method.Code == "WSCFAACH.Proc_Contrapartidas" && x.Code == "R96");
        var id = existing.Id;
        existing.Description = "Obsoleta";
        existing.BusinessStatus = IntegrationResponseBusinessStatus.Rejected;
        existing.RetryAllowed = true;
        existing.RequiresManualReview = true;
        existing.IsActive = false;
        await fixture.Context.SaveChangesAsync();

        await bootstrapper.EnsureAsync();

        var updated = await fixture.Context.IntegrationResponseCodes.SingleAsync(x => x.Id == id);
        Assert.Equal("Débito aplicado correctamente", updated.Description);
        Assert.Equal(IntegrationResponseBusinessStatus.Success, updated.BusinessStatus);
        Assert.False(updated.RetryAllowed);
        Assert.False(updated.RequiresManualReview);
        Assert.True(updated.IsActive);
    }

    [Fact]
    public async Task Resolver_UsesSourceMethodAndCode_AndNormalizesInput()
    {
        await using var fixture = await Fixture.CreateAsync();
        await new IntegrationCatalogBootstrapper(fixture.Context).EnsureAsync();
        var sut = new IntegrationResponseCatalogResolver(fixture.Context);

        var debit = await sut.ResolveAsync(" core_soap_response ", " proc_contrapartidas ", " r96 ", DateTime.UtcNow);
        var credit = await sut.ResolveAsync(IntegrationResponseCategory.CoreSoapResponse, "WSCFAACH.PROC_TRANSACCIONES", "R96", DateTime.UtcNow);

        Assert.True(debit.IsKnownCode);
        Assert.True(credit.IsKnownCode);
        Assert.NotEqual(debit.CatalogId, credit.CatalogId);
        Assert.Equal("Débito aplicado correctamente", debit.Description);
        Assert.Equal("Crédito aplicado correctamente", credit.Description);
        Assert.Equal(IntegrationResponseBusinessStatus.Success, debit.BusinessStatus);
        Assert.Equal(IntegrationResponseBusinessStatus.Success, credit.BusinessStatus);
    }

    [Theory]
    [InlineData("R97")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Resolver_UnknownCode_IsFailClosed(string? code)
    {
        await using var fixture = await Fixture.CreateAsync();
        await new IntegrationCatalogBootstrapper(fixture.Context).EnsureAsync();
        var sut = new IntegrationResponseCatalogResolver(fixture.Context);

        var result = await sut.ResolveAsync(
            IntegrationResponseCategory.CoreSoapResponse,
            "Proc_Contrapartidas",
            code,
            DateTime.UtcNow);

        Assert.False(result.IsKnownCode);
        Assert.Null(result.CatalogId);
        Assert.Equal(IntegrationResponseBusinessStatus.PendingCatalog, result.BusinessStatus);
        Assert.False(result.RetryAllowed);
        Assert.True(result.RequiresManualReview);
    }

    [Fact]
    public async Task Resolver_InactiveOrExpiredCode_IsUnknown()
    {
        await using var fixture = await Fixture.CreateAsync();
        await new IntegrationCatalogBootstrapper(fixture.Context).EnsureAsync();
        var item = await fixture.Context.IntegrationResponseCodes
            .Include(x => x.Method)
            .SingleAsync(x => x.Method.Code == "WSCFAACH.Proc_Contrapartidas" && x.Code == "R96");
        item.IsActive = false;
        await fixture.Context.SaveChangesAsync();

        var result = await new IntegrationResponseCatalogResolver(fixture.Context).ResolveAsync(
            IntegrationResponseCategory.CoreSoapResponse,
            "Proc_Contrapartidas",
            "R96",
            DateTime.UtcNow);

        Assert.False(result.IsKnownCode);
        Assert.Equal(IntegrationResponseBusinessStatus.PendingCatalog, result.BusinessStatus);
    }

    [Fact]
    public async Task CoreR96_DoesNotPolluteAchReturnCatalogs()
    {
        await using var fixture = await Fixture.CreateAsync();
        await new IntegrationCatalogBootstrapper(fixture.Context).EnsureAsync();

        Assert.False(await fixture.Context.AchReturnCodes.AnyAsync(x => x.Code == "R96"));
        Assert.False(await fixture.Context.AchFileRejectionCodes.AnyAsync(x => x.Code == "R96"));
        Assert.False(await fixture.Context.AchResponseStatusMappings.AnyAsync(x => x.CodigoEstadoExterno == "R96"));
        Assert.NotEqual(IntegrationResponseCategory.CoreSoapResponse, IntegrationResponseCategory.AchReturnCause);
        Assert.NotEqual(IntegrationResponseCategory.CoreSoapResponse, IntegrationResponseCategory.AchOperatorReturn);
        Assert.NotEqual(IntegrationResponseCategory.CoreSoapResponse, IntegrationResponseCategory.AchFatalFileError);
        Assert.NotEqual(IntegrationResponseCategory.CoreSoapResponse, IntegrationResponseCategory.AchClaimCause);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private Fixture(SqliteConnection connection, AchDbContext context)
        {
            _connection = connection;
            Context = context;
        }

        public AchDbContext Context { get; }

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options);
            await context.Database.EnsureCreatedAsync();
            return new Fixture(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
