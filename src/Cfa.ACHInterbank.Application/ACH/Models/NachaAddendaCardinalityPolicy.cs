using System.Globalization;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record NachaAddendaBounds(int Minimum, int Maximum);

public sealed record NachaAddendaServiceCardinality(
    string ServiceCode,
    NachaAddendaBounds Credit,
    NachaAddendaBounds Debit);

public sealed class NachaAddendaCardinalityPolicy
{
    private readonly IReadOnlyDictionary<string, NachaAddendaServiceCardinality> _services;

    public NachaAddendaCardinalityPolicy(
        string directionCode,
        string flowTypeCode,
        IEnumerable<NachaAddendaServiceCardinality> services)
    {
        DirectionCode = directionCode;
        FlowTypeCode = flowTypeCode;
        _services = services.ToDictionary(service => service.ServiceCode, StringComparer.OrdinalIgnoreCase);
    }

    public string DirectionCode { get; }
    public string FlowTypeCode { get; }
    public IReadOnlyCollection<NachaAddendaServiceCardinality> Services => _services.Values.ToArray();

    public bool TryResolve(string serviceCode, NachaEntryDirection direction, out NachaAddendaBounds bounds)
    {
        bounds = default!;
        if (!_services.TryGetValue(serviceCode, out var service))
        {
            return false;
        }

        bounds = direction switch
        {
            NachaEntryDirection.Credit => service.Credit,
            NachaEntryDirection.Debit => service.Debit,
            _ => default!
        };
        return bounds is not null;
    }
}

public enum NachaAddendaCardinalityMetadataStatus
{
    NotPresent,
    Resolved,
    Invalid
}

public sealed record NachaAddendaCardinalityMetadataResult(
    NachaAddendaCardinalityMetadataStatus Status,
    NachaAddendaCardinalityPolicy? Policy = null,
    string? Error = null);

public static class NachaAddendaCardinalityMetadata
{
    public const string RootPrefix = "CardinalityPolicy.";
    public const string Prefix = RootPrefix + "Service.";
    public const string DirectionKey = RootPrefix + "Direction";
    public const string FlowKey = RootPrefix + "Flow";

    public static IReadOnlyList<KeyValuePair<string, string>> ToTags(NachaAddendaCardinalityPolicy policy)
        => [
            new(DirectionKey, policy.DirectionCode),
            new(FlowKey, policy.FlowTypeCode),
            ..policy.Services.OrderBy(service => service.ServiceCode, StringComparer.Ordinal)
            .Select(service => new KeyValuePair<string, string>(
                Prefix + service.ServiceCode,
                $"Credit={Format(service.Credit)};Debit={Format(service.Debit)}"))
        ];

    public static NachaAddendaCardinalityMetadataResult Resolve(
        IEnumerable<KeyValuePair<string, string>> tags)
    {
        var selected = tags.Where(tag => tag.Key.StartsWith(RootPrefix, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (selected.Length == 0)
        {
            return new(NachaAddendaCardinalityMetadataStatus.NotPresent);
        }

        if (selected.GroupBy(tag => tag.Key, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() != 1))
        {
            return Invalid("CARDINALITY_POLICY_DUPLICATE_SERVICE");
        }

        var byKey = selected.ToDictionary(tag => tag.Key, tag => tag.Value, StringComparer.OrdinalIgnoreCase);
        if (!byKey.TryGetValue(DirectionKey, out var direction)
            || direction is not ("ENTRADA" or "SALIDA")
            || !byKey.TryGetValue(FlowKey, out var flow)
            || flow is not ("ORIGINAL" or "PRENOTIFICACION"))
        {
            return Invalid("CARDINALITY_POLICY_CONTEXT_INVALID");
        }

        var services = new List<NachaAddendaServiceCardinality>();
        foreach (var tag in selected)
        {
            if (tag.Key.Equals(DirectionKey, StringComparison.OrdinalIgnoreCase)
                || tag.Key.Equals(FlowKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (!tag.Key.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return Invalid("CARDINALITY_POLICY_UNKNOWN_TAG");
            }
            var serviceCode = tag.Key[Prefix.Length..].Trim().ToUpperInvariant();
            if (serviceCode is not ("PPD" or "CCD" or "CTX"))
            {
                return Invalid("CARDINALITY_POLICY_UNSUPPORTED_SERVICE");
            }
            if (services.Any(service => service.ServiceCode == serviceCode))
            {
                return Invalid("CARDINALITY_POLICY_DUPLICATE_SERVICE");
            }

            var parts = tag.Value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var cases = new Dictionary<string, NachaAddendaBounds>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in parts)
            {
                var separator = part.IndexOf('=');
                if (separator <= 0 || separator == part.Length - 1)
                {
                    return Invalid("CARDINALITY_POLICY_MALFORMED_CASE");
                }

                var name = part[..separator];
                if (name is not ("Credit" or "Debit") || cases.ContainsKey(name))
                {
                    return Invalid("CARDINALITY_POLICY_OVERLAPPING_CASE");
                }

                var boundsText = part[(separator + 1)..].Split(':');
                if (boundsText.Length != 2
                    || !int.TryParse(boundsText[0], NumberStyles.None, CultureInfo.InvariantCulture, out var minimum)
                    || !int.TryParse(boundsText[1], NumberStyles.None, CultureInfo.InvariantCulture, out var maximum)
                    || minimum < 0 || maximum < minimum)
                {
                    return Invalid("CARDINALITY_POLICY_INVALID_BOUNDS");
                }

                var physicalMaximum = serviceCode == "CTX" ? 9_999 : 1;
                if (maximum > physicalMaximum)
                {
                    return Invalid("CARDINALITY_POLICY_EXCEEDS_PHYSICAL_CAPACITY");
                }

                cases.Add(name, new NachaAddendaBounds(minimum, maximum));
            }

            if (cases.Count != 2 || !cases.TryGetValue("Credit", out var credit)
                || !cases.TryGetValue("Debit", out var debit))
            {
                return Invalid("CARDINALITY_POLICY_UNCOVERED_CASE");
            }

            services.Add(new NachaAddendaServiceCardinality(serviceCode, credit, debit));
        }

        if (services.Count == 0)
        {
            return Invalid("CARDINALITY_POLICY_SERVICE_MISSING");
        }
        return new(NachaAddendaCardinalityMetadataStatus.Resolved,
            new NachaAddendaCardinalityPolicy(direction, flow, services));
    }

    private static string Format(NachaAddendaBounds bounds)
        => $"{bounds.Minimum.ToString(CultureInfo.InvariantCulture)}:{bounds.Maximum.ToString(CultureInfo.InvariantCulture)}";

    private static NachaAddendaCardinalityMetadataResult Invalid(string error)
        => new(NachaAddendaCardinalityMetadataStatus.Invalid, Error: error);
}
