using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed partial class AchColombiaAutomaticReturnI06Tests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 15, 25, 0, TimeSpan.Zero);

    [Fact]
    public async Task DeploymentGateDefaultsOffAndBlocksAutomaticGeneration()
    {
        await using var db = BuildDb();
        Seed(db);
        var generator = new Mock<IAchReturnsService>(MockBehavior.Strict);
        await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(() =>
            NewService(db, generator.Object, deploymentEnabled: false).GenerateAutomaticallyAsync(1));
        Assert.Equal("PREPARADA_SIN_ENVIO", db.AchColombiaReturnPreparations.Single().Status);
        generator.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("CON_APROBACION")]
    [InlineData("DESACTIVADO")]
    public async Task AutomaticGenerationRequiresAutomaticMode(string mode)
    {
        await using var db = BuildDb();
        Seed(db);
        db.ClearingHouseConfigs.Single().AutomaticReturnMode = mode;
        await db.SaveChangesAsync();
        var generator = new Mock<IAchReturnsService>(MockBehavior.Strict);
        await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(() =>
            NewService(db, generator.Object).GenerateAutomaticallyAsync(1));
        generator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PreparedCandidateGeneratesOnlyOnceWithoutManualApprovalOrTransmission()
    {
        await using var db = BuildDb();
        Seed(db);
        var generator = WorkingGenerator(db);
        var service = NewService(db, generator.Object, artifact: CompatibleArtifact().Object);
        var file = await service.GenerateAutomaticallyAsync(1);
        Assert.Equal("00001234.RET", file.FileName);
        Assert.Equal("ARCHIVO_GENERADO_SIN_ENVIO", db.AchColombiaReturnPreparations.Single().Status);
        Assert.Single(db.AchReturnsGenerated);
        await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(() =>
            service.GenerateAutomaticallyAsync(1));
        generator.Verify(x => x.GenerateReturnsFileAsync(It.IsAny<GenerateReturnsFileRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ManualGenerationStillRequiresExplicitApproval()
    {
        await using var db = BuildDb();
        Seed(db);
        var generator = new Mock<IAchReturnsService>(MockBehavior.Strict);
        await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(() =>
            NewService(db, generator.Object).GenerateApprovedFileAsync(1));
        generator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CenitCannotEnterAutomaticAchGeneration()
    {
        await using var db = BuildDb();
        Seed(db, "CENIT");
        var generator = new Mock<IAchReturnsService>(MockBehavior.Strict);
        await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(() =>
            NewService(db, generator.Object).GenerateAutomaticallyAsync(1));
        generator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RunnerStopsWhenModeIsNotAutomatic()
    {
        await using var db = BuildDb();
        Seed(db);
        db.ClearingHouseConfigs.Single().AutomaticReturnMode = "CON_APROBACION";
        await db.SaveChangesAsync();
        var service = new Mock<IAchColombiaReturnReviewService>(MockBehavior.Strict);
        var runner = new AchColombiaAutomaticReturnFileRunner(db, service.Object);
        Assert.Equal(0, await runner.RunAsync(10, CancellationToken.None));
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RunnerProcessesPersistedPreparedCandidateOnce()
    {
        await using var db = BuildDb();
        Seed(db);
        var generator = WorkingGenerator(db);
        var service = NewService(db, generator.Object, artifact: CompatibleArtifact().Object);
        var runner = new AchColombiaAutomaticReturnFileRunner(db, service);
        Assert.Equal(1, await runner.RunAsync(10, CancellationToken.None));
        Assert.Equal(0, await runner.RunAsync(10, CancellationToken.None));
        Assert.Equal("ARCHIVO_GENERADO_SIN_ENVIO", db.AchColombiaReturnPreparations.Single().Status);
    }

    [Fact]
    public async Task ExistingFileCanBeRecoveredWithoutRegenerationOrNewSequence()
    {
        await using var db = BuildDb();
        Seed(db);
        SeedGeneratedRecovery(db);
        var generated = new Mock<IAchReturnsService>(MockBehavior.Strict);
        var artifacts = new Mock<IAchOutboundReturnArtifactService>(MockBehavior.Strict);
        artifacts.Setup(x => x.BuildAsync("00001234.RET", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchOutboundReturnArtifact("00001234.RET", Encoding.ASCII.GetBytes("ARCHIVO"),
                1, 1, "C4", 41, new[] { 77 }, "HASH"));
        var result = await NewService(db, generated.Object, artifact: artifacts.Object)
            .RecoverGeneratedFileAsync(1);
        Assert.Equal("00001234.RET", result.FileName);
        Assert.Equal("ARCHIVO", Encoding.ASCII.GetString(result.Content));
        Assert.Single(db.AchReturnsGenerated);
        generated.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SettingsExposeConnectionOnlyWithBothGatesEnabled()
    {
        await using var db = BuildDb();
        Seed(db);
        Assert.False((await new AchColombiaAutomaticReturnSettingsService(db).GetAsync()).ProcessingConnected);
        Assert.True((await new AchColombiaAutomaticReturnSettingsService(db, DeployConfig(true)).GetAsync()).ProcessingConnected);
        db.ClearingHouseConfigs.Single().AutomaticReturnMode = "DESACTIVADO";
        await db.SaveChangesAsync();
        Assert.False((await new AchColombiaAutomaticReturnSettingsService(db, DeployConfig(true)).GetAsync()).ProcessingConnected);
    }

    // I07: reutilizado por pruebas de integridad de recuperación. El evento replica
    // los campos que persiste AchReturnAuditMonitoringComponent, sin DB externa.
    private static void SeedGeneratedRecovery(AchDbContext db, string fileName = "00001234.RET",
        string content = "ARCHIVO", string? rawPayload = null, bool withAudit = true)
    {
        db.AchColombiaReturnPreparations.Single().Status = "ARCHIVO_GENERADO_SIN_ENVIO";
        db.AchReturnsGenerated.Add(new AchReturnGenerated
        {
            OriginalTransactionId = 77, ReturnCycleId = "C4", ReturnReasonCode = "R02",
            FileName = fileName, NewSequenceNumber = "000000000000078",
            OriginalSequenceNumber = "000000000000077"
        });
        if (withAudit)
        {
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)))
                .ToLowerInvariant();
            db.AchTransactionStateEvents.Add(new AchTransactionStateEvent
            {
                AchTransactionId = 77, ToState = AchTransferStateEnum.ReturnedByEpr,
                ClearingHouseId = 41, IdempotencyKey = "outbound-return-v1:77",
                PayloadJson = rawPayload ?? JsonSerializer.Serialize(new
                {
                    eventType = "ReturnFileGenerated", originalTransactionId = 77,
                    clearingHouseId = 41, fileName, returnCycleId = "C4",
                    returnReasonCode = "R02", recordCount = 1, returnCount = 1,
                    transmissionStatus = "GeneratedNotTransmitted", contentSha256 = hash
                })
            });
        }
        db.SaveChanges();
    }

    private static Mock<IAchReturnsService> WorkingGenerator(AchDbContext db)
    {
        var mock = new Mock<IAchReturnsService>(MockBehavior.Strict);
        mock.Setup(x => x.GenerateReturnsFileAsync(It.IsAny<GenerateReturnsFileRequest>(),
            It.IsAny<CancellationToken>())).Returns((GenerateReturnsFileRequest request, CancellationToken _) =>
        {
            var selection = Assert.Single(request.Items);
            db.AchReturnsGenerated.Add(new AchReturnGenerated
            {
                OriginalTransactionId = selection.TransactionId, ReturnCycleId = request.ReturnCycleId!,
                ReturnReasonCode = selection.ReturnReasonCode, FileName = "00001234.RET",
                OriginalSequenceNumber = "000000000000077", NewSequenceNumber = "000000000000078"
            });
            db.SaveChanges();
            return Task.FromResult(new GenerateReturnsFileResponse(
                "00001234.RET", "text/plain", Encoding.ASCII.GetBytes("ARCHIVO"), 1, 1));
        });
        return mock;
    }

    // I08: el artefacto de comprobación debe ser el mismo archivo/bytes que
    // devuelve el generador simulado. No simula transmisión ni aceptación.
    private static Mock<IAchOutboundReturnArtifactService> CompatibleArtifact()
    {
        var artifact = new Mock<IAchOutboundReturnArtifactService>(MockBehavior.Strict);
        artifact.Setup(x => x.BuildAsync("00001234.RET", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchOutboundReturnArtifact("00001234.RET",
                Encoding.ASCII.GetBytes("ARCHIVO"), 1, 1, "C4", 41,
                new[] { 77 }, "HUELLA-DECLARADA-NO-UTILIZADA"));
        return artifact;
    }

    private static AchColombiaReturnReviewService NewService(AchDbContext db, IAchReturnsService returns,
        bool deploymentEnabled = true, IAchOutboundReturnArtifactService? artifact = null) =>
        new(db, new FixedClock(), returns, DeployConfig(deploymentEnabled), artifact);

    private static IConfiguration DeployConfig(bool enabled) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AchCol:EnableAutomaticReturnFileGeneration"] = enabled.ToString()
        }).Build();

    private static AchDbContext BuildDb() => new(
        new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase($"i05-ach-{Guid.NewGuid():N}").Options);

    private static void Seed(AchDbContext db, string camera = "ACHCOL")
    {
        db.ClearingHouses.Add(new ClearingHouse
        {
            Id = 41, Code = camera, Name = camera, OriginCode = "00000000", ClearingHouseId = 41
        });
        db.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = 41, ClearingHouseId = 41, AutomaticReturnMode = "AUTOMATICO"
        });
        for (var i = 1; i <= 5; i++)
        {
            db.AchCycles.Add(new AchCycle
            {
                Id = $"C{i}", ClearingHouseId = 41, CycleName = $"Ciclo {i}",
                ProcessingDate = new DateTime(2026, 11, 2),
                StartTime = TimeSpan.FromHours(6 + i), EndTime = TimeSpan.FromHours(7 + i),
                OperationalStatus = i <= 2 ? AchCycleOperationalStatus.Closed : AchCycleOperationalStatus.Scheduled,
                ClearingHouseCycleConfig = new ClearingHouseCycleConfig
                {
                    Id = 100 + i, ClearingHouseId = 41, CycleName = $"Ciclo {i}", AllowsReturn = true
                }
            });
        }
        db.AchTransactions.Add(new AchTransaction
        {
            Id = 77, AchCycleId = "C1", Type = TransactionTypeEnum.Credit,
            State = AchTransferStateEnum.Pending, EffectiveEntryDate = new DateTime(2026, 11, 2),
            Reference = "REF-77", Amount = 500000m,
            SourceAccountNumber = "123456", DestinationAccountNumber = "654321"
        });
        db.ReturnReasons.Add(new ReturnReason
        {
            Code = "R02", Description = "Cuenta cerrada", IsForReturn = true
        });
        db.Set<AchReturnCode>().Add(new AchReturnCode
        {
            Id = 8, ClearingHouseId = 41, Code = "R02", IsActive = true, AppliesToCredit = true
        });
        var evt = new IncomingNachaProcessingEvent
        {
            Id = Guid.NewGuid(), AchTransactionId = 77, IncomingNachaFileIngestionId = Guid.NewGuid(),
            EventType = AchColombiaApplicationFailureDetector.DetectedEvent,
            RaisedBy = "achcol.application-failure.detector", EventStatus = "Detectada",
            OccurredAtUtc = Now.UtcDateTime, Message = "Rechazo funcional",
            EvidenceJson = JsonSerializer.Serialize(new
            {
                camera = "ACHCOL", origin = "CORE_FUNCIONAL", transactionId = 77,
                causeVerified = true, returnGenerated = false, returnSent = false,
                suggestedReturnReason = "R02", responseCode = "R02"
            })
        };
        db.IncomingNachaProcessingEvents.Add(evt);
        db.AchColombiaReturnPreparations.Add(new AchColombiaReturnPreparation
        {
            Id = 1, OriginalTransactionId = 77, SourceEventId = evt.Id,
            OriginalCycleId = "C1", SuggestedReturnCycleId = "C4", ReasonCode = "R02",
            Status = "PREPARADA_SIN_ENVIO", PreparedAtUtc = Now.UtcDateTime
        });
        db.SaveChanges();
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
