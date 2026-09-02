using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record CenitOutboundSourceBatch(
    AchBatch Batch,
    string ServiceCode,
    IReadOnlyList<AchTransaction> Transactions);

public sealed record CenitOutboundBatchPartition(
    AchBatch SourceBatch,
    string ServiceCode,
    IReadOnlyList<AchTransaction> Transactions);

public sealed record CenitOutboundFilePartition(
    int FileIndex,
    string ProfileIdentity,
    string StandardEntryClassCode,
    IReadOnlyList<string> ServiceCodes,
    IReadOnlyList<CenitOutboundBatchPartition> Batches);

public static class CenitOutboundFilePartitioner
{
    public static IReadOnlyList<CenitOutboundFilePartition> Partition(
        IReadOnlyList<CenitOutboundSourceBatch> sourceBatches,
        IReadOnlyList<NachaOutboundPartitionPolicy> policies)
    {
        ArgumentNullException.ThrowIfNull(sourceBatches);
        ArgumentNullException.ThrowIfNull(policies);

        var normalized = sourceBatches
            .Select(source => source with
            {
                ServiceCode = NormalizeServiceCode(source.ServiceCode),
                Transactions = source.Transactions.OrderBy(transaction => transaction.Id).ToArray()
            })
            .Where(source => source.Transactions.Count > 0)
            .ToArray();

        EnsureUniqueTransactionMembership(normalized);
        EnsureValidPolicies(policies, normalized);

        var result = new List<CenitOutboundFilePartition>();
        foreach (var policy in policies.OrderBy(item => item.FileOrder))
        {
            var serviceFiles = policy.Services
                .OrderBy(service => service.Order)
                .Select(service => new
                {
                    Files = PartitionService(
                        normalized.Where(source => string.Equals(
                            source.ServiceCode,
                            service.ServiceCode,
                            StringComparison.OrdinalIgnoreCase)),
                        service)
                })
                .Where(item => item.Files.Count > 0)
                .ToArray();

            if (policy.FileAllocation == NachaOutboundFileAllocation.CombineServicePartitionsByIndex)
            {
                var fileCount = serviceFiles.Length == 0 ? 0 : serviceFiles.Max(item => item.Files.Count);
                for (var index = 0; index < fileCount; index++)
                {
                    var batches = serviceFiles
                        .Where(item => index < item.Files.Count)
                        .SelectMany(item => item.Files[index])
                        .ToArray();
                    AddFile(result, policy, batches);
                }
            }
            else
            {
                foreach (var service in serviceFiles)
                {
                    foreach (var batches in service.Files)
                    {
                        AddFile(result, policy, batches);
                    }
                }
            }
        }

        return result;
    }

    private static IReadOnlyList<IReadOnlyList<CenitOutboundBatchPartition>> PartitionService(
        IEnumerable<CenitOutboundSourceBatch> sourceBatches,
        NachaOutboundServicePartitionPolicy policy)
    {
        var sources = sourceBatches.ToArray();
        ValidateAddendaCardinality(sources, policy);

        return policy.Strategy switch
        {
            NachaOutboundServicePartitionStrategy.EntriesPerFile
                => PartitionByEntriesPerFile(sources, policy.MaxEntriesPerFile!.Value),
            NachaOutboundServicePartitionStrategy.FixedEntriesPerBatch
                => PartitionByFixedEntriesPerBatch(
                    sources,
                    policy.EntriesPerBatch!.Value,
                    policy.MaxBatchesPerFile!.Value),
            NachaOutboundServicePartitionStrategy.PreserveSourceBatches
                => sources.Length == 0
                    ? []
                    : [sources.Select(ToPartition).ToArray()],
            _ => throw InvalidPolicy($"Estrategia desconocida para el servicio '{policy.ServiceCode}'.")
        };
    }

    private static IReadOnlyList<IReadOnlyList<CenitOutboundBatchPartition>> PartitionByEntriesPerFile(
        IEnumerable<CenitOutboundSourceBatch> sources,
        int maxEntriesPerFile)
    {
        var files = new List<IReadOnlyList<CenitOutboundBatchPartition>>();
        var current = new List<CenitOutboundBatchPartition>();
        var currentEntryCount = 0;

        foreach (var source in sources)
        {
            var offset = 0;
            while (offset < source.Transactions.Count)
            {
                var take = Math.Min(
                    source.Transactions.Count - offset,
                    maxEntriesPerFile - currentEntryCount);
                current.Add(new CenitOutboundBatchPartition(
                    source.Batch,
                    source.ServiceCode,
                    source.Transactions.Skip(offset).Take(take).ToArray()));
                currentEntryCount += take;
                offset += take;

                if (currentEntryCount == maxEntriesPerFile)
                {
                    files.Add(current.ToArray());
                    current = [];
                    currentEntryCount = 0;
                }
            }
        }

        if (current.Count > 0)
        {
            files.Add(current.ToArray());
        }
        return files;
    }

    private static IReadOnlyList<IReadOnlyList<CenitOutboundBatchPartition>> PartitionByFixedEntriesPerBatch(
        IEnumerable<CenitOutboundSourceBatch> sources,
        int entriesPerBatch,
        int maxBatchesPerFile)
    {
        var files = new List<IReadOnlyList<CenitOutboundBatchPartition>>();
        var current = new List<CenitOutboundBatchPartition>(maxBatchesPerFile);

        foreach (var source in sources)
        {
            for (var offset = 0; offset < source.Transactions.Count; offset += entriesPerBatch)
            {
                current.Add(new CenitOutboundBatchPartition(
                    source.Batch,
                    source.ServiceCode,
                    source.Transactions.Skip(offset).Take(entriesPerBatch).ToArray()));
                if (current.Count == maxBatchesPerFile)
                {
                    files.Add(current.ToArray());
                    current = new List<CenitOutboundBatchPartition>(maxBatchesPerFile);
                }
            }
        }

        if (current.Count > 0)
        {
            files.Add(current.ToArray());
        }
        return files;
    }

    private static void ValidateAddendaCardinality(
        IEnumerable<CenitOutboundSourceBatch> sources,
        NachaOutboundServicePartitionPolicy policy)
    {
        if (!policy.MinAddendaPerEntry.HasValue && !policy.MaxAddendaPerEntry.HasValue)
        {
            return;
        }

        foreach (var transaction in sources.SelectMany(source => source.Transactions))
        {
            if (transaction.Addendas.Count < (policy.MinAddendaPerEntry ?? 0)
                || transaction.Addendas.Count > (policy.MaxAddendaPerEntry ?? int.MaxValue))
            {
                throw new NachaGenerationException(
                    policy.AddendaCardinalityErrorCode!,
                    $"La entrada del servicio {policy.ServiceCode} no cumple la cardinalidad de adendas publicada.");
            }
        }
    }

    private static void EnsureValidPolicies(
        IReadOnlyList<NachaOutboundPartitionPolicy> policies,
        IReadOnlyList<CenitOutboundSourceBatch> sources)
    {
        if (policies.Count == 0)
        {
            throw new NachaGenerationException(
                "NACHA_OUTBOUND_POLICY_MISSING",
                "No se resolvió política outbound para particionar el archivo oficial.");
        }
        if (policies.GroupBy(policy => policy.ProfileCode, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1)
            || policies.GroupBy(policy => policy.FileOrder).Any(group => group.Count() > 1))
        {
            throw InvalidPolicy("Las políticas outbound resueltas son ambiguas.");
        }

        foreach (var policy in policies)
        {
            var error = NachaOutboundPolicyMetadata.Validate(policy);
            if (error is not null)
            {
                throw InvalidPolicy(error);
            }
        }

        foreach (var serviceCode in sources.Select(source => source.ServiceCode).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var matches = policies.Sum(policy => policy.Services.Count(service =>
                string.Equals(service.ServiceCode, serviceCode, StringComparison.OrdinalIgnoreCase)));
            if (matches == 0)
            {
                throw new NachaGenerationException(
                    "NACHA_OUTBOUND_POLICY_MISSING",
                    $"No existe política outbound para el servicio '{serviceCode}'.");
            }
            if (matches > 1)
            {
                throw InvalidPolicy($"El servicio '{serviceCode}' pertenece a más de una política outbound.");
            }
        }
    }

    private static void AddFile(
        ICollection<CenitOutboundFilePartition> result,
        NachaOutboundPartitionPolicy policy,
        IReadOnlyList<CenitOutboundBatchPartition> batches)
    {
        var serviceCodes = batches.Select(batch => batch.ServiceCode).Distinct(StringComparer.Ordinal).ToArray();
        var standardEntryClassCode = policy.FileAllocation == NachaOutboundFileAllocation.IndependentServiceFiles
            ? serviceCodes.Single()
            : policy.Services.OrderBy(service => service.Order).First().ServiceCode;
        result.Add(new CenitOutboundFilePartition(
            result.Count + 1,
            policy.ProfileCode,
            standardEntryClassCode,
            serviceCodes,
            batches));
    }

    private static CenitOutboundBatchPartition ToPartition(CenitOutboundSourceBatch source)
        => new(source.Batch, source.ServiceCode, source.Transactions);

    private static string NormalizeServiceCode(string serviceCode)
    {
        var normalized = serviceCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new NachaGenerationException(
                "NACHA_OUTBOUND_SERVICE_UNDETERMINED",
                "No se pudo determinar el servicio para particionar el archivo oficial.");
        }
        return normalized;
    }

    private static void EnsureUniqueTransactionMembership(IEnumerable<CenitOutboundSourceBatch> sources)
    {
        var transactionIds = new HashSet<int>();
        foreach (var transaction in sources.SelectMany(source => source.Transactions))
        {
            if (!transactionIds.Add(transaction.Id))
            {
                throw new NachaGenerationException(
                    "CENIT_TRANSACTION_MEMBERSHIP_DUPLICATED",
                    $"La transacción {transaction.Id} aparece en más de un lote fuente.");
            }
        }
    }

    private static NachaGenerationException InvalidPolicy(string message)
        => new("NACHA_OUTBOUND_POLICY_INVALID", message);
}
