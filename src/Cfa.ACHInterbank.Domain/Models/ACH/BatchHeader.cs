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
    public string OdfiIdentification { get; set; }

    public int NachaHeaderId { get; set; }
    public NachaHeader NachaHeader { get; set; }

    public ICollection<EntryDetail> Entries { get; set; } = new List<EntryDetail>();
    public BatchControl BatchControl { get; set; }
}
