using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Repositories.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class PrenotificationHandlerTests
{
    [Fact]
    public async Task HandleAsync_CreatesThirdParty_WithCustomerNavigation_WhenCustomerIsTrackedWithTemporaryKey()
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
            CompanyEntryDescriptionId = 1,
            OriginOrOdfi = "12345678",
            EffectiveEntryDate = cycle.ProcessingDate,
            ServiceClassCode = "220",
            BatchSequenceNumber = 1
        };

        context.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog
        {
            Id = 1,
            Term = "NOMINAS",
            Description = "Pago nómina",
            StandardEntryClassCode = "PPD",
            IsActive = true
        });

        var customer = new Customer
        {
            FirstName = "Empresa",
            LastName = "Demo",
            PersonType = "PJ",
            DocumentType = "NIT",
            DocumentNumber = "900123456",
            CompanyName = "EMPRESA"
        };
        customer.Accounts.Add(new CustomerAccount { AccountNumber = "111122223333" });
        context.Customers.Add(customer);

        var tx = new AchTransaction
        {
            Amount = 0m,
            TransactionExternalId = "TX-PRN-001",
            Reference = "REF-PRN-001",
            Type = TransactionTypeEnum.Prenotification,
            TransactionCode = "23",
            ServiceClassCode = "220",
            CompanyEntryDescriptionId = 1,
            CompanyName = "EMPRESA",
            CompanyIdentification = "900123456",
            OriginatingDFI = "123456780",
            ReceivingDFI = "765432100",
            TraceNumber = "123456780000001",
            TraceSequenceNumber = 1,
            EffectiveEntryDate = DateTime.UtcNow.Date,
            AddendaRecordIndicator = true,
            IsPrenotification = true,
            SourceAccountNumber = "111122223333",
            DestinationAccountNumber = "999988887777",
            SourceInstitutionId = 1,
            DestinationInstitutionId = 2,
            AchCycleId = cycle.Id,
            AchBatch = batch
        };
        context.AchTransactions.Add(tx);

        var customerRepo = new AchCustomerRepository(context);
        var thirdPartyRepo = new CustomerThirdPartyRepository(context);
        var handler = new PrenotificationHandler(customerRepo, thirdPartyRepo);

        var request = new AchTransactionRequestData
        {
            IsPrenotification = true,
            SourceAccountNumber = "111122223333",
            DestinationAccountNumber = "999988887777",
            DestinationInstitutionId = 2,
            RecipientIdNumber = "123456789"
        };

        await handler.HandleAsync(request, tx, CancellationToken.None);
        await context.SaveChangesAsync();

        var thirdParty = await context.CustomerThirdParties.SingleAsync();
        Assert.True(thirdParty.CustomerId > 0);
        Assert.Equal(customer.Id, thirdParty.CustomerId);
    }
}
