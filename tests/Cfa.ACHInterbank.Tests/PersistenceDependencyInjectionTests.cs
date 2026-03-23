using Cfa.ACHInterbank.Persistence;
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
