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
    IReadOnlyList<string> ServiceCodes,
    IReadOnlyList<CenitOutboundBatchPartition> Batches);

public static class CenitOutboundFilePartitioner
{
    public const int PpdEntryLimitPerFile = 10_000;
    public const int CcdBatchLimitPerFile = 10_000;
    public const int CtxAddendaLimitPerEntry = 9_999;
    public const string PpdCcdProfileIdentity = "CENIT_PPD_CCD_OUTBOUND_2026";
    public const string CtxProfileIdentity = "CENIT_CTX_OUTBOUND_2026";

    public static IReadOnlyList<CenitOutboundFilePartition> Partition(
        IReadOnlyList<CenitOutboundSourceBatch> sourceBatches)
    {
        ArgumentNullException.ThrowIfNull(sourceBatches);

        var normalized = sourceBatches
            .Select(source => source with
            {
                ServiceCode = NormalizeServiceCode(source.ServiceCode),
                Transactions = source.Transactions.OrderBy(transaction => transaction.Id).ToArray()
            })
            .Where(source => source.Transactions.Count > 0)
            .ToArray();

        EnsureUniqueTransactionMembership(normalized);

        var ppdFiles = PartitionPpd(normalized.Where(source => source.ServiceCode == "PPD"));
        var ccdFiles = PartitionCcd(normalized.Where(source => source.ServiceCode == "CCD"));
        var ordinaryFileCount = Math.Max(ppdFiles.Count, ccdFiles.Count);
        var result = new List<CenitOutboundFilePartition>(ordinaryFileCount + 1);

        for (var index = 0; index < ordinaryFileCount; index++)
        {
            var batches = new List<CenitOutboundBatchPartition>();
            if (index < ppdFiles.Count)
            {
                batches.AddRange(ppdFiles[index]);
            }
            if (index < ccdFiles.Count)
            {
                batches.AddRange(ccdFiles[index]);
            }

            result.Add(new CenitOutboundFilePartition(
                result.Count + 1,
                PpdCcdProfileIdentity,
                batches.Select(batch => batch.ServiceCode).Distinct(StringComparer.Ordinal).ToArray(),
                batches));
        }

        var ctxBatches = PartitionCtx(normalized.Where(source => source.ServiceCode == "CTX"));
        if (ctxBatches.Count > 0)
        {
            result.Add(new CenitOutboundFilePartition(
                result.Count + 1,
                CtxProfileIdentity,
                ["CTX"],
                ctxBatches));
        }

        return result;
    }

    private static List<IReadOnlyList<CenitOutboundBatchPartition>> PartitionPpd(
        IEnumerable<CenitOutboundSourceBatch> sources)
    {
        var files = new List<IReadOnlyList<CenitOutboundBatchPartition>>();
        var current = new List<CenitOutboundBatchPartition>();
        var currentEntryCount = 0;

        foreach (var source in sources)
        {
            var offset = 0;
            while (offset < source.Transactions.Count)
            {
                var remaining = source.Transactions.Count - offset;
                var remainingCapacity = PpdEntryLimitPerFile - currentEntryCount;
                var take = Math.Min(remaining, remainingCapacity);
                current.Add(new CenitOutboundBatchPartition(
                    source.Batch,
                    source.ServiceCode,
                    source.Transactions.Skip(offset).Take(take).ToArray()));
                currentEntryCount += take;
                offset += take;

                if (currentEntryCount == PpdEntryLimitPerFile)
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

    private static List<IReadOnlyList<CenitOutboundBatchPartition>> PartitionCcd(
        IEnumerable<CenitOutboundSourceBatch> sources)
    {
        var files = new List<IReadOnlyList<CenitOutboundBatchPartition>>();
        var current = new List<CenitOutboundBatchPartition>(CcdBatchLimitPerFile);

        foreach (var source in sources)
        {
            foreach (var transaction in source.Transactions)
            {
                if (transaction.Addendas.Count == 0)
                {
                    throw new NachaGenerationException(
                        "CENIT_CCD_ADDENDA_REQUIRED",
                        "Cada entrada CCD CENIT debe tener al menos una adenda.");
                }

                current.Add(new CenitOutboundBatchPartition(source.Batch, source.ServiceCode, [transaction]));
                if (current.Count == CcdBatchLimitPerFile)
                {
                    files.Add(current.ToArray());
                    current = new List<CenitOutboundBatchPartition>(CcdBatchLimitPerFile);
                }
            }
        }

        if (current.Count > 0)
        {
            files.Add(current.ToArray());
        }

        return files;
    }

    private static IReadOnlyList<CenitOutboundBatchPartition> PartitionCtx(
        IEnumerable<CenitOutboundSourceBatch> sources)
    {
        var batches = new List<CenitOutboundBatchPartition>();
        foreach (var source in sources)
        {
            foreach (var transaction in source.Transactions)
            {
                if (transaction.Addendas.Count is < 1 or > CtxAddendaLimitPerEntry)
                {
                    throw new NachaGenerationException(
                        "CENIT_CTX_ADDENDA_CARDINALITY_INVALID",
                        "Cada entrada CTX CENIT debe contener entre 1 y 9.999 adendas.");
                }
            }

            batches.Add(new CenitOutboundBatchPartition(source.Batch, source.ServiceCode, source.Transactions));
        }

        return batches;
    }

    private static string NormalizeServiceCode(string serviceCode)
    {
        var normalized = serviceCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized is not ("PPD" or "CCD" or "CTX"))
        {
            throw new NachaGenerationException(
                "CENIT_SERVICE_NOT_SUPPORTED",
                $"El servicio CENIT '{normalized}' no puede particionarse de forma segura.");
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
}
