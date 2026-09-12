using Cfa.ACHInterbank.Domain.Models.ACH.Config;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

// These are the ordinary production request dimensions used by the current builder and inbound caller.
// CENIT outbound is supported only where its existing LIVE generation gate permits execution.
internal sealed record NachaOrdinaryProductionProfileScope(
    string ClearingHouseCode,
    string FlowTypeCode,
    string DirectionCode,
    string? ServiceClassCode,
    int? RequestedVersionMajor,
    int? RequestedVersionMinor,
    bool RequireHomologated = false)
{
    internal static IReadOnlyList<NachaOrdinaryProductionProfileScope> Supported { get; } = CreateSupported();

    internal static IReadOnlyList<CfgProfile> FindMandatoryWinners(IReadOnlyList<CfgProfile> profiles)
    {
        var winners = new Dictionary<int, CfgProfile>();
        foreach (var scope in Supported)
        {
            foreach (var winner in scope.FindWinners(profiles))
            {
                winners.TryAdd(winner.Id, winner);
            }
        }
        return winners.Values.OrderBy(profile => profile.Id).ToArray();
    }

    internal IReadOnlyList<CfgProfile> FindWinners(IReadOnlyList<CfgProfile> profiles)
    {
        // Keep the same order as NachaConfigResolver: dimensions, version, publication/date,
        // homologation, specific-service preference, priority, and highest version.
        var dimensions = profiles.Where(profile =>
                Equal(profile.ClearingHouse.Code, ClearingHouseCode)
                && Equal(profile.FlowType.Code, FlowTypeCode)
                && Equal(profile.Direction.Code, DirectionCode)
                && (profile.ServiceClass is null || Equal(profile.ServiceClass.Code, ServiceClassCode)))
            .ToArray();
        var versions = dimensions.Where(profile =>
                (!RequestedVersionMajor.HasValue || profile.VersionMajor == RequestedVersionMajor)
                && (!RequestedVersionMinor.HasValue || profile.VersionMinor == RequestedVersionMinor))
            .ToArray();
        var dates = versions.Select(profile => profile.EffectiveFrom.Date)
            .Concat(versions.Where(profile => profile.EffectiveTo.HasValue && profile.EffectiveTo.Value.Date < DateTime.MaxValue.Date)
                .Select(profile => profile.EffectiveTo!.Value.Date.AddDays(1)))
            .Distinct()
            .Order()
            .ToArray();
        var winners = new Dictionary<int, CfgProfile>();
        foreach (var date in dates)
        {
            var active = versions.Where(profile =>
                    Equal(profile.Status.Code, "PUBLICADO")
                    && profile.EffectiveFrom.Date <= date
                    && (!profile.EffectiveTo.HasValue || profile.EffectiveTo.Value.Date >= date)
                    && (!RequireHomologated || IsNormativelyEnabled(profile)))
                .ToArray();
            if (!string.IsNullOrWhiteSpace(ServiceClassCode)
                && dimensions.Any(profile => profile.ServiceClass is not null))
            {
                active = active.Where(profile => profile.ServiceClass is not null
                                                  && Equal(profile.ServiceClass.Code, ServiceClassCode)).ToArray();
            }
            if (active.Length == 0)
            {
                continue;
            }

            var priority = active.Min(profile => profile.ContextPriority);
            var prioritized = active.Where(profile => profile.ContextPriority == priority).ToArray();
            var major = prioritized.Max(profile => profile.VersionMajor);
            var minor = prioritized.Where(profile => profile.VersionMajor == major).Max(profile => profile.VersionMinor);
            var finalists = prioritized.Where(profile => profile.VersionMajor == major && profile.VersionMinor == minor).ToArray();
            if (finalists.Length != 1)
            {
                throw new InvalidOperationException(
                    $"ORDINARY_PROFILE_AMBIGUOUS: {ClearingHouseCode}/{FlowTypeCode}/{DirectionCode}/{ServiceClassCode ?? "*"} " +
                    $"at {date:yyyy-MM-dd}, priority {priority}, version {major}.{minor}: " +
                    string.Join(",", finalists.Select(profile => profile.ProfileCode)));
            }
            winners.TryAdd(finalists[0].Id, finalists[0]);
        }
        return winners.Values.ToArray();
    }

    private static IReadOnlyList<NachaOrdinaryProductionProfileScope> CreateSupported()
    {
        var scopes = new List<NachaOrdinaryProductionProfileScope>();
        foreach (var flow in new[] { "ORIGINAL", "PRENOTIFICACION" })
        {
            // ACH outbound supplies the batch service; ACH inbound currently supplies null.
            foreach (var service in new[] { "PPD", "CCD", "CTX" })
            {
                scopes.Add(new("ACH", flow, "SALIDA", service, 35, 0));
            }
            scopes.Add(new("ACH", flow, "ENTRADA", null, 35, 0));

            // CENIT outbound policy and generation remain unpinned; inbound pins 1.0.
            foreach (var service in new[] { "PPD", "CCD", "CTX" })
            {
                scopes.Add(new("CENIT", flow, "SALIDA", service, null, null));
            }
            scopes.Add(new("CENIT", flow, "ENTRADA", null, 1, 0));
            scopes.Add(new("CENIT", flow, "ENTRADA", "CTX", 1, 0));
        }
        return scopes;
    }

    private static bool Equal(string? left, string? right)
        => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static bool IsNormativelyEnabled(CfgProfile profile)
    {
        var tags = profile.Tags.GroupBy(tag => tag.TagKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last().TagValue, StringComparer.OrdinalIgnoreCase);
        return tags.TryGetValue("IsHomologated", out var homologated)
               && bool.TryParse(homologated, out var enabled)
               && enabled
               && (!tags.TryGetValue("IsPlaceholder", out var placeholder)
                   || !bool.TryParse(placeholder, out var isPlaceholder)
                   || !isPlaceholder);
    }
}
