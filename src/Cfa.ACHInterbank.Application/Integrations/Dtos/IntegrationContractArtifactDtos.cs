using System.Text.Json.Serialization;
using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.Application.Integrations.Dtos;

public static class IntegrationContractArtifactLimits
{
    public const int MaxArtifactBytes = 2 * 1024 * 1024;
    public const long MaxMultipartRequestBytes = MaxArtifactBytes + (128 * 1024);
    public const long MaxXmlCharacters = 4 * 1024 * 1024;
    public const int MaxXmlDepth = 64;
    public const int MaxXmlElements = 10_000;
    public const int MaxParameters = 512;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IntegrationContractArtifactKindEnum
{
    Unknown = 0,
    Wsdl11 = 1,
    XmlSchema = 2
}

public sealed record AnalyzeIntegrationContractArtifactRequest(
    string FileName,
    byte[] Content,
    string? OperationName = null,
    string? StandaloneXsdSoapAction = null);

public sealed record ImportIntegrationContractArtifactRequest(
    string VersionLabel,
    string FileName,
    byte[] Content,
    string? OperationName,
    string? StandaloneXsdSoapAction,
    string? Notes,
    string Actor);

public sealed record IntegrationContractArtifactIssueDto(
    string Code,
    string Message);

public sealed record IntegrationContractArtifactParameterDto(
    string ParameterPath,
    string XmlElementName,
    string DisplayName,
    string DataType,
    string XsdType,
    IntegrationParameterDirectionEnum Direction,
    IntegrationParameterCardinalityEnum Cardinality,
    bool Required,
    bool Nillable,
    int MinOccurs,
    int? MaxOccurs,
    string SupportStatus,
    int SortOrder);

public sealed record IntegrationContractArtifactAnalysisDto(
    string FileName,
    IntegrationContractArtifactKindEnum ArtifactKind,
    long SizeBytes,
    string Sha256,
    string? OperationName,
    string? TargetNamespace,
    string? SoapAction,
    bool Importable,
    IReadOnlyCollection<IntegrationContractArtifactIssueDto> Issues,
    IReadOnlyCollection<IntegrationContractArtifactParameterDto> Parameters)
{
    public IReadOnlyCollection<string> AvailableOperations { get; init; } = [];
}

public sealed record IntegrationContractArtifactPreviewDto(
    IntegrationContractArtifactAnalysisDto Analysis,
    IntegrationContractDiffDto? Diff);

public sealed record IntegrationContractArtifactImportResultDto(
    IntegrationContractArtifactAnalysisDto Analysis,
    IntegrationContractVersionDetailDto Contract);

public sealed class IntegrationContractArtifactRejectedException : InvalidOperationException
{
    public IntegrationContractArtifactRejectedException(IntegrationContractArtifactAnalysisDto analysis)
        : base($"El artefacto contractual '{analysis.FileName}' fue rechazado: {string.Join("; ", analysis.Issues.Select(x => $"{x.Code}: {x.Message}"))}")
    {
        Analysis = analysis;
    }

    public IntegrationContractArtifactAnalysisDto Analysis { get; }
}
