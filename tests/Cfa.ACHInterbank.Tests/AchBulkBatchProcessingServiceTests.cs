using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class AchBulkBatchProcessingServiceTests
{
    [Fact]
    public async Task ProcessBatchAsync_SetsCompleted_WhenAllItemsSucceed()
    {
        using var connection = CreateOpenConnection();
        await using var context = CreateContext(connection);

        var batch = SeedBatchWithReadyItems(context, 3, 0);
        var attempt = SeedAttempt(context, batch.Id, 1);

        var bulkTx = new Mock<IAchBulkTransactionService>();
        bulkTx.Setup(x => x.RegisterBulkAsync(It.IsAny<BulkAchTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildResponse(3, 3, 0));

        var service = CreateService(context, bulkTx);
        await service.ProcessBatchAsync(batch.Id, attempt.Id, "job-ok");

        var persisted = await context.BulkIngestionBatches.FirstAsync(x => x.Id == batch.Id);
        Assert.Equal(BulkIngestionBatchStatusEnum.Completed, persisted.Status);
        Assert.Equal(3, persisted.TotalProcessed);
        Assert.Equal(3, persisted.TotalSucceeded);
        Assert.Equal(0, persisted.TotalFailed);
    }

    [Fact]
    public async Task ProcessBatchAsync_SetsPartiallyProcessed_WhenSomeItemsFail()
    {
        using var connection = CreateOpenConnection();
        await using var context = CreateContext(connection);

        var batch = SeedBatchWithReadyItems(context, 4, 1);
        SeedAttempt(context, batch.Id, 1);

        var bulkTx = new Mock<IAchBulkTransactionService>();
        bulkTx.Setup(x => x.RegisterBulkAsync(It.IsAny<BulkAchTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BulkAchTransactionResponse
            {
                BatchReference = batch.BatchReference,
                TotalReceived = 4,
                TotalProcessed = 4,
                TotalSucceeded = 2,
                TotalFailed = 2,
                ItemResults =
                [
                    new BulkAchTransactionItemResult { Index = 0, Succeeded = true, TransactionId = 1001 },
                    new BulkAchTransactionItemResult { Index = 1, Succeeded = false, ErrorMessage = "Duplicada" },
                    new BulkAchTransactionItemResult { Index = 2, Succeeded = true, TransactionId = 1003 },
                    new BulkAchTransactionItemResult { Index = 3, Succeeded = false, ErrorMessage = "Tercero no autorizado" }
                ]
            });

        var service = CreateService(context, bulkTx);
        await service.ProcessBatchAsync(batch.Id, null, "job-partial");

        var persisted = await context.BulkIngestionBatches.FirstAsync(x => x.Id == batch.Id);
        Assert.Equal(BulkIngestionBatchStatusEnum.PartiallyProcessed, persisted.Status);
        Assert.Equal(3, persisted.TotalFailed); // 2 processing + 1 structural preexisting
    }

    [Fact]
    public async Task ProcessBatchAsync_SetsFailed_WhenAllItemsFail()
    {
        using var connection = CreateOpenConnection();
        await using var context = CreateContext(connection);

        var batch = SeedBatchWithReadyItems(context, 2, 0);

        var bulkTx = new Mock<IAchBulkTransactionService>();
        bulkTx.Setup(x => x.RegisterBulkAsync(It.IsAny<BulkAchTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BulkAchTransactionResponse
            {
                BatchReference = batch.BatchReference,
                TotalReceived = 2,
                TotalProcessed = 2,
                TotalSucceeded = 0,
                TotalFailed = 2,
                ItemResults =
                [
                    new BulkAchTransactionItemResult { Index = 0, Succeeded = false, ErrorMessage = "Duplicada" },
                    new BulkAchTransactionItemResult { Index = 1, Succeeded = false, ErrorMessage = "Regla funcional" }
                ]
            });

        var service = CreateService(context, bulkTx);
        await service.ProcessBatchAsync(batch.Id, null, "job-failed");

        var persisted = await context.BulkIngestionBatches.FirstAsync(x => x.Id == batch.Id);
        Assert.Equal(BulkIngestionBatchStatusEnum.Failed, persisted.Status);
        Assert.Equal(2, persisted.TotalFailed);
    }

    [Fact]
    public async Task ProcessBatchAsync_HandlesReasonableVolume()
    {
        using var connection = CreateOpenConnection();
        await using var context = CreateContext(connection);

        var batch = SeedBatchWithReadyItems(context, 120, 0);

        var bulkTx = new Mock<IAchBulkTransactionService>();
        bulkTx.Setup(x => x.RegisterBulkAsync(It.IsAny<BulkAchTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildResponse(120, 120, 0));

        var service = CreateService(context, bulkTx);
        await service.ProcessBatchAsync(batch.Id, null, "job-volume");

        var processed = await context.BulkIngestionItems.CountAsync(x => x.BatchId == batch.Id && x.Status == BulkIngestionItemStatusEnum.Processed);
        Assert.Equal(120, processed);
    }

    private static AchBulkBatchProcessingService CreateService(AchDbContext context, Mock<IAchBulkTransactionService> bulkTx)
    {
        return new AchBulkBatchProcessingService(
            context,
            bulkTx.Object,
            Mock.Of<ILogger<AchBulkBatchProcessingService>>());
    }

    private static BulkAchTransactionResponse BuildResponse(int total, int success, int failed)
    {
        var itemResults = new List<BulkAchTransactionItemResult>();
        for (var i = 0; i < total; i++)
        {
            itemResults.Add(new BulkAchTransactionItemResult
            {
                Index = i,
                Succeeded = i < success,
                TransactionId = i < success ? 2000 + i : null,
                ErrorMessage = i < success ? null : "Error simulado"
            });
        }

        return new BulkAchTransactionResponse
        {
            BatchReference = "BATCH-TEST",
            TotalReceived = total,
            TotalProcessed = total,
            TotalSucceeded = success,
            TotalFailed = failed,
            ItemResults = itemResults
        };
    }

    private static BulkIngestionBatch SeedBatchWithReadyItems(AchDbContext context, int readyItems, int invalidItems)
    {
        var batch = new BulkIngestionBatch
        {
            Id = Guid.NewGuid(),
            BatchReference = $"TEST-BATCH-{Guid.NewGuid():N}",
            FileType = BulkIngestionFileTypeEnum.Json,
            FileName = "test.json",
            ContentType = "application/json",
            FileHash = Guid.NewGuid().ToString("N"),
            UploadedBy = "test",
            UploadedAtUtc = DateTime.UtcNow,
            TotalRecords = readyItems + invalidItems,
            TotalValid = readyItems,
            TotalInvalid = invalidItems,
            Status = BulkIngestionBatchStatusEnum.Queued
        };

        context.BulkIngestionBatches.Add(batch);

        for (var i = 1; i <= readyItems; i++)
        {
            var payload = new BulkAchTransactionItemRequest
            {
                Amount = 1000 + i,
                Reference = $"REF-{i:000}",
                Type = TransactionTypeEnum.Credit,
                AccountType = AccountTypeEnum.Checking,
                DestinationInstitutionId = 2,
                SourceAccountNumber = $"123456{i:0000}",
                DestinationAccountNumber = $"987654{i:0000}",
                CompanyName = "EMPRESA",
                CompanyIdentification = "900123456",
                CompanyEntryDescriptionId = 1
            };

            context.BulkIngestionItems.Add(new BulkIngestionItem
            {
                BatchId = batch.Id,
                ItemIndex = i,
                Reference = payload.Reference,
                Status = BulkIngestionItemStatusEnum.Ready,
                RawPayloadJson = System.Text.Json.JsonSerializer.Serialize(payload),
                NormalizedPayloadJson = System.Text.Json.JsonSerializer.Serialize(payload)
            });
        }

        for (var i = 1; i <= invalidItems; i++)
        {
            context.BulkIngestionItems.Add(new BulkIngestionItem
            {
                BatchId = batch.Id,
                ItemIndex = readyItems + i,
                Reference = $"INV-{i:000}",
                Status = BulkIngestionItemStatusEnum.StructuralError,
                Message = "Error estructural",
                RawPayloadJson = "{}"
            });
        }

        context.SaveChanges();
        return batch;
    }

    private static BulkIngestionAttempt SeedAttempt(AchDbContext context, Guid batchId, int attemptNumber)
    {
        var attempt = new BulkIngestionAttempt
        {
            BatchId = batchId,
            AttemptNumber = attemptNumber,
            TriggerType = BulkIngestionTriggerTypeEnum.Initial,
            Scope = BulkIngestionRetryScopeEnum.Full,
            TriggeredBy = "test",
            Status = BulkIngestionAttemptStatusEnum.Queued
        };

        context.BulkIngestionAttempts.Add(attempt);
        context.SaveChanges();
        return attempt;
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
}
