using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class BulkIngestionLifecycleServiceTests
{
    [Fact]
    public async Task RequestCancellationAsync_CancelsActiveBatch()
    {
        using var connection = CreateOpenConnection();
        await using var context = CreateContext(connection);

        var batch = new BulkIngestionBatch
        {
            Id = Guid.NewGuid(),
            BatchReference = "B-1",
            FileType = BulkIngestionFileTypeEnum.Json,
            FileName = "a.json",
            ContentType = "application/json",
            FileHash = "hash",
            UploadedBy = "u",
            UploadedAtUtc = DateTime.UtcNow,
            Status = BulkIngestionBatchStatusEnum.Processing
        };

        context.BulkIngestionBatches.Add(batch);
        await context.SaveChangesAsync();

        var service = new BulkIngestionLifecycleService(context);
        var cancelled = await service.RequestCancellationAsync(batch.Id, "ops.user");

        Assert.True(cancelled);
        var persisted = await context.BulkIngestionBatches.FirstAsync(x => x.Id == batch.Id);
        Assert.Equal(BulkIngestionBatchStatusEnum.Cancelled, persisted.Status);
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
