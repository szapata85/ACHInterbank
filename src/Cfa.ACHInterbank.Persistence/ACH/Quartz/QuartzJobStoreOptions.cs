using Microsoft.Extensions.Configuration;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz;

public sealed class QuartzJobStoreOptions
{
    public string Mode { get; set; } = "RAM";
    public string Provider { get; set; } = "Postgres";
    public string TablePrefix { get; set; } = "QRTZ_";
    public bool Clustered { get; set; }
    public int ClusterCheckinIntervalSeconds { get; set; } = 20;
    public int MisfireThresholdMilliseconds { get; set; } = 60000;
    public bool PerformSchemaValidation { get; set; } = true;

    public bool IsPersistentMode() => string.Equals(Mode, "Persistent", StringComparison.OrdinalIgnoreCase);
    public string GetNormalizedProvider() => string.Equals(Provider, "SqlServer", StringComparison.OrdinalIgnoreCase) ? "SqlServer" : "Postgres";
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
