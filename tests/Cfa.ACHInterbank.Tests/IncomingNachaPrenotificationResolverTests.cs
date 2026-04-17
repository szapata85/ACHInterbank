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

    private static AchDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AchDbContext(options);
    }
}
