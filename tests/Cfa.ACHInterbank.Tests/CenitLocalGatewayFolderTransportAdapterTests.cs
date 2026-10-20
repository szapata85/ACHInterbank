using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Tests;

public sealed class CenitLocalGatewayFolderTransportAdapterTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"cenit-gateway-{Guid.NewGuid():N}");

    [Fact]
    public void CenitLocalGatewayOptions_ShouldBeDisabledWhenUnspecified()
    {
        Assert.False(new CenitLocalGatewayOptions().Enabled);
    }

    [Fact]
    public void CenitLocalGatewayOptions_ShouldEnableWhenExplicitlyConfigured()
    {
        Assert.True(Options.Create(new CenitLocalGatewayOptions { Enabled = true }).Value.Enabled);
    }

    [Fact]
    public async Task FolderAdapter_ShouldHandoffPickupAndArchiveAtomically()
    {
        var input = Path.Combine(_root, "input");
        var output = Path.Combine(_root, "output");
        var archive = Path.Combine(_root, "archive");
        var adapter = new CenitLocalGatewayFolderTransportAdapter(Options.Create(new CenitLocalGatewayOptions
        {
            Enabled = true,
            InputPath = input,
            OutputPath = output,
            ArchivePath = archive
        }));

        await adapter.HandoffOutboundAsync("0001001.001.20260831.1", Encoding.ASCII.GetBytes("NACHA"));

        Assert.Equal("NACHA", await File.ReadAllTextAsync(Path.Combine(input, "0001001.001.20260831.1")));
        Assert.Empty(Directory.EnumerateFiles(input, "*.tmp"));

        Directory.CreateDirectory(output);
        var xmlName = "ACK-001.xml";
        await File.WriteAllTextAsync(Path.Combine(output, xmlName), "<FileAck />");
        await File.WriteAllTextAsync(Path.Combine(output, "ACK-001.meta.json"), JsonSerializer.Serialize(new
        {
            sourceResponseId = "ACK-001",
            artifactFileName = xmlName,
            messageType = "XML",
            receivedAtUtc = new DateTime(2026, 8, 31, 12, 0, 0, DateTimeKind.Utc),
            relatedOutboundFileName = "0001001.001.20260831.1",
            relatedReference = (string?)null,
            transactionTraceNumber = (string?)null,
            achCycleId = "CENIT-CYCLE-1"
        }));

        var artifact = Assert.Single(await adapter.PickupInboundAsync());
        Assert.Equal("<FileAck />", artifact.Content);
        await adapter.ArchiveInboundAsync(artifact);

        Assert.False(File.Exists(Path.Combine(output, xmlName)));
        Assert.True(File.Exists(Path.Combine(archive, xmlName)));
        Assert.True(File.Exists(Path.Combine(archive, "ACK-001.meta.json")));
    }

    public void Dispose()
    {
        var fullRoot = Path.GetFullPath(_root);
        var tempRoot = Path.GetFullPath(Path.GetTempPath());
        if (fullRoot.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(fullRoot))
            Directory.Delete(fullRoot, true);
    }
}
