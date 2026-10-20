using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Cfa.ACHInterbank.Persistence.DataBase;

public sealed class AchDbContextFactory : IDesignTimeDbContextFactory<AchDbContext>
{
    public AchDbContext CreateDbContext(string[] args)
    {
        var basePath = ResolveBasePath();

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";

        var provider = Environment.GetEnvironmentVariable("Database__Provider")
            ?? ReadSetting(basePath, environment, "Database", "Provider")
            ?? "SqlServer";

        var normalizedProvider = provider.Trim().ToLowerInvariant();
        var connectionStringName = normalizedProvider switch
        {
            "postgres" or "postgresql" or "npgsql" => "PostgresConnection",
            _ => "SqlConnection"
        };

        var connectionString = Environment.GetEnvironmentVariable($"ConnectionStrings__{connectionStringName}")
            ?? ReadSetting(basePath, environment, "ConnectionStrings", connectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{connectionStringName}' was not found. Configure it in appsettings or environment variables.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<AchDbContext>();

        switch (normalizedProvider)
        {
            case "postgres":
            case "postgresql":
            case "npgsql":
                optionsBuilder.UseNpgsql(connectionString);
                break;
            case "sqlserver":
            case "mssql":
                optionsBuilder.UseSqlServer(
                    connectionString,
                    sqlOptions => sqlOptions.MigrationsAssembly("Cfa.ACHInterbank.Persistence.Migrations.SqlServer"));
                break;
            default:
                throw new InvalidOperationException($"Unsupported database provider '{provider}'.");
        }

        return new AchDbContext(optionsBuilder.Options);
    }

    private static string? ReadSetting(string basePath, string environment, string section, string key)
    {
        var environmentFile = Path.Combine(basePath, $"appsettings.{environment}.json");
        var defaultFile = Path.Combine(basePath, "appsettings.json");

        return ReadSettingFromFile(environmentFile, section, key)
            ?? ReadSettingFromFile(defaultFile, section, key);
    }

    private static string? ReadSettingFromFile(string filePath, string section, string key)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(filePath);
            using var document = JsonDocument.Parse(stream, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            if (!document.RootElement.TryGetProperty(section, out var sectionNode))
            {
                return null;
            }

            if (!sectionNode.TryGetProperty(key, out var valueNode))
            {
                return null;
            }

            return valueNode.GetString();
        }
        catch (JsonException)
        {
            return null;
        }
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
