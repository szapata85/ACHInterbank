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

    private async Task<NachaConfigResolutionResult> SelectCandidateAsync(NachaConfigResolutionRequest request, CancellationToken ct)
    {
        var trace = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(request.ClearingHouseCode))
        {
            return Failure(
                NachaProfileSelectionStatus.ClearingHouseUndetermined,
                "No se recibió una cámara explícita para seleccionar el perfil.",
                trace,
                warnings);
        }

        var clearingHouseCode = request.ClearingHouseCode.Trim().ToUpperInvariant();
        var flowTypeCode = request.FlowTypeCode.Trim().ToUpperInvariant();
        var directionCode = request.DirectionCode.Trim().ToUpperInvariant();
        var serviceClassCode = request.ServiceClassCode?.Trim().ToUpperInvariant();
        var date = request.ProcessDateUtc.Date;
        var dimensionCandidates = await _context.CfgProfiles
            .AsNoTracking()
            .Include(x => x.Status)
            .Include(x => x.ClearingHouse)
            .Include(x => x.FlowType)
            .Include(x => x.Direction)
            .Include(x => x.ServiceClass)
            .Include(x => x.Tags)
            .Include(x => x.Records)
                .ThenInclude(x => x.RecordCode)
            .Include(x => x.Records)
                .ThenInclude(x => x.SemanticRuleSet)
                    .ThenInclude(x => x!.Rules)
                        .ThenInclude(x => x.RuleType)
            .Where(x => x.ClearingHouse.Code == clearingHouseCode
                        && x.FlowType.Code == flowTypeCode
                        && x.Direction.Code == directionCode
                        && (x.ServiceClass == null || x.ServiceClass.Code == serviceClassCode))
            .ToListAsync(ct);

        trace.Add($"Perfiles para las dimensiones exactas: {dimensionCandidates.Count}.");

        if (dimensionCandidates.Count == 0)
        {
            return Failure(
                NachaProfileSelectionStatus.ProfileNotFound,
                "No existe un perfil para la combinación de cámara, flujo, dirección y clase de servicio.",
                trace,
                warnings);
        }

        var versionCandidates = dimensionCandidates
            .Where(x => !request.RequestedVersionMajor.HasValue || x.VersionMajor == request.RequestedVersionMajor.Value)
            .Where(x => !request.RequestedVersionMinor.HasValue || x.VersionMinor == request.RequestedVersionMinor.Value)
            .ToList();

        if (versionCandidates.Count == 0)
        {
            return Failure(
                NachaProfileSelectionStatus.ProfileVersionUnsupported,
                $"La versión solicitada no está disponible para el contexto. Version={FormatRequestedVersion(request)}.",
                trace,
                warnings);
        }

        var activeCandidates = versionCandidates
            .Where(x => string.Equals(x.Status.Code, "PUBLICADO", StringComparison.OrdinalIgnoreCase)
                        && x.EffectiveFrom.Date <= date
                        && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= date))
            .Where(x => !request.RequireHomologated || IsNormativelyEnabled(x))
            .ToList();

        if (!string.IsNullOrWhiteSpace(serviceClassCode)
            && dimensionCandidates.Any(x => x.ServiceClass is not null))
        {
            activeCandidates = activeCandidates
                .Where(x => x.ServiceClass is not null
                            && string.Equals(x.ServiceClass.Code, serviceClassCode, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (activeCandidates.Count == 0)
        {
            return Failure(
                NachaProfileSelectionStatus.ProfileInactive,
                request.RequireHomologated
                    ? "El perfil existe, pero no está publicado, vigente y homologado para el contexto solicitado."
                    : "El perfil existe, pero no está publicado o vigente para la fecha solicitada.",
                trace,
                warnings);
        }

        var topPriority = activeCandidates.Min(x => x.ContextPriority);
        var topPriorityCandidates = activeCandidates
            .Where(x => x.ContextPriority == topPriority)
            .ToList();
        var highestMajor = topPriorityCandidates.Max(x => x.VersionMajor);
        var highestMinor = topPriorityCandidates
            .Where(x => x.VersionMajor == highestMajor)
            .Max(x => x.VersionMinor);
        var topProfiles = topPriorityCandidates
            .Where(x => x.VersionMajor == highestMajor && x.VersionMinor == highestMinor)
            .OrderBy(x => x.ProfileCode)
            .ToList();

        if (topProfiles.Count != 1)
        {
            return Failure(
                NachaProfileSelectionStatus.ProfileAmbiguous,
                $"Existen {topProfiles.Count} perfiles indistinguibles en prioridad {topPriority} y versión {highestMajor}.{highestMinor}: {string.Join(",", topProfiles.Select(x => x.ProfileCode))}.",
                trace,
                warnings);
        }

        var profile = topProfiles[0];

        trace.Add($"Perfil seleccionado: {profile.ProfileCode} (Id={profile.Id}).");
        return new NachaConfigResolutionResult
        {
            Success = true,
            SelectionStatus = NachaProfileSelectionStatus.ProfileSelected,
            Profile = profile,
            Trace = trace,
            Warnings = warnings
        };
    }

    public async Task<NachaConfigResolutionResult> ResolveAsync(NachaConfigResolutionRequest request, CancellationToken ct = default)
    {
        var selection = await SelectCandidateAsync(request, ct);
        if (!selection.Success || selection.Profile is null)
        {
            return selection;
        }

        var profile = selection.Profile;
        var trace = selection.Trace;
        var warnings = selection.Warnings;
        var date = request.ProcessDateUtc.Date;
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
        return ResolveGraph(request, profile, layouts, trace, warnings);
    }

    public async Task<NachaConfigResolutionResult> ResolvePublishedOrdinaryAsync(
        NachaConfigResolutionRequest request,
        CancellationToken ct = default)
    {
        var selection = await SelectCandidateAsync(request, ct);
        if (!selection.Success || selection.Profile is null)
        {
            return selection;
        }

        var selected = selection.Profile;
        var matches = await _context.HistConfigSnapshots.AsNoTracking()
            .Where(row => row.ProfileId == selected.Id
                          && row.VersionMajor == selected.VersionMajor
                          && row.VersionMinor == selected.VersionMinor
                          && row.SnapshotType == "PUBLISH")
            .Select(row => row.SnapshotJson)
            .ToListAsync(ct);
        if (matches.Count != 1)
        {
            throw new InvalidOperationException(
                $"La publicación {selected.ProfileCode} {selected.VersionMajor}.{selected.VersionMinor} requiere exactamente un snapshot PUBLISH; encontrados: {matches.Count}.");
        }

        var read = NachaPublicationSnapshotSerializer.ReadForOrdinaryGeneration(matches[0]);
        if (!read.IsSupported || read.Snapshot is null)
        {
            throw new InvalidOperationException(
                $"El snapshot PUBLISH seleccionado no está completo para generación ordinaria: {read.Status}: {read.Error}");
        }

        var snapshot = read.Snapshot;
        if (snapshot.Profile.ProfileId != selected.Id
            || snapshot.Profile.VersionMajor != selected.VersionMajor
            || snapshot.Profile.VersionMinor != selected.VersionMinor
            || !string.Equals(snapshot.Profile.ProfileCode, selected.ProfileCode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("La identidad del snapshot PUBLISH no coincide con la publicación seleccionada.");
        }

        var (profile, variants) = NachaPublicationSnapshotMaterializer.Materialize(snapshot);
        var date = request.ProcessDateUtc.Date;
        var neededRecordCodes = request.RecordCodes.Count > 0
            ? request.RecordCodes.ToHashSet(StringComparer.OrdinalIgnoreCase)
            : profile.Records.Where(record => record.IsEnabled)
                .Select(record => record.RecordCode.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var applicable = variants.Where(variant => neededRecordCodes.Contains(variant.RecordCode.Code)
                                                   && string.Equals(variant.Status.Code, "PUBLICADO", StringComparison.OrdinalIgnoreCase)
                                                   && variant.EffectiveFrom.Date <= date
                                                   && (!variant.EffectiveTo.HasValue || variant.EffectiveTo.Value.Date >= date))
            .ToList();
        return ResolveGraph(request, profile, applicable, selection.Trace, selection.Warnings,
            snapshot.StandardEntryClassMappings);
    }

    private static NachaConfigResolutionResult ResolveGraph(
        NachaConfigResolutionRequest request,
        CfgProfile profile,
        List<CfgLayoutVariant> layouts,
        List<string> trace,
        List<string> warnings,
        IReadOnlyList<NachaPublicationSnapshotSecMapping>? standardEntryClassMappings = null)
    {

        var outboundPolicyMetadata = NachaOutboundPolicyMetadata.Resolve(
            profile.ProfileCode,
            profile.Tags.Select(tag => new KeyValuePair<string, string>(tag.TagKey, tag.TagValue)));
        if (outboundPolicyMetadata.Status == NachaOutboundPolicyMetadataStatus.Invalid)
        {
            return Failure(
                NachaProfileSelectionStatus.OutboundPolicyInvalid,
                outboundPolicyMetadata.Error ?? "La política outbound del perfil es inválida.",
                trace,
                warnings,
                profile);
        }
        if (request.RequireOutboundPolicy
            && outboundPolicyMetadata.Status == NachaOutboundPolicyMetadataStatus.NotPresent)
        {
            return Failure(
                NachaProfileSelectionStatus.OutboundPolicyMissing,
                $"El perfil oficial '{profile.ProfileCode}' no publica la política outbound requerida.",
                trace,
                warnings,
                profile);
        }

        var settlementPolicyMetadata = NachaSettlementPolicyMetadata.Resolve(
            profile.Tags.Select(tag => new KeyValuePair<string, string>(tag.TagKey, tag.TagValue)));
        if (settlementPolicyMetadata.Status == NachaSettlementPolicyMetadataStatus.Invalid)
        {
            return Failure(
                NachaProfileSelectionStatus.SettlementPolicyInvalid,
                settlementPolicyMetadata.Error ?? "La política de settlement del perfil es inválida.",
                trace,
                warnings,
                profile);
        }

        var requiresSettlementPolicy = request.RecordCodes.Count == 0
            || request.RecordCodes.Contains("5", StringComparer.OrdinalIgnoreCase);
        if (requiresSettlementPolicy
            && settlementPolicyMetadata.Status == NachaSettlementPolicyMetadataStatus.NotPresent)
        {
            return Failure(
                NachaProfileSelectionStatus.SettlementPolicyMissing,
                $"El perfil oficial '{profile.ProfileCode}' no publica la política de settlement requerida.",
                trace,
                warnings,
                profile);
        }
        if (settlementPolicyMetadata.Policy.HasValue)
        {
            trace.Add($"SettlementPolicy resuelta desde CfgProfileTag: {settlementPolicyMetadata.Policy.Value}.");
        }

        var neededRecordCodes = request.RecordCodes.Count > 0
            ? request.RecordCodes.ToHashSet(StringComparer.OrdinalIgnoreCase)
            : profile.Records.Where(x => x.IsEnabled).Select(x => x.RecordCode.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        NachaServiceClassSemanticContract? semanticContract = null;
        if (neededRecordCodes.Contains("5"))
        {
            var semanticMetadata = NachaSemanticContractMetadata.Resolve(profile.Records);
            if (semanticMetadata.Status != NachaSemanticContractMetadataStatus.Resolved)
            {
                return Failure(
                    semanticMetadata.Status == NachaSemanticContractMetadataStatus.NotPresent
                        ? NachaProfileSelectionStatus.SemanticContractMissing
                        : NachaProfileSelectionStatus.SemanticContractInvalid,
                    $"{semanticMetadata.ErrorCode}: {semanticMetadata.Error}",
                    trace,
                    warnings,
                    profile);
            }

            semanticContract = semanticMetadata.Contract;
            trace.Add($"Contrato semántico ServiceClassCode resuelto desde CfgRuleSet: {string.Join(",", semanticContract!.Rules.Select(rule => rule.ServiceClassCode))}.");
        }

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
                return Failure(
                    NachaProfileSelectionStatus.ProfileNotFound,
                    $"No existe layout publicado para RecordCode={recordCode}.",
                    trace,
                    warnings,
                    profile);
            }

            var chosenPool = ApplySelectionPredicate(candidates, request.SelectionContext, warnings, recordCode);
            if (chosenPool.Count == 0)
            {
                return Failure(
                    NachaProfileSelectionStatus.ProfileNotFound,
                    $"No existe layout aplicable al contexto para RecordCode={recordCode}.",
                    trace,
                    warnings,
                    profile);
            }

            var firstPriority = chosenPool.Min(x => x.Priority);
            var firstPriorityCandidates = chosenPool.Where(x => x.Priority == firstPriority).ToList();
            var defaultCandidates = firstPriorityCandidates.Where(x => x.IsDefaultForRecord).ToList();
            var finalists = defaultCandidates.Count > 0 ? defaultCandidates : firstPriorityCandidates;

            if (finalists.Count != 1)
            {
                return Failure(
                    NachaProfileSelectionStatus.ProfileAmbiguous,
                    $"Existen {finalists.Count} layouts indistinguibles para RecordCode={recordCode} en prioridad {firstPriority}: {string.Join(",", finalists.Select(x => x.VariantCode))}.",
                    trace,
                    warnings,
                    profile);
            }

            var chosen = finalists[0];

            selectedLayouts[recordCode] = chosen;
            trace.Add($"Layout seleccionado para RecordCode={recordCode}: {chosen.VariantCode} (Id={chosen.Id}).");
        }

        return new NachaConfigResolutionResult
        {
            Success = true,
            SelectionStatus = NachaProfileSelectionStatus.ProfileSelected,
            UsedFallback = false,
            Profile = profile,
            OutboundPolicy = outboundPolicyMetadata.Policy,
            SettlementPolicy = settlementPolicyMetadata.Policy,
            SemanticContract = semanticContract,
            StandardEntryClassMappings = standardEntryClassMappings,
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
        var predicateMatches = new List<CfgLayoutVariant>();
        var defaults = new List<CfgLayoutVariant>();

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate.SelectionPredicateJson))
            {
                defaults.Add(candidate);
                continue;
            }

            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(candidate.SelectionPredicateJson);
                if (dict == null || dict.Count == 0)
                {
                    defaults.Add(candidate);
                    continue;
                }

                var matches = dict.All(kv => selectionContext.TryGetValue(kv.Key, out var value)
                                             && string.Equals(value, kv.Value, StringComparison.OrdinalIgnoreCase));

                if (matches)
                {
                    predicateMatches.Add(candidate);
                }
            }
            catch
            {
                warnings.Add($"SelectionPredicateJson inválido en layout {candidate.VariantCode} para RecordCode={recordCode}.");
            }
        }

        return predicateMatches.Count > 0 ? predicateMatches : defaults;
    }

    private static NachaConfigResolutionResult Failure(
        NachaProfileSelectionStatus status,
        string warning,
        List<string> trace,
        List<string> warnings,
        CfgProfile? profile = null)
    {
        warnings.Add(warning);
        trace.Add($"Selección cerrada: {status}.");
        return new NachaConfigResolutionResult
        {
            Success = false,
            SelectionStatus = status,
            UsedFallback = false,
            Profile = profile,
            Trace = trace,
            Warnings = warnings
        };
    }

    private static bool IsNormativelyEnabled(CfgProfile profile)
    {
        var tags = profile.Tags
            .GroupBy(x => x.TagKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Last().TagValue, StringComparer.OrdinalIgnoreCase);
        return tags.TryGetValue("IsHomologated", out var homologated)
               && bool.TryParse(homologated, out var isHomologated)
               && isHomologated
               && (!tags.TryGetValue("IsPlaceholder", out var placeholder)
                   || !bool.TryParse(placeholder, out var isPlaceholder)
                   || !isPlaceholder);
    }

    private static string FormatRequestedVersion(NachaConfigResolutionRequest request)
    {
        var major = request.RequestedVersionMajor?.ToString() ?? "*";
        var minor = request.RequestedVersionMinor?.ToString() ?? "*";
        return $"{major}.{minor}";
    }
}
