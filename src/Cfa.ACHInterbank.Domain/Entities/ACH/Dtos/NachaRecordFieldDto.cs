namespace Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

public class NachaRecordFieldDto
{
    public int Id { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public int StartPosition { get; set; }
    public int Length { get; set; }
    public string PadChar { get; set; } = " ";
    public string Justification { get; set; } = "L";
    public string DbColumn { get; set; } = string.Empty;
    public string? Format { get; set; }
}
