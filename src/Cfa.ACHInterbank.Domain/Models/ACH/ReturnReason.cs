namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class ReturnReason
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsForReturn { get; set; }
}
