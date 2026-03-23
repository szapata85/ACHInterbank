using Cfa.ACHInterbank.Application.ACH.Implementation;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaSemanticAndStrategyTests
{
    [Fact]
    public void NachaSemanticValidator_RejectsMassCreditBatchWithoutMultiCreditDescription()
    {
        var validator = new NachaSemanticValidator();
        var batch = new AchBatch
        {
            Id = 1,
            CompanyEntryDescription = "NOMINAS",
            EffectiveEntryDate = DateTime.Today
        };

        var transactions = new List<AchTransaction>
        {
            new() { Id = 1, AchBatchId = 1, Type = TransactionTypeEnum.Credit, Amount = 1000m, Addendas = [new AchTransactionAddenda { AddendaType = "05" }] },
            new() { Id = 2, AchBatchId = 1, Type = TransactionTypeEnum.Credit, Amount = 2000m, Addendas = [new AchTransactionAddenda { AddendaType = "05" }] }
        };

        var context = new NachaBuildContext
        {
            Batches = [batch],
            Transactions = transactions,
            Cycle = new AchCycle { Id = "cycle-1", CycleName = "CICLO-1", ProcessingDate = DateTime.Today }
        };

        var ex = Assert.Throws<InvalidOperationException>(() => validator.Validate(new string('1', 106) + new string('5', 106) + new string('8', 106) + new string('9', 106), context));
        Assert.Contains("MULTICREDIT", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CenitStrategy_UsesRegulatoryFileNamingConvention()
    {
        var strategy = new CenitClearingHouseStrategy();
        var cycle = new AchCycle
        {
            CycleName = "CICLO-3",
            ProcessingDate = new DateTime(2026, 03, 23),
            ClearingHouse = new ClearingHouse { OriginCode = "12345678" }
        };

        var fileName = strategy.BuildFileName(cycle, new DateTime(2026, 03, 23, 12, 00, 00, DateTimeKind.Utc));

        Assert.Equal("12345678.3.1", fileName);
    }

    [Fact]
    public void AchStrategy_ValidatesReturnAndReversalRequirements()
    {
        var strategy = new AchClearingHouseStrategy();

        Assert.False(strategy.ValidateTransaction(new AchTransaction { Type = TransactionTypeEnum.Reversal, Amount = 100m }));
        Assert.True(strategy.ValidateTransaction(new AchTransaction { Type = TransactionTypeEnum.Reversal, Amount = 100m, OriginalTraceRef = "123456780000001" }));
        Assert.True(strategy.ValidateTransaction(new AchTransaction { Type = TransactionTypeEnum.Return, Amount = 100m, OriginalTraceRef = "123456780000001", ReturnReasonCode = "DEV14" }));
    }
}
