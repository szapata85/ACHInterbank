using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class IncomingProcTransaccionesDerivedFixtureParserTests
{
    private const int RecordLength = 106;
    private static readonly DateTime OperationalDate = new(2026, 5, 24);

    [Theory]
    [InlineData("123.45")]
    [InlineData("9876.54")]
    public async Task DerivedFixture_RecalculatesControls_AndProductParserPersistsExactValues(string rawAmount)
    {
        var amount = decimal.Parse(rawAmount, System.Globalization.CultureInfo.InvariantCulture);
        var goldenPath = GoldenPath();
        var goldenBefore = await File.ReadAllBytesAsync(goldenPath);
        var goldenHashBefore = Convert.ToHexString(SHA256.HashData(goldenBefore));

        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();

        var cfa = new FinancialInstitution
        {
            Name = "CFA CANONICA",
            RoutingNumber = "00001",
            TransitCode = "006",
            IsDefaultSource = true,
            Status = FinancialInstitutionStatus.Active
        };
        cfa.CalculateCheckDigit();
        context.FinancialInstitutions.Add(cfa);
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, ClearingHouseId = 1, HolidayStrategy = "Colombian" });
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 1,
            Name = "ACH Colombia",
            Code = "ACHCOL",
            OriginCode = "12345678",
            ClearingHouseId = 1
        });
        context.AchCycles.Add(new AchCycle
        {
            Id = "ACH-20260524-06",
            CycleName = "Ciclo 6 - ACH Colombia",
            ProcessingDate = OperationalDate,
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = new TimeSpan(20, 0, 0),
            ClearingHouseId = 1
        });
        await context.SaveChangesAsync();

        var receivingDfi = $"{cfa.RoutingNumber}{cfa.TransitCode}{cfa.CheckDigit}";
        var derived = BuildDerived(
            goldenBefore,
            receiverAccount: "E2EACCOUNT0008684",
            receivingDfi,
            amount,
            externalOriginRouting: "99999900",
            uniqueRunKey: $"PTX-PARSER-{amount:0.00}".Replace('.', '-'));

        var parser = BuildParser(context);
        await using var stream = new MemoryStream(derived);
        var result = await parser.ParseAndSaveDetailedAsync(
            stream,
            "0001283.001.6",
            new NachaParseRequest
            {
                ResolvedClearingHouseId = 1,
                ResolvedAchCycleId = "ACH-20260524-06",
                OperationalDate = OperationalDate,
                CorrelationId = "e2e-derived-parser"
            });

        Assert.Empty(result.Failures);
        Assert.Equal(1, result.TotalEntries);
        Assert.Equal(1, result.TotalAddendas);

        var entry = await context.EntryDetails.AsNoTracking().SingleAsync();
        var batch = await context.BatchHeaders.AsNoTracking().SingleAsync();
        var batchControl = await context.BatchControls.AsNoTracking().SingleAsync();
        var fileControl = await context.FileControls.AsNoTracking().SingleAsync();

        Assert.Equal("22", entry.TransactionCode);
        Assert.Equal("E2EACCOUNT0008684", entry.AccountNumber);
        Assert.Equal(amount, entry.Amount);
        Assert.Equal(receivingDfi[..8], entry.ReceivingParticipantEntityCode);
        Assert.Equal(receivingDfi[8..], entry.CheckDigit);
        Assert.Equal("99999900", batch.OriginParticipantEntityCode);
        Assert.StartsWith("99999900", entry.SequenceNumber);
        Assert.Equal("99999900", batchControl.IdOrigEntity);
        Assert.Equal(2, batchControl.EntryAddendaCount);
        Assert.Equal(long.Parse(receivingDfi[..8]), batchControl.EntryHash);
        Assert.Equal(0m, batchControl.TotalDebitAmount);
        Assert.Equal(amount, batchControl.TotalCreditAmount);
        Assert.Equal(1, fileControl.BatchCount);
        Assert.Equal(1, fileControl.BlockCount);
        Assert.Equal(2, fileControl.EntryAddendaCount);
        Assert.Equal(long.Parse(receivingDfi[..8]), fileControl.EntryHash);
        Assert.Equal(0m, fileControl.TotalDebitAmount);
        Assert.Equal(amount, fileControl.TotalCreditAmount);

        var goldenAfter = await File.ReadAllBytesAsync(goldenPath);
        Assert.Equal(goldenHashBefore, Convert.ToHexString(SHA256.HashData(goldenAfter)));
        Assert.Equal(goldenBefore, goldenAfter);
    }

    private static byte[] BuildDerived(
        byte[] golden,
        string receiverAccount,
        string receivingDfi,
        decimal amount,
        string externalOriginRouting,
        string uniqueRunKey)
    {
        var content = golden.ToArray();
        var batchStart = RecordLength;
        var entryStart = 2 * RecordLength;
        var addendaStart = 3 * RecordLength;
        var batchControlStart = 4 * RecordLength;
        var fileControlStart = 5 * RecordLength;
        var cents = checked(decimal.ToInt64(amount * 100m));
        var entryHash = long.Parse(receivingDfi[..8]);

        Write(content, batchStart + 83, 8, externalOriginRouting.PadLeft(8, '0'));
        Write(content, entryStart + 3, 8, receivingDfi[..8]);
        Write(content, entryStart + 11, 1, receivingDfi[8..]);
        Write(content, entryStart + 12, 17, receiverAccount.PadRight(17));
        Write(content, entryStart + 29, 18, cents.ToString().PadLeft(18, '0'));
        Write(content, entryStart + 47, 15, IncomingProcTransaccionesE2eScenarioSetupService.SyntheticRecipientId);
        Write(content, entryStart + 87, 15, $"{externalOriginRouting}0000001");
        Write(content, batchControlStart + 91, 8, externalOriginRouting);

        Array.Fill(content, (byte)' ', addendaStart + 30, 53);
        Write(content, addendaStart + 30, uniqueRunKey.Length, uniqueRunKey);

        Write(content, batchControlStart + 4, 6, "000002");
        Write(content, batchControlStart + 10, 10, entryHash.ToString().PadLeft(10, '0'));
        Write(content, batchControlStart + 20, 18, new string('0', 18));
        Write(content, batchControlStart + 38, 18, cents.ToString().PadLeft(18, '0'));

        Write(content, fileControlStart + 1, 6, "000001");
        Write(content, fileControlStart + 7, 6, "000001");
        Write(content, fileControlStart + 13, 8, "00000002");
        Write(content, fileControlStart + 21, 10, entryHash.ToString().PadLeft(10, '0'));
        Write(content, fileControlStart + 31, 18, new string('0', 18));
        Write(content, fileControlStart + 49, 18, cents.ToString().PadLeft(18, '0'));
        return content;
    }

    private static void Write(byte[] content, int offset, int length, string value)
    {
        Assert.Equal(length, value.Length);
        Encoding.ASCII.GetBytes(value).CopyTo(content, offset);
    }

    private static NachaParserService BuildParser(AchDbContext context)
    {
        var transition = new Mock<IAchStateTransitionService>();
        transition.Setup(x => x.TransitionAsync(
                It.IsAny<int>(),
                It.IsAny<AchTransferStateEnum>(),
                It.IsAny<AchStateEventSourceEnum>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchTransaction());
        return new NachaParserService(context, Mock.Of<ILogger<NachaParserService>>(), transition.Object);
    }

    private static string GoldenPath()
        => Path.Combine(AppContext.BaseDirectory, "TestData", "Nacha", "GoldenFiles", "ACHColombia", "Incoming", "ACH_COL_IN_001.ach");
}
