using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Persistence;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class SqlServerBootstrapRuntimeTests
{
    [Fact]
    public async Task DbInitializer_SqlServerDocker_IsIdempotent()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("ACH_SQLSERVER_RUNTIME_TESTS"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__SqlConnection")
            ?? "Server=127.0.0.1,1433;Database=ACHInterbank;User Id=sa;Password=Example_sqlServer_2026*;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true";

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "SqlServer",
                ["ConnectionStrings:SqlConnection"] = connectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment());
        services.AddSingleton<IConfiguration>(configuration);

        using var provider = BuildProvider(services, configuration);

        await DbInitializer.SeedAllAsync(provider);
        var first = await ReadSnapshotAsync(provider);

        await DbInitializer.SeedAllAsync(provider);
        var second = await ReadSnapshotAsync(provider);

        Assert.Equal(first.MethodId, second.MethodId);
        Assert.Equal(first.Parameters, second.Parameters);
        Assert.Equal(first.MappingSetId, second.MappingSetId);
        Assert.Equal(first.Version, second.Version);
        Assert.Equal(first.Rules, second.Rules);
        Assert.Equal(first.SnapshotHash, second.SnapshotHash);
        Assert.Equal(first.PublishedBy, second.PublishedBy);
    }

    private static ServiceProvider BuildProvider(ServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        return services.BuildServiceProvider();
    }

    private static async Task<MappingSnapshot> ReadSnapshotAsync(ServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AchDbContext>();

        var method = await db.IntegrationMethods
            .AsNoTracking()
            .SingleAsync(x => x.Code == "WSCFAACH.Proc_Transacciones" && x.IsActive);

        var publishedSet = await db.IntegrationMappingSets
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id && x.IsActive && x.Status == IntegrationMappingSetStatusEnum.Published)
            .OrderByDescending(x => x.Version)
            .FirstAsync();

        var parameterCount = await db.IntegrationMethodParameters
            .AsNoTracking()
            .CountAsync(x => x.MethodId == method.Id && x.IsActive);

        var ruleCount = await db.IntegrationMappingRules
            .AsNoTracking()
            .CountAsync(x => x.MappingSetId == publishedSet.Id && x.Enabled);

        var history = await db.IntegrationMappingSetHistory
            .AsNoTracking()
            .Where(x => x.MappingSetId == publishedSet.Id)
            .OrderByDescending(x => x.Id)
            .FirstAsync();

        return new MappingSnapshot(
            method.Id,
            parameterCount,
            publishedSet.Id,
            publishedSet.Version,
            ruleCount,
            history.SnapshotHash,
            publishedSet.PublishedBy);
    }

    private sealed record MappingSnapshot(
        int MethodId,
        int Parameters,
        Guid MappingSetId,
        int Version,
        int Rules,
        string SnapshotHash,
        string PublishedBy);

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "Cfa.ACHInterbank.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(Directory.GetCurrentDirectory());
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public string EnvironmentName { get; set; } = Environments.Development;
    }
}
