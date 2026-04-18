using System.Text.Json;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

public sealed class NachaConfigBackfillSeeder : IDbSeeder
{
    private readonly AchDbContext _context;
    public int Order => 8;

    public NachaConfigBackfillSeeder(AchDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        if (await _context.CfgProfiles.AnyAsync())
        {
            return;
        }

        var definitions = await _context.NachaRecordDefinitions
            .AsNoTracking()
            .OrderBy(x => x.Sequence)
            .ToListAsync();

        var layouts = await _context.NachaRecordLayouts
            .AsNoTracking()
            .Include(x => x.Fields)
            .ToListAsync();

        if (definitions.Count == 0 || layouts.Count == 0)
        {
            return;
        }

        var achClearingHouseId = await ResolveCatalogIdAsync(_context.CatClearingHouses, "ACH");
        var originalFlowId = await ResolveCatalogIdAsync(_context.CatFlowTypes, "ORIGINAL");
        var outDirectionId = await ResolveCatalogIdAsync(_context.CatDirections, "SALIDA");
        var publishedStatusId = await ResolveCatalogIdAsync(_context.CatConfigStatuses, "PUBLICADO");

        var defaultServiceClassCode = await _context.CompanyEntryDescriptionCatalogs
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .Select(x => x.StandardEntryClassCode)
            .FirstOrDefaultAsync();

        var serviceClassId = await ResolveServiceClassIdAsync(defaultServiceClassCode ?? "PPD");

        var profile = new CfgProfile
        {
            ProfileCode = "LEGACY_ACH_SALIDA_ORIGINAL_V1_0",
            NameEs = "Perfil legado ACH salida original",
            Description = "Backfill inicial automático desde NachaRecordDefinitions/NachaRecordLayouts/NachaRecordFields.",
            ClearingHouseId = achClearingHouseId,
            FlowTypeId = originalFlowId,
            DirectionId = outDirectionId,
            ServiceClassId = serviceClassId,
            ContextPriority = 100,
            EffectiveFrom = DateTime.UtcNow.Date,
            EffectiveTo = null,
            StatusId = publishedStatusId,
            VersionMajor = 1,
            VersionMinor = 0,
            PublishedAt = DateTime.UtcNow,
            PublishedBy = "system-backfill"
        };

        _context.CfgProfiles.Add(profile);
        await _context.SaveChangesAsync();

        var recordCodeIds = await _context.CatRecordCodes
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Code, x => x.Id);

        var constantTypeId = await ResolveCatalogIdAsync(_context.CatDataSourceTypes, "CONSTANTE");
        var entityTypeId = await ResolveCatalogIdAsync(_context.CatDataSourceTypes, "ENTIDAD");

        var layoutByRecordCode = layouts
            .GroupBy(x => x.RecordCode)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).First());

        var profileRecords = new List<CfgProfileRecord>();
        foreach (var definition in definitions)
        {
            if (!recordCodeIds.TryGetValue(definition.RecordCode, out var recordCodeId))
            {
                continue;
            }

            if (!layoutByRecordCode.TryGetValue(definition.RecordCode, out var legacyLayout))
            {
                continue;
            }

            var layoutVariant = new CfgLayoutVariant
            {
                ProfileId = profile.Id,
                RecordCodeId = recordCodeId,
                VariantCode = $"LEGACY_R{definition.RecordCode}_BASE",
                NameEs = $"Layout legado registro {definition.RecordCode}",
                Description = legacyLayout.Description,
                Priority = 100,
                EffectiveFrom = profile.EffectiveFrom,
                EffectiveTo = null,
                StatusId = publishedStatusId,
                TotalLength = legacyLayout.TotalLength,
                SelectionPredicateJson = null,
                IsDefaultForRecord = true
            };
            _context.CfgLayoutVariants.Add(layoutVariant);
            await _context.SaveChangesAsync();

            foreach (var field in legacyLayout.Fields.OrderBy(x => x.StartPosition))
            {
                var dbColumn = field.DbColumn?.Trim() ?? string.Empty;
                var isConstant = dbColumn.StartsWith("CONST:", StringComparison.OrdinalIgnoreCase);

                var source = new CfgFieldSourceDefinition
                {
                    DataSourceTypeId = isConstant ? constantTypeId : entityTypeId,
                    ConstantValue = isConstant ? dbColumn[6..] : null,
                    EntityName = isConstant ? null : definition.SourceName,
                    PropertyPath = isConstant ? null : dbColumn,
                    SqlObjectName = null,
                    ExpressionDsl = null,
                    ExternalCatalogCode = null,
                    FallbackPolicyJson = null
                };

                _context.CfgFieldSourceDefinitions.Add(source);
                await _context.SaveChangesAsync();

                _context.CfgLayoutFields.Add(new CfgLayoutField
                {
                    LayoutVariantId = layoutVariant.Id,
                    FieldCode = BuildFieldCode(definition.RecordCode, field.FieldName),
                    FieldNameEs = field.FieldName,
                    StartPosition = field.StartPosition,
                    Length = field.Length,
                    PadChar = field.PadChar,
                    Justification = field.Justification,
                    FormatMask = field.Format,
                    SortOrder = field.StartPosition,
                    IsVisibleInBackoffice = true,
                    IsEnabled = true,
                    SourceDefinitionId = source.Id,
                    TransformationPipelineJson = null
                });
            }

            profileRecords.Add(new CfgProfileRecord
            {
                ProfileId = profile.Id,
                RecordCodeId = recordCodeId,
                Sequence = definition.Sequence,
                IsEnabled = definition.IsEnabled,
                MinOccurs = definition.RecordCode == "7" ? 0 : 1,
                MaxOccurs = null,
                SourceStrategy = "TABLE_DRIVEN",
                LayoutVariantId = layoutVariant.Id,
                SemanticRuleSetId = null
            });
        }

        if (profileRecords.Count > 0)
        {
            _context.CfgProfileRecords.AddRange(profileRecords);
        }

        _context.HistConfigSnapshots.Add(new HistConfigSnapshot
        {
            ProfileId = profile.Id,
            VersionMajor = profile.VersionMajor,
            VersionMinor = profile.VersionMinor,
            SnapshotType = "PUBLISH",
            SnapshotJson = JsonSerializer.Serialize(new
            {
                profile.ProfileCode,
                profile.NameEs,
                profile.VersionMajor,
                profile.VersionMinor,
                Records = profileRecords.Select(x => new { x.RecordCodeId, x.Sequence, x.LayoutVariantId })
            }),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = "system-backfill"
        });

        _context.HistConfigChanges.Add(new HistConfigChange
        {
            ProfileId = profile.Id,
            EntityName = nameof(CfgProfile),
            EntityId = profile.Id.ToString(),
            ChangeType = "BACKFILL_CREATE",
            BeforeJson = null,
            AfterJson = JsonSerializer.Serialize(new
            {
                profile.ProfileCode,
                profile.StatusId,
                profile.EffectiveFrom,
                profile.VersionMajor,
                profile.VersionMinor
            }),
            ChangedAtUtc = DateTime.UtcNow,
            ChangedBy = "system-backfill",
            CorrelationId = $"BACKFILL-{profile.Id}"
        });

        await _context.SaveChangesAsync();
    }

    private static string BuildFieldCode(string recordCode, string fieldName)
    {
        var normalized = new string(fieldName
            .ToUpperInvariant()
            .Where(ch => char.IsLetterOrDigit(ch) || ch == '_')
            .ToArray());

        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = "FIELD";
        }

        return $"R{recordCode}_{normalized}";
    }

    private async Task<int> ResolveServiceClassIdAsync(string serviceClassCode)
    {
        var serviceClassId = await _context.CatServiceClasses
            .AsNoTracking()
            .Where(x => x.Code == serviceClassCode)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();

        if (serviceClassId.HasValue)
        {
            return serviceClassId.Value;
        }

        var fallback = await _context.CatServiceClasses
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .FirstAsync();

        return fallback;
    }

    private static async Task<int> ResolveCatalogIdAsync<TCatalog>(IQueryable<TCatalog> query, string code)
        where TCatalog : class
    {
        var entity = await query
            .AsNoTracking()
            .FirstOrDefaultAsync(x => EF.Property<string>(x, "Code") == code)
            ?? throw new InvalidOperationException($"No se encontró catálogo requerido: {typeof(TCatalog).Name} con código {code}.");

        return EF.Property<int>(entity, "Id");
    }
}
