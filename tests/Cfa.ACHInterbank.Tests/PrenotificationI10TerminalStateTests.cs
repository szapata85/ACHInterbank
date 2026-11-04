using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class PrenotificationI10TerminalStateTests
{
    [Theory]
    [InlineData(AchTransferStateEnum.AppliedTacitly)]
    [InlineData(AchTransferStateEnum.Certified)]
    public async Task I10_CanonicalService_NeverRewritesTerminalPrenoteAutomatically(
        AchTransferStateEnum previous)
    {
        await using var db = NewContext();
        db.AchTransactions.Add(new AchTransaction
        {
            Id = 8101, IsPrenotification = true, State = previous,
            TraceNumber = "123456780008101", Type = TransactionTypeEnum.Credit,
            Amount = 0m, TransactionCode = "23", AchCycleId = "CENIT-C1"
        });
        await db.SaveChangesAsync();
        var service = new AchStateTransitionService(db);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.TransitionAsync(
            new AchStateTransitionRequest(8101, AchTransferStateEnum.ReturnedByEpr,
                AchStateEventSourceEnum.Epr, "R03", "{}", "123456780008101")));
        Assert.Contains("PRENOTE_TERMINAL_CHANGE_REQUIRES_MANUAL_REVIEW", error.Message);
        Assert.Equal(previous, (await db.AchTransactions.SingleAsync()).State);
        Assert.Empty(await db.AchTransactionStateEvents.ToListAsync());
    }

    [Fact]
    public async Task I10_TerminalAttemptCannotBeOverwrittenEvenIfTransactionStillPending()
    {
        await using var db = NewContext();
        db.AchTransactions.Add(new AchTransaction
        {
            Id = 8102, IsPrenotification = true, State = AchTransferStateEnum.Pending,
            TraceNumber = "123456780008102", TransactionCode = "23", Amount = 0m
        });
        db.PrenotificationValidationAttempts.Add(new PrenotificationValidationAttempt
        {
            Id = 1001, AchTransactionId = 8102,
            Status = PrenotificationValidationAttemptStatus.TacitlyAccepted,
            Resolution = PrenotificationValidationResolution.TacitlyAccepted,
            ThirdPartyAccountValidationId = 2001
        });
        await db.SaveChangesAsync();
        var service = new AchStateTransitionService(db);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.TransitionAsync(
            new AchStateTransitionRequest(8102, AchTransferStateEnum.ReturnedByOperator,
                AchStateEventSourceEnum.Operator, "R03")));
        Assert.Contains("PRENOTE_ATTEMPT_ALREADY_TERMINAL_REQUIRES_MANUAL_REVIEW", error.Message);
        Assert.Equal(AchTransferStateEnum.Pending, (await db.AchTransactions.SingleAsync()).State);
    }

    [Fact]
    public async Task I10_CenitOperatorRejection_UsesCanonicalTransitionAndSynchronizesAttempt()
    {
        await using var db = NewContext();
        db.ClearingHouses.Add(new ClearingHouse
        {
            Id = 2, Name = "CENIT", Code = "CENIT", OriginCode = "001"
        });
        db.AchCycles.Add(new AchCycle
        {
            Id = "CENIT-C1", ClearingHouseId = 2,
            ProcessingDate = new DateTime(2026, 11, 3)
        });
        db.AchTransactions.Add(new AchTransaction
        {
            Id = 8201, IsPrenotification = true, State = AchTransferStateEnum.Pending,
            Type = TransactionTypeEnum.Credit, Amount = 0m,
            TraceNumber = "000010070008201", TransactionCode = "23",
            AchCycleId = "CENIT-C1"
        });
        db.AchFileExports.Add(new AchFileExport
        {
            Id = 77, ClearingHouseId = 2, AchCycleId = "CENIT-C1",
            ExportKind = "NACHA", FileName = "cenit-prenote-1",
            TransmissionReference = "REF-CENIT-8201",
            Transactions = new List<AchFileExportTransaction>
            {
                new() { AchTransactionId = 8201, AchCycleId = "CENIT-C1",
                    TraceNumber = "000010070008201", FileSequence = 1 }
            }
        });
        var validation = new ThirdPartyAccountValidation
        {
            Id = 820, ThirdPartyAccountId = 9, ClearingHouseId = 2,
            CurrentAttemptId = 821, Status = ThirdPartyAccountValidationStatus.PendingResponse
        };
        db.ThirdPartyAccountValidations.Add(validation);
        db.PrenotificationValidationAttempts.Add(new PrenotificationValidationAttempt
        {
            Id = 821, ThirdPartyAccountValidationId = 820,
            ThirdPartyAccountValidation = validation, AchTransactionId = 8201,
            Status = PrenotificationValidationAttemptStatus.AwaitingResponse,
            Resolution = PrenotificationValidationResolution.None,
            SourceCycleId = "CENIT-C1"
        });
        await db.SaveChangesAsync();

        var policy = Mock.Of<IClearingHouseValidationPolicyService>();
        var canonical = new ThirdPartyAccountValidationService(db, policy,
            NullLogger<ThirdPartyAccountValidationService>.Instance);
        var transitions = new AchStateTransitionService(db, canonical);
        var cenit = new CenitChamberResponseService(db,
            NullLogger<CenitChamberResponseService>.Instance, transitions);
        var response = await cenit.ImportAsync(new CenitChamberResponseImportCommand(
            "SRC-APN-I10-8201", "operator-nack.xml", "OPERATOR",
            "<FileNack><AdditionalRefs><RelatedRef>REF-CENIT-8201</RelatedRef></AdditionalRefs>" +
            "<FileErrorHandling><Status>NACK</Status><TraceNo>000010070008201</TraceNo>" +
            "<ErrorCode>R03</ErrorCode></FileErrorHandling></FileNack>",
            new DateTime(2026, 11, 3, 19, 0, 0, DateTimeKind.Utc),
            "cenit-prenote-1", null, "000010070008201", "CENIT-C1"));

        Assert.True(response.IsApplied);
        Assert.Equal(AchTransferStateEnum.ReturnedByOperator,
            (await db.AchTransactions.SingleAsync()).State);
        Assert.Equal(PrenotificationValidationResolution.Rejected,
            (await db.PrenotificationValidationAttempts.SingleAsync()).Resolution);
        Assert.Equal(ThirdPartyAccountValidationStatus.Rejected,
            (await db.ThirdPartyAccountValidations.SingleAsync()).Status);
        Assert.Single(await db.AchTransactionStateEvents.ToListAsync());
    }

    private static AchDbContext NewContext()
        => new(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options) { AuditEnabled = false };
}
