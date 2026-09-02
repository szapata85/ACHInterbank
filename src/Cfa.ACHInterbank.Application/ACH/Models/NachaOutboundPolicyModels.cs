using System.Globalization;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public enum NachaOutboundFileAllocation
{
    CombineServicePartitionsByIndex = 1,
    IndependentServiceFiles = 2
}

public enum NachaOutboundServicePartitionStrategy
{
    EntriesPerFile = 1,
    FixedEntriesPerBatch = 2,
    PreserveSourceBatches = 3
}

public sealed record NachaOutboundPartitionPolicy(
    string ProfileCode,
    int FileOrder,
    NachaOutboundFileAllocation FileAllocation,
    IReadOnlyList<NachaOutboundServicePartitionPolicy> Services);

public sealed record NachaOutboundServicePartitionPolicy(
    string ServiceCode,
    int Order,
    NachaOutboundServicePartitionStrategy Strategy,
    int? MaxEntriesPerFile = null,
    int? MaxBatchesPerFile = null,
    int? EntriesPerBatch = null,
    int? MinAddendaPerEntry = null,
    int? MaxAddendaPerEntry = null,
    string? AddendaCardinalityErrorCode = null);

public enum NachaOutboundPolicyMetadataStatus
{
    NotPresent = 0,
    Resolved = 1,
    Invalid = 2
}

public sealed record NachaOutboundPolicyMetadataResult(
    NachaOutboundPolicyMetadataStatus Status,
    NachaOutboundPartitionPolicy? Policy = null,
    string? Error = null);

public static class NachaOutboundPolicyMetadata
{
    public const string Prefix = "OutboundPolicy.";
    public const string FileOrderKey = Prefix + "FileOrder";
    public const string FileAllocationKey = Prefix + "FileAllocation";
    public const string ServiceKeyPrefix = Prefix + "Service.";

    public static IReadOnlyList<KeyValuePair<string, string>> ToTags(NachaOutboundPartitionPolicy policy)
    {
        var error = Validate(policy);
        if (error is not null)
        {
            throw new ArgumentException(error, nameof(policy));
        }

        var tags = new List<KeyValuePair<string, string>>
        {
            new(FileOrderKey, policy.FileOrder.ToString(CultureInfo.InvariantCulture)),
            new(FileAllocationKey, policy.FileAllocation.ToString())
        };
        tags.AddRange(policy.Services
            .OrderBy(service => service.Order)
            .Select(service => new KeyValuePair<string, string>(
                ServiceKeyPrefix + service.ServiceCode,
                SerializeService(service))));
        return tags;
    }

    public static NachaOutboundPolicyMetadataResult Resolve(
        string profileCode,
        IEnumerable<KeyValuePair<string, string>> tags)
    {
        var policyTags = tags
            .Where(tag => tag.Key.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (policyTags.Length == 0)
        {
            return new(NachaOutboundPolicyMetadataStatus.NotPresent);
        }

        var duplicate = policyTags
            .GroupBy(tag => tag.Key, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() != 1);
        if (duplicate is not null)
        {
            return Invalid($"La metadata outbound contiene la clave ambigua '{duplicate.Key}'.");
        }

        var unknownPolicyTag = policyTags.FirstOrDefault(tag =>
            !string.Equals(tag.Key, FileOrderKey, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(tag.Key, FileAllocationKey, StringComparison.OrdinalIgnoreCase)
            && !tag.Key.StartsWith(ServiceKeyPrefix, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(unknownPolicyTag.Key))
        {
            return Invalid($"La metadata outbound contiene la clave desconocida '{unknownPolicyTag.Key}'.");
        }

        var byKey = policyTags.ToDictionary(tag => tag.Key, tag => tag.Value, StringComparer.OrdinalIgnoreCase);
        if (!TryPositiveInt(byKey, FileOrderKey, out var fileOrder, out var error))
        {
            return Invalid(error);
        }
        if (!byKey.TryGetValue(FileAllocationKey, out var allocationValue)
            || !Enum.TryParse<NachaOutboundFileAllocation>(allocationValue, true, out var allocation)
            || !Enum.IsDefined(allocation))
        {
            return Invalid($"La metadata outbound requiere '{FileAllocationKey}' válido.");
        }

        var services = new List<NachaOutboundServicePartitionPolicy>();
        foreach (var tag in policyTags.Where(tag => tag.Key.StartsWith(ServiceKeyPrefix, StringComparison.OrdinalIgnoreCase)))
        {
            var serviceCode = tag.Key[ServiceKeyPrefix.Length..].Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(serviceCode))
            {
                return Invalid("La metadata outbound contiene un código de servicio vacío.");
            }

            var serviceResult = ParseService(serviceCode, tag.Value);
            if (serviceResult.Error is not null)
            {
                return Invalid(serviceResult.Error);
            }
            services.Add(serviceResult.Policy!);
        }

        var policy = new NachaOutboundPartitionPolicy(profileCode, fileOrder, allocation, services);
        error = Validate(policy);
        return error is null
            ? new(NachaOutboundPolicyMetadataStatus.Resolved, policy)
            : Invalid(error);
    }

    public static string? Validate(NachaOutboundPartitionPolicy policy)
    {
        if (string.IsNullOrWhiteSpace(policy.ProfileCode))
        {
            return "La política outbound requiere identidad de perfil.";
        }
        if (policy.FileOrder <= 0)
        {
            return "La política outbound requiere un orden de archivo positivo.";
        }
        if (!Enum.IsDefined(policy.FileAllocation))
        {
            return "La política outbound contiene una asignación de archivos inválida.";
        }
        if (policy.Services.Count == 0)
        {
            return "La política outbound requiere al menos un servicio.";
        }
        if (policy.Services.GroupBy(service => service.ServiceCode, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
        {
            return "La política outbound contiene servicios duplicados.";
        }
        if (policy.Services.GroupBy(service => service.Order).Any(group => group.Count() > 1))
        {
            return "La política outbound contiene órdenes de servicio duplicados.";
        }

        foreach (var service in policy.Services)
        {
            if (string.IsNullOrWhiteSpace(service.ServiceCode) || service.Order <= 0 || !Enum.IsDefined(service.Strategy))
            {
                return "La política outbound contiene un servicio incompleto o inválido.";
            }
            if (service.MinAddendaPerEntry is < 0
                || service.MaxAddendaPerEntry is < 0
                || service.MaxAddendaPerEntry < service.MinAddendaPerEntry)
            {
                return $"La cardinalidad de adendas del servicio '{service.ServiceCode}' es inválida.";
            }
            if ((service.MinAddendaPerEntry.HasValue || service.MaxAddendaPerEntry.HasValue)
                && string.IsNullOrWhiteSpace(service.AddendaCardinalityErrorCode))
            {
                return $"El servicio '{service.ServiceCode}' requiere un código de error para su cardinalidad de adendas.";
            }

            var strategyError = service.Strategy switch
            {
                NachaOutboundServicePartitionStrategy.EntriesPerFile
                    when service.MaxEntriesPerFile is not > 0
                         || service.MaxBatchesPerFile.HasValue
                         || service.EntriesPerBatch.HasValue
                    => "requiere MaxEntriesPerFile y no admite límites de lote",
                NachaOutboundServicePartitionStrategy.FixedEntriesPerBatch
                    when service.MaxBatchesPerFile is not > 0
                         || service.EntriesPerBatch is not > 0
                         || service.MaxEntriesPerFile.HasValue
                    => "requiere MaxBatchesPerFile y EntriesPerBatch positivos",
                NachaOutboundServicePartitionStrategy.PreserveSourceBatches
                    when service.MaxEntriesPerFile.HasValue
                         || service.MaxBatchesPerFile.HasValue
                         || service.EntriesPerBatch.HasValue
                    => "no admite límites de partición",
                _ => null
            };
            if (strategyError is not null)
            {
                return $"El servicio '{service.ServiceCode}' {strategyError}.";
            }
        }

        return null;
    }

    private static string SerializeService(NachaOutboundServicePartitionPolicy service)
    {
        var values = new List<string>
        {
            $"Order={service.Order.ToString(CultureInfo.InvariantCulture)}",
            $"Strategy={service.Strategy}"
        };
        Add(values, "MaxEntriesPerFile", service.MaxEntriesPerFile);
        Add(values, "MaxBatchesPerFile", service.MaxBatchesPerFile);
        Add(values, "EntriesPerBatch", service.EntriesPerBatch);
        Add(values, "MinAddendaPerEntry", service.MinAddendaPerEntry);
        Add(values, "MaxAddendaPerEntry", service.MaxAddendaPerEntry);
        if (!string.IsNullOrWhiteSpace(service.AddendaCardinalityErrorCode))
        {
            values.Add($"AddendaErrorCode={service.AddendaCardinalityErrorCode}");
        }
        return string.Join(';', values);
    }

    private static (NachaOutboundServicePartitionPolicy? Policy, string? Error) ParseService(
        string serviceCode,
        string value)
    {
        var segments = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var pairs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var segment in segments)
        {
            var separator = segment.IndexOf('=');
            if (separator <= 0 || separator == segment.Length - 1 || !pairs.TryAdd(segment[..separator], segment[(separator + 1)..]))
            {
                return (null, $"La metadata del servicio '{serviceCode}' es ambigua o inválida.");
            }
        }

        var knownKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Order", "Strategy", "MaxEntriesPerFile", "MaxBatchesPerFile", "EntriesPerBatch",
            "MinAddendaPerEntry", "MaxAddendaPerEntry", "AddendaErrorCode"
        };
        var unknown = pairs.Keys.FirstOrDefault(key => !knownKeys.Contains(key));
        if (unknown is not null)
        {
            return (null, $"La metadata del servicio '{serviceCode}' contiene la propiedad desconocida '{unknown}'.");
        }
        if (!TryPositiveInt(pairs, "Order", out var order, out var error))
        {
            return (null, $"Servicio '{serviceCode}': {error}");
        }
        if (!pairs.TryGetValue("Strategy", out var strategyValue)
            || !Enum.TryParse<NachaOutboundServicePartitionStrategy>(strategyValue, true, out var strategy)
            || !Enum.IsDefined(strategy))
        {
            return (null, $"El servicio '{serviceCode}' requiere Strategy válido.");
        }
        if (!TryOptionalNonNegativeInt(pairs, "MaxEntriesPerFile", out var maxEntries, out error)
            || !TryOptionalNonNegativeInt(pairs, "MaxBatchesPerFile", out var maxBatches, out error)
            || !TryOptionalNonNegativeInt(pairs, "EntriesPerBatch", out var entriesPerBatch, out error)
            || !TryOptionalNonNegativeInt(pairs, "MinAddendaPerEntry", out var minAddenda, out error)
            || !TryOptionalNonNegativeInt(pairs, "MaxAddendaPerEntry", out var maxAddenda, out error))
        {
            return (null, $"Servicio '{serviceCode}': {error}");
        }

        return (new NachaOutboundServicePartitionPolicy(
            serviceCode,
            order,
            strategy,
            maxEntries,
            maxBatches,
            entriesPerBatch,
            minAddenda,
            maxAddenda,
            pairs.GetValueOrDefault("AddendaErrorCode")), null);
    }

    private static bool TryPositiveInt(
        IReadOnlyDictionary<string, string> values,
        string key,
        out int result,
        out string error)
    {
        if (values.TryGetValue(key, out var value)
            && int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out result)
            && result > 0)
        {
            error = string.Empty;
            return true;
        }
        result = 0;
        error = $"La metadata outbound requiere '{key}' como entero positivo.";
        return false;
    }

    private static bool TryOptionalNonNegativeInt(
        IReadOnlyDictionary<string, string> values,
        string key,
        out int? result,
        out string error)
    {
        if (!values.TryGetValue(key, out var value))
        {
            result = null;
            error = string.Empty;
            return true;
        }
        if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0)
        {
            result = parsed;
            error = string.Empty;
            return true;
        }
        result = null;
        error = $"'{key}' debe ser un entero no negativo.";
        return false;
    }

    private static void Add(List<string> values, string key, int? value)
    {
        if (value.HasValue)
        {
            values.Add($"{key}={value.Value.ToString(CultureInfo.InvariantCulture)}");
        }
    }

    private static NachaOutboundPolicyMetadataResult Invalid(string error)
        => new(NachaOutboundPolicyMetadataStatus.Invalid, Error: error);
}
