namespace Cfa.ACHInterbank.Domain.Models.ACH.ExternalFileNames;

public class ExternalFileNameValidationLog
{
    public long Id { get; set; }
    public long RegistryId { get; set; }
    public ExternalFileNameRegistry Registry { get; set; } = null!;
    public string ValidationStage { get; set; } = string.Empty;
    public string RuleCode { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string IssueCode { get; set; } = string.Empty;
    public string IssueMessage { get; set; } = string.Empty;
    public string IssuePayloadJson { get; set; } = "{}";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
