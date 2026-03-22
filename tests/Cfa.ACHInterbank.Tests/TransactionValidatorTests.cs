using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class TransactionValidatorTests
{
    [Fact]
    public void ValidateRequest_WhenAccountsAreEqual_Throws()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;
        using var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        context.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog
        {
            Id = 1,
            Term = "PAGOS PSE",
            Description = "Pagos PSE",
            StandardEntryClassCode = "CCD",
            IsActive = true
        });
        context.SaveChanges();

        var validator = new TransactionValidator(context);
        var request = new AchTransactionRequestData
        {
            Amount = 1000m,
            Reference = "REF-001",
            Type = TransactionTypeEnum.Credit,
            AccountType = AccountTypeEnum.Checking,
            DestinationInstitutionId = 10,
            SourceAccountNumber = "1234567890",
            DestinationAccountNumber = "1234567890",
            CompanyName = "EMPRESA",
            CompanyIdentification = "123456789",
            CompanyEntryDescriptionId = 1
        };

        Assert.Throws<ArgumentException>(() => validator.ValidateRequest(request));
    }

    [Fact]
    public void ValidateRequest_WhenRecipientIdIsInvalid_Throws()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;
        using var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        context.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog
        {
            Id = 1,
            Term = "RECAUDOS",
            Description = "Recaudos",
            StandardEntryClassCode = "PPD",
            IsActive = true
        });
        context.SaveChanges();

        var validator = new TransactionValidator(context);
        var request = new AchTransactionRequestData
        {
            Amount = 1000m,
            Reference = "REF-001",
            Type = TransactionTypeEnum.Debit,
            AccountType = AccountTypeEnum.Checking,
            DestinationInstitutionId = 10,
            SourceAccountNumber = "1234567890",
            DestinationAccountNumber = "0987654321",
            CompanyName = "EMPRESA",
            CompanyIdentification = "123456789",
            CompanyEntryDescriptionId = 1,
            RecipientIdNumber = "12",
            RecipientName = "CLIENTE"
        };

        Assert.Throws<ArgumentException>(() => validator.ValidateRequest(request));
    }
}
