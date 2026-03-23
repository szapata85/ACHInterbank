using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class AchTraceabilityServiceTests
{
    [Fact]
    public async Task GetTransactionTraceabilityAsync_ExposesCycleClearingHouseAndGeneratedFiles()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        using (var arrangeContext = new AchDbContext(options))
        {
            arrangeContext.Database.EnsureCreated();

            arrangeContext.ClearingHouseConfigs.Add(new ClearingHouseConfig
            {
                Id = 1,
                FileHeaderCode = "0",
                RecordSeparator = "\n",
                IsFixedLength = true,
                TotalLength = 106
            });

            arrangeContext.ClearingHouses.Add(new ClearingHouse
            {
                Id = 1,
                Name = "ACH Colombia",
                Code = "ACHCOL",
                OriginCode = "12345678",
                ClearingHouseId = 1
            });

            var sourceInstitution = new FinancialInstitution
            {
                Id = 1,
                Name = "Banco Origen",
                RoutingNumber = "1234567",
                TransitCode = "0",
                Status = FinancialInstitutionStatus.Active,
                IsDefaultSource = true
            };
            sourceInstitution.CalculateCheckDigit();

            var destinationInstitution = new FinancialInstitution
            {
                Id = 2,
                Name = "Banco Destino",
                RoutingNumber = "7654321",
                TransitCode = "0",
                Status = FinancialInstitutionStatus.Active,
                IsDefaultSource = false
            };
            destinationInstitution.CalculateCheckDigit();

            arrangeContext.FinancialInstitutions.AddRange(sourceInstitution, destinationInstitution);

            var cycle = new AchCycle
            {
                Id = "cycle-1",
                CycleName = "CICLO-1",
                ProcessingDate = DateTime.UtcNow.Date,
                StartTime = TimeSpan.Zero,
                EndTime = new TimeSpan(23, 59, 0),
                CutoffTime = new TimeSpan(23, 59, 0),
                ClearingHouseId = 1
            };
            arrangeContext.AchCycles.Add(cycle);

            var batch = new AchBatch
            {
                Id = 1,
                AchCycleId = cycle.Id,
                ServiceClassCode = "220",
                CompanyName = "EMPRESA",
                CompanyIdentification = "123456780",
                CompanyEntryDescription = "NOMINAS",
                EffectiveEntryDate = DateTime.UtcNow.Date,
                OriginOrOdfi = "12345678"
            };
            arrangeContext.AchBatches.Add(batch);

            var transaction = new AchTransaction
            {
                Id = 10,
                Amount = 1500m,
                Reference = "REF-TRACE",
                Type = TransactionTypeEnum.Credit,
                TransactionCode = "22",
                OriginatingDFI = "12345678",
                ReceivingDFI = "76543210",
                TraceNumber = "123456780000001",
                TraceSequenceNumber = 1,
                EffectiveEntryDate = DateTime.UtcNow.Date,
                AddendaRecordIndicator = true,
                SourceAccountNumber = "111122223333",
                DestinationAccountNumber = "999988887777",
                SourceInstitutionId = 1,
                DestinationInstitutionId = 2,
                AchCycleId = cycle.Id,
                AchBatchId = 1,
                State = AchTransferStateEnum.Pending
            };
            arrangeContext.AchTransactions.Add(transaction);

            arrangeContext.AchFileExports.Add(new AchFileExport
            {
                AchCycleId = cycle.Id,
                ClearingHouseId = 1,
                ExportKind = "NACHA",
                FileName = "NACHA_cycle-1_20260323_100000.txt",
                TotalRecords = 8,
                TotalTransactions = 1,
                IsEncrypted = false,
                GeneratedAtUtc = new DateTime(2026, 03, 23, 10, 00, 00, DateTimeKind.Utc)
            });

            arrangeContext.AchReturnsGenerated.Add(new AchReturnGenerated
            {
                OriginalTransactionId = 10,
                ReturnCycleId = cycle.Id,
                ReturnReasonCode = "DEV14",
                Amount = 1500m,
                NewSequenceNumber = "765432100000001",
                OriginalSequenceNumber = "123456780000001",
                ReceiverEntityCode = "12345678",
                OriginatorEntityCode = "76543210",
                FileName = "RET_cycle-1_20260323110000.RET",
                GeneratedAtUtc = new DateTime(2026, 03, 23, 11, 00, 00, DateTimeKind.Utc)
            });

            await arrangeContext.SaveChangesAsync();
        }

        using var executionContext = new AchDbContext(options);
        var stateTransitionService = new Mock<IAchStateTransitionService>();
        var service = new AchTraceabilityService(executionContext, stateTransitionService.Object);

        var traceability = await service.GetTransactionTraceabilityAsync(10, CancellationToken.None);

        Assert.NotNull(traceability);
        Assert.Equal("cycle-1", traceability!.AchCycleId);
        Assert.Equal("CICLO-1", traceability.AchCycleName);
        Assert.Equal("ACH Colombia", traceability.ClearingHouseName);
        Assert.Equal("ACHCOL", traceability.ClearingHouseCode);
        Assert.Equal("NACHA_cycle-1_20260323_100000.txt", traceability.CurrentNachaFileName);
        Assert.Equal("RET_cycle-1_20260323110000.RET", traceability.ReturnFileName);
        Assert.Equal("cycle-1", traceability.ReturnCycleId);
        Assert.Equal(10, traceability.ReturnOriginalTransactionId);
        Assert.Equal("DEV14", traceability.ReturnReasonCode);
    }
}
