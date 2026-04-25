namespace Cfa.ACHInterbank.Domain.Models.Configurations;

public sealed class IncomingNachaDispatchResilienceOptions
{
    public const string SectionName = "IncomingNacha:DispatchResilience";

    public int MaxAttempts { get; set; } = 5;
    public int InitialBackoffSeconds { get; set; } = 120;
    public int MaxBackoffSeconds { get; set; } = 1800;
    public double BackoffMultiplier { get; set; } = 2.0;
    public bool EnableJitter { get; set; } = false;
    public int JitterMaxSeconds { get; set; } = 15;
}
