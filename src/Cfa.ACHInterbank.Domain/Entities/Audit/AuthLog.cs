namespace Cfa.ACHInterbank.Domain.Entities.Audit;

public class AuthLog
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime LoggedAt { get; set; }
}
