using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
using Xunit.Abstractions;

namespace Cfa.ACHInterbank.Tests;

public sealed class CenitProcTransaccionesPackageCharacterizationTests
{
    private const int RecordLength = 106;
    private static readonly DateTime OperationalDate = new(2026, 7, 13);
    private readonly ITestOutputHelper _output;

    public CenitProcTransaccionesPackageCharacterizationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task OfficialPackage_IsCharacterizedWithProductionParser_WithoutMutatingZip()
    {
        var packagePath = Environment.GetEnvironmentVariable("CENIT_TEST_PACKAGE_PATH");
        if (string.IsNullOrWhiteSpace(packagePath))
        {
            _output.WriteLine("CENIT_TEST_PACKAGE_PATH no está definida; caracterización runtime omitida fuera del entorno PRE-LIVE.");
            return;
        }

        Assert.True(File.Exists(packagePath), $"No existe el paquete CENIT requerido: {packagePath}");
        var hashBefore = await HashFileAsync(packagePath);
        var expectedNames = Enumerable.Range(1, 5)
            .Select(index => $"0001283.{index:000}.20260713.1")
            .ToArray();

        using (var archive = ZipFile.OpenRead(packagePath))
        {
            var payloadNames = archive.Entries
                .Where(entry => entry.Name.StartsWith("0001283.", StringComparison.Ordinal))
                .Select(entry => entry.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            Assert.Equal(expectedNames, payloadNames);

            foreach (var name in expectedNames)
            {
                var entry = Assert.Single(archive.Entries, item => item.Name == name);
                await using var source = entry.Open();
                using var memory = new MemoryStream();
                await source.CopyToAsync(memory);
                var content = memory.ToArray();
                var physical = CharacterizePhysical(content);
                var parsed = await ParseWithProductionParserAsync(content, name);
                _output.WriteLine(JsonSerializer.Serialize(new { name, bytes = content.Length, physical, parsed }));
            }
        }

        var hashAfter = await HashFileAsync(packagePath);
        Assert.Equal(hashBefore, hashAfter);
        _output.WriteLine($"ZIP_SHA256={hashAfter}");
    }

    [Fact]
    public async Task SelectedCenitEntry_DerivesSingleAuthorizedCredit_AcceptedByProductionParser()
    {
        var packagePath = Environment.GetEnvironmentVariable("CENIT_TEST_PACKAGE_PATH");
        if (string.IsNullOrWhiteSpace(packagePath))
        {
            _output.WriteLine("CENIT_TEST_PACKAGE_PATH no está definida; prueba derivada CENIT omitida fuera del entorno PRE-LIVE.");
            return;
        }

        var hashBefore = await HashFileAsync(packagePath);
        byte[] source;
        using (var archive = ZipFile.OpenRead(packagePath))
        {
            var entry = Assert.Single(archive.Entries, item => item.Name == "0001283.002.20260713.1");
            await using var stream = entry.Open();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);
            source = memory.ToArray();
        }

        var derived = BuildDerivedSingleEntry(source);
        var parsed = await ParseWithProductionParserAsync(derived, "0001283.002.1");
        var evidence = JsonSerializer.Serialize(parsed);
        _output.WriteLine(evidence);
        Assert.Contains("\"accepted\":true", evidence, StringComparison.Ordinal);
        Assert.Contains("\"TotalEntries\":1", evidence, StringComparison.Ordinal);
        Assert.Contains("\"TotalAddendas\":1", evidence, StringComparison.Ordinal);
        Assert.Equal(hashBefore, await HashFileAsync(packagePath));
    }

    private static byte[] BuildDerivedSingleEntry(byte[] source)
    {
        Assert.Equal(20 * RecordLength, source.Length);
        var records = Enumerable.Range(0, 20)
            .Select(index => source.AsSpan(index * RecordLength, RecordLength).ToArray())
            .ToArray();
        var content = new byte[10 * RecordLength];
        records[0].CopyTo(content, 0 * RecordLength);
        records[13].CopyTo(content, 1 * RecordLength);
        records[14].CopyTo(content, 2 * RecordLength);
        records[15].CopyTo(content, 3 * RecordLength);
        records[16].CopyTo(content, 4 * RecordLength);
        Array.Fill(content, (byte)' ', 5 * RecordLength, RecordLength);
        content[5 * RecordLength] = (byte)'9';
        for (var index = 6; index < 10; index++)
        {
            Array.Fill(content, (byte)'9', index * RecordLength, RecordLength);
        }

        const string external = "99999900";
        const string receivingDfi = "000010061";
        const string trace = "999999001234567";
        const long cents = 12_345;
        var batch = RecordLength;
        var entry = 2 * RecordLength;
        var addenda = 3 * RecordLength;
        var batchControl = 4 * RecordLength;
        var fileControl = 5 * RecordLength;

        Write(content, batch + 4, 16, "BANCO UAT CENIT".PadRight(16));
        Write(content, batch + 20, 20, "ESCENARIO E2E".PadRight(20));
        Write(content, batch + 40, 10, "E2ECENIT01");
        Write(content, batch + 53, 10, "CREDITOE2E");
        Write(content, batch + 83, 8, external);
        Write(content, batch + 91, 7, "0000001");
        Write(content, entry + 3, 8, receivingDfi[..8]);
        Write(content, entry + 11, 1, receivingDfi[8..]);
        Write(content, entry + 12, 17, "E2EACCOUNT0008684");
        Write(content, entry + 29, 18, cents.ToString().PadLeft(18, '0'));
        Write(content, entry + 47, 15, "E2EPTXANCHOR001");
        Write(content, entry + 62, 22, "RECEPTOR E2E".PadRight(22));
        Write(content, entry + 84, 2, "  ");
        Write(content, entry + 87, 15, trace);
        Array.Fill(content, (byte)' ', addenda + 3, 80);
        Write(content, addenda + 3, 13, "E2EPTXANCHOR".PadRight(13));
        Write(content, addenda + 30, 24, "PTX-CENIT-PARSER-000001".PadRight(24));
        Write(content, addenda + 87, 7, trace[^7..]);
        Write(content, batchControl + 4, 6, "000002");
        Write(content, batchControl + 10, 10, receivingDfi[..8].PadLeft(10, '0'));
        Write(content, batchControl + 20, 18, new string('0', 18));
        Write(content, batchControl + 38, 18, cents.ToString().PadLeft(18, '0'));
        Write(content, batchControl + 56, 10, "E2ECENIT01");
        Write(content, batchControl + 91, 8, external);
        Write(content, batchControl + 99, 7, "0000001");
        Write(content, fileControl + 1, 6, "000001");
        Write(content, fileControl + 7, 6, "000001");
        Write(content, fileControl + 13, 8, "00000002");
        Write(content, fileControl + 21, 10, receivingDfi[..8].PadLeft(10, '0'));
        Write(content, fileControl + 31, 18, new string('0', 18));
        Write(content, fileControl + 49, 18, cents.ToString().PadLeft(18, '0'));
        return content;
    }

    private static void Write(byte[] content, int offset, int length, string value)
    {
        Assert.Equal(length, value.Length);
        Encoding.ASCII.GetBytes(value).CopyTo(content, offset);
    }

    private static object CharacterizePhysical(byte[] content)
    {
        var text = Encoding.ASCII.GetString(content);
        var parserInput = text.Split('\n', 2)[0].TrimEnd('\r');
        Assert.True(parserInput.Length >= RecordLength);
        var configuredLength = int.Parse(parserInput.Substring(36, 3));
        var completeRecords = parserInput.Length / configuredLength;
        var remainder = parserInput.Length % configuredLength;
        var records = Enumerable.Range(0, completeRecords)
            .Select(index => parserInput.Substring(index * configuredLength, configuredLength))
            .ToArray();
        var entries = records.Where(record => record[0] == '6').ToArray();
        var amounts = entries.Select(record => decimal.Parse(record.Substring(29, 18)) / 100m).ToArray();
        var accountMasks = entries.Select(record => MaskAccount(record.Substring(12, 17).Trim())).Distinct().OrderBy(value => value).ToArray();

        return new
        {
            configuredLength,
            parserInputLength = parserInput.Length,
            completeRecords,
            remainder,
            newlineCount = text.Count(character => character == '\n'),
            recordTypes = records.GroupBy(record => record[0]).ToDictionary(group => group.Key.ToString(), group => group.Count()),
            transactionCodes = entries.GroupBy(record => record.Substring(1, 2)).ToDictionary(group => group.Key, group => group.Count()),
            immediateDestination = parserInput.Substring(3, 10).Trim(),
            immediateOrigin = parserInput.Substring(13, 10).Trim(),
            operationalDate = parserInput.Substring(23, 8),
            receivingDfis = entries.Select(record => record.Substring(3, 9)).Distinct().OrderBy(value => value).ToArray(),
            receiverAccountCount = accountMasks.Length,
            receiverAccountSamples = accountMasks.Take(5).ToArray(),
            amountCount = amounts.Length,
            amountMin = amounts.Length == 0 ? 0m : amounts.Min(),
            amountMax = amounts.Length == 0 ? 0m : amounts.Max(),
            amountTotal = amounts.Sum()
        };
    }

    private static async Task<object> ParseWithProductionParserAsync(byte[] content, string fileName)
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, ClearingHouseId = 1, HolidayStrategy = "Colombian" });
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 2,
            Name = "CENIT",
            Code = "CENIT",
            OriginCode = "011111111",
            ClearingHouseId = 1
        });
        const int cycleNumber = 1;
        var cycleId = $"CENIT-20260713-{cycleNumber:00}";
        context.AchCycles.Add(new AchCycle
        {
            Id = cycleId,
            CycleName = $"Ciclo {cycleNumber} - CENIT",
            ProcessingDate = OperationalDate,
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 59),
            CutoffTime = new TimeSpan(23, 0, 0),
            ClearingHouseId = 2
        });
        await context.SaveChangesAsync();

        var transition = new Mock<IAchStateTransitionService>();
        transition.Setup(service => service.TransitionAsync(
                It.IsAny<int>(), It.IsAny<AchTransferStateEnum>(), It.IsAny<AchStateEventSourceEnum>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchTransaction());
        var parser = new NachaParserService(context, Mock.Of<ILogger<NachaParserService>>(), transition.Object);

        try
        {
            await using var stream = new MemoryStream(content, writable: false);
            var result = await parser.ParseAndSaveDetailedAsync(stream, fileName, new NachaParseRequest
            {
                ResolvedClearingHouseId = 2,
                ResolvedAchCycleId = cycleId,
                OperationalDate = OperationalDate,
                CorrelationId = $"cenit-characterization-{cycleNumber}"
            });
            var header = await context.NachaHeaders.AsNoTracking().SingleAsync();
            var batches = await context.BatchHeaders.AsNoTracking().OrderBy(batch => batch.BatchNumber).ToListAsync();
            var entries = await context.EntryDetails.AsNoTracking().ToListAsync();
            var addendas = await context.AddendaRecords.AsNoTracking().ToListAsync();
            var batchControls = await context.BatchControls.AsNoTracking().ToListAsync();
            var fileControl = await context.FileControls.AsNoTracking().SingleOrDefaultAsync();
            return new
            {
                accepted = result.ErrorCount == 0,
                result.TotalBatches,
                result.TotalEntries,
                result.TotalAddendas,
                result.ErrorCount,
                failureGroups = result.Failures
                    .GroupBy(failure => new { failure.RecordType, failure.TransactionCode, failure.Reason })
                    .Select(group => new { group.Key.RecordType, group.Key.TransactionCode, group.Key.Reason, count = group.Count() })
                    .OrderBy(group => group.RecordType).ThenBy(group => group.TransactionCode).ToArray(),
                header.ImmediateOrigin,
                header.ImmediateDestination,
                header.FileCreationDate,
                header.ClearingHouseId,
                header.AchCycleId,
                serviceClasses = batches.Select(batch => batch.ServiceClassCode?.Trim()).Distinct().OrderBy(value => value).ToArray(),
                originParticipantCodes = batches.Select(batch => batch.OriginParticipantEntityCode?.Trim()).Distinct().OrderBy(value => value).ToArray(),
                transactionCodes = entries.Select(item => item.TransactionCode).GroupBy(value => value).ToDictionary(group => group.Key ?? string.Empty, group => group.Count()),
                receivingDfis = entries.Select(item => $"{item.ReceivingParticipantEntityCode}{item.CheckDigit}").Distinct().OrderBy(value => value).ToArray(),
                accountCount = entries.Select(item => item.AccountNumber).Distinct().Count(),
                accountSamples = entries.Select(item => MaskAccount(item.AccountNumber)).Distinct().OrderBy(value => value).Take(5).ToArray(),
                amountCount = entries.Count,
                amountMin = entries.Count == 0 ? 0m : entries.Min(item => item.Amount),
                amountMax = entries.Count == 0 ? 0m : entries.Max(item => item.Amount),
                amountTotal = entries.Sum(item => item.Amount),
                addendaCount = addendas.Count,
                batchTotals = batchControls.Select(control => new { control.BatchNumber, control.EntryAddendaCount, control.EntryHash, control.TotalDebitAmount, control.TotalCreditAmount }).ToArray(),
                fileTotals = fileControl is null ? null : new { fileControl.BatchCount, fileControl.BlockCount, fileControl.EntryAddendaCount, fileControl.EntryHash, fileControl.TotalDebitAmount, fileControl.TotalCreditAmount }
            };
        }
        catch (Exception exception)
        {
            return new { accepted = false, exception = exception.Message };
        }
    }

    private static string MaskAccount(string? account)
    {
        var value = account?.Trim() ?? string.Empty;
        return value.Length <= 4 ? new string('*', value.Length) : $"{new string('*', value.Length - 4)}{value[^4..]}";
    }

    private static async Task<string> HashFileAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream));
    }
}
