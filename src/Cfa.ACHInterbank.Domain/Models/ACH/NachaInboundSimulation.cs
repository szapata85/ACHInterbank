using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class NachaInboundSimulation : AuditableEntity
{
    public int Id { get; set; }
    public Guid SimulationId { get; set; } = Guid.NewGuid();
    public int ClearingHouseId { get; set; }
    public ClearingHouse ClearingHouse { get; set; } = null!;
    public string ClearingHouseName { get; set; } = string.Empty;
    public NachaInboundSimulationType ScenarioType { get; set; }
    public InboundResponseMode? ResponseMode { get; set; }
    public string? ReasonCode { get; set; }
    public int OriginFinancialInstitutionId { get; set; }
    public FinancialInstitution OriginFinancialInstitution { get; set; } = null!;
    public int DestinationFinancialInstitutionId { get; set; }
    public FinancialInstitution DestinationFinancialInstitution { get; set; } = null!;
    public int EntriesCount { get; set; }
    public decimal Amount { get; set; }
    public DateOnly BusinessDate { get; set; }
    public string CycleCode { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public NachaInboundSimulationStatus Status { get; set; } = NachaInboundSimulationStatus.Draft;
    public bool GeneratedOnly { get; set; } = true;
    public bool AutoImported { get; set; }
    public bool UploadRequired { get; set; } = true;
    public bool ExternalTransmission { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = string.Empty;
    public ICollection<NachaInboundSimulationEntry> Entries { get; set; } = new List<NachaInboundSimulationEntry>();
}
