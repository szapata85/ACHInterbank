using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchFileExportTransaction : AuditableEntity
{
    public long Id { get; set; }
    public int AchFileExportId { get; set; }
    public AchFileExport AchFileExport { get; set; } = null!;
    public int AchTransactionId { get; set; }
    public AchTransaction AchTransaction { get; set; } = null!;
    public string AchCycleId { get; set; } = string.Empty;
    public int AchBatchId { get; set; }
    public int FileSequence { get; set; }
    public string TraceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime IncludedAtUtc { get; set; }
}
