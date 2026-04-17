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

        var result = await sut.LinkAsync(new EntryDetail { SequenceNumber = "123456789012345", Amount = 100, AccountNumber = "001", RecipIdNumber = "A" }, null);
        Assert.Equal(IncomingNachaLinkType.ExactTrace15, result.LinkType);
        Assert.Equal(1, result.AchTransactionId);
    }

    [Fact]
    public async Task LinkAsync_ByExternalId_Exact()
    {
        using var context = BuildContext();
        SeedTx(context, 2, trace: "223456789012345", externalId: "EXT-2");
        var sut = new IncomingNachaTransactionLinker(context);

        var result = await sut.LinkAsync(new EntryDetail { SequenceNumber = "000000000000001", RecipIdNumber = "EXT-2", Amount = 10, AccountNumber = "001" }, null);
        Assert.Equal(IncomingNachaLinkType.ExactTransactionExternalId, result.LinkType);
        Assert.Equal(2, result.AchTransactionId);
    }

    [Fact]
    public async Task LinkAsync_NotFound()
    {
        using var context = BuildContext();
        var sut = new IncomingNachaTransactionLinker(context);

        var result = await sut.LinkAsync(new EntryDetail { SequenceNumber = "000000000000009", RecipIdNumber = "NF", Amount = 1, AccountNumber = "1" }, null);
        Assert.Equal(IncomingNachaLinkType.NotFound, result.LinkType);
        Assert.Null(result.AchTransactionId);
    }

    [Fact]
    public async Task LinkAsync_Ambiguous_WhenMultipleTraceCandidates()
    {
        using var context = BuildContext();
        SeedTx(context, 3, trace: "333333333333333", externalId: "A1");
        SeedTx(context, 4, trace: "333333333333333", externalId: "A2");
        var sut = new IncomingNachaTransactionLinker(context);

        var result = await sut.LinkAsync(new EntryDetail { SequenceNumber = "333333333333333", Amount = 10, AccountNumber = "001" }, null);
        Assert.Equal(IncomingNachaLinkType.Ambiguous, result.LinkType);
        Assert.True(result.IsAmbiguous);
    }

    private static AchDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AchDbContext(options);
    }

    private static void SeedTx(AchDbContext context, int id, string trace, string externalId)
    {
        context.AchTransactions.Add(new AchTransaction
        {
            Id = id,
            TraceNumber = trace,
            TransactionExternalId = externalId,
            Amount = 100,
            DestinationAccountNumber = "001",
            RecipientIdNumber = "A",
            Reference = "R",
            TransactionCode = "22",
            Type = Domain.Entities.Transactions.Enums.TransactionTypeEnum.Credit,
            SourceAccountNumber = "S",
            OriginatingDFI = "00000000",
            ReceivingDFI = "00000000",
            AchCycleId = "C1",
            AchBatchId = 1,
            SourceInstitutionId = 1,
            DestinationInstitutionId = 1,
            CompanyName = "C",
            CompanyIdentification = "I",
            EffectiveEntryDate = DateTime.UtcNow
        });
        context.SaveChanges();
    }
}
