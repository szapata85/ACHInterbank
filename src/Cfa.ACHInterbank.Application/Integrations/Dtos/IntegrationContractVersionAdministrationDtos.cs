using System.Text.Json.Serialization;
using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.Application.Integrations.Dtos;

/// <summary>Solicitud para crear manualmente una nueva versión contractual en estado Draft.</summary>
public sealed record CreateIntegrationContractVersionRequest(
    string VersionLabel,
    string OperationName,
    string TargetNamespace,
    string SoapAction,
    string? SourceReference,
    string? Notes,
    string Actor);

/// <summary>Solicitud para versionar un contrato existente mediante clonación no destructiva.</summary>
public sealed record CloneIntegrationContractVersionRequest(
    Guid SourceContractVersionId,
    string VersionLabel,
    string? Notes,
    string Actor);

/// <summary>Solicitud para actualizar únicamente metadata funcional de presentación de un parámetro versionado.</summary>
public sealed record UpdateIntegrationContractParameterPresentationRequest(
    string DisplayName,
    string? DescriptionEs,
    string? UiHelpText,
    string? Category,
    string? ExampleValue,
    long ExpectedRevision,
    string Actor);

/// <summary>
/// Solicitud de activación atómica de una ContractVersion junto con el MappingSet Draft
/// asociado exactamente a esa versión contractual.
/// </summary>
public sealed record ActivateIntegrationContractVersionRequest(
    Guid MappingSetId,
    long ExpectedContractRevision,
    long ExpectedMappingRevision,
    string ActivatedBy);

/// <summary>
/// Solicitud de rollback no destructivo hacia una ContractVersion Retired y su MappingSet
/// histórico Archived. Las revisiones de ambos lados evitan transiciones sobre lecturas obsoletas.
/// </summary>
public sealed record RollbackIntegrationContractVersionRequest(
    Guid TargetMappingSetId,
    Guid CurrentContractVersionId,
    Guid CurrentMappingSetId,
    long ExpectedTargetContractRevision,
    long ExpectedTargetMappingRevision,
    long ExpectedCurrentContractRevision,
    long ExpectedCurrentMappingRevision,
    string RolledBackBy);

/// <summary>Resultado de una activación o rollback contractual atómico.</summary>
public sealed record IntegrationContractMappingTransitionResultDto(
    string Transition,
    IntegrationContractVersionDto Contract,
    IntegrationMappingSetDto MappingSet,
    Guid? PreviousContractVersionId,
    Guid? PreviousMappingSetId,
    IntegrationMappingValidationResultDto Validation,
    DateTime TransitionedAtUtc,
    string TransitionedBy);

/// <summary>Detalle contractual completo de un parámetro, incluyendo metadata técnica y funcional.</summary>
public sealed record IntegrationContractParameterDto(
    long Id,
    Guid? ContractVersionId,
    int MethodId,
    string ParameterPath,
    string XmlElementName,
    string DisplayName,
    string DescriptionEs,
    string UiHelpText,
    string Category,
    string ExampleValue,
    string DataType,
    string XsdType,
    IntegrationParameterDirectionEnum Direction,
    IntegrationParameterCardinalityEnum Cardinality,
    bool Required,
    bool Nillable,
    int MinOccurs,
    int? MaxOccurs,
    string SupportStatus,
    int SortOrder,
    bool IsActive);

/// <summary>Detalle de una versión contractual y sus parámetros en el snapshot persistido.</summary>
public sealed record IntegrationContractVersionDetailDto(
    IntegrationContractVersionDto Contract,
    IReadOnlyCollection<IntegrationContractParameterDto> Parameters);

/// <summary>Resultado de una actualización de metadata funcional con la revisión contractual resultante.</summary>
public sealed record IntegrationContractParameterPresentationResultDto(
    Guid ContractVersionId,
    long ContractRevision,
    IntegrationContractParameterDto Parameter);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IntegrationContractChangeTypeEnum
{
    Added = 1,
    Removed = 2,
    RequiredChanged = 3,
    TypeChanged = 4,
    CardinalityChanged = 5,
    OrderChanged = 6,
    NamespaceChanged = 7,
    DirectionChanged = 8
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IntegrationContractDiffImpactEnum
{
    MappingRequired = 1,
    RequiresDevelopment = 2
}

/// <summary>Cambio individual detectado al comparar dos versiones contractuales.</summary>
public sealed record IntegrationContractDiffChangeDto(
    IntegrationContractChangeTypeEnum ChangeType,
    string TechnicalName,
    string FunctionalName,
    IntegrationContractDiffImpactEnum Impact,
    string? LeftValue = null,
    string? RightValue = null);

/// <summary>Resumen agregado del diff contractual.</summary>
public sealed record IntegrationContractDiffSummaryDto(
    int Added,
    int Removed,
    int Modified,
    int RequiresMapping,
    int RequiresDevelopment);

/// <summary>Diff determinista entre dos versiones del mismo método.</summary>
public sealed record IntegrationContractDiffDto(
    Guid LeftContractVersionId,
    Guid RightContractVersionId,
    int MethodId,
    IntegrationContractDiffSummaryDto Summary,
    IReadOnlyCollection<IntegrationContractDiffChangeDto> Changes);
