using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class IncomingNachaTransactionLinkerTests
{
    [Fact]
    public async Task LinkAsync_ByTrace_Exact()
    {
        using var context = BuildContext();
        SeedTx(context, 1, trace: "123456789012345", externalId: "EXT-1");
        var sut = new IncomingNachaTransactionLinker(context);

        var result = await sut.LinkAsync(new EntryDetail { SequenceNumber = "123456789012345", Amount = 100, AccountNumber = "001", RecipIdNumber = "A", TransactionCode = "22", ReceivingParticipantEntityCode = "00000000", CheckDigit = "0" }, null, new IncomingNachaLinkingContext { FunctionalClass = IncomingNachaFunctionalClass.CreditoEntrante });
        Assert.Equal(IncomingNachaLinkType.ExactTrace15, result.LinkType);
        Assert.Equal(1, result.AchTransactionId);
    }

    [Fact]
    public async Task LinkAsync_DoesNotTreatRecipientIdAsTransactionExternalId()
    {
        using var context = BuildContext();
        SeedTx(context, 2, trace: "223456789012345", externalId: "EXT-2");
        var sut = new IncomingNachaTransactionLinker(context);

        var result = await sut.LinkAsync(new EntryDetail { SequenceNumber = "000000000000001", RecipIdNumber = "EXT-2", Amount = 10, AccountNumber = "001", TransactionCode = "22" }, null, new IncomingNachaLinkingContext { FunctionalClass = IncomingNachaFunctionalClass.CreditoEntrante });
        Assert.Equal(IncomingNachaLinkType.NotFound, result.LinkType);
        Assert.Null(result.AchTransactionId);
    }

    [Fact]
    public async Task LinkAsync_NotFound()
    {
        using var context = BuildContext();
        var sut = new IncomingNachaTransactionLinker(context);

        var result = await sut.LinkAsync(new EntryDetail { SequenceNumber = "000000000000009", RecipIdNumber = "NF", Amount = 1, AccountNumber = "1", TransactionCode = "22" }, null, new IncomingNachaLinkingContext { FunctionalClass = IncomingNachaFunctionalClass.CreditoEntrante });
        Assert.Equal(IncomingNachaLinkType.NotFound, result.LinkType);
        Assert.Null(result.AchTransactionId);
    }

    [Fact]
    public async Task LinkAsync_RorWithoutParentReturn_ShouldRemainOrphanWithRawEvidence()
    {
        using var context = BuildContext();
        var entry = new EntryDetail
        {
            EntryDetailID = 910,
            SequenceNumber = "123456780000910",
            TransactionCode = "21",
            Amount = 100m,
            AccountNumber = "1"
        };
        var addenda = new AddendaRecord
        {
            AddendaID = 911,
            EntryDetailId = entry.EntryDetailID,
            BusinessType = "ReturnOfReturn",
            ReturnReasonCode = "R60",
            OriginalTraceNumber = "123456780000001",
            NewTraceNumber = "876543210000999"
        };
        context.AddRange(entry, addenda);
        await context.SaveChangesAsync();
        var sut = new IncomingNachaTransactionLinker(context);

        var result = await sut.LinkAsync(entry, addenda, new IncomingNachaLinkingContext
        {
            FunctionalClass = IncomingNachaFunctionalClass.DevolucionDevolucion,
            ResolvedAchCycleId = "C4"
        });

        Assert.Equal(IncomingNachaLinkType.NotFound, result.LinkType);
        Assert.True(result.IsNotFound);
        Assert.Null(result.AchTransactionId);
        Assert.Equal(1, await context.EntryDetails.CountAsync(x => x.EntryDetailID == entry.EntryDetailID));
        Assert.Equal(1, await context.AddendaRecords.CountAsync(x => x.AddendaID == addenda.AddendaID));
    }

    [Fact]
    public async Task LinkAsync_Ambiguous_WhenMultipleTraceCandidates()
    {
        using var context = BuildContext();
        SeedTx(context, 3, trace: "333333333333333", externalId: "A1");
        SeedTx(context, 4, trace: "333333333333333", externalId: "A2");
        var sut = new IncomingNachaTransactionLinker(context);

        var result = await sut.LinkAsync(new EntryDetail { SequenceNumber = "333333333333333", Amount = 10, AccountNumber = "001", TransactionCode = "22" }, null, new IncomingNachaLinkingContext { FunctionalClass = IncomingNachaFunctionalClass.CreditoEntrante });
        Assert.Equal(IncomingNachaLinkType.Ambiguous, result.LinkType);
        Assert.True(result.IsAmbiguous);
    }

    [Fact]
    public async Task LinkAsync_ByOriginalTrace_ShouldUseResolvedCycleToDisambiguateDailyTraceReuse()
    {
        using var context = BuildContext();
        SeedTx(context, 5, trace: "555555555555555", externalId: "HISTORICAL", achCycleId: "CYCLE-OLD");
        SeedTx(context, 6, trace: "555555555555555", externalId: "CURRENT", achCycleId: "CYCLE-CURRENT");
        var sut = new IncomingNachaTransactionLinker(context);

        var result = await sut.LinkAsync(
            new EntryDetail { SequenceNumber = "666666666666666", Amount = 100, AccountNumber = "001", TransactionCode = "26" },
            new AddendaRecord { OriginalTraceNumber = "555555555555555", ReturnReasonCode = "R01" },
            new IncomingNachaLinkingContext
            {
                FunctionalClass = IncomingNachaFunctionalClass.Devolucion,
                ResolvedAchCycleId = "CYCLE-CURRENT"
            });

        Assert.Equal(IncomingNachaLinkType.ExactOriginalTraceRef, result.LinkType);
        Assert.Equal(6, result.AchTransactionId);
        Assert.True(result.IsFinal);
        Assert.Contains("ResolvedCycle", result.EvidenceJson);
    }

    [Fact]
    public async Task LinkAsync_ReturnWithExactOriginalTraceAndDifferentAmount_ShouldNotLink()
    {
        using var context = BuildContext();
        SeedTx(context, 7, trace: "777777777777777", externalId: "ORIGINAL", amount: 100m);
        var sut = new IncomingNachaTransactionLinker(context);

        var result = await sut.LinkAsync(
            new EntryDetail { SequenceNumber = "888888888888888", Amount = 99m, AccountNumber = "001", TransactionCode = "26" },
            new AddendaRecord { OriginalTraceNumber = "777777777777777", ReturnReasonCode = "R01" },
            new IncomingNachaLinkingContext { FunctionalClass = IncomingNachaFunctionalClass.Devolucion });

        Assert.Equal(IncomingNachaLinkType.NotFound, result.LinkType);
        Assert.False(result.IsFinal);
        Assert.Null(result.AchTransactionId);
    }

    [Fact]
    public async Task LinkAsync_ReturnWithExactOriginalTraceAndSameAmount_ShouldLink()
    {
        using var context = BuildContext();
        SeedTx(context, 8, trace: "888888888888888", externalId: "ORIGINAL", amount: 100m);
        var sut = new IncomingNachaTransactionLinker(context);

        var result = await sut.LinkAsync(
            new EntryDetail { SequenceNumber = "999999999999999", Amount = 100m, AccountNumber = "001", TransactionCode = "26" },
            new AddendaRecord { OriginalTraceNumber = "888888888888888", ReturnReasonCode = "R01" },
            new IncomingNachaLinkingContext { FunctionalClass = IncomingNachaFunctionalClass.Devolucion });

        Assert.Equal(IncomingNachaLinkType.ExactOriginalTraceRef, result.LinkType);
        Assert.True(result.IsFinal);
        Assert.Equal(8, result.AchTransactionId);
    }


    [Fact]
    public async Task LinkAsync_CompositeKey_AvoidsCollision_WithOperationalDimensions()
    {
        using var context = BuildContext();
        SeedTx(context, 10, trace: "999999999999910", externalId: "EXT-C1", amount: 500, account: "AC1", recipientId: "RID1", transactionCode: "22", achCycleId: "CYCLE-A", receivingDfi: "111111110", effectiveDate: new DateTime(2026, 4, 20));
        SeedTx(context, 11, trace: "999999999999911", externalId: "EXT-C2", amount: 500, account: "AC1", recipientId: "RID1", transactionCode: "22", achCycleId: "CYCLE-B", receivingDfi: "111111110", effectiveDate: new DateTime(2026, 4, 21));

        var sut = new IncomingNachaTransactionLinker(context);
        var result = await sut.LinkAsync(
            new EntryDetail
            {
                SequenceNumber = "000000000000000",
                Amount = 500,
                AccountNumber = "AC1",
                RecipIdNumber = "RID1",
                TransactionCode = "22",
                ReceivingParticipantEntityCode = "11111111",
                CheckDigit = "0"
            },
            null,
            new IncomingNachaLinkingContext
            {
                FunctionalClass = IncomingNachaFunctionalClass.CreditoEntrante,
                ResolvedAchCycleId = "CYCLE-A",
                OperationalDate = new DateTime(2026, 4, 20)
            });

        Assert.Equal(IncomingNachaLinkType.ExactCompositeBusinessKey, result.LinkType);
        Assert.Equal(10, result.AchTransactionId);
    }

    [Fact]
    public async Task LinkAsync_CompositeKey_Ambiguous_WhenStillNotDeterministic()
    {
        using var context = BuildContext();
        SeedTx(context, 20, trace: "999999999999920", externalId: "EXT-D1", amount: 700, account: "AC2", recipientId: "RID2", transactionCode: "22", achCycleId: "", receivingDfi: "222222220", effectiveDate: new DateTime(2026, 4, 20));
        SeedTx(context, 21, trace: "999999999999921", externalId: "EXT-D2", amount: 700, account: "AC2", recipientId: "RID2", transactionCode: "22", achCycleId: "", receivingDfi: "222222220", effectiveDate: new DateTime(2026, 4, 20));

        var sut = new IncomingNachaTransactionLinker(context);
        var result = await sut.LinkAsync(
            new EntryDetail
            {
                SequenceNumber = "000000000000000",
                Amount = 700,
                AccountNumber = "AC2",
                RecipIdNumber = "RID2",
                TransactionCode = "22",
                ReceivingParticipantEntityCode = "22222222",
                CheckDigit = "0"
            },
            null,
            new IncomingNachaLinkingContext
            {
                FunctionalClass = IncomingNachaFunctionalClass.CreditoEntrante,
                OperationalDate = new DateTime(2026, 4, 20)
            });

        Assert.Equal(IncomingNachaLinkType.Ambiguous, result.LinkType);
    }

    [Fact]
    public async Task LinkAsync_ReturnCompositeKey_DoesNotGuessTransactionType()
    {
        using var context = BuildContext();
        SeedTx(context, 30, trace: "999999999999930", externalId: "EXT-F1", amount: 810, account: "AC3", recipientId: "RID3", transactionCode: "27", achCycleId: "CYCLE-X", receivingDfi: "333333330", effectiveDate: new DateTime(2026, 4, 20), type: Domain.Entities.Transactions.Enums.TransactionTypeEnum.Credit);
        SeedTx(context, 31, trace: "999999999999931", externalId: "EXT-F2", amount: 810, account: "AC3", recipientId: "RID3", transactionCode: "27", achCycleId: "CYCLE-X", receivingDfi: "333333330", effectiveDate: new DateTime(2026, 4, 20), type: Domain.Entities.Transactions.Enums.TransactionTypeEnum.Debit);

        var sut = new IncomingNachaTransactionLinker(context);
        var result = await sut.LinkAsync(
            new EntryDetail
            {
                SequenceNumber = "000000000000000",
                Amount = 810,
                AccountNumber = "AC3",
                RecipIdNumber = "RID3",
                TransactionCode = "27",
                ReceivingParticipantEntityCode = "33333333",
                CheckDigit = "0"
            },
            null,
            new IncomingNachaLinkingContext
            {
                FunctionalClass = IncomingNachaFunctionalClass.Devolucion,
                ResolvedAchCycleId = "CYCLE-X",
                OperationalDate = new DateTime(2026, 4, 20)
            });

        Assert.Equal(IncomingNachaLinkType.Ambiguous, result.LinkType);
        Assert.Null(result.AchTransactionId);
    }

    private static AchDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AchDbContext(options);
    }

    private static void SeedTx(AchDbContext context, int id, string trace, string externalId, decimal amount = 100, string account = "001", string recipientId = "A", string transactionCode = "22", string achCycleId = "C1", string receivingDfi = "000000000", DateTime? effectiveDate = null, Domain.Entities.Transactions.Enums.TransactionTypeEnum type = Domain.Entities.Transactions.Enums.TransactionTypeEnum.Credit)
    {
        context.AchTransactions.Add(new AchTransaction
        {
            Id = id,
            TraceNumber = trace,
            TransactionExternalId = externalId,
            Amount = amount,
            DestinationAccountNumber = account,
            RecipientIdNumber = recipientId,
            Reference = "R",
            TransactionCode = transactionCode,
            Type = type,
            SourceAccountNumber = "S",
            OriginatingDFI = "00000000",
            ReceivingDFI = receivingDfi,
            AchCycleId = achCycleId,
            AchBatchId = 1,
            SourceInstitutionId = 1,
            DestinationInstitutionId = 1,
            CompanyName = "C",
            CompanyIdentification = "I",
            EffectiveEntryDate = effectiveDate ?? DateTime.UtcNow
        });
        context.SaveChanges();
    }
}
