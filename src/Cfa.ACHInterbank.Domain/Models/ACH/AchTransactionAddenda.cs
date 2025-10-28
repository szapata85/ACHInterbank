using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class AchTransactionAddenda : AuditableEntity
{
    public int Id { get; set; }

    // 🔗 Clave foránea hacia la transacción principal
    public int AchTransactionId { get; set; }

    // 🔗 Propiedad de navegación (faltaba)
    public AchTransaction Transaction { get; set; } = null!;

    // Tipo de addenda (según catálogo NACHA)
    public string AddendaType { get; set; } = "05";

    // Información adicional del campo 7 del NACHA-M
    public string Information { get; set; } = string.Empty;

    // Secuencia opcional para registros múltiples
    public int? SequenceNumber { get; set; }
}


