using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class IncomingNachaDispatchRelationalValidationTests
{
    [Fact]
    public async Task IncomingNachaDispatchQueue_EnforcesUniqueIdempotencyKey_OnSqliteRelationalEngine()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, HolidayStrategy = "Colombian" });
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 1,
            Name = "ACH Colombia",
            Code = "ACH",
            OriginCode = "12345678",
            ClearingHouseId = 1
        });
        var companyEntryDescriptionId = await context.CompanyEntryDescriptionCatalogs
            .Where(x => x.Term == "PAGOS")
            .Select(x => x.Id)
            .FirstOrDefaultAsync();
        if (companyEntryDescriptionId == 0)
        {
            companyEntryDescriptionId = 999;
            context.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog
            {
                Id = companyEntryDescriptionId,
                Term = "PAGOS",
                Description = "Pagos",
                StandardEntryClassCode = "PPD",
                IsActive = true
            });
        }
        var fi = new FinancialInstitution
        {
            Id = 1,
            Name = "Banco Test",
            RoutingNumber = "12345",
            TransitCode = "678",
            IsDefaultSource = true,
            Status = FinancialInstitutionStatus.Active
        };
        fi.CalculateCheckDigit();
        context.FinancialInstitutions.Add(fi);

        var ingestion = new IncomingNachaFileIngestion
        {
            Id = Guid.NewGuid(),
            FileName = "in.ach",
            FileHashSha256 = "h",
            FileSize = 1,
            ContentType = "text/plain",
            UploadedBy = "tester",
            CorrelationId = "c",
            Notes = "n"
        };
        var cycle = new AchCycle
        {
            Id = "C1",
            CycleName = "c1",
            ClearingHouseId = 1,
            ProcessingDate = DateTime.Today,
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = new TimeSpan(23, 0, 0)
        };
        var batch = new AchBatch { Id = 1, AchCycleId = cycle.Id, CompanyEntryDescriptionId = companyEntryDescriptionId, EffectiveEntryDate = DateTime.Today };
        var tx = new AchTransaction
        {
            Id = 100,
            Amount = 10m,
            TransactionExternalId = "EXT-1",
            Reference = "R",
            Type = TransactionTypeEnum.Credit,
            TransactionCode = "22",
            SourceAccountNumber = "S",
            DestinationAccountNumber = "D",
            SourceInstitutionId = 1,
            DestinationInstitutionId = 1,
            OriginatingDFI = "11111111",
            ReceivingDFI = "222222220",
            TraceNumber = "123456789012345",
            CompanyName = "C",
            CompanyIdentification = "I",
            AchCycleId = cycle.Id,
            AchBatchId = batch.Id,
            EffectiveEntryDate = DateTime.Today
        };
        context.EntryDetails.Add(new EntryDetail
        {
            EntryDetailID = 1,
            TransactionCode = "22",
            ReceivingParticipantEntityCode = "22222222",
            AccountNumber = "D",
            Amount = 10m,
            RecipUserName = "Receiver"
        });
        var classification = new IncomingNachaEntryClassification { Id = Guid.NewGuid(), IncomingNachaFileIngestionId = ingestion.Id, EntryDetailId = 1 };
        var link = new IncomingNachaTransactionLink { Id = Guid.NewGuid(), IncomingNachaFileIngestionId = ingestion.Id, EntryDetailId = 1, AchTransactionId = tx.Id, IsFinal = true, LinkType = IncomingNachaLinkType.ExactTrace15 };

        context.AddRange(cycle, batch, tx, ingestion, classification, link);
        await context.SaveChangesAsync();

        var key = "IDEMPOTENCY-KEY-001";
        context.IncomingNachaDispatchQueue.Add(new IncomingNachaDispatchQueue
        {
            Id = Guid.NewGuid(),
            IncomingNachaFileIngestionId = ingestion.Id,
            IncomingNachaEntryClassificationId = classification.Id,
            IncomingNachaTransactionLinkId = link.Id,
            AchTransactionId = tx.Id,
            AchCycleId = cycle.Id,
            ClearingHouseId = cycle.ClearingHouseId,
            OperationalDate = DateTime.Today,
            IdempotencyDispatchKey = key
        });
        await context.SaveChangesAsync();

        context.IncomingNachaDispatchQueue.Add(new IncomingNachaDispatchQueue
        {
            Id = Guid.NewGuid(),
            IncomingNachaFileIngestionId = ingestion.Id,
            IncomingNachaEntryClassificationId = classification.Id,
            IncomingNachaTransactionLinkId = link.Id,
            AchTransactionId = tx.Id,
            AchCycleId = cycle.Id,
            ClearingHouseId = cycle.ClearingHouseId,
            OperationalDate = DateTime.Today,
            IdempotencyDispatchKey = key
        });

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }
}
