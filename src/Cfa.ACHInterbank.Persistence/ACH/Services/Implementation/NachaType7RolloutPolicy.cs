using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaType7RolloutPolicy : INachaType7RolloutPolicy
{
    private readonly AchDbContext _context;
    private readonly NachaGenerationOptions _options;
    private readonly IHostEnvironment _hostEnvironment;

    public NachaType7RolloutPolicy(AchDbContext context, IOptions<NachaGenerationOptions> options, IHostEnvironment hostEnvironment)
    {
        _context = context;
        _options = options.Value;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<NachaType7RolloutDecision> EvaluateAsync(
        string clearingHouseCode,
        CfgLayoutVariant? layoutVariant,
        string generationMode,
        CancellationToken ct = default)
    {
        var reasons = new List<string>();

        if (!_options.Type7RolloutPolicyEnabled)
        {
            reasons.Add("Type7RolloutPolicyDisabled");
            return new NachaType7RolloutDecision { AllowLegacyFallback = true, Reasons = reasons };
        }

        if (layoutVariant is null)
        {
            reasons.Add("NoLayoutVariant");
            return new NachaType7RolloutDecision { AllowLegacyFallback = true, Reasons = reasons };
        }

        if (_options.Type7DisableFallbackEnvironments.Count > 0 &&
            !_options.Type7DisableFallbackEnvironments.Any(x => string.Equals(x, _hostEnvironment.EnvironmentName, StringComparison.OrdinalIgnoreCase)))
        {
            reasons.Add($"EnvironmentNotEnabled:{_hostEnvironment.EnvironmentName}");
            return new NachaType7RolloutDecision { AllowLegacyFallback = true, Reasons = reasons };
        }

        var explicitLayoutOptIn = _options.Type7DisableLegacyFallbackForLayouts
            .Any(x => string.Equals(x, layoutVariant.VariantCode, StringComparison.OrdinalIgnoreCase));

        var explicitClearingHouseOptIn = _options.Type7EnableTableDrivenForClearingHouses.Count == 0
                                         || _options.Type7EnableTableDrivenForClearingHouses
                                             .Any(x => string.Equals(x, clearingHouseCode, StringComparison.OrdinalIgnoreCase));

        if (!explicitLayoutOptIn || !explicitClearingHouseOptIn)
        {
            reasons.Add("ContextNotOptInForDisableFallback");
            return new NachaType7RolloutDecision { AllowLegacyFallback = true, Reasons = reasons };
        }

        if (_options.Type7RequireShadowBeforeDisableFallback && !string.Equals(generationMode, "SHADOW_COMPARE", StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("ShadowCompareRequired");
            return new NachaType7RolloutDecision { AllowLegacyFallback = true, Reasons = reasons };
        }

        var recentRuns = await _context.HistConfigChanges
            .AsNoTracking()
            .Where(x => x.ChangeType == "GENERATION_TRACE" && x.AfterJson != null)
            .OrderByDescending(x => x.ChangedAtUtc)
            .Take(Math.Max(_options.Type7MinQualifiedRuns * 3, 30))
            .Select(x => x.AfterJson!)
            .ToListAsync(ct);

        var parsed = recentRuns
            .Select(TryParse)
            .Where(x => x is not null)
            .Cast<Type7RunSnapshot>()
            .Where(x => string.Equals(x.LayoutVariantCode, layoutVariant.VariantCode, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(x.ClearingHouseCode, clearingHouseCode, StringComparison.OrdinalIgnoreCase))
            .Take(_options.Type7MinQualifiedRuns)
            .ToList();

        if (parsed.Count < _options.Type7MinQualifiedRuns)
        {
            reasons.Add("InsufficientQualifiedRuns");
            return new NachaType7RolloutDecision
            {
                AllowLegacyFallback = true,
                QualifiedRuns = parsed.Count,
                Reasons = reasons
            };
        }

        var avgEquivalence = parsed.Average(x => x.EquivalenceRatePercent);
        if (avgEquivalence < _options.Type7MinEquivalencePercent)
        {
            reasons.Add($"EquivalenceBelowThreshold:{avgEquivalence:0.00}");
            return new NachaType7RolloutDecision
            {
                AllowLegacyFallback = true,
                QualifiedRuns = parsed.Count,
                EquivalenceRatePercent = avgEquivalence,
                Reasons = reasons
            };
        }

        if (_options.Type7CriticalFieldCodes.Count > 0)
        {
            var hasCriticalDiff = parsed.Any(run => run.DiffByField.Keys.Any(field =>
                _options.Type7CriticalFieldCodes.Any(critical => string.Equals(critical, field, StringComparison.OrdinalIgnoreCase))));
            if (hasCriticalDiff)
            {
                reasons.Add("CriticalFieldDiffDetected");
                return new NachaType7RolloutDecision
                {
                    AllowLegacyFallback = true,
                    QualifiedRuns = parsed.Count,
                    EquivalenceRatePercent = avgEquivalence,
                    Reasons = reasons
                };
            }
        }

        reasons.Add("EligibleToDisableFallback");
        return new NachaType7RolloutDecision
        {
            AllowLegacyFallback = false,
            EligibleToDisableFallback = true,
            QualifiedRuns = parsed.Count,
            EquivalenceRatePercent = avgEquivalence,
            Reasons = reasons
        };
    }

    private static Type7RunSnapshot? TryParse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var traceLine = root.TryGetProperty("Trace", out var traceArray)
                ? traceArray.EnumerateArray().Select(x => x.GetString()).FirstOrDefault(x => x?.StartsWith("Type7Summary:", StringComparison.OrdinalIgnoreCase) == true)
                : null;

            var variantCode = root.TryGetProperty("Type7LayoutVariantCode", out var variantElem)
                ? variantElem.GetString() ?? string.Empty
                : string.Empty;

            var diffByField = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (root.TryGetProperty("Type7DiffByField", out var diffElem) && diffElem.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in diffElem.EnumerateObject())
                {
                    diffByField[property.Name] = property.Value.GetInt32();
                }
            }

            var clearingHouseCode = root.TryGetProperty("ClearingHouseCode", out var ch)
                ? ch.GetString() ?? "ACH"
                : "ACH";

            return new Type7RunSnapshot
            {
                LayoutVariantCode = variantCode,
                ClearingHouseCode = clearingHouseCode,
                EquivalenceRatePercent = ParseEquivalence(traceLine),
                DiffByField = diffByField
            };
        }
        catch
        {
            return null;
        }
    }

    private static decimal ParseEquivalence(string? traceLine)
    {
        if (string.IsNullOrWhiteSpace(traceLine))
        {
            return 0;
        }

        var token = traceLine.Split(';').FirstOrDefault(x => x.StartsWith("MatchRate=", StringComparison.OrdinalIgnoreCase));
        if (token is null)
        {
            return 0;
        }

        var raw = token.Replace("MatchRate=", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("%", string.Empty).Trim();
        return decimal.TryParse(raw, out var value) ? value : 0;
    }

    private sealed class Type7RunSnapshot
    {
        public string LayoutVariantCode { get; init; } = string.Empty;
        public string ClearingHouseCode { get; init; } = "ACH";
        public decimal EquivalenceRatePercent { get; init; }
        public Dictionary<string, int> DiffByField { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
