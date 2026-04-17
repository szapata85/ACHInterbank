using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class IncomingNachaPrenotificationResolverTests
{
    [Fact]
    public async Task ResolveAsync_ActivatesThirdParty_WhenSingleDeterministicCandidate()
    {
        using var context = BuildContext();
        context.CustomerThirdParties.Add(new CustomerThirdParty
        {
            Id = 1,
            CustomerId = 1,
            DestinationInstitutionId = 77,
            DestinationAccountNumber = "ACC-1",
            RecipientIdNumber = "RID-1",
            Status = CustomerThirdPartyStatusEnum.Pending
        });
        await context.SaveChangesAsync();

        var sut = new IncomingNachaPrenotificationResolver(context);
        var result = await sut.ResolveAsync(Guid.NewGuid(), new EntryDetail
        {
            AccountNumber = "ACC-1",
            RecipIdNumber = "RID-1"
        }, null, 1, new DateTime(2026, 4, 20), "tester");

        Assert.True(result.Applied);
        Assert.Equal(IncomingNachaPrenoteStatus.ActivaTercero, result.PrenoteStatus);
        var tp = await context.CustomerThirdParties.FirstAsync();
        Assert.Equal(CustomerThirdPartyStatusEnum.Active, tp.Status);
    }

    [Fact]
    public async Task ResolveAsync_RequiresManual_WhenMultipleCandidates()
    {
        using var context = BuildContext();
        context.CustomerThirdParties.AddRange(
            new CustomerThirdParty { Id = 1, CustomerId = 1, DestinationInstitutionId = 77, DestinationAccountNumber = "ACC-2", RecipientIdNumber = "RID-2", Status = CustomerThirdPartyStatusEnum.Pending },
            new CustomerThirdParty { Id = 2, CustomerId = 2, DestinationInstitutionId = 78, DestinationAccountNumber = "ACC-2", RecipientIdNumber = "RID-2", Status = CustomerThirdPartyStatusEnum.Pending });
        await context.SaveChangesAsync();

        var sut = new IncomingNachaPrenotificationResolver(context);
        var result = await sut.ResolveAsync(Guid.NewGuid(), new EntryDetail
        {
            AccountNumber = "ACC-2",
            RecipIdNumber = "RID-2"
        }, null, 1, new DateTime(2026, 4, 20), "tester");

        Assert.False(result.Applied);
        Assert.True(result.RequiresManualReview);
        Assert.Equal(IncomingNachaPrenoteStatus.RequiereRevision, result.PrenoteStatus);
    }

    [Fact]
    public async Task ResolveAsync_UsesLinkedTransactionInstitution_ToAvoidAmbiguity()
    {
        using var context = BuildContext();
        context.CustomerThirdParties.AddRange(
            new CustomerThirdParty { Id = 1, CustomerId = 1, DestinationInstitutionId = 77, DestinationAccountNumber = "ACC-3", RecipientIdNumber = "RID-3", Status = CustomerThirdPartyStatusEnum.Pending },
            new CustomerThirdParty { Id = 2, CustomerId = 2, DestinationInstitutionId = 78, DestinationAccountNumber = "ACC-3", RecipientIdNumber = "RID-3", Status = CustomerThirdPartyStatusEnum.Pending });
        context.AchTransactions.Add(new AchTransaction
        {
            Id = 100,
            Amount = 1,
            TransactionExternalId = "EXT-100",
            Reference = "REF-100",
            Type = TransactionTypeEnum.Credit,
            TransactionCode = "22",
            SourceAccountNumber = "SRC",
            DestinationAccountNumber = "ACC-3",
            SourceInstitutionId = 10,
            DestinationInstitutionId = 78,
            OriginatingDFI = "11111111",
            ReceivingDFI = "222222220",
            TraceNumber = "123456789012345",
            CompanyName = "C",
            CompanyIdentification = "I",
            AchCycleId = "C1",
            AchBatchId = 1,
            EffectiveEntryDate = new DateTime(2026, 4, 20)
        });
        await context.SaveChangesAsync();

        var sut = new IncomingNachaPrenotificationResolver(context);
        var result = await sut.ResolveAsync(Guid.NewGuid(), new EntryDetail
        {
            AccountNumber = "ACC-3",
            RecipIdNumber = "RID-3"
        }, 100, 1, new DateTime(2026, 4, 20), "tester");

        Assert.True(result.Applied);
        var tp1 = await context.CustomerThirdParties.FirstAsync(x => x.Id == 1);
        var tp2 = await context.CustomerThirdParties.FirstAsync(x => x.Id == 2);
        Assert.Equal(CustomerThirdPartyStatusEnum.Pending, tp1.Status);
        Assert.Equal(CustomerThirdPartyStatusEnum.Active, tp2.Status);
    }

    [Fact]
    public async Task ResolveAsync_RequiresManual_WhenRecipientIdMissingAndNoLinkedTransaction()
    {
        using var context = BuildContext();
        context.CustomerThirdParties.Add(new CustomerThirdParty
        {
            Id = 1,
            CustomerId = 1,
            DestinationInstitutionId = 77,
            DestinationAccountNumber = "ACC-4",
            RecipientIdNumber = "RID-4",
            Status = CustomerThirdPartyStatusEnum.Pending
        });
        await context.SaveChangesAsync();

        var sut = new IncomingNachaPrenotificationResolver(context);
        var result = await sut.ResolveAsync(Guid.NewGuid(), new EntryDetail
        {
            AccountNumber = "ACC-4",
            RecipIdNumber = null
        }, null, 1, new DateTime(2026, 4, 20), "tester");

        Assert.False(result.Applied);
        Assert.True(result.RequiresManualReview);
        Assert.Equal(IncomingNachaPrenoteStatus.RequiereRevision, result.PrenoteStatus);
    }

    private static AchDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AchDbContext(options);
    }
}
