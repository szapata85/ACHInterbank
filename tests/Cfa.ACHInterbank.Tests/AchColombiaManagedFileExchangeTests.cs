using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACHSobreDigital.ManagedDigitalEnvelope;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColombiaManagedFileExchangeTests
{
    [Theory]
    [InlineData(AchManagedFileExecutionOrigin.Manual)]
    [InlineData(AchManagedFileExecutionOrigin.Automatic)]
    public async Task Outbound_ShouldUseSameServicePath_AndPersistImmutableContent(AchManagedFileExecutionOrigin origin)
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Configuration.AutomaticOutboundEnabled = true;
        await fixture.Context.SaveChangesAsync();
        var result = await fixture.Service.ExecuteOutboundAsync("ACH-1", origin, "operator", $"out-{origin}");
        Assert.Equal(1, result.Succeeded);
        var transfer = await fixture.Context.AchManagedFileTransfers.Include(x => x.Events).SingleAsync();
        Assert.Equal(AchManagedFileTransferStatus.Transferred, transfer.Status);
        Assert.NotNull(transfer.RetainedContent);
        Assert.Contains(transfer.Events, x => x.EventType == "OutboundPrepared");
        Assert.Contains(transfer.Events, x => x.EventType == "OutboundAttempt" && x.Result == "Succeeded");
    }

    [Fact]
    public async Task AutomaticOutboundDisabled_ShouldNotGenerateOrTransfer()
    {
        await using var fixture = await Fixture.CreateAsync();
        var result = await fixture.Service.ExecuteOutboundAsync("ACH-1", AchManagedFileExecutionOrigin.Automatic, "task", "disabled");
        Assert.Equal(0, result.Processed);
        fixture.Builder.Verify(x => x.BuildNachaFileArtifactByCycleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ManualOutbound_ShouldRemainAvailableWhenAutomaticExecutionIsDisabled()
    {
        await using var fixture = await Fixture.CreateAsync();
        var result = await fixture.Service.ExecuteOutboundAsync("ACH-1", AchManagedFileExecutionOrigin.Manual, "operator", "manual-only");
        Assert.Equal(1, result.Succeeded);
    }

    [Theory]
    [InlineData(AchManagedFileDirection.Outbound)]
    [InlineData(AchManagedFileDirection.Inbound)]
    public async Task DisabledProfile_ShouldFailClosedForManualExecution(AchManagedFileDirection direction)
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Configuration.ProfileEnabled = false;
        await fixture.Context.SaveChangesAsync();

        var error = direction == AchManagedFileDirection.Outbound
            ? await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ExecuteOutboundAsync("ACH-1", AchManagedFileExecutionOrigin.Manual, "operator", "profile-off"))
            : await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ExecuteInboundAsync(AchManagedFileExecutionOrigin.Manual, "operator", "profile-off"));

        Assert.Equal("ACHCOL_MFT_DISABLED", error.Message);
    }

    [Fact]
    public async Task Administration_ShouldPersistSafeConfigurationAndProtectedCredential()
    {
        await using var fixture = await Fixture.CreateAsync();
        var before = await fixture.Service.GetAdministrationAsync();
        var updated = await fixture.Service.UpdateAdministrationAsync(new(
            "MFT principal", "ManagedFolder", "ManagedFile", true, "mft.local", 443, "operador",
            false, false, true, true, 4, 120, 180,
            "out-persisted", "in-persisted", "archive-persisted", before.ConcurrencyToken), "operator");

        var credential = await fixture.Service.SetCredentialAsync(new("Password", "super-secret"), "operator");
        var persisted = await fixture.Context.AchManagedFileTransferConfigurations.SingleAsync();
        var read = await fixture.Service.GetAdministrationAsync();

        Assert.Equal("out-persisted", updated.OutboundLocation);
        Assert.True(credential.CredentialConfigured);
        Assert.NotEqual("super-secret", persisted.ProtectedCredential);
        Assert.DoesNotContain("super-secret", System.Text.Json.JsonSerializer.Serialize(read));
        Assert.DoesNotContain(await fixture.Context.AuditLogs.Select(x => (x.BeforeJson ?? "") + (x.AfterJson ?? "") + (x.ChangedFields ?? "")).ToListAsync(), x => x.Contains("super-secret", StringComparison.Ordinal));
    }

    [Fact]
    public async Task EffectiveConfiguration_ShouldUsePersistedRoutesAndStaticProcessingLimits()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Configuration.ProfileEnabled = true;
        fixture.Configuration.OutboundLocation = "out-persisted";
        fixture.Configuration.InboundLocation = "in-persisted";
        fixture.Configuration.ArchiveLocation = "archive-persisted";
        await fixture.Context.SaveChangesAsync();

        var provider = new AchColombiaManagedMftConfigurationProvider(fixture.Context, Options.Create(new AchColombiaManagedMftOptions
        { ProcessingPath = "processing-static", MaximumFileBytes = 123 }));
        var effective = await provider.GetEffectiveAsync();

        Assert.True(effective.Enabled);
        Assert.Equal("out-persisted", effective.OutboundPath);
        Assert.Equal("in-persisted", effective.InboundPath);
        Assert.Equal("archive-persisted", effective.ArchivePath);
        Assert.Equal("processing-static", effective.ProcessingPath);
        Assert.Equal(123, effective.MaximumFileBytes);
    }

    [Theory]
    [InlineData(AchManagedFileExecutionOrigin.Manual)]
    [InlineData(AchManagedFileExecutionOrigin.Automatic)]
    public async Task Inbound_ShouldUseSameServicePath_AndArchive(AchManagedFileExecutionOrigin origin)
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Configuration.AutomaticInboundEnabled = true;
        fixture.Adapter.Artifacts = [new("received.OUT.env", [7, 8], Hash([7, 8]), "claim")];
        await fixture.Context.SaveChangesAsync();
        var result = await fixture.Service.ExecuteInboundAsync(origin, "operator", $"in-{origin}");
        Assert.Equal(1, result.Succeeded);
        var transfer = await fixture.Context.AchManagedFileTransfers.Include(x => x.Events).SingleAsync();
        Assert.Equal(AchManagedFileTransferStatus.Processed, transfer.Status);
        Assert.NotNull(transfer.ArchiveReference);
        Assert.Contains(transfer.Events, x => x.EventType == "Archived");
    }

    [Fact]
    public async Task InboundDuplicate_ShouldNotCreateSecondTransfer()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Adapter.Artifacts = [new("first.OUT.env", [9], Hash([9]), "claim-1")];
        await fixture.Service.ExecuteInboundAsync(AchManagedFileExecutionOrigin.Manual, "operator", "first");
        fixture.Adapter.Artifacts = [new("renamed.OUT.env", [9], Hash([9]), "claim-2")];
        await fixture.Service.ExecuteInboundAsync(AchManagedFileExecutionOrigin.Manual, "operator", "second");
        Assert.Equal(1, await fixture.Context.AchManagedFileTransfers.CountAsync());
        Assert.Contains(await fixture.Context.AchManagedFileTransferEvents.ToListAsync(), x => x.EventType == "DuplicateDetected");
    }

    [Fact]
    public async Task SameInboundNameWithDifferentContent_ShouldReject()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Adapter.Artifacts = [new("same.OUT.env", [1], Hash([1]), "claim-1")];
        await fixture.Service.ExecuteInboundAsync(AchManagedFileExecutionOrigin.Manual, "operator", "first");
        fixture.Adapter.Artifacts = [new("same.OUT.env", [2], Hash([2]), "claim-2")];
        await fixture.Service.ExecuteInboundAsync(AchManagedFileExecutionOrigin.Manual, "operator", "second");
        Assert.Equal(AchManagedFileTransferStatus.Rejected, (await fixture.Context.AchManagedFileTransfers.OrderBy(x => x.CreatedAtUtc).ToListAsync()).Last().Status);
    }

    [Fact]
    public async Task InboundRestartRecovery_ShouldResumePersistedClaimWithoutCreatingAnotherTransfer()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Configuration.AutomaticInboundEnabled = true;
        var transfer = fixture.SeedTransfer(AchManagedFileDirection.Inbound, AchManagedFileTransferStatus.Received, [8]);
        fixture.Adapter.Artifacts = [new("file.env", [8], Hash([8]), "processing:file.env")];
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Service.ExecuteInboundAsync(AchManagedFileExecutionOrigin.Automatic, "task", "recovery");

        Assert.Equal(1, result.Succeeded);
        Assert.Equal(1, await fixture.Context.AchManagedFileTransfers.CountAsync());
        Assert.Contains(await fixture.Context.AchManagedFileTransferEvents.ToListAsync(), x => x.EventType == "InboundRecovery");
    }

    [Fact]
    public async Task Retry_ShouldReuseRetainedContentWithoutRegeneration()
    {
        await using var fixture = await Fixture.CreateAsync();
        var transfer = fixture.SeedTransfer(AchManagedFileDirection.Outbound, AchManagedFileTransferStatus.RetryPending, [3]);
        await fixture.Context.SaveChangesAsync();
        await fixture.Service.RetryAsync(transfer.Id, "operator", "retry-1");
        fixture.Builder.Verify(x => x.BuildNachaFileArtifactByCycleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(AchManagedFileTransferStatus.Transferred, (await fixture.Context.AchManagedFileTransfers.FindAsync(transfer.Id))!.Status);
    }

    [Fact]
    public async Task UncertainTransport_ShouldRemainEligibleForControlledRetry()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Adapter.OutboundResult = new(false, true, true, "TIMEOUT", "Resultado no confirmado.", null);
        var transfer = fixture.SeedTransfer(AchManagedFileDirection.Outbound, AchManagedFileTransferStatus.RetryPending, [3]);
        await fixture.Context.SaveChangesAsync();
        var detail = await fixture.Service.RetryAsync(transfer.Id, "operator", "retry-uncertain");
        Assert.Equal(AchManagedFileTransferStatus.Uncertain, detail.Status);
    }

    [Fact]
    public async Task NonRetryableTransportFailure_ShouldFailClosedForManualRetry()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Adapter.OutboundResult = new(false, false, false, "REJECTED", "Entrega rechazada.", null);
        var transfer = fixture.SeedTransfer(AchManagedFileDirection.Outbound, AchManagedFileTransferStatus.RetryPending, [3]);
        await fixture.Context.SaveChangesAsync();
        var detail = await fixture.Service.RetryAsync(transfer.Id, "operator", "first");
        Assert.Equal(AchManagedFileTransferStatus.Failed, detail.Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.RetryAsync(transfer.Id, "operator", "second"));
    }

    [Fact]
    public async Task Reprocess_ShouldRequireEligibleStateAndBecomeIdempotentlyIneligibleAfterSuccess()
    {
        await using var fixture = await Fixture.CreateAsync();
        var transfer = fixture.SeedTransfer(AchManagedFileDirection.Inbound, AchManagedFileTransferStatus.Rejected, [4]);
        transfer.IncomingNachaFileIngestionId = Guid.NewGuid();
        await fixture.Context.SaveChangesAsync();
        var detail = await fixture.Service.ReprocessAsync(transfer.Id, "operator");
        Assert.Equal(AchManagedFileTransferStatus.Processed, detail.Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ReprocessAsync(transfer.Id, "operator"));
    }

    [Fact]
    public async Task ArchiveDownloadAndRetire_ShouldPreserveHistoryAndMetadata()
    {
        await using var fixture = await Fixture.CreateAsync();
        var transfer = fixture.SeedTransfer(AchManagedFileDirection.Outbound, AchManagedFileTransferStatus.Transferred, [4, 5]);
        await fixture.Context.SaveChangesAsync();
        await fixture.Service.ArchiveAsync(transfer.Id, "operator");
        Assert.NotNull(await fixture.Service.DownloadAsync(transfer.Id, "operator"));
        var retired = await fixture.Service.RetireAsync(transfer.Id, "operator", "Fin de conservación operativa");
        Assert.True(retired.Retired);
        Assert.Equal(Hash([4, 5]), retired.ContentSha256);
        Assert.NotEmpty(retired.History);
        Assert.Null(await fixture.Service.DownloadAsync(transfer.Id, "operator"));
    }

    [Fact]
    public async Task CorrectedOutbound_ShouldCreateNewArtifactAndPreservePredecessorRelationship()
    {
        await using var fixture = await Fixture.CreateAsync();
        var predecessor = fixture.SeedTransfer(AchManagedFileDirection.Outbound, AchManagedFileTransferStatus.Failed, [6]);
        predecessor.AchCycleId = "ACH-1";
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Service.ExecuteOutboundAsync("ACH-1", AchManagedFileExecutionOrigin.Manual,
            "operator", "corrected", default, predecessor.Id);

        Assert.Equal(1, result.Succeeded);
        var replacement = await fixture.Context.AchManagedFileTransfers.SingleAsync(x => x.CorrectedFromTransferId == predecessor.Id);
        Assert.NotEqual(predecessor.Id, replacement.Id);
        Assert.Equal(AchManagedFileTransferStatus.Transferred, replacement.Status);
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));

    private sealed class Fixture : IAsyncDisposable
    {
        public required AchDbContext Context { get; init; }
        public required Mock<INachaFileBuilder> Builder { get; init; }
        public required StubAdapter Adapter { get; init; }
        public required AchManagedFileTransferConfiguration Configuration { get; init; }
        public required AchColombiaManagedFileExchangeService Service { get; init; }

        public static async Task<Fixture> CreateAsync()
        {
            var options = new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase($"achcol-mft-{Guid.NewGuid():N}").Options;
            var context = new AchDbContext(options);
            var chamber = new ClearingHouse { Id = 1, ClearingHouseId = 1, Code = "ACHCOL", Name = "ACH Colombia", OriginCode = "0001001" };
            context.ClearingHouses.Add(chamber);
            context.AchCycles.Add(new AchCycle { Id = "ACH-1", CycleName = "Ciclo 1", ProcessingDate = new(2026, 8, 31), CutoffTime = new(8, 30, 0), ClearingHouseId = 1 });
            var configuration = new AchManagedFileTransferConfiguration { ClearingHouseId = 1, ProfileEnabled = true, ManualOutboundAllowed = true, ManualInboundAllowed = true };
            context.AchManagedFileTransferConfigurations.Add(configuration);
            await context.SaveChangesAsync();
            var builder = new Mock<INachaFileBuilder>();
            builder.Setup(x => x.BuildNachaFileArtifactByCycleAsync("ACH-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new NachaFileBuildArtifact(new string('1', 106), []));
            var naming = new Mock<IExternalFileNamePolicy>();
            naming.Setup(x => x.GenerateExternalNameAsync(It.IsAny<ExternalFileNameContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ExternalFileNameContext value, CancellationToken _) => new ExternalFileNamePolicyResult
                {
                    ExternalFileName = value.IdempotencyKey?.Contains("CORRECTED_FROM:NONE", StringComparison.Ordinal) == true
                        ? "0001001.001.20260831.1.OUT" : "0001001.001.20260831.2.OUT",
                    Components = new ExternalFileNameComponents()
                });
            var time = new Mock<IOperationalTimeSnapshotProvider>();
            time.Setup(x => x.GetOrCreate(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<TimeOnly>()))
                .Returns(new OperationalTimeSnapshot(DateTime.UtcNow, new(2026, 8, 31), new(2026, 8, 31), "America/Bogota"));
            var envelope = new Mock<INachaExportDigitalEnvelopeService>();
            envelope.Setup(x => x.EncryptAsync(1, It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int _, string fileName, byte[] _, string _, CancellationToken _) =>
                    new ManagedDigitalEnvelopeResult(Encoding.ASCII.GetBytes($"protected:{fileName}"), $"{fileName}.env", "application/octet-stream", 1, "thumb", "test"));
            var ingestion = new Mock<IIncomingNachaIngestionAppService>();
            ingestion.Setup(x => x.IngestAsync(It.IsAny<IncomingNachaIngestionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IncomingNachaIngestionRequest request, CancellationToken _) => new IncomingNachaIngestionResponse { IngestionId = Guid.NewGuid(), OriginalFileName = request.FileName, IngestionStatus = IncomingNachaIngestionStatus.Completado, ParsingStatus = IncomingNachaParsingStatus.Exitoso, OperationalDate = new(2026, 8, 31) });
            var adapter = new StubAdapter();
            var encryption = new Mock<Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces.IEncryptionService>();
            encryption.Setup(x => x.Encrypt("super-secret")).Returns("protected:super-secret");
            var service = new AchColombiaManagedFileExchangeService(context, builder.Object, naming.Object, time.Object, envelope.Object,
                new AchFileExportAuditService(context), ingestion.Object, adapter, encryption.Object, Options.Create(new AchColombiaManagedMftOptions
                { Enabled = true, OutboundPath = "out", InboundPath = "in", ProcessingPath = "processing", ArchivePath = "archive", MaximumFileBytes = 99 }));
            return new() { Context = context, Builder = builder, Adapter = adapter, Configuration = configuration, Service = service };
        }

        public AchManagedFileTransfer SeedTransfer(AchManagedFileDirection direction, AchManagedFileTransferStatus status, byte[] content)
        {
            var transfer = new AchManagedFileTransfer { ClearingHouseId = 1, Direction = direction, LogicalFileIdentity = Guid.NewGuid().ToString("N"), PhysicalFileName = "file.env", ContentSha256 = Hash(content), FileSize = content.Length, RetainedContent = content, OperationalDate = new(2026, 8, 31), ExecutionOrigin = AchManagedFileExecutionOrigin.Manual, Status = status, CorrelationId = Guid.NewGuid().ToString("N"), IdempotencyKey = Guid.NewGuid().ToString("N") };
            Context.Add(transfer); return transfer;
        }
        public async ValueTask DisposeAsync() => await Context.DisposeAsync();
    }

    private sealed class StubAdapter : IAchColombiaManagedMftAdapter
    {
        public bool Enabled => true;
        public IReadOnlyList<AchManagedMftArtifact> Artifacts { get; set; } = [];
        public AchManagedMftResult OutboundResult { get; set; } = new(true, false, false, "OK", "Entregado.", "out");
        public Task<AchManagedMftResult> HandoffOutboundAsync(string fileName, byte[] content, string contentSha256, CancellationToken ct = default) => Task.FromResult(OutboundResult);
        public Task<IReadOnlyList<AchManagedMftArtifact>> PickupInboundAsync(CancellationToken ct = default) => Task.FromResult(Artifacts);
        public Task<string> ArchiveInboundAsync(AchManagedMftArtifact artifact, CancellationToken ct = default) => Task.FromResult($"archive:{artifact.FileName}");
    }
}
