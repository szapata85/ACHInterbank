using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Internal deterministic renderer for one official table-driven NACHA-M record.
/// It owns field source resolution, configured field-rule execution, fixed-width rendering,
/// and field-level generation trace creation. It has no IO, persistence, DI, or rail policy.
/// </summary>
internal interface INachaOfficialRecordRenderer
{
    string Render(
        string recordCode,
        object record,
        CfgLayoutVariant layout,
        NachaGenerationAuditResult audit,
        NachaOfficialRecordRenderContext renderContext,
        int recordSequence,
        int lineNumber);
}

internal sealed record NachaOfficialRecordRenderContext(
    string? RuleChamberCode,
    string? RuleMetadataChamberCode,
    string? AmountFormattingChamberCode);

internal sealed class NachaOfficialRecordRenderer : INachaOfficialRecordRenderer
{
    public string Render(
        string recordCode,
        object record,
        CfgLayoutVariant layout,
        NachaGenerationAuditResult audit,
        NachaOfficialRecordRenderContext renderContext,
        int recordSequence,
        int lineNumber)
    {
        var buffer = new char[layout.TotalLength];
        Array.Fill(buffer, ' ');
        var recordEntries = new List<NachaGenerationTraceEntry>();

        foreach (var field in layout.Fields.Where(x => x.IsEnabled).OrderBy(x => x.StartPosition))
        {
            object? raw = null;
            string? value = null;
            try
            {
                raw = ResolveOfficialRawValue(recordCode, record, field);
                value = FormatOfficialValue(raw, field, renderContext.AmountFormattingChamberCode);
                value = ExecuteOfficialFieldRules(recordCode, field, value, renderContext.RuleChamberCode, renderContext.RuleMetadataChamberCode);

                value = field.Justification == 'R'
                    ? value.PadLeft(field.Length, field.PadChar)
                    : value.PadRight(field.Length, field.PadChar);

                ValidateRenderedOfficialField(recordCode, field, value, renderContext.RuleChamberCode);

                value.CopyTo(0, buffer, field.StartPosition - 1, value.Length);
                recordEntries.Add(CreateOfficialTraceEntry(audit, recordCode, recordSequence, lineNumber, layout, field, raw, value, "Ok", null, null));
            }
            catch (NachaGenerationException ex)
            {
                audit.FieldTraceEntries.Add(CreateOfficialTraceEntry(audit, recordCode, recordSequence, lineNumber, layout, field, raw, value, "Failed", ex.Code, ex.Message));
                throw;
            }
        }

        var line = new string(buffer);
        var lineEvidence = BuildSafeRecordEvidence(recordCode, line);
        foreach (var entry in recordEntries)
        {
            entry.GeneratedLinePreviewSanitized = lineEvidence;
            audit.FieldTraceEntries.Add(entry);
        }

        return line;
    }

    private static string ExecuteOfficialFieldRules(
        string recordCode,
        CfgLayoutField field,
        string value,
        string? clearingHouseCode,
        string? ruleMetadataChamberCode)
    {
        var rules = field.Rules.Where(rule => rule.IsEnabled).OrderBy(rule => rule.Order).ToList();
        if (rules.Count == 0)
        {
            throw BuildRuleException(
                "NACHA_REQUIRED_RULE_MISSING",
                "NACHA-RULE-METADATA",
                clearingHouseCode,
                recordCode,
                field,
                "CfgFieldRule ejecutable ausente.");
        }

        var current = value;
        foreach (var rule in rules)
        {
            FieldRuleRuntimeConfig config;
            try
            {
                config = ParseFieldRuleRuntimeConfig(rule, field, recordCode, ruleMetadataChamberCode);
            }
            catch (JsonException)
            {
                throw BuildRuleException(
                    "NACHA_RULE_CONFIG_INVALID",
                    rule.RuleCode,
                    clearingHouseCode,
                    recordCode,
                    field,
                    "RuleConfigJson no es válido.");
            }

            current = config.Normalizer switch
            {
                "NONE" => current,
                "TRIM" => current.Trim(),
                "UPPER" => current.ToUpperInvariant(),
                "TRIM_UPPER" => current.Trim().ToUpperInvariant(),
                _ => throw BuildRuleException(
                    "NACHA_NORMALIZER_NOT_ALLOWED",
                    rule.RuleCode,
                    clearingHouseCode,
                    recordCode,
                    field,
                    "Normalizador no autorizado por el motor oficial.")
            };

            var isBlank = string.IsNullOrWhiteSpace(current);
            if (config.Required && isBlank)
            {
                throw BuildRuleException("NACHA_REQUIRED_FIELD_MISSING", rule.RuleCode, clearingHouseCode, recordCode, field, "Valor obligatorio ausente.");
            }

            if (!isBlank)
            {
                ValidateOfficialDataType(config, current, rule.RuleCode, clearingHouseCode, recordCode, field);
                ValidateAllowedRepertoire(config, current, rule.RuleCode, clearingHouseCode, recordCode, field);

                if (config.AllowedValues.Count > 0 && !config.AllowedValues.Contains(current, StringComparer.OrdinalIgnoreCase))
                {
                    throw BuildRuleException("NACHA_ALLOWED_VALUE_INVALID", rule.RuleCode, clearingHouseCode, recordCode, field, "Valor fuera del catálogo permitido.");
                }
            }

            if (current.Length > field.Length)
            {
                throw BuildRuleException("NACHA_FIELD_LENGTH_INVALID", rule.RuleCode, clearingHouseCode, recordCode, field, "Overflow; la política oficial es REJECT.");
            }

            if (!string.Equals(config.OverflowPolicy, "REJECT", StringComparison.OrdinalIgnoreCase))
            {
                throw BuildRuleException("NACHA_OVERFLOW_POLICY_INVALID", rule.RuleCode, clearingHouseCode, recordCode, field, "La ruta oficial exige overflowPolicy=REJECT.");
            }
        }

        return current;
    }

    private static void ValidateOfficialDataType(
        FieldRuleRuntimeConfig config,
        string value,
        string ruleId,
        string? clearingHouseCode,
        string recordCode,
        CfgLayoutField field)
    {
        var valid = config.DataType switch
        {
            "NUMERIC" => value.All(char.IsDigit),
            "DATE" => DateTime.TryParseExact(value, config.Format ?? "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            "TIME" => DateTime.TryParseExact(value, config.Format ?? "HHmm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            "RESERVED" => value.All(char.IsWhiteSpace),
            "ALPHANUMERIC" => true,
            _ => false
        };

        if (!valid)
        {
            throw BuildRuleException("NACHA_FIELD_TYPE_INVALID", ruleId, clearingHouseCode, recordCode, field, "Tipo o formato del campo inválido.");
        }
    }

    private static void ValidateAllowedRepertoire(
        FieldRuleRuntimeConfig config,
        string value,
        string ruleId,
        string? clearingHouseCode,
        string recordCode,
        CfgLayoutField field)
    {
        if (config.DataType is "NUMERIC" or "DATE" or "TIME" or "RESERVED")
        {
            return;
        }

        const string permittedSpecials = " .,:;-*/&#$%=";
        if (value.Any(character => char.IsControl(character)
                                   || character > 0x7F
                                   || (!(character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9')
                                       && !permittedSpecials.Contains(character))))
        {
            throw BuildRuleException("NACHA_CHARACTER_REPERTOIRE_INVALID", ruleId, clearingHouseCode, recordCode, field, "El campo contiene caracteres fuera del repertorio permitido.");
        }
    }

    private static void ValidateRenderedOfficialField(
        string recordCode,
        CfgLayoutField field,
        string rendered,
        string? clearingHouseCode)
    {
        if (rendered.Length != field.Length)
        {
            throw BuildRuleException("NACHA_RENDERED_LENGTH_INVALID", ResolveRuleId(field), clearingHouseCode, recordCode, field, "Longitud renderizada distinta de la longitud configurada.");
        }

        if (field.Justification == 'R'
            && field.PadChar == '0'
            && !string.IsNullOrWhiteSpace(rendered)
            && rendered.Any(character => !char.IsDigit(character)))
        {
            throw BuildRuleException("NACHA_NUMERIC_RENDER_INVALID", ResolveRuleId(field), clearingHouseCode, recordCode, field, "Campo numérico renderizado contiene caracteres no numéricos.");
        }
    }

    private static FieldRuleRuntimeConfig ParseFieldRuleRuntimeConfig(CfgFieldRule rule, CfgLayoutField field, string recordCode, string? ruleMetadataChamberCode)
    {
        using var document = JsonDocument.Parse(rule.RuleConfigJson ?? "{}");
        var root = document.RootElement;
        var configuredRecord = ReadJsonString(root, "recordType");
        var configuredField = ReadJsonString(root, "field");
        var configuredStart = root.TryGetProperty("startPosition", out var start) ? start.GetInt32() : 0;
        var configuredLength = root.TryGetProperty("length", out var length) ? length.GetInt32() : 0;
        if (!string.Equals(configuredRecord, recordCode, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(configuredField, field.FieldCode, StringComparison.OrdinalIgnoreCase)
            || configuredStart != field.StartPosition
            || configuredLength != field.Length)
        {
            throw BuildRuleException("NACHA_RULE_METADATA_MISMATCH", rule.RuleCode, ruleMetadataChamberCode, recordCode, field, "La metadata de CfgFieldRule no corresponde al descriptor renderizado.");
        }

        var allowedValues = root.TryGetProperty("allowedValues", out var allowed) && allowed.ValueKind == JsonValueKind.Array
            ? allowed.EnumerateArray().Select(item => item.GetString()).Where(item => item is not null).Cast<string>().ToArray()
            : Array.Empty<string>();

        return new FieldRuleRuntimeConfig(
            ReadJsonString(root, "dataType").ToUpperInvariant(),
            root.TryGetProperty("required", out var required) && required.GetBoolean(),
            ReadJsonString(root, "normalizer", "NONE").ToUpperInvariant(),
            ReadJsonString(root, "overflowPolicy", "REJECT").ToUpperInvariant(),
            ReadJsonString(root, "format", null),
            allowedValues);
    }

    private static string ReadJsonString(JsonElement root, string propertyName, string? defaultValue = "")
        => root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? defaultValue ?? string.Empty
            : defaultValue ?? string.Empty;

    private static string ResolveRuleId(CfgLayoutField field)
        => field.Rules.FirstOrDefault(rule => rule.IsEnabled)?.RuleCode ?? "NACHA-RULE-METADATA";

    private static NachaGenerationException BuildRuleException(
        string code,
        string ruleId,
        string? clearingHouseCode,
        string recordCode,
        CfgLayoutField field,
        string cause)
        => new(
            code,
            "El campo no cumple la política oficial NACHA-M.",
            ruleId,
            clearingHouseCode,
            recordCode,
            field.FieldCode,
            cause,
            field.StartPosition,
            field.Length);


    private static string BuildSafeRecordEvidence(string recordCode, string line)
        => $"RecordType={recordCode};Length={line.Length};SHA256={Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(line)))[..16]}";

    private sealed record FieldRuleRuntimeConfig(
        string DataType,
        bool Required,
        string Normalizer,
        string OverflowPolicy,
        string? Format,
        IReadOnlyList<string> AllowedValues);

    private static NachaGenerationTraceEntry CreateOfficialTraceEntry(
        NachaGenerationAuditResult audit,
        string recordCode,
        int recordSequence,
        int lineNumber,
        CfgLayoutVariant layout,
        CfgLayoutField field,
        object? raw,
        string? rendered,
        string validationStatus,
        string? errorCode,
        string? errorMessage)
    {
        var source = field.SourceDefinition;
        var sourceType = NormalizeTraceSourceType(source.DataSourceType.Code);
        var calculationType = string.Equals(source.DataSourceType.Code, "EXPRESION", StringComparison.OrdinalIgnoreCase)
            ? TryResolveCalculationType(source.ExpressionDsl)
            : null;

        return new NachaGenerationTraceEntry
        {
            TraceId = audit.TraceId,
            RecordType = recordCode,
            RecordSequence = recordSequence,
            LineNumber = lineNumber,
            LayoutVariantId = layout.Id,
            LayoutVariantCode = layout.VariantCode,
            FieldDefinitionId = field.Id,
            FieldName = field.FieldCode,
            DisplayName = field.FieldNameEs,
            PositionStart = field.StartPosition,
            PositionEnd = field.StartPosition + field.Length - 1,
            Length = field.Length,
            DataType = ResolveConfiguredTraceDataType(field, raw),
            Required = ResolveConfiguredRequired(field),
            SourceType = sourceType,
            SourceFieldPath = source.PropertyPath,
            ConstantValueSanitized = source.ConstantValue is null ? null : SanitizeTraceValue(source.ConstantValue, field),
            CalculationType = calculationType,
            TransformationApplied = field.FormatMask,
            PaddingDirection = field.Justification == 'R' ? "Left" : "Right",
            PaddingChar = field.PadChar.ToString(),
            RawValueSanitized = SanitizeTraceValue(raw, field),
            RenderedValue = SanitizeTraceValue(rendered, field),
            RuntimeRenderedValue = rendered,
            RenderedLength = rendered?.Length ?? 0,
            ValidationStatus = validationStatus,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            ValueStartIndex = field.StartPosition,
            ValueEndIndex = field.StartPosition + field.Length - 1
        };
    }

    private static string NormalizeTraceSourceType(string sourceType)
    {
        if (string.Equals(sourceType, "CONSTANTE", StringComparison.OrdinalIgnoreCase))
        {
            return "CONSTANT";
        }

        if (string.Equals(sourceType, "ENTIDAD", StringComparison.OrdinalIgnoreCase))
        {
            return "SOURCE_FIELD";
        }

        if (string.Equals(sourceType, "EXPRESION", StringComparison.OrdinalIgnoreCase))
        {
            return "CALCULATED";
        }

        return sourceType.ToUpperInvariant();
    }

    private static string InferTraceDataType(CfgLayoutField field, object? raw)
    {
        if (raw is DateTime or DateOnly)
        {
            return "date";
        }

        if (raw is decimal or double or float)
        {
            return "decimal";
        }

        if (raw is int or long or short)
        {
            return "integer";
        }

        return field.FieldCode.Contains("AMOUNT", StringComparison.OrdinalIgnoreCase)
            || field.FieldCode.Contains("TOTAL", StringComparison.OrdinalIgnoreCase)
            || field.FieldCode.Contains("COUNT", StringComparison.OrdinalIgnoreCase)
            || field.FieldCode.Contains("HASH", StringComparison.OrdinalIgnoreCase)
            ? "numeric"
            : "string";
    }

    private static string ResolveConfiguredTraceDataType(CfgLayoutField field, object? raw)
    {
        var configured = ReadRuleConfigString(field, "dataType");
        return string.IsNullOrWhiteSpace(configured)
            ? InferTraceDataType(field, raw)
            : configured.ToLowerInvariant();
    }

    private static bool ResolveConfiguredRequired(CfgLayoutField field)
    {
        foreach (var rule in field.Rules.Where(rule => rule.IsEnabled))
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
                return false;
            }
        }

        return false;
    }

    private static string ReadRuleConfigString(CfgLayoutField field, string propertyName)
    {
        foreach (var rule in field.Rules.Where(rule => rule.IsEnabled))
        {
            try
            {
                using var document = JsonDocument.Parse(rule.RuleConfigJson ?? "{}");
                if (document.RootElement.TryGetProperty(propertyName, out var value)
                    && value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString() ?? string.Empty;
                }
            }
            catch (JsonException)
            {
                return string.Empty;
            }
        }

        return string.Empty;
    }

    private static string? SanitizeTraceValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var text = value switch
        {
            DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
            DateOnly dateOnly => dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        } ?? string.Empty;

        text = text.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);

        if (text.Contains("Bearer ", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Authorization", StringComparison.OrdinalIgnoreCase)
            || text.Contains("BEGIN PRIVATE", StringComparison.OrdinalIgnoreCase)
            || text.Contains("password", StringComparison.OrdinalIgnoreCase)
            || text.Contains("token", StringComparison.OrdinalIgnoreCase))
        {
            return "[SANITIZED]";
        }

        return text.Length <= 140 ? text : text[..140];
    }

    private static string? SanitizeTraceValue(object? value, CfgLayoutField field)
    {
        if (value is null)
        {
            return null;
        }

        var sensitivity = ReadRuleConfigString(field, "sensitivity");
        if (string.IsNullOrWhiteSpace(sensitivity) && IsSensitiveNachaField(field.FieldCode))
        {
            sensitivity = "FINANCIAL_OR_PERSONAL";
        }

        if (!string.IsNullOrWhiteSpace(sensitivity)
            && !string.Equals(sensitivity, "NONE", StringComparison.OrdinalIgnoreCase))
        {
            var length = Convert.ToString(value, CultureInfo.InvariantCulture)?.Length ?? 0;
            return $"[REDACTED;Category={sensitivity.ToUpperInvariant()};Length={length}]";
        }

        return SanitizeTraceValue(value);
    }

    private static bool IsSensitiveNachaField(string fieldCode)
    {
        var normalized = fieldCode.Replace("_", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        return new[]
        {
            "ACCOUNT", "NAME", "IDENTIFICATION", "AMOUNT", "TRACE", "REFERENCE", "CUSTOMER", "CLIENT",
            "IMMEDIATEORIGIN", "IMMEDIATEDESTINATION", "ORIGINATINGDFI", "RECEIVINGDFI", "BATCHNUMBER"
        }.Any(token => normalized.Contains(token, StringComparison.Ordinal));
    }

    private object? ResolveOfficialRawValue(string recordCode, object record, CfgLayoutField field)
    {
        var source = field.SourceDefinition;
        var sourceType = source.DataSourceType.Code;
        if (string.Equals(sourceType, "CONSTANTE", StringComparison.OrdinalIgnoreCase))
        {
            return source.ConstantValue ?? string.Empty;
        }

        if (string.Equals(sourceType, "ENTIDAD", StringComparison.OrdinalIgnoreCase))
        {
            if (TryResolveOfficialValue(record, source.PropertyPath, out var raw))
            {
                return raw;
            }

            throw new NachaGenerationException("NACHA_FIELD_SOURCE_NOT_FOUND", $"No se encontró sourceFieldPath {source.PropertyPath} para campo {field.FieldCode} en RecordCode={recordCode}.");
        }

        if (string.Equals(sourceType, "EXPRESION", StringComparison.OrdinalIgnoreCase))
        {
            var calculationType = ResolveCalculationType(source.ExpressionDsl);
            if (string.Equals(calculationType, "Filler", StringComparison.OrdinalIgnoreCase))
            {
                return new string(' ', field.Length);
            }

            if (string.Equals(calculationType, "JulianSettlementDate", StringComparison.OrdinalIgnoreCase)
                && TryResolveOfficialValue(record, "EffectiveEntryDate", out var effectiveEntryDate))
            {
                var date = effectiveEntryDate switch
                {
                    DateOnly dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
                    DateTimeOffset dateTimeOffset => dateTimeOffset.DateTime,
                    DateTime dateTime => dateTime,
                    _ => Convert.ToDateTime(effectiveEntryDate, CultureInfo.InvariantCulture)
                };
                return date.DayOfYear.ToString("D3", CultureInfo.InvariantCulture);
            }

            if (TryResolveOfficialValue(record, calculationType, out var raw))
            {
                return raw;
            }

            throw new NachaGenerationException("NACHA_CALCULATION_FAILED", $"No se pudo resolver cálculo {calculationType} para campo {field.FieldCode} en RecordCode={recordCode}.");
        }

        throw new NachaGenerationException("NACHA_FIELD_SOURCE_NOT_FOUND", $"SourceType {sourceType} no soportado en modo oficial para campo {field.FieldCode}.");
    }

    private static string ResolveCalculationType(string? expressionDsl)
    {
        try
        {
            using var document = JsonDocument.Parse(expressionDsl ?? "{}");
            if (document.RootElement.TryGetProperty("calculationType", out var property))
            {
                var value = property.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }
        catch (JsonException)
        {
            throw new NachaGenerationException("NACHA_CALCULATION_FAILED", "ExpressionDsl inválido; detalle interno omitido.");
        }

        throw new NachaGenerationException("NACHA_CALCULATION_FAILED", "ExpressionDsl no declara calculationType.");
    }

    private static string? TryResolveCalculationType(string? expressionDsl)
    {
        try
        {
            using var document = JsonDocument.Parse(expressionDsl ?? "{}");
            return document.RootElement.TryGetProperty("calculationType", out var property)
                ? property.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryResolveOfficialValue(object record, string? path, out object? raw)
    {
        raw = null;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (record is IReadOnlyDictionary<string, object?> values)
        {
            return NachaDeterministicFieldFormatter.TryResolveValue(values, path, out raw)
                   || NachaDeterministicFieldFormatter.TryResolveValue(values, ResolveOfficialAlias(path), out raw);
        }

        var property = NachaDeterministicFieldFormatter.ResolveProperty(record.GetType(), path)
                       ?? NachaDeterministicFieldFormatter.ResolveProperty(record.GetType(), ResolveOfficialAlias(path));
        if (property is null)
        {
            return false;
        }

        raw = property.GetValue(record);
        return true;
    }

    private static string ResolveOfficialAlias(string path)
    {
        return NachaDeterministicFieldFormatter.NormalizeIdentifier(path) switch
        {
            "ORIGINATINGDFI" => "OriginatingDFI",
            "ORIGINATINGDFIID" => "OriginatingDFI",
            "ORIGINATINGDFICODE" => "OriginatingDFI",
            "ORIGINATINGDFINUMBER" => "OriginatingDFI",
            "RECEIVERCUSTOMERCODE" => "RecipientIdNumber",
            "INDIVIDUALNAME" => "ReceiverName",
            "ADDENDARECORDINDICATOR" => "AddendumIndicator",
            "PAYMENTRELATEDINFORMATION" => "Purpose",
            _ => path
        };
    }

    private static string FormatOfficialValue(object? raw, CfgLayoutField field, string? amountFormattingChamberCode)
    {
        if (raw is null)
        {
            return string.Empty;
        }

        if (raw is string text)
        {
            return text;
        }

        if (raw is DateTime date)
        {
            return date.ToString(field.FormatMask ?? "yyyyMMdd", CultureInfo.InvariantCulture);
        }

        if (raw is DateOnly dateOnly)
        {
            return dateOnly.ToString(field.FormatMask ?? "yyyyMMdd", CultureInfo.InvariantCulture);
        }

        if (raw is decimal decimalValue)
        {
            return FormatAmountInCents(decimalValue, field, amountFormattingChamberCode);
        }

        if (raw is double doubleValue)
        {
            if (double.IsNaN(doubleValue) || double.IsInfinity(doubleValue))
            {
                throw BuildRuleException("NACHA_FIELD_TYPE_INVALID", ResolveRuleId(field), amountFormattingChamberCode, field.LayoutVariant?.RecordCode?.Code ?? string.Empty, field, "Monto no finito.");
            }

            return FormatAmountInCents((decimal)doubleValue, field, amountFormattingChamberCode);
        }

        if (raw is float floatValue)
        {
            if (float.IsNaN(floatValue) || float.IsInfinity(floatValue))
            {
                throw BuildRuleException("NACHA_FIELD_TYPE_INVALID", ResolveRuleId(field), amountFormattingChamberCode, field.LayoutVariant?.RecordCode?.Code ?? string.Empty, field, "Monto no finito.");
            }

            return FormatAmountInCents((decimal)floatValue, field, amountFormattingChamberCode);
        }

        if (raw is IFormattable formattable)
        {
            return formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        return raw.ToString() ?? string.Empty;
    }

    private static string FormatAmountInCents(decimal amount, CfgLayoutField field, string? amountFormattingChamberCode)
    {
        decimal cents;
        try
        {
            cents = checked(amount * 100m);
        }
        catch (OverflowException)
        {
            throw BuildRuleException("NACHA_FIELD_LENGTH_INVALID", ResolveRuleId(field), amountFormattingChamberCode, field.LayoutVariant?.RecordCode?.Code ?? string.Empty, field, "Overflow monetario.");
        }

        if (amount < 0m || cents != decimal.Truncate(cents))
        {
            throw BuildRuleException("NACHA_FIELD_TYPE_INVALID", ResolveRuleId(field), amountFormattingChamberCode, field.LayoutVariant?.RecordCode?.Code ?? string.Empty, field, "El monto debe ser no negativo y expresable en centavos exactos.");
        }

        try
        {
            return checked((long)cents).ToString(CultureInfo.InvariantCulture);
        }
        catch (OverflowException)
        {
            throw BuildRuleException("NACHA_FIELD_LENGTH_INVALID", ResolveRuleId(field), amountFormattingChamberCode, field.LayoutVariant?.RecordCode?.Code ?? string.Empty, field, "Overflow monetario.");
        }
    }

}
