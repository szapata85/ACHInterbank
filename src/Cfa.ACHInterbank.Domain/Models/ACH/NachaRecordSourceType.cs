namespace Cfa.ACHInterbank.Domain.Models.ACH;

/// <summary>
/// Modos de origen soportados por la obtención de datos de definiciones NACHA-M legacy.
/// </summary>
public enum NachaRecordSourceType
{
    /// <summary>Usa los registros provistos por el flujo legacy en lugar de consultar un origen externo.</summary>
    Custom = 0,

    /// <summary>Obtiene registros desde una entidad soportada identificada por SourceName.</summary>
    Entity = 1,

    /// <summary>Obtiene registros mediante la consulta SQL construida para SourceName.</summary>
    View = 2,

    /// <summary>Obtiene registros ejecutando SourceName como origen SQL de procedimiento.</summary>
    Procedure = 3
}
