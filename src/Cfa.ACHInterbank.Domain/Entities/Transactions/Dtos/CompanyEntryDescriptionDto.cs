namespace Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;

/// <summary>
/// Elemento del catálogo activo de Company Entry Descriptions configurado en ACHInterbank.
/// </summary>
public class CompanyEntryDescriptionDto
{
    /// <summary>Identificador interno del elemento de catálogo.</summary>
    public int Id { get; set; }

    /// <summary>Término configurado para la Company Entry Description.</summary>
    public string Term { get; set; } = string.Empty;

    /// <summary>Descripción legible del elemento configurado.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Código de clase de entrada asociado por la configuración del catálogo.</summary>
    public string StandardEntryClassCode { get; set; } = string.Empty;
}
