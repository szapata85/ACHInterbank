using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchFileExport : AuditableEntity
{
    public int Id { get; set; }
    public string AchCycleId { get; set; } = string.Empty;
    public AchCycle AchCycle { get; set; } = null!;
    public int ClearingHouseId { get; set; }
    public ClearingHouse ClearingHouse { get; set; } = null!;
    public string ExportKind { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int TotalTransactions { get; set; }
    public bool IsEncrypted { get; set; }
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}
