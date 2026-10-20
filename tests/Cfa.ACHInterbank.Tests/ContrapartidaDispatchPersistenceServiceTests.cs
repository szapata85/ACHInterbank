using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class ContrapartidaDispatchPersistenceServiceTests
{
    [Fact]
    public async Task EnsurePendingDispatchAsync_UsesBatchNavigation_WhenTransactionBatchIdIsTemporary()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();

        context.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = 1,
            HolidayStrategy = "Colombian"
        });

        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 1,
            Name = "ACH Colombia",
            Code = "ACH",
            OriginCode = "12345678",
            ClearingHouseId = 1
        });

        var sourceFi = new FinancialInstitution
        {
            Id = 1,
            Name = "Banco Origen",
            Status = FinancialInstitutionStatus.Active,
            IsDefaultSource = true,
            RoutingNumber = "12345",
            TransitCode = "678"
        };
        sourceFi.CalculateCheckDigit();

        var destinationFi = new FinancialInstitution
        {
            Id = 2,
            Name = "Banco Destino",
            Status = FinancialInstitutionStatus.Active,
            IsDefaultSource = false,
            RoutingNumber = "76543",
            TransitCode = "210"
        };
        destinationFi.CalculateCheckDigit();

        context.FinancialInstitutions.AddRange(sourceFi, destinationFi);

        var companyEntryDescriptionId = await context.CompanyEntryDescriptionCatalogs
            .Where(x => x.Term == "NOMINAS" && x.IsActive)
            .Select(x => x.Id)
            .FirstAsync();

        var cycle = new AchCycle
        {
            Id = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            ClearingHouseId = 1,
            CycleName = "CICLO-1",
            ProcessingDate = DateTime.UtcNow.Date,
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = new TimeSpan(22, 0, 0)
        };
        context.AchCycles.Add(cycle);

        var batch = new AchBatch
        {
            AchCycleId = cycle.Id,
            CompanyName = "EMPRESA",
            CompanyIdentification = "900123456",
            CompanyEntryDescription = "NOMINAS",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            OriginOrOdfi = "12345678",
            EffectiveEntryDate = cycle.ProcessingDate,
            ServiceClassCode = "220",
            BatchSequenceNumber = 1
        };

        var tx = new AchTransaction
        {
            Amount = 1000m,
            TransactionExternalId = "TX-OP-001",
            Reference = "REF-001",
            Type = TransactionTypeEnum.Debit,
            TransactionCode = "27",
            ServiceClassCode = "225",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            CompanyName = "EMPRESA",
            CompanyIdentification = "900123456",
            OriginatingDFI = "123456780",
            ReceivingDFI = "765432100",
            TraceNumber = "123456780000001",
            TraceSequenceNumber = 1,
            EffectiveEntryDate = cycle.ProcessingDate,
            AddendaRecordIndicator = true,
            IsPrenotification = false,
            Direction = AchTransactionDirection.Outgoing,
            Origin = AchTransactionOrigin.Cfa,
            MonetaryIntegrationRoute = AchMonetaryIntegrationRoute.ProcContrapartidas,
            ClassificationStatus = AchTransactionClassificationStatus.Determined,
            SourceInstitutionWasDefaultAtCreation = true,
            ClassifiedAtUtc = DateTime.UtcNow,
            ClassificationVersion = 1,
            SourceAccountNumber = "111122223333",
            DestinationAccountNumber = "999988887777",
            SourceInstitutionId = 1,
            DestinationInstitutionId = 2,
            AchCycleId = cycle.Id,
            AchBatch = batch
        };

        context.AchTransactions.Add(tx);

        var service = new ContrapartidaDispatchPersistenceService(context);
        await service.EnsurePendingDispatchAsync(tx, clearingHouseId: 1, CancellationToken.None);
        await context.SaveChangesAsync();

        var item = await context.ContrapartidaDispatchItems.SingleAsync();
        Assert.True(item.AchBatchId > 0);
        Assert.Equal(tx.AchBatchId, item.AchBatchId);
    }
}
