using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class NachaProfileDimensionResolverTests
{
    [Fact]
    public void MixedOutboundEntries_ResolveOriginalOutboundProfile()
    {
        AchTransaction[] transactions =
        [
            new() { Type = TransactionTypeEnum.Credit },
            new() { Type = TransactionTypeEnum.Debit },
            new() { Type = TransactionTypeEnum.Prenotification }
        ];

        Assert.Equal("ORIGINAL", NachaProfileDimensionResolver.ResolveFlowCode(transactions));
        Assert.Equal("SALIDA", NachaProfileDimensionResolver.ResolveDirectionCode(transactions));
    }

    [Fact]
    public void ReturnOnlyEntries_ResolveReturnInboundProfile()
    {
        AchTransaction[] transactions =
        [
            new() { Type = TransactionTypeEnum.Return },
            new() { Type = TransactionTypeEnum.Reversal }
        ];

        Assert.Equal("RETORNO", NachaProfileDimensionResolver.ResolveFlowCode(transactions));
        Assert.Equal("ENTRADA", NachaProfileDimensionResolver.ResolveDirectionCode(transactions));
    }

    [Fact]
    public void PrenotificationOnlyEntries_ResolvePrenotificationOutboundProfile()
    {
        AchTransaction[] transactions = [new() { Type = TransactionTypeEnum.Prenotification }];

        Assert.Equal("PRENOTIFICACION", NachaProfileDimensionResolver.ResolveFlowCode(transactions));
        Assert.Equal("SALIDA", NachaProfileDimensionResolver.ResolveDirectionCode(transactions));
    }
}
