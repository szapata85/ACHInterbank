using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Mapping;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Mapping;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaConfigValidationService : INachaConfigValidationService
{
    private static readonly HashSet<string> RequiredRecordCodes = ["1", "5", "6", "8", "9"];
    private static readonly string[] Record1RequiredFields =
    [
        "PRIORITYCODE", "IMMEDIATEDESTINATION", "IMMEDIATEORIGIN", "FILECREATIONDATE", "FILECREATIONTIME",
        "FILEIDMODIFIER", "RECORDSIZE", "BLOCKINGFACTOR", "FORMATCODE", "IMMEDIATEDESTINATIONNAME", "IMMEDIATEORIGINNAME", "REFERENCECODE"
    ];
    private static readonly string[] Record5RequiredFields =
    [
        "SERVICECLASSCODE", "COMPANYNAME", "COMPANYIDENTIFICATION", "STANDARDENTRYCLASSCODE", "EFFECTIVEENTRYDATE", "SETTLEMENTDATE"
    ];
    private static readonly string[] Record8RequiredFields =
    [
        "SERVICECLASSCODE", "ENTRYADDENDACOUNT", "ENTRYHASH", "TOTALDEBITAMOUNT", "TOTALCREDITAMOUNT", "COMPANYIDENTIFICATION", "ORIGINATINGDFI", "BATCHNUMBER"
    ];
    private static readonly string[] Record9RequiredFields =
    [
        "BATCHCOUNT", "BLOCKCOUNT", "ENTRYADDENDACOUNT", "ENTRYHASH", "TOTALDEBITAMOUNT", "TOTALCREDITAMOUNT"
    ];
    private readonly AchDbContext _context;
    private readonly INachaCanonicalMapper _canonicalMapper;

    public NachaConfigValidationService(AchDbContext context, INachaCanonicalMapper? canonicalMapper = null)
    {
        _context = context;
        _canonicalMapper = canonicalMapper ?? new NachaCanonicalMapper();
    }

    public async Task<NachaConfigValidationResultDto> ValidateBeforePublishAsync(int profileId, CancellationToken ct = default)
    {
        var issues = new List<NachaConfigValidationIssueDto>();
        var profile = await _context.CfgProfiles
            .AsNoTracking()
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
            .Include(x => x.ClearingHouse)
            .Include(x => x.Tags)
            .FirstOrDefaultAsync(x => x.Id == profileId, ct);

        if (profile is null)
        {
            issues.Add(new NachaConfigValidationIssueDto { Severidad = "ERROR", Codigo = "PROFILE_NOT_FOUND", Mensaje = "Perfil no existe." });
            return Build(profileId, issues);
        }

        var configuredCodes = profile.Records.Where(x => x.IsEnabled).Select(x => x.RecordCode.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var required in RequiredRecordCodes)
        {
            if (!configuredCodes.Contains(required))
            {
                issues.Add(new NachaConfigValidationIssueDto { Severidad = "ERROR", Codigo = "MISSING_RECORD", Mensaje = $"Falta record obligatorio {required}." });
            }
        }

        var enabledRecords = profile.Records.Where(x => x.IsEnabled).ToList();
        if (enabledRecords.GroupBy(x => x.Sequence).Any(g => g.Count() > 1))
        {
            issues.Add(new NachaConfigValidationIssueDto { Severidad = "ERROR", Codigo = "INVALID_SEQUENCE", Mensaje = "Existen records habilitados con secuencia duplicada." });
        }

        if (enabledRecords.Count == 0)
        {
            issues.Add(new NachaConfigValidationIssueDto { Severidad = "ERROR", Codigo = "NO_ENABLED_RECORDS", Mensaje = "El perfil no tiene records habilitados." });
        }

        var ambiguousLayouts = profile.LayoutVariants
            .GroupBy(x => x.RecordCode.Code)
            .Where(g => g.Count(v => v.IsDefaultForRecord) > 1)
            .ToList();
        foreach (var ambiguous in ambiguousLayouts)
        {
            issues.Add(new NachaConfigValidationIssueDto { Severidad = "ERROR", Codigo = "AMBIGUOUS_LAYOUT", Mensaje = $"Más de un layout default para record {ambiguous.Key}." });
        }

        var missingVariant = enabledRecords
            .Where(r => !profile.LayoutVariants.Any(v => v.RecordCodeId == r.RecordCodeId))
            .Select(r => r.RecordCode.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var code in missingVariant)
        {
            issues.Add(new NachaConfigValidationIssueDto { Severidad = "ERROR", Codigo = "MISSING_VARIANT", Mensaje = $"No hay variantes configuradas para record {code}." });
        }

        var chamberCode = ResolveChamberCode(profile.ClearingHouse?.Code, profile.ClearingHouse?.Name);
        foreach (var variant in profile.LayoutVariants)
        {
            var ordered = variant.Fields.Where(f => f.IsEnabled).OrderBy(f => f.StartPosition).ToList();
            var canonicalUsages = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            if (ordered.Count == 0)
            {
                issues.Add(new NachaConfigValidationIssueDto
                {
                    Severidad = "ERROR",
                    Codigo = "EMPTY_VARIANT",
                    Mensaje = $"La variante {variant.VariantCode} no tiene fields habilitados."
                });
                continue;
            }

            for (var i = 1; i < ordered.Count; i++)
            {
                var prev = ordered[i - 1];
                var current = ordered[i];
                if (current.StartPosition < prev.StartPosition + prev.Length)
                {
                    issues.Add(new NachaConfigValidationIssueDto
                    {
                        Severidad = "ERROR",
                        Codigo = "FIELD_OVERLAP",
                        Mensaje = $"Solapamiento de fields en variante {variant.VariantCode}: {prev.FieldCode} y {current.FieldCode}."
                    });
                }
            }

            foreach (var field in ordered)
            {
                if (field.SourceDefinitionId == 0 || (string.IsNullOrWhiteSpace(field.SourceDefinition.PropertyPath)
                    && string.IsNullOrWhiteSpace(field.SourceDefinition.ConstantValue)
                    && string.IsNullOrWhiteSpace(field.SourceDefinition.ExpressionDsl)))
                {
                    issues.Add(new NachaConfigValidationIssueDto
                    {
                        Severidad = "ERROR",
                        Codigo = "MISSING_SOURCE",
                        Mensaje = $"Field {field.FieldCode} sin source crítico."
                    });
                }
                var sourceType = field.SourceDefinition.DataSourceType?.Code ?? string.Empty;
                ValidateCanonicalPath(issues, variant.RecordCode.Code, field, sourceType, canonicalUsages);
                if (sourceType is "SQL_VIEW" or "SQL_PROCEDURE")
                {
                    issues.Add(new NachaConfigValidationIssueDto
                    {
                        Severidad = "ERROR",
                        Codigo = "UNSUPPORTED_SOURCE_TYPE",
                        Mensaje = $"Field {field.FieldCode} usa source {sourceType} no soportado en fase 1."
                    });
                }

                if (!string.IsNullOrWhiteSpace(field.SourceDefinition.ExpressionDsl))
                {
                    try
                    {
                        JsonDocument.Parse(field.SourceDefinition.ExpressionDsl);
                    }
                    catch
                    {
                        issues.Add(new NachaConfigValidationIssueDto
                        {
                            Severidad = "ERROR",
                            Codigo = "INVALID_EXPRESSION_DSL",
                            Mensaje = $"Field {field.FieldCode} tiene ExpressionDsl inválido."
                        });
                    }
                }

                if (!string.IsNullOrWhiteSpace(field.TransformationPipelineJson))
                {
                    try
                    {
                        using var pipelineDoc = JsonDocument.Parse(field.TransformationPipelineJson);
                        var steps = pipelineDoc.RootElement.ValueKind == JsonValueKind.Array
                            ? pipelineDoc.RootElement.EnumerateArray()
                            : pipelineDoc.RootElement.TryGetProperty("steps", out var arr) && arr.ValueKind == JsonValueKind.Array
                                ? arr.EnumerateArray()
                                : Enumerable.Empty<JsonElement>();

                        foreach (var step in steps)
                        {
                            if (!step.TryGetProperty("type", out var typeElement))
                            {
                                continue;
                            }

                            var type = typeElement.GetString()?.ToLowerInvariant();
                            if (type is not ("trim" or "upper" or "lower" or "truncate" or "substring" or "remove_non_digits" or "replace" or "null_to_default"))
                            {
                                issues.Add(new NachaConfigValidationIssueDto
                                {
                                    Severidad = "ERROR",
                                    Codigo = "UNSUPPORTED_TRANSFORM_TYPE",
                                    Mensaje = $"Field {field.FieldCode} usa transform {type} no soportado en fase 1."
                                });
                            }

                            if (chamberCode == "ACH" && type is "truncate" or "substring" or "null_to_default")
                            {
                                issues.Add(new NachaConfigValidationIssueDto
                                {
                                    Severidad = "ERROR",
                                    Codigo = "OFFICIAL_TRANSFORM_FAIL_CLOSED",
                                    Mensaje = $"Field {field.FieldCode} usa una transformación silenciosa prohibida para ACHCOL oficial."
                                });
                            }
                        }
                    }
                    catch
                    {
                        issues.Add(new NachaConfigValidationIssueDto
                        {
                            Severidad = "ERROR",
                            Codigo = "INVALID_TRANSFORM_PIPELINE",
                            Mensaje = $"Field {field.FieldCode} tiene TransformationPipelineJson inválido."
                        });
                    }
                }


                if (!string.IsNullOrWhiteSpace(field.SourceDefinition.FallbackPolicyJson))
                {
                    try
                    {
                        using var fallbackDoc = JsonDocument.Parse(field.SourceDefinition.FallbackPolicyJson);
                        if (fallbackDoc.RootElement.TryGetProperty("steps", out var steps) && steps.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var step in steps.EnumerateArray())
                            {
                                var stepType = step.TryGetProperty("type", out var stepTypeElement)
                                    ? stepTypeElement.GetString()?.ToLowerInvariant()
                                    : string.Empty;

                                if (stepType is not ("default" or "coalesce" or "null_if_missing" or "alias" or "secondary_source"))
                                {
                                    issues.Add(new NachaConfigValidationIssueDto
                                    {
                                        Severidad = "ERROR",
                                        Codigo = "UNSUPPORTED_FALLBACK_STEP",
                                        Mensaje = $"Field {field.FieldCode} usa fallback step {stepType} no soportado en fase 1."
                                    });
                                }
                            }
                        }
                    }
                    catch
                    {
                        issues.Add(new NachaConfigValidationIssueDto
                        {
                            Severidad = "ERROR",
                            Codigo = "INVALID_FALLBACK_POLICY",
                            Mensaje = $"Field {field.FieldCode} tiene FallbackPolicyJson inválido."
                        });
                    }
                }

                foreach (var rule in field.Rules.Where(r => r.IsEnabled))
                {
                    var ruleType = rule.RuleType?.Code ?? string.Empty;
                    if (ruleType is not ("REQUIRED" or "REGEX" or "RANGE" or "ENUM" or "DATE_FORMAT"))
                    {
                        issues.Add(new NachaConfigValidationIssueDto
                        {
                            Severidad = "ERROR",
                            Codigo = "UNSUPPORTED_RULE_TYPE",
                            Mensaje = $"Field {field.FieldCode} usa rule type {ruleType} no soportado en fase 1."
                        });
                    }
                }
            }

            foreach (var usage in canonicalUsages.Where(x => x.Value.Count > 1))
            {
                issues.Add(new NachaConfigValidationIssueDto
                {
                    Severidad = "ERROR",
                    Codigo = "AMBIGUOUS_ALIAS",
                    Mensaje = $"Colisión de alias/canonical en record {variant.RecordCode.Code} para '{usage.Key}': {string.Join(", ", usage.Value)}."
                });
            }

            ValidateHeaderNormativeRequirements(issues, chamberCode, variant, ordered);
            if (chamberCode == "ACH")
            {
                ValidateAchColExactLayoutAndRules(issues, variant, ordered);
            }
        }

        if (chamberCode == "CENIT")
        {
            issues.Add(new NachaConfigValidationIssueDto
            {
                Severidad = "ERROR",
                Codigo = "CENIT_NOT_HOMOLOGATED",
                Mensaje = "CENIT permanece NO-GO / NOT HOMOLOGATED; no puede aprobarse ni publicarse para LIVE."
            });
        }

        var conflictingEffective = await _context.CfgProfiles
            .AsNoTracking()
            .AnyAsync(x => x.Id != profile.Id
                           && x.ClearingHouseId == profile.ClearingHouseId
                           && x.FlowTypeId == profile.FlowTypeId
                           && x.DirectionId == profile.DirectionId
                           && x.ServiceClassId == profile.ServiceClassId
                           && x.Status.Code == "PUBLICADO"
                           && x.EffectiveFrom <= (profile.EffectiveTo ?? DateTime.MaxValue)
                           && (x.EffectiveTo ?? DateTime.MaxValue) >= profile.EffectiveFrom, ct);

        if (conflictingEffective)
        {
            issues.Add(new NachaConfigValidationIssueDto { Severidad = "ERROR", Codigo = "EFFECTIVE_CONFLICT", Mensaje = "Conflicto de vigencia con perfil publicado." });
        }

        return Build(profileId, issues);
    }

    private static readonly Dictionary<string, int> Record1FieldLengths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PRIORITYCODE"] = 2,
        ["IMMEDIATEDESTINATION"] = 10,
        ["IMMEDIATEORIGIN"] = 10,
        ["FILECREATIONDATE"] = 8,
        ["FILECREATIONTIME"] = 4,
        ["FILEIDMODIFIER"] = 1,
        ["RECORDSIZE"] = 3,
        ["BLOCKINGFACTOR"] = 2,
        ["FORMATCODE"] = 1,
        ["IMMEDIATEDESTINATIONNAME"] = 23,
        ["IMMEDIATEORIGINNAME"] = 23,
        ["REFERENCECODE"] = 8
    };

    private static readonly Dictionary<string, int> Record5FieldLengths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SERVICECLASSCODE"] = 3,
        ["COMPANYNAME"] = 16,
        ["COMPANYDISCRETIONARYDATA"] = 20,
        ["COMPANYIDENTIFICATION"] = 10,
        ["STANDARDENTRYCLASSCODE"] = 3,
        ["COMPANYENTRYDESCRIPTION"] = 10,
        ["COMPANYDESCRIPTIVEDATE"] = 8,
        ["EFFECTIVEENTRYDATE"] = 8,
        ["SETTLEMENTDATE"] = 3,
        ["ORIGINATORSTATUSCODE"] = 1,
        ["ORIGINATINGDFI"] = 8,
        ["BATCHNUMBER"] = 7
    };
    private static readonly Dictionary<string, int> Record8FieldLengths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SERVICECLASSCODE"] = 3,
        ["ENTRYADDENDACOUNT"] = 6,
        ["ENTRYHASH"] = 10,
        ["TOTALDEBITAMOUNT"] = 18,
        ["TOTALCREDITAMOUNT"] = 18,
        ["COMPANYIDENTIFICATION"] = 10,
        ["ORIGINATINGDFI"] = 8,
        ["BATCHNUMBER"] = 7
    };
    private static readonly Dictionary<string, int> Record9FieldLengths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BATCHCOUNT"] = 6,
        ["BLOCKCOUNT"] = 6,
        ["ENTRYADDENDACOUNT"] = 8,
        ["ENTRYHASH"] = 10,
        ["TOTALDEBITAMOUNT"] = 18,
        ["TOTALCREDITAMOUNT"] = 18
    };

    private static void ValidateAchColExactLayoutAndRules(
        ICollection<NachaConfigValidationIssueDto> issues,
        CfgLayoutVariant variant,
        IReadOnlyCollection<CfgLayoutField> orderedFields)
    {
        IReadOnlyList<AchColOfficialFieldDescriptor> expected;
        try
        {
            expected = AchColOfficialNachaLayout.ForVariant(variant.RecordCode.Code, variant.VariantCode);
        }
        catch (InvalidOperationException)
        {
            issues.Add(new NachaConfigValidationIssueDto
            {
                Severidad = "ERROR",
                Codigo = "ACHCOL_LAYOUT_NOT_DEMONSTRATED",
                Mensaje = $"La variante {variant.VariantCode} no tiene descriptor ACHCOL V35 aprobado."
            });
            return;
        }

        if (variant.TotalLength != AchColOfficialNachaLayout.RecordLength)
        {
            issues.Add(new NachaConfigValidationIssueDto
            {
                Severidad = "ERROR",
                Codigo = "ACHCOL_RECORD_LENGTH",
                Mensaje = $"La variante {variant.VariantCode} debe declarar longitud 106."
            });
        }

        var configured = orderedFields
            .GroupBy(field => field.FieldCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var descriptor in expected)
        {
            if (!configured.TryGetValue(descriptor.FieldCode, out var field))
            {
                AddAchColDescriptorIssue(issues, descriptor, "ACHCOL_FIELD_MISSING", "no está configurado");
                continue;
            }

            if (field.StartPosition != descriptor.StartPosition
                || field.Length != descriptor.Length
                || char.ToUpperInvariant(field.Justification) != descriptor.Justification
                || field.PadChar != descriptor.PadChar)
            {
                AddAchColDescriptorIssue(
                    issues,
                    descriptor,
                    "ACHCOL_FIELD_LAYOUT_MISMATCH",
                    $"debe ocupar {descriptor.StartPosition}-{descriptor.EndPosition}, alineación {descriptor.Justification} y relleno U+{(int)descriptor.PadChar:X4}");
            }

            if (!string.Equals(field.FormatMask?.Trim(), descriptor.Format ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(descriptor.Format))
            {
                AddAchColDescriptorIssue(issues, descriptor, "ACHCOL_FIELD_FORMAT_MISMATCH", $"debe usar formato {descriptor.Format}");
            }

            var rule = field.Rules.FirstOrDefault(candidate =>
                candidate.IsEnabled
                && string.Equals(candidate.RuleCode, descriptor.RuleId, StringComparison.OrdinalIgnoreCase));
            if (rule is null)
            {
                AddAchColDescriptorIssue(issues, descriptor, "ACHCOL_RULE_NOT_EXECUTABLE", $"no tiene CfgFieldRule habilitada con RuleId {descriptor.RuleId}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(rule.RuleConfigJson))
            {
                AddAchColDescriptorIssue(issues, descriptor, "ACHCOL_RULE_METADATA_MISSING", "no tiene metadata normativa ejecutable");
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(rule.RuleConfigJson);
                var root = document.RootElement;
                var overflow = root.TryGetProperty("overflowPolicy", out var overflowElement)
                    ? overflowElement.GetString()
                    : null;
                var source = root.TryGetProperty("normativeSource", out var sourceElement)
                    ? sourceElement.GetString()
                    : null;
                var version = root.TryGetProperty("normativeVersion", out var versionElement)
                    ? versionElement.GetString()
                    : null;
                if (!string.Equals(overflow, "REJECT", StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(source, descriptor.NormativeSource, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(version, descriptor.NormativeVersion, StringComparison.OrdinalIgnoreCase))
                {
                    AddAchColDescriptorIssue(issues, descriptor, "ACHCOL_RULE_METADATA_INVALID", "no declara overflow REJECT y trazabilidad MAN-004 V35");
                }
            }
            catch (JsonException)
            {
                AddAchColDescriptorIssue(issues, descriptor, "ACHCOL_RULE_METADATA_INVALID", "tiene RuleConfigJson inválido");
            }
        }

        var expectedCodes = expected.Select(field => field.FieldCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var unexpected in orderedFields.Where(field => !expectedCodes.Contains(field.FieldCode)))
        {
            issues.Add(new NachaConfigValidationIssueDto
            {
                Severidad = "ERROR",
                Codigo = "ACHCOL_UNEXPECTED_FIELD",
                Mensaje = $"La variante {variant.VariantCode} contiene un campo no demostrado por su descriptor oficial: {unexpected.FieldCode}."
            });
        }
    }

    private static void AddAchColDescriptorIssue(
        ICollection<NachaConfigValidationIssueDto> issues,
        AchColOfficialFieldDescriptor descriptor,
        string code,
        string cause)
        => issues.Add(new NachaConfigValidationIssueDto
        {
            Severidad = "ERROR",
            Codigo = code,
            Mensaje = $"{descriptor.RuleId}: record {descriptor.RecordCode}, campo {descriptor.FieldCode}, posición {descriptor.StartPosition}, longitud {descriptor.Length}: {cause}."
        });

    private static void ValidateHeaderNormativeRequirements(
        ICollection<NachaConfigValidationIssueDto> issues,
        string chamberCode,
        CfgLayoutVariant variant,
        IReadOnlyCollection<CfgLayoutField> orderedFields)
    {
        var recordCode = variant.RecordCode.Code;
        if (recordCode is not ("1" or "5" or "8" or "9"))
        {
            return;
        }

        if (variant.TotalLength != 106)
        {
            issues.Add(new NachaConfigValidationIssueDto
            {
                Severidad = "ERROR",
                Codigo = "INVALID_RECORD_LENGTH",
                Mensaje = $"Record {recordCode} debe tener longitud 106 y tiene {variant.TotalLength}."
            });
        }

        if (orderedFields.Count < 5)
        {
            issues.Add(new NachaConfigValidationIssueDto
            {
                Severidad = "WARN",
                Codigo = "HEADER_NOT_FULLY_TABLE_DRIVEN",
                Mensaje = $"Record {recordCode} aún no está completamente gobernado por layout. Se conserva control legado parcial."
            });
            return;
        }

        var fieldsByCode = orderedFields
            .GroupBy(f => Normalize(f.FieldCode))
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var normalizedFieldCodes = fieldsByCode.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var required = recordCode switch
        {
            "1" => Record1RequiredFields,
            "5" => Record5RequiredFields,
            "8" => Record8RequiredFields,
            _ => Record9RequiredFields
        };
        foreach (var requiredField in required)
        {
            if (!normalizedFieldCodes.Contains(requiredField))
            {
                issues.Add(new NachaConfigValidationIssueDto
                {
                    Severidad = "ERROR",
                    Codigo = "MISSING_HEADER_FIELD",
                    Mensaje = $"Record {recordCode} no tiene configurado el campo obligatorio {requiredField}."
                });
            }
        }

        var expectedLengths = recordCode switch
        {
            "1" => Record1FieldLengths,
            "5" => Record5FieldLengths,
            "8" => Record8FieldLengths,
            _ => Record9FieldLengths
        };
        foreach (var expected in expectedLengths)
        {
            if (!fieldsByCode.TryGetValue(expected.Key, out var field))
            {
                continue;
            }

            if (field.Length != expected.Value)
            {
                issues.Add(new NachaConfigValidationIssueDto
                {
                    Severidad = "ERROR",
                    Codigo = "INVALID_FIELD_LENGTH",
                    Mensaje = $"Record {recordCode} campo {expected.Key} debe tener longitud {expected.Value} y tiene {field.Length}."
                });
            }
        }

        if (recordCode == "1")
        {
            ValidateRecord1HeaderRules(issues, chamberCode, fieldsByCode);
        }
        else if (recordCode == "5")
        {
            ValidateRecord5HeaderRules(issues, chamberCode, fieldsByCode);
        }
        else
        {
            ValidateControlRecordRules(issues, recordCode, fieldsByCode);
        }
    }

    private static void ValidateControlRecordRules(
        ICollection<NachaConfigValidationIssueDto> issues,
        string recordCode,
        IReadOnlyDictionary<string, CfgLayoutField> fieldsByCode)
    {
        if (recordCode == "8")
        {
            ValidateFieldIsRuntimeComputed(issues, fieldsByCode, "BATCHNUMBER");
            ValidateFieldIsRuntimeComputed(issues, fieldsByCode, "ENTRYHASH");
            ValidateFieldIsRuntimeComputed(issues, fieldsByCode, "TOTALDEBITAMOUNT");
            ValidateFieldIsRuntimeComputed(issues, fieldsByCode, "TOTALCREDITAMOUNT");
        }

        if (recordCode == "9")
        {
            ValidateFieldIsRuntimeComputed(issues, fieldsByCode, "BATCHCOUNT");
            ValidateFieldIsRuntimeComputed(issues, fieldsByCode, "BLOCKCOUNT");
            ValidateFieldIsRuntimeComputed(issues, fieldsByCode, "ENTRYHASH");
        }
    }

    private static void ValidateFieldIsRuntimeComputed(
        ICollection<NachaConfigValidationIssueDto> issues,
        IReadOnlyDictionary<string, CfgLayoutField> fieldsByCode,
        string fieldCode)
    {
        if (!fieldsByCode.TryGetValue(fieldCode, out var field))
        {
            return;
        }

        if (string.Equals(field.SourceDefinition?.DataSourceType?.Code, "CONSTANTE", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new NachaConfigValidationIssueDto
            {
                Severidad = "ERROR",
                Codigo = "CONTROL_FIELD_MUST_BE_RUNTIME",
                Mensaje = $"Campo {fieldCode} debe derivarse de cálculo runtime y no puede ser constante."
            });
        }
    }

    private static void ValidateRecord1HeaderRules(
        ICollection<NachaConfigValidationIssueDto> issues,
        string chamberCode,
        IReadOnlyDictionary<string, CfgLayoutField> fieldsByCode)
    {
        ValidateFormatMask(issues, fieldsByCode, "FILECREATIONDATE", "yyyyMMdd");
        ValidateFormatMask(issues, fieldsByCode, "FILECREATIONTIME", "HHmm");
        ValidateConstantValue(issues, fieldsByCode, "RECORDSIZE", "106");
        ValidateConstantValue(issues, fieldsByCode, "BLOCKINGFACTOR", "10");
        ValidateConstantValue(issues, fieldsByCode, "FORMATCODE", "1");

        if (TryGetConstant(fieldsByCode, "IMMEDIATEORIGIN", out var origin)
            && TryGetConstant(fieldsByCode, "IMMEDIATEDESTINATION", out var destination)
            && string.Equals(origin, destination, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new NachaConfigValidationIssueDto
            {
                Severidad = "ERROR",
                Codigo = "INVALID_HEADER_COHERENCE",
                Mensaje = "ImmediateOrigin e ImmediateDestination no deben ser idénticos cuando ambos son constantes."
            });
        }

        if (TryGetConstant(fieldsByCode, "FILEIDMODIFIER", out var fileIdModifier))
        {
            var isValid = fileIdModifier.Length == 1 && fileIdModifier.All(char.IsLetterOrDigit);
            if (!isValid)
            {
                AddChamberRuleIssue(issues, chamberCode, "FileIdModifier inválido para la cámara configurada.");
            }
        }

        ValidateRoutingConstantByChamber(issues, chamberCode, fieldsByCode, "IMMEDIATEORIGIN");
        ValidateRoutingConstantByChamber(issues, chamberCode, fieldsByCode, "IMMEDIATEDESTINATION");
    }

    private static void ValidateRecord5HeaderRules(
        ICollection<NachaConfigValidationIssueDto> issues,
        string chamberCode,
        IReadOnlyDictionary<string, CfgLayoutField> fieldsByCode)
    {
        ValidateFormatMask(issues, fieldsByCode, "EFFECTIVEENTRYDATE", "yyyyMMdd");
        ValidateFormatMask(issues, fieldsByCode, "COMPANYDESCRIPTIVEDATE", "yyyyMMdd", allowMissingMask: true);

        if (TryGetConstant(fieldsByCode, "STANDARDENTRYCLASSCODE", out var secCode)
            && secCode is not ("PPD" or "CCD"))
        {
            AddChamberRuleIssue(issues, chamberCode, $"SEC inválido '{secCode}'. Solo se permite PPD o CCD.");
        }

        if (TryGetConstant(fieldsByCode, "ORIGINATINGDFI", out var dfi)
            && (dfi.Length != 8 || dfi.Any(ch => !char.IsDigit(ch))))
        {
            AddChamberRuleIssue(issues, chamberCode, "OriginatingDFI constante debe tener exactamente 8 dígitos.");
        }

        if (fieldsByCode.TryGetValue("SETTLEMENTDATE", out var settlementField))
        {
            ValidateSettlementPolicy(issues, chamberCode, settlementField);
        }
    }

    private static void ValidateSettlementPolicy(
        ICollection<NachaConfigValidationIssueDto> issues,
        string chamberCode,
        CfgLayoutField settlementField)
    {
        var sourceType = settlementField.SourceDefinition?.DataSourceType?.Code ?? string.Empty;
        var constant = (settlementField.SourceDefinition?.ConstantValue ?? string.Empty).Trim();

        if (chamberCode == "CENIT")
        {
            if (sourceType.Equals("CONSTANTE", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(constant))
            {
                issues.Add(new NachaConfigValidationIssueDto
                {
                    Severidad = "ERROR",
                    Codigo = "INVALID_SETTLEMENT_POLICY",
                    Mensaje = "CENIT requiere SettlementDate vacío (lo determina la cámara)."
                });
            }

            return;
        }

        if (chamberCode == "ACH" && sourceType.Equals("CONSTANTE", StringComparison.OrdinalIgnoreCase))
        {
            var valid = string.IsNullOrWhiteSpace(constant) || (constant.Length == 3 && constant.All(char.IsDigit));
            if (!valid)
            {
                issues.Add(new NachaConfigValidationIssueDto
                {
                    Severidad = "ERROR",
                    Codigo = "INVALID_SETTLEMENT_POLICY",
                    Mensaje = "ACH permite SettlementDate vacío o juliano de 3 dígitos cuando es constante."
                });
            }
        }
    }

    private static void ValidateRoutingConstantByChamber(
        ICollection<NachaConfigValidationIssueDto> issues,
        string chamberCode,
        IReadOnlyDictionary<string, CfgLayoutField> fieldsByCode,
        string fieldCode)
    {
        if (!TryGetConstant(fieldsByCode, fieldCode, out var value))
        {
            return;
        }

        var isNumeric = value.All(char.IsDigit);
        var valid = chamberCode == "CENIT"
            ? isNumeric && value.Length == 8
            : isNumeric && value.Length is >= 8 and <= 10;
        if (!valid)
        {
            AddChamberRuleIssue(issues, chamberCode, $"{fieldCode} no cumple formato esperado para cámara {chamberCode}.");
        }
    }

    private static void AddChamberRuleIssue(ICollection<NachaConfigValidationIssueDto> issues, string chamberCode, string message)
    {
        var code = chamberCode == "CENIT" ? "HEADER_RULE_CENIT_INVALID" : "HEADER_RULE_ACH_INVALID";
        issues.Add(new NachaConfigValidationIssueDto
        {
            Severidad = "ERROR",
            Codigo = code,
            Mensaje = message
        });
    }

    private static string ResolveChamberCode(string? clearingHouseCode, string? clearingHouseName)
    {
        if (!string.IsNullOrWhiteSpace(clearingHouseCode))
        {
            if (clearingHouseCode.Contains("CENIT", StringComparison.OrdinalIgnoreCase))
            {
                return "CENIT";
            }

            if (clearingHouseCode.Contains("ACH", StringComparison.OrdinalIgnoreCase))
            {
                return "ACH";
            }
        }

        if (!string.IsNullOrWhiteSpace(clearingHouseName) && clearingHouseName.Contains("CENIT", StringComparison.OrdinalIgnoreCase))
        {
            return "CENIT";
        }

        return "ACH";
    }

    private static void ValidateFormatMask(
        ICollection<NachaConfigValidationIssueDto> issues,
        IReadOnlyDictionary<string, CfgLayoutField> fieldsByCode,
        string fieldCode,
        string expectedMask,
        bool allowMissingMask = false)
    {
        if (!fieldsByCode.TryGetValue(fieldCode, out var field))
        {
            return;
        }

        var mask = field.FormatMask?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(mask) && allowMissingMask)
        {
            return;
        }

        if (!string.Equals(mask, expectedMask, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new NachaConfigValidationIssueDto
            {
                Severidad = "ERROR",
                Codigo = "INVALID_DATE_FORMAT",
                Mensaje = $"Campo {fieldCode} debe usar formato {expectedMask}."
            });
        }
    }

    private static void ValidateConstantValue(
        ICollection<NachaConfigValidationIssueDto> issues,
        IReadOnlyDictionary<string, CfgLayoutField> fieldsByCode,
        string fieldCode,
        string expected)
    {
        if (!TryGetConstant(fieldsByCode, fieldCode, out var constant))
        {
            return;
        }

        if (!string.Equals(constant, expected, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new NachaConfigValidationIssueDto
            {
                Severidad = "ERROR",
                Codigo = "INVALID_CONSTANT_VALUE",
                Mensaje = $"Campo {fieldCode} debe tener constante {expected}."
            });
        }
    }

    private static bool TryGetConstant(
        IReadOnlyDictionary<string, CfgLayoutField> fieldsByCode,
        string fieldCode,
        out string constant)
    {
        constant = string.Empty;
        if (!fieldsByCode.TryGetValue(fieldCode, out var field))
        {
            return false;
        }

        if (!string.Equals(field.SourceDefinition?.DataSourceType?.Code, "CONSTANTE", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        constant = (field.SourceDefinition?.ConstantValue ?? string.Empty).Trim();
        return !string.IsNullOrWhiteSpace(constant);
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value.Trim().Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    }

    private void ValidateCanonicalPath(
        ICollection<NachaConfigValidationIssueDto> issues,
        string recordCode,
        CfgLayoutField field,
        string sourceType,
        IDictionary<string, List<string>> canonicalUsages)
    {
        if (sourceType.Equals("CONSTANTE", StringComparison.OrdinalIgnoreCase)
            || sourceType.Equals("EXPRESION", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(field.SourceDefinition.PropertyPath))
        {
            return;
        }

        var propertyPath = field.SourceDefinition.PropertyPath.Trim();
        var probe = (_canonicalMapper as NachaCanonicalMapper)?.Probe(recordCode, propertyPath);
        if (_canonicalMapper.TryResolveCanonicalKey(recordCode, propertyPath, out var canonical))
        {
            if (!canonicalUsages.TryGetValue(canonical, out var usages))
            {
                usages = [];
                canonicalUsages[canonical] = usages;
            }

            usages.Add($"{field.FieldCode}:{propertyPath}");
            return;
        }

        var code = probe?.Failure switch
        {
            NachaCanonicalMapper.CanonicalResolutionFailure.AmbiguousAlias => "AMBIGUOUS_ALIAS",
            NachaCanonicalMapper.CanonicalResolutionFailure.InvalidCanonicalKey => "INVALID_CANONICAL_KEY",
            _ => "UNRESOLVABLE_ALIAS"
        };

        issues.Add(new NachaConfigValidationIssueDto
        {
            Severidad = "ERROR",
            Codigo = code,
            Mensaje = $"Field {field.FieldCode} en record {recordCode} no resuelve '{propertyPath}' ({code})."
        });
    }

    private static NachaConfigValidationResultDto Build(int profileId, IReadOnlyList<NachaConfigValidationIssueDto> issues)
    {
        var blocking = issues.Count(x => string.Equals(x.Severidad, "ERROR", StringComparison.OrdinalIgnoreCase));
        var warnings = issues.Count(x => string.Equals(x.Severidad, "WARN", StringComparison.OrdinalIgnoreCase));

        return new NachaConfigValidationResultDto
        {
            ProfileId = profileId,
            IsValid = blocking == 0,
            ErroresBloqueantes = blocking,
            Advertencias = warnings,
            Resumen = blocking == 0
                ? "Validación exitosa."
                : $"Validación con {blocking} errores bloqueantes.",
            Issues = issues.ToList()
        };
    }
}
