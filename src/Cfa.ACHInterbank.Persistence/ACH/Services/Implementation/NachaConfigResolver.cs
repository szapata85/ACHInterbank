using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaConfigResolver : INachaConfigResolver
{
    private readonly AchDbContext _context;

    public NachaConfigResolver(AchDbContext context)
    {
        _context = context;
    }

    public async Task<NachaConfigResolutionResult> ResolveAsync(NachaConfigResolutionRequest request, CancellationToken ct = default)
    {
        var trace = new List<string>();
        var warnings = new List<string>();

        var date = request.ProcessDateUtc.Date;
        var profileCandidates = await _context.CfgProfiles
            .AsNoTracking()
            .Include(x => x.Status)
            .Include(x => x.ClearingHouse)
            .Include(x => x.FlowType)
            .Include(x => x.Direction)
            .Include(x => x.ServiceClass)
            .Include(x => x.Tags)
            .Include(x => x.Records)
                .ThenInclude(x => x.RecordCode)
            .Where(x => x.ClearingHouse.Code == request.ClearingHouseCode
                        && x.FlowType.Code == request.FlowTypeCode
                        && x.Direction.Code == request.DirectionCode
                        && (x.ServiceClass == null || x.ServiceClass.Code == request.ServiceClassCode)
                        && x.Status.Code == "PUBLICADO"
                        && x.EffectiveFrom.Date <= date
                        && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= date))
            .OrderBy(x => x.ContextPriority)
            .ThenByDescending(x => x.VersionMajor)
            .ThenByDescending(x => x.VersionMinor)
            .ToListAsync(ct);

        trace.Add($"Perfiles candidatos encontrados: {profileCandidates.Count}.");

        if (profileCandidates.Count == 0)
        {
            warnings.Add("No hay perfiles publicados para el contexto solicitado.");
            return new NachaConfigResolutionResult { Success = false, Trace = trace, Warnings = warnings, UsedFallback = true };
        }

        var topPriority = profileCandidates[0].ContextPriority;
        var topProfiles = profileCandidates.Where(x => x.ContextPriority == topPriority).ToList();
        if (topProfiles.Count > 1)
        {
            warnings.Add($"Ambigüedad de perfiles en prioridad {topPriority}: {string.Join(",", topProfiles.Select(x => x.ProfileCode))}.");
        }

        var profile = topProfiles
            .OrderByDescending(x => x.VersionMajor)
            .ThenByDescending(x => x.VersionMinor)
            .First();

        trace.Add($"Perfil seleccionado: {profile.ProfileCode} (Id={profile.Id}).");

        var neededRecordCodes = request.RecordCodes.Count > 0
            ? request.RecordCodes.ToHashSet(StringComparer.OrdinalIgnoreCase)
            : profile.Records.Where(x => x.IsEnabled).Select(x => x.RecordCode.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var layouts = await _context.CfgLayoutVariants
            .AsNoTracking()
            .Include(x => x.RecordCode)
            .Include(x => x.Status)
            .Include(x => x.Fields.Where(f => f.IsEnabled))
                .ThenInclude(f => f.SourceDefinition)
                    .ThenInclude(sd => sd.DataSourceType)
            .Include(x => x.Fields.Where(f => f.IsEnabled))
                .ThenInclude(f => f.Rules.Where(r => r.IsEnabled))
                    .ThenInclude(r => r.RuleType)
            .Where(x => x.ProfileId == profile.Id
                        && neededRecordCodes.Contains(x.RecordCode.Code)
                        && x.Status.Code == "PUBLICADO"
                        && x.EffectiveFrom.Date <= date
                        && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= date))
            .ToListAsync(ct);

        var selectedLayouts = new Dictionary<string, CfgLayoutVariant>(StringComparer.OrdinalIgnoreCase);
        var variantsByRecordCode = new Dictionary<string, IReadOnlyList<CfgLayoutVariant>>(StringComparer.OrdinalIgnoreCase);

        foreach (var recordCode in neededRecordCodes)
        {
            var candidates = layouts
                .Where(x => string.Equals(x.RecordCode.Code, recordCode, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.IsDefaultForRecord)
                .ThenBy(x => x.Priority)
                .ToList();

            variantsByRecordCode[recordCode] = candidates.AsReadOnly();

            if (candidates.Count == 0)
            {
                warnings.Add($"No existe layout publicado para RecordCode={recordCode}.");
                continue;
            }

            var filtered = ApplySelectionPredicate(candidates, request.SelectionContext, warnings, recordCode);
            var chosenPool = filtered.Count > 0 ? filtered : candidates;
            var firstPriority = chosenPool[0].Priority;
            var firstPriorityCandidates = chosenPool.Where(x => x.Priority == firstPriority).ToList();

            if (firstPriorityCandidates.Count > 1)
            {
                warnings.Add($"Ambigüedad de layout para RecordCode={recordCode} en prioridad {firstPriority}: {string.Join(",", firstPriorityCandidates.Select(x => x.VariantCode))}.");
            }

            var chosen = firstPriorityCandidates
                .OrderByDescending(x => x.IsDefaultForRecord)
                .ThenBy(x => x.Id)
                .First();

            selectedLayouts[recordCode] = chosen;
            trace.Add($"Layout seleccionado para RecordCode={recordCode}: {chosen.VariantCode} (Id={chosen.Id}).");
        }

        return new NachaConfigResolutionResult
        {
            Success = selectedLayouts.Count > 0,
            UsedFallback = selectedLayouts.Count != neededRecordCodes.Count,
            Profile = profile,
            LayoutsByRecordCode = selectedLayouts,
            LayoutVariantsByRecordCode = variantsByRecordCode,
            Trace = trace,
            Warnings = warnings
        };
    }

    private static List<CfgLayoutVariant> ApplySelectionPredicate(
        List<CfgLayoutVariant> candidates,
        IReadOnlyDictionary<string, string> selectionContext,
        List<string> warnings,
        string recordCode)
    {
        var filtered = new List<CfgLayoutVariant>();

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate.SelectionPredicateJson))
            {
                filtered.Add(candidate);
                continue;
            }

            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(candidate.SelectionPredicateJson);
                if (dict == null || dict.Count == 0)
                {
                    filtered.Add(candidate);
                    continue;
                }

                var matches = dict.All(kv => selectionContext.TryGetValue(kv.Key, out var value)
                                             && string.Equals(value, kv.Value, StringComparison.OrdinalIgnoreCase));

                if (matches)
                {
                    filtered.Add(candidate);
                }
            }
            catch
            {
                warnings.Add($"SelectionPredicateJson inválido en layout {candidate.VariantCode} para RecordCode={recordCode}.");
            }
        }

        return filtered;
    }
}
