using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public sealed class CenitLocalGatewayFolderTransportAdapter(
    IOptions<CenitLocalGatewayOptions> options) : ICenitGatewayTransportAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly CenitLocalGatewayOptions _options = options.Value;
    public bool Enabled => _options.Enabled;

    public async Task HandoffOutboundAsync(string fileName, ReadOnlyMemory<byte> content, CancellationToken ct = default)
    {
        if (!Enabled) return;
        var target = ResolveChild(_options.InputPath, fileName);
        await WriteAtomicallyAsync(target, content, ct);
    }

    public async Task<IReadOnlyList<CenitGatewayInboundArtifact>> PickupInboundAsync(CancellationToken ct = default)
    {
        if (!Enabled) return [];
        Directory.CreateDirectory(_options.OutputPath);
        var artifacts = new List<CenitGatewayInboundArtifact>();
        foreach (var metadataPath in Directory.EnumerateFiles(_options.OutputPath, "*.meta.json").Order(StringComparer.Ordinal))
        {
            ct.ThrowIfCancellationRequested();
            await using var stream = new FileStream(metadataPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var metadata = await JsonSerializer.DeserializeAsync<InboundMetadata>(stream, JsonOptions, ct);
            if (metadata is null || string.IsNullOrWhiteSpace(metadata.ArtifactFileName)) continue;
            var contentPath = ResolveChild(_options.OutputPath, metadata.ArtifactFileName);
            if (!File.Exists(contentPath)) continue;
            var content = await File.ReadAllTextAsync(contentPath, Encoding.UTF8, ct);
            artifacts.Add(new CenitGatewayInboundArtifact(
                metadataPath,
                contentPath,
                metadata.SourceResponseId,
                metadata.ArtifactFileName,
                metadata.MessageType,
                content,
                metadata.ReceivedAtUtc,
                metadata.RelatedOutboundFileName,
                metadata.RelatedReference,
                metadata.TransactionTraceNumber,
                metadata.AchCycleId));
        }
        return artifacts;
    }

    public Task ArchiveInboundAsync(CenitGatewayInboundArtifact artifact, CancellationToken ct = default)
    {
        if (!Enabled) return Task.CompletedTask;
        ct.ThrowIfCancellationRequested();
        Directory.CreateDirectory(_options.ArchivePath);
        MoveToArchive(artifact.ContentPath);
        MoveToArchive(artifact.MetadataPath);
        return Task.CompletedTask;
    }

    private void MoveToArchive(string source)
    {
        var target = ResolveChild(_options.ArchivePath, Path.GetFileName(source));
        if (File.Exists(target))
        {
            target = ResolveChild(_options.ArchivePath, $"{Path.GetFileNameWithoutExtension(source)}.{Guid.NewGuid():N}{Path.GetExtension(source)}");
        }
        File.Move(source, target);
    }

    private static async Task WriteAtomicallyAsync(string target, ReadOnlyMemory<byte> content, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        var temporary = $"{target}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.WriteThrough))
            {
                await stream.WriteAsync(content, ct);
                await stream.FlushAsync(ct);
            }
            File.Move(temporary, target, false);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static string ResolveChild(string root, string fileName)
    {
        if (string.IsNullOrWhiteSpace(root)) throw new InvalidOperationException("CENIT_LOCAL_GATEWAY_PATH_REQUIRED");
        var leaf = Path.GetFileName(fileName);
        if (!string.Equals(leaf, fileName, StringComparison.Ordinal)) throw new InvalidOperationException("CENIT_LOCAL_GATEWAY_FILENAME_INVALID");
        return Path.Combine(Path.GetFullPath(root), leaf);
    }

    private sealed record InboundMetadata(
        string SourceResponseId,
        string ArtifactFileName,
        string MessageType,
        DateTime ReceivedAtUtc,
        string? RelatedOutboundFileName,
        string? RelatedReference,
        string? TransactionTraceNumber,
        string? AchCycleId);
}
