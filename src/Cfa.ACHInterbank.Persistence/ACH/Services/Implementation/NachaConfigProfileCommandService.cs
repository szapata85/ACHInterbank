using System.Text.Json;
using System.Text.Json.Serialization;
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
    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    private readonly AchDbContext _context;
    private readonly INachaConfigProfileQueryService _query;

    public NachaConfigProfileCommandService(AchDbContext context, INachaConfigProfileQueryService query)
    {
        _context = context;
        _query = query;
    }

    public async Task<NachaConfigProfileDetailDto> CreateDraftAsync(NachaConfigCreateDraftRequest request, string actor, CancellationToken ct = default)
    {
        ValidateCreateRequest(request);
        var createdId = await ExecuteInTransactionAsync(async () =>
        {
            var profileCode = request.ProfileCode.Trim().ToUpperInvariant();
            if (await _context.CfgProfiles.AnyAsync(x => x.ProfileCode == profileCode, ct))
            {
                throw new NachaConfigException(
                    "PROFILE_CODE_ALREADY_EXISTS",
                    $"Ya existe un perfil con el código {profileCode}.",
                    409);
            }

            var profile = new CfgProfile
            {
                ProfileCode = profileCode,
                NameEs = request.NombreEs.Trim(),
                Description = request.Descripcion?.Trim(),
                ClearingHouseId = await ResolveCatalogIdAsync<CatClearingHouse>(request.CamaraCode.Trim().ToUpperInvariant(), ct),
                FlowTypeId = await ResolveCatalogIdAsync<CatFlowType>(request.FlujoCode.Trim().ToUpperInvariant(), ct),
                DirectionId = await ResolveCatalogIdAsync<CatDirection>(request.DireccionCode.Trim().ToUpperInvariant(), ct),
                ServiceClassId = string.IsNullOrWhiteSpace(request.ServicioCode)
                    ? null
                    : await ResolveCatalogIdAsync<CatServiceClass>(request.ServicioCode.Trim().ToUpperInvariant(), ct),
                StatusId = await ResolveCatalogIdAsync<CatConfigStatus>("BORRADOR", ct),
                EffectiveFrom = request.EffectiveFrom,
                ContextPriority = 100,
                VersionMajor = 1,
                VersionMinor = 0
            };

            _context.CfgProfiles.Add(profile);
            await _context.SaveChangesAsync(ct);
            AppendChange(profile.Id, nameof(CfgProfile), profile.Id.ToString(), "DRAFT_CREATE", actor, null, profile);
            await _context.SaveChangesAsync(ct);
            return profile.Id;
        }, ct);

        return await _query.GetProfileDetailAsync(createdId, ct)
               ?? throw new InvalidOperationException("No se pudo cargar el perfil recién creado.");
    }

    public async Task<NachaConfigProfileDetailDto?> UpdateDraftAsync(int profileId, NachaConfigUpdateProfileRequest request, string actor, CancellationToken ct = default)
    {
        ValidateEffectiveDates(request.EffectiveFrom, request.EffectiveTo);
        var exists = false;
        await ExecuteInTransactionAsync(async () =>
        {
            var profile = await _context.CfgProfiles.Include(x => x.Status).FirstOrDefaultAsync(x => x.Id == profileId, ct);
            if (profile is null)
            {
                exists = false;
                return;
            }

            exists = true;
            EnsureExpectedRowVersion(profile, request.ExpectedRowVersion);

            if (!string.Equals(profile.Status.Code, "BORRADOR", StringComparison.OrdinalIgnoreCase))
            {
                throw new NachaConfigException("INVALID_PROFILE_STATE", "Solo se puede editar perfiles en estado BORRADOR.", 409, Convert.ToBase64String(profile.RowVersion));
            }

            var before = JsonSerializer.Serialize(profile);
            profile.NameEs = request.NombreEs.Trim();
            profile.Description = request.Descripcion;
            profile.ContextPriority = request.ContextPriority;
            profile.EffectiveFrom = request.EffectiveFrom;
            profile.EffectiveTo = request.EffectiveTo;
            profile.UpdatedAt = DateTimeOffset.UtcNow;

            AppendChange(profile.Id, nameof(CfgProfile), profile.Id.ToString(), "DRAFT_UPDATE", actor, before, profile);
            await _context.SaveChangesAsync(ct);
        }, ct);

        return exists ? await _query.GetProfileDetailAsync(profileId, ct) : null;
    }

    public async Task<NachaConfigProfileDetailDto?> CloneProfileAsync(int profileId, NachaConfigCloneProfileRequest request, string actor, CancellationToken ct = default)
    {
        if (request.EffectiveFrom == default)
        {
            throw new NachaConfigException("EFFECTIVE_FROM_REQUIRED", "La vigencia inicial es obligatoria.", 400);
        }

        var cloneId = 0;
        await ExecuteInTransactionAsync(async () =>
        {
            var source = await _context.CfgProfiles
                .Include(x => x.Records)
                .Include(x => x.LayoutVariants)
                    .ThenInclude(v => v.Fields)
                .FirstOrDefaultAsync(x => x.Id == profileId, ct);

            if (source is null)
            {
                return;
            }

            EnsureExpectedRowVersion(source, request.ExpectedRowVersion);
            var profileCode = request.NuevoProfileCode.Trim().ToUpperInvariant();
            if (await _context.CfgProfiles.AnyAsync(x => x.ProfileCode == profileCode, ct))
            {
                throw new NachaConfigException(
                    "PROFILE_CODE_ALREADY_EXISTS",
                    $"Ya existe un perfil con el código {profileCode}.",
                    409);
            }

            var draftStatusId = await ResolveCatalogIdAsync<CatConfigStatus>("BORRADOR", ct);
            var clone = new CfgProfile
            {
                ProfileCode = profileCode,
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

            source.UpdatedAt = DateTimeOffset.UtcNow;
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

            AppendChange(clone.Id, nameof(CfgProfile), clone.Id.ToString(), "CLONE", actor, null, clone);
            await _context.SaveChangesAsync(ct);
            cloneId = clone.Id;
        }, ct);

        return cloneId == 0 ? null : await _query.GetProfileDetailAsync(cloneId, ct);
    }

    public Task<bool> InactivateProfileAsync(int profileId, string actor, string expectedRowVersion, CancellationToken ct = default)
        => ChangeStatusAsync(profileId, "INACTIVO", "INACTIVATE", actor, expectedRowVersion, ct);

    public Task<bool> ArchiveProfileAsync(int profileId, string actor, string expectedRowVersion, CancellationToken ct = default)
        => ChangeStatusAsync(profileId, "ARCHIVADO", "ARCHIVE", actor, expectedRowVersion, ct);

    public async Task<bool> UpdateRecordSequenceAsync(int profileId, NachaConfigRecordSequenceUpdateRequest request, string actor, CancellationToken ct = default)
    {
        var updated = false;
        await ExecuteInTransactionAsync(async () =>
        {
            var profile = await _context.CfgProfiles.Include(x => x.Status).FirstOrDefaultAsync(x => x.Id == profileId, ct);
            if (profile is null)
            {
                return;
            }

            EnsureExpectedRowVersion(profile, request.ExpectedRowVersion);
            var records = await _context.CfgProfileRecords.Where(x => x.ProfileId == profileId).ToListAsync(ct);
            if (records.Count == 0)
            {
                throw new NachaConfigException("PROFILE_RECORDS_NOT_FOUND", "El perfil no tiene records configurados.", 409, Convert.ToBase64String(profile.RowVersion));
            }

            var map = request.Records.ToDictionary(x => x.ProfileRecordId, x => x.Sequence);
            foreach (var record in records)
            {
                if (map.TryGetValue(record.Id, out var sequence))
                {
                    record.Sequence = sequence;
                }
            }

            profile.UpdatedAt = DateTimeOffset.UtcNow;
            ValidateRecordSequence(records, profile.RowVersion);
            AppendChange(profileId, nameof(CfgProfileRecord), profileId.ToString(), "RECORD_SEQUENCE_UPDATE", actor, null, request.Records);
            await _context.SaveChangesAsync(ct);
            updated = true;
        }, ct);

        return updated;
    }

    public async Task<bool> UpdateLayoutVariantAsync(int profileId, int variantId, NachaConfigLayoutVariantEditDto request, string actor, CancellationToken ct = default)
    {
        var updated = false;
        await ExecuteInTransactionAsync(async () =>
        {
            var profile = await _context.CfgProfiles.Include(x => x.Status).FirstOrDefaultAsync(x => x.Id == profileId, ct);
            if (profile is null)
            {
                return;
            }

            EnsureExpectedRowVersion(profile, request.ExpectedRowVersion);
            EnsureProfileIsBorrador(profile);
            var variant = await _context.CfgLayoutVariants.FirstOrDefaultAsync(x => x.Id == variantId && x.ProfileId == profileId, ct);
            if (variant is null)
            {
                return;
            }

            variant.NameEs = request.NombreEs.Trim();
            variant.Description = request.Descripcion;
            variant.Priority = request.Priority;
            variant.IsDefaultForRecord = request.IsDefaultForRecord;
            variant.EffectiveFrom = request.EffectiveFrom;
            variant.EffectiveTo = request.EffectiveTo;
            profile.UpdatedAt = DateTimeOffset.UtcNow;
            AppendChange(profileId, nameof(CfgLayoutVariant), variant.Id.ToString(), "VARIANT_UPDATE", actor, null, variant);
            await _context.SaveChangesAsync(ct);
            updated = true;
        }, ct);

        return updated;
    }

    public async Task<bool> UpdateLayoutFieldAsync(int profileId, int fieldId, NachaConfigLayoutFieldEditDto request, string actor, CancellationToken ct = default)
    {
        var updated = false;
        await ExecuteInTransactionAsync(async () =>
        {
            var profile = await _context.CfgProfiles.Include(x => x.Status).FirstOrDefaultAsync(x => x.Id == profileId, ct);
            if (profile is null)
            {
                return;
            }

            EnsureExpectedRowVersion(profile, request.ExpectedRowVersion);
            EnsureProfileIsBorrador(profile);
            var field = await _context.CfgLayoutFields
                .Include(x => x.LayoutVariant)
                .Include(x => x.SourceDefinition)
                .FirstOrDefaultAsync(x => x.Id == fieldId && x.LayoutVariant.ProfileId == profileId, ct);

            if (field is null)
            {
                return;
            }

            field.FieldNameEs = request.FieldNameEs.Trim();
            field.StartPosition = request.StartPosition;
            field.Length = request.Length;
            field.IsEnabled = request.IsEnabled;
            if (!string.IsNullOrWhiteSpace(request.PropertyPath))
            {
                field.SourceDefinition.PropertyPath = request.PropertyPath.Trim();
            }

            profile.UpdatedAt = DateTimeOffset.UtcNow;
            AppendChange(profileId, nameof(CfgLayoutField), field.Id.ToString(), "FIELD_UPDATE", actor, null, field);
            await _context.SaveChangesAsync(ct);
            updated = true;
        }, ct);

        return updated;
    }

    public async Task<bool> UpdateFieldRuleAsync(int profileId, int ruleId, NachaConfigFieldRuleEditDto request, string actor, CancellationToken ct = default)
    {
        var updated = false;
        await ExecuteInTransactionAsync(async () =>
        {
            var profile = await _context.CfgProfiles.Include(x => x.Status).FirstOrDefaultAsync(x => x.Id == profileId, ct);
            if (profile is null)
            {
                return;
            }

            EnsureExpectedRowVersion(profile, request.ExpectedRowVersion);
            EnsureProfileIsBorrador(profile);
            var rule = await _context.CfgFieldRules
                .Include(x => x.LayoutField)
                    .ThenInclude(x => x.LayoutVariant)
                .FirstOrDefaultAsync(x => x.Id == ruleId && x.LayoutField.LayoutVariant.ProfileId == profileId, ct);

            if (rule is null)
            {
                return;
            }

            rule.ErrorCode = request.ErrorCode.Trim();
            rule.ErrorMessageEs = request.ErrorMessageEs.Trim();
            rule.Severity = request.Severity.Trim().ToUpperInvariant();
            rule.IsEnabled = request.IsEnabled;
            profile.UpdatedAt = DateTimeOffset.UtcNow;
            AppendChange(profileId, nameof(CfgFieldRule), rule.Id.ToString(), "RULE_UPDATE", actor, null, rule);
            await _context.SaveChangesAsync(ct);
            updated = true;
        }, ct);

        return updated;
    }

    private async Task<bool> ChangeStatusAsync(int profileId, string statusCode, string changeType, string actor, string expectedRowVersion, CancellationToken ct)
    {
        var changed = false;
        await ExecuteInTransactionAsync(async () =>
        {
            var profile = await _context.CfgProfiles.FirstOrDefaultAsync(x => x.Id == profileId, ct);
            if (profile is null)
            {
                return;
            }

            EnsureExpectedRowVersion(profile, expectedRowVersion);
            profile.StatusId = await ResolveCatalogIdAsync<CatConfigStatus>(statusCode, ct);
            profile.UpdatedAt = DateTimeOffset.UtcNow;
            AppendChange(profileId, nameof(CfgProfile), profileId.ToString(), changeType, actor, null, new { statusCode });
            await _context.SaveChangesAsync(ct);
            changed = true;
        }, ct);

        return changed;
    }

    private static void ValidateRecordSequence(IReadOnlyList<CfgProfileRecord> records, byte[] rowVersion)
    {
        var enabled = records.Where(x => x.IsEnabled).ToList();
        if (enabled.Count == 0)
        {
            throw new NachaConfigException("INVALID_SEQUENCE", "La secuencia no puede quedar sin records habilitados.", 409, Convert.ToBase64String(rowVersion));
        }

        var duplicated = enabled.GroupBy(x => x.Sequence).FirstOrDefault(g => g.Count() > 1);
        if (duplicated is not null)
        {
            throw new NachaConfigException("INVALID_SEQUENCE", $"La secuencia contiene duplicado en la posición {duplicated.Key}.", 409, Convert.ToBase64String(rowVersion));
        }
    }

    private static void EnsureExpectedRowVersion(CfgProfile profile, string expectedRowVersion)
    {
        if (string.IsNullOrWhiteSpace(expectedRowVersion))
        {
            throw new NachaConfigException("CONCURRENCY_TOKEN_REQUIRED", "Debe enviar la versión de concurrencia del perfil.", 409, Convert.ToBase64String(profile.RowVersion));
        }

        byte[] expectedBytes;
        try
        {
            expectedBytes = Convert.FromBase64String(expectedRowVersion);
        }
        catch (FormatException)
        {
            throw new NachaConfigException("INVALID_CONCURRENCY_TOKEN", "La versión de concurrencia no tiene formato Base64 válido.", 400, Convert.ToBase64String(profile.RowVersion));
        }

        if (!profile.RowVersion.SequenceEqual(expectedBytes))
        {
            throw new NachaConfigException("CONCURRENCY_CONFLICT", "El perfil fue modificado por otro usuario.", 409, Convert.ToBase64String(profile.RowVersion));
        }
    }

    private static void EnsureProfileIsBorrador(CfgProfile profile)
    {
        if (!string.Equals(profile.Status.Code, "BORRADOR", StringComparison.OrdinalIgnoreCase))
        {
            throw new NachaConfigException("INVALID_PROFILE_STATE", "Solo se puede editar perfiles en estado BORRADOR.", 409, Convert.ToBase64String(profile.RowVersion));
        }
    }

    private static void ValidateCreateRequest(NachaConfigCreateDraftRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProfileCode)
            || string.IsNullOrWhiteSpace(request.NombreEs)
            || string.IsNullOrWhiteSpace(request.CamaraCode)
            || string.IsNullOrWhiteSpace(request.FlujoCode)
            || string.IsNullOrWhiteSpace(request.DireccionCode))
        {
            throw new NachaConfigException(
                "REQUIRED_FIELDS_MISSING",
                "Código, nombre, cámara, flujo y dirección son obligatorios.",
                400);
        }

        if (request.EffectiveFrom == default)
        {
            throw new NachaConfigException("EFFECTIVE_FROM_REQUIRED", "La vigencia inicial es obligatoria.", 400);
        }
    }

    private static void ValidateEffectiveDates(DateTime effectiveFrom, DateTime? effectiveTo)
    {
        if (effectiveFrom == default)
        {
            throw new NachaConfigException("EFFECTIVE_FROM_REQUIRED", "La vigencia inicial es obligatoria.", 400);
        }

        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
        {
            throw new NachaConfigException(
                "INVALID_EFFECTIVE_RANGE",
                "La vigencia final no puede ser anterior a la vigencia inicial.",
                400);
        }
    }

    private async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation, CancellationToken ct)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await operation();
                await tx.CommitAsync(ct);
                return result;
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync(ct);
                throw new NachaConfigException("CONCURRENCY_CONFLICT", "El perfil fue modificado por otro usuario.", 409);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }

    private Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken ct)
        => ExecuteInTransactionAsync(async () =>
        {
            await operation();
            return true;
        }, ct);

    private async Task<int> ResolveCatalogIdAsync<TCatalog>(string code, CancellationToken ct)
        where TCatalog : class
    {
        var match = await _context.Set<TCatalog>()
            .AsNoTracking()
            .Where(x => EF.Property<string>(x, "Code") == code)
            .Select(x => new
            {
                Id = EF.Property<int>(x, "Id")
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"No se encontró catálogo {typeof(TCatalog).Name} código {code}.");

        return match.Id;
    }

    private void AppendChange(int profileId, string entityName, string entityId, string changeType, string actor, object? before, object? after)
    {
        _context.HistConfigChanges.Add(new HistConfigChange
        {
            ProfileId = profileId,
            EntityName = entityName,
            EntityId = entityId,
            ChangeType = changeType,
            BeforeJson = before is null ? null : JsonSerializer.Serialize(before, AuditJsonOptions),
            AfterJson = after is null ? null : JsonSerializer.Serialize(after, AuditJsonOptions),
            ChangedAtUtc = DateTime.UtcNow,
            ChangedBy = string.IsNullOrWhiteSpace(actor) ? "system" : actor,
            CorrelationId = $"NACHA-CONFIG-{DateTime.UtcNow:yyyyMMddHHmmssfff}"
        });
    }
}
