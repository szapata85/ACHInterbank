using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACHSobreDigital.ManagedDigitalEnvelope;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class IncomingNachaIngestionAppServiceTests
{
    [Fact]
    public async Task IngestAsync_AchColombiaEnvelope_DecryptsInMemoryBeforeCanonicalPipeline()
    {
        using var context = BuildContext();
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 7,
            ClearingHouseId = 7,
            Code = "ACHCOL",
            Name = "ACH Colombia",
            OriginCode = "000101006"
        });
        await context.SaveChangesAsync();

        var encrypted = System.Text.Encoding.UTF8.GetBytes("<controlled-envelope />");
        var decrypted = System.Text.Encoding.ASCII.GetBytes(new string('1', 106));
        var expectedPlaintext = decrypted.ToArray();
        const string canonicalName = "0001283.001.20260731.1.OUT";

        var envelope = new Mock<IManagedDigitalEnvelopeService>();
        envelope.Setup(x => x.DecryptAsync(
                It.Is<ManagedDigitalEnvelopeRequest>(request =>
                    request.FileName == $"{canonicalName}.env"
                    && request.ClearingHouseId == 7
                    && request.OperationMode == "LIVE"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ManagedDigitalEnvelopeResult(
                decrypted,
                canonicalName,
                "application/octet-stream",
                11,
                "masked-in-test",
                "test-profile"));

        var resolver = new Mock<IIncomingNachaCycleResolver>();
        resolver.Setup(x => x.ResolveAsync(
                It.Is<IncomingNachaCycleResolutionRequest>(request =>
                    request.FileName == canonicalName
                    && request.Records.Count == 1
                    && request.Records[0] == new string('1', 106)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaCycleResolutionResult
            {
                IsResolved = true,
                Status = IncomingNachaCycleResolutionStatus.ResueltoInferido,
                ClearingHouseId = 7,
                DetectedClearingHouseId = 7,
                AchCycleId = "ACH-20260731-01",
                OperationalDate = new DateTime(2026, 7, 31),
                Confidence = 1m,
                EvidenceJson = "{}"
            });

        var parser = new Mock<INachaParserService>();
        parser.Setup(x => x.ParseAndSaveDetailedAsync(
                It.IsAny<Stream>(),
                canonicalName,
                It.IsAny<NachaParseRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns((Stream stream, string _, NachaParseRequest _, CancellationToken _) =>
            {
                using var copy = new MemoryStream();
                stream.CopyTo(copy);
                Assert.Equal(expectedPlaintext, copy.ToArray());
                return Task.FromResult(new NachaParseResult());
            });

        var sut = new IncomingNachaIngestionAppService(
            context,
            resolver.Object,
            parser.Object,
            Mock.Of<IIncomingNachaPostParseProcessor>(),
            BuildExternalPolicyMock().Object,
            Mock.Of<ILogger<IncomingNachaIngestionAppService>>(),
            digitalEnvelopeService: envelope.Object);

        var response = await sut.IngestAsync(new IncomingNachaIngestionRequest
        {
            FileStream = new MemoryStream(encrypted),
            FileName = $"{canonicalName}.env",
            RequestedClearingHouseId = 7,
            RequestedBy = "tester"
        });

        Assert.Equal(IncomingNachaIngestionStatus.Completado, response.IngestionStatus);
        Assert.Equal(
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(encrypted)).ToLowerInvariant(),
            response.FileHash);
        Assert.Equal(1, await context.IncomingNachaProcessingEvents.CountAsync(x => x.EventType == "DigitalEnvelopeDecrypted"));
        Assert.All(decrypted, value => Assert.Equal((byte)0, value));
    }

    [Fact]
    public async Task IngestAsync_CenitPlaintext_DoesNotInvokeDigitalEnvelope()
    {
        using var context = BuildContext();
        var envelope = new Mock<IManagedDigitalEnvelopeService>(MockBehavior.Strict);
        var resolver = new Mock<IIncomingNachaCycleResolver>();
        resolver.Setup(x => x.ResolveAsync(It.IsAny<IncomingNachaCycleResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaCycleResolutionResult
            {
                IsResolved = false,
                Status = IncomingNachaCycleResolutionStatus.NoResuelto,
                Errors = ["Sin ciclo candidato"]
            });

        var sut = new IncomingNachaIngestionAppService(
            context,
            resolver.Object,
            Mock.Of<INachaParserService>(),
            Mock.Of<IIncomingNachaPostParseProcessor>(),
            BuildExternalPolicyMock().Object,
            Mock.Of<ILogger<IncomingNachaIngestionAppService>>(),
            digitalEnvelopeService: envelope.Object);

        await sut.IngestAsync(new IncomingNachaIngestionRequest
        {
            FileStream = BuildStream(),
            FileName = "0001283.001.20260731.1",
            RequestedClearingHouseId = 8
        });

        envelope.Verify(x => x.DecryptAsync(It.IsAny<ManagedDigitalEnvelopeRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(0, await context.IncomingNachaProcessingEvents.CountAsync(x => x.EventType == "DigitalEnvelopeDecrypted"));
    }

    [Fact]
    public async Task IngestAsync_CreatesIngestionAndProcessing_WhenResolvedAndParsed()
    {
        using var context = BuildContext();
        var resolver = new Mock<IIncomingNachaCycleResolver>();
        resolver.Setup(x => x.ResolveAsync(It.IsAny<IncomingNachaCycleResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaCycleResolutionResult
            {
                IsResolved = true,
                IsAmbiguous = false,
                Status = IncomingNachaCycleResolutionStatus.ResueltoInferido,
                ClearingHouseId = 1,
                DetectedClearingHouseId = 1,
                AchCycleId = "ACH-20260417-01",
                OperationalDate = new DateTime(2026, 04, 17),
                Confidence = 0.95m,
                EvidenceJson = "{}"
            });

        var parser = new Mock<INachaParserService>();
        parser.Setup(x => x.ParseAndSaveDetailedAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<NachaParseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NachaParseResult { TotalBatches = 1, TotalEntries = 2, TotalAddendas = 1, ErrorCount = 0, WarningCount = 0, Failures = [] });

        var sut = new IncomingNachaIngestionAppService(context, resolver.Object, parser.Object, Mock.Of<IIncomingNachaPostParseProcessor>(), BuildExternalPolicyMock().Object, Mock.Of<ILogger<IncomingNachaIngestionAppService>>());

        var response = await sut.IngestAsync(new IncomingNachaIngestionRequest
        {
            FileStream = BuildStream(),
            FileName = "entrante.1.ach",
            RequestedBy = "tester"
        });

        Assert.Equal(IncomingNachaIngestionStatus.Completado, response.IngestionStatus);
        Assert.Equal(1, await context.IncomingNachaFileIngestions.CountAsync());
        Assert.Equal(1, await context.IncomingNachaFileProcessingResults.CountAsync());
        Assert.Equal(0, await context.IncomingNachaTransactionLinks.CountAsync());
    }

    [Fact]
    public async Task IngestAsync_MarksDuplicate_WhenHashAndSizeMatch()
    {
        using var context = BuildContext();
        context.IncomingNachaFileIngestions.Add(new IncomingNachaFileIngestion
        {
            FileName = "prev.ach",
            FileHashSha256 = "0f883fdd6d70f3a190d322674e647f439f4e839c8e24a6a1d5e775b98eddf28e",
            FileSize = 106,
            ContentType = "text/plain",
            UploadedBy = "seed",
            CorrelationId = "seed",
            Notes = "seed"
        });
        await context.SaveChangesAsync();

        var resolver = new Mock<IIncomingNachaCycleResolver>();
        var parser = new Mock<INachaParserService>();
        var sut = new IncomingNachaIngestionAppService(context, resolver.Object, parser.Object, Mock.Of<IIncomingNachaPostParseProcessor>(), BuildExternalPolicyMock().Object, Mock.Of<ILogger<IncomingNachaIngestionAppService>>());

        var response = await sut.IngestAsync(new IncomingNachaIngestionRequest
        {
            FileStream = BuildStream(),
            FileName = "duplicado.ach",
            RequestedBy = "tester"
        });

        Assert.Equal(IncomingNachaIngestionStatus.Duplicado, response.IngestionStatus);
        Assert.Equal(1, await context.IncomingNachaFileIngestions.CountAsync());
        Assert.Equal(context.IncomingNachaFileIngestions.Single().Id, response.IngestionId);
        Assert.Equal(1, await context.IncomingNachaProcessingEvents.CountAsync(x => x.EventType == "DuplicateUploadAttempt"));
        parser.Verify(x => x.ParseAndSaveDetailedAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<NachaParseRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IngestAsync_DuplicateAfterSuccessfulReprocess_ReturnsEffectiveChildStateAndTotals()
    {
        using var context = BuildContext();
        var baseIngestion = BuildBaseIngestion();
        baseIngestion.IngestionStatus = IncomingNachaIngestionStatus.Fallido;
        baseIngestion.ParsingStatus = IncomingNachaParsingStatus.EnProceso;

        var child = BuildBaseIngestion();
        child.Id = Guid.NewGuid();
        child.IsReprocess = true;
        child.ParentIngestionId = baseIngestion.Id;
        child.UploadedAtUtc = baseIngestion.UploadedAtUtc.AddMinutes(1);
        child.IngestionStatus = IncomingNachaIngestionStatus.Completado;
        child.ParsingStatus = IncomingNachaParsingStatus.ExitosoConAdvertencias;
        child.CycleResolutionStatus = IncomingNachaCycleResolutionStatus.ResueltoInferido;
        child.ResolvedClearingHouseId = 7;
        child.ResolvedAchCycleId = "ACH-20260727-02";

        context.IncomingNachaFileIngestions.AddRange(baseIngestion, child);
        context.IncomingNachaFileProcessingResults.Add(new IncomingNachaFileProcessingResult
        {
            IncomingNachaFileIngestionId = child.Id,
            AttemptNumber = 1,
            OutcomeStatus = IncomingNachaProcessingOutcomeStatus.ExitosoConAdvertencias,
            TotalBatches = 40,
            TotalEntries = 41,
            TotalAddendas = 41,
            WarningCount = 5,
            FinishedAtUtc = DateTime.UtcNow,
            IsReprocessable = false
        });
        await context.SaveChangesAsync();

        var parser = new Mock<INachaParserService>(MockBehavior.Strict);
        var sut = new IncomingNachaIngestionAppService(
            context,
            Mock.Of<IIncomingNachaCycleResolver>(),
            parser.Object,
            Mock.Of<IIncomingNachaPostParseProcessor>(),
            BuildExternalPolicyMock().Object,
            Mock.Of<ILogger<IncomingNachaIngestionAppService>>());

        var response = await sut.IngestAsync(new IncomingNachaIngestionRequest
        {
            FileStream = BuildStream(),
            FileName = "base.ach",
            RequestedBy = "tester"
        });

        Assert.Equal(IncomingNachaIngestionStatus.Duplicado, response.IngestionStatus);
        Assert.Equal(child.Id, response.IngestionId);
        Assert.Equal(IncomingNachaParsingStatus.ExitosoConAdvertencias, response.ParsingStatus);
        Assert.Equal(40, response.TotalBatches);
        Assert.Equal(41, response.TotalEntries);
        Assert.Equal(41, response.TotalAddendas);
        var duplicateAudit = await context.IncomingNachaFileProcessingResults
            .SingleAsync(x => x.IncomingNachaFileIngestionId == baseIngestion.Id
                              && x.OutcomeStatus == IncomingNachaProcessingOutcomeStatus.Duplicado);
        Assert.False(duplicateAudit.IsReprocessable);
        parser.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IngestAsync_Blocks_WhenCycleIsAmbiguous()
    {
        using var context = BuildContext();
        var resolver = new Mock<IIncomingNachaCycleResolver>();
        resolver.Setup(x => x.ResolveAsync(It.IsAny<IncomingNachaCycleResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaCycleResolutionResult
            {
                IsResolved = false,
                IsAmbiguous = true,
                Status = IncomingNachaCycleResolutionStatus.Ambiguo,
                Errors = ["Múltiples candidatos"]
            });

        var sut = new IncomingNachaIngestionAppService(context, resolver.Object, Mock.Of<INachaParserService>(), Mock.Of<IIncomingNachaPostParseProcessor>(), BuildExternalPolicyMock().Object, Mock.Of<ILogger<IncomingNachaIngestionAppService>>());

        var response = await sut.IngestAsync(new IncomingNachaIngestionRequest { FileStream = BuildStream(), FileName = "amb.ach" });

        Assert.Equal(IncomingNachaIngestionStatus.Bloqueado, response.IngestionStatus);
        Assert.Equal(IncomingNachaCycleResolutionStatus.Ambiguo, response.CycleResolutionStatus);
    }

    [Fact]
    public async Task IngestAsync_PendingResolution_WhenNoCandidates()
    {
        using var context = BuildContext();
        var resolver = new Mock<IIncomingNachaCycleResolver>();
        resolver.Setup(x => x.ResolveAsync(It.IsAny<IncomingNachaCycleResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaCycleResolutionResult
            {
                IsResolved = false,
                IsAmbiguous = false,
                Status = IncomingNachaCycleResolutionStatus.NoResuelto,
                Errors = ["Sin ciclo candidato"]
            });

        var sut = new IncomingNachaIngestionAppService(context, resolver.Object, Mock.Of<INachaParserService>(), Mock.Of<IIncomingNachaPostParseProcessor>(), BuildExternalPolicyMock().Object, Mock.Of<ILogger<IncomingNachaIngestionAppService>>());
        var response = await sut.IngestAsync(new IncomingNachaIngestionRequest { FileStream = BuildStream(), FileName = "sin.ach" });

        Assert.Equal(IncomingNachaIngestionStatus.PendienteResolucion, response.IngestionStatus);
    }

    [Fact]
    public async Task IngestAsync_DifferentialWithoutSupportedProfile_BlocksBeforeParser_AndSecondLoadIsDuplicate()
    {
        using var context = BuildContext();
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 1,
            ClearingHouseId = 1,
            Code = "ACHCOL",
            Name = "ACH Colombia",
            OriginCode = "0001283"
        });
        await context.SaveChangesAsync();

        var cycleResolver = new Mock<IIncomingNachaCycleResolver>();
        cycleResolver.Setup(x => x.ResolveAsync(It.IsAny<IncomingNachaCycleResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaCycleResolutionResult
            {
                IsResolved = true,
                Status = IncomingNachaCycleResolutionStatus.ResueltoConfirmado,
                ClearingHouseId = 1,
                DetectedClearingHouseId = 1,
                AchCycleId = "ACH-20260723-01",
                OperationalDate = new DateTime(2026, 7, 23),
                Confidence = 1m,
                EvidenceJson = "{}"
            });

        var profileResolver = new Mock<INachaConfigResolver>();
        profileResolver.Setup(x => x.ResolveAsync(
                It.Is<NachaConfigResolutionRequest>(request =>
                    request.ClearingHouseCode == "ACH"
                    && request.FlowTypeCode == "RETORNO"
                    && request.DirectionCode == "ENTRADA"
                    && request.RequireHomologated),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NachaConfigResolutionResult
            {
                Success = false,
                SelectionStatus = NachaProfileSelectionStatus.ProfileNotFound,
                Warnings = ["Sin perfil diferencial sustentado."]
            });

        var parser = new Mock<INachaParserService>();
        var sut = new IncomingNachaIngestionAppService(
            context,
            cycleResolver.Object,
            parser.Object,
            Mock.Of<IIncomingNachaPostParseProcessor>(),
            BuildExternalPolicyMock().Object,
            Mock.Of<ILogger<IncomingNachaIngestionAppService>>(),
            profileResolver.Object);

        var first = await sut.IngestAsync(new IncomingNachaIngestionRequest
        {
            FileStream = BuildDifferentialStream(),
            FileName = "0001283.001.RET",
            RequestedBy = "tester"
        });
        var second = await sut.IngestAsync(new IncomingNachaIngestionRequest
        {
            FileStream = BuildDifferentialStream(),
            FileName = "0001283.001.RET",
            RequestedBy = "tester"
        });

        Assert.Equal(IncomingNachaIngestionStatus.Bloqueado, first.IngestionStatus);
        Assert.Equal(NachaProfileSelectionStatus.ProfileNotFound, first.ProfileSelectionStatus);
        Assert.Equal(IncomingNachaIngestionStatus.Duplicado, second.IngestionStatus);
        Assert.Equal(1, await context.IncomingNachaFileIngestions.CountAsync());
        Assert.Equal(1, await context.IncomingNachaProcessingEvents.CountAsync(x => x.EventType == "NachaProfileSelection"));
        Assert.Equal(1, await context.IncomingNachaProcessingEvents.CountAsync(x => x.EventType == "DuplicateUploadAttempt"));
        Assert.Equal(0, await context.IncomingNachaDispatchQueue.CountAsync());
        Assert.Equal(0, await context.IncomingNachaIntegrationExecution.CountAsync());
        parser.Verify(x => x.ParseAndSaveDetailedAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<NachaParseRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
        profileResolver.Verify(x => x.ResolveAsync(
            It.IsAny<NachaConfigResolutionRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestAsync_DifferentialWithSupportedProfile_PersistsSelectedIdentity()
    {
        using var context = BuildContext();
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 1,
            ClearingHouseId = 1,
            Code = "ACHCOL",
            Name = "ACH Colombia",
            OriginCode = "0001283"
        });
        await context.SaveChangesAsync();

        var cycleResolver = new Mock<IIncomingNachaCycleResolver>();
        cycleResolver.Setup(x => x.ResolveAsync(It.IsAny<IncomingNachaCycleResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaCycleResolutionResult
            {
                IsResolved = true,
                Status = IncomingNachaCycleResolutionStatus.ResueltoConfirmado,
                ClearingHouseId = 1,
                DetectedClearingHouseId = 1,
                AchCycleId = "ACH-20260723-01",
                OperationalDate = new DateTime(2026, 7, 23),
                Confidence = 1m,
                EvidenceJson = "{}"
            });

        var profileResolver = new Mock<INachaConfigResolver>();
        profileResolver.Setup(x => x.ResolveAsync(It.IsAny<NachaConfigResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NachaConfigResolutionResult
            {
                Success = true,
                SelectionStatus = NachaProfileSelectionStatus.ProfileSelected,
                Profile = new CfgProfile
                {
                    Id = 77,
                    ProfileCode = "OFFICIAL_ACH_ENTRADA_DEVOLUCION_V1_0",
                    VersionMajor = 1,
                    VersionMinor = 0
                }
            });

        var parser = new Mock<INachaParserService>();
        parser.Setup(x => x.ParseAndSaveDetailedAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<NachaParseRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NachaParseResult());
        var sut = new IncomingNachaIngestionAppService(
            context,
            cycleResolver.Object,
            parser.Object,
            Mock.Of<IIncomingNachaPostParseProcessor>(),
            BuildExternalPolicyMock().Object,
            Mock.Of<ILogger<IncomingNachaIngestionAppService>>(),
            profileResolver.Object);

        var response = await sut.IngestAsync(new IncomingNachaIngestionRequest
        {
            FileStream = BuildDifferentialStream(),
            FileName = "0001283.001.RET",
            RequestedBy = "tester"
        });

        var persisted = await context.IncomingNachaFileIngestions.SingleAsync();
        Assert.Equal(IncomingNachaIngestionStatus.Completado, response.IngestionStatus);
        Assert.Equal("OFFICIAL_ACH_ENTRADA_DEVOLUCION_V1_0", persisted.ProfileCode);
        Assert.Equal("1.0", persisted.ProfileVersion);
        parser.Verify(x => x.ParseAndSaveDetailedAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.Is<NachaParseRequest>(request => request.SelectedProfileId == 77),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestAsync_SameNameWithDifferentBytes_PersistsIndependentConflictAudit()
    {
        using var context = BuildContext();
        var cycleResolver = new Mock<IIncomingNachaCycleResolver>();
        cycleResolver.Setup(x => x.ResolveAsync(It.IsAny<IncomingNachaCycleResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaCycleResolutionResult
            {
                IsResolved = true,
                Status = IncomingNachaCycleResolutionStatus.ResueltoInferido,
                ClearingHouseId = 1,
                OperationalDate = new DateTime(2026, 7, 23),
                Confidence = 1m,
                EvidenceJson = "{}"
            });
        var parser = new Mock<INachaParserService>();
        parser.Setup(x => x.ParseAndSaveDetailedAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<NachaParseRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NachaParseResult());
        var sut = new IncomingNachaIngestionAppService(
            context,
            cycleResolver.Object,
            parser.Object,
            Mock.Of<IIncomingNachaPostParseProcessor>(),
            BuildExternalPolicyMock().Object,
            Mock.Of<ILogger<IncomingNachaIngestionAppService>>());

        var first = await sut.IngestAsync(new IncomingNachaIngestionRequest
        {
            FileStream = BuildStream('1'),
            FileName = "mismo-nombre.ach"
        });
        var second = await sut.IngestAsync(new IncomingNachaIngestionRequest
        {
            FileStream = BuildStream('2'),
            FileName = "mismo-nombre.ach"
        });

        Assert.Equal(IncomingNachaIngestionStatus.Completado, first.IngestionStatus);
        Assert.Equal(IncomingNachaIngestionStatus.Completado, second.IngestionStatus);
        Assert.Equal(2, await context.IncomingNachaFileIngestions.CountAsync());
        Assert.Equal(1, await context.IncomingNachaProcessingEvents.CountAsync(x => x.EventType == "FileNameContentConflict"));
        parser.Verify(x => x.ParseAndSaveDetailedAsync(
            It.IsAny<Stream>(),
            "mismo-nombre.ach",
            It.IsAny<NachaParseRequest>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task IngestAsync_ForceReprocess_RequiresBaseIngestion()
    {
        using var context = BuildContext();
        var sut = new IncomingNachaIngestionAppService(context, Mock.Of<IIncomingNachaCycleResolver>(), Mock.Of<INachaParserService>(), Mock.Of<IIncomingNachaPostParseProcessor>(), BuildExternalPolicyMock().Object, Mock.Of<ILogger<IncomingNachaIngestionAppService>>());

        await Assert.ThrowsAsync<ArgumentException>(() => sut.IngestAsync(new IncomingNachaIngestionRequest
        {
            FileStream = BuildStream(),
            FileName = "reprocess.ach",
            ForceReprocess = true
        }));
    }

    [Fact]
    public async Task IngestAsync_ForceReprocess_UsesParentAndDoesNotMarkAsDuplicate()
    {
        using var context = BuildContext();
        var baseIngestion = new IncomingNachaFileIngestion
        {
            FileName = "base.ach",
            FileHashSha256 = "0f883fdd6d70f3a190d322674e647f439f4e839c8e24a6a1d5e775b98eddf28e",
            FileSize = 106,
            ContentType = "text/plain",
            UploadedBy = "seed",
            CorrelationId = "seed",
            Notes = "base",
            IsReprocess = false
        };
        context.IncomingNachaFileIngestions.Add(baseIngestion);
        await context.SaveChangesAsync();

        var resolver = new Mock<IIncomingNachaCycleResolver>();
        resolver.Setup(x => x.ResolveAsync(It.IsAny<IncomingNachaCycleResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaCycleResolutionResult
            {
                IsResolved = true,
                Status = IncomingNachaCycleResolutionStatus.ResueltoInferido,
                ClearingHouseId = 1,
                DetectedClearingHouseId = 1,
                AchCycleId = "ACH-20260417-01",
                OperationalDate = new DateTime(2026, 04, 17),
                Confidence = 0.95m,
                EvidenceJson = "{}"
            });

        var parser = new Mock<INachaParserService>();
        parser.Setup(x => x.ParseAndSaveDetailedAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<NachaParseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NachaParseResult());

        var sut = new IncomingNachaIngestionAppService(context, resolver.Object, parser.Object, Mock.Of<IIncomingNachaPostParseProcessor>(), BuildExternalPolicyMock().Object, Mock.Of<ILogger<IncomingNachaIngestionAppService>>());
        var response = await sut.IngestAsync(new IncomingNachaIngestionRequest
        {
            FileStream = BuildStream(),
            FileName = "reprocess.ach",
            ForceReprocess = true,
            ParentIngestionId = baseIngestion.Id
        });

        var created = await context.IncomingNachaFileIngestions
            .OrderByDescending(x => x.UploadedAtUtc)
            .FirstAsync();

        Assert.True(created.IsReprocess);
        Assert.Equal(baseIngestion.Id, created.ParentIngestionId);
        Assert.NotEqual(IncomingNachaIngestionStatus.Duplicado, response.IngestionStatus);
    }

    [Fact]
    public async Task IngestAsync_ForceReprocess_AllowsAnotherAttemptAfterReprocessableChildFailure()
    {
        using var context = BuildContext();
        var baseIngestion = BuildBaseIngestion();
        var failedChild = BuildBaseIngestion();
        failedChild.Id = Guid.NewGuid();
        failedChild.IsReprocess = true;
        failedChild.ParentIngestionId = baseIngestion.Id;
        failedChild.UploadedAtUtc = baseIngestion.UploadedAtUtc.AddMinutes(1);
        failedChild.IngestionStatus = IncomingNachaIngestionStatus.Fallido;
        failedChild.ParsingStatus = IncomingNachaParsingStatus.FallidoReprocesable;
        context.IncomingNachaFileIngestions.AddRange(baseIngestion, failedChild);
        context.IncomingNachaFileProcessingResults.Add(new IncomingNachaFileProcessingResult
        {
            IncomingNachaFileIngestionId = failedChild.Id,
            AttemptNumber = 1,
            IsReprocessable = true,
            OutcomeStatus = IncomingNachaProcessingOutcomeStatus.Fallido,
            FinishedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var resolver = new Mock<IIncomingNachaCycleResolver>();
        resolver.Setup(x => x.ResolveAsync(It.IsAny<IncomingNachaCycleResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaCycleResolutionResult
            {
                IsResolved = true,
                Status = IncomingNachaCycleResolutionStatus.ResueltoInferido,
                ClearingHouseId = 1,
                DetectedClearingHouseId = 1,
                AchCycleId = "ACH-20260417-01",
                OperationalDate = new DateTime(2026, 4, 17),
                Confidence = 0.95m,
                EvidenceJson = "{}"
            });
        var parser = new Mock<INachaParserService>();
        parser.Setup(x => x.ParseAndSaveDetailedAsync(
                It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<NachaParseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NachaParseResult());
        var sut = new IncomingNachaIngestionAppService(
            context,
            resolver.Object,
            parser.Object,
            Mock.Of<IIncomingNachaPostParseProcessor>(),
            BuildExternalPolicyMock().Object,
            Mock.Of<ILogger<IncomingNachaIngestionAppService>>());

        var response = await sut.IngestAsync(new IncomingNachaIngestionRequest
        {
            FileStream = BuildStream(),
            FileName = "reprocess.ach",
            ForceReprocess = true,
            ParentIngestionId = baseIngestion.Id
        });

        Assert.NotEqual(IncomingNachaIngestionStatus.Duplicado, response.IngestionStatus);
        var attempts = await context.IncomingNachaFileIngestions
            .Where(x => x.FileHashSha256 == baseIngestion.FileHashSha256 && x.FileSize == baseIngestion.FileSize)
            .OrderBy(x => x.UploadedAtUtc)
            .ToListAsync();
        Assert.Equal(3, attempts.Count);
        Assert.Equal(baseIngestion.Id, attempts[^1].ParentIngestionId);
        Assert.True(attempts[^1].IsReprocess);
    }

    [Fact]
    public async Task IngestAsync_ForceReprocess_RejectsNonReprocessableParent()
    {
        using var context = BuildContext();
        var baseIngestion = BuildBaseIngestion();
        context.IncomingNachaFileIngestions.Add(baseIngestion);
        context.IncomingNachaFileProcessingResults.Add(new IncomingNachaFileProcessingResult
        {
            IncomingNachaFileIngestionId = baseIngestion.Id,
            AttemptNumber = 1,
            IsReprocessable = false,
            OutcomeStatus = IncomingNachaProcessingOutcomeStatus.Exitoso,
            FinishedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        var sut = new IncomingNachaIngestionAppService(
            context,
            Mock.Of<IIncomingNachaCycleResolver>(),
            Mock.Of<INachaParserService>(),
            Mock.Of<IIncomingNachaPostParseProcessor>(),
            BuildExternalPolicyMock().Object,
            Mock.Of<ILogger<IncomingNachaIngestionAppService>>());

        var act = () => sut.IngestAsync(new IncomingNachaIngestionRequest
        {
            FileStream = BuildStream(),
            FileName = "reprocess.ach",
            ForceReprocess = true,
            ParentIngestionId = baseIngestion.Id
        });

        var exception = await Assert.ThrowsAsync<ArgumentException>(act);
        Assert.Contains("no está autorizada", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IngestAsync_ForceReprocess_RejectsReprocessChildAsParent()
    {
        using var context = BuildContext();
        var baseIngestion = BuildBaseIngestion();
        var child = BuildBaseIngestion();
        child.Id = Guid.NewGuid();
        child.IsReprocess = true;
        child.ParentIngestionId = baseIngestion.Id;
        child.UploadedAtUtc = baseIngestion.UploadedAtUtc.AddMinutes(1);
        context.IncomingNachaFileIngestions.AddRange(baseIngestion, child);
        await context.SaveChangesAsync();
        var sut = new IncomingNachaIngestionAppService(
            context,
            Mock.Of<IIncomingNachaCycleResolver>(),
            Mock.Of<INachaParserService>(),
            Mock.Of<IIncomingNachaPostParseProcessor>(),
            BuildExternalPolicyMock().Object,
            Mock.Of<ILogger<IncomingNachaIngestionAppService>>());

        var act = () => sut.IngestAsync(new IncomingNachaIngestionRequest
        {
            FileStream = BuildStream(),
            FileName = "reprocess.ach",
            ForceReprocess = true,
            ParentIngestionId = child.Id
        });

        var exception = await Assert.ThrowsAsync<ArgumentException>(act);
        Assert.Contains("canónica original", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IngestAsync_ParserCancellation_PersistsTerminalFailureWithoutRequestToken()
    {
        using var context = BuildContext();
        using var cancellation = new CancellationTokenSource();
        var resolver = new Mock<IIncomingNachaCycleResolver>();
        resolver.Setup(x => x.ResolveAsync(It.IsAny<IncomingNachaCycleResolutionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaCycleResolutionResult
            {
                IsResolved = true,
                Status = IncomingNachaCycleResolutionStatus.ResueltoInferido,
                ClearingHouseId = 1,
                DetectedClearingHouseId = 1,
                AchCycleId = "ACH-20260417-01",
                OperationalDate = new DateTime(2026, 04, 17),
                Confidence = 0.95m,
                EvidenceJson = "{}"
            });
        var parser = new Mock<INachaParserService>();
        parser.Setup(x => x.ParseAndSaveDetailedAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<NachaParseRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns((Stream _, string _, NachaParseRequest _, CancellationToken token) =>
            {
                cancellation.Cancel();
                return Task.FromException<NachaParseResult>(new OperationCanceledException(token));
            });
        var sut = new IncomingNachaIngestionAppService(
            context,
            resolver.Object,
            parser.Object,
            Mock.Of<IIncomingNachaPostParseProcessor>(),
            BuildExternalPolicyMock().Object,
            Mock.Of<ILogger<IncomingNachaIngestionAppService>>());

        var response = await sut.IngestAsync(new IncomingNachaIngestionRequest
        {
            FileStream = BuildStream(),
            FileName = "cancelled-parser.ach"
        }, cancellation.Token);

        Assert.Equal(IncomingNachaIngestionStatus.Fallido, response.IngestionStatus);
        Assert.Equal(IncomingNachaParsingStatus.FallidoReprocesable, response.ParsingStatus);
        var persisted = await context.IncomingNachaFileProcessingResults.SingleAsync();
        Assert.Equal(IncomingNachaProcessingOutcomeStatus.Fallido, persisted.OutcomeStatus);
        Assert.Equal("Parser", persisted.FailureStage);
        Assert.NotNull(persisted.FinishedAtUtc);
    }

    private static IncomingNachaFileIngestion BuildBaseIngestion()
        => new()
        {
            FileName = "base.ach",
            FileHashSha256 = "0f883fdd6d70f3a190d322674e647f439f4e839c8e24a6a1d5e775b98eddf28e",
            FileSize = 106,
            ContentType = "text/plain",
            UploadedBy = "seed",
            CorrelationId = "seed",
            Notes = "base",
            IsReprocess = false
        };

    private static AchDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AchDbContext(options);
    }

    private static MemoryStream BuildStream(char recordType = '1')
    {
        return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(new string(recordType, 106)));
    }

    private static MemoryStream BuildDifferentialStream()
    {
        var records = new[]
        {
            "1" + new string(' ', 105),
            "7" + "99" + new string(' ', 103)
        };
        return new MemoryStream(System.Text.Encoding.ASCII.GetBytes(string.Concat(records)));
    }

    private static Mock<IExternalFileNamePolicy> BuildExternalPolicyMock()
    {
        var mock = new Mock<IExternalFileNamePolicy>();
        mock.Setup(x => x.GenerateExternalNameAsync(It.IsAny<ExternalFileNameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalFileNameContext context, CancellationToken _) => new ExternalFileNamePolicyResult
            {
                ExternalFileName = context.ProvidedExternalFileName ?? context.InternalFileName ?? "incoming.txt",
                Validation = new ExternalFileNameValidationResult { Disposition = ExternalFileValidationDisposition.Passed },
                CorrelationEvidence = new ExternalFileNameCorrelationEvidence(),
                Components = new ExternalFileNameComponents { FullName = context.ProvidedExternalFileName ?? "incoming.txt" }
            });
        return mock;
    }
}
