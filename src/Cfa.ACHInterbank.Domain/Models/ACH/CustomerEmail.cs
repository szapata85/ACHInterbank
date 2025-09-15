using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class CustomerEmail : AuditableEntity
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public EmailTypeEnum EmailType { get; set; } // Personal, Trabajo, etc.
    public string Address { get; set; } = null!;
    public bool IsPrimary { get; set; } = false;
}

