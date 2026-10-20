using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.Application.Integrations.Dtos;

/// <summary>Solicitud para crear un mapping set de integración en estado borrador.</summary>
/// <param name="MethodId">Identificador del método de integración activo al que pertenecerá el mapping set.</param>
/// <param name="Name">Nombre del mapping set; el servicio elimina espacios al inicio y al final.</param>
/// <param name="Notes">Notas del mapping set; el servicio elimina espacios al inicio y al final y usa cadena vacía cuando no hay contenido.</param>
/// <param name="CreatedBy">Actor que el servicio registra en la historia de creación.</param>
public sealed record CreateIntegrationMappingSetRequest(
    int MethodId,
    string Name,
    string Notes,
    string CreatedBy);

/// <summary>Solicitud para actualizar la metadata de un mapping set borrador.</summary>
/// <param name="Name">Nuevo nombre del mapping set; el servicio elimina espacios al inicio y al final.</param>
/// <param name="Notes">Nuevas notas; el servicio elimina espacios al inicio y al final y usa cadena vacía cuando no hay contenido.</param>
/// <param name="IsActive">Indicador de actividad que se persistirá en el borrador.</param>
/// <param name="UpdatedBy">Actor que el servicio registra en la historia de actualización.</param>
public sealed record UpdateIntegrationMappingSetRequest(
    string Name,
    string Notes,
    bool IsActive,
    string UpdatedBy);

/// <summary>Regla individual solicitada para crear o actualizar el mapping de un parámetro.</summary>
/// <param name="Id">Identificador de una regla existente que se actualiza; null indica creación de una regla nueva.</param>
/// <param name="MethodId">Identificador de método recibido por contrato; al persistir, el servicio usa el método del mapping set.</param>
/// <param name="ParameterId">Identificador de un parámetro activo que debe pertenecer al método del mapping set.</param>
/// <param name="SourceKind">Tipo de origen de la regla. La implementación de upsert rechaza <c>Expression</c>.</param>
/// <param name="SourceCatalogFieldId">Identificador opcional de campo del catálogo fuente.</param>
/// <param name="SourceFieldPath">Ruta opcional del campo fuente; el servicio la normaliza a cadena vacía cuando es null.</param>
/// <param name="FixedValue">Valor fijo opcional. El servicio conserva espacios finales cuando el valor no está vacío.</param>
/// <param name="DefaultValue">Valor por defecto opcional. El servicio conserva espacios finales cuando el valor no está vacío.</param>
/// <param name="TransformationCode">Código opcional de transformación; el servicio elimina espacios exteriores cuando se informa.</param>
/// <param name="FormatMask">Máscara opcional asociada a la transformación; el servicio elimina espacios exteriores cuando se informa.</param>
/// <param name="Priority">Prioridad solicitada; el servicio persiste como mínimo el valor 1.</param>
/// <param name="RequiredOverride">Sobrescritura opcional del indicador de obligatoriedad.</param>
/// <param name="Enabled">Indica si la regla queda habilitada.</param>
/// <param name="ConditionExpression">Expresión condicional opcional en el contrato; la implementación de upsert no permite un valor no vacío.</param>
public sealed record UpsertIntegrationMappingRuleRequest(
    long? Id,
    int MethodId,
    long ParameterId,
    IntegrationSourceKindEnum SourceKind,
    long? SourceCatalogFieldId,
    string? SourceFieldPath,
    string? FixedValue,
    string? DefaultValue,
    string? TransformationCode,
    string? FormatMask,
    int Priority,
    bool? RequiredOverride,
    bool Enabled,
    string? ConditionExpression);

/// <summary>Solicitud para crear o actualizar reglas de un mapping set borrador.</summary>
/// <param name="UpdatedBy">Actor que el servicio registra en la historia del cambio de reglas.</param>
/// <param name="Rules">Colección de reglas que el servicio crea o actualiza.</param>
public sealed record UpsertIntegrationMappingRulesRequest(
    string UpdatedBy,
    IReadOnlyCollection<UpsertIntegrationMappingRuleRequest> Rules);

/// <summary>Solicitud guiada para actualizar el origen y tratamiento de un parámetro.</summary>
/// <param name="ExpectedRevision">Revisión del MappingSet observada por la SPA antes de editar.</param>
/// <param name="SourceCatalogFieldId">Campo fuente seleccionado exclusivamente desde el catálogo backend.</param>
/// <param name="TransformationCode">Código de tratamiento compatible informado por el backend; null representa sin tratamiento.</param>
/// <param name="FormatMask">Configuración opcional del tratamiento cuando la metadata backend la admite.</param>
/// <param name="UpdatedBy">Actor que el servicio registra en la historia del cambio.</param>
public sealed record UpdateIntegrationParameterMappingRequest(
    long ExpectedRevision,
    long SourceCatalogFieldId,
    string? TransformationCode,
    string? FormatMask,
    string UpdatedBy);

/// <summary>Solicitud para clonar un mapping set existente.</summary>
/// <param name="NewName">Nombre del clon; si está vacío, el servicio deriva el nombre agregando <c>(clon)</c> al nombre de origen.</param>
/// <param name="ClonedBy">Actor que el servicio registra en la historia de clonación.</param>
public sealed record CloneIntegrationMappingSetRequest(
    string NewName,
    string ClonedBy);

/// <summary>Clasificación de un parámetro durante la clonación de mapping entre versiones contractuales.</summary>
public enum IntegrationMappingContractCloneClassificationEnum
{
    Copied = 1,
    New = 2,
    Removed = 3,
    Incompatible = 4
}

/// <summary>Solicitud para clonar un mapping set hacia otra ContractVersion sin mutar la fuente.</summary>
/// <param name="TargetContractVersionId">Versión contractual destino; debe pertenecer al mismo método y no puede estar Active/Retired.</param>
/// <param name="NewName">Nombre del mapping set Draft destino.</param>
/// <param name="ClonedBy">Actor registrado en la evidencia histórica.</param>
/// <param name="ExpectedSourceMappingRevision">Revisión esperada del mapping set fuente para detectar lecturas obsoletas.</param>
/// <param name="ExpectedTargetContractRevision">Revisión esperada de la ContractVersion destino para detectar cambios concurrentes.</param>
public sealed record CloneIntegrationMappingToContractVersionRequest(
    Guid TargetContractVersionId,
    string NewName,
    string ClonedBy,
    long ExpectedSourceMappingRevision,
    long ExpectedTargetContractRevision);

/// <summary>Clasificación de un parámetro producida por la clonación de mapping entre contratos.</summary>
public sealed record IntegrationMappingContractCloneItemDto(
    string? SourceParameterPath,
    string? TargetParameterPath,
    long? SourceParameterId,
    long? TargetParameterId,
    IntegrationMappingContractCloneClassificationEnum Classification,
    int CopiedRuleCount,
    string Reason);

/// <summary>Solicitud de validación conjunta de ContractVersion + MappingSet con control optimista de revisión.</summary>
public sealed record ValidateIntegrationContractMappingRequest(
    long ExpectedMappingRevision,
    long ExpectedContractRevision,
    bool IncludeWarnings = true);

/// <summary>Resultado de validación conjunta del contrato y su mapping set.</summary>
public sealed record IntegrationContractMappingValidationResultDto(
    Guid ContractVersionId,
    long ContractRevision,
    Guid MappingSetId,
    long MappingRevision,
    bool IsComplete,
    IntegrationMappingValidationResultDto MappingValidation);

/// <summary>Resultado de clonar un mapping set desde su ContractVersion hacia una versión contractual destino.</summary>
public sealed record IntegrationMappingContractCloneResultDto(
    Guid SourceMappingSetId,
    Guid SourceContractVersionId,
    Guid TargetContractVersionId,
    IntegrationMappingSetDto TargetMappingSet,
    IReadOnlyCollection<IntegrationMappingContractCloneItemDto> Classifications,
    IntegrationContractMappingValidationResultDto Validation);

/// <summary>Solicitud para publicar un mapping set borrador con control optimista de concurrencia.</summary>
/// <param name="ExpectedRevision">Revisión observada por la UI antes de solicitar la publicación.</param>
/// <param name="PublishedBy">Actor que se persiste como publicador y en la historia de publicación.</param>
/// <param name="PublishNote">Nota opcional que el servicio agrega a las notas del mapping set cuando contiene texto.</param>
public sealed record PublishIntegrationMappingSetRequest(
    long ExpectedRevision,
    string PublishedBy,
    string? PublishNote = null);

/// <summary>Opciones de validación de un mapping set.</summary>
/// <param name="IncludeWarnings">Cuando es true, la inconsistencia de múltiples reglas habilitadas sin prioridad 1 se reporta como Warning; cuando es false se reporta como Error.</param>
public sealed record ValidateIntegrationMappingSetRequest(
    bool IncludeWarnings = true);

/// <summary>Opciones para generar una previsualización de resolución de un mapping set.</summary>
/// <param name="SampleTransactionId">Identificador opcional de una transacción que se usa como muestra cuando no se solicita la muestra controlada.</param>
/// <param name="SampleCycleId">Identificador opcional de ciclo usado para filtrar la transacción más reciente cuando no se indica SampleTransactionId.</param>
/// <param name="MaxItems">Cantidad solicitada para limitar los elementos serializados en RawPreviewJson; la implementación usa como mínimo 1.</param>
/// <param name="UseControlledSample">Indica que la previsualización debe usar la muestra controlada construida por el servicio.</param>
public sealed record PreviewIntegrationMappingSetRequest(
    int? SampleTransactionId = null,
    string? SampleCycleId = null,
    int MaxItems = 3,
    bool UseControlledSample = false);

/// <summary>Regla persistida expuesta como parte de un mapping set.</summary>
/// <param name="Id">Identificador persistido de la regla.</param>
/// <param name="MappingSetId">Identificador del mapping set propietario.</param>
/// <param name="MethodId">Identificador del método de integración asociado.</param>
/// <param name="ParameterId">Identificador del parámetro al que aplica la regla.</param>
/// <param name="SourceKind">Tipo de origen persistido para resolver el valor.</param>
/// <param name="SourceCatalogFieldId">Campo del catálogo fuente asociado cuando existe.</param>
/// <param name="SourceFieldPath">Ruta fuente persistida.</param>
/// <param name="FixedValue">Valor fijo persistido cuando existe.</param>
/// <param name="DefaultValue">Valor por defecto persistido cuando existe.</param>
/// <param name="TransformationCode">Código de transformación persistido cuando existe.</param>
/// <param name="FormatMask">Máscara de formato persistida cuando existe.</param>
/// <param name="Priority">Prioridad persistida de la regla.</param>
/// <param name="RequiredOverride">Sobrescritura de obligatoriedad cuando existe.</param>
/// <param name="Enabled">Indica si la regla está habilitada.</param>
/// <param name="ConditionExpression">Expresión condicional persistida cuando existe.</param>
public sealed record IntegrationMappingRuleDto(
    long Id,
    Guid MappingSetId,
    int MethodId,
    long ParameterId,
    IntegrationSourceKindEnum SourceKind,
    long? SourceCatalogFieldId,
    string SourceFieldPath,
    string? FixedValue,
    string? DefaultValue,
    string? TransformationCode,
    string? FormatMask,
    int Priority,
    bool? RequiredOverride,
    bool Enabled,
    string? ConditionExpression);

/// <summary>Representación contractual de un mapping set de integración.</summary>
/// <param name="Id">Identificador único del mapping set.</param>
/// <param name="MethodId">Identificador del método de integración asociado.</param>
/// <param name="MethodCode">Código del método de integración asociado.</param>
/// <param name="Name">Nombre persistido del mapping set.</param>
/// <param name="Version">Versión persistida; los borradores se crean en 0 y la publicación asigna la siguiente versión del método.</param>
/// <param name="Status">Estado persistido del mapping set: Draft, Published o Archived.</param>
/// <param name="IsActive">Indicador de actividad persistido.</param>
/// <param name="Notes">Notas persistidas del mapping set.</param>
/// <param name="PublishedAtUtc">Fecha y hora UTC de publicación cuando existe.</param>
/// <param name="PublishedBy">Actor persistido como publicador.</param>
/// <param name="Rules">Reglas del mapping set ordenadas por ParameterId y Priority por el mapeo del servicio.</param>
public sealed record IntegrationMappingSetDto(
    Guid Id,
    int MethodId,
    string MethodCode,
    Guid? ContractVersionId,
    long Revision,
    string Name,
    int Version,
    IntegrationMappingSetStatusEnum Status,
    bool IsActive,
    string Notes,
    DateTime? PublishedAtUtc,
    string PublishedBy,
    IReadOnlyCollection<IntegrationMappingRuleDto> Rules);

/// <summary>Issue producido por la validación de un mapping set.</summary>
/// <param name="Severity">Severidad calculada por el validador, por ejemplo Error o Warning.</param>
/// <param name="Code">Código funcional del issue producido por el validador.</param>
/// <param name="Message">Mensaje descriptivo generado para el issue.</param>
/// <param name="Path">Ruta de parámetro a la que se asocia el issue.</param>
/// <param name="Category">Categoría del issue generada por el validador.</param>
public sealed record IntegrationMappingValidationIssueDto(
    string Severity,
    string Code,
    string Message,
    string Path,
    string Category);

/// <summary>Resultado de validación para un parámetro del método de integración.</summary>
/// <param name="ParameterId">Identificador del parámetro evaluado.</param>
/// <param name="ParameterPath">Ruta contractual del parámetro.</param>
/// <param name="Required">Indica si el parámetro activo está configurado como obligatorio.</param>
/// <param name="Status">Estado calculado para la cobertura del parámetro.</param>
/// <param name="ResolutionKind">Clase de resolución calculada a partir de sus reglas habilitadas.</param>
/// <param name="Hints">Sugerencias calculadas por el validador para el parámetro.</param>
public sealed record IntegrationMappingParameterValidationDto(
    long ParameterId,
    string ParameterPath,
    bool Required,
    string Status,
    string ResolutionKind,
    IReadOnlyCollection<string> Hints);

/// <summary>Resumen de cobertura calculado durante la validación de un mapping set.</summary>
/// <param name="TotalParameters">Cantidad total de parámetros activos evaluados.</param>
/// <param name="ValidParameters">Cantidad de parámetros cuyo estado calculado es valid.</param>
/// <param name="IncompleteParameters">Cantidad de parámetros cuyo estado calculado es incomplete.</param>
/// <param name="InvalidParameters">Cantidad de parámetros cuyo estado calculado es invalid.</param>
/// <param name="InactiveParameters">Cantidad de parámetros cuyo estado calculado es inactive.</param>
/// <param name="CoveredByDefaultOrFixed">Cantidad de parámetros cuya resolución calculada usa default o valor fijo.</param>
/// <param name="CoveredBySourceField">Cantidad de parámetros cuya resolución calculada usa campo fuente.</param>
public sealed record IntegrationMappingCoverageSummaryDto(
    int TotalParameters,
    int ValidParameters,
    int IncompleteParameters,
    int InvalidParameters,
    int InactiveParameters,
    int CoveredByDefaultOrFixed,
    int CoveredBySourceField);

/// <summary>Resultado agregado de validar un mapping set.</summary>
/// <param name="MappingSetId">Identificador del mapping set validado.</param>
/// <param name="IsValid">Indica si el validador no encontró issues con severidad Error.</param>
/// <param name="Issues">Issues generados por las reglas de validación.</param>
/// <param name="Coverage">Resumen de cobertura calculado.</param>
/// <param name="Parameters">Resultados calculados para los parámetros activos del método.</param>
public sealed record IntegrationMappingValidationResultDto(
    Guid MappingSetId,
    bool IsValid,
    IReadOnlyCollection<IntegrationMappingValidationIssueDto> Issues,
    IntegrationMappingCoverageSummaryDto Coverage,
    IReadOnlyCollection<IntegrationMappingParameterValidationDto> Parameters);

/// <summary>Identidad y revisión del mapping set expuesto por el workspace de relación de información.</summary>
public sealed record IntegrationMappingWorkspaceMappingSetDto(
    Guid Id,
    string Name,
    IntegrationMappingSetStatusEnum Status,
    long Revision);

/// <summary>Versión contractual asociada al workspace de relación de información.</summary>
public sealed record IntegrationMappingWorkspaceContractDto(
    Guid Id,
    string VersionLabel,
    string TargetNamespace);

/// <summary>Conteos de cobertura calculados por backend para la vista funcional.</summary>
public sealed record IntegrationMappingWorkspaceCoverageDto(
    int Total,
    int Configured,
    int PendingRequired,
    int Complete,
    int Pending,
    int Invalid,
    int NotApplicable);

/// <summary>Decisión autoritativa de publicación calculada por backend para el workspace.</summary>
public sealed record IntegrationMappingWorkspacePublicationDto(
    bool CanPublish,
    int PendingRequired,
    int NewRequiredPending,
    int BlockingIssues);

/// <summary>Detalle técnico secundario de un parámetro contractual.</summary>
public sealed record IntegrationMappingWorkspaceTechnicalDetailsDto(
    string ParameterPath,
    string XmlElementName,
    string DataType,
    string XsdType,
    string? TargetNamespace,
    int SortOrder);

/// <summary>Regla actual que la edición guiada puede volver a configurar.</summary>
public sealed record IntegrationMappingWorkspaceCurrentRuleDto(
    long? RuleId,
    long? SourceCatalogFieldId,
    IntegrationSourceKindEnum? SourceKind,
    string? SourceFieldPath,
    string? TransformationCode,
    string? FormatMask,
    int Priority,
    bool Enabled);

/// <summary>Tratamiento compatible calculado por backend para un origen y parámetro concretos.</summary>
public sealed record IntegrationMappingWorkspaceTransformationOptionDto(
    string Code,
    string DisplayName,
    string Description,
    bool RequiresFormatMask);

/// <summary>Compatibilidad autoritativa entre parámetro, origen de catálogo y tratamientos disponibles.</summary>
public sealed record IntegrationMappingWorkspaceSourceCompatibilityDto(
    long ParameterId,
    long SourceCatalogFieldId,
    bool AllowsNoTransformation,
    IReadOnlyCollection<IntegrationMappingWorkspaceTransformationOptionDto> Transformations);

/// <summary>Parámetro funcional presentado en el workspace de relación de información.</summary>
public sealed record IntegrationMappingWorkspaceParameterDto(
    long ParameterId,
    string FunctionalName,
    string FunctionalDescription,
    string TechnicalName,
    bool Required,
    string DataType,
    string Category,
    string Status,
    string? Source,
    string? Transformation,
    IntegrationMappingWorkspaceCurrentRuleDto? CurrentRule,
    IntegrationMappingWorkspaceTechnicalDetailsDto TechnicalDetails);

/// <summary>Grupo funcional de orígenes provistos por el catálogo backend para la edición guiada.</summary>
public sealed record IntegrationMappingWorkspaceSourceGroupDto(
    string Key,
    string Label,
    IReadOnlyCollection<IntegrationSourceCatalogFieldDto> Fields);

/// <summary>Read model autoritativo del workspace de mapping para la SPA.</summary>
public sealed record IntegrationMappingWorkspaceDto(
    IntegrationMappingWorkspaceMappingSetDto MappingSet,
    IntegrationMappingWorkspaceContractDto? Contract,
    IntegrationMappingWorkspaceCoverageDto Coverage,
    IntegrationMappingWorkspacePublicationDto Publication,
    IReadOnlyCollection<IntegrationMappingWorkspaceParameterDto> Parameters,
    IReadOnlyCollection<IntegrationMappingWorkspaceSourceGroupDto> SourceGroups,
    IReadOnlyCollection<IntegrationTransformationCatalogDto> Transformations,
    IReadOnlyCollection<IntegrationMappingWorkspaceSourceCompatibilityDto> SourceCompatibility);

/// <summary>Vista previa sanitizada para un parámetro CFA y su estado de validación backend.</summary>
/// <param name="ParameterId">Identificador del parámetro evaluado.</param>
/// <param name="ParameterPath">Ruta contractual técnica del parámetro.</param>
/// <param name="ResolvedFrom">Referencia técnica segura del origen de resolución; nunca contiene expresiones arbitrarias.</param>
/// <param name="PreviewValue">Valor resultante sanitizado por backend; nunca representa un valor persistido por la vista previa.</param>
/// <param name="SourceSection">Sección funcional calculada a partir del tipo de origen.</param>
/// <param name="ResolutionKind">Clase de resolución calculada por backend.</param>
/// <param name="AppliedTransformation">Código técnico de transformación aplicado cuando existe.</param>
/// <param name="Priority">Prioridad de la regla seleccionada, o cero cuando no existe regla habilitada.</param>
/// <param name="Enabled">Indica si existe una regla habilitada seleccionada.</param>
/// <param name="FunctionalName">Nombre funcional CFA del parámetro.</param>
/// <param name="FunctionalDescription">Descripción funcional CFA del parámetro.</param>
/// <param name="Required">Indica si CFA requiere el dato.</param>
/// <param name="SourceDisplayName">Nombre funcional del origen publicado por metadata backend.</param>
/// <param name="TransformationDisplayName">Nombre funcional del tratamiento publicado por metadata backend.</param>
/// <param name="ValidationStatus">Estado de validación calculado por backend para el parámetro.</param>
/// <param name="ValidationIssues">Issues backend asociados al parámetro; el código y mensaje técnico se conservan para soporte.</param>
/// <param name="IsMasked">Indica que el valor fue enmascarado o redactado antes de salir del backend.</param>
public sealed record IntegrationMappingPreviewItemDto(
    long ParameterId,
    string ParameterPath,
    string ResolvedFrom,
    string? PreviewValue,
    string SourceSection,
    string ResolutionKind,
    string? AppliedTransformation,
    int Priority,
    bool Enabled,
    string FunctionalName,
    string FunctionalDescription,
    bool Required,
    string? SourceDisplayName,
    string? TransformationDisplayName,
    string ValidationStatus,
    IReadOnlyCollection<IntegrationMappingValidationIssueDto> ValidationIssues,
    bool IsMasked);

/// <summary>Resultado sanitizado de vista previa y validación general de un mapping set.</summary>
/// <param name="MappingSetId">Identificador del mapping set previsualizado.</param>
/// <param name="MethodId">Identificador del método de integración asociado.</param>
/// <param name="MethodCode">Código del método de integración asociado.</param>
/// <param name="ContextMode">Modo de muestra determinado por el servicio para la previsualización.</param>
/// <param name="Items">Vista previa sanitizada para todos los parámetros activos del contrato.</param>
/// <param name="PayloadPreviewJson">JSON sanitizado del payload de muestra, ordenado por ruta de parámetro.</param>
/// <param name="RawPreviewJson">JSON sanitizado de elementos de previsualización limitado usando MaxItems con un mínimo efectivo de 1.</param>
/// <param name="Validation">Validación general autoritativa calculada por backend sin publicar el mapping.</param>
public sealed record IntegrationMappingPreviewResultDto(
    Guid MappingSetId,
    int MethodId,
    string MethodCode,
    string ContextMode,
    IReadOnlyCollection<IntegrationMappingPreviewItemDto> Items,
    string PayloadPreviewJson,
    string RawPreviewJson,
    IntegrationMappingValidationResultDto Validation);

/// <summary>Evento histórico persistido para un mapping set.</summary>
/// <param name="Id">Identificador del evento histórico.</param>
/// <param name="MappingSetId">Identificador del mapping set relacionado.</param>
/// <param name="MethodId">Identificador del método asociado al momento del evento.</param>
/// <param name="Version">Versión registrada en el evento.</param>
/// <param name="Status">Estado del mapping set registrado en el evento.</param>
/// <param name="Action">Acción histórica persistida.</param>
/// <param name="PerformedBy">Actor registrado para la acción.</param>
/// <param name="PerformedAtUtc">Fecha y hora UTC de la acción.</param>
/// <param name="SnapshotHash">Hash del snapshot persistido por la historia.</param>
public sealed record IntegrationMappingSetHistoryDto(
    Guid Id,
    Guid MappingSetId,
    int MethodId,
    int Version,
    IntegrationMappingSetStatusEnum Status,
    string Action,
    string PerformedBy,
    DateTime PerformedAtUtc,
    string SnapshotHash);

/// <summary>Solicitud para comparar dos mapping sets.</summary>
/// <param name="LeftMappingSetId">Identificador del mapping set izquierdo.</param>
/// <param name="RightMappingSetId">Identificador del mapping set derecho.</param>
public sealed record CompareIntegrationMappingSetsRequest(
    Guid LeftMappingSetId,
    Guid RightMappingSetId);

/// <summary>Metadata de un mapping set incluida en un resultado de comparación.</summary>
/// <param name="MappingSetId">Identificador del mapping set.</param>
/// <param name="Name">Nombre persistido.</param>
/// <param name="Version">Versión persistida.</param>
/// <param name="Status">Estado persistido.</param>
/// <param name="PublishedAtUtc">Fecha y hora UTC de publicación cuando existe.</param>
/// <param name="PublishedBy">Actor persistido como publicador.</param>
/// <param name="Notes">Notas persistidas.</param>
public sealed record IntegrationMappingSetComparisonMetadataDto(
    Guid MappingSetId,
    string Name,
    int Version,
    IntegrationMappingSetStatusEnum Status,
    DateTime? PublishedAtUtc,
    string PublishedBy,
    string Notes);

/// <summary>Comparación de la regla seleccionada para un parámetro entre dos mapping sets.</summary>
/// <param name="LeftRuleId">Identificador de la regla izquierda cuando existe.</param>
/// <param name="RightRuleId">Identificador de la regla derecha cuando existe.</param>
/// <param name="ParameterId">Identificador del parámetro comparado.</param>
/// <param name="ParameterPath">Ruta del parámetro o etiqueta de fallback cuando el parámetro no está en el catálogo consultado.</param>
/// <param name="ParameterGroup">Grupo calculado desde la ruta del parámetro.</param>
/// <param name="ChangeType">Tipo de cambio calculado: Added, Removed, Modified o Equal.</param>
/// <param name="ChangedFields">Nombres de campos de regla que difieren entre ambos lados.</param>
/// <param name="PotentialImpact">Descripción calculada del impacto potencial de la diferencia.</param>
/// <param name="Left">Regla izquierda expuesta cuando existe.</param>
/// <param name="Right">Regla derecha expuesta cuando existe.</param>
public sealed record IntegrationMappingSetRuleComparisonDto(
    long? LeftRuleId,
    long? RightRuleId,
    long ParameterId,
    string ParameterPath,
    string ParameterGroup,
    string ChangeType,
    IReadOnlyCollection<string> ChangedFields,
    string PotentialImpact,
    IntegrationMappingRuleDto? Left,
    IntegrationMappingRuleDto? Right);

/// <summary>Resultado agregado de comparar dos mapping sets.</summary>
/// <param name="Left">Metadata del mapping set izquierdo.</param>
/// <param name="Right">Metadata del mapping set derecho.</param>
/// <param name="Rules">Comparaciones de reglas seleccionadas por parámetro.</param>
public sealed record IntegrationMappingSetComparisonResultDto(
    IntegrationMappingSetComparisonMetadataDto Left,
    IntegrationMappingSetComparisonMetadataDto Right,
    IReadOnlyCollection<IntegrationMappingSetRuleComparisonDto> Rules);
