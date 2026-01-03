namespace Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

public class NachaRecordLayoutDto
{
    public int Id { get; set; }
    public string RecordType { get; set; } = string.Empty;
    public string RecordCode { get; set; } = string.Empty;
    public int TotalLength { get; set; }
    public string? Description { get; set; }
    public List<NachaRecordFieldDto> Fields { get; set; } = [];
}
