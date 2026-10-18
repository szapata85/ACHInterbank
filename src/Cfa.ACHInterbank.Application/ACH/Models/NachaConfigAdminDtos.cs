using System.ComponentModel.DataAnnotations;

namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>Resumen de un perfil de configuración NACHA persistido.</summary>
public sealed class NachaConfigProfileListItemDto
{
    /// <summary>Identificador interno del perfil.</summary>
    public int Id { get; init; }
    /// <summary>Código persistido que identifica el perfil.</summary>
    public string ProfileCode { get; init; } = string.Empty;
    /// <summary>Nombre descriptivo del perfil en español.</summary>
    public string NombreEs { get; init; } = string.Empty;
    /// <summary>Código del estado persistido del perfil.</summary>
    public string Estado { get; init; } = string.Empty;
    /// <summary>Código de la cámara asociada al perfil.</summary>
    public string Camara { get; init; } = string.Empty;
    /// <summary>Código del tipo de flujo asociado al perfil.</summary>
    public string Flujo { get; init; } = string.Empty;
    /// <summary>Código de la dirección asociada al perfil.</summary>
    public string Direccion { get; init; } = string.Empty;
    /// <summary>Código opcional de la clase de servicio asociada.</summary>
    public string? Servicio { get; init; }
    /// <summary>Componente mayor de la versión persistida.</summary>
    public int VersionMajor { get; init; }
    /// <summary>Componente menor de la versión persistida.</summary>
    public int VersionMinor { get; init; }
    /// <summary>Fecha y hora desde la que el perfil tiene vigencia configurada.</summary>
    public DateTime EffectiveFrom { get; init; }
    /// <summary>Fecha y hora opcional hasta la que el perfil tiene vigencia configurada.</summary>
    public DateTime? EffectiveTo { get; init; }
    /// <summary>Token de versión de fila serializado en Base64.</summary>
    public string RowVersion { get; init; } = string.Empty;
}

/// <summary>Detalle de un perfil de configuración NACHA con registros y layouts relacionados.</summary>
public sealed class NachaConfigProfileDetailDto
{
    /// <summary>Identificador interno del perfil.</summary>
    public int Id { get; init; }
    /// <summary>Código persistido que identifica el perfil.</summary>
    public string ProfileCode { get; init; } = string.Empty;
    /// <summary>Nombre descriptivo del perfil en español.</summary>
    public string NombreEs { get; init; } = string.Empty;
    /// <summary>Descripción opcional del perfil.</summary>
    public string? Descripcion { get; init; }
    /// <summary>Código del estado persistido del perfil.</summary>
    public string Estado { get; init; } = string.Empty;
    /// <summary>Nombre funcional en español del estado persistido.</summary>
    public string EstadoNombre { get; init; } = string.Empty;
    /// <summary>Indica si el estado actual permite edición directa del perfil.</summary>
    public bool IsEditable { get; init; }
    /// <summary>Código de la cámara asociada al perfil.</summary>
    public string Camara { get; init; } = string.Empty;
    /// <summary>Nombre de la cámara asociada al perfil.</summary>
    public string CamaraNombre { get; init; } = string.Empty;
    /// <summary>Código del tipo de flujo asociado al perfil.</summary>
    public string Flujo { get; init; } = string.Empty;
    /// <summary>Nombre funcional en español del tipo de flujo asociado.</summary>
    public string FlujoNombre { get; init; } = string.Empty;
    /// <summary>Código de la dirección asociada al perfil.</summary>
    public string Direccion { get; init; } = string.Empty;
    /// <summary>Nombre funcional en español de la dirección asociada.</summary>
    public string DireccionNombre { get; init; } = string.Empty;
    /// <summary>Código opcional de la clase de servicio asociada.</summary>
    public string? Servicio { get; init; }
    /// <summary>Nombre funcional en español de la clase de servicio asociada.</summary>
    public string? ServicioNombre { get; init; }
    /// <summary>Componente mayor de la versión persistida.</summary>
    public int VersionMajor { get; init; }
    /// <summary>Componente menor de la versión persistida.</summary>
    public int VersionMinor { get; init; }
    /// <summary>Prioridad de contexto persistida para el perfil.</summary>
    public int ContextPriority { get; init; }
    /// <summary>Fecha y hora desde la que el perfil tiene vigencia configurada.</summary>
    public DateTime EffectiveFrom { get; init; }
    /// <summary>Fecha y hora opcional hasta la que el perfil tiene vigencia configurada.</summary>
    public DateTime? EffectiveTo { get; init; }
    /// <summary>Token de versión de fila serializado en Base64.</summary>
    public string RowVersion { get; init; } = string.Empty;
    /// <summary>Registros configurados para el perfil, ordenados por secuencia por el servicio de consulta.</summary>
    public IReadOnlyList<NachaConfigProfileRecordDto> Records { get; init; } = [];
    /// <summary>Variantes de layout configuradas para el perfil.</summary>
    public IReadOnlyList<NachaConfigLayoutVariantDto> Variantes { get; init; } = [];
}

/// <summary>Registro habilitable que forma parte de un perfil de configuración NACHA.</summary>
public sealed class NachaConfigProfileRecordDto
{
    /// <summary>Identificador interno de la relación perfil-registro.</summary>
    public int Id { get; init; }
    /// <summary>Código del tipo de registro configurado.</summary>
    public string RecordCode { get; init; } = string.Empty;
    /// <summary>Nombre funcional en español del tipo de registro configurado.</summary>
    public string RecordNameEs { get; init; } = string.Empty;
    /// <summary>Posición de secuencia configurada para el registro.</summary>
    public int Sequence { get; init; }
    /// <summary>Indica si el registro está habilitado en el perfil.</summary>
    public bool IsEnabled { get; init; }
    /// <summary>Número mínimo de ocurrencias configurado.</summary>
    public int MinOccurs { get; init; }
    /// <summary>Número máximo opcional de ocurrencias configurado.</summary>
    public int? MaxOccurs { get; init; }
    /// <summary>Estrategia de origen persistida para el registro.</summary>
    public string SourceStrategy { get; init; } = string.Empty;
    /// <summary>Nombre funcional en español de la estrategia de origen.</summary>
    public string SourceStrategyName { get; init; } = string.Empty;
}

/// <summary>Variante de layout asociada a un tipo de registro de un perfil NACHA.</summary>
public sealed class NachaConfigLayoutVariantDto
{
    /// <summary>Identificador interno de la variante.</summary>
    public int Id { get; init; }
    /// <summary>Código del tipo de registro al que pertenece la variante.</summary>
    public string RecordCode { get; init; } = string.Empty;
    /// <summary>Código persistido de la variante.</summary>
    public string VariantCode { get; init; } = string.Empty;
    /// <summary>Nombre descriptivo de la variante en español.</summary>
    public string NombreEs { get; init; } = string.Empty;
    /// <summary>Descripción opcional de la variante.</summary>
    public string? Descripcion { get; init; }
    /// <summary>Prioridad persistida de la variante dentro del tipo de registro.</summary>
    public int Priority { get; init; }
    /// <summary>Indica si la variante está marcada como predeterminada para el tipo de registro.</summary>
    public bool IsDefaultForRecord { get; init; }
    /// <summary>Longitud total configurada para el layout.</summary>
    public int TotalLength { get; init; }
    /// <summary>Fecha y hora desde la que la variante tiene vigencia configurada.</summary>
    public DateTime EffectiveFrom { get; init; }
    /// <summary>Fecha y hora opcional hasta la que la variante tiene vigencia configurada.</summary>
    public DateTime? EffectiveTo { get; init; }
    /// <summary>Campos que componen la variante de layout.</summary>
    public IReadOnlyList<NachaConfigLayoutFieldDto> Fields { get; init; } = [];
}

/// <summary>Campo configurado dentro de una variante de layout NACHA.</summary>
public sealed class NachaConfigLayoutFieldDto
{
    /// <summary>Identificador interno del campo.</summary>
    public int Id { get; init; }
    /// <summary>Código persistido del campo.</summary>
    public string FieldCode { get; init; } = string.Empty;
    /// <summary>Nombre descriptivo del campo en español.</summary>
    public string FieldNameEs { get; init; } = string.Empty;
    /// <summary>Posición inicial configurada del campo.</summary>
    public int StartPosition { get; init; }
    /// <summary>Longitud configurada del campo.</summary>
    public int Length { get; init; }
    /// <summary>Carácter de relleno configurado.</summary>
    public string PadChar { get; init; } = " ";
    /// <summary>Código de justificación configurado para el campo.</summary>
    public string Justification { get; init; } = "L";
    /// <summary>Máscara de formato opcional configurada.</summary>
    public string? FormatMask { get; init; }
    /// <summary>Orden persistido del campo dentro de su configuración.</summary>
    public int SortOrder { get; init; }
    /// <summary>Indica si el campo está marcado para mostrarse en backoffice.</summary>
    public bool IsVisibleInBackoffice { get; init; }
    /// <summary>Pipeline de transformación persistido en JSON, cuando existe.</summary>
    public string? TransformationPipelineJson { get; init; }
    /// <summary>Ruta de propiedad configurada como fuente del campo.</summary>
    public string? PropertyPath { get; init; }
    /// <summary>Código del tipo de fuente configurado.</summary>
    public string? SourceType { get; init; }
    /// <summary>Nombre descriptivo del tipo de fuente configurado.</summary>
    public string? SourceTypeName { get; init; }
    /// <summary>Identificador interno de la definición de origen actualmente vinculada.</summary>
    public int SourceDefinitionId { get; init; }
    /// <summary>Valor constante configurado como fuente, cuando existe.</summary>
    public string? ConstantValue { get; init; }
    /// <summary>Nombre de entidad configurado como origen, cuando existe.</summary>
    public string? EntityName { get; init; }
    /// <summary>Nombre de objeto SQL configurado como origen, cuando existe.</summary>
    public string? SqlObjectName { get; init; }
    /// <summary>Expresión DSL configurada como origen, cuando existe.</summary>
    public string? ExpressionDsl { get; init; }
    /// <summary>Código de catálogo externo configurado, cuando existe.</summary>
    public string? ExternalCatalogCode { get; init; }
    /// <summary>Política de fallback persistida en JSON, cuando existe.</summary>
    public string? FallbackPolicyJson { get; init; }
    /// <summary>Indica si el campo está habilitado.</summary>
    public bool IsEnabled { get; init; }
    /// <summary>Reglas configuradas para el campo.</summary>
    public IReadOnlyList<NachaConfigFieldRuleDto> Reglas { get; init; } = [];
}

/// <summary>Regla asociada a un campo de layout NACHA.</summary>
public sealed class NachaConfigFieldRuleDto
{
    /// <summary>Identificador interno de la regla.</summary>
    public int Id { get; init; }
    /// <summary>Código persistido del tipo de regla.</summary>
    public string RuleType { get; init; } = string.Empty;
    /// <summary>Nombre funcional en español del tipo de regla.</summary>
    public string RuleTypeName { get; init; } = string.Empty;
    /// <summary>Código de error configurado por la regla.</summary>
    public string ErrorCode { get; init; } = string.Empty;
    /// <summary>Mensaje de error configurado en español.</summary>
    public string ErrorMessageEs { get; init; } = string.Empty;
    /// <summary>Severidad persistida de la regla.</summary>
    public string Severity { get; init; } = string.Empty;
    /// <summary>Nombre funcional en español de la severidad.</summary>
    public string SeverityName { get; init; } = string.Empty;
    /// <summary>Código técnico estable de la regla.</summary>
    public string RuleCode { get; init; } = string.Empty;
    /// <summary>Condición técnica persistida. Se expone solo para información avanzada.</summary>
    public string? ConditionDsl { get; init; }
    /// <summary>Configuración técnica persistida. Se expone solo para información avanzada.</summary>
    public string? RuleConfigJson { get; init; }
    /// <summary>Orden persistido de evaluación.</summary>
    public int Order { get; init; }
    /// <summary>Indica si la regla está habilitada.</summary>
    public bool IsEnabled { get; init; }
}

public sealed class NachaConfigCreateDraftRequest
{
    [Required, StringLength(100, MinimumLength = 6)]
    [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    public string ProfileCode { get; init; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 3)]
    public string NombreEs { get; init; } = string.Empty;

    [StringLength(1000)]
    public string? Descripcion { get; init; }

    [Required, StringLength(40)]
    public string CamaraCode { get; init; } = string.Empty;

    [Required, StringLength(40)]
    public string FlujoCode { get; init; } = string.Empty;

    [Required, StringLength(40)]
    public string DireccionCode { get; init; } = string.Empty;

    [StringLength(40)]
    public string? ServicioCode { get; init; }

    public DateTime EffectiveFrom { get; init; }
}

public sealed class NachaConfigUpdateProfileRequest
{
    [Required, StringLength(200, MinimumLength = 3)]
    public string NombreEs { get; init; } = string.Empty;

    [StringLength(1000)]
    public string? Descripcion { get; init; }

    [Range(1, 10000)]
    public int ContextPriority { get; init; } = 100;

    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }

    [Required]
    public string ExpectedRowVersion { get; init; } = string.Empty;
}

public sealed class NachaConfigCloneProfileRequest
{
    [Required, StringLength(100, MinimumLength = 6)]
    [RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    public string NuevoProfileCode { get; init; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 3)]
    public string NuevoNombreEs { get; init; } = string.Empty;

    public DateTime EffectiveFrom { get; init; }

    [Required]
    public string ExpectedRowVersion { get; init; } = string.Empty;
}

public sealed class NachaConfigValidationResultDto
{
    public int ProfileId { get; init; }
    public bool IsValid { get; init; }
    public int ErroresBloqueantes { get; init; }
    public int Advertencias { get; init; }
    public string Resumen { get; init; } = string.Empty;
    public IReadOnlyList<NachaConfigValidationIssueDto> Issues { get; init; } = [];
}

public sealed class NachaConfigValidationIssueDto
{
    public string Severidad { get; init; } = string.Empty;
    public string Codigo { get; init; } = string.Empty;
    public string Mensaje { get; init; } = string.Empty;
}

/// <summary>Resultado operativo de probar una configuración NACHA-M sin publicarla ni persistir efectos.</summary>
public sealed class NachaConfigOperationalTestResultDto
{
    public int ProfileId { get; init; }
    public DateTime ProcessDateUtc { get; init; }
    public bool CanPublish { get; init; }
    public string ResultLabelEs { get; init; } = string.Empty;
    public int ErroresBloqueantes { get; init; }
    public int Advertencias { get; init; }
    public IReadOnlyList<NachaConfigOperationalIssueDto> Issues { get; init; } = [];
    public NachaConfigSelectionTestDto Selection { get; init; } = new();
    public IReadOnlyList<NachaConfigRecordPreviewDto> Records { get; init; } = [];
}

/// <summary>Incidencia de validación presentada en lenguaje operativo, conservando el código técnico aparte.</summary>
public sealed class NachaConfigOperationalIssueDto
{
    public string Severity { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string MessageEs { get; init; } = string.Empty;
    public string? SuggestedActionEs { get; init; }
    public string? RecordCode { get; init; }
    public string? RecordNameEs { get; init; }
    public int? FieldId { get; init; }
    public string? FieldNameEs { get; init; }
    public string TechnicalMessage { get; init; } = string.Empty;
}

/// <summary>Prueba contextual de selección del perfil productivo que resolvería el backend para la fecha indicada.</summary>
public sealed class NachaConfigSelectionTestDto
{
    public bool Success { get; init; }
    public string SummaryEs { get; init; } = string.Empty;
    public int? ResolvedProfileId { get; init; }
    public string? ResolvedProfileCode { get; init; }
    public IReadOnlyList<string> WarningsEs { get; init; } = [];
    public IReadOnlyList<string> TechnicalTrace { get; init; } = [];
}

/// <summary>Vista previa estructural y, cuando el renderer oficial lo permite, ejemplo de contenido de un registro.</summary>
public sealed class NachaConfigRecordPreviewDto
{
    public int VariantId { get; init; }
    public string RecordCode { get; init; } = string.Empty;
    public string RecordNameEs { get; init; } = string.Empty;
    public string VariantCode { get; init; } = string.Empty;
    public string VariantNameEs { get; init; } = string.Empty;
    public int TotalLength { get; init; }
    public bool IsDefaultForRecord { get; init; }
    public bool ContentPreviewAvailable { get; init; }
    public string? ContentPreviewMessageEs { get; init; }
    public string? RenderedRecord { get; init; }
    public IReadOnlyList<NachaConfigFieldPreviewDto> Fields { get; init; } = [];
}

/// <summary>Bloque de campo dentro de la vista previa operativa.</summary>
public sealed class NachaConfigFieldPreviewDto
{
    public int FieldId { get; init; }
    public string FieldNameEs { get; init; } = string.Empty;
    public int StartPosition { get; init; }
    public int EndPosition { get; init; }
    public int Length { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsRequired { get; init; }
    public string SourceSummaryEs { get; init; } = string.Empty;
    public string? FormatSummaryEs { get; init; }
    public string? SampleValue { get; init; }
    public IReadOnlyList<string> RelevantRulesEs { get; init; } = [];
}

public sealed class NachaConfigPublicationResultDto
{
    public int ProfileId { get; init; }
    public bool Publicado { get; init; }
    public string Mensaje { get; init; } = string.Empty;
    public int VersionMajor { get; init; }
    public int VersionMinor { get; init; }
    public string Estado { get; init; } = string.Empty;
    public string EstadoNombreEs { get; init; } = string.Empty;
    public string? RowVersion { get; init; }
}

/// <summary>Revisión humanizada de diferencias entre un borrador y la configuración que reemplaza.</summary>
public sealed class NachaConfigChangeReviewDto
{
    public int ProfileId { get; init; }
    public int? BaseProfileId { get; init; }
    public bool HasComparisonBase { get; init; }
    public bool IsEditable { get; init; }
    public string RowVersion { get; init; } = string.Empty;
    public string CurrentVersion { get; init; } = string.Empty;
    public string NextVersion { get; init; } = string.Empty;
    public string CamaraNombreEs { get; init; } = string.Empty;
    public string FlujoNombreEs { get; init; } = string.Empty;
    public string DireccionNombreEs { get; init; } = string.Empty;
    public string? ServicioNombreEs { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public int ChangedFieldsCount { get; init; }
    public int ChangeCount { get; init; }
    public string SummaryEs { get; init; } = string.Empty;
    public IReadOnlyList<NachaConfigChangeItemDto> Changes { get; init; } = [];
}

/// <summary>Diferencia operativa individual; los valores técnicos permanecen fuera de la presentación principal.</summary>
public sealed class NachaConfigChangeItemDto
{
    public string Scope { get; init; } = string.Empty;
    public string ChangeKind { get; init; } = string.Empty;
    public string ElementNameEs { get; init; } = string.Empty;
    public string? RecordCode { get; init; }
    public string? RecordNameEs { get; init; }
    public int? FieldId { get; init; }
    public string? FieldNameEs { get; init; }
    public string PropertyNameEs { get; init; } = string.Empty;
    public string? BeforeValueEs { get; init; }
    public string? AfterValueEs { get; init; }
    public string SummaryEs { get; init; } = string.Empty;
}

/// <summary>Evento histórico registrado para un perfil de configuración NACHA.</summary>
public sealed class NachaConfigHistoryItemDto
{
    /// <summary>Identificador interno del evento histórico.</summary>
    public int Id { get; init; }
    /// <summary>Fecha y hora UTC en que se registró el cambio.</summary>
    public DateTime ChangedAtUtc { get; init; }
    /// <summary>Actor registrado como responsable del cambio.</summary>
    public string ChangedBy { get; init; } = string.Empty;
    /// <summary>Tipo de cambio registrado.</summary>
    public string ChangeType { get; init; } = string.Empty;
    /// <summary>Nombre de la entidad afectada por el cambio.</summary>
    public string EntityName { get; init; } = string.Empty;
    /// <summary>Identificador de correlación asociado al cambio, cuando existe.</summary>
    public string? CorrelationId { get; init; }
    /// <summary>Acción presentada en lenguaje operativo.</summary>
    public string ActionNameEs { get; init; } = string.Empty;
    /// <summary>Elemento afectado presentado en lenguaje operativo.</summary>
    public string ElementNameEs { get; init; } = string.Empty;
    /// <summary>Versión a la que corresponde el evento.</summary>
    public string Version { get; init; } = string.Empty;
    /// <summary>Resumen breve y entendible del evento.</summary>
    public string SummaryEs { get; init; } = string.Empty;
    /// <summary>Detalle humanizado cuando el evento contiene una comparación útil.</summary>
    public IReadOnlyList<string> DetailEs { get; init; } = [];
}

/// <summary>Snapshot histórico asociado a un perfil de configuración NACHA.</summary>
public sealed class NachaConfigSnapshotItemDto
{
    /// <summary>Identificador interno del snapshot.</summary>
    public int Id { get; init; }
    /// <summary>Fecha y hora UTC de creación del snapshot.</summary>
    public DateTime CreatedAtUtc { get; init; }
    /// <summary>Actor registrado como creador del snapshot.</summary>
    public string CreatedBy { get; init; } = string.Empty;
    /// <summary>Tipo persistido del snapshot.</summary>
    public string SnapshotType { get; init; } = string.Empty;
    /// <summary>Componente mayor de la versión capturada.</summary>
    public int VersionMajor { get; init; }
    /// <summary>Componente menor de la versión capturada.</summary>
    public int VersionMinor { get; init; }
}

public sealed class NachaConfigResolverPreviewRequest
{
    public string CamaraCode { get; init; } = "ACH";
    public string FlujoCode { get; init; } = "ORIGINAL";
    public string DireccionCode { get; init; } = "SALIDA";
    public string? ServicioCode { get; init; }
    public DateTime ProcessDateUtc { get; init; }
    public IReadOnlyList<string> RecordCodes { get; init; } = [];
}

public sealed class NachaConfigResolverPreviewResultDto
{
    public bool Success { get; init; }
    public int? ProfileId { get; init; }
    public string? ProfileCode { get; init; }
    public Dictionary<string, string> LayoutByRecordCode { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyList<string> Trace { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public sealed class NachaConfigProfileRecordSequenceDto
{
    public int ProfileRecordId { get; init; }
    public int Sequence { get; init; }
}

public sealed class NachaConfigRecordSequenceUpdateRequest
{
    public string ExpectedRowVersion { get; init; } = string.Empty;
    public IReadOnlyList<NachaConfigProfileRecordSequenceDto> Records { get; init; } = [];
}

public sealed class NachaConfigLayoutVariantEditDto
{
    public string NombreEs { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
    public int Priority { get; init; }
    public bool IsDefaultForRecord { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public string ExpectedRowVersion { get; init; } = string.Empty;
}


/// <summary>Opción simple entregada por el backend para el editor operativo.</summary>
public sealed class NachaConfigEditorOptionDto
{
    public string Code { get; init; } = string.Empty;
    public string LabelEs { get; init; } = string.Empty;
}

/// <summary>Tipo de origen disponible para el editor, proveniente del catálogo persistido.</summary>
public sealed class NachaConfigEditorSourceTypeOptionDto
{
    public string Code { get; init; } = string.Empty;
    public string LabelEs { get; init; } = string.Empty;
    public string CatalogLabelEs { get; init; } = string.Empty;
    public bool UsesFixedValue { get; init; }
    public bool RequiresDefinitionSelection { get; init; }
}

/// <summary>Origen persistido reutilizable dentro de la misma cámara.</summary>
public sealed class NachaConfigEditorSourceOptionDto
{
    public int SourceDefinitionId { get; init; }
    public string SourceTypeCode { get; init; } = string.Empty;
    public string LabelEs { get; init; } = string.Empty;
    public string? DescriptionEs { get; init; }
}

/// <summary>Read model contextual para editar un campo sin exponer rutas internas en el flujo normal.</summary>
public sealed class NachaConfigFieldEditorDto
{
    public int ProfileId { get; init; }
    public int FieldId { get; init; }
    public bool IsEditable { get; init; }
    public string RowVersion { get; init; } = string.Empty;
    public NachaConfigLayoutFieldDto Field { get; init; } = new();
    public IReadOnlyList<NachaConfigEditorSourceTypeOptionDto> SourceTypes { get; init; } = [];
    public IReadOnlyList<NachaConfigEditorSourceOptionDto> SourceOptions { get; init; } = [];
    public IReadOnlyList<NachaConfigEditorOptionDto> Alignments { get; init; } = [];
    public IReadOnlyList<NachaConfigEditorOptionDto> Formats { get; init; } = [];
    public IReadOnlyList<NachaConfigEditorOptionDto> Severities { get; init; } = [];
    public int SourceReferenceCount { get; init; }
}

public sealed class NachaConfigFieldSourceEditDto
{
    [Required, StringLength(40)]
    public string SourceTypeCode { get; init; } = string.Empty;
    public int? SourceDefinitionId { get; init; }
    [StringLength(400)]
    public string? FixedValue { get; init; }
}

public sealed class NachaConfigFieldRuleEditorUpdateDto
{
    public int Id { get; init; }
    [Required, StringLength(500)]
    public string ErrorMessageEs { get; init; } = string.Empty;
    [Required, StringLength(10)]
    public string Severity { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
}

/// <summary>Comando tipado de edición operativa de un campo NACHA-M.</summary>
public sealed class NachaConfigFieldEditorUpdateRequest
{
    [Required, StringLength(150)]
    public string FieldNameEs { get; init; } = string.Empty;
    [Range(1, int.MaxValue)]
    public int StartPosition { get; init; }
    [Range(1, int.MaxValue)]
    public int Length { get; init; }
    [Required, StringLength(1, MinimumLength = 1)]
    public string PadChar { get; init; } = " ";
    [Required, StringLength(1, MinimumLength = 1)]
    public string Justification { get; init; } = "L";
    [StringLength(80)]
    public string? FormatMask { get; init; }
    public bool IsEnabled { get; init; }
    [Required]
    public NachaConfigFieldSourceEditDto Source { get; init; } = new();
    public IReadOnlyList<NachaConfigFieldRuleEditorUpdateDto> Rules { get; init; } = [];
    [Required]
    public string ExpectedRowVersion { get; init; } = string.Empty;
}

public sealed class NachaConfigLayoutFieldEditDto
{
    public string FieldNameEs { get; init; } = string.Empty;
    public int StartPosition { get; init; }
    public int Length { get; init; }
    public string? PropertyPath { get; init; }
    public bool IsEnabled { get; init; }
    public string ExpectedRowVersion { get; init; } = string.Empty;
}

public sealed class NachaConfigFieldRuleEditDto
{
    public string ErrorCode { get; init; } = string.Empty;
    public string ErrorMessageEs { get; init; } = string.Empty;
    public string Severity { get; init; } = "ERROR";
    public bool IsEnabled { get; init; }
    public string ExpectedRowVersion { get; init; } = string.Empty;
}

public sealed class NachaConfigStateTransitionRequest
{
    [Required]
    public string ExpectedRowVersion { get; init; } = string.Empty;
}

public sealed class NachaConfigApiErrorDto
{
    public string ErrorCode { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? CurrentRowVersion { get; init; }
    public IReadOnlyList<NachaConfigValidationIssueDto> Issues { get; init; } = [];
}

/// <summary>Opción de un catálogo utilizado para filtrar perfiles de configuración NACHA.</summary>
public sealed class NachaConfigFilterCatalogOptionDto
{
    /// <summary>Código persistido de la opción.</summary>
    public string Code { get; init; } = string.Empty;
    /// <summary>Etiqueta en español expuesta para la opción.</summary>
    public string LabelEs { get; init; } = string.Empty;
}

/// <summary>Catálogos utilizados por la interfaz de consulta de configuración NACHA.</summary>
public sealed class NachaConfigFilterCatalogsDto
{
    /// <summary>Estados de configuración disponibles, ordenados por código.</summary>
    public IReadOnlyList<NachaConfigFilterCatalogOptionDto> Estados { get; init; } = [];
    /// <summary>Cámaras activas disponibles, ordenadas por código.</summary>
    public IReadOnlyList<NachaConfigFilterCatalogOptionDto> Camaras { get; init; } = [];
    /// <summary>Tipos de flujo activos disponibles, ordenados por código.</summary>
    public IReadOnlyList<NachaConfigFilterCatalogOptionDto> Flujos { get; init; } = [];
    /// <summary>Direcciones activas disponibles, ordenadas por código.</summary>
    public IReadOnlyList<NachaConfigFilterCatalogOptionDto> Direcciones { get; init; } = [];
    /// <summary>Clases de servicio activas disponibles, ordenadas por código.</summary>
    public IReadOnlyList<NachaConfigFilterCatalogOptionDto> Servicios { get; init; } = [];
    /// <summary>Tipos de registro persistidos disponibles, ordenados por código.</summary>
    public IReadOnlyList<NachaConfigFilterCatalogOptionDto> Registros { get; init; } = [];
    /// <summary>Tipos de origen de datos persistidos disponibles, ordenados por código.</summary>
    public IReadOnlyList<NachaConfigFilterCatalogOptionDto> TiposOrigen { get; init; } = [];
    /// <summary>Tipos de regla persistidos disponibles, ordenados por código.</summary>
    public IReadOnlyList<NachaConfigFilterCatalogOptionDto> TiposRegla { get; init; } = [];
}

/// <summary>
/// Read model de trabajo para una experiencia operativa NACHA-M. Mantiene códigos técnicos para
/// trazabilidad y expone los nombres funcionales persistidos que la SPA necesita para presentar el perfil.
/// </summary>
public sealed class NachaConfigWorkspaceDto
{
    /// <summary>Identificador interno del perfil.</summary>
    public int Id { get; init; }
    /// <summary>Código técnico estable del perfil.</summary>
    public string ProfileCode { get; init; } = string.Empty;
    /// <summary>Nombre funcional del perfil en español.</summary>
    public string NombreEs { get; init; } = string.Empty;
    /// <summary>Descripción funcional del perfil, cuando exista.</summary>
    public string? Descripcion { get; init; }
    /// <summary>Código técnico del estado del perfil.</summary>
    public string Estado { get; init; } = string.Empty;
    /// <summary>Nombre legible del estado del perfil.</summary>
    public string EstadoNombre { get; init; } = string.Empty;
    /// <summary>Indica si el estado actual del perfil admite edición.</summary>
    public bool IsEditable { get; init; }
    /// <summary>Código técnico de la cámara configurada.</summary>
    public string Camara { get; init; } = string.Empty;
    /// <summary>Nombre funcional de la cámara configurada.</summary>
    public string CamaraNombre { get; init; } = string.Empty;
    /// <summary>Código técnico del flujo configurado.</summary>
    public string Flujo { get; init; } = string.Empty;
    /// <summary>Nombre funcional del flujo configurado.</summary>
    public string FlujoNombre { get; init; } = string.Empty;
    /// <summary>Código técnico de la dirección configurada.</summary>
    public string Direccion { get; init; } = string.Empty;
    /// <summary>Nombre funcional de la dirección configurada.</summary>
    public string DireccionNombre { get; init; } = string.Empty;
    /// <summary>Código técnico del servicio configurado, cuando aplique.</summary>
    public string? Servicio { get; init; }
    /// <summary>Nombre funcional del servicio configurado, cuando aplique.</summary>
    public string? ServicioNombre { get; init; }
    /// <summary>Versión mayor del perfil.</summary>
    public int VersionMajor { get; init; }
    /// <summary>Versión menor del perfil.</summary>
    public int VersionMinor { get; init; }
    /// <summary>Prioridad de resolución contextual configurada para el perfil.</summary>
    public int ContextPriority { get; init; }
    /// <summary>Inicio de vigencia del perfil.</summary>
    public DateTime EffectiveFrom { get; init; }
    /// <summary>Fin de vigencia del perfil, cuando aplique.</summary>
    public DateTime? EffectiveTo { get; init; }
    /// <summary>Versión de fila usada para control de concurrencia.</summary>
    public string RowVersion { get; init; } = string.Empty;
    /// <summary>Registros configurados para el perfil con nombres persistidos.</summary>
    public IReadOnlyList<NachaConfigProfileRecordDto> Records { get; init; } = [];
    /// <summary>Variantes de layout configuradas para el perfil.</summary>
    public IReadOnlyList<NachaConfigLayoutVariantDto> Variantes { get; init; } = [];
}
