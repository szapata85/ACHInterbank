using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class CenitPublishedCardinalityPolicyTests : IClassFixture<OfficialNachaGenerationFixture>
{
    private readonly OfficialNachaGenerationFixture _fixture;

    public CenitPublishedCardinalityPolicyTests(OfficialNachaGenerationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task FreshPublication_ShouldContainCompleteSemanticMatrixAndPreservePredecessors()
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        var cases = new[]
        {
            Case("OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_3", "SALIDA", "ORIGINAL", "PPD", 1, 1, 1, 1),
            Case("OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_3", "SALIDA", "ORIGINAL", "CCD", 1, 1, 1, 1),
            Case("OFFICIAL_CENIT_SALIDA_PRENOTIFICACION_V1_3", "SALIDA", "PRENOTIFICACION", "PPD", 0, 1, 1, 1),
            Case("OFFICIAL_CENIT_SALIDA_PRENOTIFICACION_V1_3", "SALIDA", "PRENOTIFICACION", "CCD", 1, 1, 1, 1),
            Case("OFFICIAL_CENIT_CTX_SALIDA_PRENOTIFICACION_V1_3", "SALIDA", "PRENOTIFICACION", "CTX", 1, 1, 1, 1),
            Case("OFFICIAL_CENIT_ENTRADA_ORIGINAL_V1_2", "ENTRADA", "ORIGINAL", "PPD", 1, 1, 1, 1),
            Case("OFFICIAL_CENIT_ENTRADA_ORIGINAL_V1_2", "ENTRADA", "ORIGINAL", "CCD", 1, 1, 1, 1),
            Case("OFFICIAL_CENIT_ENTRADA_PRENOTIFICACION_V1_2", "ENTRADA", "PRENOTIFICACION", "PPD", 0, 1, 1, 1),
            Case("OFFICIAL_CENIT_ENTRADA_PRENOTIFICACION_V1_2", "ENTRADA", "PRENOTIFICACION", "CCD", 1, 1, 1, 1),
            Case("OFFICIAL_CENIT_CTX_ENTRADA_ORIGINAL_V1_2", "ENTRADA", "ORIGINAL", "CTX", 1, 9_999, 1, 9_999),
            Case("OFFICIAL_CENIT_CTX_ENTRADA_PRENOTIFICACION_V1_2", "ENTRADA", "PRENOTIFICACION", "CTX", 1, 1, 1, 1)
        };

        foreach (var testCase in cases)
        {
            var profile = await context.CfgProfiles.AsNoTracking().SingleAsync(item => item.ProfileCode == testCase.Code);
            var json = await context.HistConfigSnapshots.AsNoTracking()
                .Where(item => item.ProfileId == profile.Id && item.SnapshotType == "PUBLISH")
                .Select(item => item.SnapshotJson).SingleAsync();
            var read = NachaPublicationSnapshotSerializer.ReadForOrdinaryGeneration(json);
            read.IsSupported.Should().BeTrue($"{testCase.Code}: {read.Error}");
            var snapshot = read.Snapshot!;
            snapshot.SnapshotFormatVersion.Should().Be(1);
            snapshot.Profile.DirectionCode.Should().Be(testCase.Direction);
            snapshot.Profile.FlowTypeCode.Should().Be(testCase.Flow);
            var policy = NachaAddendaCardinalityMetadata.Resolve(snapshot.GenerationCriticalTags
                .Select(tag => new KeyValuePair<string, string>(tag.Key, tag.Value)));
            policy.Status.Should().Be(NachaAddendaCardinalityMetadataStatus.Resolved);
            var semantics = NachaTransactionCodeSemanticMetadata.Resolve(snapshot.Records);
            semantics.Status.Should().Be(NachaTransactionCodeSemanticMetadataStatus.Resolved);
            foreach (var direction in new[] { NachaEntryDirection.Credit, NachaEntryDirection.Debit })
            {
                semantics.Contract!.Rules.Should().Contain(rule => rule.Direction == direction
                    && rule.IsPrenotification == (testCase.Flow == "PRENOTIFICACION"));
                policy.Policy!.TryResolve(testCase.Service, direction, out var bounds).Should().BeTrue();
                bounds.Minimum.Should().Be(direction == NachaEntryDirection.Credit ? testCase.CreditMin : testCase.DebitMin);
                bounds.Maximum.Should().Be(direction == NachaEntryDirection.Credit ? testCase.CreditMax : testCase.DebitMax);
            }
        }

        var oldCodes = new[]
        {
            "OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_2",
            "OFFICIAL_CENIT_SALIDA_PRENOTIFICACION_V1_2",
            "OFFICIAL_CENIT_CTX_SALIDA_ORIGINAL_V1_2",
            "OFFICIAL_CENIT_CTX_SALIDA_PRENOTIFICACION_V1_2",
            "OFFICIAL_CENIT_ENTRADA_ORIGINAL_V1_1",
            "OFFICIAL_CENIT_ENTRADA_PRENOTIFICACION_V1_1",
            "OFFICIAL_CENIT_CTX_ENTRADA_ORIGINAL_V1_1",
            "OFFICIAL_CENIT_CTX_ENTRADA_PRENOTIFICACION_V1_1"
        };
        foreach (var code in oldCodes)
        {
            var profile = await context.CfgProfiles.AsNoTracking().SingleAsync(item => item.ProfileCode == code);
            var json = await context.HistConfigSnapshots.AsNoTracking()
                .Where(item => item.ProfileId == profile.Id && item.SnapshotType == "PUBLISH")
                .Select(item => item.SnapshotJson).SingleAsync();
            NachaPublicationSnapshotSerializer.ReadForOrdinaryGeneration(json).IsSupported.Should().BeTrue(code);
            json.Should().NotContain(NachaAddendaCardinalityMetadata.Prefix);
        }
    }

    [Fact]
    public async Task SuccessorSnapshot_ShouldRejectMissingAmbiguousAndMalformedCardinality()
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        var profile = await context.CfgProfiles.AsNoTracking().SingleAsync(item =>
            item.ProfileCode == "OFFICIAL_CENIT_ENTRADA_PRENOTIFICACION_V1_2");
        var json = await context.HistConfigSnapshots.AsNoTracking()
            .Where(item => item.ProfileId == profile.Id && item.SnapshotType == "PUBLISH")
            .Select(item => item.SnapshotJson).SingleAsync();
        var snapshot = NachaPublicationSnapshotSerializer.ReadForOrdinaryGeneration(json).Snapshot!;
        var ppdKey = NachaAddendaCardinalityMetadata.Prefix + "PPD";
        var ppd = snapshot.GenerationCriticalTags.Single(tag => tag.Key == ppdKey);
        var baseline = snapshot.GenerationCriticalTags.Where(tag => tag.Key != ppdKey).ToArray();
        var invalid = new IReadOnlyList<NachaPublicationSnapshotTag>[]
        {
            baseline,
            [..snapshot.GenerationCriticalTags, ppd],
            [..baseline, ppd with { Value = "Credit=0:1;Credit=1:1;Debit=1:1" }],
            [..baseline, ppd with { Value = "Credit=0:1" }],
            [..baseline, ppd with { Value = "Credit=2:1;Debit=1:1" }],
            [..baseline, ppd with { Value = "Credit=-1:1;Debit=1:1" }],
            [..baseline, ppd with { Value = "Credit=0:2;Debit=1:1" }],
            [..baseline, ppd with { Value = "Credit=bad:1;Debit=1:1" }],
            [..snapshot.GenerationCriticalTags, new NachaPublicationSnapshotTag(NachaAddendaCardinalityMetadata.Prefix + "XYZ", "Credit=1:1;Debit=1:1")],
            [..snapshot.GenerationCriticalTags, new NachaPublicationSnapshotTag(NachaAddendaCardinalityMetadata.Prefix + "PPD ", "Credit=1:1;Debit=1:1")],
            [..snapshot.GenerationCriticalTags, new NachaPublicationSnapshotTag(NachaAddendaCardinalityMetadata.RootPrefix + "Unknown", "value")],
            snapshot.GenerationCriticalTags.Select(tag => tag.Key == NachaAddendaCardinalityMetadata.DirectionKey
                ? tag with { Value = "SALIDA" } : tag).ToArray(),
            snapshot.GenerationCriticalTags.Select(tag => tag.Key == NachaAddendaCardinalityMetadata.FlowKey
                ? tag with { Value = "ORIGINAL" } : tag).ToArray()
        };
        foreach (var tags in invalid)
        {
            var read = NachaPublicationSnapshotSerializer.ReadForOrdinaryGeneration(
                NachaPublicationSnapshotSerializer.Serialize(snapshot with { GenerationCriticalTags = tags }));
            read.IsSupported.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Reseeding_ShouldPreserveEveryPublishedSnapshotByValue()
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        var before = await context.HistConfigSnapshots.AsNoTracking()
            .Where(item => item.SnapshotType == "PUBLISH")
            .ToDictionaryAsync(item => item.ProfileId, item => item.SnapshotJson);
        await new NachaConfigOfficialProfilesSeeder(context).SeedAsync();
        var after = await context.HistConfigSnapshots.AsNoTracking()
            .Where(item => item.SnapshotType == "PUBLISH")
            .ToDictionaryAsync(item => item.ProfileId, item => item.SnapshotJson);
        after.Should().BeEquivalentTo(before);
    }

    [Fact]
    public async Task Successors_ShouldPreservePredecessorPhysicalLayoutAndT6Semantics()
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        var pairs = new[]
        {
            ("OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_2", "OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_3"),
            ("OFFICIAL_CENIT_SALIDA_PRENOTIFICACION_V1_2", "OFFICIAL_CENIT_SALIDA_PRENOTIFICACION_V1_3"),
            ("OFFICIAL_CENIT_CTX_SALIDA_PRENOTIFICACION_V1_2", "OFFICIAL_CENIT_CTX_SALIDA_PRENOTIFICACION_V1_3"),
            ("OFFICIAL_CENIT_ENTRADA_ORIGINAL_V1_1", "OFFICIAL_CENIT_ENTRADA_ORIGINAL_V1_2"),
            ("OFFICIAL_CENIT_ENTRADA_PRENOTIFICACION_V1_1", "OFFICIAL_CENIT_ENTRADA_PRENOTIFICACION_V1_2"),
            ("OFFICIAL_CENIT_CTX_ENTRADA_ORIGINAL_V1_1", "OFFICIAL_CENIT_CTX_ENTRADA_ORIGINAL_V1_2"),
            ("OFFICIAL_CENIT_CTX_ENTRADA_PRENOTIFICACION_V1_1", "OFFICIAL_CENIT_CTX_ENTRADA_PRENOTIFICACION_V1_2")
        };
        foreach (var (predecessorCode, successorCode) in pairs)
        {
            var predecessor = await ReadAsync(context, predecessorCode);
            var successor = await ReadAsync(context, successorCode);
            successor.Profile.ClearingHouseCode.Should().Be(predecessor.Profile.ClearingHouseCode);
            successor.Profile.DirectionCode.Should().Be(predecessor.Profile.DirectionCode);
            successor.Profile.FlowTypeCode.Should().Be(predecessor.Profile.FlowTypeCode);
            successor.Profile.ServiceClassCode.Should().Be(predecessor.Profile.ServiceClassCode);
            successor.GenerationCriticalTags.Single(tag => tag.Key == "NormativeVersion").Value
                .Should().Be(predecessor.GenerationCriticalTags.Single(tag => tag.Key == "NormativeVersion").Value);
            var oldFields = predecessor.LayoutVariants.SelectMany(variant => variant.Fields.Select(field =>
                (variant.RecordCode, field.FieldCode, field.StartPosition, field.Length, field.FormatMask)))
                .OrderBy(item => item.RecordCode).ThenBy(item => item.FieldCode).ToArray();
            var newFields = successor.LayoutVariants.SelectMany(variant => variant.Fields.Select(field =>
                (variant.RecordCode, field.FieldCode, field.StartPosition, field.Length, field.FormatMask)))
                .OrderBy(item => item.RecordCode).ThenBy(item => item.FieldCode).ToArray();
            newFields.Should().Equal(oldFields);
            var oldSemantics = NachaTransactionCodeSemanticMetadata.Resolve(predecessor.Records).Contract!.Rules;
            var newSemantics = NachaTransactionCodeSemanticMetadata.Resolve(successor.Records).Contract!.Rules;
            newSemantics.Should().BeEquivalentTo(oldSemantics);
        }
    }

    [Fact]
    public async Task CanonicalResolution_ShouldSelectExactSuccessorsAndRetainCtxMonetaryPredecessor()
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        var cases = new[]
        {
            ("SALIDA", "ORIGINAL", "PPD", "OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_3", true),
            ("SALIDA", "ORIGINAL", "CCD", "OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_3", true),
            ("SALIDA", "PRENOTIFICACION", "PPD", "OFFICIAL_CENIT_SALIDA_PRENOTIFICACION_V1_3", true),
            ("SALIDA", "PRENOTIFICACION", "CCD", "OFFICIAL_CENIT_SALIDA_PRENOTIFICACION_V1_3", true),
            ("SALIDA", "ORIGINAL", "CTX", "OFFICIAL_CENIT_CTX_SALIDA_ORIGINAL_V1_2", false),
            ("SALIDA", "PRENOTIFICACION", "CTX", "OFFICIAL_CENIT_CTX_SALIDA_PRENOTIFICACION_V1_3", true),
            ("ENTRADA", "ORIGINAL", "PPD", "OFFICIAL_CENIT_ENTRADA_ORIGINAL_V1_2", true),
            ("ENTRADA", "ORIGINAL", "CCD", "OFFICIAL_CENIT_ENTRADA_ORIGINAL_V1_2", true),
            ("ENTRADA", "PRENOTIFICACION", "PPD", "OFFICIAL_CENIT_ENTRADA_PRENOTIFICACION_V1_2", true),
            ("ENTRADA", "PRENOTIFICACION", "CCD", "OFFICIAL_CENIT_ENTRADA_PRENOTIFICACION_V1_2", true),
            ("ENTRADA", "ORIGINAL", "CTX", "OFFICIAL_CENIT_CTX_ENTRADA_ORIGINAL_V1_2", true),
            ("ENTRADA", "PRENOTIFICACION", "CTX", "OFFICIAL_CENIT_CTX_ENTRADA_PRENOTIFICACION_V1_2", true)
        };
        var resolver = new NachaConfigResolver(context);
        foreach (var (direction, flow, service, code, hasCardinality) in cases)
        {
            var result = await resolver.ResolvePublishedOrdinaryAsync(new NachaConfigResolutionRequest
            {
                ClearingHouseCode = "CENIT",
                DirectionCode = direction,
                FlowTypeCode = flow,
                ServiceClassCode = service,
                ProcessDateUtc = new DateTime(2026, 8, 15),
                RequireOutboundPolicy = direction == "SALIDA"
            });
            result.Success.Should().BeTrue($"{direction}/{flow}/{service}");
            result.Profile!.ProfileCode.Should().Be(code);
            (result.CardinalityPolicy is not null).Should().Be(hasCardinality);
        }
    }

    [Fact]
    public async Task OutboundResolution_ShouldIgnoreLaterLiveCardinalityMutation()
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        var profile = await context.CfgProfiles.Include(item => item.Tags).SingleAsync(item =>
            item.ProfileCode == "OFFICIAL_CENIT_SALIDA_PRENOTIFICACION_V1_3");
        profile.Tags.Single(tag => tag.TagKey == NachaAddendaCardinalityMetadata.Prefix + "PPD")
            .TagValue = "Credit=1:1;Debit=1:1";
        await NachaConfigOutOfBandFixtureMutation.ApplyAsync(context);

        var result = await new NachaConfigResolver(context).ResolvePublishedOrdinaryAsync(
            new NachaConfigResolutionRequest
            {
                ClearingHouseCode = "CENIT",
                DirectionCode = "SALIDA",
                FlowTypeCode = "PRENOTIFICACION",
                ServiceClassCode = "PPD",
                ProcessDateUtc = new DateTime(2026, 8, 15),
                RequireOutboundPolicy = true,
                RequireCardinalityPolicy = true
            });
        result.Success.Should().BeTrue();
        result.Profile!.Id.Should().Be(profile.Id);
        result.CardinalityPolicy!.TryResolve("PPD", NachaEntryDirection.Credit, out var bounds).Should().BeTrue();
        bounds.Should().Be(new NachaAddendaBounds(0, 1));
        var transaction = BuildTransaction("23", 0);
        var partitionPolicy = result.OutboundPolicy! with
        {
            CardinalityPolicy = result.CardinalityPolicy,
            TransactionCodeContract = result.TransactionCodeContract
        };
        var file = CenitOutboundFilePartitioner.Partition(
            [new CenitOutboundSourceBatch(new AchBatch { Id = 1 }, "PPD", [transaction])],
            [partitionPolicy]);
        file.Should().ContainSingle();
    }

    [Theory]
    [InlineData("PPD", "23", 0, true)]
    [InlineData("PPD", "23", 1, true)]
    [InlineData("PPD", "23", 2, false)]
    [InlineData("PPD", "28", 0, false)]
    [InlineData("PPD", "28", 1, true)]
    [InlineData("PPD", "28", 2, false)]
    [InlineData("CCD", "23", 0, false)]
    [InlineData("CCD", "23", 1, true)]
    [InlineData("CCD", "23", 2, false)]
    [InlineData("CTX", "23", 0, false)]
    [InlineData("CTX", "23", 1, true)]
    [InlineData("CTX", "23", 2, false)]
    public async Task PublishedPartitioner_ShouldEnforcePrenotificationCardinality(
        string service, string code, int count, bool valid)
    {
        await using var context = await _fixture.CreateSeededContextAsync();
        var resolution = await new NachaConfigResolver(context).ResolvePublishedOrdinaryAsync(
            new NachaConfigResolutionRequest
            {
                ClearingHouseCode = "CENIT",
                DirectionCode = "SALIDA",
                FlowTypeCode = "PRENOTIFICACION",
                ServiceClassCode = service,
                ProcessDateUtc = new DateTime(2026, 8, 15),
                RequireOutboundPolicy = true,
                RequireCardinalityPolicy = true
            });
        resolution.Success.Should().BeTrue();
        var policy = resolution.OutboundPolicy! with
        {
            CardinalityPolicy = resolution.CardinalityPolicy,
            TransactionCodeContract = resolution.TransactionCodeContract
        };
        var source = new CenitOutboundSourceBatch(new AchBatch { Id = 1 }, service,
            [BuildTransaction(code, count)]);
        var action = () => CenitOutboundFilePartitioner.Partition([source], [policy]);
        if (valid)
        {
            action().Should().ContainSingle();
        }
        else
        {
            action.Should().Throw<NachaGenerationException>();
        }
    }

    private static AchTransaction BuildTransaction(string code, int count)
    {
        var transaction = new AchTransaction
        {
            Id = 1,
            Type = TransactionTypeEnum.Prenotification,
            TransactionCode = code,
            IsPrenotification = true,
            Amount = 0m
        };
        transaction.Addendas = Enumerable.Range(1, count).Select(sequence => new AchTransactionAddenda
        {
            Id = sequence,
            AchTransactionId = transaction.Id,
            Transaction = transaction,
            SequenceNumber = sequence
        }).ToList();
        return transaction;
    }

    private static async Task<NachaPublicationSnapshot> ReadAsync(AchDbContext context, string code)
    {
        var profile = await context.CfgProfiles.AsNoTracking().SingleAsync(item => item.ProfileCode == code);
        var json = await context.HistConfigSnapshots.AsNoTracking()
            .Where(item => item.ProfileId == profile.Id && item.SnapshotType == "PUBLISH")
            .Select(item => item.SnapshotJson).SingleAsync();
        var read = NachaPublicationSnapshotSerializer.ReadForOrdinaryGeneration(json);
        read.IsSupported.Should().BeTrue($"{code}: {read.Error}");
        return read.Snapshot!;
    }

    private static MatrixCase Case(string code, string direction, string flow, string service,
        int creditMin, int creditMax, int debitMin, int debitMax)
        => new(code, direction, flow, service, creditMin, creditMax, debitMin, debitMax);

    private sealed record MatrixCase(string Code, string Direction, string Flow, string Service,
        int CreditMin, int CreditMax, int DebitMin, int DebitMax);
}
