using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class CustomerPhone : AuditableEntity
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public PhoneTypeEnum PhoneType { get; set; } // Móvil, Fijo, Trabajo
    public string Number { get; set; } = null!;
    public bool IsPrimary { get; set; } = false;
}
