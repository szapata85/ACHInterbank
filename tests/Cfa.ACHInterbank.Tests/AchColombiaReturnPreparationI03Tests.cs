using System.Text.Json;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColombiaReturnPreparationI03Tests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 11, 2, 15, 25, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(TransactionTypeEnum.Credit)]
    [InlineData(TransactionTypeEnum.Debit)]
    [InlineData(TransactionTypeEnum.Prenotification)]
    public async Task PreparedCandidateDoesNotCreateReturnFileOrMoveOriginal(TransactionTypeEnum type)
    {
        await using var db = NewDb();
        Seed(db, "ACHCOL", "CON_APROBACION", type, verified: true);
        var service = new AchColombiaReturnPreparationService(db, new FixedClock(FixedNow));
        Assert.True(await service.PrepareFromDetectedEventAsync(77, CancellationToken.None));
        Assert.False(await service.PrepareFromDetectedEventAsync(77, CancellationToken.None));
        var row = Assert.Single(db.AchColombiaReturnPreparations);
        Assert.Equal("R02", row.ReasonCode);
        Assert.Equal(AchColombiaReturnPreparationService.Prepared, row.Status);
        Assert.Equal("C4", row.SuggestedReturnCycleId);
        Assert.Empty(db.AchReturnsGenerated);
        Assert.Equal(AchTransferStateEnum.Pending, (await db.AchTransactions.SingleAsync()).State);
    }

    [Theory]
    [InlineData("DESACTIVADO")]
    [InlineData("OTRO")]
    public async Task DisabledOrInvalidModeNeverPrepares(string mode)
    {
        await using var db = NewDb();
        Seed(db, "ACHCOL", mode, TransactionTypeEnum.Credit, verified: true);
        Assert.False(await new AchColombiaReturnPreparationService(db, new FixedClock(FixedNow))
            .PrepareFromDetectedEventAsync(77, CancellationToken.None));
        Assert.Empty(db.AchColombiaReturnPreparations);
    }

    [Fact]
    public async Task AutomaticModeOnlyPreparesItDoesNotSend()
    {
        await using var db = NewDb();
        Seed(db, "ACHCOL", "AUTOMATICO", TransactionTypeEnum.Credit, verified: true);
        var service = new AchColombiaReturnPreparationService(db, new FixedClock(FixedNow));
        Assert.Equal(1, await service.PreparePendingAsync(20, CancellationToken.None));
        Assert.Single(db.AchColombiaReturnPreparations);
        Assert.Empty(db.AchReturnsGenerated);
    }

    [Fact]
    public async Task ReviewEventNeverPreparesOrInventsReason()
    {
        await using var db = NewDb();
        Seed(db, "ACHCOL", "AUTOMATICO", TransactionTypeEnum.Credit, verified: false);
        Assert.False(await new AchColombiaReturnPreparationService(db, new FixedClock(FixedNow))
            .PrepareFromDetectedEventAsync(77, CancellationToken.None));
    }

    [Fact]
    public async Task CenitDoesNotPrepareEvenWhenModeIsAutomatic()
    {
        await using var db = NewDb();
        Seed(db, "CENIT", "AUTOMATICO", TransactionTypeEnum.Credit, verified: true);
        var service = new AchColombiaReturnPreparationService(db, new FixedClock(FixedNow));
        Assert.False(await service.PrepareFromDetectedEventAsync(77, CancellationToken.None));
        Assert.Equal(0, await service.PreparePendingAsync(20, CancellationToken.None));
        Assert.Empty(db.AchColombiaReturnPreparations);
    }

    [Fact]
    public async Task AlreadyGeneratedManualReturnPreventsPreparation()
    {
        await using var db = NewDb();
        Seed(db, "ACHCOL", "CON_APROBACION", TransactionTypeEnum.Credit, verified: true);
        db.AchReturnsGenerated.Add(new AchReturnGenerated
        {
            OriginalTransactionId = 77, ReturnCycleId = "C3", ReturnReasonCode = "R02",
            FileName = "manual.nacha", NewSequenceNumber = "000000000000123",
            OriginalSequenceNumber = "000000000000077", ReceiverEntityCode = "00000001",
            OriginatorEntityCode = "00000002"
        });
        await db.SaveChangesAsync();
        Assert.False(await new AchColombiaReturnPreparationService(db, new FixedClock(FixedNow))
            .PrepareFromDetectedEventAsync(77, CancellationToken.None));
    }

    [Fact]
    public void FourCycleWindowAndLateCreditAreNotEquivalentToDebit()
    {
        var cycles = Enumerable.Range(1, 5).Select(i => new AchCycle
        {
            Id = $"C{i}", ClearingHouseId = 41, CycleName = $"Ciclo {i}",
            ProcessingDate = new DateTime(2026, 11, 2),
            StartTime = TimeSpan.FromHours(6 + i),
            EndTime = TimeSpan.FromHours(7 + i),
            OperationalStatus = AchCycleOperationalStatus.Closed,
            ClearingHouseCycleConfig = new ClearingHouseCycleConfig { AllowsReturn = true }
        }).ToArray();
        var original = new AchTransaction { AchCycleId = "C1", Type = TransactionTypeEnum.Credit,
            EffectiveEntryDate = new DateTime(2026, 11, 2) };
        var late = AchColombiaReturnPreparationWindow.Evaluate(original, cycles, new DateTime(2026, 11, 2, 15, 0, 0));
        Assert.NotNull(late);
        Assert.True(late.Value.LateCreditReview);
        Assert.Null(late.Value.CycleId);
        original.Type = TransactionTypeEnum.Debit;
        Assert.Null(AchColombiaReturnPreparationWindow.Evaluate(original, cycles, new DateTime(2026, 11, 2, 15, 0, 0)));
        original.Type = TransactionTypeEnum.Prenotification;
        Assert.Null(AchColombiaReturnPreparationWindow.Evaluate(original, cycles, new DateTime(2026, 11, 2, 15, 0, 0)));
    }

    [Fact]
    public void SevenCyclePolicyAllowsPreviousOperationalDayAndHandlesOvernightCycleOne()
    {
        var previous = new AchCycle
        {
            Id = "P7", CycleName = "Ciclo 7", ProcessingDate = new DateTime(2026, 11, 2),
            StartTime = new TimeSpan(16, 1, 0), EndTime = new TimeSpan(18, 0, 0),
            OperationalStatus = AchCycleOperationalStatus.Closed,
            ClearingHouseCycleConfig = new ClearingHouseCycleConfig
            {
                AllowsReturn = true, PolicyVersion = "ACHCOL-7C-20261014"
            }
        };
        var overnight = new AchCycle
        {
            Id = "N1", CycleName = "Ciclo 1", ProcessingDate = new DateTime(2026, 11, 3),
            StartTime = new TimeSpan(19, 1, 0), EndTime = new TimeSpan(7, 0, 0),
            OperationalStatus = AchCycleOperationalStatus.Scheduled,
            ClearingHouseCycleConfig = new ClearingHouseCycleConfig
            {
                AllowsReturn = true, PolicyVersion = "ACHCOL-7C-20261014"
            }
        };
        var original = new AchTransaction
        {
            AchCycleId = previous.Id, Type = TransactionTypeEnum.Credit,
            EffectiveEntryDate = new DateTime(2026, 11, 2)
        };
        var result = AchColombiaReturnPreparationWindow.Evaluate(original,
            [previous, overnight], new DateTime(2026, 11, 2, 18, 30, 0));
        Assert.Equal("N1", result?.CycleId);
        Assert.False(result!.Value.LateCreditReview);
    }

    [Fact]
    public void MissingOrCancelledCycleNeverCreatesAnInventedWindow()
    {
        var original = new AchTransaction { AchCycleId = "C1", Type = TransactionTypeEnum.Credit,
            EffectiveEntryDate = new DateTime(2026, 11, 2) };
        Assert.Null(AchColombiaReturnPreparationWindow.Evaluate(original, [], new DateTime(2026, 11, 2, 10, 0, 0)));
        Assert.Null(AchColombiaReturnPreparationWindow.Evaluate(original,
            [new AchCycle { Id = "C1", CycleName = "Ciclo 1", ProcessingDate = new DateTime(2026, 11, 2) }],
            new DateTime(2026, 11, 2, 10, 0, 0)));
    }

    [Fact]
    public async Task SqliteUniqueIndexBlocksConcurrentDuplicateAtDatabaseLevel()
    {
        // Prueba del tipo de restricción requerida; ejecutable sin SQL Server.
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var create = connection.CreateCommand();
        create.CommandText = "CREATE TABLE Candidates (Id INTEGER PRIMARY KEY, OriginalTransactionId INTEGER NOT NULL); "
            + "CREATE UNIQUE INDEX UX_AchColReturnPreparation_Original ON Candidates(OriginalTransactionId);";
        await create.ExecuteNonQueryAsync();
        await using var first = connection.CreateCommand();
        first.CommandText = "INSERT INTO Candidates (OriginalTransactionId) VALUES (77)";
        await first.ExecuteNonQueryAsync();
        await using var duplicate = connection.CreateCommand();
        duplicate.CommandText = "INSERT INTO Candidates (OriginalTransactionId) VALUES (77)";
        await Assert.ThrowsAsync<SqliteException>(() => duplicate.ExecuteNonQueryAsync());
    }

    private static AchDbContext NewDb() => new(
        new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase($"i03-ach-{Guid.NewGuid():N}").Options);

    private static void Seed(AchDbContext db, string code, string mode,
        TransactionTypeEnum type, bool verified)
    {
        const int houseId = 41;
        var house = new ClearingHouse { Id = houseId, Code = code, Name = code, OriginCode = "00000000" };
        db.ClearingHouses.Add(house);
        db.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = houseId, ClearingHouseId = houseId, AutomaticReturnMode = mode
        });
        for (var i = 1; i <= 5; i++)
        {
            db.AchCycles.Add(new AchCycle
            {
                Id = $"C{i}", ClearingHouseId = houseId,
                CycleName = $"Ciclo {i}",
                ProcessingDate = new DateTime(2026, 11, 2),
                StartTime = TimeSpan.FromHours(6 + i),
                EndTime = TimeSpan.FromHours(7 + i),
                OperationalStatus = i <= 2 ? AchCycleOperationalStatus.Closed : AchCycleOperationalStatus.Scheduled,
                ClearingHouseCycleConfig = new ClearingHouseCycleConfig
                {
                    Id = 100 + i, ClearingHouseId = houseId, CycleName = $"Ciclo {i}",
                    AllowsReturn = true
                }
            });
        }
        db.AchTransactions.Add(new AchTransaction
        {
            Id = 77, AchCycleId = "C1", Type = type, State = AchTransferStateEnum.Pending,
            EffectiveEntryDate = new DateTime(2026, 11, 2), Reference = "Original",
            SourceAccountNumber = "123456", DestinationAccountNumber = "654321"
        });
        db.ReturnReasons.Add(new ReturnReason { Code = "R02", Description = "Cuenta cerrada", IsForReturn = true });
        db.Set<AchReturnCode>().Add(new AchReturnCode
        {
            Id = 14, ClearingHouseId = houseId, Code = "R02", IsActive = true,
            AppliesToCredit = true, AppliesToDebit = true, AppliesToPrenotification = true
        });
        var ingestionId = Guid.NewGuid();
        var queueId = Guid.NewGuid();
        var executionId = Guid.NewGuid();
        db.IncomingNachaDispatchQueue.Add(new IncomingNachaDispatchQueue
        {
            Id = queueId, ClearingHouseId = houseId, AchTransactionId = 77, AchCycleId = "C1",
            IncomingNachaFileIngestionId = ingestionId,
            IncomingNachaTransactionLinkId = Guid.NewGuid(),
            SourceKind = IncomingNachaDispatchSourceKinds.CycleBound
        });
        db.IncomingNachaIntegrationExecution.Add(new IncomingNachaIntegrationExecution
        {
            Id = executionId, DispatchQueueId = queueId, ClearingHouseId = houseId,
            ResultCode = "R02", AchReturnCodeId = 14, ResponseCatalogId = 14,
            ExecutionMode = "Live", ProcessedAtUtc = FixedNow.UtcDateTime,
            ProcessingStatus = IncomingNachaIndividualProcessingStatus.Completed,
            TransportStatus = IntegrationTransportStatus.Succeeded,
            BusinessStatus = IntegrationResponseBusinessStatus.Rejected,
            IsFunctionalRejection = true
        });
        db.IncomingNachaProcessingEvents.Add(new IncomingNachaProcessingEvent
        {
            Id = Guid.NewGuid(), IncomingNachaFileIngestionId = ingestionId,
            AchTransactionId = 77,
            EventType = verified ? AchColombiaApplicationFailureDetector.DetectedEvent
                : AchColombiaApplicationFailureDetector.ReviewEvent,
            RaisedBy = "achcol.application-failure.detector", EventStatus = "Detectada",
            Message = "Incidencia de aplicación", OccurredAtUtc = FixedNow.UtcDateTime,
            EvidenceJson = JsonSerializer.Serialize(new
            {
                camera = "ACHCOL", transactionId = 77, origin = "CORE_FUNCIONAL",
                causeVerified = verified, returnGenerated = false, returnSent = false,
                suggestedReturnReason = verified ? "R02" : "", responseCode = "R02",
                queueId, executionId
            })
        });
        db.SaveChanges();
    }

    private sealed class FixedClock(DateTimeOffset instant) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => instant;
    }
}
