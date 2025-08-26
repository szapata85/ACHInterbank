using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchCycle : AuditableEntity
{
    public int Id { get; set; }

    // Ejemplo: "Ciclo 1 - ACH Colombia"
    public string CycleName { get; set; } = null!;

    // Fecha en la que aplica el ciclo (puede cambiar según día hábil/festivo)
    public DateTime ProcessingDate { get; set; }

    // Hora límite de recepción del ciclo
    public TimeSpan CutoffTime { get; set; }

    // Indica si debe correrse al siguiente día hábil si cae en festivo
    public bool RescheduleOnHoliday { get; set; }

    // Relación con la cámara de compensación
    public int ClearingHouseId { get; set; }
    public ClearingHouse? ClearingHouse { get; set; }

    // Transacciones que entraron en este ciclo
    public ICollection<AchTransaction> Transactions { get; set; } = new List<AchTransaction>();
}