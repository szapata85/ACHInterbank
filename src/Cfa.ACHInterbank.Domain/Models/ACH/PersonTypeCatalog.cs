namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class PersonTypeCatalog
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
