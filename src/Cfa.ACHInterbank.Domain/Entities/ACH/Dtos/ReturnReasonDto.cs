namespace Cfa.ACHInterbank.Domain.Entities.ACH.Dtos;

public class ReturnReasonDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}
