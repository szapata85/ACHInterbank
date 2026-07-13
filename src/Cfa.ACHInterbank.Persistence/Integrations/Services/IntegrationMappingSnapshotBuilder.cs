using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Integrations.Services;

[Scoped]
public sealed class IntegrationMappingSnapshotBuilder
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = null
    };

    private readonly AchDbContext _context;

    public IntegrationMappingSnapshotBuilder(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IntegrationMappingSnapshotResult> BuildAsync(Guid mappingSetId, CancellationToken ct = default)
    {
        var set = await _context.IntegrationMappingSets
            .AsNoTracking()
            .FirstAsync(x => x.Id == mappingSetId, ct);

        var parameters = await _context.IntegrationMethodParameters
            .AsNoTracking()
            .Where(x => x.MethodId == set.MethodId && x.IsActive)
            .OrderBy(x => x.ParameterPath)
            .ToListAsync(ct);

        var parameterIds = parameters.Select(x => x.Id).ToHashSet();
        var rules = await _context.IntegrationMappingRules
            .AsNoTracking()
            .Where(x => x.MappingSetId == set.Id && parameterIds.Contains(x.ParameterId))
            .OrderBy(x => x.ParameterId)
            .ThenBy(x => x.Priority)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);

        return Build(set, parameters, rules);
    }

    public IntegrationMappingSnapshotResult Build(
        IntegrationMappingSet set,
        IReadOnlyCollection<IntegrationMethodParameter> parameters,
        IReadOnlyCollection<IntegrationMappingRule> rules)
    {
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(rules);

        var parameterSnapshots = parameters
            .OrderBy(x => x.ParameterPath, StringComparer.Ordinal)
            .Select(parameter =>
            {
                var parameterRules = rules
                    .Where(rule => rule.ParameterId == parameter.Id)
                    .OrderBy(rule => rule.Priority)
                    .ThenBy(rule => rule.Id)
                    .Select(rule => new IntegrationMappingSnapshotRule(
                        RuleId: rule.Id,
                        SourceKind: rule.SourceKind.ToString(),
                        SourceFieldPath: Normalize(rule.SourceFieldPath),
                        FixedValue: Normalize(rule.FixedValue),
                        DefaultValue: Normalize(rule.DefaultValue),
                        ConversionRule: Normalize(rule.TransformationCode),
                        ConditionExpression: Normalize(rule.ConditionExpression),
                        Priority: rule.Priority,
                        Enabled: rule.Enabled))
                    .ToList();

                return new IntegrationMappingSnapshotParameter(
                    ParameterId: parameter.Id,
                    ParameterPath: parameter.ParameterPath,
                    Required: parameter.Required,
                    Direction: parameter.Direction.ToString(),
                    Rules: parameterRules);
            })
            .ToList();

        var document = new IntegrationMappingSnapshotDocument(
            MappingSetId: set.Id,
            MethodId: set.MethodId,
            Version: set.Version,
            Status: set.Status.ToString(),
            IsActive: set.IsActive,
            Parameters: parameterSnapshots);

        var snapshotJson = JsonSerializer.Serialize(document, SnapshotJsonOptions);
        var snapshotHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(snapshotJson)));

        return new IntegrationMappingSnapshotResult(
            MappingSetId: set.Id,
            MethodId: set.MethodId,
            Version: set.Version,
            Status: set.Status,
            IsActive: set.IsActive,
            SnapshotJson: snapshotJson,
            SnapshotHash: snapshotHash);
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public sealed record IntegrationMappingSnapshotResult(
        Guid MappingSetId,
        int MethodId,
        int Version,
        IntegrationMappingSetStatusEnum Status,
        bool IsActive,
        string SnapshotJson,
        string SnapshotHash);

    private sealed record IntegrationMappingSnapshotDocument(
        Guid MappingSetId,
        int MethodId,
        int Version,
        string Status,
        bool IsActive,
        IReadOnlyList<IntegrationMappingSnapshotParameter> Parameters);

    private sealed record IntegrationMappingSnapshotParameter(
        long ParameterId,
        string ParameterPath,
        bool Required,
        string Direction,
        IReadOnlyList<IntegrationMappingSnapshotRule> Rules);

    private sealed record IntegrationMappingSnapshotRule(
        long RuleId,
        string SourceKind,
        string? SourceFieldPath,
        string? FixedValue,
        string? DefaultValue,
        string? ConversionRule,
        string? ConditionExpression,
        int Priority,
        bool Enabled);
}
