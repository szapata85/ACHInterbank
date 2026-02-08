namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class TransactionCodeCatalog
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
