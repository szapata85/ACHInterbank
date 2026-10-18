using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Application.Integrations.Exceptions;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Registries;
using Cfa.ACHInterbank.Application.Integrations.Transformations;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Integrations.Services;

[Scoped]
public class IntegrationMappingSetService : IIntegrationMappingSetService
{
    private readonly AchDbContext _context;
    private readonly IIntegrationMappingValidationService _validationService;
    private readonly IIntegrationMappingPreviewService _previewService;
    private readonly IntegrationMappingSnapshotBuilder _snapshotBuilder;
    private readonly IIntegrationTransformationRegistry _transformationRegistry;

    public IntegrationMappingSetService(
        AchDbContext context,
        IIntegrationMappingValidationService validationService,
        IIntegrationMappingPreviewService previewService,
        IntegrationMappingSnapshotBuilder? snapshotBuilder = null,
        IIntegrationTransformationRegistry? transformationRegistry = null)
    {
        _context = context;
        _validationService = validationService;
        _previewService = previewService;
        _snapshotBuilder = snapshotBuilder ?? new IntegrationMappingSnapshotBuilder(context);
        _transformationRegistry = transformationRegistry
            ?? new IntegrationTransformationRegistry(StandardIntegrationTransformationCatalog.Create());
    }

    public async Task<IReadOnlyCollection<IntegrationMappingSetDto>> GetByMethodAsync(int? methodId, CancellationToken ct = default)
    {
        await new IntegrationMappingBootstrapper(_context).EnsureAsync(ct);

        var query = _context.Set<IntegrationMappingSet>()
            .AsNoTracking()
            .Include(x => x.Method)
            .Include(x => x.Rules)
            .AsQueryable();

        if (methodId.HasValue)
        {
            query = query.Where(x => x.MethodId == methodId.Value);
        }

        var items = await query
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(ct);

        return items.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyCollection<IntegrationMappingSetDto>> GetByContractVersionAsync(
        Guid contractVersionId,
        CancellationToken ct = default)
    {
        await new IntegrationMappingBootstrapper(_context).EnsureAsync(ct);

        var items = await _context.Set<IntegrationMappingSet>()
            .AsNoTracking()
            .Include(x => x.Method)
            .Include(x => x.Rules)
            .Where(x => x.ContractVersionId == contractVersionId)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(ct);

        return items.Select(MapToDto).ToList();
    }

    public async Task<IntegrationMappingSetDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await new IntegrationMappingBootstrapper(_context).EnsureAsync(ct);

        var item = await _context.Set<IntegrationMappingSet>()
            .AsNoTracking()
            .Include(x => x.Method)
            .Include(x => x.Rules)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return item is null ? null : MapToDto(item);
    }

    public async Task<IntegrationMappingWorkspaceDto?> GetWorkspaceAsync(Guid id, CancellationToken ct = default)
    {
        await new IntegrationMappingBootstrapper(_context).EnsureAsync(ct);

        var mappingSet = await _context.Set<IntegrationMappingSet>()
            .AsNoTracking()
            .Include(x => x.Rules)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (mappingSet is null)
        {
            return null;
        }

        IntegrationContractVersion? contract = null;
        if (mappingSet.ContractVersionId.HasValue)
        {
            contract = await _context.IntegrationContractVersions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == mappingSet.ContractVersionId.Value, ct);
        }

        var validation = await _validationService.EvaluateAsync(id, includeWarnings: true, ct);
        var validationByParameter = validation.Parameters.ToDictionary(x => x.ParameterId);

        var parameterQuery = _context.Set<IntegrationMethodParameter>()
            .AsNoTracking()
            .Where(x => x.MethodId == mappingSet.MethodId && x.IsActive);
        if (contract is not null)
        {
            parameterQuery = IsBaselineBridge(contract)
                ? parameterQuery.Where(x => x.ContractVersionId == contract.Id || x.ContractVersionId == null)
                : parameterQuery.Where(x => x.ContractVersionId == contract.Id);
        }
        else
        {
            parameterQuery = parameterQuery.Where(x => x.ContractVersionId == null);
        }

        var parameters = await parameterQuery
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);

        var sourceCatalog = await _context.Set<IntegrationSourceCatalogField>()
            .AsNoTracking()
            .Where(x => x.IsActive && x.MethodId == mappingSet.MethodId)
            .OrderBy(x => x.SourceKind)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.FieldPath)
            .ToListAsync(ct);
        var sourceById = sourceCatalog.ToDictionary(x => x.Id);
        var transformationByCode = _transformationRegistry.Descriptors
            .ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

        var rows = parameters.Select(parameter =>
        {
            validationByParameter.TryGetValue(parameter.Id, out var parameterValidation);
            var currentRule = mappingSet.Rules
                .Where(rule => rule.ParameterId == parameter.Id)
                .OrderByDescending(rule => rule.Enabled)
                .ThenBy(rule => rule.Priority)
                .ThenBy(rule => rule.Id)
                .FirstOrDefault();

            string? sourceLabel = null;
            if (currentRule?.SourceCatalogFieldId is long sourceId && sourceById.TryGetValue(sourceId, out var sourceField))
            {
                sourceLabel = sourceField.DisplayName;
            }

            string? transformationLabel = null;
            if (!string.IsNullOrWhiteSpace(currentRule?.TransformationCode)
                && transformationByCode.TryGetValue(currentRule.TransformationCode!, out var descriptor))
            {
                transformationLabel = descriptor.DisplayNameEs;
            }

            return new IntegrationMappingWorkspaceParameterDto(
                parameter.Id,
                string.IsNullOrWhiteSpace(parameter.DisplayName) ? "Dato CFA sin nombre funcional" : parameter.DisplayName,
                parameter.DescriptionEs ?? string.Empty,
                parameter.ParameterPath,
                parameter.Required,
                parameter.DataType,
                parameter.Category ?? string.Empty,
                ToWorkspaceStatus(parameterValidation?.Status),
                sourceLabel,
                transformationLabel,
                currentRule is null
                    ? null
                    : new IntegrationMappingWorkspaceCurrentRuleDto(
                        currentRule.Id,
                        currentRule.SourceCatalogFieldId,
                        currentRule.SourceKind,
                        currentRule.SourceFieldPath,
                        currentRule.TransformationCode,
                        currentRule.FormatMask,
                        currentRule.Priority,
                        currentRule.Enabled),
                new IntegrationMappingWorkspaceTechnicalDetailsDto(
                    parameter.ParameterPath,
                    parameter.XmlElementName,
                    parameter.DataType,
                    parameter.XsdType,
                    contract?.TargetNamespace,
                    parameter.SortOrder));
        }).ToList();

        var guidedSources = sourceCatalog
            .Where(field => field.SourceKind is not IntegrationSourceKindEnum.Constant and not IntegrationSourceKindEnum.Expression)
            .ToList();
        var sourceGroups = guidedSources
            .GroupBy(field => field.SourceKind)
            .OrderBy(group => group.Min(field => field.SortOrder))
            .Select(group => new IntegrationMappingWorkspaceSourceGroupDto(
                group.Key.ToString(),
                SourceGroupLabel(group.Key),
                group.OrderBy(field => field.SortOrder).ThenBy(field => field.DisplayName).Select(MapSourceField).ToList()))
            .ToList();

        var sourceCompatibility = new List<IntegrationMappingWorkspaceSourceCompatibilityDto>();
        foreach (var parameter in parameters)
        {
            foreach (var source in guidedSources)
            {
                var compatibility = BuildSourceCompatibility(parameter, source);
                if (compatibility.AllowsNoTransformation || compatibility.Transformations.Count > 0)
                {
                    sourceCompatibility.Add(compatibility);
                }
            }
        }

        var transformations = _transformationRegistry.Descriptors
            .OrderBy(x => x.DisplayNameEs, StringComparer.OrdinalIgnoreCase)
            .Select(x => new IntegrationTransformationCatalogDto(
                x.Code,
                x.DisplayNameEs,
                x.DescriptionEs,
                x.RequiresMask || string.Equals(x.Code, "Concat", StringComparison.OrdinalIgnoreCase),
                string.Equals(x.Code, "Concat", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var pendingRequired = validation.Parameters.Count(x => x.Required && x.Status == "incomplete");
        var newCloneTargetPaths = await LoadNewCloneTargetPathsAsync(mappingSet.Id, ct);
        var newRequiredPending = rows.Count(row => row.Required
            && row.Status == "Pending"
            && newCloneTargetPaths.Contains(row.TechnicalDetails.ParameterPath));
        var blockingIssues = validation.Issues.Count(x => x.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase));
        var coverage = new IntegrationMappingWorkspaceCoverageDto(
            validation.Coverage.TotalParameters,
            validation.Coverage.ValidParameters + validation.Coverage.InvalidParameters,
            pendingRequired,
            validation.Coverage.ValidParameters,
            validation.Coverage.IncompleteParameters,
            validation.Coverage.InvalidParameters,
            validation.Coverage.InactiveParameters);
        var publication = new IntegrationMappingWorkspacePublicationDto(
            mappingSet.Status == IntegrationMappingSetStatusEnum.Draft
                && validation.IsValid
                && pendingRequired == 0,
            pendingRequired,
            newRequiredPending,
            blockingIssues);

        return new IntegrationMappingWorkspaceDto(
            new IntegrationMappingWorkspaceMappingSetDto(mappingSet.Id, mappingSet.Name, mappingSet.Status, mappingSet.Revision),
            contract is null
                ? null
                : new IntegrationMappingWorkspaceContractDto(contract.Id, contract.VersionLabel, contract.TargetNamespace),
            coverage,
            publication,
            rows,
            sourceGroups,
            transformations,
            sourceCompatibility);
    }

    public async Task<IntegrationMappingSetDto?> GetPublishedByMethodAsync(int methodId, CancellationToken ct = default)
    {
        await new IntegrationMappingBootstrapper(_context).EnsureAsync(ct);

        var activeContractId = await _context.IntegrationContractVersions
            .AsNoTracking()
            .Where(x => x.MethodId == methodId && x.Status == IntegrationContractVersionStatusEnum.Active)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(ct);

        var query = _context.Set<IntegrationMappingSet>()
            .AsNoTracking()
            .Include(x => x.Method)
            .Include(x => x.Rules)
            .Where(x => x.MethodId == methodId && x.Status == IntegrationMappingSetStatusEnum.Published);

        query = activeContractId.HasValue
            ? query.Where(x => x.ContractVersionId == activeContractId.Value || x.ContractVersionId == null)
            : query.Where(x => x.ContractVersionId == null);

        var item = await query
            .OrderByDescending(x => x.ContractVersionId == activeContractId)
            .ThenByDescending(x => x.Version)
            .FirstOrDefaultAsync(ct);

        return item is null ? null : MapToDto(item);
    }

    public async Task<IntegrationMappingSetDto?> GetPublishedByContractVersionAsync(
        Guid contractVersionId,
        CancellationToken ct = default)
    {
        await new IntegrationMappingBootstrapper(_context).EnsureAsync(ct);

        var item = await _context.Set<IntegrationMappingSet>()
            .AsNoTracking()
            .Include(x => x.Method)
            .Include(x => x.Rules)
            .Where(x => x.ContractVersionId == contractVersionId
                && x.Status == IntegrationMappingSetStatusEnum.Published)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(ct);

        return item is null ? null : MapToDto(item);
    }

    public async Task<IntegrationMappingSetDto> CreateDraftAsync(CreateIntegrationMappingSetRequest request, CancellationToken ct = default)
    {
        var method = await _context.Set<IntegrationMethod>()
            .FirstOrDefaultAsync(x => x.Id == request.MethodId && x.IsActive, ct)
            ?? throw new KeyNotFoundException($"No existe método {request.MethodId}.");

        var activeContractVersionId = await _context.IntegrationContractVersions
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id && x.Status == IntegrationContractVersionStatusEnum.Active)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(ct);

        var set = new IntegrationMappingSet
        {
            MethodId = method.Id,
            ContractVersionId = activeContractVersionId,
            Name = request.Name.Trim(),
            Notes = request.Notes?.Trim() ?? string.Empty,
            Status = IntegrationMappingSetStatusEnum.Draft,
            IsActive = true,
            Version = 0
        };

        _context.Set<IntegrationMappingSet>().Add(set);
        await _context.SaveChangesAsync(ct);

        await AppendHistoryAsync(set, "CreatedDraft", request.CreatedBy, ct);
        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(set.Id, ct) ?? throw new InvalidOperationException("No se pudo crear MappingSet.");
    }

    public async Task<IntegrationMappingSetDto> UpdateDraftAsync(Guid id, UpdateIntegrationMappingSetRequest request, CancellationToken ct = default)
    {
        var set = await GetDraftForUpdateAsync(id, ct);

        set.Name = request.Name.Trim();
        set.Notes = request.Notes?.Trim() ?? string.Empty;
        set.IsActive = request.IsActive;

        await _context.SaveChangesAsync(ct);
        await AppendHistoryAsync(set, "UpdatedDraft", request.UpdatedBy, ct);
        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(set.Id, ct) ?? throw new InvalidOperationException("No se pudo actualizar MappingSet.");
    }

    public async Task<IntegrationMappingSetDto> UpsertRulesAsync(Guid id, UpsertIntegrationMappingRulesRequest request, CancellationToken ct = default)
    {
        var set = await GetDraftForUpdateAsync(id, ct);
        var parameterIds = request.Rules.Select(x => x.ParameterId).Distinct().ToArray();

        var validParametersQuery = _context.Set<IntegrationMethodParameter>()
            .Where(x => x.MethodId == set.MethodId && parameterIds.Contains(x.Id) && x.IsActive);
        if (set.ContractVersionId.HasValue)
        {
            validParametersQuery = validParametersQuery.Where(x =>
                x.ContractVersionId == set.ContractVersionId.Value || x.ContractVersionId == null);
        }
        else
        {
            validParametersQuery = validParametersQuery.Where(x => x.ContractVersionId == null);
        }

        var validParameterIds = await validParametersQuery
            .Select(x => x.Id)
            .ToListAsync(ct);

        if (validParameterIds.Count != parameterIds.Length)
        {
            throw new InvalidOperationException("Uno o más ParameterId no son válidos para el método del MappingSet.");
        }

        foreach (var rule in request.Rules)
        {
            if (rule.SourceKind == IntegrationSourceKindEnum.Expression)
            {
                throw new InvalidOperationException("SourceKind=Expression no está permitido en el motor funcional gobernado.");
            }

            if (!string.IsNullOrWhiteSpace(rule.ConditionExpression))
            {
                throw new InvalidOperationException("ConditionExpression no está permitido en el motor funcional gobernado.");
            }

            IntegrationMappingRule entity;
            if (rule.Id.HasValue)
            {
                entity = await _context.Set<IntegrationMappingRule>()
                    .FirstOrDefaultAsync(x => x.Id == rule.Id.Value && x.MappingSetId == set.Id, ct)
                    ?? throw new KeyNotFoundException($"No existe regla {rule.Id.Value} en MappingSet {set.Id}.");
            }
            else
            {
                entity = new IntegrationMappingRule
                {
                    MappingSetId = set.Id,
                    MethodId = set.MethodId
                };
                _context.Set<IntegrationMappingRule>().Add(entity);
            }

            entity.MethodId = set.MethodId;
            entity.ParameterId = rule.ParameterId;
            entity.SourceKind = rule.SourceKind;
            entity.SourceCatalogFieldId = rule.SourceCatalogFieldId;
            entity.SourceFieldPath = rule.SourceFieldPath?.Trim() ?? string.Empty;
            // Los espacios finales pueden formar parte del contrato SOAP (por ejemplo, campos
            // alfanuméricos de longitud fija). No deben normalizarse al guardar desde el SPA.
            entity.FixedValue = string.IsNullOrEmpty(rule.FixedValue) ? null : rule.FixedValue;
            entity.DefaultValue = string.IsNullOrEmpty(rule.DefaultValue) ? null : rule.DefaultValue;
            entity.TransformationCode = string.IsNullOrWhiteSpace(rule.TransformationCode) ? null : rule.TransformationCode.Trim();
            entity.FormatMask = string.IsNullOrWhiteSpace(rule.FormatMask) ? null : rule.FormatMask.Trim();
            entity.Priority = Math.Max(1, rule.Priority);
            entity.RequiredOverride = rule.RequiredOverride;
            entity.Enabled = rule.Enabled;
            entity.ConditionExpression = string.IsNullOrWhiteSpace(rule.ConditionExpression) ? null : rule.ConditionExpression.Trim();
        }

        set.Revision = checked(set.Revision + 1);
        await _context.SaveChangesAsync(ct);
        await AppendHistoryAsync(set, "RulesUpserted", request.UpdatedBy, ct);
        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(set.Id, ct) ?? throw new InvalidOperationException("No se pudo actualizar reglas.");
    }

    public async Task<IntegrationMappingSetDto> UpdateParameterMappingAsync(
        Guid id,
        long parameterId,
        UpdateIntegrationParameterMappingRequest request,
        CancellationToken ct = default)
    {
        var set = await GetDraftForUpdateAsync(id, ct);
        if (set.Revision != request.ExpectedRevision)
        {
            throw new IntegrationMappingRevisionConflictException(request.ExpectedRevision, set.Revision);
        }

        var parameterQuery = _context.Set<IntegrationMethodParameter>()
            .Where(x => x.Id == parameterId && x.MethodId == set.MethodId && x.IsActive);
        parameterQuery = set.ContractVersionId.HasValue
            ? parameterQuery.Where(x => x.ContractVersionId == set.ContractVersionId.Value || x.ContractVersionId == null)
            : parameterQuery.Where(x => x.ContractVersionId == null);
        var parameter = await parameterQuery.SingleOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("El parámetro no pertenece al contrato editable del MappingSet.");

        var source = await _context.Set<IntegrationSourceCatalogField>()
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.Id == request.SourceCatalogFieldId
                && x.IsActive
                && x.MethodId == set.MethodId, ct)
            ?? throw new InvalidOperationException("El origen seleccionado no pertenece al catálogo backend disponible para este método.");

        if (source.SourceKind is IntegrationSourceKindEnum.Constant or IntegrationSourceKindEnum.Expression)
        {
            throw new InvalidOperationException("El origen seleccionado no está habilitado para la edición guiada.");
        }

        var compatibility = BuildSourceCompatibility(parameter, source);
        var transformationCode = string.IsNullOrWhiteSpace(request.TransformationCode)
            ? null
            : request.TransformationCode.Trim();
        IntegrationMappingWorkspaceTransformationOptionDto? selectedTransformation = null;
        if (transformationCode is null)
        {
            if (!compatibility.AllowsNoTransformation)
            {
                throw new InvalidOperationException("El origen seleccionado requiere un tratamiento compatible informado por backend.");
            }
        }
        else
        {
            selectedTransformation = compatibility.Transformations
                .FirstOrDefault(item => string.Equals(item.Code, transformationCode, StringComparison.OrdinalIgnoreCase));
            if (selectedTransformation is null)
            {
                throw new InvalidOperationException("El tratamiento seleccionado no es compatible con el origen y parámetro indicados.");
            }

            if (selectedTransformation.RequiresFormatMask && string.IsNullOrWhiteSpace(request.FormatMask))
            {
                throw new InvalidOperationException("El tratamiento seleccionado requiere configuración adicional.");
            }
        }

        var entity = await _context.Set<IntegrationMappingRule>()
            .Where(x => x.MappingSetId == set.Id && x.ParameterId == parameter.Id)
            .OrderByDescending(x => x.Enabled)
            .ThenBy(x => x.Priority)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (entity is null)
        {
            entity = new IntegrationMappingRule
            {
                MappingSetId = set.Id,
                MethodId = set.MethodId,
                ParameterId = parameter.Id,
                Priority = 1,
                Enabled = true
            };
            _context.Set<IntegrationMappingRule>().Add(entity);
        }

        entity.MethodId = set.MethodId;
        entity.ParameterId = parameter.Id;
        entity.SourceKind = source.SourceKind;
        entity.SourceCatalogFieldId = source.Id;
        entity.SourceFieldPath = source.FieldPath;
        entity.FixedValue = null;
        entity.DefaultValue = null;
        entity.TransformationCode = selectedTransformation?.Code;
        entity.FormatMask = selectedTransformation is null || string.IsNullOrWhiteSpace(request.FormatMask)
            ? null
            : request.FormatMask.Trim();
        entity.Priority = Math.Max(1, entity.Priority);
        entity.Enabled = true;
        entity.ConditionExpression = null;

        set.Revision = checked(set.Revision + 1);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            var currentRevision = await _context.Set<IntegrationMappingSet>()
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => x.Revision)
                .SingleOrDefaultAsync(ct);
            throw new IntegrationMappingRevisionConflictException(request.ExpectedRevision, currentRevision);
        }

        await AppendHistoryAsync(set, "ParameterMappingUpdated", request.UpdatedBy, ct);
        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(set.Id, ct)
            ?? throw new InvalidOperationException("No se pudo refrescar el MappingSet después de guardar el parámetro.");
    }

    public Task<IntegrationMappingValidationResultDto> ValidateAsync(Guid id, ValidateIntegrationMappingSetRequest request, CancellationToken ct = default)
        => _validationService.ValidateAsync(id, request.IncludeWarnings, ct);

    public Task<IntegrationMappingPreviewResultDto> PreviewAsync(Guid id, PreviewIntegrationMappingSetRequest request, CancellationToken ct = default)
        => _previewService.PreviewAsync(id, request, ct);

    public async Task<IntegrationMappingSetDto> PublishAsync(Guid id, PublishIntegrationMappingSetRequest request, CancellationToken ct = default)
    {
        var set = await GetDraftForUpdateAsync(id, ct);
        if (set.Revision != request.ExpectedRevision)
        {
            throw new IntegrationMappingRevisionConflictException(request.ExpectedRevision, set.Revision);
        }

        var validation = await _validationService.EvaluateAsync(id, includeWarnings: true, ct);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException("El MappingSet no es publicable. Revisar validaciones obligatorias.");
        }

        var currentPublishedQuery = _context.Set<IntegrationMappingSet>()
            .Where(x => x.MethodId == set.MethodId && x.Status == IntegrationMappingSetStatusEnum.Published);
        currentPublishedQuery = set.ContractVersionId.HasValue
            ? currentPublishedQuery.Where(x => x.ContractVersionId == set.ContractVersionId.Value)
            : currentPublishedQuery.Where(x => x.ContractVersionId == null);
        var currentPublished = await currentPublishedQuery.ToListAsync(ct);

        foreach (var published in currentPublished)
        {
            published.Status = IntegrationMappingSetStatusEnum.Archived;
            published.IsActive = false;
        }

        var versionQuery = _context.Set<IntegrationMappingSet>()
            .Where(x => x.MethodId == set.MethodId);
        versionQuery = set.ContractVersionId.HasValue
            ? versionQuery.Where(x => x.ContractVersionId == set.ContractVersionId.Value)
            : versionQuery.Where(x => x.ContractVersionId == null);
        var nextVersion = await versionQuery
            .Select(x => (int?)x.Version)
            .MaxAsync(ct) ?? 0;

        set.Version = nextVersion + 1;
        set.Status = IntegrationMappingSetStatusEnum.Published;
        set.IsActive = true;
        set.PublishedAtUtc = DateTime.UtcNow;
        set.PublishedBy = request.PublishedBy.Trim();
        set.Notes = string.IsNullOrWhiteSpace(request.PublishNote)
            ? set.Notes
            : $"{set.Notes}\n[PublishNote] {request.PublishNote.Trim()}";
        set.Revision = checked(set.Revision + 1);

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            var currentRevision = await _context.Set<IntegrationMappingSet>()
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => x.Revision)
                .SingleOrDefaultAsync(ct);
            throw new IntegrationMappingRevisionConflictException(request.ExpectedRevision, currentRevision);
        }
        foreach (var published in currentPublished)
        {
            await AppendHistoryAsync(published, "ArchivedByNewPublication", request.PublishedBy, ct);
        }

        await AppendHistoryAsync(set, "Published", request.PublishedBy, ct);
        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(set.Id, ct) ?? throw new InvalidOperationException("No se pudo publicar MappingSet.");
    }

    public async Task<IntegrationMappingSetDto> CloneAsync(Guid id, CloneIntegrationMappingSetRequest request, CancellationToken ct = default)
    {
        var source = await _context.Set<IntegrationMappingSet>()
            .Include(x => x.Rules)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException($"No existe MappingSet {id}.");

        var clone = new IntegrationMappingSet
        {
            MethodId = source.MethodId,
            ContractVersionId = source.ContractVersionId,
            Name = string.IsNullOrWhiteSpace(request.NewName) ? $"{source.Name} (clon)" : request.NewName.Trim(),
            Notes = source.Notes,
            Status = IntegrationMappingSetStatusEnum.Draft,
            Version = 0,
            IsActive = true
        };

        _context.Set<IntegrationMappingSet>().Add(clone);
        await _context.SaveChangesAsync(ct);

        var cloneRules = source.Rules.Select(r => new IntegrationMappingRule
        {
            MappingSetId = clone.Id,
            MethodId = source.MethodId,
            ParameterId = r.ParameterId,
            SourceKind = r.SourceKind,
            SourceCatalogFieldId = r.SourceCatalogFieldId,
            SourceFieldPath = r.SourceFieldPath,
            FixedValue = r.FixedValue,
            DefaultValue = r.DefaultValue,
            TransformationCode = r.TransformationCode,
            FormatMask = r.FormatMask,
            Priority = r.Priority,
            RequiredOverride = r.RequiredOverride,
            Enabled = r.Enabled,
            ConditionExpression = r.ConditionExpression
        });

        _context.Set<IntegrationMappingRule>().AddRange(cloneRules);
        await _context.SaveChangesAsync(ct);
        await AppendHistoryAsync(clone, "Cloned", request.ClonedBy, ct);
        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(clone.Id, ct) ?? throw new InvalidOperationException("No se pudo clonar MappingSet.");
    }

    public async Task<IntegrationMappingContractCloneResultDto> CloneToContractVersionAsync(
        Guid id,
        CloneIntegrationMappingToContractVersionRequest request,
        CancellationToken ct = default)
    {
        var actor = RequiredText(request.ClonedBy, nameof(request.ClonedBy));
        var targetName = RequiredText(request.NewName, nameof(request.NewName));
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        var sourceSet = await _context.IntegrationMappingSets
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException($"No existe MappingSet {id}.");

        if (sourceSet.Revision != request.ExpectedSourceMappingRevision)
        {
            throw new DbUpdateConcurrencyException(
                $"MappingSet {id} cambió de revisión. Esperada={request.ExpectedSourceMappingRevision}, actual={sourceSet.Revision}.");
        }

        if (!sourceSet.ContractVersionId.HasValue)
        {
            throw new InvalidOperationException("La clonación entre contratos exige un MappingSet fuente asociado explícitamente a una ContractVersion.");
        }

        var sourceContract = await _context.IntegrationContractVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == sourceSet.ContractVersionId.Value, ct)
            ?? throw new InvalidOperationException($"MappingSet {id} referencia una ContractVersion fuente inexistente.");

        var targetContract = await _context.IntegrationContractVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.TargetContractVersionId, ct)
            ?? throw new KeyNotFoundException($"No existe ContractVersion destino {request.TargetContractVersionId}.");

        if (sourceContract.MethodId != sourceSet.MethodId || targetContract.MethodId != sourceSet.MethodId)
        {
            throw new InvalidOperationException("MappingSet fuente y ambas ContractVersions deben pertenecer al mismo método.");
        }

        if (sourceContract.Id == targetContract.Id)
        {
            throw new InvalidOperationException("La clonación entre contratos requiere una ContractVersion destino diferente de la fuente.");
        }

        if (targetContract.Status is IntegrationContractVersionStatusEnum.Active or IntegrationContractVersionStatusEnum.Retired)
        {
            throw new InvalidOperationException("La ContractVersion destino debe permanecer editable (Draft o Validated); Active/Retired no admite clonación de mapping.");
        }

        if (targetContract.Revision != request.ExpectedTargetContractRevision)
        {
            throw new DbUpdateConcurrencyException(
                $"ContractVersion {targetContract.Id} cambió de revisión. Esperada={request.ExpectedTargetContractRevision}, actual={targetContract.Revision}.");
        }

        var sourceParameters = await LoadContractParametersAsync(sourceContract, ct);
        var targetParameters = await LoadContractParametersAsync(targetContract, ct);
        var sourceByPath = sourceParameters.ToDictionary(x => x.ParameterPath, StringComparer.Ordinal);

        var sourceRules = await _context.IntegrationMappingRules
            .AsNoTracking()
            .Include(x => x.Parameter)
            .Where(x => x.MappingSetId == sourceSet.Id)
            .OrderBy(x => x.ParameterId)
            .ThenBy(x => x.Priority)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);

        ValidateSourceRuleContractBinding(sourceContract, sourceByPath, sourceRules);

        var sourceRulesByPath = sourceRules
            .GroupBy(x => x.Parameter.ParameterPath, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<IntegrationMappingRule>)group.ToList(),
                StringComparer.Ordinal);

        var targetSet = new IntegrationMappingSet
        {
            Id = Guid.NewGuid(),
            MethodId = sourceSet.MethodId,
            ContractVersionId = targetContract.Id,
            Name = targetName,
            Notes = sourceSet.Notes,
            Status = IntegrationMappingSetStatusEnum.Draft,
            Version = 0,
            Revision = 1,
            IsActive = true
        };

        var classifications = new List<IntegrationMappingContractCloneItemDto>();
        var copiedRules = new List<IntegrationMappingRule>();
        var consumedSourcePaths = new HashSet<string>(StringComparer.Ordinal);

        foreach (var target in targetParameters.OrderBy(x => x.SortOrder).ThenBy(x => x.ParameterPath, StringComparer.Ordinal))
        {
            if (sourceByPath.TryGetValue(target.ParameterPath, out var exactSource))
            {
                consumedSourcePaths.Add(exactSource.ParameterPath);
                var compatibilityIssue = ResolveCompatibilityIssue(exactSource, target);
                sourceRulesByPath.TryGetValue(exactSource.ParameterPath, out var rulesForPath);
                rulesForPath ??= Array.Empty<IntegrationMappingRule>();

                if (compatibilityIssue is not null)
                {
                    classifications.Add(new(
                        exactSource.ParameterPath,
                        target.ParameterPath,
                        exactSource.Id,
                        target.Id,
                        IntegrationMappingContractCloneClassificationEnum.Incompatible,
                        0,
                        compatibilityIssue));
                    continue;
                }

                if (rulesForPath.Count == 0)
                {
                    classifications.Add(new(
                        exactSource.ParameterPath,
                        target.ParameterPath,
                        exactSource.Id,
                        target.Id,
                        exactSource.Required
                            ? IntegrationMappingContractCloneClassificationEnum.Incompatible
                            : IntegrationMappingContractCloneClassificationEnum.Copied,
                        0,
                        exactSource.Required
                            ? "El parámetro required existe en ambas versiones, pero el MappingSet fuente no contiene una regla que pueda clonarse."
                            : "El parámetro optional existe en ambas versiones y conserva explícitamente la ausencia de mapping."));
                    continue;
                }

                foreach (var sourceRule in rulesForPath)
                {
                    copiedRules.Add(CloneRule(sourceRule, targetSet.Id, target.Id, targetSet.MethodId));
                }

                classifications.Add(new(
                    exactSource.ParameterPath,
                    target.ParameterPath,
                    exactSource.Id,
                    target.Id,
                    IntegrationMappingContractCloneClassificationEnum.Copied,
                    rulesForPath.Count,
                    "ParameterPath y contrato técnico compatibles; las reglas se copiaron al ParameterId de la versión destino."));
                continue;
            }

            var semanticCandidates = sourceParameters
                .Where(source => !consumedSourcePaths.Contains(source.ParameterPath)
                    && string.Equals(source.XmlElementName, target.XmlElementName, StringComparison.Ordinal)
                    && source.Direction == target.Direction)
                .ToList();

            if (semanticCandidates.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Ambigüedad contractual al asociar {target.ParameterPath}: múltiples parámetros fuente comparten XmlElementName={target.XmlElementName}.");
            }

            if (semanticCandidates.Count == 1)
            {
                var semanticSource = semanticCandidates[0];
                consumedSourcePaths.Add(semanticSource.ParameterPath);
                classifications.Add(new(
                    semanticSource.ParameterPath,
                    target.ParameterPath,
                    semanticSource.Id,
                    target.Id,
                    IntegrationMappingContractCloneClassificationEnum.Incompatible,
                    0,
                    "ParameterPath cambió entre versiones; el mapping no se copia implícitamente."));
                continue;
            }

            classifications.Add(new(
                null,
                target.ParameterPath,
                null,
                target.Id,
                IntegrationMappingContractCloneClassificationEnum.New,
                0,
                target.Required
                    ? "Parámetro nuevo required; el Draft permanece incompleto hasta que se configure un mapping válido."
                    : "Parámetro nuevo optional; no se crea mapping implícito."));
        }

        foreach (var removedSource in sourceParameters
                     .Where(candidate => !consumedSourcePaths.Contains(candidate.ParameterPath))
                     .OrderBy(candidate => candidate.SortOrder)
                     .ThenBy(candidate => candidate.ParameterPath, StringComparer.Ordinal))
        {
            classifications.Add(new(
                removedSource.ParameterPath,
                null,
                removedSource.Id,
                null,
                IntegrationMappingContractCloneClassificationEnum.Removed,
                0,
                "Parámetro eliminado de la versión destino; se conserva solo como evidencia histórica y no genera regla activa."));
        }

        _context.IntegrationMappingSets.Add(targetSet);
        _context.IntegrationMappingRules.AddRange(copiedRules);
        await _context.SaveChangesAsync(ct);

        var cloneEvidence = new
        {
            SourceMappingSetId = sourceSet.Id,
            SourceContractVersionId = sourceContract.Id,
            TargetContractVersionId = targetContract.Id,
            SourceMappingRevision = sourceSet.Revision,
            TargetContractRevision = targetContract.Revision,
            Classifications = classifications.Select(item => new
            {
                item.SourceParameterPath,
                item.TargetParameterPath,
                item.SourceParameterId,
                item.TargetParameterId,
                Classification = item.Classification.ToString(),
                item.CopiedRuleCount,
                item.Reason
            }).ToList()
        };

        await AppendHistoryAsync(targetSet, "ClonedAcrossContractVersions", actor, ct, cloneEvidence);
        await _context.SaveChangesAsync(ct);

        var validation = await ValidateContractMappingAsync(
            targetSet.Id,
            new ValidateIntegrationContractMappingRequest(
                ExpectedMappingRevision: targetSet.Revision,
                ExpectedContractRevision: targetContract.Revision,
                IncludeWarnings: true),
            ct);

        var persistedSource = await _context.IntegrationMappingSets
            .AsNoTracking()
            .SingleAsync(x => x.Id == sourceSet.Id, ct);
        if (persistedSource.Revision != request.ExpectedSourceMappingRevision
            || persistedSource.ContractVersionId != sourceSet.ContractVersionId)
        {
            throw new DbUpdateConcurrencyException("El MappingSet fuente cambió durante la clonación; el resultado no puede considerarse estable.");
        }

        await transaction.CommitAsync(ct);

        var targetDto = await GetByIdAsync(targetSet.Id, ct)
            ?? throw new InvalidOperationException("No se pudo consultar el MappingSet destino clonado.");

        return new IntegrationMappingContractCloneResultDto(
            sourceSet.Id,
            sourceContract.Id,
            targetContract.Id,
            targetDto,
            classifications,
            validation);
    }

    public async Task<IntegrationContractMappingValidationResultDto> ValidateContractMappingAsync(
        Guid id,
        ValidateIntegrationContractMappingRequest request,
        CancellationToken ct = default)
    {
        var set = await _context.IntegrationMappingSets
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException($"No existe MappingSet {id}.");

        if (!set.ContractVersionId.HasValue)
        {
            throw new InvalidOperationException("La validación contract+mapping exige un MappingSet asociado explícitamente a una ContractVersion.");
        }

        if (set.Revision != request.ExpectedMappingRevision)
        {
            throw new DbUpdateConcurrencyException(
                $"MappingSet {id} cambió de revisión. Esperada={request.ExpectedMappingRevision}, actual={set.Revision}.");
        }

        var contract = await _context.IntegrationContractVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == set.ContractVersionId.Value, ct)
            ?? throw new InvalidOperationException($"MappingSet {id} referencia una ContractVersion inexistente.");

        if (contract.MethodId != set.MethodId)
        {
            throw new InvalidOperationException("MappingSet y ContractVersion deben pertenecer al mismo método.");
        }

        if (contract.Revision != request.ExpectedContractRevision)
        {
            throw new DbUpdateConcurrencyException(
                $"ContractVersion {contract.Id} cambió de revisión. Esperada={request.ExpectedContractRevision}, actual={contract.Revision}.");
        }

        var mappingValidation = await _validationService.ValidateAsync(id, request.IncludeWarnings, ct);

        var contractAfter = await _context.IntegrationContractVersions
            .AsNoTracking()
            .SingleAsync(x => x.Id == contract.Id, ct);
        if (contractAfter.Revision != request.ExpectedContractRevision)
        {
            throw new DbUpdateConcurrencyException(
                $"ContractVersion {contract.Id} cambió durante la validación conjunta. Esperada={request.ExpectedContractRevision}, actual={contractAfter.Revision}.");
        }

        var mappingAfter = await _context.IntegrationMappingSets
            .AsNoTracking()
            .SingleAsync(x => x.Id == id, ct);

        return new IntegrationContractMappingValidationResultDto(
            contractAfter.Id,
            contractAfter.Revision,
            mappingAfter.Id,
            mappingAfter.Revision,
            mappingValidation.IsValid,
            mappingValidation);
    }

    public async Task<IReadOnlyCollection<IntegrationMappingSetHistoryDto>> GetHistoryAsync(Guid id, CancellationToken ct = default)
    {
        var items = await _context.Set<IntegrationMappingSetHistory>()
            .AsNoTracking()
            .Where(x => x.MappingSetId == id)
            .OrderByDescending(x => x.PerformedAtUtc)
            .ToListAsync(ct);

        return items
            .Select(x => new IntegrationMappingSetHistoryDto(
                x.Id,
                x.MappingSetId,
                x.MethodId,
                x.Version,
                x.Status,
                x.Action,
                x.PerformedBy,
                x.PerformedAtUtc,
                x.SnapshotHash))
            .ToList();
    }

    public async Task<IntegrationMappingSetComparisonResultDto> CompareAsync(CompareIntegrationMappingSetsRequest request, CancellationToken ct = default)
    {
        var sets = await _context.Set<IntegrationMappingSet>()
            .AsNoTracking()
            .Include(x => x.Rules)
            .Where(x => x.Id == request.LeftMappingSetId || x.Id == request.RightMappingSetId)
            .ToListAsync(ct);

        var left = sets.FirstOrDefault(x => x.Id == request.LeftMappingSetId)
                   ?? throw new KeyNotFoundException($"No existe MappingSet {request.LeftMappingSetId}.");
        var right = sets.FirstOrDefault(x => x.Id == request.RightMappingSetId)
                    ?? throw new KeyNotFoundException($"No existe MappingSet {request.RightMappingSetId}.");

        if (left.MethodId != right.MethodId)
        {
            throw new InvalidOperationException("Solo se pueden comparar MappingSets del mismo método.");
        }

        var parameters = await _context.Set<IntegrationMethodParameter>()
            .AsNoTracking()
            .Where(x => x.MethodId == left.MethodId)
            .ToDictionaryAsync(x => x.Id, ct);

        var leftByParam = left.Rules.GroupBy(x => x.ParameterId).ToDictionary(x => x.Key, x => x.OrderBy(r => r.Priority).First());
        var rightByParam = right.Rules.GroupBy(x => x.ParameterId).ToDictionary(x => x.Key, x => x.OrderBy(r => r.Priority).First());
        var allParamIds = leftByParam.Keys.Union(rightByParam.Keys).OrderBy(x => x).ToList();

        var diffs = new List<IntegrationMappingSetRuleComparisonDto>();
        foreach (var paramId in allParamIds)
        {
            leftByParam.TryGetValue(paramId, out var leftRule);
            rightByParam.TryGetValue(paramId, out var rightRule);
            var parameterPath = parameters.TryGetValue(paramId, out var p) ? p.ParameterPath : $"Parameter:{paramId}";

            var changeType = "Equal";
            var changedFields = new List<string>();
            string potentialImpact;

            if (leftRule is null && rightRule is not null)
            {
                changeType = "Added";
                changedFields.Add("Rule");
                potentialImpact = "Nuevo parámetro cubierto en versión derecha.";
            }
            else if (leftRule is not null && rightRule is null)
            {
                changeType = "Removed";
                changedFields.Add("Rule");
                potentialImpact = "Parámetro dejó de cubrirse en versión derecha.";
            }
            else
            {
                CompareField(nameof(IntegrationMappingRule.Priority), leftRule!.Priority, rightRule!.Priority);
                CompareField(nameof(IntegrationMappingRule.RequiredOverride), leftRule.RequiredOverride, rightRule.RequiredOverride);
                CompareField(nameof(IntegrationMappingRule.SourceKind), leftRule.SourceKind, rightRule.SourceKind);
                CompareField(nameof(IntegrationMappingRule.SourceFieldPath), leftRule.SourceFieldPath, rightRule.SourceFieldPath);
                CompareField(nameof(IntegrationMappingRule.SourceCatalogFieldId), leftRule.SourceCatalogFieldId, rightRule.SourceCatalogFieldId);
                CompareField(nameof(IntegrationMappingRule.TransformationCode), leftRule.TransformationCode, rightRule.TransformationCode);
                CompareField(nameof(IntegrationMappingRule.FormatMask), leftRule.FormatMask, rightRule.FormatMask);
                CompareField(nameof(IntegrationMappingRule.FixedValue), leftRule.FixedValue, rightRule.FixedValue);
                CompareField(nameof(IntegrationMappingRule.DefaultValue), leftRule.DefaultValue, rightRule.DefaultValue);
                CompareField(nameof(IntegrationMappingRule.Enabled), leftRule.Enabled, rightRule.Enabled);

                if (changedFields.Count > 0)
                {
                    changeType = "Modified";
                }

                potentialImpact = changedFields.Count == 0
                    ? "Sin impacto funcional entre versiones."
                    : "Puede alterar payload resuelto, prioridad de regla o cobertura funcional.";
            }

            diffs.Add(new IntegrationMappingSetRuleComparisonDto(
                leftRule?.Id,
                rightRule?.Id,
                paramId,
                parameterPath,
                ResolveGroup(parameterPath),
                changeType,
                changedFields,
                potentialImpact,
                leftRule is null ? null : MapRule(leftRule),
                rightRule is null ? null : MapRule(rightRule)));

            void CompareField<T>(string fieldName, T leftValue, T rightValue)
            {
                if (!EqualityComparer<T>.Default.Equals(leftValue, rightValue))
                {
                    changedFields.Add(fieldName);
                }
            }
        }

        return new IntegrationMappingSetComparisonResultDto(
            new IntegrationMappingSetComparisonMetadataDto(left.Id, left.Name, left.Version, left.Status, left.PublishedAtUtc, left.PublishedBy, left.Notes),
            new IntegrationMappingSetComparisonMetadataDto(right.Id, right.Name, right.Version, right.Status, right.PublishedAtUtc, right.PublishedBy, right.Notes),
            diffs);
    }

    private async Task<IntegrationMappingSet> GetDraftForUpdateAsync(Guid id, CancellationToken ct)
    {
        var set = await _context.Set<IntegrationMappingSet>()
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new KeyNotFoundException($"No existe MappingSet {id}.");

        if (set.Status != IntegrationMappingSetStatusEnum.Draft)
        {
            throw new InvalidOperationException("Solo se permiten cambios sobre MappingSets en estado Draft.");
        }

        return set;
    }

    private async Task AppendHistoryAsync(
        IntegrationMappingSet set,
        string action,
        string actor,
        CancellationToken ct,
        object? evidence = null)
    {
        var snapshot = await _snapshotBuilder.BuildAsync(set.Id, ct);
        var snapshotJson = snapshot.SnapshotJson;
        var snapshotHash = snapshot.SnapshotHash;

        if (evidence is not null)
        {
            snapshotJson = JsonSerializer.Serialize(new
            {
                MappingSnapshotJson = snapshot.SnapshotJson,
                Evidence = evidence
            });
            snapshotHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshotJson)));
        }

        var history = new IntegrationMappingSetHistory
        {
            MappingSetId = set.Id,
            MethodId = set.MethodId,
            Version = set.Version,
            Status = set.Status,
            Action = action,
            PerformedBy = string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim(),
            PerformedAtUtc = DateTime.UtcNow,
            SnapshotJson = snapshotJson,
            SnapshotHash = snapshotHash
        };

        _context.Set<IntegrationMappingSetHistory>().Add(history);
    }

    private async Task<IReadOnlyList<IntegrationMethodParameter>> LoadContractParametersAsync(
        IntegrationContractVersion contract,
        CancellationToken ct)
    {
        var query = _context.IntegrationMethodParameters
            .AsNoTracking()
            .Where(x => x.MethodId == contract.MethodId
                && x.IsActive
                && x.Direction == IntegrationParameterDirectionEnum.Input);

        query = IsBaselineBridge(contract)
            ? query.Where(x => x.ContractVersionId == contract.Id || x.ContractVersionId == null)
            : query.Where(x => x.ContractVersionId == contract.Id);

        var parameters = await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);

        var grouped = parameters
            .GroupBy(x => x.ParameterPath, StringComparer.Ordinal)
            .ToList();

        if (!IsBaselineBridge(contract) && grouped.Any(group => group.Count() > 1))
        {
            var duplicate = grouped.First(group => group.Count() > 1).Key;
            throw new InvalidOperationException(
                $"ContractVersion {contract.Id} contiene ParameterPath duplicado '{duplicate}'; la clonación falla de forma cerrada.");
        }

        return grouped
            .Select(group => group
                .OrderByDescending(parameter => parameter.ContractVersionId == contract.Id)
                .ThenBy(parameter => parameter.Id)
                .First())
            .OrderBy(parameter => parameter.SortOrder)
            .ThenBy(parameter => parameter.ParameterPath, StringComparer.Ordinal)
            .ToList();
    }

    private static void ValidateSourceRuleContractBinding(
        IntegrationContractVersion sourceContract,
        IReadOnlyDictionary<string, IntegrationMethodParameter> sourceByPath,
        IReadOnlyCollection<IntegrationMappingRule> sourceRules)
    {
        foreach (var rule in sourceRules)
        {
            var parameter = rule.Parameter
                ?? throw new InvalidOperationException($"Regla fuente {rule.Id} no tiene parámetro contractual resoluble.");

            var contractBindingIsValid = IsBaselineBridge(sourceContract)
                ? parameter.ContractVersionId is null || parameter.ContractVersionId == sourceContract.Id
                : parameter.ContractVersionId == sourceContract.Id;

            if (parameter.MethodId != sourceContract.MethodId
                || !contractBindingIsValid
                || !sourceByPath.ContainsKey(parameter.ParameterPath))
            {
                throw new InvalidOperationException(
                    $"Regla fuente {rule.Id} referencia un parámetro que no pertenece inequívocamente a ContractVersion {sourceContract.Id}.");
            }
        }
    }

    private static string? ResolveCompatibilityIssue(
        IntegrationMethodParameter source,
        IntegrationMethodParameter target)
    {
        if (!string.Equals(source.ParameterPath, target.ParameterPath, StringComparison.Ordinal))
        {
            return "ParameterPath no es idéntico entre versiones.";
        }

        if (source.Direction != target.Direction)
        {
            return $"Dirección incompatible: {source.Direction} -> {target.Direction}.";
        }

        if (source.Cardinality != target.Cardinality || source.MaxOccurs != target.MaxOccurs)
        {
            return $"Cardinalidad incompatible: {source.Cardinality}/{source.MaxOccurs?.ToString() ?? "unbounded"} -> {target.Cardinality}/{target.MaxOccurs?.ToString() ?? "unbounded"}.";
        }

        if (!string.Equals(source.DataType, target.DataType, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(NormalizeXsdType(source.XsdType), NormalizeXsdType(target.XsdType), StringComparison.OrdinalIgnoreCase))
        {
            return $"Tipo incompatible: {source.DataType}|{source.XsdType} -> {target.DataType}|{target.XsdType}.";
        }

        if (!IsNoCodeSupported(source) || !IsNoCodeSupported(target))
        {
            return "El parámetro fuente o destino requiere desarrollo y no admite clonación automática de mapping.";
        }

        return null;
    }

    private static string NormalizeXsdType(string value)
    {
        var trimmed = value.Trim();
        var colon = trimmed.LastIndexOf(':');
        return colon >= 0 && colon < trimmed.Length - 1
            ? trimmed[(colon + 1)..]
            : trimmed;
    }

    private static bool IsNoCodeSupported(IntegrationMethodParameter parameter)
        => parameter.Direction == IntegrationParameterDirectionEnum.Input
           && parameter.Cardinality == IntegrationParameterCardinalityEnum.Scalar
           && string.Equals(parameter.SupportStatus, "Supported", StringComparison.OrdinalIgnoreCase);

    private static bool IsBaselineBridge(IntegrationContractVersion contract)
        => contract.SourceType == IntegrationContractSourceTypeEnum.BaselineMigration
           && string.Equals(contract.VersionLabel, "baseline-v1", StringComparison.Ordinal);

    private static IntegrationMappingRule CloneRule(
        IntegrationMappingRule source,
        Guid targetMappingSetId,
        long targetParameterId,
        int methodId)
        => new()
        {
            MappingSetId = targetMappingSetId,
            MethodId = methodId,
            ParameterId = targetParameterId,
            SourceKind = source.SourceKind,
            SourceCatalogFieldId = source.SourceCatalogFieldId,
            SourceFieldPath = source.SourceFieldPath,
            FixedValue = source.FixedValue,
            DefaultValue = source.DefaultValue,
            TransformationCode = source.TransformationCode,
            FormatMask = source.FormatMask,
            Priority = source.Priority,
            RequiredOverride = source.RequiredOverride,
            Enabled = source.Enabled,
            ConditionExpression = source.ConditionExpression
        };

    private IntegrationMappingWorkspaceSourceCompatibilityDto BuildSourceCompatibility(
        IntegrationMethodParameter parameter,
        IntegrationSourceCatalogField source)
    {
        var typeCompatible = AreMappingTypesCompatible(source.DataType, parameter.DataType);
        var directCardinalityCompatible = IsCardinalityCompatible(source.Cardinality, parameter.Cardinality, null);
        var allowsNoTransformation = typeCompatible && directCardinalityCompatible;

        var transformations = _transformationRegistry.Descriptors
            .Where(descriptor =>
                typeCompatible
                && descriptor.InputTypes.Any(input => AreTransformationTypesCompatible(source.DataType, input))
                && IsCardinalityCompatible(source.Cardinality, parameter.Cardinality, descriptor.Code)
                && AreMappingTypesCompatible(
                    string.Equals(descriptor.OutputType, "same", StringComparison.OrdinalIgnoreCase)
                        ? source.DataType
                        : descriptor.OutputType,
                    parameter.DataType))
            .OrderBy(descriptor => descriptor.DisplayNameEs, StringComparer.OrdinalIgnoreCase)
            .Select(descriptor => new IntegrationMappingWorkspaceTransformationOptionDto(
                descriptor.Code,
                descriptor.DisplayNameEs,
                descriptor.DescriptionEs,
                descriptor.RequiresMask))
            .ToList();

        return new IntegrationMappingWorkspaceSourceCompatibilityDto(
            parameter.Id,
            source.Id,
            allowsNoTransformation,
            transformations);
    }

    private static IntegrationSourceCatalogFieldDto MapSourceField(IntegrationSourceCatalogField field)
        => new(
            field.Id,
            field.MethodId,
            field.SourceKind,
            field.EntityName,
            field.FieldPath,
            field.DisplayName,
            field.DataType,
            field.Cardinality,
            field.Nullable,
            field.SortOrder,
            field.IsActive);

    private static string SourceGroupLabel(IntegrationSourceKindEnum sourceKind)
        => sourceKind switch
        {
            IntegrationSourceKindEnum.Transaction => "Transacción",
            IntegrationSourceKindEnum.Addenda => "Adenda",
            IntegrationSourceKindEnum.Batch => "Lote",
            IntegrationSourceKindEnum.Cycle => "Ciclo",
            IntegrationSourceKindEnum.ClearingHouse => "Cámara de compensación",
            IntegrationSourceKindEnum.NachaHeader => "Archivo NACHA-M · Encabezado",
            IntegrationSourceKindEnum.BatchHeader => "Archivo NACHA-M · Lote",
            IntegrationSourceKindEnum.EntryDetail => "Archivo NACHA-M · Detalle",
            IntegrationSourceKindEnum.AddendaRecord => "Archivo NACHA-M · Adenda",
            IntegrationSourceKindEnum.BatchControl => "Archivo NACHA-M · Control de lote",
            IntegrationSourceKindEnum.FileControl => "Archivo NACHA-M · Control de archivo",
            IntegrationSourceKindEnum.Prenotification => "Prenotificación",
            IntegrationSourceKindEnum.DifferentialResponse => "Respuesta diferencial",
            _ => sourceKind.ToString()
        };

    private static bool IsCardinalityCompatible(
        IntegrationParameterCardinalityEnum source,
        IntegrationParameterCardinalityEnum destination,
        string? transformationCode)
    {
        if (source == IntegrationParameterCardinalityEnum.Scalar || destination != IntegrationParameterCardinalityEnum.Scalar)
        {
            return true;
        }

        return string.Equals(transformationCode, "Concat", StringComparison.OrdinalIgnoreCase);
    }

    private static bool AreMappingTypesCompatible(string sourceType, string destinationType)
    {
        if (sourceType.Equals(destinationType, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var numeric = new[] { "int", "long", "decimal", "double", "float" };
        if (numeric.Contains(sourceType, StringComparer.OrdinalIgnoreCase)
            && numeric.Contains(destinationType, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        if (destinationType.Equals("string", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return (sourceType.Equals("datetime", StringComparison.OrdinalIgnoreCase)
                || sourceType.Equals("timespan", StringComparison.OrdinalIgnoreCase))
               && destinationType.Equals("string", StringComparison.OrdinalIgnoreCase);
    }

    private static bool AreTransformationTypesCompatible(string sourceType, string expectedType)
    {
        var source = NormalizeDataType(sourceType);
        var expected = NormalizeDataType(expectedType);
        if (expected is "object" or "same" || source == expected)
        {
            return true;
        }

        var numeric = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "int", "integer", "int32", "long", "int64", "decimal", "double", "float", "single"
        };
        if (numeric.Contains(source) && numeric.Contains(expected))
        {
            return true;
        }

        if (source == "string")
        {
            return true;
        }

        return source == "datetime" && expected == "string"
               || source == "collection" && expected == "string";
    }

    private static string NormalizeDataType(string value)
        => string.IsNullOrWhiteSpace(value) ? "string" : value.Trim().ToLowerInvariant();

    private async Task<HashSet<string>> LoadNewCloneTargetPathsAsync(Guid mappingSetId, CancellationToken ct)
    {
        var snapshotJson = await _context.IntegrationMappingSetHistory
            .AsNoTracking()
            .Where(x => x.MappingSetId == mappingSetId && x.Action == "ClonedAcrossContractVersions")
            .OrderByDescending(x => x.PerformedAtUtc)
            .Select(x => x.SnapshotJson)
            .FirstOrDefaultAsync(ct);

        var paths = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(snapshotJson))
        {
            return paths;
        }

        try
        {
            using var document = JsonDocument.Parse(snapshotJson);
            if (!document.RootElement.TryGetProperty("Evidence", out var evidence)
                || !evidence.TryGetProperty("Classifications", out var classifications)
                || classifications.ValueKind != JsonValueKind.Array)
            {
                return paths;
            }

            foreach (var item in classifications.EnumerateArray())
            {
                if (!item.TryGetProperty("Classification", out var classification)
                    || !string.Equals(classification.GetString(), "New", StringComparison.Ordinal)
                    || !item.TryGetProperty("TargetParameterPath", out var pathElement))
                {
                    continue;
                }

                var path = pathElement.GetString();
                if (!string.IsNullOrWhiteSpace(path))
                {
                    paths.Add(path);
                }
            }
        }
        catch (JsonException)
        {
            return paths;
        }

        return paths;
    }

    private static string ToWorkspaceStatus(string? status)
        => status switch
        {
            "valid" => "Complete",
            "incomplete" => "Pending",
            "invalid" => "Invalid",
            "inactive" => "NotApplicable",
            _ => "Pending"
        };

    private static string RequiredText(string? value, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{argumentName} es obligatorio.", argumentName);
        }

        return value.Trim();
    }

    private static IntegrationMappingSetDto MapToDto(IntegrationMappingSet x)
        => new(
            x.Id,
            x.MethodId,
            x.Method?.Code ?? string.Empty,
            x.ContractVersionId,
            x.Revision,
            x.Name,
            x.Version,
            x.Status,
            x.IsActive,
            x.Notes,
            x.PublishedAtUtc,
            x.PublishedBy,
            x.Rules
                .OrderBy(r => r.ParameterId)
                .ThenBy(r => r.Priority)
                .Select(r => new IntegrationMappingRuleDto(
                    r.Id,
                    r.MappingSetId,
                    r.MethodId,
                    r.ParameterId,
                    r.SourceKind,
                    r.SourceCatalogFieldId,
                    r.SourceFieldPath,
                    r.FixedValue,
                    r.DefaultValue,
                    r.TransformationCode,
                    r.FormatMask,
                    r.Priority,
                    r.RequiredOverride,
                    r.Enabled,
                    r.ConditionExpression))
                .ToList());

    private static IntegrationMappingRuleDto MapRule(IntegrationMappingRule r)
        => new(
            r.Id,
            r.MappingSetId,
            r.MethodId,
            r.ParameterId,
            r.SourceKind,
            r.SourceCatalogFieldId,
            r.SourceFieldPath,
            r.FixedValue,
            r.DefaultValue,
            r.TransformationCode,
            r.FormatMask,
            r.Priority,
            r.RequiredOverride,
            r.Enabled,
            r.ConditionExpression);

    private static string ResolveGroup(string parameterPath)
    {
        var path = parameterPath.ToLowerInvariant();
        if (path.Contains("addendas")) return "addenda";
        if (path.Contains("transactions")) return "transaccion";
        if (path.Contains("batch")) return "lote";
        if (path.Contains("cycle") || path.Contains("clearinghouse")) return "ciclo-camara";
        return "configuracion";
    }
}
