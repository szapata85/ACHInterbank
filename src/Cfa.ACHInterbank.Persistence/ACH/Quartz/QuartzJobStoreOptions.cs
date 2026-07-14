using Microsoft.Extensions.Configuration;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz;

public sealed class QuartzJobStoreOptions
{
    public string Mode { get; set; } = "RAM";
    public string Provider { get; set; } = string.Empty;
    public string TablePrefix { get; set; } = "QRTZ_";
    public bool Clustered { get; set; }
    public int ClusterCheckinIntervalSeconds { get; set; } = 20;
    public int MisfireThresholdMilliseconds { get; set; } = 60000;
    public bool PerformSchemaValidation { get; set; } = true;

    public bool IsPersistentMode() => string.Equals(Mode, "Persistent", StringComparison.OrdinalIgnoreCase);
    public string GetNormalizedProvider()
    {
        if (string.IsNullOrWhiteSpace(Provider))
        {
            throw new InvalidOperationException("Quartz job store provider is required. Supported values are 'Postgres' and 'SqlServer'.");
        }

        var normalizedProvider = Provider.Trim();
        if (string.Equals(normalizedProvider, "Postgres", StringComparison.OrdinalIgnoreCase))
        {
            return "Postgres";
        }

        if (string.Equals(normalizedProvider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            return "SqlServer";
        }

        throw new InvalidOperationException($"Unsupported Quartz job store provider '{Provider}'. Supported values are 'Postgres' and 'SqlServer'.");
    }
}

public static class QuartzJobStoreOptionsFactory
{
    public static QuartzJobStoreOptions Create(IConfiguration configuration)
    {
        var options = new QuartzJobStoreOptions();
        configuration.GetSection("Quartz:JobStore").Bind(options);
        return options;
    }
}
