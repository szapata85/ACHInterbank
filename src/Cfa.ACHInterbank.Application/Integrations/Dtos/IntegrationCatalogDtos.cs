using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.Application.Integrations.Dtos;

/// <summary>Método expuesto por el catálogo de integración.</summary>
/// <param name="Id">Identificador persistido del método.</param>
/// <param name="Code">Código técnico persistido que identifica el método.</param>
/// <param name="DisplayName">Nombre descriptivo persistido.</param>
/// <param name="SoapClientCode">Código de cliente SOAP asociado al método.</param>
/// <param name="IsActive">Indica si el método está activo en el catálogo.</param>
/// <param name="IntegrationKey">Clave de integración calculada por el servicio a partir del código.</param>
/// <param name="OperationKey">Clave de operación calculada por el servicio a partir del código.</param>
/// <param name="MappingDirection">Clasificación de dirección de mapping calculada por el servicio.</param>
/// <param name="MappingPurpose">Propósito funcional de mapping calculado por el servicio.</param>
/// <param name="FunctionalNature">Naturaleza funcional calculada para el método.</param>
/// <param name="FunctionalOriginator">Origen funcional calculado para el método.</param>
/// <param name="MovesMoney">Indica la clasificación del servicio sobre si el método representa movimiento monetario.</param>
public sealed record IntegrationMethodDto(
    int Id,
    string Code,
    string DisplayName,
    string SoapClientCode,
    bool IsActive,
    string IntegrationKey,
    string OperationKey,
    string MappingDirection,
    string MappingPurpose,
    string FunctionalNature,
    string FunctionalOriginator,
    bool MovesMoney);

/// <summary>Parámetro activo perteneciente a un método del catálogo de integración.</summary>
/// <param name="Id">Identificador persistido del parámetro.</param>
/// <param name="MethodId">Identificador del método de integración propietario.</param>
/// <param name="ParameterPath">Ruta o nombre técnico del parámetro.</param>
/// <param name="DisplayName">Nombre descriptivo del parámetro.</param>
/// <param name="DescriptionEs">Descripción funcional en español persistida en el catálogo.</param>
/// <param name="Category">Categoría funcional persistida.</param>
/// <param name="ExampleValue">Valor ilustrativo almacenado por el catálogo para apoyar la configuración.</param>
/// <param name="UiHelpText">Texto de ayuda persistido para la interfaz de configuración.</param>
/// <param name="DataType">Tipo de dato declarado por el catálogo.</param>
/// <param name="Direction">Dirección de entrada o salida del parámetro.</param>
/// <param name="Cardinality">Cardinalidad declarada para el parámetro.</param>
/// <param name="Required">Indica si el catálogo marca el parámetro como requerido.</param>
/// <param name="SortOrder">Orden de presentación persistido.</param>
/// <param name="IsActive">Indica si el parámetro está activo.</param>
public sealed record IntegrationMethodParameterDto(
    long Id,
    int MethodId,
    string ParameterPath,
    string DisplayName,
    string DescriptionEs,
    string Category,
    string ExampleValue,
    string UiHelpText,
    string DataType,
    IntegrationParameterDirectionEnum Direction,
    IntegrationParameterCardinalityEnum Cardinality,
    bool Required,
    int SortOrder,
    bool IsActive);

/// <summary>Campo fuente que puede participar en mappings de integración.</summary>
/// <param name="Id">Identificador persistido del campo fuente.</param>
/// <param name="MethodId">Método específico al que pertenece; null identifica un campo global.</param>
/// <param name="SourceKind">Clase de origen del campo.</param>
/// <param name="EntityName">Nombre de la entidad o contexto fuente.</param>
/// <param name="FieldPath">Ruta técnica del campo fuente.</param>
/// <param name="DisplayName">Nombre descriptivo del campo.</param>
/// <param name="DataType">Tipo de dato declarado por el catálogo.</param>
/// <param name="Cardinality">Cardinalidad declarada para el campo.</param>
/// <param name="Nullable">Indica si el catálogo permite ausencia de valor.</param>
/// <param name="SortOrder">Orden de presentación persistido.</param>
/// <param name="IsActive">Indica si el campo está activo.</param>
public sealed record IntegrationSourceCatalogFieldDto(
    long Id,
    int? MethodId,
    IntegrationSourceKindEnum SourceKind,
    string EntityName,
    string FieldPath,
    string DisplayName,
    string DataType,
    IntegrationParameterCardinalityEnum Cardinality,
    bool Nullable,
    int SortOrder,
    bool IsActive);

/// <summary>Transformación disponible para reglas de mapping de integración.</summary>
/// <param name="Code">Código técnico de la transformación.</param>
/// <param name="DisplayName">Nombre presentado por el catálogo.</param>
/// <param name="Description">Descripción funcional mantenida por el servicio.</param>
/// <param name="SupportsFormatMask">Indica si la transformación admite una máscara de formato.</param>
/// <param name="SupportsMultipleSources">Indica si la transformación admite múltiples fuentes.</param>
public sealed record IntegrationTransformationCatalogDto(
    string Code,
    string DisplayName,
    string Description,
    bool SupportsFormatMask,
    bool SupportsMultipleSources = false);

/// <summary>Versión persistida del contrato SOAP asociado a un método de integración.</summary>
public sealed record IntegrationContractVersionDto(
    Guid Id,
    int MethodId,
    string VersionLabel,
    IntegrationContractVersionStatusEnum Status,
    string OperationName,
    string TargetNamespace,
    string SoapAction,
    IntegrationContractSourceTypeEnum SourceType,
    string? SourceDocumentName,
    string? SourceDocumentHash,
    string? SourceReference,
    string? Notes,
    long Revision,
    DateTime? ImportedAtUtc,
    string? ImportedBy,
    DateTime? ActivatedAtUtc,
    string? ActivatedBy)
{
    /// <summary>Capacidades de UI derivadas en backend de las mismas reglas de mutabilidad contractual.</summary>
    public IntegrationContractVersionCapabilitiesDto Capabilities => new(
        IsReadOnly: Status is IntegrationContractVersionStatusEnum.Active or IntegrationContractVersionStatusEnum.Retired,
        CanEditFunctionalMetadata: Status is IntegrationContractVersionStatusEnum.Draft or IntegrationContractVersionStatusEnum.Validated);
}

/// <summary>Capacidades contractuales que la UI consume sin reconstruir lifecycle en Angular.</summary>
public sealed record IntegrationContractVersionCapabilitiesDto(
    bool IsReadOnly,
    bool CanEditFunctionalMetadata);
