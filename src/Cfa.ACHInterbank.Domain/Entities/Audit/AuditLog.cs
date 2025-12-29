namespace Cfa.ACHInterbank.Domain.Entities.Audit;

public class AuditLog
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; set; }
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
}
