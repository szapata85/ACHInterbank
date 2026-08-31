using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Helpers;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class CenitOrdinaryInbound2026ParserTests : IClassFixture<OfficialNachaGenerationFixture>
{
    private readonly OfficialNachaGenerationFixture _fixture;

    public CenitOrdinaryInbound2026ParserTests(OfficialNachaGenerationFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("PPD", "22", "220", false, CenitOrdinaryInbound2026Layout.OriginalProfileCode)]
    [InlineData("PPD", "27", "225", false, CenitOrdinaryInbound2026Layout.OriginalProfileCode)]
    [InlineData("CCD", "22", "220", false, CenitOrdinaryInbound2026Layout.OriginalProfileCode)]
    [InlineData("CCD", "27", "225", false, CenitOrdinaryInbound2026Layout.OriginalProfileCode)]
    [InlineData("PPD", "23", "220", true, CenitOrdinaryInbound2026Layout.PrenotificationProfileCode)]
    [InlineData("CCD", "28", "225", true, CenitOrdinaryInbound2026Layout.PrenotificationProfileCode)]
    public async Task Parser_ShouldReadOfficialPpdCcdMonetaryAndPrenotificationProfiles(
        string service,
        string transactionCode,
        string serviceClass,
        bool prenotification,
        string profileCode)
    {
        await using var context = await CreateContextAsync();
        var clearingHouse = await EnsureCenitOperationalContextAsync(context);
        var amount = prenotification ? 0L : 10_000L;
        var content = BuildSingleEntryFile(service, transactionCode, serviceClass, amount);

        var result = await ParseAsync(context, clearingHouse, profileCode, content, $"cenit-{service}-{transactionCode}");

        result.Failures.Should().BeEmpty();
        result.TotalBatches.Should().Be(1);
        result.TotalEntries.Should().Be(1);
        result.TotalAddendas.Should().Be(1);
        var batch = await context.BatchHeaders.AsNoTracking().SingleAsync();
        batch.StandardEntryClassCode.Should().Be(service);
        var entry = await context.EntryDetails.AsNoTracking().SingleAsync();
        entry.TransactionCode.Should().Be(transactionCode);
        entry.Amount.Should().Be(amount / 100m);
        var addenda = await context.AddendaRecords.AsNoTracking().SingleAsync();
        addenda.CodeTypeAddendumRecord.Should().Be("05");
        addenda.AddendumSequence.Should().Be("0001");
        addenda.EntryDetailSequenceNumber.Should().Be("0000001");
        addenda.PaymentRelatedInformation.Should().StartWith("INFORMACION CENIT");
    }

    [Fact]
    public async Task Parser_ShouldPreserveOperatorBatchesAndValidateFileControls()
    {
        await using var context = await CreateContextAsync();
        var clearingHouse = await EnsureCenitOperationalContextAsync(context);
        var content = BuildTwoOriginatingParticipantFile();

        var result = await ParseAsync(
            context,
            clearingHouse,
            CenitOrdinaryInbound2026Layout.OriginalProfileCode,
            content,
            "cenit-two-originators");

        result.Failures.Should().BeEmpty();
        result.TotalBatches.Should().Be(2);
        result.TotalEntries.Should().Be(2);
        result.TotalAddendas.Should().Be(2);
        var batches = await context.BatchHeaders.AsNoTracking().OrderBy(batch => batch.BatchNumber).ToListAsync();
        batches.Select(batch => batch.OriginParticipantEntityCode).Should().Equal("87654321", "76543210");
        batches.Select(batch => batch.StandardEntryClassCode).Should().Equal("PPD", "CCD");
        var entries = await context.EntryDetails.AsNoTracking().OrderBy(entry => entry.BatchNumber).ToListAsync();
        entries.Select(entry => entry.BatchNumber).Should().Equal(1, 2);
        var control = await context.FileControls.AsNoTracking().SingleAsync();
        control.BatchCount.Should().Be(2);
        control.BlockCount.Should().Be(1);
        control.EntryAddendaCount.Should().Be(4);
        control.EntryHash.Should().Be(35_802_467);
        control.TotalDebitAmount.Should().Be(50m);
        control.TotalCreditAmount.Should().Be(100m);
    }

    [Fact]
    public async Task Parser_ShouldAssociateMultipleCtxAddendasPerEntry()
    {
        await using var context = await CreateContextAsync();
        var clearingHouse = await EnsureCenitOperationalContextAsync(context);
        var content = BuildCtxFile();

        var result = await ParseAsync(
            context,
            clearingHouse,
            CenitOrdinaryInbound2026Layout.CtxOriginalProfileCode,
            content,
            "cenit-ctx-multiple");

        result.Failures.Should().BeEmpty();
        result.TotalBatches.Should().Be(1);
        result.TotalEntries.Should().Be(2);
        result.TotalAddendas.Should().Be(5);
        var entries = await context.EntryDetails
            .AsNoTracking()
            .Include(entry => entry.AddendaRecords)
            .OrderBy(entry => entry.SequenceNumber)
            .ToListAsync();
        entries.Should().HaveCount(2);
        entries[0].AddendaRecords.OrderBy(addenda => addenda.AddendumSequence)
            .Select(addenda => addenda.AddendumSequence).Should().Equal("0001", "0002");
        entries[1].AddendaRecords.OrderBy(addenda => addenda.AddendumSequence)
            .Select(addenda => addenda.AddendumSequence).Should().Equal("0001", "0002", "0003");
        entries[0].AddendaRecords.Should().OnlyContain(addenda => addenda.EntryDetailSequenceNumber == "0000001");
        entries[1].AddendaRecords.Should().OnlyContain(addenda => addenda.EntryDetailSequenceNumber == "0000002");
        var batch = await context.BatchHeaders.AsNoTracking().SingleAsync();
        batch.StandardEntryClassCode.Should().Be("CTX");
        var control = await context.FileControls.AsNoTracking().SingleAsync();
        control.EntryAddendaCount.Should().Be(7);
        control.EntryHash.Should().Be(35_802_467);
        control.TotalDebitAmount.Should().Be(50m);
        control.TotalCreditAmount.Should().Be(100m);
        control.BatchCount.Should().Be(1);
        control.BlockCount.Should().Be(2);
    }

    [Fact]
    public async Task Parser_ShouldReadCtxCreditAndDebitPrenotifications()
    {
        await using var context = await CreateContextAsync();
        var clearingHouse = await EnsureCenitOperationalContextAsync(context);
        var content = BuildCtxFile("23", 0, "28", 0);

        var result = await ParseAsync(
            context,
            clearingHouse,
            CenitOrdinaryInbound2026Layout.CtxPrenotificationProfileCode,
            content,
            "cenit-ctx-prenotification");

        result.Failures.Should().BeEmpty();
        result.TotalEntries.Should().Be(2);
        result.TotalAddendas.Should().Be(5);
        var entries = await context.EntryDetails.AsNoTracking().OrderBy(entry => entry.SequenceNumber).ToListAsync();
        entries.Select(entry => entry.TransactionCode).Should().Equal("23", "28");
        entries.Should().OnlyContain(entry => entry.Amount == 0m);
    }

    [Theory]
    [InlineData(CtxMutation.DeclaredCountMismatch, "adenda")]
    [InlineData(CtxMutation.DuplicateSequence, "SEQUENCE")]
    [InlineData(CtxMutation.SequenceGap, "SEQUENCE")]
    [InlineData(CtxMutation.WrongAssociation, "ASSOCIATION")]
    [InlineData(CtxMutation.WrongService, "PROFILE_SERVICE_MISMATCH")]
    [InlineData(CtxMutation.WrongFileHash, "Hash Total")]
    public async Task Parser_ShouldFailClosedForInvalidCtx(CtxMutation mutation, string expectedMessage)
    {
        await using var context = await CreateContextAsync();
        var clearingHouse = await EnsureCenitOperationalContextAsync(context);
        var content = MutateCtxFile(BuildCtxFile(), mutation);

        var action = () => ParseAsync(
            context,
            clearingHouse,
            CenitOrdinaryInbound2026Layout.CtxOriginalProfileCode,
            content,
            $"cenit-ctx-invalid-{mutation}");

        var exception = await action.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Message.Should().Contain(expectedMessage);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9_999)]
    public void CtxAddendaCardinality_ShouldAcceptOfficialBoundary(int count)
    {
        var action = () => NachaParserService.ValidateCenitCtxAddendaCardinality(count);
        action.Should().NotThrow();
    }

    [Fact]
    public void CtxAddendaCardinality_ShouldRejectTenThousand()
    {
        var action = () => NachaParserService.ValidateCenitCtxAddendaCardinality(10_000);
        action.Should().Throw<InvalidOperationException>().WithMessage("*1 y 9999*");
    }

    [Fact]
    public void ApplicationProfileServiceDetection_ShouldIsolateCtxAndRejectUnsupportedMixes()
    {
        IncomingNachaIngestionAppService.ResolveCenitInboundProfileService([BuildType5("PPD", "220", "87654321", 1)])
            .Should().BeNull();
        IncomingNachaIngestionAppService.ResolveCenitInboundProfileService([BuildType5("CCD", "225", "87654321", 1)])
            .Should().BeNull();
        IncomingNachaIngestionAppService.ResolveCenitInboundProfileService([BuildType5("CTX", "200", "87654321", 1)])
            .Should().Be("CTX");
        IncomingNachaIngestionAppService.ResolveCenitInboundProfileService(
                [BuildType5("PPD", "220", "87654321", 1), BuildType5("CTX", "200", "76543210", 2)])
            .Should().Be("CENIT_INBOUND_SERVICE_UNSUPPORTED");
    }

    private async Task<NachaParseResult> ParseAsync(
        AchDbContext context,
        ClearingHouse clearingHouse,
        string profileCode,
        string content,
        string correlationId)
    {
        var profile = await context.CfgProfiles.SingleAsync(profile => profile.ProfileCode == profileCode);
        var parser = new NachaParserService(
            context,
            Mock.Of<ILogger<NachaParserService>>(),
            Mock.Of<IAchStateTransitionService>());
        await using var stream = new MemoryStream(Encoding.Latin1.GetBytes(content));
        return await parser.ParseAndSaveDetailedAsync(stream, "1234567.001.20260815.1", new NachaParseRequest
        {
            ResolvedClearingHouseId = clearingHouse.Id,
            ResolvedAchCycleId = "CENIT-IN-2026",
            OperationalDate = new DateTime(2026, 8, 15),
            CorrelationId = correlationId,
            SelectedProfileId = profile.Id,
            SelectedProfileCode = profile.ProfileCode,
            IncomingNachaFileIngestionId = Guid.NewGuid()
        }, CancellationToken.None);
    }

    private Task<AchDbContext> CreateContextAsync() => _fixture.CreateSeededContextAsync();

    private static async Task<ClearingHouse> EnsureCenitOperationalContextAsync(AchDbContext context)
    {
        var clearingHouse = await context.ClearingHouses.FirstOrDefaultAsync(item => item.Code == "CENIT");
        if (clearingHouse is null)
        {
            clearingHouse = new ClearingHouse { Id = 7201, Code = "CENIT", Name = "CENIT", OriginCode = "00000000" };
            context.ClearingHouses.Add(clearingHouse);
        }

        if (!await context.AchCycles.AnyAsync(cycle => cycle.Id == "CENIT-IN-2026"))
        {
            context.AchCycles.Add(new AchCycle
            {
                Id = "CENIT-IN-2026",
                CycleName = "Ciclo 1",
                ProcessingDate = new DateTime(2026, 8, 15),
                CutoffTime = new TimeSpan(12, 0, 0),
                ClearingHouse = clearingHouse
            });
        }

        if (!await context.Customers.AnyAsync(customer => customer.Id == 82_001))
        {
            context.Customers.AddRange(
                BuildCustomer(82_001, "DOC000000000001", "CUENTA00000000001"),
                BuildCustomer(82_002, "DOC000000000002", "CUENTA00000000002"));
        }

        await context.SaveChangesAsync();
        return clearingHouse;
    }

    private static Customer BuildCustomer(int id, string documentNumber, string accountNumber)
        => new()
        {
            Id = id,
            FirstName = "RECEPTOR",
            LastName = id.ToString(),
            PersonType = "PN",
            DocumentType = "CC",
            DocumentNumber = documentNumber,
            Accounts =
            [
                new CustomerAccount
                {
                    Id = id,
                    AccountNumber = accountNumber
                }
            ]
        };

    private static string BuildSingleEntryFile(string service, string transactionCode, string serviceClass, long amount)
    {
        const string origin = "87654321";
        const string receiving = "12345678";
        var debit = transactionCode is "27" or "28" ? amount : 0;
        var credit = debit == 0 ? amount : 0;
        var records = new List<string>
        {
            BuildType1(),
            BuildType5(service, serviceClass, origin, 1),
            BuildOrdinaryType6(transactionCode, receiving, origin, amount, 1),
            BuildType7("INFORMACION CENIT", 1, 1),
            BuildType8(serviceClass, receiving, origin, 1, 2, debit, credit),
            BuildType9(1, 1, 2, receiving, debit, credit)
        };
        records.AddRange(Enumerable.Repeat(new string('9', 106), 4));
        return string.Concat(records);
    }

    private static string BuildTwoOriginatingParticipantFile()
    {
        var records = new List<string>
        {
            BuildType1(),
            BuildType5("PPD", "220", "87654321", 1),
            BuildOrdinaryType6("22", "12345678", "87654321", 10_000, 1),
            BuildType7("PAGO ORIGINADOR A", 1, 1),
            BuildType8("220", "12345678", "87654321", 1, 2, 0, 10_000),
            BuildType5("CCD", "225", "76543210", 2),
            BuildOrdinaryType6("27", "23456789", "76543210", 5_000, 2),
            BuildType7("PAGO ORIGINADOR B", 1, 2),
            BuildType8("225", "23456789", "76543210", 2, 2, 5_000, 0),
            BuildType9(2, 1, 4, "35802467", 5_000, 10_000)
        };
        return string.Concat(records);
    }

    private static string BuildCtxFile(
        string firstTransactionCode = "22",
        long firstAmount = 10_000,
        string secondTransactionCode = "27",
        long secondAmount = 5_000)
    {
        var records = new List<string>
        {
            BuildType1(),
            BuildType5("CTX", "200", "87654321", 1),
            BuildCtxType6(firstTransactionCode, "12345678", "87654321", firstAmount, 2, 1),
            BuildType7("CTX A UNO", 1, 1),
            BuildType7("CTX A DOS", 2, 1),
            BuildCtxType6(secondTransactionCode, "23456789", "87654321", secondAmount, 3, 2),
            BuildType7("CTX B UNO", 1, 2),
            BuildType7("CTX B DOS", 2, 2),
            BuildType7("CTX B TRES", 3, 2),
            BuildType8("200", "35802467", "87654321", 1, 7, secondAmount, firstAmount),
            BuildType9(1, 2, 7, "35802467", secondAmount, firstAmount)
        };
        records.AddRange(Enumerable.Repeat(new string('9', 106), 9));
        return string.Concat(records);
    }

    private static string MutateCtxFile(string content, CtxMutation mutation)
    {
        var records = Enumerable.Range(0, content.Length / 106)
            .Select(index => content.Substring(index * 106, 106).ToCharArray())
            .ToArray();
        switch (mutation)
        {
            case CtxMutation.DeclaredCountMismatch:
                Put(records[2], 63, 4, "0003");
                break;
            case CtxMutation.DuplicateSequence:
                Put(records[4], 84, 4, "0001");
                break;
            case CtxMutation.SequenceGap:
                Put(records[4], 84, 4, "0003");
                break;
            case CtxMutation.WrongAssociation:
                Put(records[4], 88, 7, "0000002");
                break;
            case CtxMutation.WrongService:
                Put(records[1], 51, 3, "PPD");
                break;
            case CtxMutation.WrongFileHash:
                Put(records[10], 22, 10, "0000000000");
                break;
        }

        return string.Concat(records.Select(record => new string(record)));
    }

    private static string BuildType1()
    {
        var line = Blank('1');
        Put(line, 2, 2, "01");
        Put(line, 4, 10, "0000000000");
        Put(line, 14, 10, "0000000000");
        Put(line, 24, 8, "20260815");
        Put(line, 32, 4, "1200");
        Put(line, 36, 1, "A");
        Put(line, 37, 3, "106");
        Put(line, 40, 2, "10");
        Put(line, 42, 1, "1");
        Put(line, 43, 23, "CFA UAT");
        Put(line, 66, 23, "CENIT");
        return new string(line);
    }

    private static string BuildType5(string service, string serviceClass, string origin, int batchNumber)
    {
        var line = Blank('5');
        Put(line, 2, 3, serviceClass);
        Put(line, 5, 16, $"ORIGINADOR {batchNumber}");
        Put(line, 41, 10, $"900000000{batchNumber}");
        Put(line, 51, 3, service);
        Put(line, 54, 10, service == "CTX" ? "CORPORATE" : "PAGOS");
        Put(line, 64, 8, "20260815");
        Put(line, 72, 8, "20260815");
        Put(line, 80, 3, "228");
        Put(line, 83, 1, "1");
        Put(line, 84, 8, origin);
        Put(line, 92, 7, batchNumber.ToString("0000000"));
        return new string(line);
    }

    private static string BuildOrdinaryType6(
        string transactionCode,
        string receiving,
        string origin,
        long amount,
        int sequence)
    {
        var line = Blank('6');
        Put(line, 2, 2, transactionCode);
        Put(line, 4, 8, receiving);
        Put(line, 12, 1, DigitoChequeoHelper.CalcularDigitoChequeo(receiving));
        Put(line, 13, 17, $"CUENTA{sequence:00000000000}");
        Put(line, 30, 18, amount.ToString("000000000000000000"));
        Put(line, 48, 15, $"DOC{sequence:000000000000}");
        Put(line, 63, 22, $"RECEPTOR {sequence}");
        Put(line, 87, 1, "1");
        Put(line, 88, 15, $"{origin}{sequence:0000000}");
        return new string(line);
    }

    private static string BuildCtxType6(
        string transactionCode,
        string receiving,
        string origin,
        long amount,
        int addendaCount,
        int sequence)
    {
        var line = Blank('6');
        Put(line, 2, 2, transactionCode);
        Put(line, 4, 8, receiving);
        Put(line, 12, 1, DigitoChequeoHelper.CalcularDigitoChequeo(receiving));
        Put(line, 13, 17, $"CUENTA{sequence:00000000000}");
        Put(line, 30, 18, amount.ToString("000000000000000000"));
        Put(line, 48, 15, $"DOC{sequence:000000000000}");
        Put(line, 63, 4, addendaCount.ToString("0000"));
        Put(line, 67, 16, $"RECEPTOR {sequence}");
        Put(line, 87, 1, "1");
        Put(line, 88, 15, $"{origin}{sequence:0000000}");
        return new string(line);
    }

    private static string BuildType7(string information, int sequence, int entrySequence)
    {
        var line = Blank('7');
        Put(line, 2, 2, "05");
        Put(line, 4, 80, information);
        Put(line, 84, 4, sequence.ToString("0000"));
        Put(line, 88, 7, entrySequence.ToString("0000000"));
        return new string(line);
    }

    private static string BuildType8(
        string serviceClass,
        string hash,
        string origin,
        int batchNumber,
        int count,
        long debit,
        long credit)
    {
        var line = Blank('8');
        Put(line, 2, 3, serviceClass);
        Put(line, 5, 6, count.ToString("000000"));
        Put(line, 11, 10, long.Parse(hash).ToString("0000000000"));
        Put(line, 21, 18, debit.ToString("000000000000000000"));
        Put(line, 39, 18, credit.ToString("000000000000000000"));
        Put(line, 57, 10, $"900000000{batchNumber}");
        Put(line, 92, 8, origin);
        Put(line, 100, 7, batchNumber.ToString("0000000"));
        return new string(line);
    }

    private static string BuildType9(
        int batchCount,
        int blockCount,
        int count,
        string hash,
        long debit,
        long credit)
    {
        var line = Blank('9');
        Put(line, 2, 6, batchCount.ToString("000000"));
        Put(line, 8, 6, blockCount.ToString("000000"));
        Put(line, 14, 8, count.ToString("00000000"));
        Put(line, 22, 10, long.Parse(hash).ToString("0000000000"));
        Put(line, 32, 18, debit.ToString("000000000000000000"));
        Put(line, 50, 18, credit.ToString("000000000000000000"));
        return new string(line);
    }

    private static char[] Blank(char recordType)
    {
        var line = Enumerable.Repeat(' ', 106).ToArray();
        line[0] = recordType;
        return line;
    }

    private static void Put(char[] target, int startPosition, int length, string value)
    {
        var formatted = value.Length >= length ? value[..length] : value.PadRight(length);
        Array.Copy(formatted.ToCharArray(), 0, target, startPosition - 1, length);
    }

    public enum CtxMutation
    {
        DeclaredCountMismatch,
        DuplicateSequence,
        SequenceGap,
        WrongAssociation,
        WrongService,
        WrongFileHash
    }
}
