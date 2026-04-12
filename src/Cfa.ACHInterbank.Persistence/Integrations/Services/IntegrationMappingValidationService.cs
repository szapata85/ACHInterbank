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

        foreach (var requiredParameter in parameters.Where(p => p.Required))
        {
            var paramRules = rules.Where(r => r.ParameterId == requiredParameter.Id).ToList();
            if (paramRules.Count == 0)
            {
                issues.Add(new("Error", "REQUIRED_PARAMETER_MISSING", $"Falta regla para parámetro obligatorio {requiredParameter.ParameterPath}.", requiredParameter.ParameterPath));
                continue;
            }

            if (!paramRules.Any(r => r.Enabled))
            {
                issues.Add(new("Error", "REQUIRED_PARAMETER_INACTIVE", $"Todas las reglas del parámetro obligatorio {requiredParameter.ParameterPath} están inactivas.", requiredParameter.ParameterPath));
            }
        }

        var duplicateConflicts = rules
            .Where(r => r.Enabled)
            .GroupBy(r => new { r.ParameterId, r.Priority })
            .Where(g => g.Count() > 1);
        foreach (var conflict in duplicateConflicts)
        {
            issues.Add(new("Error", "CONFLICTING_PRIORITY", $"Existen reglas habilitadas con misma prioridad para ParameterId={conflict.Key.ParameterId}.", $"ParameterId:{conflict.Key.ParameterId}"));
        }

        foreach (var rule in rules)
        {
            var parameter = parameters.FirstOrDefault(p => p.Id == rule.ParameterId);
            if (parameter is null)
            {
                issues.Add(new("Error", "UNKNOWN_PARAMETER", $"Regla {rule.Id} referencia parámetro inexistente.", $"Rule:{rule.Id}"));
                continue;
            }

            var hasSource = rule.SourceCatalogFieldId.HasValue || !string.IsNullOrWhiteSpace(rule.SourceFieldPath);
            var hasFixed = !string.IsNullOrWhiteSpace(rule.FixedValue);

            if (rule.SourceKind == IntegrationSourceKindEnum.Constant && !hasFixed && string.IsNullOrWhiteSpace(rule.DefaultValue))
            {
                issues.Add(new("Error", "CONSTANT_WITHOUT_VALUE", $"Regla {rule.Id} tipo Constant requiere FixedValue o DefaultValue.", parameter.ParameterPath));
            }

            if (rule.SourceKind != IntegrationSourceKindEnum.Constant && rule.SourceKind != IntegrationSourceKindEnum.Expression && !hasSource)
            {
                issues.Add(new("Error", "SOURCE_NOT_DEFINED", $"Regla {rule.Id} no define origen.", parameter.ParameterPath));
            }

            if (!string.IsNullOrWhiteSpace(rule.TransformationCode) && !AllowedTransformations.Contains(rule.TransformationCode))
            {
                issues.Add(new("Error", "TRANSFORMATION_NOT_ALLOWED", $"Transformación {rule.TransformationCode} no permitida.", parameter.ParameterPath));
            }

            if (!string.IsNullOrWhiteSpace(rule.ConditionExpression) && rule.SourceKind != IntegrationSourceKindEnum.Expression)
            {
                issues.Add(new("Error", "CONDITION_NOT_ALLOWED", $"ConditionExpression solo está habilitado para SourceKind=Expression (regla {rule.Id}).", parameter.ParameterPath));
            }

            if (rule.SourceCatalogFieldId.HasValue && catalog.TryGetValue(rule.SourceCatalogFieldId.Value, out var sourceField))
            {
                if (!AreTypesCompatible(sourceField.DataType, parameter.DataType))
                {
                    issues.Add(new("Error", "TYPE_INCOMPATIBLE", $"Incompatibilidad de tipos origen={sourceField.DataType} destino={parameter.DataType}.", parameter.ParameterPath));
                }

                if (sourceField.Cardinality != IntegrationParameterCardinalityEnum.Scalar
                    && parameter.Cardinality == IntegrationParameterCardinalityEnum.Scalar
                    && !string.Equals(rule.TransformationCode, "Concat", StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(new("Error", "CARDINALITY_INCONSISTENT", $"Cardinalidad inconsistente en regla {rule.Id}: origen {sourceField.Cardinality}, destino {parameter.Cardinality}.", parameter.ParameterPath));
                }
            }
            else if (rule.SourceCatalogFieldId.HasValue)
            {
                issues.Add(new("Error", "SOURCE_FIELD_NOT_FOUND", $"SourceCatalogFieldId {rule.SourceCatalogFieldId.Value} no existe.", parameter.ParameterPath));
            }
        }

        if (includeWarnings)
        {
            var ambiguous = rules
                .Where(r => r.Enabled)
                .GroupBy(r => r.ParameterId)
                .Where(g => g.Count() > 1 && !g.Any(x => x.Priority == 1));

            foreach (var item in ambiguous)
            {
                issues.Add(new("Warning", "AMBIGUOUS_PRIORITY", $"ParameterId={item.Key} tiene múltiples reglas habilitadas sin prioridad 1.", $"ParameterId:{item.Key}"));
            }
        }

        var isValid = !issues.Any(i => i.Severity.Equals("Error", StringComparison.OrdinalIgnoreCase));

        var persisted = await _context.Set<IntegrationMappingSet>().FirstAsync(x => x.Id == mappingSetId, ct);
        persisted.ValidationSummaryJson = JsonSerializer.Serialize(new { isValid, issues });
        await _context.SaveChangesAsync(ct);

        return new IntegrationMappingValidationResultDto(mappingSetId, isValid, issues);
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
