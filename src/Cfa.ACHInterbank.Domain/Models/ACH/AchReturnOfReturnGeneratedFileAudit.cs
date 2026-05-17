using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchReturnOfReturnGeneratedFileAudit : AuditableEntity
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int ClearingHouseId { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public int GeneratedFlowCount { get; set; }
    public int ContentLength { get; set; }
    public string ContentSha256 { get; set; } = string.Empty;
    public string? RequestedBy { get; set; }
    public string? Source { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public ICollection<AchReturnOfReturnGeneratedFileAuditFlow> Flows { get; set; } = new List<AchReturnOfReturnGeneratedFileAuditFlow>();
}

public class AchReturnOfReturnGeneratedFileAuditFlow
{
    public int Id { get; set; }
    public int AchReturnOfReturnGeneratedFileAuditId { get; set; }
    public long ReturnOfReturnFlowId { get; set; }

    public AchReturnOfReturnGeneratedFileAudit Audit { get; set; } = null!;
    public ReturnOfReturnFlow? ReturnOfReturnFlow { get; set; }
}
