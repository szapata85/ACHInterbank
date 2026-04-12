using Cfa.ACHInterbank.Application.Reports.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Reports;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class ReportServicesDataQualityTests
{
    [Fact]
    public async Task SentTransactions_AppliesFiltersPaginationAndLatestNachaFile()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var arrange = new AchDbContext(options))
        {
            await SeedScenarioAsync(arrange);
        }

        await using var context = new AchDbContext(options);
        var service = new AchTransactionReportService(context);

        var response = await service.GetSentTransactionsAsync(new AchTransactionReportFilter
        {
            BankId = 1,
            ClearingHouseId = 1,
            Page = 1,
            PageSize = 1
        });

        Assert.Equal(2, response.Total);
        Assert.Single(response.Items);
        Assert.Equal("REF-SENT-NEW", response.Items[0].Reference);
        Assert.Equal("NACHA_cycle-1_latest.txt", response.Items[0].NachaFileName);
        Assert.Equal(250m, response.Totals.TotalCreditAmount);
        Assert.Equal(200m, response.Totals.TotalDebitAmount);
    }

    [Fact]
    public async Task ReturnsReport_FiltersByReturnCodes_AndResolvesCausalAndOriginalTransaction()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var arrange = new AchDbContext(options))
        {
            await SeedScenarioAsync(arrange);
        }

        await using var context = new AchDbContext(options);
        var service = new AchReturnRejectionReportService(context);

        var response = await service.GetReturnsAsync(new AchReturnRejectionReportFilter
        {
            Causal = "dev14"
        });

        Assert.Single(response.Items);
        var row = response.Items[0];
        Assert.Equal("DEV14", row.CausalCode);
        Assert.Equal("Cuenta cerrada", row.CausalDescription);
        Assert.Equal("REF-SENT-OLD", row.OriginalTransactionReference);
        Assert.Equal(1001, row.OriginalTransactionId);
    }

    [Fact]
    public async Task ReconciliationReport_CalculatesTotalsDiffsAndInconsistencies()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var arrange = new AchDbContext(options))
        {
            await SeedScenarioAsync(arrange);
        }

        await using var context = new AchDbContext(options);
        var service = new AchReconciliationReportService(context);

        var response = await service.GetReconciliationAsync(new AchReconciliationReportFilter
        {
            ClearingHouseId = 1
        });

        Assert.Equal(7, response.Totals.SentCount);
        Assert.Equal(1054m, response.Totals.SentAmount);
        Assert.Equal(2, response.Totals.ReceivedCount);
        Assert.Equal(300m, response.Totals.ReceivedAmount);
        Assert.Equal(3, response.Totals.ReturnedCount);
        Assert.Equal(304m, response.Totals.ReturnedAmount);

        Assert.Equal(5, response.Differences.SentVsReceivedCountDiff);
        Assert.Equal(754m, response.Differences.SentVsReceivedAmountDiff);

        Assert.Contains(response.Inconsistencies, x => x.Code == "INC-RET-NO-CAUSAL" && x.AffectedCount == 1);
        Assert.Contains(response.Inconsistencies, x => x.Code == "INC-CAUSAL-STATE" && x.AffectedCount == 1);
        Assert.Contains(response.Inconsistencies, x => x.Code == "INC-NEG-AMOUNT" && x.AffectedCount == 1);
    }

    private static async Task SeedScenarioAsync(AchDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        context.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = 1,
            FileHeaderCode = "0",
            RecordSeparator = "\n",
            IsFixedLength = true,
            TotalLength = 106
        });

        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 1,
            Name = "ACH Colombia",
            Code = "ACHCOL",
            OriginCode = "12345678",
            ClearingHouseId = 1
        });

        var bank1 = CreateBank(1, "Banco A", "1234567");
        var bank2 = CreateBank(2, "Banco B", "7654321", false);
        var bank3 = CreateBank(3, "Banco C", "2223334", false);
        context.FinancialInstitutions.AddRange(bank1, bank2, bank3);

        var cycle = new AchCycle
        {
            Id = "cycle-1",
            CycleName = "CICLO-1",
            ProcessingDate = new DateTime(2026, 04, 01, 0, 0, 0, DateTimeKind.Utc),
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = new TimeSpan(21, 00, 0),
            ClearingHouseId = 1
        };
        context.AchCycles.Add(cycle);

        context.AchBatches.AddRange(
            new AchBatch
            {
                Id = 201,
                AchCycleId = cycle.Id,
                ServiceClassCode = "220",
                CompanyName = "Empresa 1",
                CompanyIdentification = "123456780",
                CompanyEntryDescription = "PAGOS",
                EffectiveEntryDate = new DateTime(2026, 04, 01, 0, 0, 0, DateTimeKind.Utc),
                OriginOrOdfi = "12345678",
                BatchSequenceNumber = 1
            },
            new AchBatch
            {
                Id = 202,
                AchCycleId = cycle.Id,
                ServiceClassCode = "225",
                CompanyName = "Empresa 2",
                CompanyIdentification = "876543210",
                CompanyEntryDescription = "NOMINA",
                EffectiveEntryDate = new DateTime(2026, 04, 01, 0, 0, 0, DateTimeKind.Utc),
                OriginOrOdfi = "12345678",
                BatchSequenceNumber = 2
            });

        context.AchFileExports.AddRange(
            new AchFileExport
            {
                AchCycleId = cycle.Id,
                ClearingHouseId = 1,
                ExportKind = "NACHA",
                FileName = "NACHA_cycle-1_old.txt",
                TotalRecords = 10,
                TotalTransactions = 4,
                IsEncrypted = false,
                GeneratedAtUtc = new DateTime(2026, 04, 01, 8, 00, 0, DateTimeKind.Utc)
            },
            new AchFileExport
            {
                AchCycleId = cycle.Id,
                ClearingHouseId = 1,
                ExportKind = "NACHA",
                FileName = "NACHA_cycle-1_latest.txt",
                TotalRecords = 12,
                TotalTransactions = 8,
                IsEncrypted = false,
                GeneratedAtUtc = new DateTime(2026, 04, 01, 9, 30, 0, DateTimeKind.Utc)
            });

        context.ReturnReasons.Add(new ReturnReason
        {
            Code = "DEV14",
            Description = "Cuenta cerrada",
            Category = "Return",
            IsForReturn = true
        });

        context.AchTransactions.AddRange(
            CreateTransaction(1001, 250m, "REF-SENT-NEW", TransactionTypeEnum.Credit, AchTransferStateEnum.Certified, "123456780000001", string.Empty, string.Empty, 1, 2, "cycle-1", 201, new DateTimeOffset(2026, 04, 01, 10, 30, 0, TimeSpan.Zero)),
            CreateTransaction(1002, 200m, "REF-SENT-OLD", TransactionTypeEnum.Debit, AchTransferStateEnum.Pending, "123456780000002", string.Empty, string.Empty, 1, 3, "cycle-1", 202, new DateTimeOffset(2026, 04, 01, 8, 30, 0, TimeSpan.Zero)),
            CreateTransaction(1003, 90m, "REF-OTHER-BANK", TransactionTypeEnum.Credit, AchTransferStateEnum.AppliedTacitly, "123456780000003", string.Empty, string.Empty, 2, 1, "cycle-1", 201, new DateTimeOffset(2026, 04, 01, 8, 40, 0, TimeSpan.Zero)),
            CreateTransaction(1004, 300m, "REF-RETURN-DEV", TransactionTypeEnum.Credit, AchTransferStateEnum.ReturnedByOperator, "123456780000004", "DEV14", "123456780000002", 2, 1, "cycle-1", 202, new DateTimeOffset(2026, 04, 01, 11, 00, 0, TimeSpan.Zero)),
            CreateTransaction(1005, -5m, "REF-NEGATIVE", TransactionTypeEnum.Credit, AchTransferStateEnum.Pending, "123456780000005", string.Empty, string.Empty, 2, 3, "cycle-1", 202, new DateTimeOffset(2026, 04, 01, 11, 20, 0, TimeSpan.Zero)),
            CreateTransaction(1006, 4m, "REF-RETURN-NO-CAUSAL", TransactionTypeEnum.Credit, AchTransferStateEnum.ReturnedByEpr, "123456780000006", string.Empty, string.Empty, 1, 3, "cycle-1", 201, new DateTimeOffset(2026, 04, 01, 11, 30, 0, TimeSpan.Zero)),
            CreateTransaction(1007, 215m, "REF-CAUSAL-WRONG-STATE", TransactionTypeEnum.Credit, AchTransferStateEnum.Pending, "123456780000007", "R01", string.Empty, 1, 2, "cycle-1", 201, new DateTimeOffset(2026, 04, 01, 11, 45, 0, TimeSpan.Zero)),
            CreateTransaction(1008, 0m, "REF-REJECT-D01", TransactionTypeEnum.Credit, AchTransferStateEnum.RejectedByOperator, "123456780000008", "D01", string.Empty, 1, 2, "cycle-1", 201, new DateTimeOffset(2026, 04, 01, 12, 00, 0, TimeSpan.Zero)));

        await context.SaveChangesAsync();
    }

    private static FinancialInstitution CreateBank(int id, string name, string routingNumber, bool isDefaultSource = true)
    {
        var bank = new FinancialInstitution
        {
            Id = id,
            Name = name,
            RoutingNumber = routingNumber,
            TransitCode = "0",
            Status = FinancialInstitutionStatus.Active,
            IsDefaultSource = isDefaultSource
        };

        bank.CalculateCheckDigit();
        return bank;
    }

    private static AchTransaction CreateTransaction(
        int id,
        decimal amount,
        string reference,
        TransactionTypeEnum type,
        AchTransferStateEnum state,
        string traceNumber,
        string returnReasonCode,
        string originalTraceRef,
        int sourceInstitutionId,
        int destinationInstitutionId,
        string cycleId,
        int batchId,
        DateTimeOffset createdAt)
    {
        return new AchTransaction
        {
            Id = id,
            Amount = amount,
            Reference = reference,
            Type = type,
            TransactionCode = "22",
            OriginatingDFI = "12345678",
            ReceivingDFI = "76543210",
            TraceNumber = traceNumber,
            TraceSequenceNumber = id,
            EffectiveEntryDate = new DateTime(2026, 04, 01, 0, 0, 0, DateTimeKind.Utc),
            State = state,
            StateChangedAtUtc = new DateTime(2026, 04, 01, 12, 0, 0, DateTimeKind.Utc),
            ReturnReasonCode = returnReasonCode,
            OriginalTraceRef = originalTraceRef,
            AddendaRecordIndicator = false,
            SourceAccountNumber = "111122223333",
            DestinationAccountNumber = "999988887777",
            SourceInstitutionId = sourceInstitutionId,
            DestinationInstitutionId = destinationInstitutionId,
            AchCycleId = cycleId,
            AchBatchId = batchId,
            CreatedAt = createdAt
        };
    }
}
