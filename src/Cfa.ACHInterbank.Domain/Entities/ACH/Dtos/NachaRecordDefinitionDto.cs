using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

/// <summary>
/// Representa una definición legacy usada para diagnosticar cómo se obtiene un tipo de registro NACHA-M.
/// </summary>
public class NachaRecordDefinitionDto
{
    /// <summary>Identificador interno persistido de la definición legacy.</summary>
    public int Id { get; set; }

    /// <summary>Código de registro asociado a la definición legacy.</summary>
    public string RecordCode { get; set; } = string.Empty;

    /// <summary>Valor usado para ordenar las definiciones legacy durante su consulta y procesamiento.</summary>
    public int Sequence { get; set; }

    /// <summary>Tipo de origen desde el que la implementación legacy obtiene los datos del registro.</summary>
    public NachaRecordSourceType SourceType { get; set; }

    /// <summary>Nombre opcional del origen usado por los modos Entity, View o Procedure.</summary>
    public string? SourceName { get; set; }

    /// <summary>Clave opcional de filtro del origen legacy; el proveedor actual reconoce CycleId y BatchId para fuentes SQL.</summary>
    public string? FilterKey { get; set; }

    /// <summary>Indica si la definición legacy está habilitada.</summary>
    public bool IsEnabled { get; set; }
}
