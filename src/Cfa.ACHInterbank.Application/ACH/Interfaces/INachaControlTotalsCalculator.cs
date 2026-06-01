using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaControlTotalsCalculator
{
    NachaControlTotalsResult Calculate(NachaControlTotalsRequest request);

    string ResolveFileIdModifier(int dailySequence);
}

public sealed class NachaControlTotalsRequest
{
    public IReadOnlyList<AchBatch> Batches { get; set; } = Array.Empty<AchBatch>();
    public IReadOnlyDictionary<int, IReadOnlyList<AchTransaction>> TransactionsByBatchId { get; set; } = new Dictionary<int, IReadOnlyList<AchTransaction>>();
    public IReadOnlyDictionary<int, int> AddendaRecordCountByBatchId { get; set; } = new Dictionary<int, int>();
    public string? EntryHashSourceFieldPath { get; set; }
    public int BatchEntryHashLength { get; set; }
    public int FileEntryHashLength { get; set; }
    public int BatchEntryAddendaCountLength { get; set; }
    public int FileEntryAddendaCountLength { get; set; }
    public int BatchTotalDebitAmountLength { get; set; }
    public int FileTotalDebitAmountLength { get; set; }
    public int BatchTotalCreditAmountLength { get; set; }
    public int FileTotalCreditAmountLength { get; set; }
    public int BatchCountLength { get; set; }
    public int BlockCountLength { get; set; }
    public int PhysicalRecordCountBeforePadding { get; set; }
    public int BlockSize { get; set; } = 10;
}

public sealed class NachaControlTotalsResult
{
    public IReadOnlyList<NachaBatchControlTotals> BatchTotals { get; init; } = Array.Empty<NachaBatchControlTotals>();
    public NachaFileControlTotals FileTotals { get; init; } = new();
}

public sealed class NachaBatchControlTotals
{
    public int BatchId { get; init; }
    public int EntryDetailCount { get; init; }
    public int AddendaCount { get; init; }
    public int EntryAddendaCount { get; init; }
    public long EntryHash { get; init; }
    public long TotalDebitAmountInCents { get; init; }
    public long TotalCreditAmountInCents { get; init; }
}

public sealed class NachaFileControlTotals
{
    public int BatchCount { get; init; }
    public int BlockCount { get; init; }
    public int EntryAddendaCount { get; init; }
    public long EntryHash { get; init; }
    public long TotalDebitAmountInCents { get; init; }
    public long TotalCreditAmountInCents { get; init; }
    public int PhysicalRecordCountBeforePadding { get; init; }
    public int PaddingRecordCount { get; init; }
    public int PhysicalRecordCountAfterPadding { get; init; }
}
