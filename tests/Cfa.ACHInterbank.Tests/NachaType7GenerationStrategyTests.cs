using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaType7GenerationStrategyTests
{
    [Fact]
    public void BuildCandidates_ShouldCreateCreditDebitAndReturnCandidates()
    {
        var resolver = new NachaType7FieldValueResolver();
        INachaType7GenerationStrategy strategy = new NachaType7GenerationStrategy(resolver);

        var batch = new AchBatch
        {
            Id = 1,
            CompanyEntryDescription = "PAGOS",
            Transactions =
            [
                new AchTransaction
                {
                    Id = 10,
                    Type = TransactionTypeEnum.Credit,
                    TraceNumber = "123456789012345",
                    Addendas =
                    [
                        new AchTransactionAddenda
                        {
                            AddendaType = "05",
                            BusinessType = AchAddendaBusinessType.Credit,
                            Purpose = "PAGOS",
                            Reference = "REF"
                        }
                    ]
                },
                new AchTransaction
                {
                    Id = 11,
                    Type = TransactionTypeEnum.Debit,
                    TraceNumber = "123456789012346",
                    Addendas =
                    [
                        new AchTransactionAddenda
                        {
                            AddendaType = "05",
                            BusinessType = AchAddendaBusinessType.Debit,
                            CollectorId = "900111222",
                            ReceiverCustomerCode = "CLI001",
                            ServiceDescription = "PAGOS"
                        }
                    ]
                },
                new AchTransaction
                {
                    Id = 12,
                    Type = TransactionTypeEnum.Return,
                    TraceNumber = "123456789012347",
                    Addendas =
                    [
                        new AchTransactionAddenda
                        {
                            AddendaType = "99",
                            BusinessType = AchAddendaBusinessType.Return,
                            ReturnReasonCode = "R01",
                            OriginalTraceNumber = "111",
                            NewTraceNumber = "222"
                        }
                    ]
                }
            ]
        };

        var result = strategy.BuildCandidates([batch]);

        Assert.Equal(3, result.Count);
        Assert.Equal("CREDIT", result[0].FieldValues["BusinessType"]);
        Assert.Equal("DEBIT", result[1].FieldValues["BusinessType"]);
        Assert.Equal("RETURN", result[2].FieldValues["BusinessType"]);
    }

    [Fact]
    public void LegacyRenderer_ShouldRenderType7Credit()
    {
        INachaType7LegacyRenderer renderer = new NachaType7LegacyRenderer();
        var tx = new AchTransaction { Id = 1, Type = TransactionTypeEnum.Credit, TraceNumber = "123456789012345" };
        var batch = new AchBatch { CompanyEntryDescription = "PAGOS" };
        var addenda = new AchTransactionAddenda
        {
            AddendaType = "05",
            BusinessType = AchAddendaBusinessType.Credit,
            Purpose = "PAGOS",
            Reference = "ABC"
        };

        var line = renderer.Render(batch, tx, addenda);

        Assert.Equal('7', line[0]);
        Assert.Equal(106, line.Length);
    }
}
