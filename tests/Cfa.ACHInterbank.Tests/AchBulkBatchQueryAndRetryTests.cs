using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class AchBulkBatchQueryAndRetryTests
{
    [Fact]
    public async Task GetBatchSummaryAsync_ReturnsProgressAndAttempts()
    {
        using var connection = CreateOpenConnection();
        await using var context = CreateContext(connection);

        var batch = SeedBatch(context, BulkIngestionBatchStatusEnum.Processing);
        context.BulkIngestionAttempts.Add(new BulkIngestionAttempt
        {
            BatchId = batch.Id,
            AttemptNumber = 1,
            TriggerType = BulkIngestionTriggerTypeEnum.Initial,
            Scope = BulkIngestionRetryScopeEnum.Full,
            TriggeredBy = "ops",
            Status = BulkIngestionAttemptStatusEnum.Processing,
            ResultMessage = "Ejecutando"
        });
        await context.SaveChangesAsync();

        var service = new AchBulkBatchQueryService(context);
        var summary = await service.GetBatchSummaryAsync(batch.Id);

        Assert.NotNull(summary);
        Assert.Equal(batch.Id, summary!.BatchId);
        Assert.True(summary.Status.ProgressPercent > 0m);
        Assert.Single(summary.Attempts);
    }

    [Fact]
    public async Task RetryAsync_EnqueuesJob_AndResetsEligibleItems()
    {
        using var connection = CreateOpenConnection();
        await using var context = CreateContext(connection);

        var batch = SeedBatch(context, BulkIngestionBatchStatusEnum.Failed, retryCount: 1);

        context.BulkIngestionItems.AddRange(
            new BulkIngestionItem { BatchId = batch.Id, ItemIndex = 1, Reference = "A", Status = BulkIngestionItemStatusEnum.ProcessingError, Message = "Duplicado", RawPayloadJson = "{}" },
            new BulkIngestionItem { BatchId = batch.Id, ItemIndex = 2, Reference = "B", Status = BulkIngestionItemStatusEnum.StructuralError, Message = "Estructural", RawPayloadJson = "{}" }
        );
        await context.SaveChangesAsync();

        var scheduler = new Mock<IAchBulkJobScheduler>();
        scheduler.Setup(x => x.EnqueueBatchAsync(batch.Id, It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("job-retry-1");

        var service = new AchBulkBatchRetryService(context, scheduler.Object);
        var response = await service.RetryAsync(batch.Id, new RetryBatchRequest { Scope = BulkIngestionRetryScopeEnum.FailedOnly }, "ops.user");

        Assert.Equal(batch.Id, response.BatchId);
        Assert.Equal(BulkIngestionBatchStatusEnum.Retrying, response.Status);

        var refreshedBatch = await context.BulkIngestionBatches.FirstAsync(x => x.Id == batch.Id);
        Assert.Equal(BulkIngestionBatchStatusEnum.Retrying, refreshedBatch.Status);
        Assert.Equal("job-retry-1", refreshedBatch.LastJobId);

        var failedItem = await context.BulkIngestionItems.FirstAsync(x => x.BatchId == batch.Id && x.ItemIndex == 1);
        Assert.Equal(BulkIngestionItemStatusEnum.Ready, failedItem.Status);

        var structural = await context.BulkIngestionItems.FirstAsync(x => x.BatchId == batch.Id && x.ItemIndex == 2);
        Assert.Equal(BulkIngestionItemStatusEnum.StructuralError, structural.Status);
    }

    private static BulkIngestionBatch SeedBatch(AchDbContext context, BulkIngestionBatchStatusEnum status, int retryCount = 0)
    {
        var batch = new BulkIngestionBatch
        {
            Id = Guid.NewGuid(),
            BatchReference = $"SEED-{Guid.NewGuid():N}",
            FileType = BulkIngestionFileTypeEnum.Json,
            FileName = "seed.json",
            ContentType = "application/json",
            FileHash = Guid.NewGuid().ToString("N"),
            UploadedBy = "seed",
            UploadedAtUtc = DateTime.UtcNow.AddMinutes(-15),
            ProcessingStartedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            TotalRecords = 10,
            TotalValid = 10,
            TotalInvalid = 0,
            TotalProcessed = status == BulkIngestionBatchStatusEnum.Processing ? 5 : 10,
            TotalSucceeded = status == BulkIngestionBatchStatusEnum.Processing ? 3 : 0,
            TotalFailed = status == BulkIngestionBatchStatusEnum.Processing ? 2 : 10,
            Status = status,
            RetryCount = retryCount,
            SummaryErrorsJson = "[]"
        };

        context.BulkIngestionBatches.Add(batch);
        context.SaveChanges();
        return batch;
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
