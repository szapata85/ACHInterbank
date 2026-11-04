using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class IncomingNachaFileBasedDispatchSchemaF33Tests
{
    [Fact]
    public void Model_TransactionLinkIsNullable_AndIdempotencyIndexRemainsUnique()
    {
        using var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite("DataSource=:memory:").Options);
        var entity = context.Model.FindEntityType(typeof(IncomingNachaDispatchQueue))!;
        Assert.True(entity.FindProperty(nameof(IncomingNachaDispatchQueue.IncomingNachaTransactionLinkId))!.IsNullable);
        Assert.Contains(entity.GetIndexes(), x => x.IsUnique &&
            x.Properties.Count == 1 && x.Properties[0].Name == nameof(IncomingNachaDispatchQueue.IdempotencyDispatchKey));
    }

    [Fact]
    public async Task Sqlite_FileBasedWithoutLegacyLink_IsValid_AndCycleBoundWithoutLinkIsInvalid()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        var ing = new IncomingNachaFileIngestion
        {
            FileName = "0001283.008.20260731.10.OUT", FileHashSha256 = "F33HASH", FileSize = 1,
            ContentType = "text/plain", UploadedBy = "tester", CorrelationId = "f33", Notes = "fixture"
        };
        var entry = new EntryDetail { TransactionCode = "22", ReceivingParticipantEntityCode = "22222222", Amount = 100m, AccountNumber = "123" };
        context.AddRange(ing, entry);
        await context.SaveChangesAsync();
        var cls = new IncomingNachaEntryClassification { IncomingNachaFileIngestionId = ing.Id, EntryDetailId = entry.EntryDetailID };
        context.Add(cls);
        await context.SaveChangesAsync();
        var row = new IncomingNachaDispatchQueue
        {
            IncomingNachaFileIngestionId = ing.Id, IncomingNachaEntryClassificationId = cls.Id,
            SourceKind = IncomingNachaDispatchSourceKinds.AchColFileBased,
            EntryDetailId = entry.EntryDetailID, IncomingNachaTransactionLinkId = null,
            AchCycleId = null, AchTransactionId = null,
            IdempotencyDispatchKey = "F33_VALID", QueueStatus = IncomingNachaDispatchQueueStatus.Blocked,
            ClearingHouseId = 1, OperationalDate = new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc)
        };
        context.Add(row);
        await context.SaveChangesAsync();
        Assert.Single(context.IncomingNachaDispatchQueue);
        context.Add(new IncomingNachaDispatchQueue
        {
            IncomingNachaFileIngestionId = ing.Id, IncomingNachaEntryClassificationId = cls.Id,
            SourceKind = IncomingNachaDispatchSourceKinds.CycleBound,
            EntryDetailId = null, IncomingNachaTransactionLinkId = null,
            AchCycleId = null, AchTransactionId = null,
            IdempotencyDispatchKey = "F33_INVALID", ClearingHouseId = 1, OperationalDate = row.OperationalDate
        });
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
