using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Tests;

public class AchColExternalNamingGateTests
{
    [Fact]
    public async Task AchColLive_RemainsBlockedWhileContractualNamingIsNotHomologated()
    {
        var sequence = new RecordingSequenceService();
        var builder = new ExternalFileNameBuilder(
            sequence,
            new IdentifierMap(),
            generationOptions: Options.Create(new NachaGenerationOptions
            {
                ExecutionScope = "LIVE",
                AchColExternalNamingHomologated = false
            }));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => builder.BuildAsync(CreateContext()));

        Assert.Contains("FILENAME-CONTRACTUAL-NOT-DEMONSTRATED", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, sequence.CallCount);
    }

    [Fact]
    public async Task AchColDevelopment_CanBuildForControlledTesting()
    {
        var builder = new ExternalFileNameBuilder(
            new RecordingSequenceService(),
            new IdentifierMap(),
            generationOptions: Options.Create(new NachaGenerationOptions
            {
                ExecutionScope = "DEVELOPMENT",
                AchColExternalNamingHomologated = false
            }));

        var result = await builder.BuildAsync(CreateContext());

        Assert.Equal("1234567.001.1", result.FullName);
        Assert.Equal('A', result.FileIdModifier);
    }

    private static ExternalFileNameContext CreateContext() => new()
    {
        ClearingHouseId = 1,
        ClearingHouseCode = "ACHCOL",
        ClearingHouseOriginCode = "1234567",
        CycleNumber = 1,
        ProcessingDate = new DateTime(2026, 7, 16),
        ExternalFileType = ExternalFileType.NachaOut,
        Flow = ExternalFileFlow.Originacion,
        Direction = ExternalFileDirection.Outbound
    };

    private sealed class RecordingSequenceService : IExternalFileNameSequenceService
    {
        public int CallCount { get; private set; }

        public Task<int> ReserveNextSequenceAsync(ExternalFileNameContext context, CancellationToken ct = default)
        {
            CallCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class IdentifierMap : INachaFileIdentifierMapService
    {
        public Task<char> ResolveIdentifierAsync(int sequence, CancellationToken ct = default)
            => Task.FromResult('A');
    }
}
