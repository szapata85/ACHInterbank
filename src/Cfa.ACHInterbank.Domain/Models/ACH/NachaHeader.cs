using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class NachaHeader
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int NachaID { get; set; }
    public string PriorityCode { get; set; }
    public string ImmediateDestination { get; set; }
    public string ImmediateOrigin { get; set; }
    public string FileCreationDate { get; set; }
    public string FileCreationTime { get; set; }
    public string FileIdModifier { get; set; }
    public string RecordSize { get; set; }
    public string BlockingFactor { get; set; }
    public string FormatCode { get; set; }
    public string ImmediateDestinationName { get; set; }
    public string ImmediateOriginName { get; set; }
    public string ReferenceCode { get; set; }

    public virtual ICollection<BatchHeader> Batches { get; set; }
    public virtual ICollection<EntryDetail> EntryDetails { get; set; }
    public virtual ICollection<AddendaRecord> AddendaRecords { get; set; }
}
