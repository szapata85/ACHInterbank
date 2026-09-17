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
            || !string.Equals(snapshot.Profile.ProfileCode, selected.ProfileCode, StringComparison.Ordinal)
            || !string.Equals(snapshot.Profile.ClearingHouseCode, selected.ClearingHouse.Code, StringComparison.Ordinal)
            || !string.Equals(snapshot.Profile.DirectionCode, selected.Direction.Code, StringComparison.Ordinal)
            || !string.Equals(snapshot.Profile.FlowTypeCode, selected.FlowType.Code, StringComparison.Ordinal)
            || !string.Equals(snapshot.Profile.ServiceClassCode, selected.ServiceClass?.Code, StringComparison.Ordinal))
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

    public async Task<NachaConfigResolutionResult> ResolvePublishedInboundAsync(
        NachaConfigResolutionRequest request,
        IReadOnlyList<string> physicalRecords,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.ClearingHouseCode))
        {
            return InboundFailure(NachaProfileSelectionStatus.ClearingHouseUndetermined, "INBOUND_CHAMBER_MISSING");
        }
        if (!string.Equals(request.DirectionCode, "ENTRADA", StringComparison.OrdinalIgnoreCase))
        {
            return InboundFailure(NachaProfileSelectionStatus.ProfileNotFound, "INBOUND_DIRECTION_REQUIRED");
        }

        var date = request.ProcessDateUtc.Date;
        var profiles = await _context.CfgProfiles.AsNoTracking()
            .Include(profile => profile.Status)
            .Include(profile => profile.ClearingHouse)
            .Include(profile => profile.Direction)
            .Include(profile => profile.FlowType)
            .Include(profile => profile.ServiceClass)
            .Where(profile => profile.ClearingHouse.Code == request.ClearingHouseCode
                              && profile.Direction.Code == "ENTRADA"
                              && (profile.FlowType.Code == "ORIGINAL" || profile.FlowType.Code == "PRENOTIFICACION")
                              && (!request.RequestedVersionMajor.HasValue || profile.VersionMajor == request.RequestedVersionMajor.Value)
                              && (!request.RequestedVersionMinor.HasValue || profile.VersionMinor == request.RequestedVersionMinor.Value))
            .ToListAsync(ct);
        var active = profiles.Where(profile => profile.Status.Code == "PUBLICADO"
                                               && profile.EffectiveFrom.Date <= date
                                               && (!profile.EffectiveTo.HasValue || profile.EffectiveTo.Value.Date >= date))
            .ToArray();
        if (active.Length == 0)
        {
            return InboundFailure(NachaProfileSelectionStatus.ProfileNotFound, "INBOUND_PUBLISH_AUTHORITY_MISSING");
        }

        var winners = new List<CfgProfile>();
        foreach (var group in active.GroupBy(profile => (profile.FlowType.Code, Service: profile.ServiceClass?.Code)))
        {
            var selection = await SelectCandidateAsync(new NachaConfigResolutionRequest
            {
                ClearingHouseCode = request.ClearingHouseCode,
                FlowTypeCode = group.Key.Code,
                DirectionCode = "ENTRADA",
                ServiceClassCode = group.Key.Service,
                ProcessDateUtc = request.ProcessDateUtc,
                RequestedVersionMajor = request.RequestedVersionMajor,
                RequestedVersionMinor = request.RequestedVersionMinor,
                RecordCodes = request.RecordCodes
            }, ct);
            if (!selection.Success || selection.Profile is null)
            {
                return InboundFailure(selection.SelectionStatus, "INBOUND_PUBLICATION_AMBIGUOUS");
            }
            winners.Add(selection.Profile);
        }

        var selected = new List<(CfgProfile Profile, NachaPublicationSnapshot Snapshot, string? Service)>();
        var meanings = new Dictionary<string, NachaTransactionCodeSemanticRule>(StringComparer.Ordinal);
        var readableCodes = false;
        var recognizedCodes = false;
        var batchRecords = physicalRecords.Where(record => record.Length > 0 && record[0] == '5').ToArray();
        var entryRecords = physicalRecords.Where(record => record.Length > 0 && record[0] == '6').ToArray();
        if (batchRecords.Length == 0 || entryRecords.Length == 0)
        {
            return InboundFailure(NachaProfileSelectionStatus.ProfileNotFound, "INBOUND_BOOTSTRAP_RECORDS_MISSING");
        }

        foreach (var profile in winners)
        {
            var publications = await _context.HistConfigSnapshots.AsNoTracking()
                .Where(row => row.ProfileId == profile.Id && row.SnapshotType == "PUBLISH")
                .Select(row => row.SnapshotJson)
                .ToListAsync(ct);
            if (publications.Count != 1)
            {
                return InboundFailure(NachaProfileSelectionStatus.SemanticContractMissing, "INBOUND_PUBLISH_SNAPSHOT_MISSING");
            }
            var read = NachaPublicationSnapshotSerializer.ReadForOrdinaryGeneration(publications[0]);
            if (!read.IsSupported || read.Snapshot is null)
            {
                return InboundFailure(NachaProfileSelectionStatus.SemanticContractInvalid, $"INBOUND_PUBLISH_SNAPSHOT_INVALID: {read.Status}");
            }
            var snapshot = read.Snapshot;
            if (snapshot.Profile.ProfileId != profile.Id
                || snapshot.Profile.ProfileCode != profile.ProfileCode
                || snapshot.Profile.VersionMajor != profile.VersionMajor
                || snapshot.Profile.VersionMinor != profile.VersionMinor
                || snapshot.Profile.ClearingHouseCode != request.ClearingHouseCode
                || snapshot.Profile.DirectionCode != "ENTRADA"
                || snapshot.Profile.FlowTypeCode != profile.FlowType.Code
                || snapshot.Profile.ServiceClassCode != profile.ServiceClass?.Code)
            {
                return InboundFailure(NachaProfileSelectionStatus.SemanticContractInvalid, "INBOUND_PUBLISH_IDENTITY_CONFLICT");
            }
            var semantic = NachaTransactionCodeSemanticMetadata.Resolve(snapshot.Records);
            if (semantic.Status != NachaTransactionCodeSemanticMetadataStatus.Resolved || semantic.Contract is null)
            {
                return InboundFailure(
                    semantic.Status == NachaTransactionCodeSemanticMetadataStatus.NotPresent
                        ? NachaProfileSelectionStatus.SemanticContractMissing
                        : NachaProfileSelectionStatus.SemanticContractInvalid,
                    $"INBOUND_TXCODE_METADATA_INVALID: {semantic.ErrorCode ?? semantic.Status.ToString()}");
            }

            NachaProfileRecordReader reader;
            try
            {
                reader = NachaProfileRecordReader.FromPublication(snapshot);
            }
            catch (InvalidOperationException)
            {
                return InboundFailure(NachaProfileSelectionStatus.SemanticContractInvalid, "INBOUND_PUBLISH_LAYOUT_INVALID");
            }
            if (!TryGetPublishedSecValues(snapshot, out var allowedServices))
            {
                return InboundFailure(NachaProfileSelectionStatus.SemanticContractInvalid, "INBOUND_PUBLISH_SERVICE_METADATA_MISSING");
            }

            string[] services;
            string[] codes;
            try
            {
                services = batchRecords.Select(record => reader.Read(record, "5", "STANDARDENTRYCLASSCODE").Trim().ToUpperInvariant()).ToArray();
                codes = entryRecords.Select(record => reader.Read(record, "6", "TRANSACTIONCODE").Trim()).ToArray();
            }
            catch (InvalidOperationException)
            {
                continue;
            }
            readableCodes = true;

            var rules = new List<NachaTransactionCodeSemanticRule>(codes.Length);
            foreach (var code in codes)
            {
                if (!semantic.Contract.TryGetRule(code, out var rule))
                {
                    rules.Clear();
                    break;
                }
                if (meanings.TryGetValue(code, out var existing) && existing != rule)
                {
                    return InboundFailure(NachaProfileSelectionStatus.ProfileAmbiguous, "INBOUND_TXCODE_AUTHORITY_CONFLICT");
                }
                meanings[code] = rule;
                rules.Add(rule);
            }
            if (rules.Count == codes.Length)
            {
                recognizedCodes = true;
            }
            if (rules.Count != codes.Length || !services.All(allowedServices.Contains))
            {
                continue;
            }
            if (snapshot.Profile.ServiceClassCode is { } requiredService
                && !services.All(service => string.Equals(service, requiredService, StringComparison.Ordinal)))
            {
                continue;
            }
            var flow = rules.All(rule => rule.IsPrenotification) ? "PRENOTIFICACION" : "ORIGINAL";
            if (snapshot.Profile.FlowTypeCode != flow)
            {
                continue;
            }
            selected.Add((profile, snapshot, services.Distinct(StringComparer.Ordinal).Count() == 1 ? services[0] : null));
        }

        if (selected.Count != 1)
        {
            return InboundFailure(selected.Count == 0
                    ? NachaProfileSelectionStatus.ProfileNotFound
                    : NachaProfileSelectionStatus.ProfileAmbiguous,
                selected.Count == 0
                    ? readableCodes && !recognizedCodes
                        ? "INBOUND_TXCODE_UNSUPPORTED"
                        : "INBOUND_PUBLICATION_NO_COMPATIBLE_AUTHORITY"
                    : "INBOUND_PUBLICATION_AMBIGUOUS");
        }

        var authority = selected[0];
        var resolved = await ResolvePublishedOrdinaryAsync(new NachaConfigResolutionRequest
        {
            ClearingHouseCode = request.ClearingHouseCode,
            FlowTypeCode = authority.Snapshot.Profile.FlowTypeCode,
            DirectionCode = "ENTRADA",
            ServiceClassCode = authority.Service,
            ProcessDateUtc = request.ProcessDateUtc,
            RequestedVersionMajor = authority.Profile.VersionMajor,
            RequestedVersionMinor = authority.Profile.VersionMinor,
            RecordCodes = request.RecordCodes,
            SelectionContext = new Dictionary<string, string>(request.SelectionContext, StringComparer.OrdinalIgnoreCase)
            {
                ["MessageType"] = authority.Snapshot.Profile.FlowTypeCode == "PRENOTIFICACION"
                    ? "Prenotification"
                    : "Original",
                ["AddendaType"] = "05"
            }
        }, ct);
        return resolved.Success && resolved.Profile?.Id == authority.Profile.Id && !resolved.UsedFallback
            ? resolved
            : InboundFailure(NachaProfileSelectionStatus.ProfileAmbiguous, "INBOUND_PUBLISH_RESOLUTION_CONFLICT");
    }

    internal static bool TryGetPublishedSecValues(
        NachaPublicationSnapshot snapshot,
        out HashSet<string> allowed)
    {
        allowed = new HashSet<string>(StringComparer.Ordinal);
        var entryVariants = snapshot.LayoutVariants.Where(variant => variant.RecordCode == "6").ToArray();
        if (entryVariants.Length != 1 || !entryVariants[0].IsDefaultForRecord)
        {
            return false;
        }
        var variants = snapshot.LayoutVariants.Where(variant => variant.RecordCode == "5" && variant.IsDefaultForRecord).ToArray();
        if (variants.Length != 1
            || snapshot.LayoutVariants.Count(variant => variant.RecordCode == "5") != 1)
        {
            return false;
        }
        var fields = variants[0].Fields.Where(field => field.IsEnabled && field.FieldCode == "STANDARDENTRYCLASSCODE").ToArray();
        if (fields.Length != 1)
        {
            return false;
        }
        var rules = fields[0].Rules.Where(rule => rule.IsEnabled && rule.RuleTypeCode == "ENUM").ToArray();
        if (rules.Length != 1 || rules[0].RuleConfiguration is not { } configuration
            || configuration.ValueKind != JsonValueKind.Object
            || !configuration.TryGetProperty("allowedValues", out var values)
            || values.ValueKind != JsonValueKind.Array)
        {
            return false;
        }
        foreach (var value in values.EnumerateArray())
        {
            if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            {
                return false;
            }
            if (!allowed.Add(value.GetString()!.Trim().ToUpperInvariant()))
            {
                return false;
            }
        }
        return allowed.Count > 0;
    }

    private static NachaConfigResolutionResult InboundFailure(NachaProfileSelectionStatus status, string code)
        => new()
        {
            Success = false,
            SelectionStatus = status,
            Warnings = [code],
            Trace = [$"Selección cerrada: {code}."]
        };

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
        var cardinalityMetadata = NachaAddendaCardinalityMetadata.Resolve(
            profile.Tags.Select(tag => new KeyValuePair<string, string>(tag.TagKey, tag.TagValue)));
        if (cardinalityMetadata.Status == NachaAddendaCardinalityMetadataStatus.Invalid
            || (request.RequireCardinalityPolicy
                && cardinalityMetadata.Status == NachaAddendaCardinalityMetadataStatus.NotPresent))
        {
            return Failure(
                NachaProfileSelectionStatus.SemanticContractInvalid,
                cardinalityMetadata.Error ?? "CARDINALITY_POLICY_UNRESOLVED",
                trace,
                warnings,
                profile);
        }
        if (cardinalityMetadata.Policy is { } cardinalityPolicy
            && (cardinalityPolicy.DirectionCode != profile.Direction.Code
                || cardinalityPolicy.FlowTypeCode != profile.FlowType.Code))
        {
            return Failure(
                NachaProfileSelectionStatus.SemanticContractInvalid,
                "CARDINALITY_POLICY_CONTEXT_MISMATCH",
                trace,
                warnings,
                profile);
        }
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

        NachaTransactionCodeSemanticContract? transactionCodeContract = null;
        if (neededRecordCodes.Contains("6"))
        {
            var transactionCodeMetadata = NachaTransactionCodeSemanticMetadata.Resolve(profile.Records);
            if (transactionCodeMetadata.Status == NachaTransactionCodeSemanticMetadataStatus.Invalid)
            {
                return Failure(
                    NachaProfileSelectionStatus.SemanticContractInvalid,
                    $"{transactionCodeMetadata.ErrorCode}: {transactionCodeMetadata.Error}",
                    trace,
                    warnings,
                    profile);
            }

            transactionCodeContract = transactionCodeMetadata.Contract;
            if (transactionCodeContract is not null)
            {
                trace.Add($"Contrato semántico TransactionCode resuelto desde CfgRuleSet T6: {transactionCodeContract.Rules.Count} tuplas.");
            }
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
            CardinalityPolicy = cardinalityMetadata.Policy,
            SettlementPolicy = settlementPolicyMetadata.Policy,
            SemanticContract = semanticContract,
            TransactionCodeContract = transactionCodeContract,
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
