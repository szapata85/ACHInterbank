using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class AchReturnsServiceEligibilityIntegrationTests
{
    [Fact]
    public async Task AchReturnsService_ShouldUseEligibilityService_BeforeGeneratingReturn()
    {
        await using var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.ClearingHouses.Add(new ClearingHouse { Id = 1, Name = "CH", Code = "CENIT", OriginCode = "000101006" });
        context.AchCycles.Add(new AchCycle { Id = "C1", CycleName = "C1", ProcessingDate = DateTime.UtcNow.Date, CutoffTime = new TimeSpan(8, 0, 0), ClearingHouseId = 1 });
        context.AchTransactions.Add(new AchTransaction { Id = 1, AchCycleId = "C1", Type = TransactionTypeEnum.Credit, State = AchTransferStateEnum.Pending, EffectiveEntryDate = DateTime.UtcNow.Date, TransactionCode = "22", TraceNumber = "123456789012345", ReceivingDFI = "12345678", OriginatingDFI = "12345678", Amount = 100m, Reference = "R", SourceAccountNumber = "1", DestinationAccountNumber = "2" });
        await context.SaveChangesAsync();

        var catalog = new Mock<IAchRegulatoryCatalogService>();
        var eligibility = new Mock<IAchReturnEligibilityService>();
        eligibility.Setup(x => x.EvaluateOutgoingReturnAsync(It.IsAny<AchReturnEligibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchReturnEligibilityResult(true, "R01", 1, "Credit", "Pending", []));

        var sut = new AchReturnsService(context, regulatoryCatalogService: catalog.Object, returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create());
        await sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("C1", [new ReturnSelectionItemDto(1, "R01")]), CancellationToken.None);

        eligibility.Verify(x => x.EvaluateOutgoingReturnAsync(It.IsAny<AchReturnEligibilityRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task GenerateReturnsFileAsync_WhenEligibilityRejects_ThrowsFirstFailureMessage()
    {
        await using var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        context.ClearingHouses.Add(new ClearingHouse { Id = 7, Name = "CH", Code = "CENIT", OriginCode = "000101006" });
        context.AchCycles.Add(new AchCycle { Id = "C1", CycleName = "C1", ProcessingDate = DateTime.UtcNow.Date, CutoffTime = new TimeSpan(8, 0, 0), ClearingHouseId = 7 });
        context.AchTransactions.Add(new AchTransaction { Id = 1, AchCycleId = "C1", Type = TransactionTypeEnum.Credit, State = AchTransferStateEnum.Pending, EffectiveEntryDate = DateTime.UtcNow.Date, TransactionCode = "22", TraceNumber = "123456789012345", ReceivingDFI = "12345678", OriginatingDFI = "12345678", Amount = 100m, Reference = "R", SourceAccountNumber = "1", DestinationAccountNumber = "2" });
        await context.SaveChangesAsync();

        var catalog = new Mock<IAchRegulatoryCatalogService>();
        var eligibility = new Mock<IAchReturnEligibilityService>();
        eligibility.Setup(x => x.EvaluateOutgoingReturnAsync(It.IsAny<AchReturnEligibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchReturnEligibilityResult(false, "R01", 7, "Credit", "Pending", [new AchReturnEligibilityFailure("RETURN_POLICY_REJECTED", "Política no permite retorno.")]));

        var sut = new AchReturnsService(context, regulatoryCatalogService: catalog.Object, returnEligibilityService: eligibility.Object, returnGenerationLockService: new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GenerateReturnsFileAsync(new GenerateReturnsFileRequest("C1", [new ReturnSelectionItemDto(1, "R01")]), CancellationToken.None));
        Assert.Equal("Política no permite retorno.", ex.Message);
    }

}
