using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class FinancialInstitution : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;   // Código propio de la entidad
    public bool IsDefaultSource { get; set; } = false; // Indica si es la entidad por defecto para origen

    // 🔹 Nueva relación
    public int ClearingHouseId { get; set; }
    public ClearingHouse ClearingHouse { get; set; } = null!;

    public ICollection<AchTransaction> SourceTransactions { get; set; } = new List<AchTransaction>();
    public ICollection<AchTransaction> DestinationTransactions { get; set; } = new List<AchTransaction>();
}


