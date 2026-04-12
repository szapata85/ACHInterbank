using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Integrations.Services;

[Scoped]
public class IntegrationMappingSetService : IIntegrationMappingSetService
{
    private readonly AchDbContext _context;
    private readonly IIntegrationMappingValidationService _validationService;
    private readonly IIntegrationMappingPreviewService _previewService;

    public IntegrationMappingSetService(
        AchDbContext context,
        IIntegrationMappingValidationService validationService,
        IIntegrationMappingPreviewService previewService)
    {
        _context = context;
        _validationService = validationService;
        _previewService = previewService;
    }

    public async Task<IReadOnlyCollection<IntegrationMappingSetDto>> GetByMethodAsync(int? methodId, CancellationToken ct = default)
    {
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

    public async Task<IntegrationMappingSetDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _context.Set<IntegrationMappingSet>()
            .AsNoTracking()
            .Include(x => x.Method)
            .Include(x => x.Rules)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return item is null ? null : MapToDto(item);
    }

    public async Task<IntegrationMappingSetDto?> GetPublishedByMethodAsync(int methodId, CancellationToken ct = default)
    {
        var item = await _context.Set<IntegrationMappingSet>()
            .AsNoTracking()
            .Include(x => x.Method)
            .Include(x => x.Rules)
            .Where(x => x.MethodId == methodId && x.Status == IntegrationMappingSetStatusEnum.Published)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(ct);

        return item is null ? null : MapToDto(item);
    }

    public async Task<IntegrationMappingSetDto> CreateDraftAsync(CreateIntegrationMappingSetRequest request, CancellationToken ct = default)
    {
        var method = await _context.Set<IntegrationMethod>()
            .FirstOrDefaultAsync(x => x.Id == request.MethodId && x.IsActive, ct)
            ?? throw new KeyNotFoundException($"No existe método {request.MethodId}.");

        var set = new IntegrationMappingSet
        {
            MethodId = method.Id,
            Name = request.Name.Trim(),
            Notes = request.Notes?.Trim() ?? string.Empty,
            Status = IntegrationMappingSetStatusEnum.Draft,
            IsActive = true,
            Version = 0,
            CreatedBy = request.CreatedBy
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
        set.UpdatedBy = request.UpdatedBy;

        await _context.SaveChangesAsync(ct);
        await AppendHistoryAsync(set, "UpdatedDraft", request.UpdatedBy, ct);
        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(set.Id, ct) ?? throw new InvalidOperationException("No se pudo actualizar MappingSet.");
    }

    public async Task<IntegrationMappingSetDto> UpsertRulesAsync(Guid id, UpsertIntegrationMappingRulesRequest request, CancellationToken ct = default)
    {
        var set = await GetDraftForUpdateAsync(id, ct);
        var parameterIds = request.Rules.Select(x => x.ParameterId).Distinct().ToArray();

        var validParameterIds = await _context.Set<IntegrationMethodParameter>()
            .Where(x => x.MethodId == set.MethodId && parameterIds.Contains(x.Id) && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(ct);

        if (validParameterIds.Count != parameterIds.Length)
        {
            throw new InvalidOperationException("Uno o más ParameterId no son válidos para el método del MappingSet.");
        }

        foreach (var rule in request.Rules)
        {
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
                    MethodId = set.MethodId,
                    CreatedBy = request.UpdatedBy
                };
                _context.Set<IntegrationMappingRule>().Add(entity);
            }

            entity.MethodId = set.MethodId;
            entity.ParameterId = rule.ParameterId;
            entity.SourceKind = rule.SourceKind;
            entity.SourceCatalogFieldId = rule.SourceCatalogFieldId;
            entity.SourceFieldPath = rule.SourceFieldPath?.Trim() ?? string.Empty;
            entity.FixedValue = string.IsNullOrWhiteSpace(rule.FixedValue) ? null : rule.FixedValue.Trim();
            entity.DefaultValue = string.IsNullOrWhiteSpace(rule.DefaultValue) ? null : rule.DefaultValue.Trim();
            entity.TransformationCode = string.IsNullOrWhiteSpace(rule.TransformationCode) ? null : rule.TransformationCode.Trim();
            entity.FormatMask = string.IsNullOrWhiteSpace(rule.FormatMask) ? null : rule.FormatMask.Trim();
            entity.Priority = Math.Max(1, rule.Priority);
            entity.RequiredOverride = rule.RequiredOverride;
            entity.Enabled = rule.Enabled;
            entity.ConditionExpression = string.IsNullOrWhiteSpace(rule.ConditionExpression) ? null : rule.ConditionExpression.Trim();
            entity.UpdatedBy = request.UpdatedBy;
        }

        await _context.SaveChangesAsync(ct);
        await AppendHistoryAsync(set, "RulesUpserted", request.UpdatedBy, ct);
        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(set.Id, ct) ?? throw new InvalidOperationException("No se pudo actualizar reglas.");
    }

    public Task<IntegrationMappingValidationResultDto> ValidateAsync(Guid id, ValidateIntegrationMappingSetRequest request, CancellationToken ct = default)
        => _validationService.ValidateAsync(id, request.IncludeWarnings, ct);

    public Task<IntegrationMappingPreviewResultDto> PreviewAsync(Guid id, PreviewIntegrationMappingSetRequest request, CancellationToken ct = default)
        => _previewService.PreviewAsync(id, request, ct);

    public async Task<IntegrationMappingSetDto> PublishAsync(Guid id, PublishIntegrationMappingSetRequest request, CancellationToken ct = default)
    {
        var set = await GetDraftForUpdateAsync(id, ct);

        var validation = await _validationService.ValidateAsync(id, includeWarnings: true, ct);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException("El MappingSet no es publicable. Revisar validaciones obligatorias.");
        }

        var currentPublished = await _context.Set<IntegrationMappingSet>()
            .Where(x => x.MethodId == set.MethodId && x.Status == IntegrationMappingSetStatusEnum.Published)
            .ToListAsync(ct);

        foreach (var published in currentPublished)
        {
            published.Status = IntegrationMappingSetStatusEnum.Archived;
            published.IsActive = false;
            published.UpdatedBy = request.PublishedBy;
            await AppendHistoryAsync(published, "ArchivedByNewPublication", request.PublishedBy, ct);
        }

        var nextVersion = await _context.Set<IntegrationMappingSet>()
            .Where(x => x.MethodId == set.MethodId)
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
        set.UpdatedBy = request.PublishedBy;

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
            Name = string.IsNullOrWhiteSpace(request.NewName) ? $"{source.Name} (clon)" : request.NewName.Trim(),
            Notes = source.Notes,
            Status = IntegrationMappingSetStatusEnum.Draft,
            Version = 0,
            IsActive = true,
            CreatedBy = request.ClonedBy
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
            ConditionExpression = r.ConditionExpression,
            CreatedBy = request.ClonedBy
        });

        _context.Set<IntegrationMappingRule>().AddRange(cloneRules);
        await AppendHistoryAsync(clone, "Cloned", request.ClonedBy, ct);
        await _context.SaveChangesAsync(ct);

        return await GetByIdAsync(clone.Id, ct) ?? throw new InvalidOperationException("No se pudo clonar MappingSet.");
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

    private async Task AppendHistoryAsync(IntegrationMappingSet set, string action, string actor, CancellationToken ct)
    {
        var snapshot = await BuildSnapshotAsync(set.Id, ct);
        var history = new IntegrationMappingSetHistory
        {
            MappingSetId = set.Id,
            MethodId = set.MethodId,
            Version = set.Version,
            Status = set.Status,
            Action = action,
            PerformedBy = string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim(),
            PerformedAtUtc = DateTime.UtcNow,
            SnapshotJson = snapshot,
            SnapshotHash = ComputeSha256(snapshot),
            CreatedBy = actor
        };

        _context.Set<IntegrationMappingSetHistory>().Add(history);
    }

    private async Task<string> BuildSnapshotAsync(Guid mappingSetId, CancellationToken ct)
    {
        var set = await _context.Set<IntegrationMappingSet>()
            .AsNoTracking()
            .Include(x => x.Rules)
            .FirstAsync(x => x.Id == mappingSetId, ct);

        return JsonSerializer.Serialize(new
        {
            set.Id,
            set.MethodId,
            set.Name,
            set.Version,
            set.Status,
            set.IsActive,
            set.Notes,
            set.PublishedAtUtc,
            set.PublishedBy,
            Rules = set.Rules
                .OrderBy(r => r.ParameterId)
                .ThenBy(r => r.Priority)
                .Select(r => new
                {
                    r.Id,
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
                    r.ConditionExpression
                })
        });
    }

    private static string ComputeSha256(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static IntegrationMappingSetDto MapToDto(IntegrationMappingSet x)
        => new(
            x.Id,
            x.MethodId,
            x.Method?.Code ?? string.Empty,
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
