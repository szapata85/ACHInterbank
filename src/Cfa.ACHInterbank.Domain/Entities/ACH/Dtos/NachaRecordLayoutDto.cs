namespace Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

/// <summary>
/// Representa un layout legacy de ancho fijo y los campos que lo componen.
/// </summary>
public class NachaRecordLayoutDto
{
    /// <summary>Identificador interno persistido del layout legacy.</summary>
    public int Id { get; set; }

    /// <summary>Tipo de registro asociado al layout legacy.</summary>
    public string RecordType { get; set; } = string.Empty;

    /// <summary>Código de registro escrito por el renderer legacy en el primer carácter cuando está informado.</summary>
    public string RecordCode { get; set; } = string.Empty;

    /// <summary>Longitud total del buffer de caracteres construido para el registro legacy.</summary>
    public int TotalLength { get; set; }

    /// <summary>Descripción opcional almacenada para el layout legacy.</summary>
    public string? Description { get; set; }

    /// <summary>Campos del layout legacy; las consultas los devuelven ordenados por StartPosition.</summary>
    public List<NachaRecordFieldDto> Fields { get; set; } = [];
}
