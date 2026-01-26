namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class EmailTypeCatalog
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public ICollection<CustomerEmail> CustomerEmails { get; set; } = new List<CustomerEmail>();
}
