namespace Cfa.ACHInterbank.Domain.Models.ACH.ExternalFileNames;

public class ExternalFileNameRegistry
{
    public long Id { get; set; }
    public int ClearingHouseId { get; set; }
    public string FlowCode { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string ExternalFileName { get; set; } = string.Empty;
    public string? InternalFileName { get; set; }
    public string ExternalFileType { get; set; } = string.Empty;
    public string? FileIdModifier { get; set; }
    public int? ExternalSequence { get; set; }
    public int? DeclaredDetailCount { get; set; }
    public int? ActualDetailCount { get; set; }
    public string? FileHash { get; set; }
    public long? FileSize { get; set; }
    public DateTime ProcessingDate { get; set; }
    public string? CycleId { get; set; }
    public string ValidationDisposition { get; set; } = string.Empty;
    public string ValidationResult { get; set; } = string.Empty;
    public string ValidationIssuesJson { get; set; } = "[]";
    public string CorrelationEvidenceJson { get; set; } = "{}";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "system";
    public byte[] RowVersion { get; set; } = [];

    public ICollection<ExternalFileNameValidationLog> ValidationLogs { get; set; } = new List<ExternalFileNameValidationLog>();
}
