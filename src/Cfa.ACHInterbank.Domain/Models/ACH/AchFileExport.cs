using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

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
    public int? Version { get; set; }
    public string? ContentSha256 { get; set; }
    public AchFileExportLifecycleStatus LifecycleStatus { get; set; } = AchFileExportLifecycleStatus.HistoricalUnknown;
    public string? TransmissionReference { get; set; }
    public DateTime? TransmittedAtUtc { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
    public string? AcknowledgementCode { get; set; }
    public CenitChamberResponseState ChamberResponseState { get; set; } = CenitChamberResponseState.Pending;
    public DateTime? ChamberResponseUpdatedAtUtc { get; set; }
    public ICollection<AchFileExportTransaction> Transactions { get; set; } = new List<AchFileExportTransaction>();
    public ICollection<AchFileTransmissionAttempt> TransmissionAttempts { get; set; } = new List<AchFileTransmissionAttempt>();
    public ICollection<AchFileTransportResult> TransportResults { get; set; } = new List<AchFileTransportResult>();
    public ICollection<CenitChamberResponse> ChamberResponses { get; set; } = new List<CenitChamberResponse>();

    public bool AllowsCenitChamberRetransmission()
        => ChamberResponseState == CenitChamberResponseState.Pending;
}
