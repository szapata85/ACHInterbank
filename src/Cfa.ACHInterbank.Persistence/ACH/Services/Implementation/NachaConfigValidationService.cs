using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Mapping;
using Cfa.ACHInterbank.Application.ACH.Models;
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
            .Include(x => x.LayoutVariants)
                .ThenInclude(x => x.Fields)
                    .ThenInclude(x => x.Rules)
                        .ThenInclude(x => x.RuleType)
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

            ValidateHeaderNormativeRequirements(issues, variant, ordered);
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
        ["FILECREATIONDATE"] = 6,
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
        ["COMPANYDESCRIPTIVEDATE"] = 6,
        ["EFFECTIVEENTRYDATE"] = 6,
        ["SETTLEMENTDATE"] = 3,
        ["ORIGINATORSTATUSCODE"] = 1,
        ["ORIGINATINGDFI"] = 8,
        ["BATCHNUMBER"] = 7
    };

    private static void ValidateHeaderNormativeRequirements(
        ICollection<NachaConfigValidationIssueDto> issues,
        CfgLayoutVariant variant,
        IReadOnlyCollection<CfgLayoutField> orderedFields)
    {
        var recordCode = variant.RecordCode.Code;
        if (recordCode is not ("1" or "5"))
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
        var required = recordCode == "1" ? Record1RequiredFields : Record5RequiredFields;
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

        var expectedLengths = recordCode == "1" ? Record1FieldLengths : Record5FieldLengths;
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
            ValidateRecord1HeaderRules(issues, fieldsByCode);
        }
        else
        {
            ValidateRecord5HeaderRules(issues, fieldsByCode);
        }
    }

    private static void ValidateRecord1HeaderRules(
        ICollection<NachaConfigValidationIssueDto> issues,
        IReadOnlyDictionary<string, CfgLayoutField> fieldsByCode)
    {
        ValidateFormatMask(issues, fieldsByCode, "FILECREATIONDATE", "yyMMdd");
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
    }

    private static void ValidateRecord5HeaderRules(
        ICollection<NachaConfigValidationIssueDto> issues,
        IReadOnlyDictionary<string, CfgLayoutField> fieldsByCode)
    {
        ValidateFormatMask(issues, fieldsByCode, "EFFECTIVEENTRYDATE", "yyMMdd");
        ValidateFormatMask(issues, fieldsByCode, "COMPANYDESCRIPTIVEDATE", "yyMMdd", allowMissingMask: true);

        if (TryGetConstant(fieldsByCode, "STANDARDENTRYCLASSCODE", out var secCode)
            && secCode is not ("PPD" or "CCD"))
        {
            issues.Add(new NachaConfigValidationIssueDto
            {
                Severidad = "ERROR",
                Codigo = "INVALID_SEC_CODE",
                Mensaje = $"SEC inválido '{secCode}'. Solo se permite PPD o CCD en la versión actual."
            });
        }

        if (TryGetConstant(fieldsByCode, "ORIGINATINGDFI", out var dfi)
            && (dfi.Length != 8 || dfi.Any(ch => !char.IsDigit(ch))))
        {
            issues.Add(new NachaConfigValidationIssueDto
            {
                Severidad = "ERROR",
                Codigo = "INVALID_ORIGINATING_DFI",
                Mensaje = "OriginatingDFI constante debe tener exactamente 8 dígitos."
            });
        }
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
