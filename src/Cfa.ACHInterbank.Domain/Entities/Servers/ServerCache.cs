namespace Cfa.ACHInterbank.Domain.Entities.Servers;

public class ServerCache
{
    public string? Url { set; get; }
    public bool IsHealthy { get; set; } = false;
    public DateTime LastHealthCheck { get; set; } = DateTime.MinValue;
    public int FailedChecks { get; set; } = 0;
    public int ActiveConnections { get; set; } = 0;
}
