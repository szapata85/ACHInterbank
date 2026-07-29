using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class NachaTransactionValidationServiceTests
{
    [Fact]
    public void Constructor_RejectsUnavailablePrerequisitePolicy_InsteadOfEnablingFallback()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var context = CreateContext(connection);
        var holidays = Mock.Of<IBankHoliday>();

        Assert.Throws<ArgumentNullException>(
            () => new NachaTransactionValidationService(context, holidays, null!));
    }

    [Fact]
    public async Task ValidateTransactionsForSendAsync_DelegatesMonetaryDecisionToCanonicalPolicy()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var context = CreateContext(connection);
        var policy = new Mock<ITransactionPrerequisitePolicyService>();
        policy
            .Setup(x => x.ValidateForNachaExportAsync(
                It.IsAny<AchTransaction>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionPrerequisiteValidationResult(true, "OK", "Válida", null));
        var sut = new NachaTransactionValidationService(context, Mock.Of<IBankHoliday>(), policy.Object);
        var transaction = BuildTransaction(TransactionTypeEnum.Credit, "22");

        await sut.ValidateTransactionsForSendAsync([transaction], CancellationToken.None);

        policy.Verify(
            x => x.ValidateForNachaExportAsync(transaction, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateTransactionsForSendAsync_UsesTableDrivenPolicy_ForReversalCodeThatLooksLikeDebit()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        using var context = CreateContext(connection);
        var holidays = new Mock<IBankHoliday>();
        holidays.Setup(x => x.GetHolidays(It.IsAny<int>())).Returns([]);
        var policy = new TransactionPrerequisitePolicyService(context, holidays.Object);
        var sut = new NachaTransactionValidationService(context, holidays.Object, policy);
        var transaction = BuildTransaction(TransactionTypeEnum.Reversal, "27");

        await sut.ValidateTransactionsForSendAsync([transaction], CancellationToken.None);
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

    private static AchTransaction BuildTransaction(TransactionTypeEnum type, string code)
        => new()
        {
            Id = 1,
            Type = type,
            TransactionCode = code,
            Amount = 100m,
            Reference = "VALIDATION-001",
            TransactionExternalId = "VALIDATION-001",
            SourceAccountNumber = "SOURCE",
            DestinationAccountNumber = "DESTINATION",
            SourceInstitutionId = 1,
            DestinationInstitutionId = 2,
            AchCycleId = "CYCLE-001",
            AchBatchId = 1,
            EffectiveEntryDate = new DateTime(2026, 7, 14),
            CompanyEntryDescriptionId = 1
        };
}
