using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaConfigProfileCommandService : INachaConfigProfileCommandService
{
    private readonly AchDbContext _context;
    private readonly INachaConfigProfileQueryService _query;

    public NachaConfigProfileCommandService(AchDbContext context, INachaConfigProfileQueryService query)
    {
        _context = context;
        _query = query;
    }

    public async Task<NachaConfigProfileDetailDto> CreateDraftAsync(NachaConfigCreateDraftRequest request, string actor, CancellationToken ct = default)
    {
        var profile = new CfgProfile
        {
            ProfileCode = request.ProfileCode.Trim(),
            NameEs = request.NombreEs.Trim(),
            Description = request.Descripcion,
            ClearingHouseId = await ResolveCatalogIdAsync<CatClearingHouse>(request.CamaraCode, ct),
            FlowTypeId = await ResolveCatalogIdAsync<CatFlowType>(request.FlujoCode, ct),
            DirectionId = await ResolveCatalogIdAsync<CatDirection>(request.DireccionCode, ct),
            ServiceClassId = string.IsNullOrWhiteSpace(request.ServicioCode)
                ? null
                : await ResolveCatalogIdAsync<CatServiceClass>(request.ServicioCode, ct),
            StatusId = await ResolveCatalogIdAsync<CatConfigStatus>("BORRADOR", ct),
            EffectiveFrom = request.EffectiveFrom,
            ContextPriority = 100,
            VersionMajor = 1,
            VersionMinor = 0
        };

        _context.CfgProfiles.Add(profile);
        await _context.SaveChangesAsync(ct);
        await AppendChangeAsync(profile.Id, nameof(CfgProfile), profile.Id.ToString(), "DRAFT_CREATE", actor, null, profile, ct);

        return await _query.GetProfileDetailAsync(profile.Id, ct)
               ?? throw new InvalidOperationException("No se pudo cargar el perfil recién creado.");
    }

    public async Task<NachaConfigProfileDetailDto?> UpdateDraftAsync(int profileId, NachaConfigUpdateProfileRequest request, string actor, CancellationToken ct = default)
    {
        var profile = await _context.CfgProfiles.Include(x => x.Status).FirstOrDefaultAsync(x => x.Id == profileId, ct);
        if (profile is null)
        {
            return null;
        }

        if (!string.Equals(profile.Status.Code, "BORRADOR", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Solo se puede editar perfiles en estado BORRADOR.");
        }

        var before = JsonSerializer.Serialize(profile);
        profile.NameEs = request.NombreEs.Trim();
        profile.Description = request.Descripcion;
        profile.ContextPriority = request.ContextPriority;
        profile.EffectiveFrom = request.EffectiveFrom;
        profile.EffectiveTo = request.EffectiveTo;
        await _context.SaveChangesAsync(ct);

        await AppendChangeAsync(profile.Id, nameof(CfgProfile), profile.Id.ToString(), "DRAFT_UPDATE", actor, before, profile, ct);
        return await _query.GetProfileDetailAsync(profileId, ct);
    }

    public async Task<NachaConfigProfileDetailDto?> CloneProfileAsync(int profileId, NachaConfigCloneProfileRequest request, string actor, CancellationToken ct = default)
    {
        var source = await _context.CfgProfiles
            .Include(x => x.Records)
            .Include(x => x.LayoutVariants)
                .ThenInclude(v => v.Fields)
            .FirstOrDefaultAsync(x => x.Id == profileId, ct);

        if (source is null)
        {
            return null;
        }

        var draftStatusId = await ResolveCatalogIdAsync<CatConfigStatus>("BORRADOR", ct);
        var clone = new CfgProfile
        {
            ProfileCode = request.NuevoProfileCode.Trim(),
            NameEs = request.NuevoNombreEs.Trim(),
            Description = source.Description,
            ClearingHouseId = source.ClearingHouseId,
            FlowTypeId = source.FlowTypeId,
            DirectionId = source.DirectionId,
            ServiceClassId = source.ServiceClassId,
            ContextPriority = source.ContextPriority,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = source.EffectiveTo,
            StatusId = draftStatusId,
            VersionMajor = source.VersionMajor + 1,
            VersionMinor = 0,
            SupersedesProfileId = source.Id
        };

        _context.CfgProfiles.Add(clone);
        await _context.SaveChangesAsync(ct);

        foreach (var record in source.Records)
        {
            _context.CfgProfileRecords.Add(new CfgProfileRecord
            {
                ProfileId = clone.Id,
                RecordCodeId = record.RecordCodeId,
                Sequence = record.Sequence,
                IsEnabled = record.IsEnabled,
                MinOccurs = record.MinOccurs,
                MaxOccurs = record.MaxOccurs,
                SourceStrategy = record.SourceStrategy,
                SemanticRuleSetId = record.SemanticRuleSetId
            });
        }

        await _context.SaveChangesAsync(ct);
        await AppendChangeAsync(clone.Id, nameof(CfgProfile), clone.Id.ToString(), "CLONE", actor, null, clone, ct);
        return await _query.GetProfileDetailAsync(clone.Id, ct);
    }

    public async Task<bool> InactivateProfileAsync(int profileId, string actor, CancellationToken ct = default)
    {
        return await ChangeStatusAsync(profileId, "INACTIVO", "INACTIVATE", actor, ct);
    }

    public async Task<bool> ArchiveProfileAsync(int profileId, string actor, CancellationToken ct = default)
    {
        return await ChangeStatusAsync(profileId, "ARCHIVADO", "ARCHIVE", actor, ct);
    }

    public async Task<bool> UpdateRecordSequenceAsync(int profileId, IReadOnlyList<NachaConfigProfileRecordSequenceDto> request, string actor, CancellationToken ct = default)
    {
        var records = await _context.CfgProfileRecords.Where(x => x.ProfileId == profileId).ToListAsync(ct);
        if (records.Count == 0)
        {
            return false;
        }

        var map = request.ToDictionary(x => x.ProfileRecordId, x => x.Sequence);
        foreach (var record in records)
        {
            if (map.TryGetValue(record.Id, out var sequence))
            {
                record.Sequence = sequence;
            }
        }

        await _context.SaveChangesAsync(ct);
        await AppendChangeAsync(profileId, nameof(CfgProfileRecord), profileId.ToString(), "RECORD_SEQUENCE_UPDATE", actor, null, request, ct);
        return true;
    }

    public async Task<bool> UpdateLayoutVariantAsync(int profileId, int variantId, NachaConfigLayoutVariantEditDto request, string actor, CancellationToken ct = default)
    {
        var variant = await _context.CfgLayoutVariants.FirstOrDefaultAsync(x => x.Id == variantId && x.ProfileId == profileId, ct);
        if (variant is null)
        {
            return false;
        }

        variant.NameEs = request.NombreEs.Trim();
        variant.Description = request.Descripcion;
        variant.Priority = request.Priority;
        variant.IsDefaultForRecord = request.IsDefaultForRecord;
        variant.EffectiveFrom = request.EffectiveFrom;
        variant.EffectiveTo = request.EffectiveTo;
        await _context.SaveChangesAsync(ct);
        await AppendChangeAsync(profileId, nameof(CfgLayoutVariant), variant.Id.ToString(), "VARIANT_UPDATE", actor, null, variant, ct);
        return true;
    }

    public async Task<bool> UpdateLayoutFieldAsync(int profileId, int fieldId, NachaConfigLayoutFieldEditDto request, string actor, CancellationToken ct = default)
    {
        var field = await _context.CfgLayoutFields
            .Include(x => x.LayoutVariant)
            .Include(x => x.SourceDefinition)
            .FirstOrDefaultAsync(x => x.Id == fieldId && x.LayoutVariant.ProfileId == profileId, ct);

        if (field is null)
        {
            return false;
        }

        field.FieldNameEs = request.FieldNameEs.Trim();
        field.StartPosition = request.StartPosition;
        field.Length = request.Length;
        field.IsEnabled = request.IsEnabled;
        if (!string.IsNullOrWhiteSpace(request.PropertyPath))
        {
            field.SourceDefinition.PropertyPath = request.PropertyPath.Trim();
        }

        await _context.SaveChangesAsync(ct);
        await AppendChangeAsync(profileId, nameof(CfgLayoutField), field.Id.ToString(), "FIELD_UPDATE", actor, null, field, ct);
        return true;
    }

    public async Task<bool> UpdateFieldRuleAsync(int profileId, int ruleId, NachaConfigFieldRuleEditDto request, string actor, CancellationToken ct = default)
    {
        var rule = await _context.CfgFieldRules
            .Include(x => x.LayoutField)
                .ThenInclude(x => x.LayoutVariant)
            .FirstOrDefaultAsync(x => x.Id == ruleId && x.LayoutField.LayoutVariant.ProfileId == profileId, ct);

        if (rule is null)
        {
            return false;
        }

        rule.ErrorCode = request.ErrorCode.Trim();
        rule.ErrorMessageEs = request.ErrorMessageEs.Trim();
        rule.Severity = request.Severity.Trim().ToUpperInvariant();
        rule.IsEnabled = request.IsEnabled;
        await _context.SaveChangesAsync(ct);
        await AppendChangeAsync(profileId, nameof(CfgFieldRule), rule.Id.ToString(), "RULE_UPDATE", actor, null, rule, ct);
        return true;
    }

    private async Task<bool> ChangeStatusAsync(int profileId, string statusCode, string changeType, string actor, CancellationToken ct)
    {
        var profile = await _context.CfgProfiles.FirstOrDefaultAsync(x => x.Id == profileId, ct);
        if (profile is null)
        {
            return false;
        }

        profile.StatusId = await ResolveCatalogIdAsync<CatConfigStatus>(statusCode, ct);
        await _context.SaveChangesAsync(ct);
        await AppendChangeAsync(profileId, nameof(CfgProfile), profileId.ToString(), changeType, actor, null, new { statusCode }, ct);
        return true;
    }

    private async Task<int> ResolveCatalogIdAsync<TCatalog>(string code, CancellationToken ct)
        where TCatalog : class
    {
        var entity = await _context.Set<TCatalog>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => EF.Property<string>(x, "Code") == code, ct)
            ?? throw new InvalidOperationException($"No se encontró catálogo {typeof(TCatalog).Name} código {code}.");

        return EF.Property<int>(entity, "Id");
    }

    private async Task AppendChangeAsync(int profileId, string entityName, string entityId, string changeType, string actor, object? before, object? after, CancellationToken ct)
    {
        _context.HistConfigChanges.Add(new HistConfigChange
        {
            ProfileId = profileId,
            EntityName = entityName,
            EntityId = entityId,
            ChangeType = changeType,
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before),
            AfterJson = after is null ? null : JsonSerializer.Serialize(after),
            ChangedAtUtc = DateTime.UtcNow,
            ChangedBy = string.IsNullOrWhiteSpace(actor) ? "system" : actor,
            CorrelationId = $"NACHA-CONFIG-{DateTime.UtcNow:yyyyMMddHHmmssfff}"
        });
        await _context.SaveChangesAsync(ct);
    }
}
