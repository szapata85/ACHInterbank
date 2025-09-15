using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class Customer : AuditableEntity
{
    public int Id { get; set; }

    // Datos personales
    public string FirstName { get; set; } = null!;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = null!;
    public string? SecondLastName { get; set; }

    // Documento
    public DocumentTypeEnum DocumentType { get; set; }
    public string DocumentNumber { get; set; } = null!;

    // Cuenta origen
    public string AccountNumber { get; set; } = null!;

    // Relaciones
    public ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();
    public ICollection<CustomerPhone> Phones { get; set; } = new List<CustomerPhone>();
    public ICollection<CustomerEmail> Emails { get; set; } = new List<CustomerEmail>();

    public ICollection<AchTransaction> Transactions { get; set; } = new List<AchTransaction>();
}
