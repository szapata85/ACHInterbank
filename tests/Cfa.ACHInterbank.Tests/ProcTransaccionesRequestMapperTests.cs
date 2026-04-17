using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class ProcTransaccionesRequestMapperTests
{
    [Fact]
    public async Task ResolveAsync_FailFast_WhenPublishedMappingDoesNotExist()
    {
        await using var context = BuildContext();
        context.IntegrationMethods.Add(new IntegrationMethod
        {
            Id = 10,
            Code = "WSCFAACH.Proc_Transacciones",
            DisplayName = "Proc_Transacciones",
            SoapClientCode = "WSCFAACH",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var sut = new ProcTransaccionesRequestMapper(context);
        var queue = BuildQueue();
        var ingestion = new IncomingNachaFileIngestion { Id = queue.IncomingNachaFileIngestionId, FileName = "in.ach", FileHashSha256 = "h", ContentType = "txt", CorrelationId = "c", UploadedBy = "u", Notes = "n" };
        var classification = new IncomingNachaEntryClassification { Id = queue.IncomingNachaEntryClassificationId };
        var transaction = new AchTransaction { Id = queue.AchTransactionId, Amount = 100m, TransactionCode = "22", TraceNumber = "1", TransactionExternalId = "ext", AchCycleId = "C1", Reference = "r", CompanyName = "c", CompanyIdentification = "i", SourceAccountNumber = "s", DestinationAccountNumber = "d", OriginatingDFI = "o", ReceivingDFI = "r", SourceInstitutionId = 1, DestinationInstitutionId = 1, AchBatchId = 1, Type = Domain.Entities.Transactions.Enums.TransactionTypeEnum.Credit, EffectiveEntryDate = DateTime.Today };
        var cycle = new AchCycle { Id = "C1", ProcessingDate = DateTime.Today, StartTime = TimeSpan.Zero, EndTime = new TimeSpan(23, 59, 0), ClearingHouseId = 1, CycleName = "c1" };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ResolveAsync(queue, ingestion, classification, transaction, cycle, DateTime.Now));
    }

    private static IncomingNachaDispatchQueue BuildQueue()
    {
        return new IncomingNachaDispatchQueue
        {
            Id = Guid.NewGuid(),
            IncomingNachaFileIngestionId = Guid.NewGuid(),
            IncomingNachaEntryClassificationId = Guid.NewGuid(),
            IncomingNachaTransactionLinkId = Guid.NewGuid(),
            AchTransactionId = 100,
            AchCycleId = "C1",
            ClearingHouseId = 1,
            OperationalDate = DateTime.Today
        };
    }

    private static AchDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AchDbContext(options);
    }
}
