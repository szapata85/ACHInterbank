using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Integrations.Services;

[Scoped]
public sealed class IntegrationContractArtifactImportService : IIntegrationContractArtifactImportService
{
    private readonly AchDbContext _context;
    private readonly IIntegrationContractArtifactAnalyzer _analyzer;
    private readonly IIntegrationContractVersionService _versions;

    public IntegrationContractArtifactImportService(
        AchDbContext context,
        IIntegrationContractArtifactAnalyzer analyzer,
        IIntegrationContractVersionService versions)
    {
        _context = context;
        _analyzer = analyzer;
        _versions = versions;
    }

    public async Task<IntegrationContractArtifactPreviewDto> PreviewDraftAsync(
        int methodId,
        Guid baseContractVersionId,
        AnalyzeIntegrationContractArtifactRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await new IntegrationCatalogBootstrapper(_context).EnsureAsync(ct);

        var methodExists = await _context.IntegrationMethods
            .AsNoTracking()
            .AnyAsync(method => method.Id == methodId && method.IsActive, ct);
        if (!methodExists)
        {
            throw new KeyNotFoundException($"No existe método de integración activo {methodId}.");
        }

        var analysis = await _analyzer.AnalyzeAsync(request, ct);
        if (!analysis.Importable)
        {
            return new IntegrationContractArtifactPreviewDto(analysis, null);
        }

        var diff = await _versions.CompareArtifactAsync(baseContractVersionId, methodId, analysis, ct);
        return new IntegrationContractArtifactPreviewDto(analysis, diff);
    }

    public async Task<IntegrationContractArtifactImportResultDto> ImportDraftAsync(
        int methodId,
        ImportIntegrationContractArtifactRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await new IntegrationCatalogBootstrapper(_context).EnsureAsync(ct);

        var versionLabel = RequiredText(request.VersionLabel, nameof(request.VersionLabel));
        var actor = RequiredText(request.Actor, nameof(request.Actor));
        if (versionLabel.Length > 100)
        {
            throw new ArgumentException("VersionLabel excede 100 caracteres.", nameof(request.VersionLabel));
        }
        if (actor.Length > 200)
        {
            throw new ArgumentException("Actor excede 200 caracteres.", nameof(request.Actor));
        }
        if (request.Notes is { Length: > 2000 })
        {
            throw new ArgumentException("Notes excede 2000 caracteres.", nameof(request.Notes));
        }

        var methodExists = await _context.IntegrationMethods
            .AsNoTracking()
            .AnyAsync(method => method.Id == methodId && method.IsActive, ct);
        if (!methodExists)
        {
            throw new KeyNotFoundException($"No existe método de integración activo {methodId}.");
        }

        if (await _context.IntegrationContractVersions
            .AsNoTracking()
            .AnyAsync(contract => contract.MethodId == methodId && contract.VersionLabel == versionLabel, ct))
        {
            throw new InvalidOperationException($"Ya existe la versión contractual '{versionLabel}' para MethodId={methodId}.");
        }

        var analysis = await _analyzer.AnalyzeAsync(
            new AnalyzeIntegrationContractArtifactRequest(
                request.FileName,
                request.Content,
                request.OperationName,
                request.StandaloneXsdSoapAction),
            ct);

        if (!analysis.Importable
            || string.IsNullOrWhiteSpace(analysis.OperationName)
            || string.IsNullOrWhiteSpace(analysis.TargetNamespace)
            || string.IsNullOrWhiteSpace(analysis.SoapAction))
        {
            throw new IntegrationContractArtifactRejectedException(analysis);
        }

        var contract = new IntegrationContractVersion
        {
            MethodId = methodId,
            VersionLabel = versionLabel,
            Status = IntegrationContractVersionStatusEnum.Draft,
            OperationName = analysis.OperationName,
            TargetNamespace = analysis.TargetNamespace,
            SoapAction = analysis.SoapAction,
            SourceType = IntegrationContractSourceTypeEnum.WsdlImport,
            SourceDocumentName = analysis.FileName,
            SourceDocumentHash = analysis.Sha256,
            SourceReference = "local-upload",
            Notes = OptionalText(request.Notes),
            Revision = 1,
            ImportedAtUtc = DateTime.UtcNow,
            ImportedBy = actor
        };

        _context.IntegrationContractVersions.Add(contract);
        foreach (var parameter in analysis.Parameters.OrderBy(x => x.SortOrder))
        {
            _context.IntegrationMethodParameters.Add(new IntegrationMethodParameter
            {
                MethodId = methodId,
                ContractVersionId = contract.Id,
                ParameterPath = parameter.ParameterPath,
                XmlElementName = parameter.XmlElementName,
                DisplayName = parameter.DisplayName,
                DescriptionEs = string.Empty,
                UiHelpText = string.Empty,
                Category = "Imported",
                ExampleValue = string.Empty,
                DataType = parameter.DataType,
                XsdType = parameter.XsdType,
                Direction = parameter.Direction,
                Cardinality = parameter.Cardinality,
                Required = parameter.Required,
                Nillable = parameter.Nillable,
                MinOccurs = parameter.MinOccurs,
                MaxOccurs = parameter.MaxOccurs,
                SupportStatus = parameter.SupportStatus,
                SortOrder = parameter.SortOrder,
                IsActive = true
            });
        }

        await _context.SaveChangesAsync(ct);
        var detail = await _versions.GetDetailAsync(contract.Id, ct)
            ?? throw new InvalidOperationException("No se pudo consultar la versión contractual importada.");

        if (detail.Contract.Status != IntegrationContractVersionStatusEnum.Draft)
        {
            throw new InvalidOperationException("La importación contractual local debe permanecer en estado Draft.");
        }

        return new IntegrationContractArtifactImportResultDto(analysis, detail);
    }

    private static string RequiredText(string? value, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{argumentName} es obligatorio.", argumentName);
        }

        return value.Trim();
    }

    private static string? OptionalText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
