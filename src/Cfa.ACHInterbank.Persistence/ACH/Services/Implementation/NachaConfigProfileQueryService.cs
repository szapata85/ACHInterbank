using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaConfigProfileQueryService : INachaConfigProfileQueryService
{
    private readonly AchDbContext _context;

    public NachaConfigProfileQueryService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<NachaConfigProfileListItemDto>> GetProfilesAsync(CancellationToken ct = default)
    {
        var profiles = await _context.CfgProfiles
            .AsNoTracking()
            .Include(x => x.Status)
            .Include(x => x.ClearingHouse)
            .Include(x => x.FlowType)
            .Include(x => x.Direction)
            .Include(x => x.ServiceClass)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(ct);

        return profiles.Select(x => new NachaConfigProfileListItemDto
        {
            Id = x.Id,
            ProfileCode = x.ProfileCode,
            NombreEs = x.NameEs,
            Estado = x.Status.Code,
            Camara = x.ClearingHouse.Code,
            Flujo = x.FlowType.Code,
            Direccion = x.Direction.Code,
            Servicio = x.ServiceClass?.Code,
            VersionMajor = x.VersionMajor,
            VersionMinor = x.VersionMinor,
            EffectiveFrom = x.EffectiveFrom,
            EffectiveTo = x.EffectiveTo,
            RowVersion = Convert.ToBase64String(x.RowVersion)
        }).ToList();
    }

    public async Task<NachaConfigProfileDetailDto?> GetProfileDetailAsync(int profileId, CancellationToken ct = default)
    {
        var profile = await _context.CfgProfiles
            .AsNoTracking()
            .Include(x => x.Status)
            .Include(x => x.ClearingHouse)
            .Include(x => x.FlowType)
            .Include(x => x.Direction)
            .Include(x => x.ServiceClass)
            .Include(x => x.Records)
                .ThenInclude(x => x.RecordCode)
            .Include(x => x.LayoutVariants)
                .ThenInclude(x => x.RecordCode)
            .Include(x => x.LayoutVariants)
                .ThenInclude(x => x.Fields)
                    .ThenInclude(x => x.SourceDefinition)
                        .ThenInclude(x => x.DataSourceType)
            .Include(x => x.LayoutVariants)
                .ThenInclude(x => x.Fields)
                    .ThenInclude(x => x.Rules)
                        .ThenInclude(x => x.RuleType)
            .FirstOrDefaultAsync(x => x.Id == profileId, ct);

        if (profile is null)
        {
            return null;
        }

        return new NachaConfigProfileDetailDto
        {
            Id = profile.Id,
            ProfileCode = profile.ProfileCode,
            NombreEs = profile.NameEs,
            Descripcion = profile.Description,
            Estado = profile.Status.Code,
            EstadoNombre = HumanizeCode(profile.Status.Code),
            IsEditable = profile.Status.IsEditable,
            Camara = profile.ClearingHouse.Code,
            CamaraNombre = profile.ClearingHouse.Name,
            Flujo = profile.FlowType.Code,
            FlujoNombre = profile.FlowType.NameEs,
            Direccion = profile.Direction.Code,
            DireccionNombre = profile.Direction.NameEs,
            Servicio = profile.ServiceClass?.Code,
            ServicioNombre = profile.ServiceClass?.NameEs,
            VersionMajor = profile.VersionMajor,
            VersionMinor = profile.VersionMinor,
            ContextPriority = profile.ContextPriority,
            EffectiveFrom = profile.EffectiveFrom,
            EffectiveTo = profile.EffectiveTo,
            RowVersion = Convert.ToBase64String(profile.RowVersion),
            Records = profile.Records.OrderBy(x => x.Sequence).Select(x => new NachaConfigProfileRecordDto
            {
                Id = x.Id,
                RecordCode = x.RecordCode.Code,
                RecordNameEs = x.RecordCode.NameEs,
                Sequence = x.Sequence,
                IsEnabled = x.IsEnabled,
                MinOccurs = x.MinOccurs,
                MaxOccurs = x.MaxOccurs,
                SourceStrategy = x.SourceStrategy,
                SourceStrategyName = HumanizeSourceStrategy(x.SourceStrategy)
            }).ToList(),
            Variantes = profile.LayoutVariants.OrderBy(x => x.RecordCode.Code).ThenBy(x => x.Priority).Select(v => new NachaConfigLayoutVariantDto
            {
                Id = v.Id,
                RecordCode = v.RecordCode.Code,
                VariantCode = v.VariantCode,
                NombreEs = v.NameEs,
                Descripcion = v.Description,
                Priority = v.Priority,
                IsDefaultForRecord = v.IsDefaultForRecord,
                TotalLength = v.TotalLength,
                EffectiveFrom = v.EffectiveFrom,
                EffectiveTo = v.EffectiveTo,
                Fields = v.Fields.OrderBy(f => f.StartPosition).Select(f => new NachaConfigLayoutFieldDto
                {
                    Id = f.Id,
                    FieldCode = f.FieldCode,
                    FieldNameEs = f.FieldNameEs,
                    StartPosition = f.StartPosition,
                    Length = f.Length,
                    PadChar = f.PadChar.ToString(),
                    Justification = f.Justification.ToString(),
                    FormatMask = f.FormatMask,
                    SortOrder = f.SortOrder,
                    IsVisibleInBackoffice = f.IsVisibleInBackoffice,
                    TransformationPipelineJson = f.TransformationPipelineJson,
                    PropertyPath = f.SourceDefinition.PropertyPath,
                    SourceType = f.SourceDefinition.DataSourceType.Code,
                    SourceTypeName = f.SourceDefinition.DataSourceType.NameEs,
                    SourceDefinitionId = f.SourceDefinitionId,
                    ConstantValue = f.SourceDefinition.ConstantValue,
                    EntityName = f.SourceDefinition.EntityName,
                    SqlObjectName = f.SourceDefinition.SqlObjectName,
                    ExpressionDsl = f.SourceDefinition.ExpressionDsl,
                    ExternalCatalogCode = f.SourceDefinition.ExternalCatalogCode,
                    FallbackPolicyJson = f.SourceDefinition.FallbackPolicyJson,
                    IsEnabled = f.IsEnabled,
                    Reglas = f.Rules
                        .OrderBy(r => r.Id)
                        .Select(r => new NachaConfigFieldRuleDto
                        {
                            Id = r.Id,
                            RuleType = r.RuleType.Code,
                            RuleTypeName = r.RuleType.NameEs,
                            ErrorCode = r.ErrorCode,
                            ErrorMessageEs = r.ErrorMessageEs,
                            Severity = r.Severity,
                            SeverityName = HumanizeSeverity(r.Severity),
                            RuleCode = r.RuleCode,
                            ConditionDsl = r.ConditionDsl,
                            RuleConfigJson = r.RuleConfigJson,
                            Order = r.Order,
                            IsEnabled = r.IsEnabled
                        }).ToList()
                }).ToList()
            }).ToList()
        };
    }

    public async Task<NachaConfigWorkspaceDto?> GetWorkspaceAsync(int profileId, CancellationToken ct = default)
    {
        var detail = await GetProfileDetailAsync(profileId, ct);
        if (detail is null)
        {
            return null;
        }

        return new NachaConfigWorkspaceDto
        {
            Id = detail.Id,
            ProfileCode = detail.ProfileCode,
            NombreEs = detail.NombreEs,
            Descripcion = detail.Descripcion,
            Estado = detail.Estado,
            EstadoNombre = detail.EstadoNombre,
            IsEditable = detail.IsEditable,
            Camara = detail.Camara,
            CamaraNombre = detail.CamaraNombre,
            Flujo = detail.Flujo,
            FlujoNombre = detail.FlujoNombre,
            Direccion = detail.Direccion,
            DireccionNombre = detail.DireccionNombre,
            Servicio = detail.Servicio,
            ServicioNombre = detail.ServicioNombre,
            VersionMajor = detail.VersionMajor,
            VersionMinor = detail.VersionMinor,
            ContextPriority = detail.ContextPriority,
            EffectiveFrom = detail.EffectiveFrom,
            EffectiveTo = detail.EffectiveTo,
            RowVersion = detail.RowVersion,
            Records = detail.Records,
            Variantes = detail.Variantes
        };
    }

    public async Task<NachaConfigFieldEditorDto?> GetFieldEditorAsync(int profileId, int fieldId, CancellationToken ct = default)
    {
        var profile = await _context.CfgProfiles
            .AsNoTracking()
            .Include(x => x.Status)
            .FirstOrDefaultAsync(x => x.Id == profileId, ct);
        if (profile is null)
        {
            return null;
        }

        var detail = await GetProfileDetailAsync(profileId, ct);
        var field = detail?.Variantes.SelectMany(x => x.Fields).FirstOrDefault(x => x.Id == fieldId);
        if (field is null)
        {
            return null;
        }

        var sourceTypes = await _context.CatDataSourceTypes
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Code, x.NameEs })
            .ToListAsync(ct);

        var sourceCandidates = await _context.CfgLayoutFields
            .AsNoTracking()
            .Where(x => x.LayoutVariant.Profile.ClearingHouseId == profile.ClearingHouseId)
            .Where(x => x.SourceDefinition.DataSourceType.Code != "CONSTANTE")
            .OrderBy(x => x.SourceDefinitionId)
            .ThenBy(x => x.FieldNameEs)
            .Select(x => new
            {
                x.SourceDefinitionId,
                SourceTypeCode = x.SourceDefinition.DataSourceType.Code,
                SourceTypeName = x.SourceDefinition.DataSourceType.NameEs,
                x.FieldNameEs
            })
            .ToListAsync(ct);

        var sourceOptions = sourceCandidates
            .GroupBy(x => x.SourceDefinitionId)
            .Select(group =>
            {
                var item = group.First();
                var labelPrefix = EditorSourceTypeLabel(item.SourceTypeCode, item.SourceTypeName);
                return new NachaConfigEditorSourceOptionDto
                {
                    SourceDefinitionId = item.SourceDefinitionId,
                    SourceTypeCode = item.SourceTypeCode,
                    LabelEs = $"{labelPrefix} → {item.FieldNameEs}"
                };
            })
            .OrderBy(x => x.SourceTypeCode)
            .ThenBy(x => x.LabelEs)
            .ToList();

        var formatCandidates = await _context.CfgLayoutFields
            .AsNoTracking()
            .Where(x => x.LayoutVariant.Profile.ClearingHouseId == profile.ClearingHouseId)
            .Where(x => x.FormatMask != null && x.FormatMask != "")
            .OrderBy(x => x.FieldNameEs)
            .Select(x => new { x.FormatMask, x.FieldNameEs })
            .ToListAsync(ct);

        var formats = formatCandidates
            .Where(x => !string.IsNullOrWhiteSpace(x.FormatMask))
            .GroupBy(x => x.FormatMask!)
            .Select(group => new NachaConfigEditorOptionDto
            {
                Code = group.Key,
                LabelEs = $"Formato usado en {group.First().FieldNameEs}"
            })
            .OrderBy(x => x.LabelEs)
            .ToList();

        var availableSourceTypeCodes = sourceOptions
            .Select(x => x.SourceTypeCode)
            .Append("CONSTANTE")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sourceReferenceCount = await _context.CfgLayoutFields
            .AsNoTracking()
            .CountAsync(x => x.SourceDefinitionId == field.SourceDefinitionId, ct);

        return new NachaConfigFieldEditorDto
        {
            ProfileId = profileId,
            FieldId = fieldId,
            IsEditable = profile.Status.IsEditable,
            RowVersion = Convert.ToBase64String(profile.RowVersion),
            Field = field,
            SourceReferenceCount = sourceReferenceCount,
            SourceTypes = sourceTypes
                .Where(x => availableSourceTypeCodes.Contains(x.Code))
                .Select(x => new NachaConfigEditorSourceTypeOptionDto
                {
                    Code = x.Code,
                    LabelEs = EditorSourceTypeLabel(x.Code, x.NameEs),
                    CatalogLabelEs = x.NameEs,
                    UsesFixedValue = x.Code == "CONSTANTE",
                    RequiresDefinitionSelection = x.Code != "CONSTANTE"
                })
                .ToList(),
            SourceOptions = sourceOptions,
            Alignments =
            [
                new NachaConfigEditorOptionDto { Code = "L", LabelEs = "Alinear a la izquierda" },
                new NachaConfigEditorOptionDto { Code = "R", LabelEs = "Alinear a la derecha" }
            ],
            Formats = formats,
            Severities =
            [
                new NachaConfigEditorOptionDto { Code = "ERROR", LabelEs = HumanizeSeverity("ERROR") },
                new NachaConfigEditorOptionDto { Code = "WARN", LabelEs = HumanizeSeverity("WARN") }
            ]
        };
    }

    public Task<NachaConfigFilterCatalogsDto> GetFilterCatalogsAsync(CancellationToken ct = default)
        => GetOperationalCatalogsAsync(ct: ct);

    public async Task<NachaConfigFilterCatalogsDto> GetOperationalCatalogsAsync(
        string? camaraCode = null,
        string? flujoCode = null,
        CancellationToken ct = default)
    {
        var normalizedCamaraCode = NormalizeCode(camaraCode);
        var normalizedFlujoCode = NormalizeCode(flujoCode);
        var hasCamaraContext = normalizedCamaraCode.Length > 0;
        var hasFlujoContext = normalizedFlujoCode.Length > 0;

        var clearingHouseId = hasCamaraContext
            ? await _context.CatClearingHouses
                .AsNoTracking()
                .Where(x => x.IsActive && x.Code == normalizedCamaraCode)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(ct)
            : null;

        var flowContext = hasFlujoContext
            ? await _context.CatFlowTypes
                .AsNoTracking()
                .Where(x => x.IsActive && x.Code == normalizedFlujoCode)
                .Select(x => new { x.Id, x.DirectionDefaultId })
                .FirstOrDefaultAsync(ct)
            : null;

        var directions = _context.CatDirections.AsNoTracking().Where(x => x.IsActive);
        if (hasFlujoContext)
        {
            directions = flowContext is null
                ? directions.Where(_ => false)
                : flowContext.DirectionDefaultId.HasValue
                    ? directions.Where(x => x.Id == flowContext.DirectionDefaultId.Value)
                    : directions;
        }

        var services = _context.CatServiceClasses.AsNoTracking().Where(x => x.IsActive);
        if (hasCamaraContext)
        {
            services = clearingHouseId.HasValue
                ? services.Where(x => x.ClearingHouseId == null || x.ClearingHouseId == clearingHouseId.Value)
                : services.Where(_ => false);
        }

        var statusCodes = await _context.CatConfigStatuses
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => x.Code)
            .ToListAsync(ct);

        return new NachaConfigFilterCatalogsDto
        {
            Estados = statusCodes
                .Select(code => new NachaConfigFilterCatalogOptionDto
                {
                    Code = code,
                    LabelEs = HumanizeCode(code)
                })
                .ToList(),
            Camaras = await _context.CatClearingHouses
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Code)
                .Select(x => new NachaConfigFilterCatalogOptionDto
                {
                    Code = x.Code,
                    LabelEs = x.Name
                })
                .ToListAsync(ct),
            Flujos = await _context.CatFlowTypes
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Code)
                .Select(x => new NachaConfigFilterCatalogOptionDto
                {
                    Code = x.Code,
                    LabelEs = x.NameEs
                })
                .ToListAsync(ct),
            Direcciones = await directions
                .OrderBy(x => x.Code)
                .Select(x => new NachaConfigFilterCatalogOptionDto
                {
                    Code = x.Code,
                    LabelEs = x.NameEs
                })
                .ToListAsync(ct),
            Servicios = await services
                .OrderBy(x => x.Code)
                .Select(x => new NachaConfigFilterCatalogOptionDto
                {
                    Code = x.Code,
                    LabelEs = x.NameEs
                })
                .ToListAsync(ct),
            Registros = await _context.CatRecordCodes
                .AsNoTracking()
                .OrderBy(x => x.Code)
                .Select(x => new NachaConfigFilterCatalogOptionDto
                {
                    Code = x.Code,
                    LabelEs = x.NameEs
                })
                .ToListAsync(ct),
            TiposOrigen = await _context.CatDataSourceTypes
                .AsNoTracking()
                .OrderBy(x => x.Code)
                .Select(x => new NachaConfigFilterCatalogOptionDto
                {
                    Code = x.Code,
                    LabelEs = x.NameEs
                })
                .ToListAsync(ct),
            TiposRegla = await _context.CatRuleTypes
                .AsNoTracking()
                .OrderBy(x => x.Code)
                .Select(x => new NachaConfigFilterCatalogOptionDto
                {
                    Code = x.Code,
                    LabelEs = x.NameEs
                })
                .ToListAsync(ct)
        };
    }

    private static string EditorSourceTypeLabel(string code, string catalogLabel)
        => code.ToUpperInvariant() switch
        {
            "CONSTANTE" => "Valor fijo",
            "ENTIDAD" => "Dato del sistema",
            "EXPRESION" => "Calculado por el sistema",
            "SQL_VIEW" or "SQL_PROCEDURE" => "Dato autorizado",
            _ => catalogLabel
        };

    private static string NormalizeCode(string? value)
        => value?.Trim().ToUpperInvariant() ?? string.Empty;

    private static string HumanizeCode(string value)
    {
        var normalized = value.Replace('_', ' ').Trim().ToLowerInvariant();
        return CultureInfo.GetCultureInfo("es-CO").TextInfo.ToTitleCase(normalized);
    }

    private static string HumanizeSourceStrategy(string value)
        => string.Equals(value, "TABLE_DRIVEN", StringComparison.OrdinalIgnoreCase)
            ? "Configuración parametrizada"
            : HumanizeCode(value);

    private static string HumanizeSeverity(string value)
        => value.Trim().ToUpperInvariant() switch
        {
            "ERROR" => "Error bloqueante",
            "WARN" or "WARNING" => "Advertencia",
            _ => HumanizeCode(value)
        };
}
