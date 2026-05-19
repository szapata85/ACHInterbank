using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class TransactionsControllerTests
{
    [Fact]
    public async Task CreateTransaction_ReturnsCreated_WhenSingleTransactionIsValid()
    {
        var txService = new Mock<IAchTransactionService>();
        txService.Setup(s => s.RegisterTransactionAsync(
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<AccountTypeEnum>(),
                It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<List<AddendaDto>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchTransaction { Id = 25, Reference = "UNIT-001" });

        var policy = new Mock<ITransactionPolicyService>();
        var bulk = new Mock<IAchBulkTransactionService>();
        var ingestion = new Mock<IAchBulkIngestionService>();
        var logger = new Mock<ILogger<TransactionsController>>();

        var controller = new TransactionsController(txService.Object, policy.Object, bulk.Object, ingestion.Object, logger.Object);

        var result = await controller.CreateTransaction(new AchTransactionRequest
        {
            Amount = 1000,
            Reference = "UNIT-001",
            Type = TransactionTypeEnum.Credit,
            AccountType = AccountTypeEnum.Checking,
            DestinationInstitutionId = 2,
            SourceAccountNumber = "1234567890",
            DestinationAccountNumber = "9876543210",
            CompanyName = "EMPRESA",
            CompanyIdentification = "900123456",
            CompanyEntryDescriptionId = 1,
            Addendas = [new AddendaDto { AddendaType = "05", Information = "Pago" }]
        }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var payload = Assert.IsType<AchTransaction>(created.Value);
        Assert.Equal(25, payload.Id);
    }

    [Fact]
    public async Task SubmitBulkIngestion_ReturnsBadRequest_WhenSourceIsUnsupported()
    {
        var txService = new Mock<IAchTransactionService>();
        var policy = new Mock<ITransactionPolicyService>();
        var bulk = new Mock<IAchBulkTransactionService>();
        var ingestion = new Mock<IAchBulkIngestionService>();
        ingestion.Setup(x => x.SubmitAsync(It.IsAny<BulkIngestionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotSupportedException("CSV no soportado"));
        var logger = new Mock<ILogger<TransactionsController>>();

        var controller = new TransactionsController(txService.Object, policy.Object, bulk.Object, ingestion.Object, logger.Object);

        var response = await controller.SubmitBulkIngestion(new BulkIngestionRequest
        {
            BatchReference = "BATCH-1",
            SourceType = BulkIngestionSourceType.CsvFile
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(response);
    }

    [Fact]
    public async Task CreateTransaction_AllowsOperationalIdWithoutLegacyReference()
    {
        var txService = new Mock<IAchTransactionService>();
        txService.Setup(s => s.RegisterTransactionAsync(
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<AccountTypeEnum>(),
                It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<List<AddendaDto>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchTransaction { Id = 26, TransactionExternalId = "TX-OP-026", Reference = string.Empty });

        var policy = new Mock<ITransactionPolicyService>();
        var bulk = new Mock<IAchBulkTransactionService>();
        var ingestion = new Mock<IAchBulkIngestionService>();
        var logger = new Mock<ILogger<TransactionsController>>();
        var controller = new TransactionsController(txService.Object, policy.Object, bulk.Object, ingestion.Object, logger.Object);

        var result = await controller.CreateTransaction(new AchTransactionRequest
        {
            Amount = 1000,
            TransactionExternalId = "TX-OP-026",
            Reference = string.Empty,
            Type = TransactionTypeEnum.Credit,
            AccountType = AccountTypeEnum.Checking,
            DestinationInstitutionId = 2,
            SourceAccountNumber = "1234567890",
            DestinationAccountNumber = "9876543210",
            CompanyName = "EMPRESA",
            CompanyIdentification = "900123456",
            CompanyEntryDescriptionId = 1
        }, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result);
        txService.Verify(s => s.RegisterTransactionAsync(
            It.IsAny<decimal>(),
            string.Empty,
            It.IsAny<TransactionTypeEnum>(),
            It.IsAny<AccountTypeEnum>(),
            It.IsAny<bool>(),
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            It.IsAny<string?>(),
            "TX-OP-026",
            It.IsAny<bool>(),
            It.IsAny<List<AddendaDto>?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTransaction_ReturnsBadRequestJson_WhenDuplicatePolicyRejects()
    {
        var duplicateMessage = "Ya existe una transacción equivalente para el mismo ciclo.";
        var txService = new Mock<IAchTransactionService>();
        txService.Setup(s => s.RegisterTransactionAsync(
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<AccountTypeEnum>(),
                It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<List<AddendaDto>?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(duplicateMessage));

        var policy = new Mock<ITransactionPolicyService>();
        var bulk = new Mock<IAchBulkTransactionService>();
        var ingestion = new Mock<IAchBulkIngestionService>();
        var logger = new Mock<ILogger<TransactionsController>>();
        var controller = new TransactionsController(txService.Object, policy.Object, bulk.Object, ingestion.Object, logger.Object);

        var result = await controller.CreateTransaction(new AchTransactionRequest
        {
            Amount = 1000,
            TransactionExternalId = "TX-DUP-001",
            Reference = "TX-DUP-001",
            Type = TransactionTypeEnum.Credit,
            AccountType = AccountTypeEnum.Checking,
            DestinationInstitutionId = 2,
            SourceAccountNumber = "1234567890",
            DestinationAccountNumber = "9876543210",
            CompanyName = "EMPRESA",
            CompanyIdentification = "900123456",
            CompanyEntryDescriptionId = 1
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var message = badRequest.Value?.GetType().GetProperty("message")?.GetValue(badRequest.Value)?.ToString();
        Assert.Equal(duplicateMessage, message);
    }

    [Fact]
    public async Task CreateTransaction_ReturnsBadRequest_WhenOperationalIdAndReferenceAreMissing()
    {
        var txService = new Mock<IAchTransactionService>();
        var policy = new Mock<ITransactionPolicyService>();
        var bulk = new Mock<IAchBulkTransactionService>();
        var ingestion = new Mock<IAchBulkIngestionService>();
        var logger = new Mock<ILogger<TransactionsController>>();
        var controller = new TransactionsController(txService.Object, policy.Object, bulk.Object, ingestion.Object, logger.Object);

        var result = await controller.CreateTransaction(new AchTransactionRequest
        {
            Amount = 1000,
            TransactionExternalId = " ",
            Reference = " ",
            Type = TransactionTypeEnum.Credit,
            AccountType = AccountTypeEnum.Checking,
            DestinationInstitutionId = 2,
            SourceAccountNumber = "1234567890",
            DestinationAccountNumber = "9876543210",
            CompanyName = "EMPRESA",
            CompanyIdentification = "900123456",
            CompanyEntryDescriptionId = 1
        }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        txService.Verify(s => s.RegisterTransactionAsync(
            It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<TransactionTypeEnum>(), It.IsAny<AccountTypeEnum>(),
            It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<List<AddendaDto>?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
