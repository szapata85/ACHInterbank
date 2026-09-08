using Cfa.ACHInterbank.Application.ACH.Implementation;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaSemanticAndStrategyTests
{
    [Theory]
    [InlineData("200", TransactionTypeEnum.Credit, "22")]
    [InlineData("200", TransactionTypeEnum.Debit, "27")]
    [InlineData("220", TransactionTypeEnum.Credit, "22")]
    [InlineData("225", TransactionTypeEnum.Debit, "27")]
    public void NachaSemanticValidator_AcceptsServiceClassDirectionContract(
        string serviceClassCode,
        TransactionTypeEnum transactionType,
        string transactionCode)
    {
        var (content, context) = BuildSemanticFile(serviceClassCode, serviceClassCode, transactionType, transactionCode);

        new NachaSemanticValidator().Validate(content, context, OfficialSemanticContract());
    }

    [Theory]
    [InlineData("220", TransactionTypeEnum.Debit, "27")]
    [InlineData("225", TransactionTypeEnum.Credit, "22")]
    public void NachaSemanticValidator_RejectsDirectionDisallowedByServiceClass(
        string serviceClassCode,
        TransactionTypeEnum transactionType,
        string transactionCode)
    {
        var (content, context) = BuildSemanticFile(serviceClassCode, serviceClassCode, transactionType, transactionCode);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new NachaSemanticValidator().Validate(content, context, OfficialSemanticContract()));

        Assert.Contains("NACHA_SERVICE_CLASS_DIRECTION_NOT_ALLOWED", ex.Message);
    }

    [Fact]
    public void NachaSemanticValidator_RejectsTransactionCodeDirectionMismatch()
    {
        var (content, context) = BuildSemanticFile("200", "200", TransactionTypeEnum.Credit, "27");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new NachaSemanticValidator().Validate(content, context, OfficialSemanticContract()));

        Assert.Contains("NACHA_TRANSACTION_CODE_DIRECTION_MISMATCH", ex.Message);
    }

    [Fact]
    public void NachaSemanticValidator_RejectsType5Type8ServiceClassMismatch()
    {
        var (content, context) = BuildSemanticFile("200", "220", TransactionTypeEnum.Credit, "22");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new NachaSemanticValidator().Validate(content, context, OfficialSemanticContract()));

        Assert.Contains("NACHA_T5_T8_SERVICE_CLASS_MISMATCH", ex.Message);
    }

    [Fact]
    public void NachaSemanticValidator_RejectsMissingResolvedSemanticContract()
    {
        var (content, context) = BuildSemanticFile("200", "200", TransactionTypeEnum.Credit, "22");

        Assert.Throws<ArgumentNullException>(() =>
            new NachaSemanticValidator().Validate(content, context, null!));
    }

    [Fact]
    public void NachaSemanticValidator_FollowsResolvedMetadata_NotProfileOrClearingHouse()
    {
        var (content, context) = BuildSemanticFile("220", "220", TransactionTypeEnum.Credit, "22");
        var creditContract = OfficialSemanticContract();
        var debitContract = new NachaServiceClassSemanticContract(
        [
            new("200", true, true),
            new("220", false, true),
            new("225", false, true)
        ]);

        new NachaSemanticValidator().Validate(content, context, creditContract);
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new NachaSemanticValidator().Validate(content, context, debitContract));

        Assert.Contains("NACHA_SERVICE_CLASS_DIRECTION_NOT_ALLOWED", ex.Message);
    }

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

    private static (string Content, NachaBuildContext Context) BuildSemanticFile(
        string type5ServiceClass,
        string type8ServiceClass,
        TransactionTypeEnum transactionType,
        string transactionCode)
    {
        var batch = new AchBatch
        {
            Id = 1,
            ServiceClassCode = type5ServiceClass,
            CompanyEntryDescription = "PAGOS",
            EffectiveEntryDate = new DateTime(2026, 9, 8)
        };
        var transaction = new AchTransaction
        {
            Id = 1,
            AchBatchId = 1,
            Type = transactionType,
            TransactionCode = transactionCode,
            Amount = 100m
        };
        var context = new NachaBuildContext
        {
            Batches = [batch],
            Transactions = [transaction],
            Cycle = new AchCycle { Id = "semantic-cycle", CycleName = "CICLO-1", ProcessingDate = new DateTime(2026, 9, 8) }
        };

        var type1 = Record('1');
        var type5 = Record('5', (2, type5ServiceClass), (54, "PAGOS"));
        var type6 = Record('6', (2, transactionCode), (87, "0"));
        var type8 = Record('8', (2, type8ServiceClass));
        var type9 = Record('9');
        return (type1 + type5 + type6 + type8 + type9, context);
    }

    private static string Record(char recordType, params (int Position, string Value)[] values)
    {
        var chars = Enumerable.Repeat(' ', 106).ToArray();
        chars[0] = recordType;
        foreach (var (position, value) in values)
        {
            value.CopyTo(0, chars, position - 1, value.Length);
        }

        return new string(chars);
    }

    private static NachaServiceClassSemanticContract OfficialSemanticContract()
        => new(
        [
            new("200", true, true),
            new("220", true, false),
            new("225", false, true)
        ]);
}
