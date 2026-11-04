using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColombiaApprovedReturnGenerationI05Tests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 15, 25, 0, TimeSpan.Zero);

    [Fact]
    public async Task ApprovedCandidateGeneratesOneFileWithOfficialNameAndNoTransport()
    {
        await using var db = BuildDb();
        Seed(db);
        var generator = WorkingGenerator(db);
        var service = NewService(db, generator.Object);

        var candidate = Assert.Single(await service.ListAsync(50));
        Assert.False(candidate.CanApprove);
        Assert.True(candidate.CanGenerateFile);

        var file = await service.GenerateApprovedFileAsync(candidate.Id);

        Assert.Equal("00001234.RET", file.FileName);
        Assert.Equal(1, file.TotalReturns);
        Assert.Equal("ARCHIVO_GENERADO_SIN_ENVIO", db.AchColombiaReturnPreparations.Single().Status);
        Assert.Single(db.AchReturnsGenerated);
        generator.Verify(x => x.GenerateReturnsFileAsync(It.Is<GenerateReturnsFileRequest>(request =>
            request.CycleId == "C1" && request.ReturnCycleId == "C4"
            && request.Items.Count == 1 && request.Items[0].TransactionId == 77
            && request.Items[0].ReturnReasonCode == "R02"), It.IsAny<CancellationToken>()), Times.Once);
        Assert.False(Assert.Single(await service.ListAsync(50)).CanGenerateFile);
        await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(
            () => service.GenerateApprovedFileAsync(candidate.Id));
    }

    [Fact]
    public async Task RequiresApprovalBeforeGenerating()
    {
        await using var db = BuildDb();
        Seed(db);
        db.AchColombiaReturnPreparations.Single().Status = "PREPARADA_SIN_ENVIO";
        await db.SaveChangesAsync();
        var generator = new Mock<IAchReturnsService>(MockBehavior.Strict);
        await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(
            () => NewService(db, generator.Object).GenerateApprovedFileAsync(1));
        generator.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("DESACTIVADO")]
    [InlineData("NO_VALIDO")]
    public async Task DisabledOrUnrecognizedModeNeverCallsGenerator(string mode)
    {
        await using var db = BuildDb();
        Seed(db);
        db.ClearingHouseConfigs.Single().AutomaticReturnMode = mode;
        await db.SaveChangesAsync();
        var generator = new Mock<IAchReturnsService>(MockBehavior.Strict);
        await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(
            () => NewService(db, generator.Object).GenerateApprovedFileAsync(1));
        generator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CenitCandidateIsNeverGeneratedByAchColombiaEndpoint()
    {
        await using var db = BuildDb();
        Seed(db, "CENIT");
        var generator = new Mock<IAchReturnsService>(MockBehavior.Strict);
        await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(
            () => NewService(db, generator.Object).GenerateApprovedFileAsync(1));
        generator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExistingManualFileBlocksApprovedCandidate()
    {
        await using var db = BuildDb();
        Seed(db);
        db.AchReturnsGenerated.Add(new AchReturnGenerated
        {
            OriginalTransactionId = 77, ReturnCycleId = "C4", ReturnReasonCode = "R02",
            FileName = "MANUAL.RET", OriginalSequenceNumber = "000000000000077",
            NewSequenceNumber = "000000000000099"
        });
        await db.SaveChangesAsync();
        var generator = new Mock<IAchReturnsService>(MockBehavior.Strict);
        await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(
            () => NewService(db, generator.Object).GenerateApprovedFileAsync(1));
        generator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExpiredWindowOrChangedCycleBlocksGeneration()
    {
        await using var db = BuildDb();
        Seed(db);
        db.AchColombiaReturnPreparations.Single().SuggestedReturnCycleId = "C5";
        await db.SaveChangesAsync();
        var generator = new Mock<IAchReturnsService>(MockBehavior.Strict);
        await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(
            () => NewService(db, generator.Object).GenerateApprovedFileAsync(1));
        generator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task R10WithoutVerifiableBasisBlocksGeneration()
    {
        await using var db = BuildDb();
        Seed(db);
        db.AchColombiaReturnPreparations.Single().ReasonCode = "R10";
        await db.SaveChangesAsync();
        var generator = new Mock<IAchReturnsService>(MockBehavior.Strict);
        await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(
            () => NewService(db, generator.Object).GenerateApprovedFileAsync(1));
        generator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GeneratorFailureDoesNotMarkCandidateAsFileGenerated()
    {
        await using var db = BuildDb();
        Seed(db);
        var generator = new Mock<IAchReturnsService>(MockBehavior.Strict);
        generator.Setup(x => x.GenerateReturnsFileAsync(It.IsAny<GenerateReturnsFileRequest>(),
            It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("El generador falló"));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => NewService(db, generator.Object).GenerateApprovedFileAsync(1));
        Assert.Equal("APROBADA_SIN_ENVIO", db.AchColombiaReturnPreparations.Single().Status);
        Assert.Empty(db.AchReturnsGenerated);
    }

    [Fact]
    public async Task GeneratorResponseWithoutPersistedReturnIsNotAccepted()
    {
        await using var db = BuildDb();
        Seed(db);
        var generator = new Mock<IAchReturnsService>(MockBehavior.Strict);
        generator.Setup(x => x.GenerateReturnsFileAsync(It.IsAny<GenerateReturnsFileRequest>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(new GenerateReturnsFileResponse(
            "00001234.RET", "text/plain", Encoding.ASCII.GetBytes("ARCHIVO"), 1, 1));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => NewService(db, generator.Object).GenerateApprovedFileAsync(1));
        Assert.Equal("APROBADA_SIN_ENVIO", db.AchColombiaReturnPreparations.Single().Status);
    }

    [Fact]
    public async Task APreviouslyDiscardedCandidateCannotGenerateAFile()
    {
        await using var db = BuildDb();
        Seed(db);
        db.AchColombiaReturnPreparations.Single().Status = "DESCARTADA";
        await db.SaveChangesAsync();
        var generator = new Mock<IAchReturnsService>(MockBehavior.Strict);
        await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(
            () => NewService(db, generator.Object).GenerateApprovedFileAsync(1));
        generator.VerifyNoOtherCalls();
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

    private static AchColombiaReturnReviewService NewService(AchDbContext db, IAchReturnsService returns) =>
        new(db, new FixedClock(), returns);

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
            Id = 41, ClearingHouseId = 41, AutomaticReturnMode = "CON_APROBACION"
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
            Status = "APROBADA_SIN_ENVIO", PreparedAtUtc = Now.UtcDateTime
        });
        db.SaveChanges();
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
