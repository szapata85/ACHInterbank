using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.Application.Integrations.Dtos;

public sealed record IntegrationMethodDto(
    int Id,
    string Code,
    string DisplayName,
    string SoapClientCode,
    bool IsActive);

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

public sealed record IntegrationTransformationCatalogDto(
    string Code,
    string DisplayName,
    string Description,
    bool SupportsFormatMask,
    bool SupportsMultipleSources = false);
