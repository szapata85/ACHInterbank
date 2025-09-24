using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchBatch : AuditableEntity
{
    public int Id { get; set; }

    // Cámara compensadora a la que pertenece el lote
    public int ClearingHouseId { get; set; }
    public ClearingHouse ClearingHouse { get; set; } = null!;

    // Ciclo de procesamiento (relación opcional si un lote corresponde a un ciclo)
    public int? AchCycleId { get; set; }
    public AchCycle? AchCycle { get; set; }

    // Campos propios del encabezado de lote
    public string CompanyName { get; set; } = null!;
    public string CompanyIdentification { get; set; } = null!;
    public DateTime EffectiveEntryDate { get; set; }

    // Transacciones del lote
    public ICollection<AchTransaction> Transactions { get; set; } = new List<AchTransaction>();
}

