using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Cfa.ACHInterbank.Persistence.DataBase;

public sealed class AchDbContextFactory : IDesignTimeDbContextFactory<AchDbContext>
{
    public AchDbContext CreateDbContext(string[] args)
    {
        var basePath = ResolveBasePath();

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var provider = (configuration["Database:Provider"] ?? "SqlServer").Trim().ToLowerInvariant();
        var connectionStringName = provider switch
        {
            "postgres" or "postgresql" or "npgsql" => "PostgresConnection",
            _ => "SqlConnection"
        };

        var connectionString = configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{connectionStringName}' was not found. Ensure appsettings contains a valid value.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<AchDbContext>();

        switch (provider)
        {
            case "postgres":
            case "postgresql":
            case "npgsql":
                optionsBuilder.UseNpgsql(connectionString);
                break;
            case "sqlserver":
            case "mssql":
                optionsBuilder.UseSqlServer(connectionString);
                break;
            default:
                throw new InvalidOperationException($"Unsupported database provider '{provider}'.");
        }

        return new AchDbContext(optionsBuilder.Options);
    }

    private static string ResolveBasePath()
    {
        var current = Directory.GetCurrentDirectory();

        var candidates = new[]
        {
            current,
            Path.Combine(current, "src", "Cfa.ACHInterbank.Api"),
            Path.Combine(current, "..", "Cfa.ACHInterbank.Api"),
            Path.Combine(current, "..", "..", "src", "Cfa.ACHInterbank.Api")
        };

        foreach (var candidate in candidates)
        {
            var fullPath = Path.GetFullPath(candidate);
            if (File.Exists(Path.Combine(fullPath, "appsettings.json")))
            {
                return fullPath;
            }
        }

        return current;
    }
}
