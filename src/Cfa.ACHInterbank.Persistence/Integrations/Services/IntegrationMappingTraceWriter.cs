using System.Globalization;
using System.Reflection;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Integrations.Services;

[Scoped]
public sealed class IntegrationMappingTraceWriter : IIntegrationMappingTraceWriter
{
    private readonly AchDbContext _context;
    private readonly IIntegrationCatalogService _catalogService;

    public IntegrationMappingTraceWriter(AchDbContext context, IIntegrationCatalogService catalogService)
    {
        _context = context;
        _catalogService = catalogService;
    }

    public async Task<IntegrationMappingTraceWriteResult> WriteAsync(
        TransactionIntegrationOperationResult operation,
        object sourcePayload,
        int? transactionId,
        string reference,
        string correlationId,
        bool dryRun,
        bool externalTransmission,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(sourcePayload);

        await _catalogService.GetMethodsAsync(ct);

        var methodCode = $"{operation.IntegrationKey}.{operation.OperationKey}";
        var method = await _context.IntegrationMethods
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == methodCode && x.IsActive, ct)
            ?? throw new InvalidOperationException($"INTEGRATION_METHOD_NOT_CONFIGURED: {methodCode}.");

        var mappingSet = await _context.IntegrationMappingSets
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id && x.Status == IntegrationMappingSetStatusEnum.Published && x.IsActive)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"INTEGRATION_MAPPING_REQUIRED: no existe mapping publicado para {methodCode}.");

        var parameters = await _context.IntegrationMethodParameters
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id && x.IsActive && x.Direction == ToParameterDirection(operation.MappingDirection))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.ParameterPath)
            .ToListAsync(ct);

        var parameterIds = parameters.Select(x => x.Id).ToHashSet();
        var rules = await _context.IntegrationMappingRules
            .AsNoTracking()
            .Where(x => x.MappingSetId == mappingSet.Id && parameterIds.Contains(x.ParameterId))
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);

        var trace = new IntegrationMappingTrace
        {
            IntegrationKey = operation.IntegrationKey,
            OperationKey = operation.OperationKey,
            MappingPurpose = operation.MappingPurpose,
            MappingDirection = operation.MappingDirection,
            TransactionId = transactionId ?? operation.TransactionId,
            Reference = reference,
            MappingSetId = mappingSet.Id,
            MappingVersion = mappingSet.Version,
            CorrelationId = correlationId,
            DryRun = dryRun,
            ExternalTransmission = externalTransmission,
            MonetaryMovementCreated = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        var missingRequired = new List<string>();
        var errors = new List<string>();

        foreach (var parameter in parameters)
        {
            var rule = rules.FirstOrDefault(x => x.ParameterId == parameter.Id && x.Enabled);
            var sourceField = rule?.SourceFieldPath ?? string.Empty;
            var mappedValue = ResolveMappedValue(sourcePayload, parameter.ParameterPath, rule);
            var missing = parameter.Required && string.IsNullOrWhiteSpace(mappedValue);

            if (missing)
            {
                missingRequired.Add(parameter.ParameterPath);
                errors.Add($"DIFFERENTIAL_RESPONSE_REQUIRED_FIELD_MISSING:{parameter.ParameterPath}");
            }

            trace.Entries.Add(new IntegrationMappingTraceEntry
            {
                SourceField = sourceField,
                TargetField = parameter.ParameterPath,
                SourceValueSanitized = Sanitize(rule is null ? null : ResolveRawValue(sourcePayload, rule.SourceFieldPath)),
                MappedValueSanitized = Sanitize(mappedValue),
                MappingRuleId = rule?.Id,
                TransformationApplied = rule?.TransformationCode ?? string.Empty,
                DefaultValueApplied = !string.IsNullOrWhiteSpace(rule?.DefaultValue) && string.IsNullOrWhiteSpace(ResolveRawValue(sourcePayload, rule.SourceFieldPath)),
                Required = parameter.Required,
                UsedFallback = false,
                Missing = missing,
                ErrorCode = missing ? "REQUIRED_FIELD_MISSING" : string.Empty,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        _context.IntegrationMappingTraces.Add(trace);
        await _context.SaveChangesAsync(ct);

        return new IntegrationMappingTraceWriteResult(trace.Id, trace.Entries.Count, missingRequired, errors);
    }

    private static IntegrationParameterDirectionEnum ToParameterDirection(string direction)
        => string.Equals(direction, IntegrationGuaranteeConstants.InboundResponse, StringComparison.OrdinalIgnoreCase)
            ? IntegrationParameterDirectionEnum.Input
            : IntegrationParameterDirectionEnum.Input;

    private static string? ResolveMappedValue(object sourcePayload, string targetField, IntegrationMappingRule? rule)
    {
        if (TryReadDictionaryValue(sourcePayload, targetField, out var directValue))
        {
            return directValue;
        }

        return rule is null ? null : ResolveValue(sourcePayload, rule);
    }

    private static string? ResolveValue(object sourcePayload, IntegrationMappingRule rule)
    {
        if (!string.IsNullOrWhiteSpace(rule.FixedValue))
        {
            return rule.FixedValue.Trim();
        }

        var raw = ResolveRawValue(sourcePayload, rule.SourceFieldPath);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return rule.DefaultValue;
        }

        return raw;
    }

    private static bool TryReadDictionaryValue(object sourcePayload, string key, out string? value)
    {
        value = null;
        var property = sourcePayload.GetType().GetProperty("Parameters", BindingFlags.Instance | BindingFlags.Public);
        if (property?.GetValue(sourcePayload) is not IEnumerable<KeyValuePair<string, string>> values)
        {
            return false;
        }

        foreach (var item in values)
        {
            if (string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = item.Value;
                return true;
            }
        }

        return false;
    }

    private static string? ResolveRawValue(object sourcePayload, string? sourceFieldPath)
    {
        var key = (sourceFieldPath ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var sourceValuesProperty = sourcePayload.GetType().GetProperty("SourceValues", BindingFlags.Instance | BindingFlags.Public);
        if (sourceValuesProperty?.GetValue(sourcePayload) is IEnumerable<KeyValuePair<string, string>> sourceValues)
        {
            foreach (var item in sourceValues)
            {
                if (string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    return item.Value;
                }
            }
        }

        if (key.Contains('.', StringComparison.Ordinal))
        {
            key = key.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault() ?? key;
        }

        var property = sourcePayload.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(x => string.Equals(x.Name, key, StringComparison.OrdinalIgnoreCase));
        var value = property?.GetValue(sourcePayload);

        return value switch
        {
            null => null,
            DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 64)
        {
            trimmed = string.Concat(trimmed.AsSpan(0, 32), "...", trimmed.AsSpan(trimmed.Length - 16));
        }

        return trimmed;
    }
}
