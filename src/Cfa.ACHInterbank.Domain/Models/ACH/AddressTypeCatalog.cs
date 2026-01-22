namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AddressTypeCatalog
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public ICollection<CustomerAddress> CustomerAddresses { get; set; } = new List<CustomerAddress>();
}
