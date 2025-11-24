using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class AchTransactionNachaTests
{
    [Fact]
    public async Task RegisterTransactionAsync_CreatesTransactionAndBatch()
    {
        using var connection = CreateOpenConnection();

        AchTransaction tx = null!;
        using (var arrangeContext = CreateContext(connection))
        {
            SeedCoreEntities(arrangeContext);

            var routing = new Mock<IRoutingStrategyService>();
            routing
                .Setup(r => r.ResolveClearingHouseForTransactionAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var holiday = new Mock<IBankHoliday>();
            holiday
                .Setup(h => h.GetHolidays(It.IsAny<int>()))
                .Returns(new List<BankHolidayModel>());

            var service = new AchTransactionService(arrangeContext, routing.Object, holiday.Object);

            tx = await service.RegisterTransactionAsync(
                amount: 1500m,
                reference: "PAGO-REF-001",
                type: TransactionTypeEnum.Credit,
                destinationInstitutionId: 2,
                sourceAccountNumber: "111122223333",
                destinationAccountNumber: "999988887777",
                addendas: new List<AddendaDto>
                {
                    new() { AddendaType = "05", Information = "Factura #123" },
                    new() { AddendaType = "99", Information = "Pago complementario" }
                },
                ct: CancellationToken.None);

            Assert.NotEqual(0, tx.Id);
            Assert.Equal("PAGO-REF-001", tx.Reference);
            Assert.StartsWith("12345678", tx.TraceNumber);
            Assert.Equal("123456780", tx.CompanyIdentification);
            Assert.Equal("12345678", tx.OriginatingDFI);
            Assert.Equal("76543210", tx.ReceivingDFI);
        }

        using var verification = CreateContext(connection);

        var savedTransaction = await verification.AchTransactions
            .Include(t => t.AchBatch)
            .Include(t => t.Addendas)
            .SingleAsync();

        Assert.Equal(tx.Id, savedTransaction.Id);
        Assert.Equal("220", savedTransaction.AchBatch.ServiceClassCode);
        Assert.True(savedTransaction.AddendaRecordIndicator);
        Assert.Equal(2, savedTransaction.Addendas.Count);
        Assert.Collection(
            savedTransaction.Addendas.OrderBy(a => a.SequenceNumber),
            first =>
            {
                Assert.Equal("05", first.AddendaType);
                Assert.Equal("Factura #123", first.Information);
                Assert.Equal(1, first.SequenceNumber);
            },
            second =>
            {
                Assert.Equal("99", second.AddendaType);
                Assert.Equal("Pago complementario", second.Information);
                Assert.Equal(2, second.SequenceNumber);
            });
        Assert.Equal(1, savedTransaction.SourceInstitutionId);

        var batch = await verification.AchBatches.Include(b => b.Transactions).SingleAsync();
        Assert.Single(batch.Transactions);
        Assert.Equal(tx.Id, batch.Transactions.Single().Id);
    }

    [Fact]
    public async Task RegisterTransactionAsync_WithoutDefaultSource_Throws()
    {
        using var connection = CreateOpenConnection();

        using var arrangeContext = CreateContext(connection);
        SeedCoreEntities(arrangeContext);

        var defaultSource = await arrangeContext.FinancialInstitutions
            .SingleAsync(fi => fi.IsDefaultSource);
        defaultSource.IsDefaultSource = false;
        arrangeContext.FinancialInstitutions.Update(defaultSource);
        await arrangeContext.SaveChangesAsync();

        var routing = new Mock<IRoutingStrategyService>();
        routing
            .Setup(r => r.ResolveClearingHouseForTransactionAsync(
                It.IsAny<int>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var holiday = new Mock<IBankHoliday>();
        holiday
            .Setup(h => h.GetHolidays(It.IsAny<int>()))
            .Returns(new List<BankHolidayModel>());

        var service = new AchTransactionService(arrangeContext, routing.Object, holiday.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterTransactionAsync(
            amount: 1500m,
            reference: "PAGO-REF-003",
            type: TransactionTypeEnum.Credit,
            destinationInstitutionId: 2,
            sourceAccountNumber: "111122223333",
            destinationAccountNumber: "999988887777",
            addendas: null,
            ct: CancellationToken.None));
    }

    [Fact]
    public async Task BuildNachaFileByCycleAsync_GeneratesSequentialRecords()
    {
        using var connection = CreateOpenConnection();

        using (var arrangeContext = CreateContext(connection))
        {
            SeedCoreEntities(arrangeContext);
            SeedNachaLayouts(arrangeContext);

            var routing = new Mock<IRoutingStrategyService>();
            routing
                .Setup(r => r.ResolveClearingHouseForTransactionAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var holiday = new Mock<IBankHoliday>();
            holiday
                .Setup(h => h.GetHolidays(It.IsAny<int>()))
                .Returns(new List<BankHolidayModel>());

            var transactionService = new AchTransactionService(arrangeContext, routing.Object, holiday.Object);

            await transactionService.RegisterTransactionAsync(
                amount: 1500m,
                reference: "PAGO-REF-002",
                type: TransactionTypeEnum.Credit,
                destinationInstitutionId: 2,
                sourceAccountNumber: "111122223333",
                destinationAccountNumber: "999988887777",
                addendas: null,
                ct: CancellationToken.None);
        }

        using var executionContext = CreateContext(connection);
        var builder = new NachaFileBuilder(executionContext);
        var nachaContent = await builder.BuildNachaFileByCycleAsync(1, CancellationToken.None);

        // 5 registros esperados: 1,5,6,8,9
        Assert.Equal(130, nachaContent.Length);

        var segments = new List<string>
        {
            nachaContent[..20],
            nachaContent.Substring(20, 30),
            nachaContent.Substring(50, 40),
            nachaContent.Substring(90, 20),
            nachaContent.Substring(110, 20)
        };

        Assert.StartsWith("1", segments[0]);
        Assert.StartsWith("5", segments[1]);
        Assert.StartsWith("6", segments[2]);
        Assert.StartsWith("8", segments[3]);
        Assert.StartsWith("9", segments[4]);

        Assert.Contains("PAGO-REF-002", segments[2]);
        Assert.Contains("0000150000", segments[2]);
    }

    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    private static AchDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static void SeedCoreEntities(AchDbContext context)
    {
        var config = new ClearingHouseConfig
        {
            Id = 1,
            ClearingHouseId = 1,
            HolidayStrategy = "Test"
        };

        var clearingHouse = new ClearingHouse
        {
            Id = 1,
            Name = "ACH Test",
            Code = "ACH",
            OriginCode = "ORG",
            ClearingHouseId = 1,
            ClearingHouseConfig = config
        };

        var cycle = new AchCycle
        {
            Id = 1,
            CycleName = "CICLO-TEST",
            ProcessingDate = DateTime.Today,
            CutoffTime = TimeSpan.FromHours(17),
            RescheduleOnHoliday = false,
            ClearingHouseId = 1,
            ClearingHouse = clearingHouse
        };

        var sourceInstitution = new FinancialInstitution
        {
            Id = 1,
            Name = "Banco Origen",
            IsDefaultSource = true,
            RoutingNumber = "1234567",
            TransitCode = "8",
            Status = FinancialInstitutionStatus.Active
        };
        sourceInstitution.CalculateCheckDigit();

        var destinationInstitution = new FinancialInstitution
        {
            Id = 2,
            Name = "Banco Destino",
            IsDefaultSource = false,
            RoutingNumber = "7654321",
            TransitCode = "0",
            Status = FinancialInstitutionStatus.Active
        };
        destinationInstitution.CalculateCheckDigit();

        context.ClearingHouseConfigs.Add(config);
        context.ClearingHouses.Add(clearingHouse);
        context.AchCycles.Add(cycle);
        var alternativeSource = new FinancialInstitution
        {
            Id = 3,
            Name = "Banco Alterno",
            IsDefaultSource = false,
            RoutingNumber = "3333333",
            TransitCode = "1",
            Status = FinancialInstitutionStatus.Active
        };
        alternativeSource.CalculateCheckDigit();

        context.FinancialInstitutions.AddRange(sourceInstitution, destinationInstitution, alternativeSource);
        context.SaveChanges();
    }

    private static void SeedNachaLayouts(AchDbContext context)
    {
        var layout1 = new NachaRecordLayout
        {
            RecordType = "1",
            RecordCode = "1",
            TotalLength = 20,
            Description = "File Header"
        };
        layout1.Fields.Add(new NachaRecordField
        {
            FieldName = "CycleName",
            StartPosition = 2,
            Length = 10,
            PadChar = ' ',
            Justification = 'L',
            DbColumn = nameof(AchCycle.CycleName),
            Layout = layout1
        });
        layout1.Fields.Add(new NachaRecordField
        {
            FieldName = "ProcessingDate",
            StartPosition = 12,
            Length = 8,
            PadChar = '0',
            Justification = 'R',
            DbColumn = nameof(AchCycle.ProcessingDate),
            Format = "yyyyMMdd",
            Layout = layout1
        });

        var layout5 = new NachaRecordLayout
        {
            RecordType = "5",
            RecordCode = "5",
            TotalLength = 30,
            Description = "Batch Header"
        };
        layout5.Fields.Add(new NachaRecordField
        {
            FieldName = "CompanyName",
            StartPosition = 2,
            Length = 20,
            PadChar = ' ',
            Justification = 'L',
            DbColumn = nameof(AchBatch.CompanyName),
            Layout = layout5
        });
        layout5.Fields.Add(new NachaRecordField
        {
            FieldName = "EffectiveEntryDate",
            StartPosition = 22,
            Length = 8,
            PadChar = '0',
            Justification = 'R',
            DbColumn = nameof(AchBatch.EffectiveEntryDate),
            Format = "yyyyMMdd",
            Layout = layout5
        });

        var layout6 = new NachaRecordLayout
        {
            RecordType = "6",
            RecordCode = "6",
            TotalLength = 40,
            Description = "Entry Detail"
        };
        layout6.Fields.Add(new NachaRecordField
        {
            FieldName = "Reference",
            StartPosition = 2,
            Length = 20,
            PadChar = ' ',
            Justification = 'L',
            DbColumn = nameof(AchTransaction.Reference),
            Layout = layout6
        });
        layout6.Fields.Add(new NachaRecordField
        {
            FieldName = "Amount",
            StartPosition = 22,
            Length = 10,
            PadChar = '0',
            Justification = 'R',
            DbColumn = nameof(AchTransaction.Amount),
            Layout = layout6
        });
        layout6.Fields.Add(new NachaRecordField
        {
            FieldName = "TraceNumber",
            StartPosition = 32,
            Length = 9,
            PadChar = ' ',
            Justification = 'L',
            DbColumn = nameof(AchTransaction.TraceNumber),
            Layout = layout6
        });

        var layout7 = new NachaRecordLayout
        {
            RecordType = "7",
            RecordCode = "7",
            TotalLength = 20,
            Description = "Addenda"
        };
        layout7.Fields.Add(new NachaRecordField
        {
            FieldName = "Information",
            StartPosition = 2,
            Length = 18,
            PadChar = ' ',
            Justification = 'L',
            DbColumn = nameof(AchTransactionAddenda.Information),
            Layout = layout7
        });

        var layout8 = new NachaRecordLayout
        {
            RecordType = "8",
            RecordCode = "8",
            TotalLength = 20,
            Description = "Batch Control"
        };
        layout8.Fields.Add(new NachaRecordField
        {
            FieldName = "CompanyName",
            StartPosition = 2,
            Length = 18,
            PadChar = ' ',
            Justification = 'L',
            DbColumn = nameof(AchBatch.CompanyName),
            Layout = layout8
        });

        var layout9 = new NachaRecordLayout
        {
            RecordType = "9",
            RecordCode = "9",
            TotalLength = 20,
            Description = "File Control"
        };
        layout9.Fields.Add(new NachaRecordField
        {
            FieldName = "CycleName",
            StartPosition = 2,
            Length = 18,
            PadChar = ' ',
            Justification = 'L',
            DbColumn = nameof(AchCycle.CycleName),
            Layout = layout9
        });

        context.NachaRecordLayouts.AddRange(layout1, layout5, layout6, layout7, layout8, layout9);
        context.SaveChanges();
    }
}
