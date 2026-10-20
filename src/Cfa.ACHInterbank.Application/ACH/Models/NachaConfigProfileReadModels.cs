namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>Resumen agregado de los perfiles NACHA expuestos por el read model oficial.</summary>
public sealed class NachaConfigProfilesDashboardReadModel
{
    /// <summary>Estado productivo fijo expuesto por este read model.</summary>
    public string ProductiveStatus { get; init; } = "NO-GO";
    /// <summary>Indica que la proyección corresponde al modelo oficial.</summary>
    public bool IsOfficialModel { get; init; } = true;
    /// <summary>Indica que la superficie legacy relacionada está marcada como deprecada.</summary>
    public bool LegacyDeprecated { get; init; } = true;
    /// <summary>Número total de perfiles persistidos incluidos en el resumen.</summary>
    public int ProfileCount { get; init; }
    /// <summary>Número de perfiles cuyo estado es PUBLICADO.</summary>
    public int PublishedProfileCount { get; init; }
    /// <summary>Número de perfiles cuya ventana de vigencia contiene el instante UTC evaluado por el servicio.</summary>
    public int CurrentProfileCount { get; init; }
    /// <summary>Total de variantes de layout asociadas a los perfiles resumidos.</summary>
    public int LayoutVariantCount { get; init; }
    /// <summary>Total de campos pertenecientes a las variantes de los perfiles resumidos.</summary>
    public int FieldCount { get; init; }
    /// <summary>Códigos distintos de cámaras presentes en los perfiles, ordenados alfabéticamente.</summary>
    public IReadOnlyList<string> ClearingHouses { get; init; } = [];
    /// <summary>Códigos distintos de tipos de registro presentes en los perfiles, ordenados alfabéticamente.</summary>
    public IReadOnlyList<string> RecordTypes { get; init; } = [];
    /// <summary>Avisos generados por el servicio de lectura; actualmente informa cuando no existen perfiles persistidos.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

/// <summary>Proyección de solo lectura de un perfil de configuración NACHA.</summary>
public class NachaConfigProfileReadModel
{
    /// <summary>Identificador interno del perfil.</summary>
    public int ProfileId { get; init; }
    /// <summary>Código persistido que identifica el perfil.</summary>
    public string ProfileCode { get; init; } = string.Empty;
    /// <summary>Nombre descriptivo persistido del perfil.</summary>
    public string ProfileName { get; init; } = string.Empty;
    /// <summary>Código de la cámara asociada al perfil.</summary>
    public string ClearingHouseCode { get; init; } = string.Empty;
    /// <summary>Código del tipo de flujo asociado al perfil.</summary>
    public string FlowType { get; init; } = string.Empty;
    /// <summary>Código del estado persistido del perfil.</summary>
    public string Status { get; init; } = string.Empty;
    /// <summary>Versión formateada por el servicio como v&lt;major&gt;.&lt;minor&gt;.</summary>
    public string Version { get; init; } = string.Empty;
    /// <summary>Indica si el estado del perfil es PUBLICADO.</summary>
    public bool IsPublished { get; init; }
    /// <summary>Indica si el instante UTC evaluado por el servicio está dentro de la vigencia EffectiveFrom/EffectiveTo.</summary>
    public bool IsCurrent { get; init; }
    /// <summary>Inicio de vigencia configurado para el perfil.</summary>
    public DateTime EffectiveFrom { get; init; }
    /// <summary>Fin opcional de vigencia configurado para el perfil.</summary>
    public DateTime? EffectiveTo { get; init; }
    /// <summary>Número de variantes de layout asociadas al perfil.</summary>
    public int LayoutVariantCount { get; init; }
    /// <summary>Número total de campos pertenecientes a las variantes del perfil.</summary>
    public int FieldCount { get; init; }
    /// <summary>Tipos de registro distintos asociados al perfil, ordenados alfabéticamente.</summary>
    public IReadOnlyList<string> RecordTypes { get; init; } = [];
    /// <summary>Indica que la proyección corresponde al modelo oficial.</summary>
    public bool IsOfficialModel { get; init; } = true;
    /// <summary>Indica que la superficie legacy relacionada está marcada como deprecada.</summary>
    public bool LegacyDeprecated { get; init; } = true;
}

/// <summary>Detalle de solo lectura de un perfil NACHA con variantes y campos proyectados.</summary>
public sealed class NachaConfigProfileDetailReadModel : NachaConfigProfileReadModel
{
    /// <summary>Variantes de layout del perfil proyectadas por el servicio.</summary>
    public IReadOnlyList<NachaConfigProfileVariantReadModel> Variants { get; init; } = [];
    /// <summary>Campos de todas las variantes del perfil proyectados por el servicio.</summary>
    public IReadOnlyList<NachaConfigProfileFieldReadModel> Fields { get; init; } = [];
}

/// <summary>Proyección de solo lectura de una variante de layout NACHA.</summary>
public sealed class NachaConfigProfileVariantReadModel
{
    /// <summary>Identificador interno de la variante.</summary>
    public int VariantId { get; init; }
    /// <summary>Código persistido de la variante.</summary>
    public string VariantCode { get; init; } = string.Empty;
    /// <summary>Código del tipo de registro al que pertenece la variante.</summary>
    public string RecordType { get; init; } = string.Empty;
    /// <summary>Longitud total configurada para el registro de la variante.</summary>
    public int RecordLength { get; init; }
    /// <summary>Factor de bloqueo derivado del campo BLOCKINGFACTOR o 10 cuando no puede obtenerse un entero.</summary>
    public int BlockingFactor { get; init; }
    /// <summary>Indica si la variante es la predeterminada del registro y su estado es PUBLICADO.</summary>
    public bool IsActive { get; init; }
    /// <summary>Número de campos asociados a la variante.</summary>
    public int FieldCount { get; init; }
}

/// <summary>Proyección de solo lectura de un campo de layout NACHA.</summary>
public sealed class NachaConfigProfileFieldReadModel
{
    /// <summary>Identificador interno del campo.</summary>
    public int FieldId { get; init; }
    /// <summary>Código del tipo de registro al que pertenece el campo.</summary>
    public string RecordType { get; init; } = string.Empty;
    /// <summary>Nombre en español del campo o, si está vacío, su código persistido.</summary>
    public string FieldName { get; init; } = string.Empty;
    /// <summary>Posición inicial configurada del campo.</summary>
    public int StartPosition { get; init; }
    /// <summary>Longitud configurada del campo.</summary>
    public int Length { get; init; }
    /// <summary>Posición final calculada como StartPosition + Length - 1.</summary>
    public int EndPosition { get; init; }
    /// <summary>Código del tipo de fuente de datos asociado al campo.</summary>
    public string DataType { get; init; } = string.Empty;
    /// <summary>Refleja si el campo está habilitado en la configuración persistida.</summary>
    public bool IsRequired { get; init; }
    /// <summary>Valor constante configurado como fuente del campo, cuando existe.</summary>
    public string? DefaultValue { get; init; }
    /// <summary>Ruta de fuente derivada de entidad y propiedad, propiedad directa o la marca computed para expresiones.</summary>
    public string? SourceFieldPath { get; init; }
    /// <summary>Dirección de padding derivada de la justificación: LeftPad para R y RightPad en otro caso.</summary>
    public string PaddingDirection { get; init; } = string.Empty;
    /// <summary>Carácter de relleno configurado.</summary>
    public string PaddingChar { get; init; } = string.Empty;
    /// <summary>Máscara de formato configurada, cuando existe.</summary>
    public string? Format { get; init; }
    /// <summary>Indica si el tipo de fuente del campo es EXPRESION.</summary>
    public bool IsComputed { get; init; }
    /// <summary>Indica si el código del campo pertenece al conjunto de campos de total de control reconocido por el servicio.</summary>
    public bool IsControlTotalField { get; init; }
}
