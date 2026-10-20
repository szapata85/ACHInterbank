using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public class NachaBuildContext
{
    public required AchCycle Cycle { get; init; }
    public required IReadOnlyList<AchBatch> Batches { get; init; }
    public required IReadOnlyList<AchTransaction> Transactions { get; init; }
    public string? StandardEntryClassCode { get; init; }
    public int? SelectedProfileId { get; init; }
    public string? SelectedProfileCode { get; init; }
    public int? SelectedVersionMajor { get; init; }
    public int? SelectedVersionMinor { get; init; }
}
