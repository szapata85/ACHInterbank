using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

public class NachaRecordDefinitionDto
{
    public int Id { get; set; }
    public string RecordCode { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public NachaRecordSourceType SourceType { get; set; }
    public string? SourceName { get; set; }
    public string? FilterKey { get; set; }
    public bool IsEnabled { get; set; }
}
