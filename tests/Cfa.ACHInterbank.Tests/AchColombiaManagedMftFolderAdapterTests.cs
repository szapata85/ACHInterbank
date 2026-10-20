using System.Security.Cryptography;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.External.Connections;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColombiaManagedMftFolderAdapterTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"achcol-mft-{Guid.NewGuid():N}");

    [Fact]
    public async Task Disabled_ShouldFailClosed()
    {
        var adapter = Create(false);
        var result = await adapter.HandoffOutboundAsync("file.env", [1], Hash([1]));
        Assert.False(result.Succeeded);
        Assert.Equal("ACHCOL_MFT_DISABLED", result.Code);
        Assert.Empty(await adapter.PickupInboundAsync());
    }

    [Fact]
    public async Task Outbound_ShouldCommitAtomicallyAndDeduplicateByHash()
    {
        var adapter = Create();
        byte[] content = [1, 2, 3];
        var first = await adapter.HandoffOutboundAsync("0001001.001.20260831.1.OUT.env", content, Hash(content));
        var second = await adapter.HandoffOutboundAsync("0001001.001.20260831.1.OUT.env", content, Hash(content));
        Assert.True(first.Succeeded);
        Assert.Equal("HANDOFF_ALREADY_COMMITTED", second.Code);
        Assert.Empty(Directory.EnumerateFiles(Path.Combine(_root, "outbound"), "*.tmp"));
    }

    [Fact]
    public async Task Outbound_ShouldRejectSameNameWithDifferentContent()
    {
        var adapter = Create();
        await adapter.HandoffOutboundAsync("file.env", [1], Hash([1]));
        var result = await adapter.HandoffOutboundAsync("file.env", [2], Hash([2]));
        Assert.False(result.Succeeded);
        Assert.Equal("ACHCOL_MFT_NAME_COLLISION", result.Code);
    }

    [Fact]
    public async Task Inbound_ShouldClaimRecoverAndArchive()
    {
        var adapter = Create();
        Directory.CreateDirectory(Path.Combine(_root, "inbound"));
        await File.WriteAllBytesAsync(Path.Combine(_root, "inbound", "received.OUT.env"), [4, 5, 6]);
        var artifact = Assert.Single(await adapter.PickupInboundAsync());
        Assert.NotNull(artifact.BoundaryLastWriteTimeUtc);
        Assert.False(File.Exists(Path.Combine(_root, "inbound", artifact.FileName)));
        Assert.Single(await adapter.PickupInboundAsync());
        var reference = await adapter.ArchiveInboundAsync(artifact);
        Assert.StartsWith("mft-archive:", reference);
        Assert.Empty(await adapter.PickupInboundAsync());
    }

    [Fact]
    public async Task Inbound_ShouldAcceptOnlyOutAndRetFinalEnvelopes()
    {
        var adapter = Create();
        var inbound = Path.Combine(_root, "inbound");
        Directory.CreateDirectory(inbound);
        await File.WriteAllBytesAsync(Path.Combine(inbound, "one.OUT.env"), [1]);
        await File.WriteAllBytesAsync(Path.Combine(inbound, "two.RET.env"), [2]);
        await File.WriteAllBytesAsync(Path.Combine(inbound, "unknown.env"), [3]);
        await File.WriteAllBytesAsync(Path.Combine(inbound, ".partial.OUT.env"), [4]);

        var artifacts = await adapter.PickupInboundAsync();

        Assert.Equal(2, artifacts.Count);
        Assert.Contains(artifacts, x => x.FileName == "one.OUT.env");
        Assert.Contains(artifacts, x => x.FileName == "two.RET.env");
        Assert.True(File.Exists(Path.Combine(inbound, "unknown.env")));
        Assert.True(File.Exists(Path.Combine(inbound, ".partial.OUT.env")));
    }

    private AchColombiaManagedMftFolderAdapter Create(bool enabled = true) => new(Options.Create(new AchColombiaManagedMftOptions
    {
        Enabled = enabled,
        OutboundPath = Path.Combine(_root, "outbound"),
        InboundPath = Path.Combine(_root, "inbound"),
        ProcessingPath = Path.Combine(_root, "processing"),
        ArchivePath = Path.Combine(_root, "archive")
    }));
    private static string Hash(byte[] content) => Convert.ToHexString(SHA256.HashData(content));
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
}
