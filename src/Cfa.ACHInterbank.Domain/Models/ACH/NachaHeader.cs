using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class NachaHeader
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [MaxLength(64)]
    public string? NachaID { get; set; }
    public string? PriorityCode { get; set; }
    public string? ImmediateDestination { get; set; }
    public string? ImmediateOrigin { get; set; }
    public string? FileCreationDate { get; set; }
    public string? FileCreationTime { get; set; }
    public string? FileIdModifier { get; set; }
    public string? RecordSize { get; set; }
    public string? BlockingFactor { get; set; }
    public string? FormatCode { get; set; }
    public string? ImmediateDestinationName { get; set; }
    public string? ImmediateOriginName { get; set; }
    public string? ReferenceCode { get; set; }

    // Relación con ClearingHouse
    public int? ClearingHouseId { get; set; }
    public ClearingHouse? ClearingHouse { get; set; }

    public int CycleNumber { get; set; }

    public int? AchCycleId { get; set; }
    public AchCycle? AchCycle { get; set; }


    public virtual ICollection<BatchHeader>? Batches { get; set; }
    public virtual ICollection<EntryDetail>? EntryDetails { get; set; }
    public virtual ICollection<AddendaRecord>? AddendaRecords { get; set; }
    public virtual ICollection<BatchControl>? BatchControls { get; set; }
    public virtual ICollection<FileControl>? FileControls { get; set; }
}
