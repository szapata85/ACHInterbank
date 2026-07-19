using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using FluentAssertions;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaControlTotalsCalculatorTests
{
    private readonly INachaControlTotalsCalculator _sut = new NachaControlTotalsCalculator();

    [Fact]
    public void CalculateBatchTotals_WhenOnlyCredits_ShouldSetCreditAmountAndZeroDebit()
    {
        var result = _sut.Calculate(Request([Tx(1, TransactionTypeEnum.Credit, 123.45m, "12345678")], addendaCount: 0));

        result.BatchTotals.Single().TotalCreditAmountInCents.Should().Be(12345);
        result.BatchTotals.Single().TotalDebitAmountInCents.Should().Be(0);
    }

    [Fact]
    public void CalculateBatchTotals_WhenOnlyDebits_ShouldSetDebitAmountAndZeroCredit()
    {
        var result = _sut.Calculate(Request([Tx(1, TransactionTypeEnum.Debit, 50m, "12345678")], addendaCount: 0));

        result.BatchTotals.Single().TotalDebitAmountInCents.Should().Be(5000);
        result.BatchTotals.Single().TotalCreditAmountInCents.Should().Be(0);
    }

    [Fact]
    public void CalculateBatchTotals_WhenMixedEntries_ShouldSeparateDebitsAndCredits()
    {
        var result = _sut.Calculate(Request([
            Tx(1, TransactionTypeEnum.Credit, 10m, "12345678"),
            Tx(2, TransactionTypeEnum.Debit, 20m, "87654321")
        ], addendaCount: 0));

        result.BatchTotals.Single().TotalCreditAmountInCents.Should().Be(1000);
        result.BatchTotals.Single().TotalDebitAmountInCents.Should().Be(2000);
    }

    [Fact]
    public void CalculateBatchTotals_ShouldRoundHalfCentsAwayFromZeroExplicitly()
    {
        var result = _sut.Calculate(Request([Tx(1, TransactionTypeEnum.Credit, 1.005m, "12345678")], addendaCount: 0));

        result.BatchTotals.Single().TotalCreditAmountInCents.Should().Be(101);
    }

    [Fact]
    public void CalculateBatchTotals_ShouldPreserveMaximumNachaMonetaryScale()
    {
        var request = Request([Tx(1, TransactionTypeEnum.Credit, 9_999_999_999_999_999.99m, "12345678")], addendaCount: 0);
        request.BatchTotalCreditAmountLength = 18;
        request.FileTotalCreditAmountLength = 18;

        var result = _sut.Calculate(request);

        result.BatchTotals.Single().TotalCreditAmountInCents.Should().Be(999_999_999_999_999_999L);
        result.FileTotals.TotalCreditAmountInCents.Should().Be(999_999_999_999_999_999L);
    }

    [Fact]
    public void CalculateBatchTotals_ShouldCountEntryDetailsAndAddendas()
    {
        var result = _sut.Calculate(Request([
            Tx(1, TransactionTypeEnum.Credit, 10m, "12345678"),
            Tx(2, TransactionTypeEnum.Credit, 20m, "87654321")
        ], addendaCount: 3));

        result.BatchTotals.Single().EntryDetailCount.Should().Be(2);
        result.BatchTotals.Single().AddendaCount.Should().Be(3);
        result.BatchTotals.Single().EntryAddendaCount.Should().Be(5);
    }

    [Fact]
    public void CalculateBatchTotals_ShouldCalculateEntryHashFromConfiguredField()
    {
        var result = _sut.Calculate(Request([
            Tx(1, TransactionTypeEnum.Credit, 10m, "12345678"),
            Tx(2, TransactionTypeEnum.Credit, 20m, "87654321")
        ], addendaCount: 0));

        result.BatchTotals.Single().EntryHash.Should().Be(99999999);
    }

    [Fact]
    public void CalculateBatchTotals_ShouldTruncateEntryHashToConfiguredLength()
    {
        var request = Request([Tx(1, TransactionTypeEnum.Credit, 10m, "99999999999")], addendaCount: 0);
        request.BatchEntryHashLength = 10;
        request.FileEntryHashLength = 10;

        var result = _sut.Calculate(request);

        result.BatchTotals.Single().EntryHash.Should().Be(9999999999);
    }

    [Fact]
    public void CalculateFileTotals_ShouldAggregateAllBatchTotals()
    {
        var request = Request([Tx(1, TransactionTypeEnum.Credit, 10m, "12345678")], addendaCount: 1);
        var batch2 = new AchBatch { Id = 200, ServiceClassCode = "200" };
        var tx2 = Tx(2, TransactionTypeEnum.Debit, 20m, "87654321", batch2.Id);
        request.Batches = [request.Batches[0], batch2];
        request.TransactionsByBatchId = new Dictionary<int, IReadOnlyList<AchTransaction>>
        {
            [100] = request.TransactionsByBatchId[100],
            [200] = [tx2]
        };
        request.AddendaRecordCountByBatchId = new Dictionary<int, int> { [100] = 1, [200] = 2 };
        request.PhysicalRecordCountBeforePadding = 8;

        var result = _sut.Calculate(request);

        result.FileTotals.BatchCount.Should().Be(2);
        result.FileTotals.EntryAddendaCount.Should().Be(5);
        result.FileTotals.TotalCreditAmountInCents.Should().Be(1000);
        result.FileTotals.TotalDebitAmountInCents.Should().Be(2000);
    }

    [Fact]
    public void CalculateFileTotals_ShouldCalculateBlockCount()
    {
        var request = Request([Tx(1, TransactionTypeEnum.Credit, 10m, "12345678")], addendaCount: 0);
        request.PhysicalRecordCountBeforePadding = 11;

        _sut.Calculate(request).FileTotals.BlockCount.Should().Be(2);
    }

    [Fact]
    public void CalculateFileTotals_ShouldCalculatePaddingRecordCount()
    {
        var request = Request([Tx(1, TransactionTypeEnum.Credit, 10m, "12345678")], addendaCount: 0);
        request.PhysicalRecordCountBeforePadding = 11;

        _sut.Calculate(request).FileTotals.PaddingRecordCount.Should().Be(9);
    }

    [Theory]
    [InlineData(1, "A")]
    [InlineData(26, "Z")]
    [InlineData(27, "0")]
    [InlineData(36, "9")]
    public void ResolveFileIdModifier_ShouldMapOfficialRange(int sequence, string expected)
    {
        _sut.ResolveFileIdModifier(sequence).Should().Be(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(37)]
    public void ResolveFileIdModifier_ShouldFailWhenSequenceIsOutOfRange(int sequence)
    {
        var act = () => _sut.ResolveFileIdModifier(sequence);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*Valor recibido: {sequence}*");
    }

    private static NachaControlTotalsRequest Request(IReadOnlyList<AchTransaction> transactions, int addendaCount)
    {
        var batch = new AchBatch { Id = 100, ServiceClassCode = "200", Transactions = transactions.ToList() };
        foreach (var transaction in transactions)
        {
            transaction.AchBatchId = batch.Id;
            transaction.AchBatch = batch;
        }

        return new NachaControlTotalsRequest
        {
            Batches = [batch],
            TransactionsByBatchId = new Dictionary<int, IReadOnlyList<AchTransaction>> { [batch.Id] = transactions },
            AddendaRecordCountByBatchId = new Dictionary<int, int> { [batch.Id] = addendaCount },
            EntryHashSourceFieldPath = nameof(AchTransaction.ReceivingDFI),
            BatchEntryHashLength = 10,
            FileEntryHashLength = 10,
            BatchEntryAddendaCountLength = 6,
            FileEntryAddendaCountLength = 8,
            BatchTotalDebitAmountLength = 12,
            FileTotalDebitAmountLength = 12,
            BatchTotalCreditAmountLength = 12,
            FileTotalCreditAmountLength = 12,
            BatchCountLength = 6,
            BlockCountLength = 6,
            PhysicalRecordCountBeforePadding = 6,
            BlockSize = 10
        };
    }

    private static AchTransaction Tx(int id, TransactionTypeEnum type, decimal amount, string receivingDfi, int batchId = 100)
    {
        return new AchTransaction
        {
            Id = id,
            Type = type,
            Amount = amount,
            ReceivingDFI = receivingDfi,
            AchBatchId = batchId,
            TransactionCode = type == TransactionTypeEnum.Debit ? "27" : "22"
        };
    }
}
