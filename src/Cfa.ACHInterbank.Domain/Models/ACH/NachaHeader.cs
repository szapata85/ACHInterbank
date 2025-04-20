namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class NachaHeader
{
    public int Id { get; set; }
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

    public ICollection<BatchHeader> Batches { get; set; } = new List<BatchHeader>();
}
