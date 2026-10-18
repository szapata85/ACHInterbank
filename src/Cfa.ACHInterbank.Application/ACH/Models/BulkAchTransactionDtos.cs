using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>
/// Solicitud para registrar directamente un lote de transacciones inline.
/// </summary>
public sealed class BulkAchTransactionRequest
{
    /// <summary>Referencia obligatoria que identifica el lote dentro del resultado del procesamiento.</summary>
    public string BatchReference { get; set; } = string.Empty;

    /// <summary>Transacciones que serán validadas y procesadas individualmente.</summary>
    public List<BulkAchTransactionItemRequest> Transactions { get; set; } = [];

    /// <summary>Tamaño de bloque opcional usado por el servicio para agrupar procesamiento y confirmaciones.</summary>
    public int? ChunkSize { get; set; }
}

/// <summary>
/// Datos de una transacción individual incluida en un lote.
/// </summary>
public sealed class BulkAchTransactionItemRequest
{
    /// <summary>Monto evaluado por las mismas validaciones transaccionales del registro individual.</summary>
    public decimal Amount { get; set; }

    /// <summary>Identificador operativo/idempotencia canónico del ítem. Cuando está vacío, el servicio usa Reference legado como respaldo.</summary>
    public string? TransactionExternalId { get; set; }

    /// <summary>Referencia legada o transicional usada como identificador de respaldo cuando no se informa TransactionExternalId.</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>Tipo de transacción solicitado para el ítem.</summary>
    public TransactionTypeEnum Type { get; set; }

    /// <summary>Tipo de cuenta o producto usado para resolver el código transaccional.</summary>
    public AccountTypeEnum AccountType { get; set; } = AccountTypeEnum.Checking;

    /// <summary>Indica si el ítem corresponde a una prenotificación.</summary>
    public bool IsPrenotification { get; set; }

    /// <summary>Identificador de la institución destino usado por el flujo transaccional.</summary>
    public int DestinationInstitutionId { get; set; }

    /// <summary>Número de cuenta origen suministrado para la transacción.</summary>
    public string SourceAccountNumber { get; set; } = string.Empty;

    /// <summary>Número de cuenta destino suministrado para la transacción.</summary>
    public string DestinationAccountNumber { get; set; } = string.Empty;

    /// <summary>Identificación opcional del receptor; determinadas transacciones o validaciones pueden exigirla.</summary>
    public string? RecipientIdNumber { get; set; }

    /// <summary>Nombre opcional del receptor asociado a su identificación cuando se suministra.</summary>
    public string? RecipientName { get; set; }

    /// <summary>Indica que el flujo debe aplicar la validación de identidad del receptor prevista por las reglas actuales.</summary>
    public bool RequiresIdentityValidation { get; set; }

    /// <summary>Nombre del originador suministrado al flujo de registro.</summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>Identificación del originador suministrada al flujo de registro.</summary>
    public string CompanyIdentification { get; set; } = string.Empty;

    /// <summary>Identificador de la Company Entry Description activa seleccionada para la transacción.</summary>
    public int CompanyEntryDescriptionId { get; set; }

    /// <summary>Tipo de persona del originador cuando se suministra explícitamente.</summary>
    public string? SourcePersonType { get; set; }

    /// <summary>Tipo de persona del receptor cuando se suministra explícitamente.</summary>
    public string? RecipientPersonType { get; set; }

    /// <summary>Addendas opcionales que se validan junto con la transacción según el flujo existente.</summary>
    public List<AddendaDto>? Addendas { get; set; }
}

/// <summary>
/// Resultado agregado del procesamiento de un lote de transacciones.
/// </summary>
public sealed class BulkAchTransactionResponse
{
    /// <summary>Referencia del lote procesado.</summary>
    public string BatchReference { get; set; } = string.Empty;

    /// <summary>Cantidad de ítems recibidos en la solicitud.</summary>
    public int TotalReceived { get; set; }

    /// <summary>Cantidad de ítems para los que se produjo un resultado individual.</summary>
    public int TotalProcessed { get; set; }

    /// <summary>Cantidad de ítems procesados satisfactoriamente.</summary>
    public int TotalSucceeded { get; set; }

    /// <summary>Cantidad de ítems rechazados por validaciones o reglas capturadas por ítem.</summary>
    public int TotalFailed { get; set; }

    /// <summary>Identificadores internos de las transacciones creadas y confirmadas.</summary>
    public List<int> CreatedTransactionIds { get; set; } = [];

    /// <summary>Resultado individual de cada ítem procesado, incluidos éxitos y fallos capturados.</summary>
    public List<BulkAchTransactionItemResult> ItemResults { get; set; } = [];
}

/// <summary>
/// Resultado del procesamiento de un ítem dentro de una solicitud masiva.
/// </summary>
public sealed class BulkAchTransactionItemResult
{
    /// <summary>Posición original del ítem dentro de Transactions.</summary>
    public int Index { get; set; }

    /// <summary>Identificador operativo efectivo del ítem: TransactionExternalId o, como respaldo, Reference.</summary>
    public string TransactionExternalId { get; set; } = string.Empty;

    /// <summary>Referencia legada recibida para el ítem.</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>Indica si el ítem fue procesado satisfactoriamente.</summary>
    public bool Succeeded { get; set; }

    /// <summary>Identificador interno de la transacción creada cuando el procesamiento fue satisfactorio y quedó confirmado.</summary>
    public int? TransactionId { get; set; }

    /// <summary>Código técnico del error capturado para el ítem, cuando falla una validación o regla procesada individualmente.</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Mensaje del error capturado para el ítem.</summary>
    public string? ErrorMessage { get; set; }
}
