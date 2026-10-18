using System.Globalization;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Read-only operational test for NACHA configuration. It reuses the publish validator, resolver preview
/// and official record renderer without publishing, persisting generated files, dispatching or mutating configuration.
/// </summary>
[Scoped]
public sealed class NachaConfigOperationalTestService : INachaConfigOperationalTestService
{
    private readonly AchDbContext _context;
    private readonly INachaConfigValidationService _validation;
    private readonly INachaConfigPreviewService _preview;

    public NachaConfigOperationalTestService(
        AchDbContext context,
        INachaConfigValidationService validation,
        INachaConfigPreviewService preview)
    {
        _context = context;
        _validation = validation;
        _preview = preview;
    }

    public async Task<NachaConfigOperationalTestResultDto?> TestAsync(int profileId, CancellationToken ct = default)
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
                .ThenInclude(x => x.Status)
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

        var validation = await _validation.ValidateBeforePublishAsync(profileId, ct);
        var processDateUtc = ResolveProcessDate(profile);
        var enabledRecordCodes = profile.Records
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.Sequence)
            .Select(x => x.RecordCode.Code)
            .ToArray();

        var selection = await _preview.PreviewResolverAsync(new NachaConfigResolverPreviewRequest
        {
            CamaraCode = profile.ClearingHouse.Code,
            FlujoCode = profile.FlowType.Code,
            DireccionCode = profile.Direction.Code,
            ServicioCode = profile.ServiceClass?.Code,
            ProcessDateUtc = processDateUtc,
            RecordCodes = enabledRecordCodes
        }, ct);

        var issues = validation.Issues
            .Select(issue => HumanizeIssue(issue, profile))
            .ToList();

        var previews = new List<NachaConfigRecordPreviewDto>();
        foreach (var record in profile.Records.Where(x => x.IsEnabled).OrderBy(x => x.Sequence))
        {
            var variants = profile.LayoutVariants
                .Where(x => x.RecordCodeId == record.RecordCodeId)
                .OrderByDescending(x => x.Id == record.LayoutVariantId)
                .ThenByDescending(x => x.IsDefaultForRecord)
                .ThenBy(x => x.Priority)
                .ThenBy(x => x.VariantCode)
                .ToList();

            foreach (var variant in variants)
            {
                previews.Add(BuildRecordPreview(profile, record, variant, processDateUtc));
            }
        }

        return new NachaConfigOperationalTestResultDto
        {
            ProfileId = profileId,
            ProcessDateUtc = processDateUtc,
            CanPublish = validation.IsValid,
            ResultLabelEs = validation.IsValid ? "Apto para publicar" : "Requiere correcciones",
            ErroresBloqueantes = validation.ErroresBloqueantes,
            Advertencias = validation.Advertencias,
            Issues = issues,
            Selection = BuildSelectionSummary(profile, selection, processDateUtc),
            Records = previews
        };
    }

    private static NachaConfigRecordPreviewDto BuildRecordPreview(
        CfgProfile profile,
        CfgProfileRecord record,
        CfgLayoutVariant variant,
        DateTime processDateUtc)
    {
        string? renderedRecord = null;
        string? contentMessage = null;
        var renderer = new NachaOfficialRecordRenderer();
        var hasUnsafeRange = variant.TotalLength <= 0 || variant.Fields
            .Where(field => field.IsEnabled)
            .Any(field => field.StartPosition <= 0
                          || field.Length <= 0
                          || (long)field.StartPosition + field.Length - 1 > variant.TotalLength);
        if (hasUnsafeRange)
        {
            contentMessage = "La estructura puede revisarse, pero el ejemplo de contenido no está disponible hasta corregir posiciones o longitudes inválidas.";
        }
        else
        try
        {
            var values = BuildSyntheticValues(variant, processDateUtc);
            renderedRecord = renderer.Render(
                record.RecordCode.Code,
                values,
                variant,
                new NachaGenerationAuditResult
                {
                    Mode = "CONFIG_PREVIEW",
                    ProfileId = profile.Id,
                    ProfileCode = profile.ProfileCode,
                    ProfileStatus = profile.Status.Code,
                    ClearingHouseCode = profile.ClearingHouse.Code,
                    EffectiveDate = processDateUtc
                },
                new NachaOfficialRecordRenderContext(
                    profile.ClearingHouse.Code,
                    profile.ClearingHouse.Code,
                    profile.ClearingHouse.Code),
                recordSequence: 1,
                lineNumber: 1);
        }
        catch (NachaGenerationException)
        {
            contentMessage = "La estructura puede revisarse, pero el ejemplo de contenido no está disponible hasta corregir la configuración indicada por la validación.";
        }
        catch (InvalidOperationException)
        {
            contentMessage = "La estructura puede revisarse, pero el ejemplo de contenido no pudo generarse con datos sintéticos.";
        }
        catch (FormatException)
        {
            contentMessage = "La estructura puede revisarse, pero el ejemplo de contenido no pudo generarse con el formato configurado.";
        }

        var fields = variant.Fields
            .OrderBy(x => x.StartPosition)
            .ThenBy(x => x.SortOrder)
            .Select(field => BuildFieldPreview(field, renderedRecord))
            .ToList();

        return new NachaConfigRecordPreviewDto
        {
            VariantId = variant.Id,
            RecordCode = record.RecordCode.Code,
            RecordNameEs = record.RecordCode.NameEs,
            VariantCode = variant.VariantCode,
            VariantNameEs = variant.NameEs,
            TotalLength = variant.TotalLength,
            IsDefaultForRecord = variant.IsDefaultForRecord || record.LayoutVariantId == variant.Id,
            ContentPreviewAvailable = renderedRecord is not null,
            ContentPreviewMessageEs = contentMessage,
            RenderedRecord = renderedRecord,
            Fields = fields
        };
    }

    private static NachaConfigFieldPreviewDto BuildFieldPreview(CfgLayoutField field, string? renderedRecord)
    {
        var sampleValue = renderedRecord is not null
                          && field.StartPosition > 0
                          && field.Length > 0
                          && field.StartPosition - 1 + field.Length <= renderedRecord.Length
            ? renderedRecord.Substring(field.StartPosition - 1, field.Length)
            : null;

        return new NachaConfigFieldPreviewDto
        {
            FieldId = field.Id,
            FieldNameEs = field.FieldNameEs,
            StartPosition = field.StartPosition,
            EndPosition = field.StartPosition + field.Length - 1,
            Length = field.Length,
            IsEnabled = field.IsEnabled,
            IsRequired = IsRequired(field),
            SourceSummaryEs = SourceSummary(field),
            FormatSummaryEs = string.IsNullOrWhiteSpace(field.FormatMask) ? null : field.FormatMask,
            SampleValue = sampleValue,
            RelevantRulesEs = field.Rules
                .Where(rule => rule.IsEnabled)
                .OrderBy(rule => rule.Order)
                .Select(rule => string.IsNullOrWhiteSpace(rule.ErrorMessageEs)
                    ? rule.RuleType.NameEs
                    : $"{rule.RuleType.NameEs}: {rule.ErrorMessageEs}")
                .ToList()
        };
    }

    private static Dictionary<string, object?> BuildSyntheticValues(CfgLayoutVariant variant, DateTime processDateUtc)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["EffectiveEntryDate"] = processDateUtc.Date
        };

        foreach (var field in variant.Fields.Where(x => x.IsEnabled))
        {
            var source = field.SourceDefinition;
            var sample = CreateSyntheticValue(field, processDateUtc);
            if (string.Equals(source.DataSourceType.Code, "ENTIDAD", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(source.PropertyPath))
            {
                values[source.PropertyPath] = sample;
            }
            else if (string.Equals(source.DataSourceType.Code, "EXPRESION", StringComparison.OrdinalIgnoreCase))
            {
                var calculationType = TryReadCalculationType(source.ExpressionDsl);
                if (!string.IsNullOrWhiteSpace(calculationType)
                    && !string.Equals(calculationType, "Filler", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(calculationType, "JulianSettlementDate", StringComparison.OrdinalIgnoreCase))
                {
                    values[calculationType] = sample;
                }
            }
        }

        return values;
    }

    private static object CreateSyntheticValue(CfgLayoutField field, DateTime processDateUtc)
    {
        foreach (var rule in field.Rules.Where(x => x.IsEnabled).OrderBy(x => x.Order))
        {
            try
            {
                using var document = JsonDocument.Parse(rule.RuleConfigJson ?? "{}");
                var root = document.RootElement;
                if (root.TryGetProperty("allowedValues", out var allowedValues)
                    && allowedValues.ValueKind == JsonValueKind.Array)
                {
                    var allowed = allowedValues.EnumerateArray()
                        .Where(item => item.ValueKind == JsonValueKind.String)
                        .Select(item => item.GetString())
                        .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
                    if (!string.IsNullOrWhiteSpace(allowed))
                    {
                        return allowed;
                    }
                }

                var dataType = root.TryGetProperty("dataType", out var dataTypeElement)
                    ? dataTypeElement.GetString()?.ToUpperInvariant()
                    : null;
                var format = root.TryGetProperty("format", out var formatElement)
                    ? formatElement.GetString()
                    : null;

                return dataType switch
                {
                    "NUMERIC" => "0",
                    "DATE" => processDateUtc.ToString(format ?? field.FormatMask ?? "yyyyMMdd", CultureInfo.InvariantCulture),
                    "TIME" => processDateUtc.ToString(format ?? field.FormatMask ?? "HHmm", CultureInfo.InvariantCulture),
                    "RESERVED" => new string(' ', Math.Max(1, field.Length)),
                    _ => field.Length > 0 ? new string('X', Math.Min(field.Length, 3)) : string.Empty
                };
            }
            catch (JsonException)
            {
                return field.Length > 0 ? new string('X', Math.Min(field.Length, 3)) : string.Empty;
            }
        }

        return field.Length > 0 ? new string('X', Math.Min(field.Length, 3)) : string.Empty;
    }

    private static bool IsRequired(CfgLayoutField field)
    {
        foreach (var rule in field.Rules.Where(x => x.IsEnabled))
        {
            try
            {
                using var document = JsonDocument.Parse(rule.RuleConfigJson ?? "{}");
                if (document.RootElement.TryGetProperty("required", out var required)
                    && required.ValueKind is JsonValueKind.True or JsonValueKind.False)
                {
                    return required.GetBoolean();
                }
            }
            catch (JsonException)
            {
                // The existing validator will report invalid rule metadata. Preview remains read-only.
            }
        }

        return false;
    }

    private static string SourceSummary(CfgLayoutField field)
    {
        var code = field.SourceDefinition.DataSourceType.Code;
        if (string.Equals(code, "CONSTANTE", StringComparison.OrdinalIgnoreCase))
        {
            return "Valor fijo";
        }

        if (string.Equals(code, "ENTIDAD", StringComparison.OrdinalIgnoreCase))
        {
            return $"Dato del sistema → {field.FieldNameEs}";
        }

        if (string.Equals(code, "EXPRESION", StringComparison.OrdinalIgnoreCase))
        {
            return $"Cálculo configurado → {field.FieldNameEs}";
        }

        return field.SourceDefinition.DataSourceType.NameEs;
    }

    private static string? TryReadCalculationType(string? expressionDsl)
    {
        try
        {
            using var document = JsonDocument.Parse(expressionDsl ?? "{}");
            return document.RootElement.TryGetProperty("calculationType", out var calculationType)
                ? calculationType.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static NachaConfigOperationalIssueDto HumanizeIssue(NachaConfigValidationIssueDto issue, CfgProfile profile)
    {
        var field = profile.LayoutVariants
            .SelectMany(variant => variant.Fields)
            .OrderByDescending(candidate => candidate.FieldCode.Length)
            .FirstOrDefault(candidate => ContainsToken(issue.Mensaje, candidate.FieldCode));
        var variant = field is not null
            ? profile.LayoutVariants.FirstOrDefault(candidate => candidate.Fields.Any(candidateField => candidateField.Id == field.Id))
            : profile.LayoutVariants
                .OrderByDescending(candidate => candidate.VariantCode.Length)
                .FirstOrDefault(candidate => ContainsToken(issue.Mensaje, candidate.VariantCode));
        var record = variant?.RecordCode;

        var fieldName = field?.FieldNameEs;
        var recordName = record?.NameEs;
        var message = issue.Codigo switch
        {
            "FIELD_OVERLAP" => $"Hay campos que ocupan posiciones superpuestas en {recordName ?? "el registro"}.",
            "INVALID_RECORD_LENGTH" => $"La longitud configurada para {recordName ?? "el registro"} no es válida.",
            "INVALID_FIELD_RANGE" => $"{fieldName ?? "Un campo"} se sale de los límites del registro.",
            "MISSING_SOURCE" => $"{fieldName ?? "Un campo obligatorio"} no tiene un origen de valor completo.",
            "EMPTY_VARIANT" => $"{recordName ?? "El registro"} no tiene campos habilitados en una de sus estructuras.",
            "INVALID_EXPRESSION_DSL" => $"El cálculo configurado para {fieldName ?? "un campo"} necesita corrección.",
            "INVALID_RULE_CONFIG" or "INVALID_FIELD_RULE_CONFIG" => $"Una regla de {fieldName ?? "un campo"} necesita corrección.",
            _ => HumanizeTechnicalMessage(issue.Mensaje, fieldName, recordName)
        };

        var suggestedAction = issue.Codigo switch
        {
            "FIELD_OVERLAP" or "INVALID_FIELD_RANGE" => "Ajusta la posición inicial o la longitud de los campos señalados y vuelve a probar.",
            "INVALID_RECORD_LENGTH" => "Revisa la longitud de la estructura seleccionada antes de publicar.",
            "MISSING_SOURCE" => "Selecciona un origen de valor permitido para el campo y vuelve a probar.",
            "INVALID_EXPRESSION_DSL" or "INVALID_RULE_CONFIG" or "INVALID_FIELD_RULE_CONFIG" => "Revisa la configuración del campo y sus reglas; no es necesario editar información técnica desde esta pantalla.",
            _ => null
        };

        return new NachaConfigOperationalIssueDto
        {
            Severity = issue.Severidad,
            Code = issue.Codigo,
            MessageEs = message,
            SuggestedActionEs = suggestedAction,
            RecordCode = record?.Code,
            RecordNameEs = recordName,
            FieldId = field?.Id,
            FieldNameEs = fieldName,
            TechnicalMessage = issue.Mensaje
        };
    }

    private static string HumanizeTechnicalMessage(string message, string? fieldName, string? recordName)
    {
        var humanized = message
            .Replace("Field ", "Campo ", StringComparison.OrdinalIgnoreCase)
            .Replace("field ", "campo ", StringComparison.OrdinalIgnoreCase)
            .Replace("record ", "registro ", StringComparison.OrdinalIgnoreCase)
            .Replace("layout", "estructura", StringComparison.OrdinalIgnoreCase)
            .Replace("source", "origen", StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(fieldName))
        {
            humanized = $"{fieldName}: {humanized}";
        }
        else if (!string.IsNullOrWhiteSpace(recordName))
        {
            humanized = $"{recordName}: {humanized}";
        }

        return humanized;
    }

    private static bool ContainsToken(string text, string token)
        => !string.IsNullOrWhiteSpace(token)
           && text.Contains(token, StringComparison.OrdinalIgnoreCase);

    private static NachaConfigSelectionTestDto BuildSelectionSummary(
        CfgProfile profile,
        NachaConfigResolverPreviewResultDto selection,
        DateTime processDateUtc)
    {
        var context = string.Join(" / ", new[]
        {
            profile.ClearingHouse.Name,
            profile.FlowType.NameEs,
            profile.Direction.NameEs,
            profile.ServiceClass?.NameEs
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
        var date = processDateUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        string summary;
        if (selection.Success && selection.ProfileId == profile.Id)
        {
            summary = $"Para {context} / fecha {date}, el backend resolverá esta configuración.";
        }
        else if (selection.Success)
        {
            summary = $"Para {context} / fecha {date}, el backend resolverá la configuración publicada {selection.ProfileCode}. La versión que estás probando no reemplaza esa autoridad hasta publicarse.";
        }
        else
        {
            summary = $"Para {context} / fecha {date}, no hay una configuración publicada vigente que el backend pueda resolver con ese contexto.";
        }

        return new NachaConfigSelectionTestDto
        {
            Success = selection.Success,
            SummaryEs = summary,
            ResolvedProfileId = selection.ProfileId,
            ResolvedProfileCode = selection.ProfileCode,
            WarningsEs = selection.Warnings,
            TechnicalTrace = selection.Trace
        };
    }

    private static DateTime ResolveProcessDate(CfgProfile profile)
    {
        var today = DateTime.UtcNow.Date;
        if (today < profile.EffectiveFrom.Date)
        {
            return profile.EffectiveFrom.Date;
        }

        if (profile.EffectiveTo.HasValue && today > profile.EffectiveTo.Value.Date)
        {
            return profile.EffectiveTo.Value.Date;
        }

        return today;
    }
}
