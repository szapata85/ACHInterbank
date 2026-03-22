using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

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

    public AchAddendaBusinessType BusinessType { get; set; } = AchAddendaBusinessType.Credit;

    // Legacy: mantener temporalmente para compatibilidad/migración
    public string? Information { get; set; }

    // Crédito / prenotificación de crédito
    public string? Purpose { get; set; }
    public string? Reference { get; set; }

    // Débito / prenotificación de débito
    public string? CollectorId { get; set; }
    public string? ReceiverCustomerCode { get; set; }
    public string? ServiceDescription { get; set; }

    // Devolución RET
    public string? ReturnReasonCode { get; set; }
    public string? OriginalTraceNumber { get; set; }
    public string? NewTraceNumber { get; set; }

    // Secuencia opcional para registros múltiples
    public int? SequenceNumber { get; set; }
}

