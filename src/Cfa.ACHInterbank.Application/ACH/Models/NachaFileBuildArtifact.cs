namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record NachaFileBuildArtifact(
    string Content,
    IReadOnlyList<int> AchTransactionIds);
