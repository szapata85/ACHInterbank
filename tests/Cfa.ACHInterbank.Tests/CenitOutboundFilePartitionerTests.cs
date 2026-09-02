using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

namespace Cfa.ACHInterbank.Tests;

public sealed class CenitOutboundFilePartitionerTests : IClassFixture<OfficialNachaGenerationFixture>
{
    private readonly OfficialNachaGenerationFixture _fixture;

    public CenitOutboundFilePartitionerTests(OfficialNachaGenerationFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(9_999, 1)]
    [InlineData(10_000, 1)]
    [InlineData(10_001, 2)]
    [InlineData(20_000, 2)]
    [InlineData(20_001, 3)]
    public async Task Ppd_PartitionsByFileEntryLimit_WithoutLossOrDuplication(int entryCount, int expectedFiles)
    {
        var sources = entryCount == 20_000
            ? new[]
            {
                SourceBatch(10, "PPD", CreateTransactions(7_000, withAddenda: false)),
                SourceBatch(11, "PPD", CreateTransactions(7_000, withAddenda: false, firstId: 7_001)),
                SourceBatch(12, "PPD", CreateTransactions(6_000, withAddenda: false, firstId: 14_001))
            }
            : [SourceBatch(10, "PPD", CreateTransactions(entryCount, withAddenda: false))];

        var policies = await ResolvePoliciesAsync("PPD");
        var entryLimit = policies.Single().Services.Single(service => service.ServiceCode == "PPD").MaxEntriesPerFile!.Value;
        var files = CenitOutboundFilePartitioner.Partition(sources, policies);

        Assert.Equal(expectedFiles, files.Count);
        Assert.Equal(
            Enumerable.Range(0, expectedFiles)
                .Select(index => Math.Min(
                    entryLimit,
                    entryCount - (index * entryLimit))),
            files.Select(file => file.Batches.Sum(batch => batch.Transactions.Count)));
        AssertMembership(files, entryCount);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(9_999, 1)]
    [InlineData(10_000, 1)]
    [InlineData(10_001, 2)]
    public async Task Ccd_AllocatesExactlyOneEntryPerBatch_AndSplitsAtTenThousandBatches(
        int entryCount,
        int expectedFiles)
    {
        var source = SourceBatch(20, "CCD", CreateTransactions(entryCount, withAddenda: true));

        var policies = await ResolvePoliciesAsync("CCD");
        var batchLimit = policies.Single().Services.Single(service => service.ServiceCode == "CCD").MaxBatchesPerFile!.Value;
        var files = CenitOutboundFilePartitioner.Partition([source], policies);

        Assert.Equal(expectedFiles, files.Count);
        Assert.All(files, file => Assert.InRange(
            file.Batches.Count,
            1,
            batchLimit));
        Assert.All(files.SelectMany(file => file.Batches), batch => Assert.Single(batch.Transactions));
        AssertMembership(files, entryCount);
    }

    [Fact]
    public async Task MixedOrdinaryAndCtx_ProducesIndependentProfilePartitions()
    {
        var ppd = SourceBatch(10, "PPD", CreateTransactions(2, withAddenda: false, firstId: 1));
        var ctx = SourceBatch(20, "CTX", CreateTransactions(2, withAddenda: true, firstId: 3));

        var policies = await ResolvePoliciesAsync("PPD", "CTX");
        var files = CenitOutboundFilePartitioner.Partition([ppd, ctx], policies);

        Assert.Equal(2, files.Count);
        Assert.Equal(CenitOrdinaryOutbound2026Layout.OriginalProfileCode, files[0].ProfileIdentity);
        Assert.Equal(["PPD"], files[0].ServiceCodes);
        Assert.Equal(CenitCtxOutbound2026Layout.OriginalProfileCode, files[1].ProfileIdentity);
        Assert.Equal(["CTX"], files[1].ServiceCodes);
        Assert.Single(files[1].Batches);
        Assert.Equal(2, files[1].Batches[0].Transactions.Count);
        AssertMembership(files, 4);
    }

    [Fact]
    public async Task MixedPpdAndCcd_SharesTheOrdinaryProfilePartition()
    {
        var ppd = SourceBatch(10, "PPD", CreateTransactions(1, withAddenda: false, firstId: 1));
        var ccd = SourceBatch(20, "CCD", CreateTransactions(1, withAddenda: true, firstId: 2));

        var file = Assert.Single(CenitOutboundFilePartitioner.Partition(
            [ppd, ccd],
            await ResolvePoliciesAsync("PPD", "CCD")));

        Assert.Equal(CenitOrdinaryOutbound2026Layout.OriginalProfileCode, file.ProfileIdentity);
        Assert.Equal(["PPD", "CCD"], file.ServiceCodes);
        AssertMembership([file], 2);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9_999)]
    public async Task Ctx_PreservesTransactionOwnedAddendas_AtSupportedBoundaries(int addendaCount)
    {
        var transaction = CreateTransaction(1, addendaCount);
        var source = SourceBatch(30, "CTX", [transaction]);

        var file = Assert.Single(CenitOutboundFilePartitioner.Partition(
            [source],
            await ResolvePoliciesAsync("CTX")));
        var emitted = Assert.Single(Assert.Single(file.Batches).Transactions);

        Assert.Same(transaction, emitted);
        Assert.Equal(addendaCount, emitted.Addendas.Count);
    }

    [Fact]
    public async Task Ctx_RejectsMoreThanNineThousandNineHundredNinetyNineAddendas()
    {
        var source = SourceBatch(30, "CTX", [CreateTransaction(1, 10_000)]);

        var policies = await ResolvePoliciesAsync("CTX");
        var exception = Assert.Throws<NachaGenerationException>(
            () => CenitOutboundFilePartitioner.Partition([source], policies));

        Assert.Equal("CENIT_CTX_ADDENDA_CARDINALITY_INVALID", exception.Code);
    }

    [Fact]
    public async Task Ccd_RejectsEntryWithoutMandatoryAddenda()
    {
        var source = SourceBatch(20, "CCD", CreateTransactions(1, withAddenda: false));

        var policies = await ResolvePoliciesAsync("CCD");
        var exception = Assert.Throws<NachaGenerationException>(
            () => CenitOutboundFilePartitioner.Partition([source], policies));

        Assert.Equal("CENIT_CCD_ADDENDA_REQUIRED", exception.Code);
    }

    [Fact]
    public async Task Ppd_PartitionBoundary_IsDrivenByResolvedPolicy()
    {
        var resolved = Assert.Single(await ResolvePoliciesAsync("PPD"));
        var services = resolved.Services
            .Select(service => service.ServiceCode == "PPD" ? service with { MaxEntriesPerFile = 2 } : service)
            .ToArray();
        var testPolicy = resolved with { Services = services };

        var files = CenitOutboundFilePartitioner.Partition(
            [SourceBatch(10, "PPD", CreateTransactions(3, withAddenda: false))],
            [testPolicy]);

        Assert.Equal(2, files.Count);
        Assert.Equal([2, 1], files.Select(file => file.Batches.Sum(batch => batch.Transactions.Count)));
    }

    private async Task<IReadOnlyList<NachaOutboundPartitionPolicy>> ResolvePoliciesAsync(params string[] serviceCodes)
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        var resolver = new NachaConfigResolver(context);
        var policies = new Dictionary<int, NachaOutboundPartitionPolicy>();
        foreach (var serviceCode in serviceCodes)
        {
            var result = await resolver.ResolveAsync(new NachaConfigResolutionRequest
            {
                ClearingHouseCode = "CENIT",
                FlowTypeCode = "ORIGINAL",
                DirectionCode = "SALIDA",
                ServiceClassCode = serviceCode,
                ProcessDateUtc = new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc),
                RequireOutboundPolicy = true
            });
            Assert.True(result.Success, string.Join(" | ", result.Trace));
            policies.TryAdd(result.Profile!.Id, result.OutboundPolicy!);
        }

        return policies.Values.OrderBy(policy => policy.FileOrder).ToArray();
    }

    private static CenitOutboundSourceBatch SourceBatch(
        int batchId,
        string serviceCode,
        IReadOnlyList<AchTransaction> transactions)
        => new(
            new AchBatch
            {
                Id = batchId,
                AchCycleId = "CENIT-CYCLE",
                CompanyName = "COMPANY",
                CompanyIdentification = "COMPANY-ID",
                CompanyEntryDescription = serviceCode,
                OriginOrOdfi = "00001001",
                Transactions = transactions.ToList()
            },
            serviceCode,
            transactions);

    private static IReadOnlyList<AchTransaction> CreateTransactions(
        int count,
        bool withAddenda,
        int firstId = 1)
        => Enumerable.Range(firstId, count)
            .Select(id => CreateTransaction(id, withAddenda ? 1 : 0))
            .ToArray();

    private static AchTransaction CreateTransaction(int id, int addendaCount)
    {
        var transaction = new AchTransaction
        {
            Id = id,
            Type = TransactionTypeEnum.Credit,
            Amount = 1m,
            ReceivingDFI = "00000000"
        };
        transaction.Addendas = Enumerable.Range(1, addendaCount)
            .Select(sequence => new AchTransactionAddenda
            {
                Id = sequence,
                AchTransactionId = id,
                Transaction = transaction,
                SequenceNumber = sequence
            })
            .ToList();
        return transaction;
    }

    private static void AssertMembership(
        IReadOnlyList<CenitOutboundFilePartition> files,
        int expectedEntryCount)
    {
        var transactionIds = files
            .SelectMany(file => file.Batches)
            .SelectMany(batch => batch.Transactions)
            .Select(transaction => transaction.Id)
            .ToArray();

        Assert.Equal(expectedEntryCount, transactionIds.Length);
        Assert.Equal(expectedEntryCount, transactionIds.Distinct().Count());
        Assert.Equal(Enumerable.Range(1, expectedEntryCount), transactionIds.OrderBy(id => id));
        AssertControlsUseEmittedMembership(files);
    }

    private static void AssertControlsUseEmittedMembership(IReadOnlyList<CenitOutboundFilePartition> files)
    {
        var calculator = new NachaControlTotalsCalculator();
        foreach (var file in files)
        {
            var generatedBatches = file.Batches
                .Select((_, index) => new AchBatch { Id = index + 1 })
                .ToArray();
            var transactionsByBatchId = file.Batches
                .Select((batch, index) => new { BatchId = index + 1, batch.Transactions })
                .ToDictionary(item => item.BatchId, item => item.Transactions);
            var addendasByBatchId = transactionsByBatchId.ToDictionary(
                item => item.Key,
                item => item.Value.Sum(transaction => transaction.Addendas.Count));
            var entryCount = transactionsByBatchId.Values.Sum(transactions => transactions.Count);
            var addendaCount = addendasByBatchId.Values.Sum();
            var controls = calculator.Calculate(new NachaControlTotalsRequest
            {
                Batches = generatedBatches,
                TransactionsByBatchId = transactionsByBatchId,
                AddendaRecordCountByBatchId = addendasByBatchId,
                EntryHashSourceFieldPath = "ReceivingDFI",
                BatchEntryHashLength = 10,
                FileEntryHashLength = 10,
                BatchEntryAddendaCountLength = 8,
                FileEntryAddendaCountLength = 8,
                BatchTotalDebitAmountLength = 18,
                FileTotalDebitAmountLength = 18,
                BatchTotalCreditAmountLength = 18,
                FileTotalCreditAmountLength = 18,
                BatchCountLength = 6,
                BlockCountLength = 6,
                PhysicalRecordCountBeforePadding = 2 + (generatedBatches.Length * 2) + entryCount + addendaCount,
                BlockSize = 10
            });

            Assert.Equal(generatedBatches.Length, controls.FileTotals.BatchCount);
            Assert.Equal(entryCount + addendaCount, controls.FileTotals.EntryAddendaCount);
            Assert.Equal(entryCount * 100L, controls.FileTotals.TotalCreditAmountInCents);
            Assert.Equal(0L, controls.FileTotals.TotalDebitAmountInCents);
            Assert.Equal(0, controls.FileTotals.PhysicalRecordCountAfterPadding % 10);
            Assert.All(controls.BatchTotals, batch =>
                Assert.Equal(batch.EntryDetailCount + batch.AddendaCount, batch.EntryAddendaCount));
        }
    }
}
