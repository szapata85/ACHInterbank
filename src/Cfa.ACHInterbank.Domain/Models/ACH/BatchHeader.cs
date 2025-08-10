using Cfa.ACHInterbank.Domain.Entities.User;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class BatchHeader
{
    public int Id { get; set; }
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

    public int NachaHeaderId { get; set; }
    public NachaHeader NachaHeader { get; set; }

    public ICollection<EntryDetail> Entries { get; set; } = new List<EntryDetail>();
    public BatchControl BatchControl { get; set; }
}
