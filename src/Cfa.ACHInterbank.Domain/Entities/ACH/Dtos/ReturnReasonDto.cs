namespace Cfa.ACHInterbank.Domain.Entities.ACH.Dtos;

/// <summary>
/// Causal persistida del catálogo de devoluciones.
/// </summary>
public class ReturnReasonDto
{
    /// <summary>Identificador interno de la causal.</summary>
    public int Id { get; set; }

    /// <summary>Código persistido de la causal.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Descripción persistida de la causal.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Categoría persistida asociada a la causal.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Indica si la causal está marcada en el catálogo para el flujo de devolución.</summary>
    public bool IsForReturn { get; set; }
}
