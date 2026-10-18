using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
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
    public async Task PreviewPolicy_ReturnsIntegratedCfaOperationalRestrictionDecision()
    {
        var txService = new Mock<IAchTransactionService>();
        var expected = new TransactionPolicyPreview(
            false,
            "El tipo de producto ElectronicDeposits está bloqueado por una restricción operacional CFA.",
            "CYCLE-5",
            "Ciclo 5",
            new DateTime(2026, 8, 3),
            "ACH Colombia",
            7,
            "2026-08-03 12:00 - 2026-08-03 14:00",
            true,
            25_000_000m,
            250_000_000m,
            500,
            "idem-1",
            false);
        var policy = new Mock<ITransactionPolicyService>();
        policy.Setup(x => x.PreviewAsync(It.IsAny<TransactionPolicyPreviewRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var bulk = new Mock<IAchBulkTransactionService>();
        var ingestion = new Mock<IAchBulkIngestionService>();
        var logger = new Mock<ILogger<TransactionsController>>();
        var controller = new TransactionsController(txService.Object, policy.Object, bulk.Object, ingestion.Object, logger.Object);

        var result = await controller.PreviewPolicy(
            1_000m,
            null,
            "PREVIEW-CFA-001",
            TransactionTypeEnum.Debit,
            AccountTypeEnum.ElectronicDeposits,
            false,
            2,
            "1234567890",
            "9876543210",
            "900123456",
            null,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
        policy.Verify(x => x.PreviewAsync(
            It.Is<TransactionPolicyPreviewRequest>(request =>
                request.Type == TransactionTypeEnum.Debit
                && request.AccountType == AccountTypeEnum.ElectronicDeposits
                && request.Reference == "PREVIEW-CFA-001"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ThirdPartyAccountSelector_ForwardsCanonicalTransactionContext()
    {
        var txService = new Mock<IAchTransactionService>();
        var policy = new Mock<ITransactionPolicyService>();
        var bulk = new Mock<IAchBulkTransactionService>();
        var ingestion = new Mock<IAchBulkIngestionService>();
        var selector = new Mock<ITransactionThirdPartyAccountSelectorService>();
        var expected = new TransactionThirdPartyAccountSelectorResponse(
            10,
            7,
            2,
            "CENIT",
            "CENIT",
            Cfa.ACHInterbank.Domain.Models.ACH.Enums.TransactionNature.Debit,
            new DateTime(2026, 10, 12),
            "PPD",
            []);
        selector.Setup(x => x.GetAsync(
                It.IsAny<TransactionThirdPartyAccountSelectorRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var logger = new Mock<ILogger<TransactionsController>>();
        var controller = new TransactionsController(
            txService.Object,
            policy.Object,
            bulk.Object,
            ingestion.Object,
            logger.Object,
            thirdPartyAccountSelectorService: selector.Object);

        var result = await controller.GetThirdPartyAccountSelector(
            "1234567890",
            7,
            TransactionTypeEnum.Debit,
            12,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
        selector.Verify(x => x.GetAsync(
            It.Is<TransactionThirdPartyAccountSelectorRequest>(request =>
                request.SourceAccountNumber == "1234567890"
                && request.DestinationInstitutionId == 7
                && request.TransactionType == TransactionTypeEnum.Debit
                && request.CompanyEntryDescriptionId == 12),
            It.IsAny<CancellationToken>()), Times.Once);
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

    [Fact]
    public async Task GetTransactionIntegrationReadiness_ShouldReturnExpectedOperation()
    {
        var txService = new Mock<IAchTransactionService>();
        var policy = new Mock<ITransactionPolicyService>();
        var bulk = new Mock<IAchBulkTransactionService>();
        var ingestion = new Mock<IAchBulkIngestionService>();
        var logger = new Mock<ILogger<TransactionsController>>();
        var readiness = new Mock<ITransactionIntegrationReadinessService>();
        readiness.Setup(x => x.GetTransactionReadinessAsync(254, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionIntegrationReadinessResult(
                254,
                "UAT-ACH-DEB-MATURED-001",
                IntegrationGuaranteeConstants.Wscfaach,
                IntegrationGuaranteeConstants.ProcContrapartidas,
                IntegrationGuaranteeConstants.MonetaryDebitRequest,
                IntegrationGuaranteeConstants.OutboundRequest,
                "Debito monetario",
                "CFA originadora",
                true,
                "Debito monetario originado por CFA.",
                true,
                new IntegrationMappingReadinessResult(
                    true,
                    "Ok",
                    "OK",
                    IntegrationGuaranteeConstants.Wscfaach,
                    IntegrationGuaranteeConstants.ProcContrapartidas,
                    IntegrationGuaranteeConstants.MonetaryDebitRequest,
                    IntegrationGuaranteeConstants.OutboundRequest,
                    10,
                    10,
                    [],
                    [],
                    [],
                    [],
                    false,
                    true,
                    [],
                    [])));

        var controller = new TransactionsController(txService.Object, policy.Object, bulk.Object, ingestion.Object, logger.Object, readiness.Object);

        var response = await controller.GetIntegrationReadiness(254, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response);
        var payload = Assert.IsType<TransactionIntegrationReadinessResult>(ok.Value);
        Assert.Equal(IntegrationGuaranteeConstants.ProcContrapartidas, payload.OperationKey);
        Assert.True(payload.Readiness.IsReady);
    }
}
