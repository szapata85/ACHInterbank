using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Persistence;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class PersistenceDependencyInjectionTests
{
    [Fact]
    public void AddPersistence_FallsBackToSqlServerWhenPostgresIsConfiguredButMissing()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Postgres",
                ["ConnectionStrings:PostgresConnection"] = "",
                ["ConnectionStrings:SqlConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=AchInterbank;Trusted_Connection=True;"
            })
            .Build();

        var ex = Record.Exception(() => services.AddPersistence(configuration));

        Assert.Null(ex);
    }


    [Fact]
    public void AddPersistence_RegistersAchReturnEligibilityService()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "sqlserver",
                ["ConnectionStrings:SqlConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=AchInterbank;Trusted_Connection=True;"
            })
            .Build();

        services.AddPersistence(configuration);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAchReturnEligibilityService));
        Assert.NotNull(descriptor);
        Assert.Equal(typeof(AchReturnEligibilityService), descriptor!.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }


    [Fact]
    public void AddPersistence_RegistersAchReturnGenerationLockService_AsSingleton()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "sqlserver",
            ["ConnectionStrings:SqlConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=AchInterbank;Trusted_Connection=True;"
        }).Build();

        services.AddPersistence(configuration);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAchReturnGenerationLockService));
        Assert.NotNull(descriptor);
        Assert.Equal(typeof(AchReturnGenerationLockService), descriptor!.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }


    [Fact]
    public void AddPersistence_RegistersAchIncomingReturnIngestionService_AsScoped()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Provider"] = "sqlserver",
            ["ConnectionStrings:SqlConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=AchInterbank;Trusted_Connection=True;"
        }).Build();
        services.AddPersistence(configuration);
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAchIncomingReturnIngestionService));
        Assert.NotNull(descriptor);
        Assert.Equal(typeof(AchIncomingReturnIngestionService), descriptor!.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddPersistence_ThrowsClearErrorWhenNoConnectionStringIsConfigured()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "Postgres",
                ["ConnectionStrings:PostgresConnection"] = "",
                ["ConnectionStrings:SqlConnection"] = ""
            })
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() => services.AddPersistence(configuration));

        Assert.Contains("ConnectionStrings:PostgresConnection", ex.Message, StringComparison.Ordinal);
        Assert.Contains("ConnectionStrings:SqlConnection", ex.Message, StringComparison.Ordinal);
    }
}
