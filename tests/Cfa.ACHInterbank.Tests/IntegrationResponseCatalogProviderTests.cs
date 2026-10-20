using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class IntegrationResponseCatalogProviderTests
{
    [Fact]
    public async Task SqlServer_SeedR96_IsIdempotent_WhenProviderIsEnabled()
    {
        var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_RESPONSE_CATALOG_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlServer(connectionString).Options;
        await AssertProviderAsync(options);
    }

    [Fact]
    public async Task PostgreSql_SeedR96_IsIdempotent_WhenProviderIsEnabled()
    {
        var connectionString = Environment.GetEnvironmentVariable("POSTGRES_RESPONSE_CATALOG_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var options = new DbContextOptionsBuilder<AchDbContext>().UseNpgsql(connectionString).Options;
        await AssertProviderAsync(options);
    }

    private static async Task AssertProviderAsync(DbContextOptions<AchDbContext> options)
    {
        await using var context = new AchDbContext(options);
        var bootstrapper = new IntegrationCatalogBootstrapper(context);
        await bootstrapper.EnsureAsync();
        await bootstrapper.EnsureAsync();

        var codes = await context.IntegrationResponseCodes
            .AsNoTracking()
            .Include(x => x.Method)
            .Where(x => x.Source == IntegrationResponseCategory.CoreSoapResponse && x.Code == "R96")
            .ToListAsync();

        Assert.Equal(2, codes.Count);
        Assert.Equal(2, codes.Select(x => new { x.Source, x.MethodId, x.Code }).Distinct().Count());
        Assert.Contains(codes, x => x.Method.Code == "WSCFAACH.Proc_Contrapartidas"
            && x.Description == "Débito aplicado correctamente");
        Assert.Contains(codes, x => x.Method.Code == "WSCFAACH.Proc_Transacciones"
            && x.Description == "Crédito aplicado correctamente");
        Assert.All(codes, x =>
        {
            Assert.Equal(IntegrationResponseBusinessStatus.Success, x.BusinessStatus);
            Assert.False(x.RetryAllowed);
            Assert.False(x.RequiresManualReview);
        });

        var resolver = new IntegrationResponseCatalogResolver(context);
        var debit = await resolver.ResolveAsync(
            IntegrationResponseCategory.CoreSoapResponse,
            "  proc_contrapartidas ",
            " r96 ",
            DateTime.UtcNow,
            CancellationToken.None);
        var credit = await resolver.ResolveAsync(
            IntegrationResponseCategory.CoreSoapResponse,
            "PROC_TRANSACCIONES",
            "R96",
            DateTime.UtcNow,
            CancellationToken.None);

        Assert.True(debit.IsKnownCode);
        Assert.Equal("Débito aplicado correctamente", debit.Description);
        Assert.True(credit.IsKnownCode);
        Assert.Equal("Crédito aplicado correctamente", credit.Description);
    }
}
