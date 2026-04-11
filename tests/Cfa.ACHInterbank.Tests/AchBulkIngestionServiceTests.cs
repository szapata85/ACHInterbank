using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class AchBulkIngestionServiceTests
{
    [Fact]
    public async Task SubmitAsync_DelegatesInlineSynchronousToBulkTransactionService()
    {
        var bulkTx = new Mock<IAchBulkTransactionService>();
        bulkTx.Setup(x => x.RegisterBulkAsync(It.IsAny<BulkAchTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BulkAchTransactionResponse
            {
                BatchReference = "BATCH-1",
                TotalReceived = 1,
                TotalProcessed = 1,
                TotalSucceeded = 1,
                TotalFailed = 0
            });

        var service = new AchBulkIngestionService(bulkTx.Object);
        var result = await service.SubmitAsync(new BulkIngestionRequest
        {
            BatchReference = "BATCH-1",
            SourceType = BulkIngestionSourceType.InlineTransactions,
            ProcessingMode = BulkIngestionProcessingMode.Synchronous,
            Transactions = [
                new BulkAchTransactionItemRequest
                {
                    Amount = 1000,
                    Reference = "R-1",
                    Type = TransactionTypeEnum.Credit,
                    AccountType = AccountTypeEnum.Checking,
                    DestinationInstitutionId = 2,
                    SourceAccountNumber = "1234567890",
                    DestinationAccountNumber = "9876543210",
                    CompanyName = "EMPRESA",
                    CompanyIdentification = "900123456",
                    CompanyEntryDescriptionId = 1
                }
            ]
        });

        Assert.Equal(BulkIngestionProcessingMode.Synchronous, result.ProcessingMode);
        Assert.NotNull(result.ImmediateResult);
        Assert.Equal(1, result.ImmediateResult!.TotalSucceeded);
    }

    [Fact]
    public async Task SubmitAsync_ReturnsNotImplementedStatus_ForAsyncMode()
    {
        var bulkTx = new Mock<IAchBulkTransactionService>();
        var service = new AchBulkIngestionService(bulkTx.Object);

        var result = await service.SubmitAsync(new BulkIngestionRequest
        {
            BatchReference = "BATCH-ASYNC",
            ProcessingMode = BulkIngestionProcessingMode.AsynchronousJob
        });

        Assert.Equal(BulkIngestionProcessingMode.AsynchronousJob, result.ProcessingMode);
        Assert.Equal("NOT_IMPLEMENTED", result.Status);
    }
}
