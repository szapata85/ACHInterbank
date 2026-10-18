using System.Globalization;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaConfigHistoryService : INachaConfigHistoryService
{
    private readonly AchDbContext _context;

    public NachaConfigHistoryService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<NachaConfigChangeReviewDto?> GetChangeReviewAsync(int profileId, CancellationToken ct = default)
    {
        var current = await NachaCompleteProfileQuery.Create(_context)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == profileId, ct);
        if (current is null)
        {
            return null;
        }

        CfgProfile? baseline = null;
        if (current.SupersedesProfileId.HasValue)
        {
            baseline = await NachaCompleteProfileQuery.Create(_context)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == current.SupersedesProfileId.Value, ct);
            if (baseline is null)
            {
                throw new NachaConfigException(
                    "COMPARISON_BASE_NOT_FOUND",
                    "No fue posible encontrar la configuración base que este borrador reemplaza.",
                    409,
                    Convert.ToBase64String(current.RowVersion));
            }
            if (baseline.ClearingHouseId != current.ClearingHouseId)
            {
                throw new NachaConfigException(
                    "COMPARISON_BASE_CONTEXT_MISMATCH",
                    "La configuración base pertenece a otra cámara y no puede utilizarse para esta revisión.",
                    409,
                    Convert.ToBase64String(current.RowVersion));
            }
        }

        var changes = BuildChanges(current, baseline);
        var changedFields = changes
            .Where(x => x.FieldId.HasValue)
            .Select(x => x.FieldId!.Value)
            .Distinct()
            .Count();

        return new NachaConfigChangeReviewDto
        {
            ProfileId = current.Id,
            BaseProfileId = baseline?.Id,
            HasComparisonBase = baseline is not null,
            IsEditable = current.Status.IsEditable,
            RowVersion = Convert.ToBase64String(current.RowVersion),
            CurrentVersion = $"{current.VersionMajor}.{current.VersionMinor}",
            NextVersion = $"{current.VersionMajor}.{current.VersionMinor + 1}",
            CamaraNombreEs = current.ClearingHouse.Name,
            FlujoNombreEs = current.FlowType.NameEs,
            DireccionNombreEs = current.Direction.NameEs,
            ServicioNombreEs = current.ServiceClass?.NameEs,
            EffectiveFrom = current.EffectiveFrom,
            EffectiveTo = current.EffectiveTo,
            ChangedFieldsCount = changedFields,
            ChangeCount = changes.Count,
            SummaryEs = changes.Count == 0
                ? "No hay cambios pendientes frente a la configuración base."
                : $"Se encontraron {changes.Count} cambios reales en la configuración; {changedFields} campos resultan afectados.",
            Changes = changes
        };
    }

    public async Task<IReadOnlyList<NachaConfigHistoryItemDto>> GetHistoryAsync(int profileId, CancellationToken ct = default)
    {
        var profile = await NachaCompleteProfileQuery.Create(_context)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == profileId, ct);
        if (profile is null)
        {
            return [];
        }

        var changes = await _context.HistConfigChanges
            .AsNoTracking()
            .Where(x => x.ProfileId == profileId)
            .OrderByDescending(x => x.ChangedAtUtc)
            .ToListAsync(ct);

        return changes.Select(change => HumanizeHistory(profile, change)).ToList();
    }

    public async Task<IReadOnlyList<NachaConfigSnapshotItemDto>> GetSnapshotsAsync(int profileId, CancellationToken ct = default)
    {
        return await _context.HistConfigSnapshots
            .AsNoTracking()
            .Where(x => x.ProfileId == profileId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new NachaConfigSnapshotItemDto
            {
                Id = x.Id,
                CreatedAtUtc = x.CreatedAtUtc,
                CreatedBy = x.CreatedBy,
                SnapshotType = x.SnapshotType,
                VersionMajor = x.VersionMajor,
                VersionMinor = x.VersionMinor
            })
            .ToListAsync(ct);
    }

    private static List<NachaConfigChangeItemDto> BuildChanges(CfgProfile current, CfgProfile? baseline)
    {
        var changes = new List<NachaConfigChangeItemDto>();
        Compare(changes, "PROFILE", "Perfil", null, null, null, null, "Nombre", baseline?.NameEs, current.NameEs);
        Compare(changes, "PROFILE", "Perfil", null, null, null, null, "Descripción", baseline?.Description, current.Description);
        Compare(changes, "PROFILE", "Perfil", null, null, null, null, "Prioridad", baseline?.ContextPriority.ToString(CultureInfo.InvariantCulture), current.ContextPriority.ToString(CultureInfo.InvariantCulture));
        Compare(changes, "PROFILE", "Perfil", null, null, null, null, "Vigencia desde", DateLabel(baseline?.EffectiveFrom), DateLabel(current.EffectiveFrom));
        Compare(changes, "PROFILE", "Perfil", null, null, null, null, "Vigencia hasta", DateLabel(baseline?.EffectiveTo), DateLabel(current.EffectiveTo));

        var baselineRecords = baseline?.Records.ToDictionary(x => x.RecordCode.Code, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, CfgProfileRecord>(StringComparer.OrdinalIgnoreCase);
        foreach (var record in current.Records.OrderBy(x => x.Sequence))
        {
            baselineRecords.TryGetValue(record.RecordCode.Code, out var before);
            Compare(changes, "RECORD", record.RecordCode.NameEs, record.RecordCode.Code, record.RecordCode.NameEs, null, null, "Secuencia", before?.Sequence.ToString(CultureInfo.InvariantCulture), record.Sequence.ToString(CultureInfo.InvariantCulture));
            Compare(changes, "RECORD", record.RecordCode.NameEs, record.RecordCode.Code, record.RecordCode.NameEs, null, null, "Estado", before is null ? null : EnabledLabel(before.IsEnabled), EnabledLabel(record.IsEnabled));
            Compare(changes, "RECORD", record.RecordCode.NameEs, record.RecordCode.Code, record.RecordCode.NameEs, null, null, "Ocurrencias mínimas", before?.MinOccurs.ToString(CultureInfo.InvariantCulture), record.MinOccurs.ToString(CultureInfo.InvariantCulture));
            Compare(changes, "RECORD", record.RecordCode.NameEs, record.RecordCode.Code, record.RecordCode.NameEs, null, null, "Ocurrencias máximas", before?.MaxOccurs?.ToString(CultureInfo.InvariantCulture), record.MaxOccurs?.ToString(CultureInfo.InvariantCulture));
            Compare(changes, "RECORD", record.RecordCode.NameEs, record.RecordCode.Code, record.RecordCode.NameEs, null, null, "Origen del registro", before is null ? null : SourceStrategyLabel(before.SourceStrategy), SourceStrategyLabel(record.SourceStrategy), before?.SourceStrategy, record.SourceStrategy);
        }

        if (baseline is not null)
        {
            foreach (var removed in baseline.Records.Where(x => current.Records.All(c => !string.Equals(c.RecordCode.Code, x.RecordCode.Code, StringComparison.OrdinalIgnoreCase))))
            {
                AddChange(changes, "RECORD", "REMOVED", removed.RecordCode.NameEs, removed.RecordCode.Code, removed.RecordCode.NameEs, null, null, "Registro", "Incluido", "Retirado");
            }
        }

        var baselineVariants = baseline?.LayoutVariants.ToDictionary(VariantKey, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, CfgLayoutVariant>(StringComparer.OrdinalIgnoreCase);
        foreach (var variant in current.LayoutVariants.OrderBy(x => x.RecordCode.Code).ThenBy(x => x.Priority))
        {
            baselineVariants.TryGetValue(VariantKey(variant), out var beforeVariant);
            var elementName = $"{variant.RecordCode.NameEs} · {variant.NameEs}";
            Compare(changes, "VARIANT", elementName, variant.RecordCode.Code, variant.RecordCode.NameEs, null, null, "Nombre de variante", beforeVariant?.NameEs, variant.NameEs);
            Compare(changes, "VARIANT", elementName, variant.RecordCode.Code, variant.RecordCode.NameEs, null, null, "Prioridad", beforeVariant?.Priority.ToString(CultureInfo.InvariantCulture), variant.Priority.ToString(CultureInfo.InvariantCulture));
            Compare(changes, "VARIANT", elementName, variant.RecordCode.Code, variant.RecordCode.NameEs, null, null, "Variante predeterminada", beforeVariant is null ? null : YesNo(beforeVariant.IsDefaultForRecord), YesNo(variant.IsDefaultForRecord));
            Compare(changes, "VARIANT", elementName, variant.RecordCode.Code, variant.RecordCode.NameEs, null, null, "Longitud del registro", beforeVariant?.TotalLength.ToString(CultureInfo.InvariantCulture), variant.TotalLength.ToString(CultureInfo.InvariantCulture));
            Compare(changes, "VARIANT", elementName, variant.RecordCode.Code, variant.RecordCode.NameEs, null, null, "Vigencia desde", DateLabel(beforeVariant?.EffectiveFrom), DateLabel(variant.EffectiveFrom));
            Compare(changes, "VARIANT", elementName, variant.RecordCode.Code, variant.RecordCode.NameEs, null, null, "Vigencia hasta", DateLabel(beforeVariant?.EffectiveTo), DateLabel(variant.EffectiveTo));

            var beforeFields = beforeVariant?.Fields.ToDictionary(x => x.FieldCode, StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, CfgLayoutField>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in variant.Fields.OrderBy(x => x.StartPosition))
            {
                beforeFields.TryGetValue(field.FieldCode, out var beforeField);
                CompareField(changes, variant, field, beforeField);
            }

            if (beforeVariant is not null)
            {
                foreach (var removed in beforeVariant.Fields.Where(x => variant.Fields.All(c => !string.Equals(c.FieldCode, x.FieldCode, StringComparison.OrdinalIgnoreCase))))
                {
                    AddChange(changes, "FIELD", "REMOVED", removed.FieldNameEs, variant.RecordCode.Code, variant.RecordCode.NameEs, null, removed.FieldNameEs, "Campo", "Incluido", "Retirado");
                }
            }
        }

        if (baseline is not null)
        {
            foreach (var removed in baseline.LayoutVariants.Where(x => current.LayoutVariants.All(c => !string.Equals(VariantKey(c), VariantKey(x), StringComparison.OrdinalIgnoreCase))))
            {
                AddChange(changes, "VARIANT", "REMOVED", $"{removed.RecordCode.NameEs} · {removed.NameEs}", removed.RecordCode.Code, removed.RecordCode.NameEs, null, null, "Variante", "Incluida", "Retirada");
            }
        }

        return changes;
    }

    private static void CompareField(List<NachaConfigChangeItemDto> changes, CfgLayoutVariant variant, CfgLayoutField current, CfgLayoutField? baseline)
    {
        Compare(changes, "FIELD", current.FieldNameEs, variant.RecordCode.Code, variant.RecordCode.NameEs, current.Id, current.FieldNameEs, "Nombre funcional", baseline?.FieldNameEs, current.FieldNameEs);
        Compare(changes, "FIELD", current.FieldNameEs, variant.RecordCode.Code, variant.RecordCode.NameEs, current.Id, current.FieldNameEs, "Posición inicial", baseline?.StartPosition.ToString(CultureInfo.InvariantCulture), current.StartPosition.ToString(CultureInfo.InvariantCulture));
        Compare(changes, "FIELD", current.FieldNameEs, variant.RecordCode.Code, variant.RecordCode.NameEs, current.Id, current.FieldNameEs, "Longitud", baseline?.Length.ToString(CultureInfo.InvariantCulture), current.Length.ToString(CultureInfo.InvariantCulture));
        Compare(changes, "FIELD", current.FieldNameEs, variant.RecordCode.Code, variant.RecordCode.NameEs, current.Id, current.FieldNameEs, "Estado", baseline is null ? null : EnabledLabel(baseline.IsEnabled), EnabledLabel(current.IsEnabled));
        Compare(changes, "FIELD", current.FieldNameEs, variant.RecordCode.Code, variant.RecordCode.NameEs, current.Id, current.FieldNameEs, "Origen del valor", baseline is null ? null : SourceLabel(baseline), SourceLabel(current), baseline is null ? null : SourceSignature(baseline), SourceSignature(current));
        Compare(changes, "FIELD", current.FieldNameEs, variant.RecordCode.Code, variant.RecordCode.NameEs, current.Id, current.FieldNameEs, "Formato", baseline is null ? null : FormatLabel(baseline.FormatMask), FormatLabel(current.FormatMask), baseline?.FormatMask, current.FormatMask);
        Compare(changes, "FIELD", current.FieldNameEs, variant.RecordCode.Code, variant.RecordCode.NameEs, current.Id, current.FieldNameEs, "Alineación", baseline is null ? null : JustificationLabel(baseline.Justification), JustificationLabel(current.Justification));
        Compare(changes, "FIELD", current.FieldNameEs, variant.RecordCode.Code, variant.RecordCode.NameEs, current.Id, current.FieldNameEs, "Carácter de relleno", baseline is null ? null : PadLabel(baseline.PadChar), PadLabel(current.PadChar));

        var baselineRules = baseline?.Rules.ToDictionary(x => x.RuleCode, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, CfgFieldRule>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in current.Rules.OrderBy(x => x.Order))
        {
            baselineRules.TryGetValue(rule.RuleCode, out var beforeRule);
            var ruleName = string.IsNullOrWhiteSpace(rule.RuleType.NameEs) ? "Validación" : rule.RuleType.NameEs;
            Compare(changes, "RULE", current.FieldNameEs, variant.RecordCode.Code, variant.RecordCode.NameEs, current.Id, current.FieldNameEs, $"Validación · {ruleName} · estado", beforeRule is null ? null : EnabledLabel(beforeRule.IsEnabled), EnabledLabel(rule.IsEnabled));
            Compare(changes, "RULE", current.FieldNameEs, variant.RecordCode.Code, variant.RecordCode.NameEs, current.Id, current.FieldNameEs, $"Validación · {ruleName} · comportamiento", beforeRule is null ? null : SeverityLabel(beforeRule.Severity), SeverityLabel(rule.Severity));
            Compare(changes, "RULE", current.FieldNameEs, variant.RecordCode.Code, variant.RecordCode.NameEs, current.Id, current.FieldNameEs, $"Validación · {ruleName} · mensaje", beforeRule?.ErrorMessageEs, rule.ErrorMessageEs);
        }
        if (baseline is not null)
        {
            foreach (var removed in baseline.Rules.Where(x => current.Rules.All(c => !string.Equals(c.RuleCode, x.RuleCode, StringComparison.OrdinalIgnoreCase))))
            {
                var ruleName = string.IsNullOrWhiteSpace(removed.RuleType.NameEs) ? "Validación" : removed.RuleType.NameEs;
                AddChange(changes, "RULE", "REMOVED", current.FieldNameEs, variant.RecordCode.Code, variant.RecordCode.NameEs, current.Id, current.FieldNameEs, $"Validación · {ruleName}", "Incluida", "Retirada");
            }
        }
    }

    private static void Compare(
        List<NachaConfigChangeItemDto> changes,
        string scope,
        string elementName,
        string? recordCode,
        string? recordName,
        int? fieldId,
        string? fieldName,
        string property,
        string? beforeDisplay,
        string? afterDisplay,
        string? beforeComparison = null,
        string? afterComparison = null)
    {
        var left = Normalize(beforeComparison ?? beforeDisplay);
        var right = Normalize(afterComparison ?? afterDisplay);
        if (string.Equals(left, right, StringComparison.Ordinal))
        {
            return;
        }

        var kind = beforeDisplay is null ? "ADDED" : afterDisplay is null ? "REMOVED" : "MODIFIED";
        AddChange(changes, scope, kind, elementName, recordCode, recordName, fieldId, fieldName, property, beforeDisplay, afterDisplay);
    }

    private static void AddChange(
        List<NachaConfigChangeItemDto> changes,
        string scope,
        string kind,
        string elementName,
        string? recordCode,
        string? recordName,
        int? fieldId,
        string? fieldName,
        string property,
        string? before,
        string? after)
    {
        changes.Add(new NachaConfigChangeItemDto
        {
            Scope = scope,
            ChangeKind = kind,
            ElementNameEs = elementName,
            RecordCode = recordCode,
            RecordNameEs = recordName,
            FieldId = fieldId,
            FieldNameEs = fieldName,
            PropertyNameEs = property,
            BeforeValueEs = before,
            AfterValueEs = after,
            SummaryEs = $"{property}: {DisplayEmpty(before)} → {DisplayEmpty(after)}"
        });
    }

    private static NachaConfigHistoryItemDto HumanizeHistory(CfgProfile profile, HistConfigChange change)
    {
        var field = int.TryParse(change.EntityId, out var entityId)
            ? profile.LayoutVariants.SelectMany(x => x.Fields).FirstOrDefault(x => x.Id == entityId)
            : null;
        var rule = int.TryParse(change.EntityId, out entityId)
            ? profile.LayoutVariants.SelectMany(x => x.Fields).SelectMany(x => x.Rules).FirstOrDefault(x => x.Id == entityId)
            : null;
        if (rule is not null)
        {
            field = rule.LayoutField;
        }

        var action = ActionLabel(change.ChangeType);
        var element = field?.FieldNameEs
            ?? rule?.LayoutField?.FieldNameEs
            ?? (change.EntityName == nameof(CfgProfile) ? profile.NameEs : HumanizeEntity(change.EntityName));
        var detail = change.ChangeType == "FIELD_EDITOR_UPDATE"
            ? HumanizeFieldAudit(change.BeforeJson, change.AfterJson)
            : new List<string>();
        var version = VersionForHistory(profile, change);
        var summary = detail.Count > 0
            ? $"{action} en {element}: {detail.Count} cambios."
            : change.ChangeType == "PUBLISH"
                ? $"Se publicó la versión {version}."
                : $"{action} en {element}.";

        return new NachaConfigHistoryItemDto
        {
            Id = change.Id,
            ChangedAtUtc = change.ChangedAtUtc,
            ChangedBy = change.ChangedBy,
            ChangeType = change.ChangeType,
            EntityName = change.EntityName,
            CorrelationId = change.CorrelationId,
            ActionNameEs = action,
            ElementNameEs = element,
            Version = version,
            SummaryEs = summary,
            DetailEs = detail
        };
    }

    private static List<string> HumanizeFieldAudit(string? beforeJson, string? afterJson)
    {
        if (string.IsNullOrWhiteSpace(beforeJson) || string.IsNullOrWhiteSpace(afterJson))
        {
            return [];
        }

        try
        {
            using var before = JsonDocument.Parse(beforeJson);
            using var after = JsonDocument.Parse(afterJson);
            var result = new List<string>();
            AddJsonDiff(result, before.RootElement, after.RootElement, "FieldNameEs", "Nombre funcional");
            AddJsonDiff(result, before.RootElement, after.RootElement, "StartPosition", "Posición inicial");
            AddJsonDiff(result, before.RootElement, after.RootElement, "Length", "Longitud");
            AddJsonDiff(result, before.RootElement, after.RootElement, "FormatMask", "Formato");
            AddJsonDiff(result, before.RootElement, after.RootElement, "IsEnabled", "Estado", FormatBool);
            AddJsonDiff(result, before.RootElement, after.RootElement, "Justification", "Alineación", FormatJustification);
            AddJsonDiff(result, before.RootElement, after.RootElement, "PadChar", "Carácter de relleno", FormatPad);
            if (TryGet(before.RootElement, "SourceDefinitionId") != TryGet(after.RootElement, "SourceDefinitionId")
                || TryGet(before.RootElement, "Source") != TryGet(after.RootElement, "Source"))
            {
                result.Add("Origen del valor: actualizado");
            }
            if (TryGet(before.RootElement, "Rules") != TryGet(after.RootElement, "Rules"))
            {
                result.Add("Validaciones: actualizadas");
            }
            return result;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static void AddJsonDiff(List<string> result, JsonElement before, JsonElement after, string property, string label, Func<string?, string>? formatter = null)
    {
        var left = TryGet(before, property);
        var right = TryGet(after, property);
        if (left == right)
        {
            return;
        }
        var format = formatter ?? (value => DisplayEmpty(value));
        result.Add($"{label}: {format(left)} → {format(right)}");
    }

    private static string? TryGet(JsonElement element, string property)
        => element.TryGetProperty(property, out var value)
            ? value.ValueKind == JsonValueKind.Null ? null : value.ToString()
            : null;

    private static string VariantKey(CfgLayoutVariant variant) => $"{variant.RecordCode.Code}|{variant.VariantCode}";
    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;
    private static string DisplayEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? "Sin valor" : value;
    private static string DateLabel(DateTime? value) => value.HasValue ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "Sin fecha límite";
    private static string EnabledLabel(bool enabled) => enabled ? "Habilitado" : "Inactivo";
    private static string YesNo(bool value) => value ? "Sí" : "No";
    private static string SeverityLabel(string? severity) => string.Equals(severity, "WARN", StringComparison.OrdinalIgnoreCase) ? "Advertencia" : "Error bloqueante";
    private static string SourceStrategyLabel(string? value) => string.Equals(value, "TABLE_DRIVEN", StringComparison.OrdinalIgnoreCase)
        ? "Configuración parametrizada"
        : DisplayEmpty(value?.Replace('_', ' ').ToLowerInvariant());
    private static string JustificationLabel(char value) => char.ToUpperInvariant(value) == 'R' ? "Alinear a la derecha" : "Alinear a la izquierda";
    private static string PadLabel(char value) => value == ' ' ? "Espacio" : value.ToString();
    private static string FormatLabel(string? value) => string.IsNullOrWhiteSpace(value) ? "Sin formato especial" : value;
    private static string FormatBool(string? value) => string.Equals(value, "True", StringComparison.OrdinalIgnoreCase) ? "Habilitado" : "Inactivo";
    private static string FormatJustification(string? value) => string.Equals(value, "R", StringComparison.OrdinalIgnoreCase) ? "Alinear a la derecha" : "Alinear a la izquierda";
    private static string FormatPad(string? value) => value == " " ? "Espacio" : DisplayEmpty(value);

    private static string SourceSignature(CfgLayoutField field)
    {
        var source = field.SourceDefinition;
        return string.Join("|", new[]
        {
            source.DataSourceType.Code,
            source.ConstantValue,
            source.EntityName,
            source.PropertyPath,
            source.SqlObjectName,
            source.ExpressionDsl,
            source.ExternalCatalogCode,
            source.FallbackPolicyJson
        }.Select(Normalize));
    }

    private static string SourceLabel(CfgLayoutField field)
    {
        var source = field.SourceDefinition;
        if (string.Equals(source.DataSourceType.Code, "CONSTANTE", StringComparison.OrdinalIgnoreCase))
        {
            return $"Valor fijo → {DisplayEmpty(source.ConstantValue)}";
        }
        return $"{source.DataSourceType.NameEs} → {field.FieldNameEs}";
    }

    private static string ActionLabel(string changeType) => changeType switch
    {
        "DRAFT_CREATE" => "Borrador creado",
        "DRAFT_UPDATE" => "Datos generales actualizados",
        "CLONE" => "Nueva versión creada",
        "RECORD_SEQUENCE_UPDATE" => "Estructura reordenada",
        "VARIANT_UPDATE" => "Variante actualizada",
        "FIELD_UPDATE" => "Campo actualizado",
        "FIELD_EDITOR_UPDATE" => "Campo actualizado",
        "RULE_UPDATE" => "Validación actualizada",
        "PUBLISH" => "Configuración publicada",
        "INACTIVATE" => "Configuración inactivada",
        "ARCHIVE" => "Configuración archivada",
        _ => "Configuración actualizada"
    };

    private static string HumanizeEntity(string entityName) => entityName switch
    {
        nameof(CfgProfileRecord) => "Estructura del archivo",
        nameof(CfgLayoutVariant) => "Variante de registro",
        nameof(CfgLayoutField) => "Campo",
        nameof(CfgFieldRule) => "Validación",
        _ => "Configuración"
    };

    private static string VersionForHistory(CfgProfile profile, HistConfigChange change)
    {
        if (change.ChangeType == "PUBLISH" && !string.IsNullOrWhiteSpace(change.AfterJson))
        {
            try
            {
                using var json = JsonDocument.Parse(change.AfterJson);
                if (json.RootElement.TryGetProperty("VersionMajor", out var major)
                    && json.RootElement.TryGetProperty("VersionMinor", out var minor))
                {
                    return $"{major.GetInt32()}.{minor.GetInt32()}";
                }
            }
            catch (JsonException)
            {
                // Use persisted profile version below.
            }
        }

        if (profile.PublishedAt.HasValue && change.ChangedAtUtc < profile.PublishedAt.Value && change.ChangeType != "PUBLISH")
        {
            return $"{profile.VersionMajor}.{Math.Max(0, profile.VersionMinor - 1)}";
        }
        return $"{profile.VersionMajor}.{profile.VersionMinor}";
    }
}
