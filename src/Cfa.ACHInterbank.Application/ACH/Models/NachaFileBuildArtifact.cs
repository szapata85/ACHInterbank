namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record NachaFileBuildArtifact(
    string Content,
    IReadOnlyList<int> AchTransactionIds)
{
    public string ProfileIdentity { get; init; } = string.Empty;
    public IReadOnlyList<string> ServiceCodes { get; init; } = [];
    public IReadOnlyList<NachaFileBatchMembership> Batches { get; init; } = [];
}

public sealed record NachaFileBatchMembership(
    int BatchOrdinal,
    int SourceAchBatchId,
    string ServiceCode,
    IReadOnlyList<int> AchTransactionIds);

public sealed record NachaFileBuildResult(IReadOnlyList<NachaFileBuildArtifact> Files)
{
    public static NachaFileBuildResult Empty { get; } = new([]);
}
