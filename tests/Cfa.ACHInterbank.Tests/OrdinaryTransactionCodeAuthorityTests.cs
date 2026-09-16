using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class OrdinaryTransactionCodeAuthorityTests : IClassFixture<OfficialNachaGenerationFixture>
{
    private readonly OfficialNachaGenerationFixture _fixture;

    public OrdinaryTransactionCodeAuthorityTests(OfficialNachaGenerationFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("ACH", TransactionTypeEnum.Credit, AccountTypeEnum.Checking, false, "22")]
    [InlineData("ACH", TransactionTypeEnum.Credit, AccountTypeEnum.Checking, true, "23")]
    [InlineData("ACH", TransactionTypeEnum.Debit, AccountTypeEnum.Checking, false, "27")]
    [InlineData("ACH", TransactionTypeEnum.Debit, AccountTypeEnum.Checking, true, "28")]
    [InlineData("ACH", TransactionTypeEnum.Credit, AccountTypeEnum.Savings, false, "32")]
    [InlineData("ACH", TransactionTypeEnum.Credit, AccountTypeEnum.Savings, true, "33")]
    [InlineData("ACH", TransactionTypeEnum.Debit, AccountTypeEnum.Savings, false, "37")]
    [InlineData("ACH", TransactionTypeEnum.Debit, AccountTypeEnum.Savings, true, "38")]
    [InlineData("ACH", TransactionTypeEnum.Credit, AccountTypeEnum.ElectronicDeposits, false, "52")]
    [InlineData("ACH", TransactionTypeEnum.Credit, AccountTypeEnum.ElectronicDeposits, true, "53")]
    [InlineData("ACH", TransactionTypeEnum.Debit, AccountTypeEnum.ElectronicDeposits, false, "55")]
    [InlineData("ACH", TransactionTypeEnum.Debit, AccountTypeEnum.ElectronicDeposits, true, "57")]
    [InlineData("CENIT", TransactionTypeEnum.Credit, AccountTypeEnum.Checking, false, "22")]
    [InlineData("CENIT", TransactionTypeEnum.Credit, AccountTypeEnum.Checking, true, "23")]
    [InlineData("CENIT", TransactionTypeEnum.Debit, AccountTypeEnum.Checking, false, "27")]
    [InlineData("CENIT", TransactionTypeEnum.Debit, AccountTypeEnum.Checking, true, "28")]
    [InlineData("CENIT", TransactionTypeEnum.Credit, AccountTypeEnum.Savings, false, "32")]
    [InlineData("CENIT", TransactionTypeEnum.Credit, AccountTypeEnum.Savings, true, "33")]
    [InlineData("CENIT", TransactionTypeEnum.Debit, AccountTypeEnum.Savings, false, "37")]
    [InlineData("CENIT", TransactionTypeEnum.Debit, AccountTypeEnum.Savings, true, "38")]
    [InlineData("CENIT", TransactionTypeEnum.Credit, AccountTypeEnum.ElectronicDeposits, false, "52")]
    [InlineData("CENIT", TransactionTypeEnum.Credit, AccountTypeEnum.ElectronicDeposits, true, "53")]
    [InlineData("CENIT", TransactionTypeEnum.Debit, AccountTypeEnum.ElectronicDeposits, false, "55")]
    [InlineData("CENIT", TransactionTypeEnum.Debit, AccountTypeEnum.ElectronicDeposits, true, "57")]
    public async Task PublishedAuthority_ShouldResolveSupportedOrdinaryMatrix(
        string chamber,
        TransactionTypeEnum type,
        AccountTypeEnum accountType,
        bool isPrenotification,
        string expected)
    {
        await using var db = await _fixture.CreateSeededContextAsync();
        var sut = new OrdinaryTransactionCodeAuthority(new NachaConfigResolver(db));

        var resolved = await sut.ResolveAsync(
            Context(chamber),
            new OrdinaryTransactionCodeSemanticRequest(type, accountType, isPrenotification));

        resolved.Should().Be(expected);
    }

    [Fact]
    public async Task PrenotificationAuthorities_ShouldRequireCrossFlowConvergence()
    {
        var resolver = new ResolverStub(request => Success(
            request,
            request.FlowTypeCode == "ORIGINAL" ? "23" : "99"));
        var sut = new OrdinaryTransactionCodeAuthority(resolver);

        var act = () => sut.ResolveAsync(
            Context("ACH"),
            new OrdinaryTransactionCodeSemanticRequest(TransactionTypeEnum.Credit, AccountTypeEnum.Checking, true));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ORDINARY_TXCODE_AUTHORITY_CONFLICT*");
    }

    [Fact]
    public async Task AchMonetaryAuthority_ShouldRequireMixedAndPureBatchServiceConvergence()
    {
        var resolver = new ResolverStub(request => Success(
            request,
            request.ServiceClassCode == "200" ? "22" : "27"));
        var sut = new OrdinaryTransactionCodeAuthority(resolver);

        var act = () => sut.ResolveAsync(
            Context("ACH"),
            new OrdinaryTransactionCodeSemanticRequest(TransactionTypeEnum.Credit, AccountTypeEnum.Checking, false));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ORDINARY_TXCODE_AUTHORITY_CONFLICT*");
        resolver.Requests.Select(request => request.ServiceClassCode).Should().Equal("200", "220");
    }

    [Theory]
    [InlineData("zero")]
    [InlineData("ambiguous")]
    [InlineData("snapshot")]
    [InlineData("contract")]
    [InlineData("tuple")]
    public async Task MissingOrAmbiguousAuthority_ShouldFailClosedWithoutTaxonomyFallback(string mode)
    {
        var resolver = new ResolverStub(request => mode switch
        {
            "zero" => Failure(NachaProfileSelectionStatus.ProfileNotFound),
            "ambiguous" => Failure(NachaProfileSelectionStatus.ProfileAmbiguous),
            "snapshot" => throw new InvalidOperationException("PUBLISH snapshot missing"),
            "contract" => Success(request, transactionCode: null),
            _ => Success(request, "32")
        });
        var sut = new OrdinaryTransactionCodeAuthority(resolver);

        var act = () => sut.ResolveAsync(
            Context("ACH"),
            new OrdinaryTransactionCodeSemanticRequest(TransactionTypeEnum.Credit, AccountTypeEnum.Checking, false));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CenitCombinedPolicy_ShouldResolveFinalPpdAuthorityForCcdSource()
    {
        var resolver = new ResolverStub(request => Success(request, "22"));
        var sut = new OrdinaryTransactionCodeAuthority(resolver);
        var context = Context("CENIT") with { SourceServiceClassCode = "CCD" };

        (await sut.ResolveAsync(context,
                new OrdinaryTransactionCodeSemanticRequest(TransactionTypeEnum.Credit, AccountTypeEnum.Checking, false)))
            .Should().Be("22");

        resolver.Requests.Select(request => request.ServiceClassCode).Should().Equal("CCD", "PPD");
        resolver.Requests.Should().OnlyContain(request => request.ClearingHouseCode == "CENIT");
    }

    [Fact]
    public async Task SharedBulkAuthorityContext_ShouldReusePublishedSnapshotResolution()
    {
        var resolver = new ResolverStub(request => Success(request, "22"));
        var sut = new OrdinaryTransactionCodeAuthority(resolver);
        var context = Context("CENIT");

        for (var index = 0; index < 50; index++)
        {
            (await sut.ResolveAsync(context,
                    new OrdinaryTransactionCodeSemanticRequest(TransactionTypeEnum.Credit, AccountTypeEnum.Checking, false)))
                .Should().Be("22");
        }

        resolver.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task CenitCtxPolicy_ShouldRemainIndependentlyReachableAcrossPrenoteFlows()
    {
        var resolver = new ResolverStub(request => Success(request, "23"));
        var sut = new OrdinaryTransactionCodeAuthority(resolver);
        var context = Context("CENIT") with { SourceServiceClassCode = "CTX" };

        (await sut.ResolveAsync(context,
                new OrdinaryTransactionCodeSemanticRequest(TransactionTypeEnum.Credit, AccountTypeEnum.Checking, true)))
            .Should().Be("23");

        resolver.Requests.Should().HaveCount(2)
            .And.OnlyContain(request => request.ServiceClassCode == "CTX");
        resolver.Requests.Select(request => request.FlowTypeCode)
            .Should().BeEquivalentTo(["ORIGINAL", "PRENOTIFICACION"]);
    }

    [Fact]
    public async Task PublishedSnapshot_ShouldRemainAuthorityAfterEquivalentLiveRuleMutation()
    {
        await using var db = await _fixture.CreateSeededContextAsync();
        var semantic = new OrdinaryTransactionCodeSemanticRequest(
            TransactionTypeEnum.Credit,
            AccountTypeEnum.Checking,
            false);
        var before = await new OrdinaryTransactionCodeAuthority(new NachaConfigResolver(db))
            .ResolveAsync(Context("ACH"), semantic);
        var liveRule = await db.CfgRuleSetRules.SingleAsync(rule =>
            rule.RuleSet.RuleSetCode == "NACHA_ACH_TRANSACTION_CODE_V1"
            && rule.RuleCode == "TRANSACTION_CODE_22");
        liveRule.RuleConfigJson = "{\"transactionCode\":\"99\",\"direction\":\"CREDIT\",\"accountType\":\"Checking\",\"isPrenotification\":false}";
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var after = await new OrdinaryTransactionCodeAuthority(new NachaConfigResolver(db))
            .ResolveAsync(Context("ACH"), semantic);

        before.Should().Be("22");
        after.Should().Be(before);
    }

    private static OrdinaryTransactionCodeAuthorityContext Context(string chamber)
        => new(
            chamber,
            chamber == "CENIT" ? 2 : 1,
            "CYCLE-1",
            new DateTime(2026, 8, 24, 0, 0, 0, DateTimeKind.Utc),
            "PPD");

    private static NachaConfigResolutionResult Success(
        NachaConfigResolutionRequest request,
        string? transactionCode)
    {
        var rules = transactionCode is null
            ? null
            : new NachaTransactionCodeSemanticContract(
            [
                new NachaTransactionCodeSemanticRule(
                    NachaEntryDirection.Credit,
                    transactionCode == "32" ? AccountTypeEnum.Savings : AccountTypeEnum.Checking,
                    request.FlowTypeCode == "PRENOTIFICACION" || transactionCode is "23" or "99",
                    transactionCode)
            ]);
        return new NachaConfigResolutionResult
        {
            Success = true,
            SelectionStatus = NachaProfileSelectionStatus.ProfileSelected,
            Profile = new CfgProfile { Id = 1, ProfileCode = $"{request.ClearingHouseCode}-{request.FlowTypeCode}" },
            TransactionCodeContract = rules,
            OutboundPolicy = request.ClearingHouseCode == "CENIT"
                ? string.Equals(request.ServiceClassCode, "CTX", StringComparison.OrdinalIgnoreCase)
                    ? new NachaOutboundPartitionPolicy(
                        "CENIT-CTX",
                        20,
                        NachaOutboundFileAllocation.IndependentServiceFiles,
                        [
                            new NachaOutboundServicePartitionPolicy(
                                "CTX", 10, NachaOutboundServicePartitionStrategy.PreserveSourceBatches)
                        ])
                    : new NachaOutboundPartitionPolicy(
                    "CENIT-ORDINARY",
                    10,
                    NachaOutboundFileAllocation.CombineServicePartitionsByIndex,
                    [
                        new NachaOutboundServicePartitionPolicy(
                            "PPD", 10, NachaOutboundServicePartitionStrategy.EntriesPerFile, MaxEntriesPerFile: 10_000),
                        new NachaOutboundServicePartitionPolicy(
                            "CCD", 20, NachaOutboundServicePartitionStrategy.FixedEntriesPerBatch, MaxBatchesPerFile: 10_000, EntriesPerBatch: 1)
                    ])
                : null
        };
    }

    private static NachaConfigResolutionResult Failure(NachaProfileSelectionStatus status)
        => new() { Success = false, SelectionStatus = status };

    private sealed class ResolverStub : INachaConfigResolver
    {
        private readonly Func<NachaConfigResolutionRequest, NachaConfigResolutionResult> _resolve;

        public ResolverStub(Func<NachaConfigResolutionRequest, NachaConfigResolutionResult> resolve)
        {
            _resolve = resolve;
        }

        public List<NachaConfigResolutionRequest> Requests { get; } = [];

        public Task<NachaConfigResolutionResult> ResolveAsync(
            NachaConfigResolutionRequest request,
            CancellationToken ct = default)
            => ResolvePublishedOrdinaryAsync(request, ct);

        public Task<NachaConfigResolutionResult> ResolvePublishedOrdinaryAsync(
            NachaConfigResolutionRequest request,
            CancellationToken ct = default)
        {
            Requests.Add(request);
            return Task.FromResult(_resolve(request));
        }

        public Task<NachaConfigResolutionResult> ResolvePublishedInboundAsync(
            NachaConfigResolutionRequest request,
            IReadOnlyList<string> physicalRecords,
            CancellationToken ct = default)
            => Task.FromResult(_resolve(request));
    }
}
