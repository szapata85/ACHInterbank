using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class CustomerAddress : AuditableEntity
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string AddressType { get; set; } = null!; // Personal, Trabajo, Correspondencia…
    public string Line1 { get; set; } = null!;
    public string? Line2 { get; set; }
    public string City { get; set; } = null!;
    public string State { get; set; } = null!;
    public string Country { get; set; } = "Colombia";
    public string PostalCode { get; set; } = null!;

    public AddressTypeCatalog AddressTypeCatalog { get; set; } = null!;
}
