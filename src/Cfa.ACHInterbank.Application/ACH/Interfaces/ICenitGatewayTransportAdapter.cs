namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public sealed record CenitGatewayInboundArtifact(
    string MetadataPath,
    string ContentPath,
    string SourceResponseId,
    string SourceFileName,
    string MessageType,
    string Content,
    DateTime ReceivedAtUtc,
    string? RelatedOutboundFileName,
    string? RelatedReference,
    string? TransactionTraceNumber,
    string? AchCycleId);

public interface ICenitGatewayTransportAdapter
{
    bool Enabled { get; }
    Task HandoffOutboundAsync(string fileName, ReadOnlyMemory<byte> content, CancellationToken ct = default);
    Task<IReadOnlyList<CenitGatewayInboundArtifact>> PickupInboundAsync(CancellationToken ct = default);
    Task ArchiveInboundAsync(CenitGatewayInboundArtifact artifact, CancellationToken ct = default);
}
