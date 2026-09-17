using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class OfficialNachaGenerationTableDrivenTests : IClassFixture<OfficialNachaGenerationFixture>
{
    private static readonly string[] OfficialRecordCodes = ["1", "5", "6", "7", "8", "9"];
    private readonly OfficialNachaGenerationFixture _fixture;

    public OfficialNachaGenerationTableDrivenTests(OfficialNachaGenerationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ReturnOutAchV35_ShouldRenderProfileVariantsFieldsRulesAndControls()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var result = await setup.Sut.BuildReturnOutAsync(BuildReturnOutRequest(), CancellationToken.None);
        var records = SplitRecords(result.Content);

        result.ProfileCode.Should().Be("OFFICIAL_ACH_SALIDA_DEVOLUCION_V35_1_0");
        result.NormativeVersion.Should().Be("V35");
        result.LegacyFallbackUsed.Should().BeFalse();
        records.Should().HaveCount(10);
        records.Should().OnlyContain(record => record.Length == 106);
        records.Take(6).Select(record => record[0]).Should().Equal('1', '5', '6', '7', '8', '9');
        records[0].Substring(35, 1).Should().Be("C");
        records[0].Substring(36, 3).Should().Be("106");
        records[0].Substring(39, 2).Should().Be("10");
        records[2].Substring(1, 2).Should().Be("26");
        records[2].Substring(3, 8).Should().Be("12345678");
        records[2].Substring(29, 18).Should().Be("000000000000123450");
        records[2].Substring(87, 15).Should().Be("876543210000001");
        records[3].Substring(1, 2).Should().Be("99");
        records[3].Substring(3, 3).Should().Be("R01");
        records[3].Substring(6, 15).Should().Be("123456780000099");
        records[3].Substring(21, 8).Should().Be("20260807");
        records[3].Substring(29, 8).Should().Be("87654321");
        records[3].Substring(81, 15).Should().Be("876543210000001");
        records[4].Substring(4, 6).Should().Be("000002");
        records[5].Substring(1, 6).Should().Be("000001");
        records[5].Substring(7, 6).Should().Be("000001");
        setup.LegacyRenderer.VerifyNoOtherCalls();
        setup.LegacyRecordProvider.VerifyNoOtherCalls();
        setup.Loader.Verify(x => x.LoadLayoutsAsync(It.IsAny<CancellationToken>()), Times.Never);
        setup.Loader.Verify(x => x.LoadDefinitionsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReturnOutAchV35_Prenote_ShouldRenderZeroAmount()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");
        var request = BuildReturnOutRequest();
        var entry = request.Batches[0].Entries[0] with { Amount = 0m, TransactionCode = "21" };

        var result = await setup.Sut.BuildReturnOutAsync(request with
        {
            Batches = [request.Batches[0] with { ServiceClassCode = "220", Entries = [entry] }]
        }, CancellationToken.None);

        SplitRecords(result.Content)[2].Substring(29, 18).Should().Be("000000000000000000");
    }

    [Fact]
    public async Task ReturnOutAchV35_WhenProfileInactive_ShouldFailClosedWithoutLegacyFallback()
    {
        await using var context = await SeedAsync();
        var profile = await context.CfgProfiles.SingleAsync(x => x.ProfileCode == "OFFICIAL_ACH_SALIDA_DEVOLUCION_V35_1_0");
        profile.StatusId = await context.CatConfigStatuses.Where(x => x.Code == "INACTIVO").Select(x => x.Id).SingleAsync();
        await context.SaveChangesAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var ex = await Assert.ThrowsAsync<NachaGenerationException>(() =>
            setup.Sut.BuildReturnOutAsync(BuildReturnOutRequest(), CancellationToken.None));

        ex.Code.Should().Be("NACHA_PROFILE_NOT_PUBLISHED");
        setup.LegacyRenderer.VerifyNoOtherCalls();
        setup.LegacyRecordProvider.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReturnOutAchV35_WhenCauseIsNotInAnnex9_ShouldFailClosed()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");
        var request = BuildReturnOutRequest();
        var invalidEntry = request.Batches[0].Entries[0] with { ReturnReasonCode = "DEV14" };

        var ex = await Assert.ThrowsAsync<NachaGenerationException>(() => setup.Sut.BuildReturnOutAsync(request with
        {
            Batches = [request.Batches[0] with { Entries = [invalidEntry] }]
        }, CancellationToken.None));

        ex.Code.Should().Be("NACHA_ALLOWED_VALUE_INVALID");
        setup.LegacyRenderer.VerifyNoOtherCalls();
        setup.LegacyRecordProvider.VerifyNoOtherCalls();
    }

    private static NachaReturnOutBuildRequest BuildReturnOutRequest()
    {
        var entry = new NachaReturnOutEntry(
            1, "26", "12345678", "0", "123456789", 1234.50m, "900123", "PERSONA UAT", "R",
            "876543210000001", "R01", "123456780000099", "20260807", "87654321", "CAUSAL UAT", "876543210000001");
        var batch = new NachaReturnOutBatch(
            "225", "USUARIO UAT", string.Empty, "9001234567", "PPD", "RETURN", new DateTime(2026, 8, 7),
            new DateTime(2026, 8, 7), string.Empty, "87654321", 1, [entry]);
        return new NachaReturnOutBuildRequest(
            new DateTime(2026, 8, 7, 14, 30, 0, DateTimeKind.Utc), "C", "000101006", "876543210",
            "ACH COLOMBIA", "CFA", "RETURN", [batch], PersistAudit: false);
    }

    [Fact]
    public async Task OfficialNachaGeneration_ShouldUsePublishedAchProfile()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var content = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        content.Should().NotBeNullOrWhiteSpace();
        SplitRecords(content).Select(x => x[0]).Should().Contain(['1', '5', '6', '7', '8', '9']);
        setup.LegacyRenderer.VerifyNoOtherCalls();
        setup.LegacyRecordProvider.VerifyNoOtherCalls();
        setup.Loader.Verify(x => x.LoadLayoutsAsync(It.IsAny<CancellationToken>()), Times.Never);
        setup.Loader.Verify(x => x.LoadDefinitionsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OrdinaryAchGeneration_Pre2E2UpgradeKeepsBytesAfterV1CoverageBackfill()
    {
        await using var context = await SeedAsync();
        var profile = await context.CfgProfiles.SingleAsync(item =>
            item.ProfileCode == AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode);
        var originalVersion = (profile.VersionMajor, profile.VersionMinor, profile.StatusId);
        var setup = CreateOfficialSut(context, "ACH Colombia");
        var before = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var published = await context.HistConfigSnapshots.SingleAsync(item => item.ProfileId == profile.Id && item.SnapshotType == "PUBLISH");
        context.HistConfigSnapshots.Remove(published);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await Assert.ThrowsAsync<InvalidOperationException>(() => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));
        await new NachaPublicationSnapshotCoverageSeeder(context).SeedAsync();
        var after = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        after.Should().Be(before);
        (await context.HistConfigSnapshots.CountAsync(item => item.ProfileId == profile.Id && item.SnapshotType == "PUBLISH"))
            .Should().Be(1);
        var reloaded = await context.CfgProfiles.AsNoTracking().SingleAsync(item => item.Id == profile.Id);
        (reloaded.VersionMajor, reloaded.VersionMinor, reloaded.StatusId).Should().Be(originalVersion);
    }

    [Fact]
    public async Task OfficialNachaGeneration_ShouldUsePublishedCenitProfile()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "CENIT");

        var content = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        content.Should().NotBeNullOrWhiteSpace();
        SplitRecords(content).Select(x => x[0]).Should().Contain(['1', '5', '6', '7', '8', '9']);
        content.Should().Contain("CENIT");
    }

    [Theory]
    [InlineData("ACH Colombia", AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode)]
    [InlineData("CENIT", CenitOrdinaryOutbound2026Layout.TxCodeAwareOriginalProfileCode)]
    public async Task TxCodeAwareSuccessorSelection_ShouldRejectPredecessorWithoutAuthority(
        string clearingHouseName,
        string successorProfileCode)
    {
        await using var context = await SeedAsync();
        var successor = await context.CfgProfiles.SingleAsync(profile => profile.ProfileCode == successorProfileCode);
        var publishedStatusId = successor.StatusId;
        var inactiveStatusId = await context.CatConfigStatuses.Where(status => status.Code == "INACTIVO")
            .Select(status => status.Id)
            .SingleAsync();
        successor.StatusId = inactiveStatusId;
        var cardinalitySuccessor = clearingHouseName == "CENIT"
            ? await context.CfgProfiles.SingleAsync(profile =>
                profile.ProfileCode == CenitOrdinaryOutbound2026Layout.CardinalityOriginalProfileCode)
            : null;
        if (cardinalitySuccessor is not null)
        {
            cardinalitySuccessor.StatusId = inactiveStatusId;
        }
        await context.SaveChangesAsync();
        var setup = CreateOfficialSut(context, clearingHouseName);
        var predecessorAct = () => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        (await predecessorAct.Should().ThrowAsync<NachaGenerationException>())
            .Which.Code.Should().Be("NACHA_TRANSACTION_CODE_CONTRACT_MISSING");

        successor.StatusId = publishedStatusId;
        if (cardinalitySuccessor is not null)
        {
            cardinalitySuccessor.StatusId = publishedStatusId;
        }
        await context.SaveChangesAsync();
        var successorContent = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        successorContent.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("PAGOS", "PPD", false)]
    [InlineData("PAGOS", "PPD", true)]
    [InlineData("CONCENTRA", "CCD", false)]
    public async Task CenitOrdinaryMay2026_ShouldRenderExactPhysicalAndControlContract(string description, string expectedSec, bool isDebit)
    {
        await using var context = await SeedAsync();
        var model = BuildContext("CENIT");
        model.Batches.Single().CompanyEntryDescription = description;
        model.Batches.Single().ServiceClassCode = isDebit ? "225" : "220";
        model.Transactions.Single().Amount = 1234567890123456.78m;
        model.Transactions.Single().Type = isDebit ? TransactionTypeEnum.Debit : TransactionTypeEnum.Credit;
        model.Transactions.Single().TransactionCode = isDebit ? "27" : "22";
        var setup = CreateOfficialSut(context, "CENIT", model);

        var content = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var records = SplitRecords(content);

        content.Should().NotContain("\r").And.NotContain("\n");
        records.Should().HaveCount(10).And.OnlyContain(record => record.Length == 106);
        records.Take(6).Select(record => record[0]).Should().Equal('1', '5', '6', '7', '8', '9');
        records.Skip(6).Should().OnlyContain(record => record == new string('9', 106));

        records[0].Substring(23, 8).Should().Be("20260524");
        records[0][35].Should().Be('A');
        records[0].Substring(36, 3).Should().Be("106");
        records[0].Substring(39, 2).Should().Be("10");
        records[0][41].Should().Be('1');

        records[1].Substring(50, 3).Should().Be(expectedSec);
        records[1].Substring(63, 8).Should().Be("20260524");
        records[1].Substring(71, 8).Should().Be("20260524");
        records[1].Substring(79, 3).Should().Be("144");

        records[2].Substring(29, 18).Should().Be("123456789012345678");
        records[2].Substring(47, 15).Should().Be("RCV001         ");
        records[2][86].Should().Be('1');
        records[2].Substring(87, 15).Should().Be("123456780000001");

        records[3].Substring(1, 2).Should().Be("05");
        records[3].Substring(83, 4).Should().Be("0001");
        records[3].Substring(87, 7).Should().Be("0000001");

        records[4].Substring(4, 6).Should().Be("000002");
        records[4].Substring(10, 10).Should().Be("0012345678");
        records[4].Substring(20, 18).Should().Be(isDebit ? "123456789012345678" : new string('0', 18));
        records[4].Substring(38, 18).Should().Be(isDebit ? new string('0', 18) : "123456789012345678");

        records[5].Substring(1, 6).Should().Be("000001");
        records[5].Substring(7, 6).Should().Be("000001");
        records[5].Substring(13, 8).Should().Be("00000002");
        records[5].Substring(21, 10).Should().Be("0012345678");
        records[5].Substring(31, 18).Should().Be(isDebit ? "123456789012345678" : new string('0', 18));
        records[5].Substring(49, 18).Should().Be(isDebit ? new string('0', 18) : "123456789012345678");
    }

    [Theory]
    [InlineData("PAGOS", "PPD", 1)]
    [InlineData("CONCENTRA", "CCD", 2)]
    public async Task CenitMultiFileBuilder_AllocatesServiceAwareBatches_AndCalculatesEmittedControls(
        string description,
        string serviceCode,
        int expectedBatchCount)
    {
        await using var context = await SeedAsync();
        var model = BuildContext("CENIT", batchCount: 2);
        var sourceBatch = model.Batches[0];
        sourceBatch.CompanyEntryDescription = description;
        var transactions = model.Transactions.OrderBy(transaction => transaction.Id).ToArray();
        foreach (var transaction in transactions)
        {
            transaction.AchBatchId = sourceBatch.Id;
            transaction.AchBatch = sourceBatch;
            transaction.Addendas =
            [
                new AchTransactionAddenda
                {
                    Id = transaction.Id,
                    AchTransactionId = transaction.Id,
                    Transaction = transaction,
                    Information = $"PAYMENT {transaction.Id}",
                    SequenceNumber = 1
                }
            ];
        }
        sourceBatch.Transactions = transactions;
        model = new NachaBuildContext
        {
            Cycle = model.Cycle,
            Batches = [sourceBatch],
            Transactions = transactions
        };
        var setup = CreateOfficialSut(context, "CENIT", model);
        var result = await setup.Sut.BuildNachaFilesByCycleAsync(model.Cycle.Id, CancellationToken.None);

        var artifact = result.Files.Should().ContainSingle().Subject;
        artifact.ProfileIdentity.Should().Be(CenitOrdinaryOutbound2026Layout.CardinalityOriginalProfileCode);
        artifact.ServiceCodes.Should().Equal(serviceCode);
        artifact.Batches.Should().HaveCount(expectedBatchCount);
        if (serviceCode == "CCD")
        {
            artifact.Batches.Should().OnlyContain(batch => batch.AchTransactionIds.Count == 1);
        }
        else
        {
            artifact.Batches.Should().ContainSingle().Which.AchTransactionIds.Should().HaveCount(2);
        }
        artifact.Batches.Select(batch => batch.SourceAchBatchId).Should().OnlyContain(batchId => batchId == sourceBatch.Id);
        artifact.AchTransactionIds.Should().BeEquivalentTo(transactions.Select(transaction => transaction.Id));

        var records = SplitRecords(artifact.Content);
        records.Should().HaveCount(10);
        records.Count(record => record[0] == '5').Should().Be(expectedBatchCount);
        records.Count(record => record[0] == '6').Should().Be(2);
        records.Count(record => record[0] == '7').Should().Be(2);
        records.Count(record => record[0] == '8').Should().Be(expectedBatchCount);
        records.Where(record => record[0] == '8').Should().OnlyContain(record =>
            record.Substring(4, 6) == (serviceCode == "CCD" ? "000002" : "000004"));
        var fileControl = records.First(record => record[0] == '9');
        fileControl.Substring(1, 6).Should().Be($"{expectedBatchCount:000000}");
        fileControl.Substring(13, 8).Should().Be("00000004");
    }

    [Fact]
    public async Task CenitCtxOfficialProfile_RendersMultipleEntriesAndOwnedAddendasInOneBatch()
    {
        await using var context = await SeedAsync();
        var model = BuildContext("CENIT", batchCount: 2);
        var sourceBatch = model.Batches[0];
        sourceBatch.CompanyEntryDescription = "CORPORATE";
        var transactions = model.Transactions.OrderBy(transaction => transaction.Id).ToArray();
        for (var transactionIndex = 0; transactionIndex < transactions.Length; transactionIndex++)
        {
            var transaction = transactions[transactionIndex];
            transaction.AchBatchId = sourceBatch.Id;
            transaction.AchBatch = sourceBatch;
            transaction.Addendas = Enumerable.Range(1, transactionIndex + 2)
                .Select(sequence => new AchTransactionAddenda
                {
                    Id = (transaction.Id * 10) + sequence,
                    AchTransactionId = transaction.Id,
                    Transaction = transaction,
                    AddendaType = "05",
                    Information = $"CTX PAYMENT {transaction.Id}-{sequence}",
                    SequenceNumber = sequence
                })
                .ToList();
        }
        sourceBatch.Transactions = transactions;
        model = new NachaBuildContext
        {
            Cycle = model.Cycle,
            Batches = [sourceBatch],
            Transactions = transactions
        };
        var setup = CreateOfficialSut(context, "CENIT", model);
        foreach (var customer in context.Customers)
        {
            customer.FirstName = "CTX";
            customer.LastName = "RECEIVER";
        }
        await context.SaveChangesAsync();

        var result = await setup.Sut.BuildNachaFilesByCycleAsync(model.Cycle.Id, CancellationToken.None);

        var artifact = result.Files.Should().ContainSingle().Subject;
        artifact.ProfileIdentity.Should().Be(CenitCtxOutbound2026Layout.TxCodeAwareOriginalProfileCode);
        artifact.ServiceCodes.Should().Equal("CTX");
        artifact.Batches.Should().ContainSingle().Which.AchTransactionIds.Should().HaveCount(2);

        var records = SplitRecords(artifact.Content);
        records.Should().OnlyContain(record => record.Length == 106);
        records.Take(11).Select(record => record[0]).Should().Equal('1', '5', '6', '7', '7', '6', '7', '7', '7', '8', '9');
        records.Skip(11).Should().OnlyContain(record => record == new string('9', 106));
        records[1].Substring(50, 3).Should().Be("CTX");

        records[2].Substring(62, 4).Should().Be("0002");
        records[2][86].Should().Be('1');
        records[3].Substring(83, 4).Should().Be("0001");
        records[4].Substring(83, 4).Should().Be("0002");
        records[3].Substring(87, 7).Should().Be("0000001");
        records[4].Substring(87, 7).Should().Be("0000001");

        records[5].Substring(62, 4).Should().Be("0003");
        records[6].Substring(83, 4).Should().Be("0001");
        records[7].Substring(83, 4).Should().Be("0002");
        records[8].Substring(83, 4).Should().Be("0003");
        records.Skip(6).Take(3).Should().OnlyContain(record => record.Substring(87, 7) == "0000002");

        records[9].Substring(4, 6).Should().Be("000007");
        records[9].Substring(10, 10).Should().Be("0024691356");
        records[9].Substring(20, 18).Should().Be(new string('0', 18));
        records[9].Substring(38, 18).Should().Be("000000000000020100");
        records[10].Substring(1, 6).Should().Be("000001");
        records[10].Substring(7, 6).Should().Be("000002");
        records[10].Substring(13, 8).Should().Be("00000007");
        records[10].Substring(21, 10).Should().Be("0024691356");
        records[10].Substring(31, 18).Should().Be(new string('0', 18));
        records[10].Substring(49, 18).Should().Be("000000000000020100");
    }

    [Fact]
    public async Task CenitCtxOfficialProfile_RejectsNonConsecutiveAddendaSequence()
    {
        await using var context = await SeedAsync();
        var model = BuildContext("CENIT");
        var transaction = model.Transactions.Single();
        model.Batches.Single().CompanyEntryDescription = "CORPORATE";
        transaction.Addendas =
        [
            new AchTransactionAddenda
            {
                Id = transaction.Id,
                AchTransactionId = transaction.Id,
                Transaction = transaction,
                Information = "CTX PAYMENT 2",
                SequenceNumber = 2
            }
        ];
        model.Batches.Single().Transactions = [transaction];
        var setup = CreateOfficialSut(context, "CENIT", model);

        var action = async () => await setup.Sut.BuildNachaFilesByCycleAsync(model.Cycle.Id, CancellationToken.None);

        var exception = await action.Should().ThrowAsync<NachaGenerationException>();
        exception.Which.Code.Should().Be("CENIT_CTX_ADDENDA_SEQUENCE_INVALID");
    }

    [Fact]
    public async Task OfficialNachaGeneration_ShouldNotFallbackToLegacy_WhenProfileMissing()
    {
        await using var context = await CreateContextAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var ex = await Assert.ThrowsAsync<NachaGenerationException>(() => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));

        ex.Code.Should().Be("NACHA_PROFILE_NOT_PUBLISHED");
        setup.Loader.Verify(x => x.LoadLayoutsAsync(It.IsAny<CancellationToken>()), Times.Never);
        setup.Loader.Verify(x => x.LoadDefinitionsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MissingRecord_ShouldReturn_NACHA_REQUIRED_RECORD_MISSING()
    {
        await using var context = await SeedAsync();
        await MutatePublishedSnapshotAsync(context, snapshot =>
        {
            foreach (var variant in snapshot["layoutVariants"]!.AsArray()
                         .Where(item => item!["recordCode"]!.GetValue<string>() == "7"))
            {
                variant!["statusCode"] = "INACTIVO";
            }
        });
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var ex = await Assert.ThrowsAsync<NachaGenerationException>(() => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));

        ex.Code.Should().Be("NACHA_REQUIRED_RECORD_MISSING");
    }

    [Fact]
    public async Task MissingRequiredField_ShouldReturn_NACHA_REQUIRED_FIELD_MISSING()
    {
        await using var context = await SeedAsync();
        await MutatePublishedSnapshotAsync(context, snapshot =>
            PublishedField(snapshot, "6", "AMOUNT")["isEnabled"] = false);
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var ex = await Assert.ThrowsAsync<NachaGenerationException>(() => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));

        ex.Code.Should().Be("NACHA_REQUIRED_FIELD_MISSING");
    }

    [Fact]
    public async Task FieldSourceNotFound_ShouldReturn_NACHA_FIELD_SOURCE_NOT_FOUND()
    {
        await using var context = await SeedAsync();
        await MutatePublishedSnapshotAsync(context, snapshot =>
            PublishedField(snapshot, "6", "AMOUNT")["source"]!["propertyPath"] = "CampoInexistente");
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var ex = await Assert.ThrowsAsync<NachaGenerationException>(() => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));

        ex.Code.Should().Be("NACHA_FIELD_SOURCE_NOT_FOUND");
    }

    [Fact]
    public async Task FieldExceedsLength_ShouldReturn_NACHA_FIELD_LENGTH_INVALID()
    {
        await using var context = await SeedAsync();
        await MutatePublishedSnapshotAsync(context, snapshot =>
            PublishedField(snapshot, "1", "IMMEDIATEDESTINATION")["source"]!["constantValue"] = "12345678901234567890");
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var ex = await Assert.ThrowsAsync<NachaGenerationException>(() => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));

        ex.Code.Should().Be("NACHA_FIELD_LENGTH_INVALID");
    }

    [Fact]
    public async Task CalculationFailure_ShouldReturn_NACHA_CALCULATION_FAILED()
    {
        await using var context = await SeedAsync();
        await MutatePublishedSnapshotAsync(context, snapshot =>
            PublishedField(snapshot, "9", "BLOCKCOUNT")["source"]!["expressionDsl"] =
                """{"source":"runtime","calculationType":"CalculationNotAvailable"}""");
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var ex = await Assert.ThrowsAsync<NachaGenerationException>(() => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));

        ex.Code.Should().Be("NACHA_CALCULATION_FAILED");
    }

    [Fact]
    public async Task ChangingPublishedAchColombiaLiveField_ShouldNotAffectEitherFile()
    {
        await using var context = await SeedAsync();
        var achSetup = CreateOfficialSut(context, "ACH Colombia");
        var cenitSetup = CreateOfficialSut(context, "CENIT");
        var achBefore = await achSetup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var cenitBefore = await cenitSetup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        var achField = await LoadFieldAsync(context, AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode, "1", "IMMEDIATEORIGINNAME");
        achField.SourceDefinition.ConstantValue = "ACH-CAMBIO-UAT";
        await context.SaveChangesAsync();

        var achAfter = await achSetup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var cenitAfter = await cenitSetup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        achAfter.Should().Be(achBefore);
        cenitAfter.Should().Be(cenitBefore);
    }

    [Fact]
    public async Task ChangingPublishedCenitLiveField_ShouldNotAffectEitherFile()
    {
        await using var context = await SeedAsync();
        var achSetup = CreateOfficialSut(context, "ACH Colombia");
        var cenitSetup = CreateOfficialSut(context, "CENIT");
        var achBefore = await achSetup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var cenitBefore = await cenitSetup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        var cenitField = await LoadFieldAsync(context, CenitOrdinaryOutbound2026Layout.TxCodeAwareOriginalProfileCode, "1", "IMMEDIATEORIGINNAME");
        cenitField.SourceDefinition.ConstantValue = "CENIT-CAMBIO";
        await context.SaveChangesAsync();

        var achAfter = await achSetup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var cenitAfter = await cenitSetup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        achAfter.Should().Be(achBefore);
        cenitAfter.Should().Be(cenitBefore);
    }

    [Fact]
    public async Task OrdinaryRuntime_ShouldUseStructurallyValidPublishedVariantWithoutStaticIdentityRecognition()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");
        var before = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        await MutatePublishedSnapshotAsync(context, snapshot =>
            PublishedVariant(snapshot, "1")["variantCode"] = "SNAPSHOT_ONLY_T1");

        var after = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        after.Should().Be(before);
    }

    [Fact]
    public async Task OrdinaryRuntime_ShouldFailClosed_WhenPublishedFieldExceedsRecordBounds()
    {
        await using var context = await SeedAsync();
        await MutatePublishedSnapshotAsync(context, snapshot =>
        {
            var field = PublishedField(snapshot, "1", "IMMEDIATEORIGINNAME");
            field["startPosition"] = 106;
            field["length"] = 2;
        });
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));

        ex.Message.Should().Contain("LegacyOrIncomplete");
    }

    [Fact]
    public async Task OrdinaryRuntime_ShouldFailClosed_WhenPublishedFieldsOverlap()
    {
        await using var context = await SeedAsync();
        await MutatePublishedSnapshotAsync(context, snapshot =>
            PublishedField(snapshot, "1", "IMMEDIATEORIGINNAME")["startPosition"] = 40);
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));

        ex.Message.Should().Contain("LegacyOrIncomplete");
    }

    [Fact]
    public async Task OfficialGeneration_ShouldGenerateNonEmptyFile_ForAchColombia()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var content = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        content.Length.Should().BeGreaterThan(0);
        (content.Length % 106).Should().Be(0);
    }

    [Fact]
    public async Task OfficialGeneration_ShouldGenerateNonEmptyFile_ForCenit()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "CENIT");

        var content = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        content.Length.Should().BeGreaterThan(0);
        (content.Length % 106).Should().Be(0);
    }

    [Fact]
    public async Task OfficialGeneration_ShouldEmitTrace_ForAchColombia()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var trace = await LoadLatestTraceAsync(context);

        trace.Mode.Should().Be("TABLE_DRIVEN");
        trace.ProfileCode.Should().Be(AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode);
        trace.FieldTraceEntries.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OfficialGeneration_ShouldEmitTrace_ForCenit()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "CENIT");

        await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var trace = await LoadLatestTraceAsync(context);

        trace.Mode.Should().Be("TABLE_DRIVEN");
        trace.ProfileCode.Should().Be(CenitOrdinaryOutbound2026Layout.CardinalityOriginalProfileCode);
        trace.ClearingHouseCode.Should().Be("CENIT");
        trace.Trace.Should().Contain(entry => entry.Contains(
            $"SettlementPolicy resuelta desde CfgProfileTag: {NachaSettlementPolicy.JulianSettlementDate}",
            StringComparison.Ordinal));
        trace.Trace.Should().NotContain(entry => entry.Contains("CENIT_BLANK_ONLY", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Trace_ShouldIncludeProfileInformation()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var trace = await LoadLatestTraceAsync(context);

        trace.ProfileId.Should().NotBeNull();
        trace.ProfileCode.Should().Be(AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode);
        trace.ProfileVersion.Should().Be("35.1");
        trace.ProfileStatus.Should().Be("PUBLICADO");
        trace.EffectiveDate.Should().Be(new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Trace_ShouldIncludeClearingHouseInformation()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "CENIT");

        await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var trace = await LoadLatestTraceAsync(context);

        trace.ClearingHouseCode.Should().Be("CENIT");
        trace.ClearingHouseName.Should().Be("CENIT");
    }

    [Fact]
    public async Task Trace_ShouldIncludeRecords_1_5_6_7_8_9()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var trace = await LoadLatestTraceAsync(context);

        trace.FieldTraceEntries.Select(x => x.RecordType).Distinct().Should().Contain(OfficialRecordCodes);
    }

    [Fact]
    public async Task Trace_ShouldIncludeEveryRenderedField()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var trace = await LoadLatestTraceAsync(context);

        trace.TotalFields.Should().Be(trace.FieldTraceEntries.Count);
        trace.FieldTraceEntries.Where(x => x.FieldName != "PADDING_RECORD").Should().OnlyContain(x => x.ValidationStatus == "Ok");
        trace.FieldTraceEntries.Count(x => x.RecordType == "6").Should().BeGreaterThanOrEqualTo(11);
    }

    [Fact]
    public async Task Trace_ShouldLinkCfgLayoutField_ToRenderedValue()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var trace = await LoadLatestTraceAsync(context);

        var amount = trace.FieldTraceEntries.Single(x => x.RecordType == "6" && x.FieldName == "AMOUNT");
        amount.FieldDefinitionId.Should().BeGreaterThan(0);
        amount.RawValueSanitized.Should().StartWith("[REDACTED;Category=FINANCIAL;");
        amount.RenderedValue.Should().StartWith("[REDACTED;Category=FINANCIAL;");
        amount.RenderedLength.Should().Be(18);
    }

    [Fact]
    public async Task Trace_ShouldIncludePositionAndLength()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var trace = await LoadLatestTraceAsync(context);

        var traceNumber = trace.FieldTraceEntries.Single(x => x.RecordType == "6" && x.FieldName == "TRACENUMBER");
        traceNumber.PositionStart.Should().Be(88);
        traceNumber.PositionEnd.Should().Be(102);
        traceNumber.Length.Should().Be(15);
        traceNumber.RenderedLength.Should().Be(15);
    }

    [Fact]
    public async Task Trace_ShouldIncludeSourceTypeAndSourceFieldPath()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var trace = await LoadLatestTraceAsync(context);

        var entry = trace.FieldTraceEntries.Single(x => x.RecordType == "6" && x.FieldName == "DFIACCOUNTNUMBER");
        entry.SourceType.Should().Be("SOURCE_FIELD");
        entry.SourceFieldPath.Should().Be("DestinationAccountNumber");
    }

    [Fact]
    public async Task Trace_ShouldIncludeCalculatedFields()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var trace = await LoadLatestTraceAsync(context);

        trace.FieldTraceEntries.Should().Contain(x => x.SourceType == "CALCULATED" && x.CalculationType == "ENTRYHASH");
        trace.FieldTraceEntries.Should().Contain(x => x.SourceType == "CALCULATED" && x.CalculationType == "BLOCKCOUNT");
        trace.FieldTraceEntries.Should().Contain(x => x.SourceType == "CALCULATED" && x.CalculationType == "FILEIDMODIFIER");
    }

    [Fact]
    public async Task Trace_ShouldIncludeEntryHashCalculation()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var trace = await LoadLatestTraceAsync(context);

        trace.FieldTraceEntries.Where(x => x.FieldName == "ENTRYHASH")
            .Should().OnlyContain(x => x.SourceType == "CALCULATED"
                && x.RawValueSanitized!.StartsWith("[REDACTED;Category=CORRELATABLE;", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Trace_ShouldIncludeBlockCountCalculation()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var trace = await LoadLatestTraceAsync(context);

        trace.FieldTraceEntries.Single(x => x.RecordType == "9" && x.FieldName == "BLOCKCOUNT")
            .CalculationType.Should().Be("BLOCKCOUNT");
    }

    [Fact]
    public async Task Trace_ShouldIncludeFileIdModifierCalculation()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var trace = await LoadLatestTraceAsync(context);

        trace.FieldTraceEntries.Single(x => x.RecordType == "1" && x.FieldName == "FILEIDMODIFIER")
            .RenderedValue.Should().Be("A");
    }

    [Fact]
    public async Task GenerateOfficialFile_ShouldRenderBatchControlTotalsFromCalculator()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var content = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var batchControl = SplitRecords(content).Single(x => x[0] == '8');

        batchControl.Substring(4, 6).Should().Be("000002");
        batchControl.Substring(10, 10).Should().Be("0012345678");
        batchControl.Substring(20, 18).Should().Be("000000000000000000");
        batchControl.Substring(38, 18).Should().Be("000000000000010000");
    }

    [Fact]
    public async Task GenerateOfficialFile_ShouldRenderFileControlTotalsFromCalculator()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var content = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var fileControl = SplitRecords(content).First(x => x[0] == '9' && x.Substring(1, 6) != "999999");

        fileControl.Substring(1, 6).Should().Be("000001");
        fileControl.Substring(7, 6).Should().Be("000001");
        fileControl.Substring(13, 8).Should().Be("00000002");
        fileControl.Substring(21, 10).Should().Be("0012345678");
        fileControl.Substring(31, 18).Should().Be("000000000000000000");
        fileControl.Substring(49, 18).Should().Be("000000000000010000");
    }

    [Fact]
    public async Task GenerateFile_ShouldAddPaddingRecordsWhenRequired()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var content = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var records = SplitRecords(content);

        records.Count.Should().Be(10);
        records.TakeLast(4).Should().OnlyContain(x => x == new string('9', 106));
    }

    [Fact]
    public async Task Trace_ShouldIncludeBatchTotals()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var trace = await LoadLatestTraceAsync(context);

        trace.Phase.Should().Be("6B.3B");
        trace.BatchTotals.Should().ContainSingle(x =>
            x.BatchId == 100 &&
            x.EntryAddendaCount == 2 &&
            x.EntryHash == 12345678 &&
            x.TotalCreditAmountInCents == 10000 &&
            x.TotalDebitAmountInCents == 0 &&
            x.EntryDetailCount == 1 &&
            x.AddendaCount == 1);
    }

    [Fact]
    public async Task Trace_ShouldIncludeFileTotals()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var trace = await LoadLatestTraceAsync(context);

        trace.FileTotals.Should().NotBeNull();
        trace.FileTotals!.BatchCount.Should().Be(1);
        trace.FileTotals.EntryAddendaCount.Should().Be(2);
        trace.FileTotals.EntryHash.Should().Be(12345678);
        trace.FileTotals.TotalCreditAmountInCents.Should().Be(10000);
    }

    [Fact]
    public async Task Trace_ShouldIncludeBlockCountAndPaddingCount()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var trace = await LoadLatestTraceAsync(context);

        trace.FileTotals!.BlockCount.Should().Be(1);
        trace.FileTotals.PhysicalRecordCountBeforePadding.Should().Be(6);
        trace.FileTotals.PaddingRecordCount.Should().Be(4);
        trace.FileTotals.PhysicalRecordCountAfterPadding.Should().Be(10);
    }

    [Fact]
    public async Task Trace_ShouldIncludeFileIdModifierResolution()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var trace = await LoadLatestTraceAsync(context);

        trace.FileIdModifier.Should().NotBeNull();
        trace.FileIdModifier!.DailySequence.Should().Be(1);
        trace.FileIdModifier.ResolvedValue.Should().Be("A");
    }

    [Fact]
    public async Task ValidateOfficialLayout_ShouldCompareCalculatedVsRenderedTotals()
    {
        await using var context = await SeedAsync();
        await MutatePublishedSnapshotAsync(context, snapshot =>
            PublishedField(snapshot, "8", "ENTRYHASH")["source"]!["expressionDsl"] =
                JsonSerializer.Serialize(new { source = "runtime", calculationType = "BatchNumber" }));
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var ex = await Assert.ThrowsAsync<NachaGenerationException>(() => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));

        ex.Code.Should().Be("NACHA_CONTROL_TOTAL_MISMATCH");
    }

    [Fact]
    public async Task Trace_ShouldMarkLegacyFallbackUsedFalse()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var trace = await LoadLatestTraceAsync(context);

        trace.LegacyFallbackUsed.Should().BeFalse();
        trace.LegacyRecordCodes.Should().BeEmpty();
    }

    [Fact]
    public async Task Trace_ShouldCaptureFieldLengthError()
    {
        await using var context = await SeedAsync();
        await MutatePublishedSnapshotAsync(context, snapshot =>
            PublishedField(snapshot, "1", "IMMEDIATEDESTINATION")["source"]!["constantValue"] = "12345678901234567890");
        var setup = CreateOfficialSut(context, "ACH Colombia");

        await Assert.ThrowsAsync<NachaGenerationException>(() => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));
        var trace = await LoadLatestTraceAsync(context);

        trace.Status.Should().Be("Failed");
        trace.ErrorCode.Should().Be("NACHA_FIELD_LENGTH_INVALID");
        trace.FieldTraceEntries.Should().Contain(x => x.FieldName == "IMMEDIATEDESTINATION" && x.ErrorCode == "NACHA_FIELD_LENGTH_INVALID");
    }

    [Fact]
    public async Task Trace_ShouldCaptureMissingRequiredFieldError()
    {
        await using var context = await SeedAsync();
        await MutatePublishedSnapshotAsync(context, snapshot =>
            PublishedField(snapshot, "6", "AMOUNT")["isEnabled"] = false);
        var setup = CreateOfficialSut(context, "ACH Colombia");

        await Assert.ThrowsAsync<NachaGenerationException>(() => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));
        var trace = await LoadLatestTraceAsync(context);

        trace.Status.Should().Be("Failed");
        trace.ErrorCode.Should().Be("NACHA_REQUIRED_FIELD_MISSING");
        trace.FieldTraceEntries.Should().Contain(x => x.RecordType == "6" && x.FieldName == "AMOUNT" && x.ErrorCode == "NACHA_REQUIRED_FIELD_MISSING");
    }

    [Fact]
    public async Task Trace_ShouldNotContainSecrets()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var traceJson = JsonSerializer.Serialize(await LoadLatestTraceAsync(context));

        traceJson.Should().NotContain("Bearer ");
        traceJson.Should().NotContain("Authorization");
        traceJson.Should().NotContain("BEGIN PRIVATE");
        traceJson.Should().NotContain("password");
    }

    [Fact]
    public async Task Trace_ShouldNotAllowReconstructingSensitiveLineFromEntries()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var trace = await LoadLatestTraceAsync(context);
        trace.FieldTraceEntries
            .Where(entry => entry.RenderedValue?.StartsWith("[REDACTED", StringComparison.Ordinal) == true)
            .Should().NotBeEmpty();
        trace.FieldTraceEntries.Should().OnlyContain(entry => entry.RuntimeRenderedValue == null);
    }

    [Fact]
    public async Task AchAndCenitTrace_ShouldReferenceDifferentProfiles()
    {
        await using var context = await SeedAsync();
        await CreateOfficialSut(context, "ACH Colombia").Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var achTrace = await LoadLatestTraceAsync(context);
        await CreateOfficialSut(context, "CENIT").Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var cenitTrace = await LoadLatestTraceAsync(context);

        achTrace.ProfileCode.Should().NotBe(cenitTrace.ProfileCode);
        achTrace.ProfileId.Should().NotBe(cenitTrace.ProfileId);
    }

    [Fact]
    public async Task PublishedAchTrace_ShouldIgnoreLiveFieldChange()
    {
        await using var context = await SeedAsync();
        var achField = await LoadFieldAsync(context, AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode, "1", "IMMEDIATEORIGINNAME");
        achField.SourceDefinition.ConstantValue = "ACH-CAMBIO-UAT";
        await context.SaveChangesAsync();

        await CreateOfficialSut(context, "ACH Colombia").Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var achTrace = await LoadLatestTraceAsync(context);
        await CreateOfficialSut(context, "CENIT").Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var cenitTrace = await LoadLatestTraceAsync(context);

        achTrace.FieldTraceEntries.Single(x => x.RecordType == "1" && x.FieldName == "IMMEDIATEORIGINNAME")
            .RawValueSanitized.Should().Contain("Length=7");
        cenitTrace.FieldTraceEntries.Single(x => x.RecordType == "1" && x.FieldName == "IMMEDIATEORIGINNAME")
            .RawValueSanitized.Should().NotContain("Length=14");
    }

    [Fact]
    public async Task PublishedCenitTrace_ShouldIgnoreLiveFieldChange()
    {
        await using var context = await SeedAsync();
        var cenitField = await LoadFieldAsync(context, CenitOrdinaryOutbound2026Layout.TxCodeAwareOriginalProfileCode, "1", "IMMEDIATEORIGINNAME");
        cenitField.SourceDefinition.ConstantValue = "CENIT-CAMBIO";
        await context.SaveChangesAsync();

        await CreateOfficialSut(context, "ACH Colombia").Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var achTrace = await LoadLatestTraceAsync(context);
        await CreateOfficialSut(context, "CENIT").Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var cenitTrace = await LoadLatestTraceAsync(context);

        achTrace.FieldTraceEntries.Single(x => x.RecordType == "1" && x.FieldName == "IMMEDIATEORIGINNAME")
            .RawValueSanitized.Should().NotContain("Length=12");
        cenitTrace.FieldTraceEntries.Single(x => x.RecordType == "1" && x.FieldName == "IMMEDIATEORIGINNAME")
            .RawValueSanitized.Should().Contain("Length=7");
    }

    [Fact]
    public async Task AchColV35_ShouldRenderCriticalOffsetsAndPhysicalRules()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var content = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        await using var stream = new MemoryStream(Encoding.ASCII.GetBytes(content));
        var records = await NachaParserService.ReadPhysicalRecordsAsync(stream, CancellationToken.None);

        content.Length.Should().Be(1060);
        content.Should().NotContain("\r").And.NotContain("\n").And.NotContain("UAT6B2");
        records.Should().HaveCount(10);
        records.Should().OnlyContain(record => record.Length == 106);
        records.Select(record => record[0]).Should().ContainInOrder('1', '5', '6', '7', '8', '9');
        records.Skip(6).Should().OnlyContain(record => record == new string('9', 106));

        var type1 = records[0];
        type1.Substring(3, 10).Should().Be(" 000101006");
        type1.Substring(13, 10).Should().Be(" 000128300");
        type1.Substring(23, 8).Should().Be("20260524");
        type1.Substring(31, 4).Should().Be("1400");
        type1.Substring(35, 1).Should().Be("A");
        type1.Substring(36, 3).Should().Be("106");
        type1.Substring(39, 2).Should().Be("10");
        type1.Substring(41, 1).Should().Be("1");
        type1.Substring(88, 8).Should().Be(new string(' ', 8));
        type1.Substring(96, 10).Should().Be(new string(' ', 10));

        var type5 = records[1];
        type5.Substring(63, 8).Should().Be("20260524");
        type5.Substring(71, 8).Should().Be("20260524");
        type5.Substring(82, 1).Should().Be("1");
        type5.Substring(83, 8).Should().Be("12345678");
        type5.Substring(91, 7).Should().Be("0000001");
        type5.Substring(98, 8).Should().Be(new string(' ', 8));

        var type6 = records[2];
        type6.Substring(29, 18).Should().Be("000000000000010000");
        type6.Substring(47, 15).TrimEnd().Should().Be("RCV001");
        type6.Substring(62, 22).TrimEnd().Should().Be("PERSONA SINTETICA 10");
        type6.Substring(86, 1).Should().Be("1");
        type6.Substring(87, 15).Should().Be("123456780000001");
        type6.Substring(102, 4).Should().Be(new string(' ', 4));

        var type7 = records[3];
        type7.Substring(1, 2).Should().Be("05");
        type7.Substring(3, 15).TrimEnd().Should().Be("9001234567");
        type7.Substring(18, 2).Should().Be("  ");
        type7.Substring(30, 24).Should().Be(new string('0', 24));
        type7.Substring(54, 2).Should().Be("  ");
        type7.Substring(56, 24).Should().Be(new string('0', 24));
        type7.Substring(80, 3).Should().Be("   ");
        type7.Substring(83, 4).Should().Be("0001");
        type7.Substring(87, 7).Should().Be(type6.Substring(95, 7));

        var type8 = records[4];
        type8.Substring(20, 18).Should().Be("000000000000000000");
        type8.Substring(38, 18).Should().Be("000000000000010000");
        type8.Substring(99, 7).Should().Be(type5.Substring(91, 7));

        var type9 = records[5];
        type9.Substring(31, 18).Should().Be("000000000000000000");
        type9.Substring(49, 18).Should().Be("000000000000010000");
        type9.Substring(67, 39).Should().Be(new string(' ', 39));
    }

    [Theory]
    [InlineData("CREDIT", "22", false, AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode, "AB09B7C3F3422EF810D4AF41F389E5D4B26651804005D7943F09B8A7AD885724")]
    [InlineData("DEBIT", "27", false, AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode, "33F0E773FDF02E6211DB0652D426556DFB24F5627492E243549E9C77C1B1EF16")]
    [InlineData("CREDIT", "23", true, AchColOfficialNachaLayout.TxCodeAwareOutboundPrenotificationProfileCode, "A3D1FCD5A488243219687146AFC6799984A7E40B82E25B8E30D6BE7DE85E49B8")]
    [InlineData("DEBIT", "28", true, AchColOfficialNachaLayout.TxCodeAwareOutboundPrenotificationProfileCode, "501236C903A15CF7BC340E2C7B5371047D246839601171412E192473135B2C40")]
    public async Task AchColV35_ShouldGenerateEverySupportedOutboundOrdinaryFamily(
        string businessType,
        string transactionCode,
        bool isPrenotification,
        string expectedProfileCode,
        string expectedSha256)
    {
        await using var context = await SeedAsync();
        var model = BuildContext("ACH Colombia");
        var transaction = model.Transactions.Single();
        transaction.Type = isPrenotification
            ? TransactionTypeEnum.Prenotification
            : businessType == "DEBIT" ? TransactionTypeEnum.Debit : TransactionTypeEnum.Credit;
        transaction.IsPrenotification = isPrenotification;
        transaction.TransactionCode = transactionCode;
        transaction.Amount = isPrenotification ? 0m : 100m;
        transaction.AchBatch!.ServiceClassCode = businessType == "DEBIT" ? "225" : "220";
        transaction.Addendas =
        [
            new AchTransactionAddenda
            {
                AddendaType = "05",
                BusinessType = businessType == "DEBIT" ? AchAddendaBusinessType.Debit : AchAddendaBusinessType.Credit,
                Purpose = "PAGOS",
                Reference = "FACTURA0001INFORMACIONLIBRE",
                CollectorId = businessType == "DEBIT" ? "9001234567890" : null,
                ReceiverCustomerCode = businessType == "DEBIT" ? "CLIENTE-SINTETICO" : null,
                ServiceDescription = businessType == "DEBIT" ? "RECAUDO" : null,
                SequenceNumber = 1
            }
        ];

        var content = await CreateOfficialSut(context, "ACH Colombia", model).Sut
            .BuildNachaFileAsync([100], CancellationToken.None);
        var records = SplitRecords(content);
        var type6 = records.Single(record => record[0] == '6');
        var type7 = records.Single(record => record[0] == '7');
        var trace = await LoadLatestTraceAsync(context);

        Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(content)))
            .Should().Be(expectedSha256);
        trace.ProfileCode.Should().Be(expectedProfileCode);
        trace.ProfileVersion.Should().Be("35.1");
        type6.Substring(1, 2).Should().Be(transactionCode);
        type6.Substring(29, 18).Should().Be(isPrenotification
            ? new string('0', 18)
            : "000000000000010000");
        if (businessType == "DEBIT")
        {
            type7.Substring(3, 13).Should().Be("9001234567890");
            type7.Substring(16, 30).TrimEnd().Should().Be("CLIENTE-SINTETICO");
        }
        else if (isPrenotification)
        {
            type7.Substring(30, 53).TrimEnd().Should().Be("FACTURA0001INFORMACIONLIBRE");
        }
        else
        {
            type7.Substring(30, 24).Should().Be("FACTURA0001INFORMACIONLI");
            type7.Substring(56, 24).TrimEnd().Should().Be("BRE");
        }
    }

    [Fact]
    public async Task PublishedAchType7Selector_ShouldSelectEquivalentMaterializedVariantWhenVariantCodeChanges()
    {
        await using var context = await SeedAsync();
        var publication = await context.HistConfigSnapshots.SingleAsync(row => row.SnapshotType == "PUBLISH"
            && row.Profile.ProfileCode == AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode);
        var snapshot = NachaPublicationSnapshotSerializer.ReadForOrdinaryGeneration(publication.SnapshotJson).Snapshot!;
        var materialized = NachaPublicationSnapshotMaterializer.Materialize(snapshot).Variants
            .Where(variant => variant.RecordCode.Code == "7")
            .ToList();
        var publishedDebit = materialized.Single(variant => PredicateEquals(variant, "BusinessType", "DEBIT"));
        const string renamedVariantCode = "SYNTHETIC_T7_DEBIT_VARIANT_ID";
        var renamedDebit = new CfgLayoutVariant
        {
            Id = publishedDebit.Id,
            ProfileId = publishedDebit.ProfileId,
            RecordCode = publishedDebit.RecordCode,
            VariantCode = renamedVariantCode,
            Priority = publishedDebit.Priority,
            IsDefaultForRecord = publishedDebit.IsDefaultForRecord,
            TotalLength = publishedDebit.TotalLength,
            EffectiveFrom = publishedDebit.EffectiveFrom,
            EffectiveTo = publishedDebit.EffectiveTo,
            Status = publishedDebit.Status,
            SelectionPredicateJson = publishedDebit.SelectionPredicateJson,
            Fields = publishedDebit.Fields
        };
        materialized[materialized.IndexOf(publishedDebit)] = renamedDebit;
        var resolution = new NachaConfigResolutionResult
        {
            LayoutVariantsByRecordCode = new Dictionary<string, IReadOnlyList<CfgLayoutVariant>>
            {
                ["7"] = materialized
            }
        };
        var candidate = new NachaType7RecordCandidate
        {
            Batch = new AchBatch(),
            Transaction = new AchTransaction { IsPrenotification = false },
            Addenda = new AchTransactionAddenda { BusinessType = AchAddendaBusinessType.Debit },
            FieldValues = new Dictionary<string, object?>()
        };

        var selected = NachaFileBuilder.ResolveType7Layout(resolution, candidate);

        selected.Should().BeSameAs(renamedDebit);
        selected.VariantCode.Should().Be(renamedVariantCode);
        selected.SelectionPredicateJson.Should().Be(publishedDebit.SelectionPredicateJson);
        selected.IsDefaultForRecord.Should().Be(publishedDebit.IsDefaultForRecord);
        selected.Priority.Should().Be(publishedDebit.Priority);
        selected.Fields.Should().BeSameAs(publishedDebit.Fields);
    }

    [Fact]
    public async Task PublishedAchType7Selector_ShouldHonorOriginalAndPrenotificationPredicateContracts()
    {
        await using var context = await SeedAsync();
        var publications = await context.HistConfigSnapshots
            .Include(row => row.Profile)
            .Where(row => row.SnapshotType == "PUBLISH"
                          && (row.Profile.ProfileCode == AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode
                              || row.Profile.ProfileCode == AchColOfficialNachaLayout.TxCodeAwareOutboundPrenotificationProfileCode))
            .ToDictionaryAsync(row => row.Profile.ProfileCode, row => row.SnapshotJson);
        var original = MaterializeType7(publications[AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode]);
        var prenotification = MaterializeType7(publications[AchColOfficialNachaLayout.TxCodeAwareOutboundPrenotificationProfileCode]);
        var originalDefault = original.Single(variant => variant.IsDefaultForRecord);
        var originalDebit = original.Single(variant => PredicateEquals(variant, "BusinessType", "DEBIT"));
        var originalCreditPrenote = original.Single(variant => PredicateEquals(variant, "BusinessType", "CREDIT")
                                                               && PredicateEquals(variant, "TransactionFamily", "PRENOTIFICATION"));
        var prenotificationDefault = prenotification.Single(variant => variant.IsDefaultForRecord);
        var prenotificationDebit = prenotification.Single(variant => PredicateEquals(variant, "BusinessType", "DEBIT"));

        SelectType7(original, AchAddendaBusinessType.Credit, false).Should().BeSameAs(originalDefault);
        SelectType7(original, AchAddendaBusinessType.Credit, true).Should().BeSameAs(originalCreditPrenote);
        SelectType7(original, AchAddendaBusinessType.Debit, false).Should().BeSameAs(originalDebit);
        SelectType7(original, AchAddendaBusinessType.Debit, true).Should().BeSameAs(originalDebit);
        SelectType7(prenotification, AchAddendaBusinessType.Credit, true).Should().BeSameAs(prenotificationDefault);
        SelectType7(prenotification, AchAddendaBusinessType.Debit, true).Should().BeSameAs(prenotificationDebit);
        originalDefault.SelectionPredicateJson.Should().BeNull();
        prenotificationDefault.SelectionPredicateJson.Should().BeNull();
    }

    [Fact]
    public async Task PublishedAchType7Selector_ShouldIgnoreLaterLivePredicateMutation()
    {
        await using var context = await SeedAsync();
        var liveDebit = await context.CfgLayoutVariants
            .Include(variant => variant.Profile)
            .Include(variant => variant.RecordCode)
            .SingleAsync(variant => variant.Profile.ProfileCode == AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode
                                    && variant.RecordCode.Code == "7"
                                    && variant.SelectionPredicateJson != null
                                    && variant.SelectionPredicateJson.Contains("DEBIT"));
        liveDebit.SelectionPredicateJson = """{"BusinessType":"CREDIT"}""";
        await context.SaveChangesAsync();
        var model = BuildContext("ACH Colombia");
        var transaction = model.Transactions.Single();
        transaction.Type = TransactionTypeEnum.Debit;
        transaction.TransactionCode = "27";
        transaction.AchBatch!.ServiceClassCode = "225";
        transaction.Addendas =
        [
            new AchTransactionAddenda
            {
                AddendaType = "05",
                BusinessType = AchAddendaBusinessType.Debit,
                Purpose = "PAGOS",
                CollectorId = "9001234567890",
                ReceiverCustomerCode = "CLIENTE-SINTETICO",
                ServiceDescription = "RECAUDO",
                SequenceNumber = 1
            }
        ];

        var content = await CreateOfficialSut(context, "ACH Colombia", model).Sut
            .BuildNachaFileAsync([100], CancellationToken.None);
        var type7 = SplitRecords(content).Single(record => record[0] == '7');

        type7.Substring(3, 13).Should().Be("9001234567890");
        type7.Substring(16, 30).TrimEnd().Should().Be("CLIENTE-SINTETICO");
        liveDebit.SelectionPredicateJson.Should().Be("""{"BusinessType":"CREDIT"}""");
    }

    [Fact]
    public async Task OrdinaryGeneration_ShouldRejectUnknownPersistedCodeWithoutMutationOrRecomputation()
    {
        await using var context = await SeedAsync();
        var model = BuildContext("ACH Colombia");
        var transaction = model.Transactions.Single();
        transaction.TransactionCode = "99";
        var setup = CreateOfficialSut(context, "ACH Colombia", model);

        var act = () => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        (await act.Should().ThrowAsync<NachaGenerationException>())
            .Which.Code.Should().Be("NACHA_TRANSACTION_CODE_NOT_ACCEPTED");
        transaction.TransactionCode.Should().Be("99");
    }

    [Fact]
    public async Task OrdinaryGeneration_ShouldRejectLiteralPrenoteCodeWhenPersistedSemanticStateIsMonetary()
    {
        await using var context = await SeedAsync();
        var model = BuildContext("ACH Colombia");
        var transaction = model.Transactions.Single();
        transaction.Type = TransactionTypeEnum.Credit;
        transaction.IsPrenotification = false;
        transaction.TransactionCode = "23";
        var setup = CreateOfficialSut(context, "ACH Colombia", model);

        var act = () => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        (await act.Should().ThrowAsync<NachaGenerationException>())
            .Which.Code.Should().Be("NACHA_TRANSACTION_CODE_PRENOTE_MISMATCH");
        transaction.TransactionCode.Should().Be("23");
    }

    [Fact]
    public async Task OrdinaryGeneration_ShouldRejectPersistedDirectionMismatchWithoutMutation()
    {
        await using var context = await SeedAsync();
        var model = BuildContext("ACH Colombia");
        var transaction = model.Transactions.Single();
        transaction.Type = TransactionTypeEnum.Credit;
        transaction.IsPrenotification = false;
        transaction.TransactionCode = "27";
        var setup = CreateOfficialSut(context, "ACH Colombia", model);

        var act = () => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        (await act.Should().ThrowAsync<NachaGenerationException>())
            .Which.Code.Should().Be("NACHA_TRANSACTION_CODE_DIRECTION_MISMATCH");
        transaction.TransactionCode.Should().Be("27");
    }

    [Fact]
    public async Task AchColBatchOrdinal_ShouldRestartForEachFile_AndMatchType5Type8()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var first = SplitRecords(await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));
        var second = SplitRecords(await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));

        foreach (var records in new[] { first, second })
        {
            var type5 = records.Single(record => record[0] == '5');
            var type8 = records.Single(record => record[0] == '8');
            type5.Substring(91, 7).Should().Be("0000001");
            type8.Substring(99, 7).Should().Be(type5.Substring(91, 7));
        }
    }

    [Fact]
    public async Task AchColMultipleBatches_ShouldUseFileLocalOrdinals_AndInterleaveType6Type7()
    {
        await using var context = await SeedAsync();
        var original = BuildContext("ACH Colombia", batchCount: 2);
        var model = new NachaBuildContext
        {
            Cycle = original.Cycle,
            Batches = original.Batches.Reverse().ToList(),
            Transactions = original.Transactions.Reverse().ToList()
        };
        var setup = CreateOfficialSut(context, "ACH Colombia", model);

        var content = await setup.Sut.BuildNachaFileAsync([101, 100], CancellationToken.None);
        var records = SplitRecords(content);

        records.Take(10).Select(record => record[0]).Should().ContainInOrder('1', '5', '6', '7', '8', '5', '6', '7', '8', '9');
        records.Where(record => record[0] == '5').Select(record => record.Substring(91, 7))
            .Should().ContainInOrder("0000001", "0000002");
        records.Where(record => record[0] == '8').Select(record => record.Substring(99, 7))
            .Should().ContainInOrder("0000001", "0000002");
        records.Where(record => record[0] == '6').Select(record => record.Substring(29, 18))
            .Should().ContainInOrder("000000000000010100", "000000000000010000");
        new NachaSemanticValidator().Validate(content, model);
    }

    [Fact]
    public async Task AchColOfficial_ShouldRejectOverflowWithoutExposingValue()
    {
        await using var context = await SeedAsync();
        var model = BuildContext("ACH Colombia");
        const string oversizedAccount = "SYNTHETIC-ACCOUNT-OVERFLOW";
        model.Transactions[0].DestinationAccountNumber = oversizedAccount;
        var setup = CreateOfficialSut(context, "ACH Colombia", model);

        var exception = await Assert.ThrowsAsync<NachaGenerationException>(
            () => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));

        exception.Code.Should().Be("NACHA_FIELD_LENGTH_INVALID");
        exception.RuleId.Should().Be("ACHCOL-T6-ACCOUNT-NUMBER");
        exception.Message.Should().NotContain(oversizedAccount);
    }

    [Fact]
    public async Task AchColOfficial_ShouldRejectInvalidCharacterWithoutSilentNormalization()
    {
        await using var context = await SeedAsync();
        var model = BuildContext("ACH Colombia");
        var setup = CreateOfficialSut(context, "ACH Colombia", model);
        var customer = await context.Customers.SingleAsync(item => item.DocumentNumber == "RCV001");
        customer.FirstName = "PERSONA|INVALIDA";
        await context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<NachaGenerationException>(
            () => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));

        exception.Code.Should().Be("NACHA_CHARACTER_REPERTOIRE_INVALID");
        exception.RuleId.Should().Be("ACHCOL-T6-INDIVIDUAL-NAME");
        exception.Message.Should().NotContain("PERSONA|INVALIDA");
    }

    [Fact]
    public async Task CenitLive_ShouldRemainFailClosed_WhileAchColIsNotBlockedByGate()
    {
        await using var context = await SeedAsync();
        var liveOptions = new NachaGenerationOptions
        {
            Mode = "TABLE_DRIVEN",
            ExecutionScope = "LIVE",
            AllowNonHomologatedCenitDevelopment = true
        };
        var cenit = CreateOfficialSut(context, "CENIT", generationOptionsOverride: liveOptions);

        var exception = await Assert.ThrowsAsync<NachaGenerationException>(
            () => cenit.Sut.BuildNachaFileAsync([100], CancellationToken.None));
        exception.Code.Should().Be("CENIT_NOT_HOMOLOGATED");
        exception.RuleId.Should().Be("CENIT-FORMAT-NACHAM");
        exception.Message.Should().NotContain("123456789").And.NotContain("RCV001");

        var achCol = CreateOfficialSut(context, "ACH Colombia", generationOptionsOverride: liveOptions);
        var content = await achCol.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AchColLive_ShouldRejectHybridAndLegacyGenerationModes()
    {
        await using var context = await SeedAsync();
        var options = new NachaGenerationOptions
        {
            Mode = "HYBRID",
            ExecutionScope = "LIVE"
        };
        var setup = CreateOfficialSut(context, "ACH Colombia", generationOptionsOverride: options);

        var exception = await Assert.ThrowsAsync<NachaGenerationException>(
            () => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));

        exception.Code.Should().Be("NACHA_LIVE_OFFICIAL_MODE_REQUIRED");
        exception.RuleId.Should().Be("ACHCOL-GENERATION-FAIL-CLOSED");
        exception.Message.Should().NotContain("123456789").And.NotContain("RCV001");
    }

    [Fact]
    public async Task ParserPhysicalReader_ShouldRejectLineEndingsAndResidualBytes()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");
        var content = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        await using var withLineEnding = new MemoryStream(Encoding.ASCII.GetBytes(content + "\r\n"));
        var lineEndingException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => NachaParserService.ReadPhysicalRecordsAsync(withLineEnding, CancellationToken.None));
        lineEndingException.Message.Should().Contain("ACHCOL-PHYSICAL-NO-LINE-ENDINGS");

        await using var withResidualByte = new MemoryStream(Encoding.ASCII.GetBytes(content + "X"));
        var residualException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => NachaParserService.ReadPhysicalRecordsAsync(withResidualByte, CancellationToken.None));
        residualException.Message.Should().Contain("ACHCOL-PHYSICAL-RECORD-LENGTH");
    }

    [Theory]
    [InlineData(0xD1, 'Ñ')]
    [InlineData(0xF1, 'ñ')]
    public async Task ParserPhysicalReader_ShouldAcceptNormativeAchColombiaEnye(int encodedValue, char expected)
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");
        var content = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var bytes = Encoding.Latin1.GetBytes(content);
        var entryOffset = Enumerable.Range(0, bytes.Length / 106)
            .Select(index => index * 106)
            .First(offset => bytes[offset] == (byte)'6');
        bytes[entryOffset + 62] = (byte)encodedValue;

        await using var stream = new MemoryStream(bytes);
        var records = await NachaParserService.ReadPhysicalRecordsAsync(stream, CancellationToken.None);

        records.Single(record => record[0] == '6')[62].Should().Be(expected);
    }

    [Fact]
    public async Task ParserPhysicalReader_ShouldRejectCharacterOutsideNormativeAchColombiaTable()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");
        var content = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var bytes = Encoding.Latin1.GetBytes(content);
        var entryOffset = Enumerable.Range(0, bytes.Length / 106)
            .Select(index => index * 106)
            .First(offset => bytes[offset] == (byte)'6');
        bytes[entryOffset + 62] = (byte)'@';

        await using var stream = new MemoryStream(bytes);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => NachaParserService.ReadPhysicalRecordsAsync(stream, CancellationToken.None));

        exception.Message.Should().Contain("ACHCOL-PHYSICAL-ASCII-REPERTOIRE");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AchColOfficial_ShouldRoundTripThroughProductParserWithSyntheticData(bool publishedSuccessor)
    {
        await using var context = await SeedAsync();
        var model = BuildContext("ACH Colombia");
        var setup = CreateOfficialSut(context, "ACH Colombia", model);
        var content = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        model.Cycle.ClearingHouse!.OriginCode = "000101006";
        context.AchCycles.Add(model.Cycle);
        await context.SaveChangesAsync();

        var parser = new NachaParserService(
            context,
            Mock.Of<ILogger<NachaParserService>>(),
            Mock.Of<IAchStateTransitionService>());
        var profile = await context.CfgProfiles.AsNoTracking().SingleAsync(item =>
            item.ProfileCode == (publishedSuccessor
                ? AchColOfficialNachaLayout.TxCodeAwareInboundOriginalProfileCode
                : AchColOfficialNachaLayout.InboundOriginalProfileCode));
        await using var stream = new MemoryStream(Encoding.ASCII.GetBytes(content));
        var result = await parser.ParseAndSaveDetailedAsync(
            stream,
            "0001283.001.20260524.1.OUT",
            new NachaParseRequest
            {
                ResolvedClearingHouseId = model.Cycle.ClearingHouseId,
                ResolvedAchCycleId = model.Cycle.Id,
                OperationalDate = model.Cycle.ProcessingDate,
                CorrelationId = "synthetic-roundtrip-execution-2",
                SelectedProfileId = profile.Id,
                SelectedProfileCode = profile.ProfileCode,
                RequirePublishedProfileSnapshot = publishedSuccessor
            },
            CancellationToken.None);

        result.Failures.Should().BeEmpty();
        result.TotalBatches.Should().Be(1);
        result.TotalEntries.Should().Be(1);
        result.TotalAddendas.Should().Be(1);

        var parsedHeader = await context.NachaHeaders.AsNoTracking().SingleAsync();
        var parsedBatch = await context.BatchHeaders.AsNoTracking().SingleAsync();
        var parsedEntry = await context.EntryDetails.AsNoTracking().SingleAsync();
        var parsedAddenda = await context.AddendaRecords.AsNoTracking().SingleAsync();
        var parsedBatchControl = await context.BatchControls.AsNoTracking().SingleAsync();

        parsedHeader.FileCreationDate.Should().Be("20260524");
        parsedHeader.ReferenceCode.Should().BeEmpty();
        parsedBatch.BatchNumber.Should().Be(1);
        parsedEntry.Amount.Should().Be(100m);
        parsedEntry.AddendumIndicator.Should().Be("1");
        parsedAddenda.BusinessType.Should().Be("Credit");
        parsedAddenda.InvoiceOrAccountNumber.Should().Be(new string('0', 24));
        parsedAddenda.InfofromOriginator.Should().Be(new string('0', 24));
        parsedAddenda.AddendumSequence.Should().Be("0001");
        parsedAddenda.EntryDetailSequenceNumber.Should().Be(parsedEntry.SequenceNumber![^7..]);
        parsedBatchControl.BatchNumber.Should().Be("0000001");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AchColV35Prenotification_ShouldParseWithExplicitInboundProfile(bool publishedSuccessor)
    {
        await using var context = await SeedAsync();
        var model = BuildContext("ACH Colombia");
        var transaction = model.Transactions.Single();
        transaction.Type = TransactionTypeEnum.Prenotification;
        transaction.IsPrenotification = true;
        transaction.TransactionCode = "23";
        transaction.Amount = 0m;
        transaction.Addendas =
        [
            new AchTransactionAddenda
            {
                AddendaType = "05",
                BusinessType = AchAddendaBusinessType.Credit,
                Purpose = "PAGOS",
                Reference = "PRENOTIFICACION-SINTETICA",
                SequenceNumber = 1
            }
        ];
        var content = await CreateOfficialSut(context, "ACH Colombia", model).Sut
            .BuildNachaFileAsync([100], CancellationToken.None);

        model.Cycle.ClearingHouse!.OriginCode = "000101006";
        context.AchCycles.Add(model.Cycle);
        await context.SaveChangesAsync();
        var profile = await context.CfgProfiles.AsNoTracking().SingleAsync(item =>
            item.ProfileCode == (publishedSuccessor
                ? AchColOfficialNachaLayout.TxCodeAwareInboundPrenotificationProfileCode
                : AchColOfficialNachaLayout.InboundPrenotificationProfileCode));
        var parser = new NachaParserService(
            context,
            Mock.Of<ILogger<NachaParserService>>(),
            Mock.Of<IAchStateTransitionService>());
        await using var stream = new MemoryStream(Encoding.ASCII.GetBytes(content));

        var result = await parser.ParseAndSaveDetailedAsync(
            stream,
            "0001283.001.20260524.1.OUT",
            new NachaParseRequest
            {
                ResolvedClearingHouseId = model.Cycle.ClearingHouseId,
                ResolvedAchCycleId = model.Cycle.Id,
                OperationalDate = model.Cycle.ProcessingDate,
                CorrelationId = "synthetic-prenote-v35",
                SelectedProfileId = profile.Id,
                SelectedProfileCode = profile.ProfileCode,
                RequirePublishedProfileSnapshot = publishedSuccessor
            },
            CancellationToken.None);

        result.Failures.Should().BeEmpty();
        (await context.EntryDetails.AsNoTracking().SingleAsync()).Amount.Should().Be(0m);
        (await context.AddendaRecords.AsNoTracking().SingleAsync()).InvoiceOrAccountNumber
            .Should().Be("PRENOTIFICACION-SINTETICA");
    }

    [Fact]
    public async Task AchColIncomingReturnProfile_ShouldPersistReturnWithoutLocalRecipientIdentity()
    {
        await using var context = await SeedAsync();
        var model = BuildContext("ACH Colombia");
        var setup = CreateOfficialSut(context, "ACH Colombia", model);
        var generated = await setup.Sut.BuildReturnOutAsync(BuildReturnOutRequest(), CancellationToken.None);
        var profile = await context.CfgProfiles
            .AsNoTracking()
            .SingleAsync(x => x.ProfileCode == "OFFICIAL_ACH_ENTRADA_DEVOLUCION_V1_0");

        model.Cycle.ClearingHouse!.OriginCode = "000101006";
        context.AchCycles.Add(model.Cycle);
        await context.SaveChangesAsync();
        (await context.Customers
                .AsNoTracking()
                .AnyAsync(customer => customer.DocumentNumber == "900123"
                                      && customer.Accounts.Any(account => account.AccountNumber == "123456789")))
            .Should().BeFalse();

        var parser = new NachaParserService(
            context,
            Mock.Of<ILogger<NachaParserService>>(),
            Mock.Of<IAchStateTransitionService>());
        await using var stream = new MemoryStream(Encoding.Latin1.GetBytes(generated.Content));
        var result = await parser.ParseAndSaveDetailedAsync(
            stream,
            "0001283.001.20260807.1.OUT",
            new NachaParseRequest
            {
                ResolvedClearingHouseId = model.Cycle.ClearingHouseId,
                ResolvedAchCycleId = model.Cycle.Id,
                OperationalDate = model.Cycle.ProcessingDate,
                CorrelationId = "incoming-return-profile-roundtrip",
                SelectedProfileId = profile.Id,
                SelectedProfileCode = profile.ProfileCode
            },
            CancellationToken.None);

        result.Failures.Should().BeEmpty();
        result.TotalEntries.Should().Be(1);
        result.TotalAddendas.Should().Be(1);
        var parsedAddenda = await context.AddendaRecords.AsNoTracking().SingleAsync();
        parsedAddenda.ReturnReasonCode.Should().Be("R01");
        parsedAddenda.OriginalTraceNumber.Should().Be("123456780000099");
        parsedAddenda.NewTraceNumber.Should().Be("876543210000001");
        parsedAddenda.AddendumSequence.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task PublishedOrdinaryResolver_PreservesSelectedPublicationIdentity()
    {
        await using var context = await SeedAsync();
        var published = await context.HistConfigSnapshots.SingleAsync(row => row.SnapshotType == "PUBLISH"
            && row.Profile.ProfileCode == AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode);
        var request = new NachaConfigResolutionRequest
        {
            ClearingHouseCode = "ACH",
            FlowTypeCode = "ORIGINAL",
            DirectionCode = "SALIDA",
            ServiceClassCode = "220",
            ProcessDateUtc = BuildContext("ACH Colombia").Cycle.ProcessingDate,
            RequestedVersionMajor = AchColOfficialNachaLayout.ProfileVersionMajor,
            RequestedVersionMinor = null,
            RecordCodes = OfficialRecordCodes
        };
        var resolver = new NachaConfigResolver(context);

        var live = await resolver.ResolveAsync(request);
        var snapshot = await resolver.ResolvePublishedOrdinaryAsync(request);

        live.Success.Should().BeTrue();
        snapshot.Success.Should().BeTrue();
        live.Profile!.Id.Should().Be(published.ProfileId);
        snapshot.Profile!.Id.Should().Be(live.Profile.Id);
        snapshot.Profile.VersionMajor.Should().Be(live.Profile.VersionMajor);
        snapshot.Profile.VersionMinor.Should().Be(live.Profile.VersionMinor);
    }

    [Fact]
    public async Task PublishedOrdinarySnapshot_KeepsBytesSecAndTraceStableAfterLiveMutationAndCoverageRerun()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");
        var publication = await context.HistConfigSnapshots.SingleAsync(row => row.SnapshotType == "PUBLISH"
            && row.Profile.ProfileCode == AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode);
        var snapshotJson = publication.SnapshotJson;
        var published = NachaPublicationSnapshotSerializer.ReadForOrdinaryGeneration(snapshotJson).Snapshot!;
        var expectedSec = published.StandardEntryClassMappings!.Single(item => item.Term == "PAGOS").StandardEntryClassCode;
        var publishedField = published.LayoutVariants.Single(variant => variant.RecordCode == "1" && variant.IsDefaultForRecord)
            .Fields.Single(field => field.FieldCode == "IMMEDIATEORIGINNAME");

        var before = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var beforeTrace = await LoadLatestTraceAsync(context);
        var liveField = await LoadFieldAsync(context, AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode,
            "1", "IMMEDIATEORIGINNAME");
        liveField.SourceDefinition.ConstantValue = "ACH-CAMBIO-UAT";
        liveField.FieldNameEs = "LIVE CHANGED";
        var liveSec = await context.CompanyEntryDescriptionCatalogs.SingleAsync(item => item.Term == "PAGOS");
        liveSec.StandardEntryClassCode = expectedSec == "PPD" ? "CCD" : "PPD";
        await context.SaveChangesAsync();

        await new NachaPublicationSnapshotCoverageSeeder(context).SeedAsync();
        var after = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var afterTrace = await LoadLatestTraceAsync(context);

        after.Should().Be(before);
        SplitRecords(after)[1].Substring(50, 3).Should().Be(expectedSec);
        liveSec.StandardEntryClassCode.Should().NotBe(expectedSec);
        beforeTrace.ProfileId.Should().Be(published.Profile.ProfileId);
        afterTrace.ProfileId.Should().Be(beforeTrace.ProfileId);
        afterTrace.ProfileVersion.Should().Be(beforeTrace.ProfileVersion);
        var traceField = afterTrace.FieldTraceEntries.Single(entry => entry.RecordType == "1"
            && entry.FieldName == "IMMEDIATEORIGINNAME");
        traceField.LayoutVariantId.Should().Be(published.LayoutVariants.Single(variant => variant.RecordCode == "1"
            && variant.IsDefaultForRecord).LayoutVariantId);
        traceField.FieldDefinitionId.Should().Be(publishedField.FieldDefinitionId);
        traceField.DisplayName.Should().Be(publishedField.FieldNameEs);
        traceField.DisplayName.Should().NotBe(liveField.FieldNameEs);
        (await context.HistConfigSnapshots.SingleAsync(row => row.Id == publication.Id)).SnapshotJson.Should().Be(snapshotJson);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("corrupt")]
    [InlineData("trace-incomplete")]
    [InlineData("sec-incomplete")]
    public async Task PublishedOrdinarySnapshot_UnavailableOrIncomplete_FailsWithoutLiveFallback(string condition)
    {
        await using var context = await SeedAsync();
        var publication = await context.HistConfigSnapshots.SingleAsync(row => row.SnapshotType == "PUBLISH"
            && row.Profile.ProfileCode == AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode);
        var profileId = publication.ProfileId;
        if (condition == "missing")
        {
            context.HistConfigSnapshots.Remove(publication);
        }
        else if (condition == "duplicate")
        {
            context.HistConfigSnapshots.Add(new HistConfigSnapshot
            {
                ProfileId = publication.ProfileId,
                VersionMajor = publication.VersionMajor,
                VersionMinor = publication.VersionMinor,
                SnapshotType = "PUBLISH",
                SnapshotJson = publication.SnapshotJson,
                CreatedAtUtc = publication.CreatedAtUtc,
                CreatedBy = "test"
            });
        }
        else if (condition == "corrupt")
        {
            publication.SnapshotJson = "not-json";
        }
        else
        {
            var json = JsonNode.Parse(publication.SnapshotJson)!.AsObject();
            if (condition == "trace-incomplete")
            {
                json["layoutVariants"]!.AsArray()[0]!.AsObject().Remove("layoutVariantId");
            }
            else
            {
                json.Remove("standardEntryClassMappings");
            }
            publication.SnapshotJson = json.ToJsonString();
            NachaPublicationSnapshotSerializer.ReadForOrdinaryGeneration(publication.SnapshotJson)
                .IsSupported.Should().BeFalse();
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        (await context.CfgProfiles.CountAsync(profile => profile.Id == profileId)).Should().Be(1);
        (await context.CompanyEntryDescriptionCatalogs.CountAsync(item => item.IsActive)).Should().BeGreaterThan(0);
        var setup = CreateOfficialSut(context, "ACH Colombia");
        var auditCount = await context.HistConfigChanges.CountAsync(item => item.ChangeType == "GENERATION_TRACE");

        var action = () => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        await action.Should().ThrowAsync<InvalidOperationException>();
        (await context.HistConfigChanges.CountAsync(item => item.ChangeType == "GENERATION_TRACE"))
            .Should().Be(auditCount);
    }

    private static OfficialSut CreateOfficialSut(
        AchDbContext context,
        string clearingHouseName,
        NachaBuildContext? contextDataOverride = null,
        NachaGenerationOptions? generationOptionsOverride = null)
    {
        var loader = new Mock<INachaDataLoader>(MockBehavior.Strict);
        var validation = new Mock<INachaTransactionValidationService>(MockBehavior.Strict);
        var renderer = new Mock<INachaFixedWidthRecordRenderer>(MockBehavior.Strict);
        var recordProvider = new Mock<INachaRecordDataProvider>(MockBehavior.Strict);
        var semantic = new Mock<INachaSemanticValidator>(MockBehavior.Strict);
        var holiday = new Mock<IBankHoliday>(MockBehavior.Strict);
        var batchNumberGenerator = new Mock<IBatchNumberGenerator>(MockBehavior.Strict);
        var logger = new Mock<ILogger<NachaFileBuilder>>(MockBehavior.Loose);

        var contextData = contextDataOverride ?? BuildContext(clearingHouseName);
        foreach (var transaction in contextData.Transactions)
        {
            if (context.Customers.Any(customer =>
                    customer.DocumentNumber == transaction.RecipientIdNumber
                    && customer.Accounts.Any(account => account.AccountNumber == transaction.DestinationAccountNumber)))
            {
                continue;
            }

            context.Customers.Add(new Customer
            {
                Id = 500 + transaction.Id,
                FirstName = "PERSONA",
                LastName = $"SINTETICA {transaction.Id}",
                PersonType = "PN",
                DocumentType = "CC",
                DocumentNumber = transaction.RecipientIdNumber,
                Accounts =
                [
                    new CustomerAccount
                    {
                        Id = 500 + transaction.Id,
                        AccountNumber = transaction.DestinationAccountNumber
                    }
                ]
            });
        }
        context.SaveChanges();
        loader.Setup(x => x.LoadBatchesByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contextData.Batches);
        loader.Setup(x => x.LoadByCycleAsync(contextData.Cycle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contextData);
        loader.Setup(x => x.LoadHeaderAsync(contextData.Cycle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NachaHeader
            {
                AchCycleId = contextData.Cycle.Id,
                FileCreationDate = "20260524",
                FileCreationTime = "1400",
                FileIdModifier = "A",
                ReferenceCode = null
            });
        loader.Setup(x => x.LoadCompanyEntryDescriptionCatalogAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(string Term, string StandardEntryClassCode)>
            {
                ("PAGOS", "PPD"),
                ("CONCENTRA", "CCD"),
                ("CORPORATE", "CTX")
            });
        validation.Setup(x => x.ValidateTransactionsForSendAsync(It.IsAny<IReadOnlyList<AchTransaction>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        semantic.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<NachaBuildContext>(), It.IsAny<NachaServiceClassSemanticContract>()));
        batchNumberGenerator.Setup(x => x.AssignBatchNumbersAsync(It.IsAny<IReadOnlyList<AchBatch>>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AchBatch> batches, string _, DateTime _, CancellationToken _) =>
                new BatchNumberAssignmentResult(
                    batches.ToDictionary(x => x.Id, _ => 1),
                    "TEST_FIXED",
                    1,
                    []));

        var sut = new NachaFileBuilder(
            context,
            holiday.Object,
            loader.Object,
            validation.Object,
            renderer.Object,
            recordProvider.Object,
            semantic.Object,
            configResolver: new NachaConfigResolver(context),
            type7GenerationStrategy: new NachaType7GenerationStrategy(new NachaType7FieldValueResolver(new NachaType7AliasMap())),
            generationOptions: Options.Create(generationOptionsOverride ?? new NachaGenerationOptions
            {
                Mode = "TABLE_DRIVEN",
                ExecutionScope = clearingHouseName.Contains("CENIT", StringComparison.OrdinalIgnoreCase)
                    ? "DEVELOPMENT"
                    : "LIVE",
                AllowNonHomologatedCenitDevelopment = clearingHouseName.Contains("CENIT", StringComparison.OrdinalIgnoreCase)
            }),
            logger: logger.Object,
            batchNumberGenerator: batchNumberGenerator.Object);

        return new OfficialSut(sut, loader, renderer, recordProvider, logger);
    }

    private static NachaBuildContext BuildContext(string clearingHouseName, int batchCount = 1)
    {
        var cycle = new AchCycle
        {
            Id = $"cycle-{clearingHouseName.Replace(" ", "-", StringComparison.OrdinalIgnoreCase)}",
            CycleName = "UAT6B2",
            ProcessingDate = new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc),
            ClearingHouseId = clearingHouseName.Contains("CENIT", StringComparison.OrdinalIgnoreCase) ? 2 : 1,
            ClearingHouse = new ClearingHouse
            {
                Id = clearingHouseName.Contains("CENIT", StringComparison.OrdinalIgnoreCase) ? 2 : 1,
                Name = clearingHouseName,
                Code = clearingHouseName.Contains("CENIT", StringComparison.OrdinalIgnoreCase) ? "CENIT" : "ACH",
                OriginCode = clearingHouseName.Contains("CENIT", StringComparison.OrdinalIgnoreCase) ? "87654321" : "12345678"
            }
        };

        var batches = new List<AchBatch>();
        var transactions = new List<AchTransaction>();
        for (var index = 0; index < batchCount; index++)
        {
            var batch = new AchBatch
            {
                Id = 100 + index,
                AchCycleId = cycle.Id,
                AchCycle = cycle,
                ServiceClassCode = "220",
                CompanyEntryDescription = "PAGOS",
                CompanyIdentification = "9001234567",
                CompanyName = "EMPRESA UAT",
                OriginOrOdfi = "12345678",
                EffectiveEntryDate = new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc)
            };

            var transaction = new AchTransaction
            {
                Id = 10 + index,
                Type = TransactionTypeEnum.Credit,
                Amount = 100m + index,
                AchBatchId = batch.Id,
                AchBatch = batch,
                AchCycleId = cycle.Id,
                TransactionCode = "22",
                ReceivingDFI = "12345678",
                TraceNumber = $"12345678{index + 1:0000000}",
                EffectiveEntryDate = batch.EffectiveEntryDate,
                DestinationAccountNumber = $"12345678{index + 9}",
                RecipientIdNumber = $"RCV{index + 1:000}",
                CompanyIdentification = batch.CompanyIdentification,
                OriginatingDFI = batch.OriginOrOdfi,
                Addendas = []
            };
            batch.Transactions = [transaction];
            batches.Add(batch);
            transactions.Add(transaction);
        }

        return new NachaBuildContext
        {
            Cycle = cycle,
            Batches = batches,
            Transactions = transactions
        };
    }

    private Task<AchDbContext> SeedAsync() => _fixture.CreateSeededContextAsync();

    private Task<AchDbContext> CreateContextAsync() => _fixture.CreateEmptyContextAsync();

    private static List<CfgLayoutVariant> MaterializeType7(string snapshotJson)
        => NachaPublicationSnapshotMaterializer.Materialize(
                NachaPublicationSnapshotSerializer.ReadForOrdinaryGeneration(snapshotJson).Snapshot!)
            .Variants
            .Where(variant => variant.RecordCode.Code == "7")
            .ToList();

    private static CfgLayoutVariant SelectType7(
        IReadOnlyList<CfgLayoutVariant> variants,
        AchAddendaBusinessType businessType,
        bool isPrenotification)
        => NachaFileBuilder.ResolveType7Layout(
            new NachaConfigResolutionResult
            {
                LayoutVariantsByRecordCode = new Dictionary<string, IReadOnlyList<CfgLayoutVariant>>
                {
                    ["7"] = variants
                }
            },
            new NachaType7RecordCandidate
            {
                Batch = new AchBatch(),
                Transaction = new AchTransaction { IsPrenotification = isPrenotification },
                Addenda = new AchTransactionAddenda { BusinessType = businessType },
                FieldValues = new Dictionary<string, object?>()
            });

    private static bool PredicateEquals(CfgLayoutVariant variant, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(variant.SelectionPredicateJson))
        {
            return false;
        }

        var predicate = JsonSerializer.Deserialize<Dictionary<string, string>>(variant.SelectionPredicateJson);
        return predicate?.TryGetValue(key, out var actual) == true
               && string.Equals(actual, value, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<CfgLayoutField> LoadFieldAsync(AchDbContext context, string profileCode, string recordCode, string fieldCode)
    {
        return await context.CfgLayoutFields
            .Include(x => x.SourceDefinition)
            .Include(x => x.LayoutVariant)
                .ThenInclude(x => x.RecordCode)
            .Include(x => x.LayoutVariant)
                .ThenInclude(x => x.Profile)
            .SingleAsync(x => x.LayoutVariant.Profile.ProfileCode == profileCode
                              && x.LayoutVariant.RecordCode.Code == recordCode
                              && x.FieldCode == fieldCode);
    }

    private static IReadOnlyList<string> SplitRecords(string content)
        => Enumerable.Range(0, content.Length / 106)
            .Select(i => content.Substring(i * 106, 106))
            .ToList();

    private static async Task<NachaGenerationAuditResult> LoadLatestTraceAsync(AchDbContext context)
    {
        var change = await context.HistConfigChanges
            .Where(x => x.EntityName == "NachaFileBuilder" && x.ChangeType == "GENERATION_TRACE")
            .OrderByDescending(x => x.ChangedAtUtc)
            .ThenByDescending(x => x.Id)
            .FirstAsync();

        return JsonSerializer.Deserialize<NachaGenerationAuditResult>(change.AfterJson!)!;
    }

    private static async Task MutatePublishedSnapshotAsync(AchDbContext context, Action<JsonObject> mutate)
    {
        var publication = await context.HistConfigSnapshots.SingleAsync(row => row.SnapshotType == "PUBLISH"
            && row.Profile.ProfileCode == AchColOfficialNachaLayout.TxCodeAwareOutboundOriginalProfileCode);
        var snapshot = JsonNode.Parse(publication.SnapshotJson)!.AsObject();
        mutate(snapshot);
        publication.SnapshotJson = snapshot.ToJsonString();
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private static JsonObject PublishedField(JsonObject snapshot, string recordCode, string fieldCode)
        => snapshot["layoutVariants"]!.AsArray()
            .Where(variant => variant!["recordCode"]!.GetValue<string>() == recordCode)
            .SelectMany(variant => variant!["fields"]!.AsArray())
            .Single(field => field!["fieldCode"]!.GetValue<string>() == fieldCode)!
            .AsObject();

    private static JsonObject PublishedVariant(JsonObject snapshot, string recordCode)
        => snapshot["layoutVariants"]!.AsArray()
            .Single(variant => variant!["recordCode"]!.GetValue<string>() == recordCode)!
            .AsObject();

    private sealed record OfficialSut(
        NachaFileBuilder Sut,
        Mock<INachaDataLoader> Loader,
        Mock<INachaFixedWidthRecordRenderer> LegacyRenderer,
        Mock<INachaRecordDataProvider> LegacyRecordProvider,
        Mock<ILogger<NachaFileBuilder>> Logger);
}
