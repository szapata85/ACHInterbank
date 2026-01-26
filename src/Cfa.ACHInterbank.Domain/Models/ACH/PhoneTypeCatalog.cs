namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class PhoneTypeCatalog
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public ICollection<CustomerPhone> CustomerPhones { get; set; } = new List<CustomerPhone>();
}
