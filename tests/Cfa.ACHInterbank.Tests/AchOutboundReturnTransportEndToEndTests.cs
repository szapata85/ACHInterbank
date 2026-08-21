using System.Security.Cryptography;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACHSobreDigital.ManagedDigitalEnvelope;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.External.Connections;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchOutboundReturnTransportEndToEndTests
{
    [Fact]
    public async Task ManagedHandoff_TransmitsAtomically_AndIsContentIdempotent()
    {
        var directory = NewTemporaryDirectory();
        try
        {
            var adapter = CreateAdapter(directory);
            var content = "protected-return"u8.ToArray();
            var request = TransportRequest("0001122.001.1.ENV", content, "transport-1");

            var first = await adapter.TransmitAsync(request);
            var replay = await adapter.TransmitAsync(request);

            Assert.True(first.Succeeded);
            Assert.Equal("HANDOFF_COMMITTED", first.ResultCode);
            Assert.True(replay.Succeeded);
            Assert.Equal("HANDOFF_ALREADY_COMMITTED", replay.ResultCode);
            Assert.Equal(first.ExternalReference, replay.ExternalReference);
            Assert.Single(Directory.GetFiles(directory, "*.ENV"));
            Assert.DoesNotContain(Directory.GetFiles(directory), x => x.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData(7002, "ACH", "ACH Colombia", "ACH-RET-C1", "0001122.001.1")]
    [InlineData(2, "CENIT", "CENIT", "CENIT-RET-C2", "0001122.001.2")]
    public async Task Dispatch_ThenAcceptedResult_PersistsWholeLifecycle_AndDeduplicates(
        int clearingHouseId,
        string clearingHouseCode,
        string clearingHouseName,
        string cycleId,
        string fileName)
    {
        var directory = NewTemporaryDirectory();
        try
        {
            await using var context = CreateContext();
            await SeedTransactionAsync(context, clearingHouseId, clearingHouseCode, clearingHouseName, cycleId);
            var plain = "return-out"u8.ToArray();
            var protectedContent = "protected-return-out"u8.ToArray();
            var artifact = new AchOutboundReturnArtifact(
                fileName,
                plain,
                10,
                1,
                cycleId,
                clearingHouseId,
                [901],
                Convert.ToHexString(SHA256.HashData(plain)));
            var artifactService = new Mock<IAchOutboundReturnArtifactService>();
            artifactService.Setup(x => x.BuildAsync(artifact.FileName, It.IsAny<CancellationToken>())).ReturnsAsync(artifact);
            var envelope = new Mock<INachaExportDigitalEnvelopeService>();
            envelope.Setup(x => x.EncryptAsync(clearingHouseId, artifact.FileName, artifact.Content, "operator", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ManagedDigitalEnvelopeResult(
                    protectedContent,
                    artifact.FileName + ".ENV",
                    "application/octet-stream",
                    1,
                    "masked",
                    "test-profile"));
            var audit = new AchFileExportAuditService(context);
            var recorder = new AchFileTransmissionEvidenceRecorder(context);
            var dispatch = new AchOutboundReturnDispatchService(
                context,
                Mock.Of<IAchReturnsService>(),
                artifactService.Object,
                envelope.Object,
                audit,
                CreateAdapter(directory),
                recorder);

            var sent = await dispatch.DispatchAsync(new AchOutboundReturnDispatchRequest(
                artifact.FileName,
                "dispatch-request-1",
                "operator"));
            var replay = await dispatch.DispatchAsync(new AchOutboundReturnDispatchRequest(
                artifact.FileName,
                "dispatch-request-1",
                "operator"));

            Assert.True(sent.Succeeded);
            Assert.Equal(AchFileExportLifecycleStatus.Transmitted, sent.LifecycleStatus);
            Assert.True(replay.WasDuplicate);
            Assert.Single(await context.AchFileTransmissionAttempts.ToListAsync());
            var transmitted = await context.AchFileExports.SingleAsync(x => x.IsEncrypted);
            Assert.Equal(clearingHouseId, transmitted.ClearingHouseId);
            Assert.Equal(cycleId, transmitted.AchCycleId);
            Assert.False(string.IsNullOrWhiteSpace(transmitted.TransmissionReference));
            Assert.NotNull(transmitted.TransmittedAtUtc);

            var processor = new AchOutboundReturnResultProcessor(context, recorder);
            var acceptedRequest = new AchOutboundReturnResultRequest(
                "mft-event-1",
                transmitted.FileName,
                transmitted.TransmissionReference!,
                AchOutboundReturnOutcome.Accepted,
                "ACCEPTED",
                DateTime.UtcNow);
            var accepted = await processor.ProcessAsync(acceptedRequest);
            var duplicate = await processor.ProcessAsync(acceptedRequest);

            Assert.True(accepted.Applied);
            Assert.Equal(AchResponseCorrelationStatus.Matched, accepted.CorrelationStatus);
            Assert.Equal(AchFileExportLifecycleStatus.Accepted, accepted.LifecycleStatus);
            Assert.True(duplicate.WasDuplicate);
            Assert.Single(await context.AchFileTransportResults.ToListAsync());
            transmitted = await context.AchFileExports.SingleAsync(x => x.IsEncrypted);
            Assert.Equal(AchFileExportLifecycleStatus.Accepted, transmitted.LifecycleStatus);
            Assert.Equal("ACCEPTED", transmitted.AcknowledgementCode);
            Assert.NotNull(transmitted.AcknowledgedAtUtc);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task RetryAfterTechnicalFailure_ReusesPersistedProtectedPayload_WithoutRegeneration()
    {
        await using var context = CreateContext();
        await SeedTransactionAsync(context);
        var plain = "return-out"u8.ToArray();
        var protectedContent = "protected-return-out"u8.ToArray();
        var artifact = new AchOutboundReturnArtifact(
            "0001122.001.1",
            plain,
            10,
            1,
            "ACH-RET-C1",
            7002,
            [901],
            Convert.ToHexString(SHA256.HashData(plain)));
        var artifactService = new Mock<IAchOutboundReturnArtifactService>();
        artifactService.Setup(x => x.BuildAsync(artifact.FileName, It.IsAny<CancellationToken>())).ReturnsAsync(artifact);
        var envelope = new Mock<INachaExportDigitalEnvelopeService>();
        envelope.Setup(x => x.EncryptAsync(7002, artifact.FileName, artifact.Content, "operator", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ManagedDigitalEnvelopeResult(
                protectedContent,
                artifact.FileName + ".ENV",
                "application/octet-stream",
                1,
                "masked",
                "test-profile"));
        var reference = $"CFA-MFT-HANDOFF:{Convert.ToHexString(SHA256.HashData(protectedContent))}";
        var transport = new Mock<IAchOutboundReturnTransportAdapter>();
        transport.SetupSequence(x => x.TransmitAsync(It.IsAny<AchOutboundReturnTransportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchOutboundReturnTransportResult(false, true, "TEMPORARY_IO", "Falla temporal.", null, DateTime.UtcNow))
            .ReturnsAsync(new AchOutboundReturnTransportResult(true, false, "HANDOFF_COMMITTED", "Entregado.", reference, DateTime.UtcNow));
        var recorder = new AchFileTransmissionEvidenceRecorder(context);
        var dispatch = new AchOutboundReturnDispatchService(
            context,
            Mock.Of<IAchReturnsService>(),
            artifactService.Object,
            envelope.Object,
            new AchFileExportAuditService(context),
            transport.Object,
            recorder);

        var failed = await dispatch.DispatchAsync(new AchOutboundReturnDispatchRequest(artifact.FileName, "retry-1", "operator"));
        var succeeded = await dispatch.DispatchAsync(new AchOutboundReturnDispatchRequest(artifact.FileName, "retry-2", "operator"));

        Assert.False(failed.Succeeded);
        Assert.True(failed.Retryable);
        Assert.True(succeeded.Succeeded);
        Assert.Equal(2, await context.AchFileTransmissionAttempts.CountAsync());
        Assert.All(await context.AchFileTransmissionAttempts.ToListAsync(), x => Assert.Equal(protectedContent, x.ProtectedContent));
        envelope.Verify(x => x.EncryptAsync(7002, artifact.FileName, artifact.Content, "operator", It.IsAny<CancellationToken>()), Times.Once);
        transport.Verify(x => x.TransmitAsync(
            It.Is<AchOutboundReturnTransportRequest>(request => request.Content.SequenceEqual(protectedContent)),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ResultWithoutCorrelation_IsPersistedForManualReview_WithoutMutatingExport()
    {
        await using var context = CreateContext();
        var processor = new AchOutboundReturnResultProcessor(
            context,
            new AchFileTransmissionEvidenceRecorder(context));

        var result = await processor.ProcessAsync(new AchOutboundReturnResultRequest(
            "unknown-event-1",
            "0001122.001.1.ENV",
            "CFA-MFT-HANDOFF:UNKNOWN",
            AchOutboundReturnOutcome.Unknown,
            "UNKNOWN",
            DateTime.UtcNow));

        Assert.Equal(AchResponseCorrelationStatus.NotFound, result.CorrelationStatus);
        Assert.True(result.RequiresManualReview);
        Assert.False(result.Applied);
        Assert.Single(await context.AchFileTransportResults.ToListAsync());
        Assert.Empty(await context.AchFileExports.ToListAsync());
    }

    [Fact]
    public async Task CenitRejectedResult_IsCorrelatedPersistedAndReplaySafe()
    {
        await using var context = CreateContext();
        var export = new AchFileExport
        {
            AchCycleId = "CENIT-RET-C2",
            ClearingHouseId = 2,
            ExportKind = "RETURN",
            FileName = "0001122.001.2.ENV",
            IsEncrypted = true,
            GeneratedAtUtc = DateTime.UtcNow.AddMinutes(-2),
            LifecycleStatus = AchFileExportLifecycleStatus.Transmitted,
            TransmissionReference = "CFA-MFT-HANDOFF:CENIT-REJECT",
            TransmittedAtUtc = DateTime.UtcNow.AddMinutes(-1)
        };
        context.AchFileExports.Add(export);
        await context.SaveChangesAsync();
        var processor = new AchOutboundReturnResultProcessor(context, new AchFileTransmissionEvidenceRecorder(context));
        var request = new AchOutboundReturnResultRequest(
            "cenit-reject-event-1",
            export.FileName,
            export.TransmissionReference,
            AchOutboundReturnOutcome.Rejected,
            "REJECTED",
            DateTime.UtcNow,
            "Rechazo funcional controlado.");

        var rejected = await processor.ProcessAsync(request);
        var replay = await processor.ProcessAsync(request);

        Assert.True(rejected.Applied);
        Assert.Equal(AchResponseCorrelationStatus.Matched, rejected.CorrelationStatus);
        Assert.Equal(AchFileExportLifecycleStatus.Rejected, rejected.LifecycleStatus);
        Assert.True(replay.WasDuplicate);
        Assert.Single(await context.AchFileTransportResults.ToListAsync());
        var persisted = await context.AchFileExports.SingleAsync();
        Assert.Equal(2, persisted.ClearingHouseId);
        Assert.Equal(AchFileExportLifecycleStatus.Rejected, persisted.LifecycleStatus);
        Assert.Equal("REJECTED", persisted.AcknowledgementCode);
    }

    [Fact]
    public async Task ConflictingFinalResult_IsPersistedForManualReview_AndDoesNotReverseAcceptedState()
    {
        await using var context = CreateContext();
        var export = new AchFileExport
        {
            AchCycleId = "CENIT-RET-C2",
            ClearingHouseId = 2,
            ExportKind = "RETURN",
            FileName = "0001122.001.2.ENV",
            IsEncrypted = true,
            GeneratedAtUtc = DateTime.UtcNow,
            LifecycleStatus = AchFileExportLifecycleStatus.Accepted,
            TransmissionReference = "CFA-MFT-HANDOFF:ABC",
            TransmittedAtUtc = DateTime.UtcNow.AddMinutes(-2),
            AcknowledgedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            AcknowledgementCode = "ACCEPTED"
        };
        context.AchFileExports.Add(export);
        await context.SaveChangesAsync();
        var processor = new AchOutboundReturnResultProcessor(context, new AchFileTransmissionEvidenceRecorder(context));

        var result = await processor.ProcessAsync(new AchOutboundReturnResultRequest(
            "conflict-event-1",
            export.FileName,
            export.TransmissionReference,
            AchOutboundReturnOutcome.Rejected,
            "REJECTED",
            DateTime.UtcNow));

        Assert.Equal(AchResponseCorrelationStatus.ManualReviewRequired, result.CorrelationStatus);
        Assert.True(result.RequiresManualReview);
        Assert.False(result.Applied);
        Assert.Equal(AchFileExportLifecycleStatus.Accepted, (await context.AchFileExports.SingleAsync()).LifecycleStatus);
    }

    private static AchOutboundReturnTransportAdapter CreateAdapter(string directory)
        => new(Options.Create(new AchOutboundReturnTransportOptions
        {
            Enabled = true,
            Mode = "CfaManagedHandoff",
            HandoffDirectory = directory,
            MaxFileBytes = 1024 * 1024
        }));

    private static AchOutboundReturnTransportRequest TransportRequest(string fileName, byte[] content, string key)
        => new(1, 7002, fileName, content, Convert.ToHexString(SHA256.HashData(content)), key);

    private static AchDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task SeedTransactionAsync(
        AchDbContext context,
        int clearingHouseId = 7002,
        string clearingHouseCode = "ACH",
        string clearingHouseName = "ACH Colombia",
        string cycleId = "ACH-RET-C1")
    {
        context.ClearingHouses.Add(new ClearingHouse { Id = clearingHouseId, Code = clearingHouseCode, Name = clearingHouseName, OriginCode = "0001122" });
        context.AchCycles.Add(new AchCycle
        {
            Id = cycleId,
            CycleName = "Ciclo 1",
            ProcessingDate = DateTime.UtcNow.Date,
            CutoffTime = TimeSpan.FromHours(8),
            ClearingHouseId = clearingHouseId
        });
        context.AchTransactions.Add(new AchTransaction
        {
            Id = 901,
            AchCycleId = cycleId,
            Type = TransactionTypeEnum.Credit,
            State = AchTransferStateEnum.ReturnedByEpr,
            EffectiveEntryDate = DateTime.UtcNow.Date,
            TransactionCode = "22",
            TraceNumber = "123456780000901",
            ReceivingDFI = "12345678",
            OriginatingDFI = "87654321",
            Amount = 100m,
            Reference = "RET-E2E-901",
            SourceAccountNumber = "source",
            DestinationAccountNumber = "destination"
        });
        await context.SaveChangesAsync();
    }

    private static string NewTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "ach-ret-gap-017", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
