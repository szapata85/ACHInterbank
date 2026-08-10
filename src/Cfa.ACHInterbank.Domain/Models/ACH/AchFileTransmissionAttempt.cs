using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchFileTransmissionAttempt : AuditableEntity
{
    public long Id { get; set; }
    public int AchFileExportId { get; set; }
    public AchFileExport AchFileExport { get; set; } = null!;
    public int AttemptNumber { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public AchFileTransmissionAttemptStatus Status { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public bool Retryable { get; set; }
    public string ResultCode { get; set; } = string.Empty;
    public string ResultSummary { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
    public string ContentSha256 { get; set; } = string.Empty;
    public byte[] ProtectedContent { get; set; } = [];
}
