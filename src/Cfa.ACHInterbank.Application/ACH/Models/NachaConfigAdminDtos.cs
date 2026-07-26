using System.ComponentModel.DataAnnotations;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed class NachaConfigProfileListItemDto
{
    public int Id { get; init; }
    public string ProfileCode { get; init; } = string.Empty;
    public string NombreEs { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public string Camara { get; init; } = string.Empty;
    public string Flujo { get; init; } = string.Empty;
    public string Direccion { get; init; } = string.Empty;
    public string? Servicio { get; init; }
    public int VersionMajor { get; init; }
    public int VersionMinor { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public string RowVersion { get; init; } = string.Empty;
}

public sealed class NachaConfigProfileDetailDto
{
    public int Id { get; init; }
    public string ProfileCode { get; init; } = string.Empty;
    public string NombreEs { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
    public string Estado { get; init; } = string.Empty;
    public string Camara { get; init; } = string.Empty;
    public string CamaraNombre { get; init; } = string.Empty;
    public string Flujo { get; init; } = string.Empty;
    public string Direccion { get; init; } = string.Empty;
    public string? Servicio { get; init; }
    public int VersionMajor { get; init; }
    public int VersionMinor { get; init; }
    public int ContextPriority { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public string RowVersion { get; init; } = string.Empty;
    public IReadOnlyList<NachaConfigProfileRecordDto> Records { get; init; } = [];
    public IReadOnlyList<NachaConfigLayoutVariantDto> Variantes { get; init; } = [];
}

public sealed class NachaConfigProfileRecordDto
{
    public int Id { get; init; }
    public string RecordCode { get; init; } = string.Empty;
    public int Sequence { get; init; }
    public bool IsEnabled { get; init; }
    public int MinOccurs { get; init; }
    public int? MaxOccurs { get; init; }
    public string SourceStrategy { get; init; } = string.Empty;
}

public sealed class NachaConfigLayoutVariantDto
{
    public int Id { get; init; }
    public string RecordCode { get; init; } = string.Empty;
    public string VariantCode { get; init; } = string.Empty;
    public string NombreEs { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
    public int Priority { get; init; }
    public bool IsDefaultForRecord { get; init; }
    public int TotalLength { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public IReadOnlyList<NachaConfigLayoutFieldDto> Fields { get; init; } = [];
}

public sealed class NachaConfigLayoutFieldDto
{
    public int Id { get; init; }
    public string FieldCode { get; init; } = string.Empty;
    public string FieldNameEs { get; init; } = string.Empty;
    public int StartPosition { get; init; }
    public int Length { get; init; }
    public string PadChar { get; init; } = " ";
    public string Justification { get; init; } = "L";
    public string? FormatMask { get; init; }
    public int SortOrder { get; init; }
    public bool IsVisibleInBackoffice { get; init; }
    public string? TransformationPipelineJson { get; init; }
    public string? PropertyPath { get; init; }
    public string? SourceType { get; init; }
    public string? SourceTypeName { get; init; }
    public string? ConstantValue { get; init; }
    public string? EntityName { get; init; }
    public string? SqlObjectName { get; init; }
    public string? ExpressionDsl { get; init; }
    public string? ExternalCatalogCode { get; init; }
    public string? FallbackPolicyJson { get; init; }
    public bool IsEnabled { get; init; }
    public IReadOnlyList<NachaConfigFieldRuleDto> Reglas { get; init; } = [];
}

public sealed class NachaConfigFieldRuleDto
{
    public int Id { get; init; }
    public string ErrorCode { get; init; } = string.Empty;
    public string ErrorMessageEs { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
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

public sealed class NachaConfigPublicationResultDto
{
    public int ProfileId { get; init; }
    public bool Publicado { get; init; }
    public string Mensaje { get; init; } = string.Empty;
    public int VersionMajor { get; init; }
    public int VersionMinor { get; init; }
    public string? RowVersion { get; init; }
}

public sealed class NachaConfigHistoryItemDto
{
    public int Id { get; init; }
    public DateTime ChangedAtUtc { get; init; }
    public string ChangedBy { get; init; } = string.Empty;
    public string ChangeType { get; init; } = string.Empty;
    public string EntityName { get; init; } = string.Empty;
    public string? CorrelationId { get; init; }
}

public sealed class NachaConfigSnapshotItemDto
{
    public int Id { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public string SnapshotType { get; init; } = string.Empty;
    public int VersionMajor { get; init; }
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

public sealed class NachaConfigFilterCatalogOptionDto
{
    public string Code { get; init; } = string.Empty;
    public string LabelEs { get; init; } = string.Empty;
}

public sealed class NachaConfigFilterCatalogsDto
{
    public IReadOnlyList<NachaConfigFilterCatalogOptionDto> Estados { get; init; } = [];
    public IReadOnlyList<NachaConfigFilterCatalogOptionDto> Camaras { get; init; } = [];
    public IReadOnlyList<NachaConfigFilterCatalogOptionDto> Flujos { get; init; } = [];
    public IReadOnlyList<NachaConfigFilterCatalogOptionDto> Direcciones { get; init; } = [];
    public IReadOnlyList<NachaConfigFilterCatalogOptionDto> Servicios { get; init; } = [];
}
