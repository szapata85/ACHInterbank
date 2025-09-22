namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class NachaRecordField
{
    public int Id { get; set; }
    public int NachaRecordLayoutId { get; set; }
    public NachaRecordLayout Layout { get; set; } = null!;

    public string FieldName { get; set; } = null!;
    public int StartPosition { get; set; }
    public int Length { get; set; }
    public char PadChar { get; set; }
    public char Justification { get; set; }   // 'L' o 'R'
    public string DbColumn { get; set; } = null!;
    public string? Format { get; set; }
}
