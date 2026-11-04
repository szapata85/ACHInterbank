using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

/// <summary>
/// F3.1 schema and relational-contract tests. Does not authorize CFA transmission.
/// </summary>
public class IncomingNachaDispatchFileBasedSchemaF31Tests
{
    [Fact]
    public void EfModel_HasNullableLegacyReferences_AndIndependentNachaEntry()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        using var context = new AchDbContext(options);
        var entity = context.Model.FindEntityType(typeof(IncomingNachaDispatchQueue));
        Assert.NotNull(entity);
        Assert.True(entity!.FindProperty(nameof(IncomingNachaDispatchQueue.AchTransactionId))!.IsNullable);
        Assert.True(entity.FindProperty(nameof(IncomingNachaDispatchQueue.AchCycleId))!.IsNullable);
        Assert.True(entity.FindProperty(nameof(IncomingNachaDispatchQueue.EntryDetailId))!.IsNullable);
        Assert.Equal(IncomingNachaDispatchSourceKinds.CycleBound,
            entity.FindProperty(nameof(IncomingNachaDispatchQueue.SourceKind))!.GetDefaultValue());
        Assert.Contains(entity.GetIndexes(), x => x.IsUnique &&
            x.Properties.Count == 1 && x.Properties[0].Name == nameof(IncomingNachaDispatchQueue.IdempotencyDispatchKey));
        Assert.Contains(entity.GetForeignKeys(), x =>
            x.Properties.Count == 1 && x.Properties[0].Name == nameof(IncomingNachaDispatchQueue.EntryDetailId)
            && x.PrincipalEntityType.ClrType == typeof(EntryDetail));
        Assert.Contains(entity.GetCheckConstraints(), x => x.Name == "CK_IncomingNachaDispatchQueue_SourceShape");
    }

    [Fact]
    public async Task Sqlite_EntryScopedSource_PersistsWithNoAchTransactionOrCycle()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;
        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var file = new IncomingNachaFileIngestion
        {
            FileName = "0001283.008.20260731.10.OUT.env",
            FileHashSha256 = "sha256-fixture-h",
            FileSize = 42,
            ContentType = "application/octet-stream",
            UploadedBy = "unit-test",
            CorrelationId = "corr-file-h",
            Notes = "schema only",
            FileIdModifier = "H",
            FileSequence = 8,
            ExternalCycleNumber = 10
        };
        var entry = new EntryDetail
        {
            TransactionCode = "22",
            ReceivingParticipantEntityCode = "22222222",
            AccountNumber = "1234",
            Amount = 10m,
            RecipUserName = "DESTINATION"
        };
        context.AddRange(file, entry);
        await context.SaveChangesAsync();

        var cls = new IncomingNachaEntryClassification
        {
            IncomingNachaFileIngestionId = file.Id,
            EntryDetailId = entry.EntryDetailID
        };
        var link = new IncomingNachaTransactionLink
        {
            IncomingNachaFileIngestionId = file.Id,
            EntryDetailId = entry.EntryDetailID,
            IsFinal = true
        };
        context.AddRange(cls, link);
        await context.SaveChangesAsync();

        var item = new IncomingNachaDispatchQueue
        {
            SourceKind = IncomingNachaDispatchSourceKinds.AchColFileBased,
            EntryDetailId = entry.EntryDetailID,
            IncomingNachaFileIngestionId = file.Id,
            IncomingNachaEntryClassificationId = cls.Id,
            IncomingNachaTransactionLinkId = link.Id,
            AchTransactionId = null,
            AchCycleId = null,
            ClearingHouseId = 1,
            OperationalDate = new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc),
            IdempotencyDispatchKey = "ACHCOL|Proc_Transacciones|v1|fixture-h-10",
            QueueStatus = IncomingNachaDispatchQueueStatus.Blocked // schema-only; not eligible for execution
        };
        context.IncomingNachaDispatchQueue.Add(item);
        await context.SaveChangesAsync();

        var saved = await context.IncomingNachaDispatchQueue.AsNoTracking().SingleAsync();
        Assert.Equal(IncomingNachaDispatchSourceKinds.AchColFileBased, saved.SourceKind);
        Assert.Equal(entry.EntryDetailID, saved.EntryDetailId);
        Assert.Null(saved.AchCycleId);
        Assert.Null(saved.AchTransactionId);

        // Business-source identity remains protected even when no legacy ACH transaction exists.
        context.IncomingNachaDispatchQueue.Add(new IncomingNachaDispatchQueue
        {
            SourceKind = IncomingNachaDispatchSourceKinds.AchColFileBased,
            EntryDetailId = entry.EntryDetailID,
            IncomingNachaFileIngestionId = file.Id,
            IncomingNachaEntryClassificationId = cls.Id,
            IncomingNachaTransactionLinkId = link.Id,
            ClearingHouseId = 1,
            OperationalDate = item.OperationalDate,
            IdempotencyDispatchKey = item.IdempotencyDispatchKey,
            QueueStatus = IncomingNachaDispatchQueueStatus.Blocked
        });
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Sqlite_RejectsEntryScopedQueueWithoutRequiredEntryId()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;
        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var file = new IncomingNachaFileIngestion
        {
            FileName = "h.OUT", FileHashSha256 = "fixture", FileSize = 1,
            ContentType = "text/plain", UploadedBy = "tester", CorrelationId = "c"
        };
        var entry = new EntryDetail
        {
            TransactionCode = "22", ReceivingParticipantEntityCode = "22222222",
            AccountNumber = "1234", Amount = 10m, RecipUserName = "DESTINATION"
        };
        context.AddRange(file, entry);
        await context.SaveChangesAsync();
        var cls = new IncomingNachaEntryClassification { IncomingNachaFileIngestionId = file.Id, EntryDetailId = entry.EntryDetailID };
        var link = new IncomingNachaTransactionLink { IncomingNachaFileIngestionId = file.Id, EntryDetailId = entry.EntryDetailID };
        context.AddRange(cls, link);
        await context.SaveChangesAsync();
        context.IncomingNachaDispatchQueue.Add(new IncomingNachaDispatchQueue
        {
            SourceKind = IncomingNachaDispatchSourceKinds.AchColFileBased,
            // missing EntryDetailId is rejected by the origin discriminator's shape constraint
            IncomingNachaFileIngestionId = file.Id,
            IncomingNachaEntryClassificationId = cls.Id,
            IncomingNachaTransactionLinkId = link.Id,
            ClearingHouseId = 1,
            OperationalDate = new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc),
            IdempotencyDispatchKey = "invalid-missing-entry",
            QueueStatus = IncomingNachaDispatchQueueStatus.Blocked
        });
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
