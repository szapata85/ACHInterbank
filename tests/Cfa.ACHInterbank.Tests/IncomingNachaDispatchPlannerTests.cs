using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class IncomingNachaDispatchPlannerTests
{
    [Fact]
    public async Task PlanForIngestionAsync_EnqueuesOnlyEligibleTransactions()
    {
        await using var context = BuildContext();
        var ingestion = SeedCommonGraph(context);

        var eligibility = new IncomingNachaDispatchEligibilityPolicy();
        var sut = new IncomingNachaDispatchPlanner(context, eligibility);

        var created = await sut.PlanForIngestionAsync(ingestion.Id, "tester");

        Assert.Equal(2, created);
        var queued = await context.IncomingNachaDispatchQueue.CountAsync(x => x.QueueStatus == IncomingNachaDispatchQueueStatus.Queued);
        var blocked = await context.IncomingNachaDispatchQueue.CountAsync(x => x.QueueStatus == IncomingNachaDispatchQueueStatus.Blocked);
        Assert.Equal(1, queued);
        Assert.Equal(1, blocked);
    }

    private static IncomingNachaFileIngestion SeedCommonGraph(AchDbContext context)
    {
        var cycle = new AchCycle
        {
            Id = "C1",
            CycleName = "ciclo 1",
            ClearingHouseId = 1,
            ProcessingDate = DateTime.Today,
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = new TimeSpan(23, 0, 0)
        };
        context.AchCycles.Add(cycle);
        context.AchBatches.Add(new AchBatch { Id = 1, AchCycleId = "C1", CompanyEntryDescriptionId = 1, EffectiveEntryDate = DateTime.Today });

        var tx1 = new AchTransaction
        {
            Id = 100,
            Amount = 100m,
            TransactionExternalId = "EXT-100",
            Reference = "R100",
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
            AchCycleId = "C1",
            AchBatchId = 1,
            EffectiveEntryDate = DateTime.Today
        };
        var tx2 = new AchTransaction
        {
            Id = 101,
            Amount = 101m,
            TransactionExternalId = "EXT-101",
            Reference = "R101",
            Type = TransactionTypeEnum.Credit,
            TransactionCode = "22",
            SourceAccountNumber = "S",
            DestinationAccountNumber = "D",
            SourceInstitutionId = 1,
            DestinationInstitutionId = 1,
            OriginatingDFI = "11111111",
            ReceivingDFI = "222222220",
            TraceNumber = "123456789012346",
            CompanyName = "C",
            CompanyIdentification = "I",
            AchCycleId = "C1",
            AchBatchId = 1,
            EffectiveEntryDate = DateTime.Today
        };
        context.AchTransactions.AddRange(tx1, tx2);

        var ingestion = new IncomingNachaFileIngestion
        {
            Id = Guid.NewGuid(),
            FileName = "in.ach",
            FileHashSha256 = "h",
            FileSize = 1,
            ContentType = "text/plain",
            UploadedBy = "tester",
            CorrelationId = "c",
            Notes = "n",
            IngestionStatus = IncomingNachaIngestionStatus.Completado,
            CycleResolutionStatus = IncomingNachaCycleResolutionStatus.ResueltoConfirmado,
            ResolvedAchCycleId = "C1",
            ResolvedClearingHouseId = 1,
            OperationalDate = DateTime.Today
        };
        context.IncomingNachaFileIngestions.Add(ingestion);

        var classEligible = new IncomingNachaEntryClassification
        {
            Id = Guid.NewGuid(),
            IncomingNachaFileIngestionId = ingestion.Id,
            EntryDetailId = 1,
            FunctionalClass = IncomingNachaFunctionalClass.CreditoEntrante,
            EligibilityStatus = IncomingNachaEligibilityStatus.Elegible
        };
        var classBlocked = new IncomingNachaEntryClassification
        {
            Id = Guid.NewGuid(),
            IncomingNachaFileIngestionId = ingestion.Id,
            EntryDetailId = 2,
            FunctionalClass = IncomingNachaFunctionalClass.Prenotificacion,
            EligibilityStatus = IncomingNachaEligibilityStatus.Elegible
        };
        context.IncomingNachaEntryClassifications.AddRange(classEligible, classBlocked);

        context.IncomingNachaTransactionLinks.AddRange(
            new IncomingNachaTransactionLink
            {
                Id = Guid.NewGuid(),
                IncomingNachaFileIngestionId = ingestion.Id,
                EntryDetailId = 1,
                AchTransactionId = tx1.Id,
                LinkType = IncomingNachaLinkType.ExactTrace15,
                IsFinal = true
            },
            new IncomingNachaTransactionLink
            {
                Id = Guid.NewGuid(),
                IncomingNachaFileIngestionId = ingestion.Id,
                EntryDetailId = 2,
                AchTransactionId = tx2.Id,
                LinkType = IncomingNachaLinkType.ExactTrace15,
                IsFinal = true
            });

        context.SaveChanges();
        return ingestion;
    }

    private static AchDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AchDbContext(options);
    }
}
