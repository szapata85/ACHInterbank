using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Integrations.Services;

[Scoped]
public sealed class IntegrationContractVersionService : IIntegrationContractVersionService
{
    private const string SupportedParameterStatus = "Supported";
    private readonly AchDbContext _context;
    private readonly IIntegrationMappingValidationService _mappingValidationService;
    private readonly IntegrationMappingSnapshotBuilder _mappingSnapshotBuilder;

    public IntegrationContractVersionService(AchDbContext context)
        : this(
            context,
            new IntegrationMappingValidationService(context),
            new IntegrationMappingSnapshotBuilder(context))
    {
    }

    public IntegrationContractVersionService(
        AchDbContext context,
        IIntegrationMappingValidationService mappingValidationService,
        IntegrationMappingSnapshotBuilder mappingSnapshotBuilder)
    {
        _context = context;
        _mappingValidationService = mappingValidationService;
        _mappingSnapshotBuilder = mappingSnapshotBuilder;
    }

    public async Task<IReadOnlyCollection<IntegrationContractVersionDto>> GetByMethodAsync(
        int methodId,
        CancellationToken ct = default)
    {
        await EnsureBaselineAsync(ct);
        var items = await _context.IntegrationContractVersions
            .AsNoTracking()
            .Where(contract => contract.MethodId == methodId)
            .OrderByDescending(contract => contract.Status == IntegrationContractVersionStatusEnum.Active)
            .ThenByDescending(contract => contract.Revision)
            .ThenByDescending(contract => contract.CreatedAt)
            .ToListAsync(ct);

        return items.Select(Map).ToList();
    }

    public async Task<IntegrationContractVersionDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await EnsureBaselineAsync(ct);
        var contract = await _context.IntegrationContractVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, ct);
        return contract is null ? null : Map(contract);
    }

    public async Task<IntegrationContractVersionDetailDto?> GetDetailAsync(Guid id, CancellationToken ct = default)
    {
        await EnsureBaselineAsync(ct);
        var contract = await _context.IntegrationContractVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, ct);
        if (contract is null)
        {
            return null;
        }

        var parameters = await GetParameterQuery(contract)
            .OrderBy(parameter => parameter.SortOrder)
            .ThenBy(parameter => parameter.ParameterPath)
            .ToListAsync(ct);

        return new IntegrationContractVersionDetailDto(Map(contract), parameters.Select(MapContractParameter).ToList());
    }

    public async Task<IntegrationContractVersionDto?> ResolveAsync(
        int methodId,
        Guid? contractVersionId = null,
        CancellationToken ct = default)
    {
        await EnsureBaselineAsync(ct);

        var query = _context.IntegrationContractVersions
            .AsNoTracking()
            .Where(contract => contract.MethodId == methodId);

        IntegrationContractVersion? contract;
        if (contractVersionId.HasValue)
        {
            contract = await query.FirstOrDefaultAsync(contract => contract.Id == contractVersionId.Value, ct);
        }
        else
        {
            contract = await query.SingleOrDefaultAsync(
                contract => contract.Status == IntegrationContractVersionStatusEnum.Active,
                ct);
        }

        return contract is null ? null : Map(contract);
    }

    public async Task<IReadOnlyCollection<IntegrationMethodParameterDto>> GetParametersAsync(
        Guid contractVersionId,
        CancellationToken ct = default)
    {
        await EnsureBaselineAsync(ct);
        var contract = await _context.IntegrationContractVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == contractVersionId, ct)
            ?? throw new KeyNotFoundException($"No existe ContractVersion {contractVersionId}.");

        var parameters = await GetParameterQuery(contract)
            .Where(parameter => parameter.IsActive)
            .OrderBy(parameter => parameter.SortOrder)
            .ThenBy(parameter => parameter.ParameterPath)
            .ToListAsync(ct);

        return parameters.Select(MapParameter).ToList();
    }

    public async Task<IntegrationContractVersionDto> CreateDraftAsync(
        int methodId,
        CreateIntegrationContractVersionRequest request,
        CancellationToken ct = default)
    {
        await EnsureBaselineAsync(ct);
        await EnsureActiveMethodAsync(methodId, ct);

        var versionLabel = RequiredText(request.VersionLabel, nameof(request.VersionLabel));
        await EnsureVersionLabelAvailableAsync(methodId, versionLabel, ct);
        var actor = RequiredText(request.Actor, nameof(request.Actor));

        var contract = new IntegrationContractVersion
        {
            MethodId = methodId,
            VersionLabel = versionLabel,
            Status = IntegrationContractVersionStatusEnum.Draft,
            OperationName = RequiredText(request.OperationName, nameof(request.OperationName)),
            TargetNamespace = RequiredText(request.TargetNamespace, nameof(request.TargetNamespace)),
            SoapAction = RequiredText(request.SoapAction, nameof(request.SoapAction)),
            SourceType = IntegrationContractSourceTypeEnum.ManualApproved,
            SourceReference = OptionalText(request.SourceReference),
            Notes = OptionalText(request.Notes),
            Revision = 1,
            ImportedAtUtc = DateTime.UtcNow,
            ImportedBy = actor
        };

        _context.IntegrationContractVersions.Add(contract);
        await _context.SaveChangesAsync(ct);
        return Map(contract);
    }

    public async Task<IntegrationContractVersionDetailDto> CloneAsync(
        int methodId,
        CloneIntegrationContractVersionRequest request,
        CancellationToken ct = default)
    {
        await EnsureBaselineAsync(ct);
        await EnsureActiveMethodAsync(methodId, ct);

        var source = await _context.IntegrationContractVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(contract => contract.Id == request.SourceContractVersionId, ct)
            ?? throw new KeyNotFoundException($"No existe ContractVersion {request.SourceContractVersionId}.");
        if (source.MethodId != methodId)
        {
            throw new InvalidOperationException("La versión contractual de origen no pertenece al método solicitado.");
        }

        var versionLabel = RequiredText(request.VersionLabel, nameof(request.VersionLabel));
        await EnsureVersionLabelAvailableAsync(methodId, versionLabel, ct);
        var actor = RequiredText(request.Actor, nameof(request.Actor));

        var sourceParameters = await GetParameterQuery(source)
            .OrderBy(parameter => parameter.SortOrder)
            .ThenBy(parameter => parameter.ParameterPath)
            .ToListAsync(ct);

        // The nullable baseline bridge can expose both a version-bound row and a legacy row for the same path.
        // Prefer the explicitly version-bound row so the clone remains deterministic and fully versioned.
        sourceParameters = sourceParameters
            .GroupBy(parameter => parameter.ParameterPath, StringComparer.Ordinal)
            .Select(group => group
                .OrderByDescending(parameter => parameter.ContractVersionId == source.Id)
                .ThenBy(parameter => parameter.Id)
                .First())
            .OrderBy(parameter => parameter.SortOrder)
            .ThenBy(parameter => parameter.ParameterPath)
            .ToList();

        var clone = new IntegrationContractVersion
        {
            MethodId = methodId,
            VersionLabel = versionLabel,
            Status = IntegrationContractVersionStatusEnum.Draft,
            OperationName = source.OperationName,
            TargetNamespace = source.TargetNamespace,
            SoapAction = source.SoapAction,
            SourceType = IntegrationContractSourceTypeEnum.ManualApproved,
            SourceDocumentName = source.SourceDocumentName,
            SourceDocumentHash = source.SourceDocumentHash,
            SourceReference = source.SourceReference,
            Notes = OptionalText(request.Notes) ?? source.Notes,
            Revision = 1,
            ImportedAtUtc = DateTime.UtcNow,
            ImportedBy = actor
        };

        _context.IntegrationContractVersions.Add(clone);
        foreach (var parameter in sourceParameters)
        {
            _context.IntegrationMethodParameters.Add(CloneParameter(parameter, clone.Id));
        }

        await _context.SaveChangesAsync(ct);
        return await GetDetailAsync(clone.Id, ct)
            ?? throw new InvalidOperationException("No se pudo consultar la versión contractual clonada.");
    }

    public async Task<IntegrationContractParameterPresentationResultDto> UpdateParameterPresentationAsync(
        Guid contractVersionId,
        long parameterId,
        UpdateIntegrationContractParameterPresentationRequest request,
        CancellationToken ct = default)
    {
        _ = RequiredText(request.Actor, nameof(request.Actor));

        var contract = await _context.IntegrationContractVersions
            .FirstOrDefaultAsync(item => item.Id == contractVersionId, ct)
            ?? throw new KeyNotFoundException($"No existe ContractVersion {contractVersionId}.");

        if (contract.Status is IntegrationContractVersionStatusEnum.Active or IntegrationContractVersionStatusEnum.Retired)
        {
            throw new InvalidOperationException("La metadata funcional de una versión contractual Active o Retired es inmutable; cree una nueva versión Draft.");
        }

        if (contract.Revision != request.ExpectedRevision)
        {
            throw new DbUpdateConcurrencyException(
                $"ContractVersion {contractVersionId} cambió de revisión. Esperada={request.ExpectedRevision}, actual={contract.Revision}.");
        }

        var parameter = await _context.IntegrationMethodParameters
            .FirstOrDefaultAsync(item => item.Id == parameterId && item.ContractVersionId == contractVersionId, ct)
            ?? throw new KeyNotFoundException($"No existe Parameter {parameterId} en ContractVersion {contractVersionId}.");

        parameter.DisplayName = RequiredText(request.DisplayName, nameof(request.DisplayName));
        parameter.DescriptionEs = OptionalText(request.DescriptionEs) ?? string.Empty;
        parameter.UiHelpText = OptionalText(request.UiHelpText) ?? string.Empty;
        parameter.Category = OptionalText(request.Category) ?? string.Empty;
        parameter.ExampleValue = OptionalText(request.ExampleValue) ?? string.Empty;

        // Revision is the aggregate concurrency token. A presentation change advances the contract revision
        // even though it deliberately leaves all technical/runtime fields untouched.
        contract.Revision = checked(contract.Revision + 1);
        await _context.SaveChangesAsync(ct);

        return new IntegrationContractParameterPresentationResultDto(
            contract.Id,
            contract.Revision,
            MapContractParameter(parameter));
    }

    public async Task<IntegrationContractDiffDto> CompareAsync(Guid leftId, Guid rightId, CancellationToken ct = default)
    {
        await EnsureBaselineAsync(ct);

        var contracts = await _context.IntegrationContractVersions
            .AsNoTracking()
            .Where(contract => contract.Id == leftId || contract.Id == rightId)
            .ToListAsync(ct);
        var left = contracts.FirstOrDefault(contract => contract.Id == leftId)
            ?? throw new KeyNotFoundException($"No existe ContractVersion {leftId}.");
        var right = contracts.FirstOrDefault(contract => contract.Id == rightId)
            ?? throw new KeyNotFoundException($"No existe ContractVersion {rightId}.");
        if (left.MethodId != right.MethodId)
        {
            throw new InvalidOperationException("Solo se pueden comparar versiones contractuales del mismo método.");
        }

        var leftParameters = await LoadParametersByPathAsync(left, ct);
        var rightParameters = await LoadParametersByPathAsync(right, ct);
        return BuildDiff(
            left.Id,
            right.Id,
            left.MethodId,
            left.TargetNamespace,
            right.TargetNamespace,
            leftParameters,
            rightParameters);
    }

    public async Task<IntegrationContractDiffDto> CompareArtifactAsync(
        Guid baseContractVersionId,
        int methodId,
        IntegrationContractArtifactAnalysisDto analysis,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        var targetNamespace = analysis.TargetNamespace;
        if (!analysis.Importable || string.IsNullOrWhiteSpace(targetNamespace))
        {
            throw new InvalidOperationException("El diff previo exige un análisis contractual importable producido por backend.");
        }

        await EnsureBaselineAsync(ct);
        var left = await _context.IntegrationContractVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(contract => contract.Id == baseContractVersionId, ct)
            ?? throw new KeyNotFoundException($"No existe ContractVersion {baseContractVersionId}.");
        if (left.MethodId != methodId)
        {
            throw new InvalidOperationException("La versión base no pertenece al método que se desea importar.");
        }

        var leftParameters = await LoadParametersByPathAsync(left, ct);
        var rightParameters = analysis.Parameters
            .Select(parameter => new IntegrationMethodParameter
            {
                MethodId = methodId,
                ContractVersionId = null,
                ParameterPath = parameter.ParameterPath,
                XmlElementName = parameter.XmlElementName,
                DisplayName = parameter.DisplayName,
                DescriptionEs = string.Empty,
                UiHelpText = string.Empty,
                Category = "ImportedPreview",
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
            })
            .GroupBy(parameter => parameter.ParameterPath, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);

        return BuildDiff(
            left.Id,
            Guid.Empty,
            methodId,
            left.TargetNamespace,
            targetNamespace,
            leftParameters,
            rightParameters);
    }

    private static IntegrationContractDiffDto BuildDiff(
        Guid leftId,
        Guid rightId,
        int methodId,
        string leftNamespace,
        string rightNamespace,
        IReadOnlyDictionary<string, IntegrationMethodParameter> leftParameters,
        IReadOnlyDictionary<string, IntegrationMethodParameter> rightParameters)
    {
        var changes = new List<IntegrationContractDiffChangeDto>();

        foreach (var parameterPath in leftParameters.Keys
                     .Union(rightParameters.Keys, StringComparer.Ordinal)
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            var hasLeft = leftParameters.TryGetValue(parameterPath, out var leftParameter);
            var hasRight = rightParameters.TryGetValue(parameterPath, out var rightParameter);

            if (!hasLeft)
            {
                changes.Add(Change(
                    IntegrationContractChangeTypeEnum.Added,
                    rightParameter!,
                    ImpactForTarget(rightParameter!),
                    null,
                    DescribeParameter(rightParameter!)));
                continue;
            }

            if (!hasRight)
            {
                changes.Add(Change(
                    IntegrationContractChangeTypeEnum.Removed,
                    leftParameter!,
                    IntegrationContractDiffImpactEnum.MappingRequired,
                    DescribeParameter(leftParameter!),
                    null));
                continue;
            }

            AddModifiedParameterChanges(changes, leftParameter!, rightParameter!);
        }

        if (!string.Equals(leftNamespace, rightNamespace, StringComparison.Ordinal))
        {
            changes.Add(new IntegrationContractDiffChangeDto(
                IntegrationContractChangeTypeEnum.NamespaceChanged,
                "$contract",
                "Namespace SOAP",
                IntegrationContractDiffImpactEnum.RequiresDevelopment,
                leftNamespace,
                rightNamespace));
        }

        changes = changes
            .OrderBy(change => change.TechnicalName, StringComparer.Ordinal)
            .ThenBy(change => change.ChangeType)
            .ToList();

        var summary = new IntegrationContractDiffSummaryDto(
            Added: changes.Count(change => change.ChangeType == IntegrationContractChangeTypeEnum.Added),
            Removed: changes.Count(change => change.ChangeType == IntegrationContractChangeTypeEnum.Removed),
            Modified: changes.Count(change => change.ChangeType is not IntegrationContractChangeTypeEnum.Added
                and not IntegrationContractChangeTypeEnum.Removed),
            RequiresMapping: changes.Count(change => change.Impact == IntegrationContractDiffImpactEnum.MappingRequired),
            RequiresDevelopment: changes.Count(change => change.Impact == IntegrationContractDiffImpactEnum.RequiresDevelopment));

        return new IntegrationContractDiffDto(leftId, rightId, methodId, summary, changes);
    }

    public async Task<IntegrationContractVersionDto> MarkValidatedAsync(
        Guid id,
        long expectedRevision,
        CancellationToken ct = default)
    {
        var contract = await GetForLifecycleUpdateAsync(id, expectedRevision, ct);
        if (contract.Status == IntegrationContractVersionStatusEnum.Validated)
        {
            return Map(contract);
        }

        if (contract.Status != IntegrationContractVersionStatusEnum.Draft)
        {
            throw new InvalidOperationException($"Solo Draft puede pasar a Validated. Estado actual: {contract.Status}.");
        }

        contract.Status = IntegrationContractVersionStatusEnum.Validated;
        await _context.SaveChangesAsync(ct);
        return Map(contract);
    }

    public async Task<IntegrationContractMappingTransitionResultDto> ActivateAsync(
        Guid id,
        ActivateIntegrationContractVersionRequest request,
        CancellationToken ct = default)
    {
        var actor = RequiredText(request.ActivatedBy, nameof(request.ActivatedBy));
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var contract = await _context.IntegrationContractVersions
            .FirstOrDefaultAsync(item => item.Id == id, ct)
            ?? throw new KeyNotFoundException($"No existe ContractVersion {id}.");
        var mapping = await _context.IntegrationMappingSets
            .FirstOrDefaultAsync(item => item.Id == request.MappingSetId, ct)
            ?? throw new KeyNotFoundException($"No existe MappingSet {request.MappingSetId}.");

        EnsureExpectedRevision(contract, request.ExpectedContractRevision);
        EnsureExpectedRevision(mapping, request.ExpectedMappingRevision);
        EnsureMappingBelongsToContract(mapping, contract);

        if (contract.Status != IntegrationContractVersionStatusEnum.Validated)
        {
            throw new InvalidOperationException(
                $"La activación atómica exige una ContractVersion Validated. Estado actual: {contract.Status}.");
        }

        if (mapping.Status != IntegrationMappingSetStatusEnum.Draft)
        {
            throw new InvalidOperationException(
                $"La activación atómica exige un MappingSet Draft. Estado actual: {mapping.Status}.");
        }

        var existingPublishedForTarget = await _context.IntegrationMappingSets
            .AsNoTracking()
            .Where(item => item.MethodId == contract.MethodId
                && item.ContractVersionId == contract.Id
                && item.Id != mapping.Id
                && item.Status == IntegrationMappingSetStatusEnum.Published)
            .Select(item => item.Id)
            .ToListAsync(ct);
        if (existingPublishedForTarget.Count > 0)
        {
            throw new InvalidOperationException(
                $"ContractVersion {contract.Id} ya tiene un MappingSet Published; la activación falla de forma cerrada.");
        }

        var activeContracts = await _context.IntegrationContractVersions
            .Where(item => item.MethodId == contract.MethodId
                && item.Id != contract.Id
                && item.Status == IntegrationContractVersionStatusEnum.Active)
            .ToListAsync(ct);
        if (activeContracts.Count > 1)
        {
            throw new InvalidOperationException(
                $"El método {contract.MethodId} tiene más de una ContractVersion Active; no se permite continuar.");
        }

        var currentContract = activeContracts.SingleOrDefault();
        IntegrationMappingSet? currentMapping = null;
        if (currentContract is not null)
        {
            var diff = await CompareAsync(currentContract.Id, contract.Id, ct);
            if (diff.Summary.RequiresDevelopment > 0)
            {
                throw new InvalidOperationException(
                    $"ContractVersion {contract.Id} contiene {diff.Summary.RequiresDevelopment} cambio(s) RequiresDevelopment y no puede activarse sin desarrollo explícito.");
            }

            currentMapping = await LoadPublishedMappingForContractAsync(currentContract, ct);
        }

        var validation = await _mappingValidationService.EvaluateAsync(mapping.Id, includeWarnings: true, ct);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                "ContractVersion + MappingSet no están completos/compatibles; la activación fue rechazada sin modificar el estado vigente.");
        }

        await EnsureRevisionsStillMatchAsync(
            contract.Id,
            request.ExpectedContractRevision,
            mapping.Id,
            request.ExpectedMappingRevision,
            ct);

        var transitionAtUtc = DateTime.UtcNow;
        Guid? previousContractVersionId = null;
        Guid? previousMappingSetId = null;

        if (currentContract is not null)
        {
            previousContractVersionId = currentContract.Id;
            previousMappingSetId = currentMapping?.Id;
            currentContract.Status = IntegrationContractVersionStatusEnum.Retired;
            if (currentMapping is not null)
            {
                currentMapping.Status = IntegrationMappingSetStatusEnum.Archived;
                currentMapping.IsActive = false;
            }

            await _context.SaveChangesAsync(ct);
        }

        await EnsureRevisionsStillMatchAsync(
            contract.Id,
            request.ExpectedContractRevision,
            mapping.Id,
            request.ExpectedMappingRevision,
            ct);

        var nextVersion = (await _context.IntegrationMappingSets
            .Where(item => item.ContractVersionId == contract.Id)
            .Select(item => (int?)item.Version)
            .MaxAsync(ct) ?? 0) + 1;

        contract.Status = IntegrationContractVersionStatusEnum.Active;
        contract.ActivatedAtUtc = transitionAtUtc;
        contract.ActivatedBy = actor;

        mapping.Status = IntegrationMappingSetStatusEnum.Published;
        mapping.IsActive = true;
        mapping.Version = nextVersion;
        mapping.PublishedAtUtc = transitionAtUtc;
        mapping.PublishedBy = actor;
        mapping.ValidationSummaryJson = SerializeValidationSummary(validation);

        await _context.SaveChangesAsync(ct);

        if (currentMapping is not null)
        {
            await AppendMappingHistoryAsync(
                currentMapping,
                "ArchivedByContractActivation",
                actor,
                new
                {
                    PreviousContractVersionId = currentContract!.Id,
                    NewContractVersionId = contract.Id,
                    NewMappingSetId = mapping.Id,
                    TransitionedAtUtc = transitionAtUtc
                },
                ct);
        }

        await AppendMappingHistoryAsync(
            mapping,
            "ActivatedWithContract",
            actor,
            new
            {
                ContractVersionId = contract.Id,
                MappingSetId = mapping.Id,
                PreviousContractVersionId = previousContractVersionId,
                PreviousMappingSetId = previousMappingSetId,
                ExpectedContractRevision = request.ExpectedContractRevision,
                ExpectedMappingRevision = request.ExpectedMappingRevision,
                Validation = ValidationEvidence(validation),
                TransitionedAtUtc = transitionAtUtc
            },
            ct);
        await _context.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        var persistedContract = await GetByIdAsync(contract.Id, ct)
            ?? throw new InvalidOperationException("No se pudo consultar la ContractVersion activada.");
        var persistedMapping = await LoadMappingDtoAsync(mapping.Id, ct)
            ?? throw new InvalidOperationException("No se pudo consultar el MappingSet activado.");

        return new IntegrationContractMappingTransitionResultDto(
            "Activation",
            persistedContract,
            persistedMapping,
            previousContractVersionId,
            previousMappingSetId,
            validation,
            transitionAtUtc,
            actor);
    }

    public async Task<IntegrationContractMappingTransitionResultDto> RollbackAsync(
        Guid targetContractVersionId,
        RollbackIntegrationContractVersionRequest request,
        CancellationToken ct = default)
    {
        var actor = RequiredText(request.RolledBackBy, nameof(request.RolledBackBy));
        if (targetContractVersionId == request.CurrentContractVersionId)
        {
            throw new InvalidOperationException("El rollback requiere una ContractVersion histórica diferente de la Active actual.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var targetContract = await _context.IntegrationContractVersions
            .FirstOrDefaultAsync(item => item.Id == targetContractVersionId, ct)
            ?? throw new KeyNotFoundException($"No existe ContractVersion destino {targetContractVersionId}.");
        var targetMapping = await _context.IntegrationMappingSets
            .FirstOrDefaultAsync(item => item.Id == request.TargetMappingSetId, ct)
            ?? throw new KeyNotFoundException($"No existe MappingSet destino {request.TargetMappingSetId}.");
        var currentContract = await _context.IntegrationContractVersions
            .FirstOrDefaultAsync(item => item.Id == request.CurrentContractVersionId, ct)
            ?? throw new KeyNotFoundException($"No existe ContractVersion Active indicada {request.CurrentContractVersionId}.");
        var currentMapping = await _context.IntegrationMappingSets
            .FirstOrDefaultAsync(item => item.Id == request.CurrentMappingSetId, ct)
            ?? throw new KeyNotFoundException($"No existe MappingSet Published indicado {request.CurrentMappingSetId}.");

        EnsureExpectedRevision(targetContract, request.ExpectedTargetContractRevision);
        EnsureExpectedRevision(targetMapping, request.ExpectedTargetMappingRevision);
        EnsureExpectedRevision(currentContract, request.ExpectedCurrentContractRevision);
        EnsureExpectedRevision(currentMapping, request.ExpectedCurrentMappingRevision);
        EnsureMappingBelongsToContract(targetMapping, targetContract);
        EnsureMappingBelongsToContract(currentMapping, currentContract);

        if (targetContract.MethodId != currentContract.MethodId)
        {
            throw new InvalidOperationException("Rollback exige ContractVersions del mismo método.");
        }

        if (targetContract.Status != IntegrationContractVersionStatusEnum.Retired)
        {
            throw new InvalidOperationException(
                $"Rollback exige una ContractVersion destino Retired. Estado actual: {targetContract.Status}.");
        }

        if (targetMapping.Status != IntegrationMappingSetStatusEnum.Archived)
        {
            throw new InvalidOperationException(
                $"Rollback exige el MappingSet histórico Archived de la versión destino. Estado actual: {targetMapping.Status}.");
        }

        if (currentContract.Status != IntegrationContractVersionStatusEnum.Active)
        {
            throw new InvalidOperationException(
                $"La ContractVersion actual indicada no está Active. Estado actual: {currentContract.Status}.");
        }

        if (currentMapping.Status != IntegrationMappingSetStatusEnum.Published || !currentMapping.IsActive)
        {
            throw new InvalidOperationException(
                "El MappingSet actual indicado debe estar Published y activo para ejecutar rollback.");
        }

        var activeIds = await _context.IntegrationContractVersions
            .AsNoTracking()
            .Where(item => item.MethodId == currentContract.MethodId
                && item.Status == IntegrationContractVersionStatusEnum.Active)
            .Select(item => item.Id)
            .ToListAsync(ct);
        if (activeIds.Count != 1 || activeIds[0] != currentContract.Id)
        {
            throw new InvalidOperationException("El estado Active actual no coincide inequívocamente con la solicitud de rollback.");
        }

        var diff = await CompareAsync(currentContract.Id, targetContract.Id, ct);
        if (diff.Summary.RequiresDevelopment > 0)
        {
            throw new InvalidOperationException(
                $"La ContractVersion histórica ya no es compatible con el runtime actual: {diff.Summary.RequiresDevelopment} cambio(s) RequiresDevelopment.");
        }

        var validation = await _mappingValidationService.EvaluateAsync(targetMapping.Id, includeWarnings: true, ct);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                "La ContractVersion/MappingSet histórica ya no es compatible/completa; rollback rechazado sin modificar el estado vigente.");
        }

        await EnsureRevisionsStillMatchAsync(
            targetContract.Id,
            request.ExpectedTargetContractRevision,
            targetMapping.Id,
            request.ExpectedTargetMappingRevision,
            ct);
        await EnsureRevisionsStillMatchAsync(
            currentContract.Id,
            request.ExpectedCurrentContractRevision,
            currentMapping.Id,
            request.ExpectedCurrentMappingRevision,
            ct);

        var transitionAtUtc = DateTime.UtcNow;
        currentContract.Status = IntegrationContractVersionStatusEnum.Retired;
        currentMapping.Status = IntegrationMappingSetStatusEnum.Archived;
        currentMapping.IsActive = false;
        await _context.SaveChangesAsync(ct);

        await EnsureRevisionsStillMatchAsync(
            targetContract.Id,
            request.ExpectedTargetContractRevision,
            targetMapping.Id,
            request.ExpectedTargetMappingRevision,
            ct);

        targetContract.Status = IntegrationContractVersionStatusEnum.Active;
        targetMapping.Status = IntegrationMappingSetStatusEnum.Published;
        targetMapping.IsActive = true;
        targetMapping.ValidationSummaryJson = SerializeValidationSummary(validation);
        await _context.SaveChangesAsync(ct);

        await AppendMappingHistoryAsync(
            currentMapping,
            "ArchivedByRollback",
            actor,
            new
            {
                PreviousContractVersionId = currentContract.Id,
                TargetContractVersionId = targetContract.Id,
                TargetMappingSetId = targetMapping.Id,
                TransitionedAtUtc = transitionAtUtc
            },
            ct);
        await AppendMappingHistoryAsync(
            targetMapping,
            "ReactivatedByRollback",
            actor,
            new
            {
                TargetContractVersionId = targetContract.Id,
                TargetMappingSetId = targetMapping.Id,
                PreviousContractVersionId = currentContract.Id,
                PreviousMappingSetId = currentMapping.Id,
                ExpectedTargetContractRevision = request.ExpectedTargetContractRevision,
                ExpectedTargetMappingRevision = request.ExpectedTargetMappingRevision,
                ExpectedCurrentContractRevision = request.ExpectedCurrentContractRevision,
                ExpectedCurrentMappingRevision = request.ExpectedCurrentMappingRevision,
                Validation = ValidationEvidence(validation),
                TransitionedAtUtc = transitionAtUtc
            },
            ct);
        await _context.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        var persistedContract = await GetByIdAsync(targetContract.Id, ct)
            ?? throw new InvalidOperationException("No se pudo consultar la ContractVersion restaurada.");
        var persistedMapping = await LoadMappingDtoAsync(targetMapping.Id, ct)
            ?? throw new InvalidOperationException("No se pudo consultar el MappingSet restaurado.");

        return new IntegrationContractMappingTransitionResultDto(
            "Rollback",
            persistedContract,
            persistedMapping,
            currentContract.Id,
            currentMapping.Id,
            validation,
            transitionAtUtc,
            actor);
    }

    public Task<IntegrationContractVersionDto> ActivateAsync(
        Guid id,
        long expectedRevision,
        string activatedBy,
        CancellationToken ct = default)
        => throw new InvalidOperationException(
            "La activación contractual directa está deshabilitada. Use la activación atómica ContractVersion + MappingSet con revisiones esperadas de ambos recursos.");

    public Task<IntegrationContractVersionDto> RetireAsync(
        Guid id,
        long expectedRevision,
        CancellationToken ct = default)
        => throw new InvalidOperationException(
            "El retiro contractual directo está deshabilitado. Active/Retired se transicionan únicamente dentro de activación o rollback atómicos ContractVersion + MappingSet.");

    private async Task<Dictionary<string, IntegrationMethodParameter>> LoadParametersByPathAsync(
        IntegrationContractVersion contract,
        CancellationToken ct)
    {
        var parameters = await GetParameterQuery(contract)
            .Where(parameter => parameter.IsActive)
            .OrderBy(parameter => parameter.SortOrder)
            .ThenBy(parameter => parameter.Id)
            .ToListAsync(ct);

        return parameters
            .GroupBy(parameter => parameter.ParameterPath, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(parameter => parameter.ContractVersionId == contract.Id)
                    .ThenBy(parameter => parameter.Id)
                    .First(),
                StringComparer.Ordinal);
    }

    private IQueryable<IntegrationMethodParameter> GetParameterQuery(IntegrationContractVersion contract)
    {
        var query = _context.IntegrationMethodParameters
            .AsNoTracking()
            .Where(parameter => parameter.MethodId == contract.MethodId);

        return IsBaselineBridge(contract)
            ? query.Where(parameter => parameter.ContractVersionId == contract.Id || parameter.ContractVersionId == null)
            : query.Where(parameter => parameter.ContractVersionId == contract.Id);
    }

    private static bool IsBaselineBridge(IntegrationContractVersion contract)
        => contract.SourceType == IntegrationContractSourceTypeEnum.BaselineMigration
           && string.Equals(contract.VersionLabel, "baseline-v1", StringComparison.Ordinal);

    private static void AddModifiedParameterChanges(
        ICollection<IntegrationContractDiffChangeDto> changes,
        IntegrationMethodParameter left,
        IntegrationMethodParameter right)
    {
        var impact = ImpactForTarget(right);

        if (left.Required != right.Required
            || left.Nillable != right.Nillable
            || left.MinOccurs != right.MinOccurs)
        {
            changes.Add(Change(
                IntegrationContractChangeTypeEnum.RequiredChanged,
                right,
                impact,
                $"required={left.Required}; nillable={left.Nillable}; min={left.MinOccurs}",
                $"required={right.Required}; nillable={right.Nillable}; min={right.MinOccurs}"));
        }

        if (!string.Equals(left.DataType, right.DataType, StringComparison.Ordinal)
            || !string.Equals(left.XsdType, right.XsdType, StringComparison.Ordinal)
            || !string.Equals(left.SupportStatus, right.SupportStatus, StringComparison.Ordinal))
        {
            changes.Add(Change(
                IntegrationContractChangeTypeEnum.TypeChanged,
                right,
                impact,
                $"{left.DataType}|{left.XsdType}|{left.SupportStatus}",
                $"{right.DataType}|{right.XsdType}|{right.SupportStatus}"));
        }

        if (left.Cardinality != right.Cardinality || left.MaxOccurs != right.MaxOccurs)
        {
            changes.Add(Change(
                IntegrationContractChangeTypeEnum.CardinalityChanged,
                right,
                impact,
                $"{left.Cardinality}|max={left.MaxOccurs?.ToString() ?? "unbounded"}",
                $"{right.Cardinality}|max={right.MaxOccurs?.ToString() ?? "unbounded"}"));
        }

        if (left.SortOrder != right.SortOrder)
        {
            changes.Add(Change(
                IntegrationContractChangeTypeEnum.OrderChanged,
                right,
                impact,
                left.SortOrder.ToString(),
                right.SortOrder.ToString()));
        }

        if (left.Direction != right.Direction)
        {
            changes.Add(Change(
                IntegrationContractChangeTypeEnum.DirectionChanged,
                right,
                impact,
                left.Direction.ToString(),
                right.Direction.ToString()));
        }
    }

    private static IntegrationContractDiffChangeDto Change(
        IntegrationContractChangeTypeEnum type,
        IntegrationMethodParameter parameter,
        IntegrationContractDiffImpactEnum impact,
        string? leftValue,
        string? rightValue)
        => new(
            type,
            parameter.ParameterPath,
            string.IsNullOrWhiteSpace(parameter.DisplayName) ? parameter.ParameterPath : parameter.DisplayName,
            impact,
            leftValue,
            rightValue);

    private static IntegrationContractDiffImpactEnum ImpactForTarget(IntegrationMethodParameter parameter)
        => parameter.Cardinality != IntegrationParameterCardinalityEnum.Scalar
           || parameter.Direction != IntegrationParameterDirectionEnum.Input
           || !string.Equals(parameter.SupportStatus, SupportedParameterStatus, StringComparison.OrdinalIgnoreCase)
            ? IntegrationContractDiffImpactEnum.RequiresDevelopment
            : IntegrationContractDiffImpactEnum.MappingRequired;

    private static string DescribeParameter(IntegrationMethodParameter parameter)
        => $"{parameter.DataType}|{parameter.XsdType}|{parameter.Cardinality}|{parameter.Direction}|required={parameter.Required}|support={parameter.SupportStatus}";

    private static IntegrationMethodParameter CloneParameter(IntegrationMethodParameter source, Guid contractVersionId)
        => new()
        {
            MethodId = source.MethodId,
            ContractVersionId = contractVersionId,
            ParameterPath = source.ParameterPath,
            XmlElementName = source.XmlElementName,
            DisplayName = source.DisplayName,
            DescriptionEs = source.DescriptionEs,
            Category = source.Category,
            ExampleValue = source.ExampleValue,
            UiHelpText = source.UiHelpText,
            DataType = source.DataType,
            XsdType = source.XsdType,
            Nillable = source.Nillable,
            MinOccurs = source.MinOccurs,
            MaxOccurs = source.MaxOccurs,
            SupportStatus = source.SupportStatus,
            Direction = source.Direction,
            Cardinality = source.Cardinality,
            Required = source.Required,
            SortOrder = source.SortOrder,
            IsActive = source.IsActive
        };

    private static void EnsureExpectedRevision(IntegrationContractVersion contract, long expectedRevision)
    {
        if (contract.Revision != expectedRevision)
        {
            throw new DbUpdateConcurrencyException(
                $"ContractVersion {contract.Id} cambió de revisión. Esperada={expectedRevision}, actual={contract.Revision}.");
        }
    }

    private static void EnsureExpectedRevision(IntegrationMappingSet mapping, long expectedRevision)
    {
        if (mapping.Revision != expectedRevision)
        {
            throw new DbUpdateConcurrencyException(
                $"MappingSet {mapping.Id} cambió de revisión. Esperada={expectedRevision}, actual={mapping.Revision}.");
        }
    }

    private static void EnsureMappingBelongsToContract(
        IntegrationMappingSet mapping,
        IntegrationContractVersion contract)
    {
        if (mapping.MethodId != contract.MethodId || mapping.ContractVersionId != contract.Id)
        {
            throw new InvalidOperationException(
                $"MappingSet {mapping.Id} no pertenece exactamente a ContractVersion {contract.Id}.");
        }
    }

    private async Task<IntegrationMappingSet> LoadPublishedMappingForContractAsync(
        IntegrationContractVersion contract,
        CancellationToken ct)
    {
        var query = _context.IntegrationMappingSets
            .Where(item => item.MethodId == contract.MethodId
                && item.Status == IntegrationMappingSetStatusEnum.Published);

        query = IsBaselineBridge(contract)
            ? query.Where(item => item.ContractVersionId == contract.Id || item.ContractVersionId == null)
            : query.Where(item => item.ContractVersionId == contract.Id);

        var mappings = await query.ToListAsync(ct);
        if (mappings.Count != 1)
        {
            throw new InvalidOperationException(
                $"ContractVersion Active {contract.Id} debe resolver exactamente un MappingSet Published; encontrados={mappings.Count}.");
        }

        return mappings[0];
    }

    private async Task EnsureRevisionsStillMatchAsync(
        Guid contractVersionId,
        long expectedContractRevision,
        Guid mappingSetId,
        long expectedMappingRevision,
        CancellationToken ct)
    {
        var contractRevision = await _context.IntegrationContractVersions
            .AsNoTracking()
            .Where(item => item.Id == contractVersionId)
            .Select(item => item.Revision)
            .SingleAsync(ct);
        if (contractRevision != expectedContractRevision)
        {
            throw new DbUpdateConcurrencyException(
                $"ContractVersion {contractVersionId} cambió durante la transición. Esperada={expectedContractRevision}, actual={contractRevision}.");
        }

        var mappingRevision = await _context.IntegrationMappingSets
            .AsNoTracking()
            .Where(item => item.Id == mappingSetId)
            .Select(item => item.Revision)
            .SingleAsync(ct);
        if (mappingRevision != expectedMappingRevision)
        {
            throw new DbUpdateConcurrencyException(
                $"MappingSet {mappingSetId} cambió durante la transición. Esperada={expectedMappingRevision}, actual={mappingRevision}.");
        }
    }

    private async Task AppendMappingHistoryAsync(
        IntegrationMappingSet mapping,
        string action,
        string actor,
        object evidence,
        CancellationToken ct)
    {
        var snapshot = await _mappingSnapshotBuilder.BuildAsync(mapping.Id, ct);
        var snapshotJson = JsonSerializer.Serialize(new
        {
            MappingSnapshotJson = snapshot.SnapshotJson,
            Evidence = evidence
        });
        var snapshotHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshotJson)));

        _context.IntegrationMappingSetHistory.Add(new IntegrationMappingSetHistory
        {
            MappingSetId = mapping.Id,
            MethodId = mapping.MethodId,
            Version = mapping.Version,
            Status = mapping.Status,
            Action = action,
            PerformedBy = actor,
            PerformedAtUtc = DateTime.UtcNow,
            SnapshotJson = snapshotJson,
            SnapshotHash = snapshotHash
        });
    }

    private async Task<IntegrationMappingSetDto?> LoadMappingDtoAsync(Guid id, CancellationToken ct)
    {
        var mapping = await _context.IntegrationMappingSets
            .AsNoTracking()
            .Include(item => item.Method)
            .Include(item => item.Rules)
            .FirstOrDefaultAsync(item => item.Id == id, ct);
        return mapping is null ? null : MapMappingSet(mapping);
    }

    private static string SerializeValidationSummary(IntegrationMappingValidationResultDto validation)
        => JsonSerializer.Serialize(new
        {
            isValid = validation.IsValid,
            coverage = validation.Coverage,
            issues = validation.Issues,
            parameters = validation.Parameters
        });

    private static object ValidationEvidence(IntegrationMappingValidationResultDto validation)
        => new
        {
            validation.IsValid,
            validation.Coverage.TotalParameters,
            validation.Coverage.ValidParameters,
            validation.Coverage.IncompleteParameters,
            validation.Coverage.InvalidParameters,
            ErrorCodes = validation.Issues
                .Where(issue => issue.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase))
                .Select(issue => issue.Code)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(code => code, StringComparer.Ordinal)
                .ToArray()
        };

    private static IntegrationMappingSetDto MapMappingSet(IntegrationMappingSet mapping)
        => new(
            mapping.Id,
            mapping.MethodId,
            mapping.Method?.Code ?? string.Empty,
            mapping.ContractVersionId,
            mapping.Revision,
            mapping.Name,
            mapping.Version,
            mapping.Status,
            mapping.IsActive,
            mapping.Notes,
            mapping.PublishedAtUtc,
            mapping.PublishedBy,
            mapping.Rules
                .OrderBy(rule => rule.ParameterId)
                .ThenBy(rule => rule.Priority)
                .Select(rule => new IntegrationMappingRuleDto(
                    rule.Id,
                    rule.MappingSetId,
                    rule.MethodId,
                    rule.ParameterId,
                    rule.SourceKind,
                    rule.SourceCatalogFieldId,
                    rule.SourceFieldPath,
                    rule.FixedValue,
                    rule.DefaultValue,
                    rule.TransformationCode,
                    rule.FormatMask,
                    rule.Priority,
                    rule.RequiredOverride,
                    rule.Enabled,
                    rule.ConditionExpression))
                .ToList());

    private async Task EnsureActiveMethodAsync(int methodId, CancellationToken ct)
    {
        var exists = await _context.IntegrationMethods
            .AsNoTracking()
            .AnyAsync(method => method.Id == methodId && method.IsActive, ct);
        if (!exists)
        {
            throw new KeyNotFoundException($"No existe método de integración activo {methodId}.");
        }
    }

    private async Task EnsureVersionLabelAvailableAsync(int methodId, string versionLabel, CancellationToken ct)
    {
        if (await _context.IntegrationContractVersions
            .AsNoTracking()
            .AnyAsync(contract => contract.MethodId == methodId && contract.VersionLabel == versionLabel, ct))
        {
            throw new InvalidOperationException($"Ya existe la versión contractual '{versionLabel}' para MethodId={methodId}.");
        }
    }

    private async Task<IntegrationContractVersion> GetForLifecycleUpdateAsync(
        Guid id,
        long expectedRevision,
        CancellationToken ct)
    {
        var contract = await _context.IntegrationContractVersions
            .FirstOrDefaultAsync(item => item.Id == id, ct)
            ?? throw new KeyNotFoundException($"No existe ContractVersion {id}.");

        if (contract.Revision != expectedRevision)
        {
            throw new DbUpdateConcurrencyException(
                $"ContractVersion {id} cambió de revisión. Esperada={expectedRevision}, actual={contract.Revision}.");
        }

        return contract;
    }

    private async Task EnsureBaselineAsync(CancellationToken ct)
        => await new IntegrationCatalogBootstrapper(_context).EnsureAsync(ct);

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

    private static IntegrationContractVersionDto Map(IntegrationContractVersion contract)
        => new(
            contract.Id,
            contract.MethodId,
            contract.VersionLabel,
            contract.Status,
            contract.OperationName,
            contract.TargetNamespace,
            contract.SoapAction,
            contract.SourceType,
            contract.SourceDocumentName,
            contract.SourceDocumentHash,
            contract.SourceReference,
            contract.Notes,
            contract.Revision,
            contract.ImportedAtUtc,
            contract.ImportedBy,
            contract.ActivatedAtUtc,
            contract.ActivatedBy);

    private static IntegrationMethodParameterDto MapParameter(IntegrationMethodParameter parameter)
        => new(
            parameter.Id,
            parameter.MethodId,
            parameter.ParameterPath,
            parameter.DisplayName,
            parameter.DescriptionEs,
            parameter.Category,
            parameter.ExampleValue,
            parameter.UiHelpText,
            parameter.DataType,
            parameter.Direction,
            parameter.Cardinality,
            parameter.Required,
            parameter.SortOrder,
            parameter.IsActive);

    private static IntegrationContractParameterDto MapContractParameter(IntegrationMethodParameter parameter)
        => new(
            parameter.Id,
            parameter.ContractVersionId,
            parameter.MethodId,
            parameter.ParameterPath,
            parameter.XmlElementName,
            parameter.DisplayName,
            parameter.DescriptionEs,
            parameter.UiHelpText,
            parameter.Category,
            parameter.ExampleValue,
            parameter.DataType,
            parameter.XsdType,
            parameter.Direction,
            parameter.Cardinality,
            parameter.Required,
            parameter.Nillable,
            parameter.MinOccurs,
            parameter.MaxOccurs,
            parameter.SupportStatus,
            parameter.SortOrder,
            parameter.IsActive);
}
