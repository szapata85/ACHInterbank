using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class ProcContrapartidasFallbackClosureTests
{
    [Fact]
    public async Task ProcContrapartidasMapper_ShouldFailControlled_WhenNoPublishedMappingExists()
    {
        var functionalResolver = new Mock<IProcContrapartidasFunctionalMappingResolver>();
        functionalResolver
            .Setup(x => x.TryResolveAsync(
                It.IsAny<AchCycle>(),
                It.IsAny<IReadOnlyCollection<AchTransaction>>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProcContrapartidasRequestResolution?)null);
        var sut = new ProcContrapartidasRequestMapper(functionalResolver.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ResolveAsync(BuildCycle(), [BuildDebitTransaction()], DateTime.UtcNow));

        Assert.Contains("INTEGRATION_MAPPING_REQUIRED", ex.Message);
        Assert.Contains("No se permite fallback transicional", ex.Message);
    }

    [Fact]
    public async Task MissingMapping_ShouldNotGenerateEnvelope()
    {
        var functionalResolver = new Mock<IProcContrapartidasFunctionalMappingResolver>();
        functionalResolver
            .Setup(x => x.TryResolveAsync(
                It.IsAny<AchCycle>(),
                It.IsAny<IReadOnlyCollection<AchTransaction>>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProcContrapartidasRequestResolution?)null);
        var sut = new ProcContrapartidasRequestMapper(functionalResolver.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ResolveAsync(BuildCycle(), [BuildDebitTransaction()], DateTime.UtcNow));
    }

    private static AchCycle BuildCycle()
        => new()
        {
            Id = "CYCLE-FALLBACK",
            ClearingHouseId = 1,
            ProcessingDate = DateTime.UtcNow.Date,
            ClearingHouse = new ClearingHouse { Id = 1, Code = "ACH", Name = "ACH Colombia", OriginCode = "0001283", ClearingHouseId = 1 }
        };

    private static AchTransaction BuildDebitTransaction()
        => new()
        {
            Id = 10,
            Type = TransactionTypeEnum.Debit,
            Amount = 1000m,
            TransactionExternalId = "UAT-DEB-010",
            Reference = "UAT-DEB-010",
            CompanyIdentification = "900000001",
            SourceAccountNumber = "0000001001",
            EffectiveEntryDate = DateTime.UtcNow.Date,
            AchBatchId = 1
        };
}
