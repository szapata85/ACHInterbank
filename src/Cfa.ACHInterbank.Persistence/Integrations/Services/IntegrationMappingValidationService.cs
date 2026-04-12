using System.Text.Json;
using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Integrations.Services;

[Scoped]
public class IntegrationMappingValidationService : IIntegrationMappingValidationService
{
    private static readonly HashSet<string> AllowedTransformations =
    [
        "Trim", "Uppercase", "Lowercase", "PadLeft", "PadRight", "Substring", "Concat",
        "DateFormat", "NumericFormat", "NullIfEmpty", "DefaultIfNull"
    ];

    private readonly AchDbContext _context;

    public IntegrationMappingValidationService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IntegrationMappingValidationResultDto> ValidateAsync(Guid mappingSetId, bool includeWarnings = true, CancellationToken ct = default)
    {
        var set = await _context.Set<IntegrationMappingSet>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == mappingSetId, ct)
            ?? throw new KeyNotFoundException($"No existe MappingSet {mappingSetId}.");

        var parameters = await _context.Set<IntegrationMethodParameter>()
            .AsNoTracking()
            .Where(x => x.MethodId == set.MethodId && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(ct);

        var rules = await _context.Set<IntegrationMappingRule>()
            .AsNoTracking()
            .Where(x => x.MappingSetId == set.Id)
            .ToListAsync(ct);

        var catalog = await _context.Set<IntegrationSourceCatalogField>()
            .AsNoTracking()
            .Where(x => x.MethodId == set.MethodId && x.IsActive)
            .ToDictionaryAsync(x => x.Id, ct);

        var issues = new List<IntegrationMappingValidationIssueDto>();
        var parameterSummaries = new List<IntegrationMappingParameterValidationDto>();

        foreach (var parameter in parameters)
        {
            var paramRules = rules
                .Where(r => r.ParameterId == parameter.Id)
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.Id)
                .ToList();

            var paramIssuesBefore = issues.Count;

            if (parameter.Required && paramRules.Count == 0)
            {
                issues.Add(new("Error", "REQUIRED_PARAMETER_MISSING", $"Falta regla para parámetro obligatorio {parameter.ParameterPath}.", parameter.ParameterPath, "Structural"));
            }

            if (parameter.Required && paramRules.Count > 0 && !paramRules.Any(r => r.Enabled))
            {
                issues.Add(new("Error", "REQUIRED_PARAMETER_INACTIVE", $"Todas las reglas del parámetro obligatorio {parameter.ParameterPath} están inactivas.", parameter.ParameterPath, "Structural"));
            }

            var enabledRules = paramRules.Where(r => r.Enabled).ToList();

            var duplicateByPriority = enabledRules
                .GroupBy(r => r.Priority)
                .Where(g => g.Count() > 1);

            foreach (var duplicate in duplicateByPriority)
            {
                issues.Add(new("Error", "CONFLICTING_RULES_DUPLICATE_PRIORITY", $"Existen reglas conflictivas con prioridad {duplicate.Key} para {parameter.ParameterPath}.", parameter.ParameterPath, "Functional"));
            }

            if (enabledRules.Count > 1 && !enabledRules.Any(r => r.Priority == 1))
            {
                issues.Add(new(includeWarnings ? "Warning" : "Error", "PRIORITY_RESULT_INCONSISTENT", $"{parameter.ParameterPath} tiene múltiples reglas habilitadas sin prioridad 1.", parameter.ParameterPath, "Functional"));
            }

            foreach (var rule in paramRules)
            {
                var hasSource = rule.SourceCatalogFieldId.HasValue || !string.IsNullOrWhiteSpace(rule.SourceFieldPath);
                var hasFixed = !string.IsNullOrWhiteSpace(rule.FixedValue);

                if (rule.SourceKind == IntegrationSourceKindEnum.Constant && !hasFixed && string.IsNullOrWhiteSpace(rule.DefaultValue))
                {
                    issues.Add(new("Error", "CONSTANT_WITHOUT_VALUE", $"Regla {rule.Id} tipo Constant requiere FixedValue o DefaultValue.", parameter.ParameterPath, "Structural"));
                }

                if (rule.SourceKind != IntegrationSourceKindEnum.Constant && rule.SourceKind != IntegrationSourceKindEnum.Expression && !hasSource)
                {
                    issues.Add(new("Error", "SOURCE_NOT_DEFINED", $"Regla {rule.Id} no define origen.", parameter.ParameterPath, "Structural"));
                }

                if (!string.IsNullOrWhiteSpace(rule.TransformationCode) && !AllowedTransformations.Contains(rule.TransformationCode))
                {
                    issues.Add(new("Error", "TRANSFORMATION_INVALID", $"Transformación {rule.TransformationCode} no permitida.", parameter.ParameterPath, "Functional"));
                }

                if (!IsFormatMaskValid(rule.TransformationCode, rule.FormatMask))
                {
                    issues.Add(new("Error", "FORMAT_INVALID", $"FormatMask inválido para transformación {rule.TransformationCode}.", parameter.ParameterPath, "Functional"));
                }

                if (!string.IsNullOrWhiteSpace(rule.ConditionExpression) && rule.SourceKind != IntegrationSourceKindEnum.Expression)
                {
                    issues.Add(new("Error", "CONDITION_NOT_ALLOWED", $"ConditionExpression solo aplica para SourceKind=Expression (regla {rule.Id}).", parameter.ParameterPath, "Structural"));
                }

                if (rule.SourceCatalogFieldId.HasValue && catalog.TryGetValue(rule.SourceCatalogFieldId.Value, out var sourceField))
                {
                    if (!AreTypesCompatible(sourceField.DataType, parameter.DataType))
                    {
                        issues.Add(new("Error", "TYPE_INCOMPATIBLE", $"Incompatibilidad de tipos origen={sourceField.DataType} destino={parameter.DataType}.", parameter.ParameterPath, "Functional"));
                    }

                    if (sourceField.Cardinality != IntegrationParameterCardinalityEnum.Scalar
                        && parameter.Cardinality == IntegrationParameterCardinalityEnum.Scalar
                        && !string.Equals(rule.TransformationCode, "Concat", StringComparison.OrdinalIgnoreCase))
                    {
                        issues.Add(new("Error", "CARDINALITY_INCOMPATIBLE", $"Cardinalidad incompatible origen={sourceField.Cardinality}, destino={parameter.Cardinality}.", parameter.ParameterPath, "Functional"));
                    }
                }
                else if (rule.SourceCatalogFieldId.HasValue)
                {
                    issues.Add(new("Error", "SOURCE_FIELD_NOT_FOUND", $"SourceCatalogFieldId {rule.SourceCatalogFieldId.Value} no existe.", parameter.ParameterPath, "Structural"));
                }

                if (parameter.Required && !rule.Enabled)
                {
                    issues.Add(new("Error", "REQUIRED_MAPPING_DISABLED", $"Regla {rule.Id} está inactiva para parámetro requerido {parameter.ParameterPath}.", parameter.ParameterPath, "Structural"));
                }
            }

            var paramIssues = issues.Skip(paramIssuesBefore).ToList();
            var hasErrors = paramIssues.Any(x => x.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase));
            var resolutionKind = ResolveResolutionKind(enabledRules);
            var status = ResolveParameterStatus(parameter, paramRules, hasErrors, resolutionKind);

            var hints = BuildHints(parameter, paramRules, paramIssues, resolutionKind);
            parameterSummaries.Add(new IntegrationMappingParameterValidationDto(parameter.Id, parameter.ParameterPath, parameter.Required, status, resolutionKind, hints));
        }

        var coverage = new IntegrationMappingCoverageSummaryDto(
            parameters.Count,
            parameterSummaries.Count(x => x.Status == "valid"),
            parameterSummaries.Count(x => x.Status == "incomplete"),
            parameterSummaries.Count(x => x.Status == "invalid"),
            parameterSummaries.Count(x => x.Status == "inactive"),
            parameterSummaries.Count(x => x.ResolutionKind == "default-fixed"),
            parameterSummaries.Count(x => x.ResolutionKind == "source-field"));

        var isValid = !issues.Any(i => i.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase))
                      && coverage.IncompleteParameters == 0;

        var persisted = await _context.Set<IntegrationMappingSet>().FirstAsync(x => x.Id == mappingSetId, ct);
        persisted.ValidationSummaryJson = JsonSerializer.Serialize(new { isValid, coverage, issues, parameters = parameterSummaries });
        await _context.SaveChangesAsync(ct);

        return new IntegrationMappingValidationResultDto(mappingSetId, isValid, issues, coverage, parameterSummaries);
    }

    private static bool IsFormatMaskValid(string? transformationCode, string? formatMask)
    {
        if (string.IsNullOrWhiteSpace(transformationCode))
        {
            return string.IsNullOrWhiteSpace(formatMask);
        }

        if (string.IsNullOrWhiteSpace(formatMask))
        {
            return !RequiresFormat(transformationCode);
        }

        return transformationCode switch
        {
            "DateFormat" => formatMask.Contains('y', StringComparison.OrdinalIgnoreCase)
                             || formatMask.Contains('d', StringComparison.OrdinalIgnoreCase),
            "NumericFormat" => formatMask.Contains('0') || formatMask.Contains('#'),
            "PadLeft" or "PadRight" => int.TryParse(formatMask, out var n) && n > 0,
            "Substring" => formatMask.Contains(':'),
            _ => true
        };
    }

    private static bool RequiresFormat(string transformationCode)
        => transformationCode is "DateFormat" or "NumericFormat" or "PadLeft" or "PadRight" or "Substring";

    private static string ResolveResolutionKind(IReadOnlyCollection<IntegrationMappingRule> enabledRules)
    {
        var winner = enabledRules.OrderBy(x => x.Priority).ThenBy(x => x.Id).FirstOrDefault();
        if (winner is null)
        {
            return "none";
        }

        if (!string.IsNullOrWhiteSpace(winner.FixedValue) || !string.IsNullOrWhiteSpace(winner.DefaultValue))
        {
            return "default-fixed";
        }

        if (winner.SourceKind == IntegrationSourceKindEnum.Constant)
        {
            return "default-fixed";
        }

        if (winner.SourceKind == IntegrationSourceKindEnum.Expression)
        {
            return "expression";
        }

        return "source-field";
    }

    private static string ResolveParameterStatus(
        IntegrationMethodParameter parameter,
        IReadOnlyCollection<IntegrationMappingRule> paramRules,
        bool hasErrors,
        string resolutionKind)
    {
        if (hasErrors)
        {
            return "invalid";
        }

        if (paramRules.Count == 0)
        {
            return parameter.Required ? "incomplete" : "inactive";
        }

        if (!paramRules.Any(r => r.Enabled))
        {
            return parameter.Required ? "incomplete" : "inactive";
        }

        if (resolutionKind == "none")
        {
            return "incomplete";
        }

        return "valid";
    }

    private static IReadOnlyCollection<string> BuildHints(
        IntegrationMethodParameter parameter,
        IReadOnlyCollection<IntegrationMappingRule> rules,
        IReadOnlyCollection<IntegrationMappingValidationIssueDto> issues,
        string resolutionKind)
    {
        var hints = new List<string>();

        if (parameter.Required)
        {
            hints.Add("Parámetro obligatorio: debe quedar cubierto antes de publicar.");
        }

        if (rules.Count == 0)
        {
            hints.Add("Crea una regla inicial desde el panel central.");
        }

        if (!rules.Any(x => x.Enabled) && rules.Count > 0)
        {
            hints.Add("Activa al menos una regla para que el valor se resuelva.");
        }

        if (resolutionKind == "default-fixed")
        {
            hints.Add("Actualmente se resuelve por default/fixed. Verifica si debe venir de datos transaccionales.");
        }

        if (issues.Any())
        {
            hints.Add("Corrige las observaciones del panel de validación para continuar.");
        }

        return hints;
    }

    private static bool AreTypesCompatible(string sourceType, string destinationType)
    {
        if (sourceType.Equals(destinationType, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var numeric = new[] { "int", "long", "decimal", "double", "float" };
        if (numeric.Contains(sourceType, StringComparer.OrdinalIgnoreCase)
            && numeric.Contains(destinationType, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        if (destinationType.Equals("string", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if ((sourceType.Equals("datetime", StringComparison.OrdinalIgnoreCase) || sourceType.Equals("timespan", StringComparison.OrdinalIgnoreCase))
            && destinationType.Equals("string", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
