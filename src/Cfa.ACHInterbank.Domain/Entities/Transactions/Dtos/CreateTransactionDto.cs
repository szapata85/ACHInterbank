using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;

public class CreateTransactionDto
{
    public decimal Amount { get; set; }
    public string? TransactionExternalId { get; set; }
    public string? Reference { get; set; }
    public TransactionTypeEnum Type { get; set; }

    public int DestinationInstitutionId { get; set; }

    // ✅ Nuevos campos
    public string SourceAccountNumber { get; set; } = null!;
    public string DestinationAccountNumber { get; set; } = null!;

    // Addendas opcionales
    public List<AddendaDto>? Addendas { get; set; }
}

/// <summary>
/// Addenda suministrada junto con una transacción. Sus campos se interpretan según el tipo de negocio y las validaciones actuales del flujo de transacciones.
/// </summary>
public class AddendaDto
{
    /// <summary>Código técnico del tipo de addenda enviado por el consumidor.</summary>
    public string AddendaType { get; set; } = "05";

    /// <summary>Clasificación de negocio opcional de la addenda; cuando se omite, el validador puede resolverla a partir de la transacción y del tipo de addenda.</summary>
    public AchAddendaBusinessType? BusinessType { get; set; }

    /// <summary>Campo de información legado conservado para compatibilidad con flujos existentes.</summary>
    public string? Information { get; set; }

    /// <summary>Propósito asociado a la addenda cuando ese campo aplica al tipo de negocio resuelto.</summary>
    public string? Purpose { get; set; }

    /// <summary>Referencia asociada a la addenda cuando ese campo aplica.</summary>
    public string? Reference { get; set; }

    /// <summary>Identificador del recaudador cuando ese dato aplica a la addenda.</summary>
    public string? CollectorId { get; set; }

    /// <summary>Código del cliente receptor cuando ese dato aplica a la addenda.</summary>
    public string? ReceiverCustomerCode { get; set; }

    /// <summary>Descripción del servicio cuando ese dato aplica a la addenda.</summary>
    public string? ServiceDescription { get; set; }

    /// <summary>Código de causal de devolución cuando la addenda representa ese flujo.</summary>
    public string? ReturnReasonCode { get; set; }

    /// <summary>Número de trazabilidad original cuando la addenda representa un flujo que lo requiere.</summary>
    public string? OriginalTraceNumber { get; set; }

    /// <summary>Nuevo número de trazabilidad cuando la addenda representa un flujo que lo requiere.</summary>
    public string? NewTraceNumber { get; set; }
}
