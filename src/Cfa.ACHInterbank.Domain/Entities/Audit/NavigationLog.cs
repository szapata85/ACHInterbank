namespace Cfa.ACHInterbank.Domain.Entities.Audit;

public class NavigationLog
{
    public Guid Id { get; set; }
    public string? UserId { get; set; }
    public string Route { get; set; } = string.Empty;
    public DateTime VisitedAt { get; set; }
    public string? SessionId { get; set; }
    public int? DurationMs { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
