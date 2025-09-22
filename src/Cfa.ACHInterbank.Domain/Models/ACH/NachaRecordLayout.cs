namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class NachaRecordLayout
{
    public int Id { get; set; }
    public string RecordType { get; set; } = null!;
    public string RecordCode { get; set; } = null!;
    public int TotalLength { get; set; }
    public string? Description { get; set; }

    public ICollection<NachaRecordField> Fields { get; set; } = new List<NachaRecordField>();
}
