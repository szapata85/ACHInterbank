using Cfa.ACHInterbank.Domain.Entities.User;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class BatchHeader
{
    public int BatchID { get; set; }
    public string ServiceClassCode { get; set; }
    public string CompanyName { get; set; }
    public string DiscretionaryData { get; set; }
    public string CompanyId { get; set; }
    public string StandardEntryClassCode { get; set; }
    public string CompanyEntryDescription { get; set; }
    public string DescriptiveDate { get; set; }
    public string EffectiveEntryDate { get; set; }
    public string CompensationDate { get; set; }
    public string OriginUserStatusCode { get; set; }
    public string OriginParticipantEntityCode { get; set; }
    public int BatchNumber { get; set; }

    [ForeignKey("NachaID")]
    public int NachaHeaderNachaID { get; set; }
    public virtual NachaHeader NachaHeader { get; set; }

    //public ICollection<EntryDetail> Entries { get; set; } = new List<EntryDetail>();
    //public BatchControl BatchControl { get; set; }
}
