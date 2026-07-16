using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class UatContrapartidasControllerTests
{
    [Fact]
    public async Task DispatchCycle_ReturnsNotFound_WhenUatDispatchIsDisabled()
    {
        var dispatchJob = new Mock<IContrapartidaDispatchJobService>(MockBehavior.Strict);
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns("Production");
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

        var sut = new UatContrapartidasController(dispatchJob.Object, environment.Object, configuration);

        var result = await sut.DispatchCycle(new UatContrapartidasDispatchCycleRequest
        {
            CycleId = "CYCLE-01",
            ClearingHouseId = 1,
            TriggeredBy = "g34-playwright",
            ChunkSize = 50
        }, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        dispatchJob.Verify(x => x.ProcessCycleAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchCycle_CallsProcessCycleAsync_WhenFeatureFlagIsEnabled()
    {
        var dispatchJob = new Mock<IContrapartidaDispatchJobService>(MockBehavior.Strict);
        dispatchJob
            .Setup(x => x.ProcessCycleAsync("CYCLE-01", 1, "g34-playwright", 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContrapartidaCycleDispatchResult("CYCLE-01", 1, 2, 1, 1, 0, 1, "summary"));

        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns("Production");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RUN_UAT_TRANSACTION_NACHA_DISPATCH"] = "true"
            })
            .Build();

        var sut = new UatContrapartidasController(dispatchJob.Object, environment.Object, configuration);

        var result = await sut.DispatchCycle(new UatContrapartidasDispatchCycleRequest
        {
            CycleId = "CYCLE-01",
            ClearingHouseId = 1,
            TriggeredBy = "g34-playwright",
            ChunkSize = 50
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<ContrapartidaCycleDispatchResult>(ok.Value);
        Assert.Equal("summary", payload.Summary);
        dispatchJob.VerifyAll();
    }

    [Fact]
    public async Task DispatchCycle_WithTransactionId_CallsOnlyTargetedDispatch()
    {
        var dispatchJob = new Mock<IContrapartidaDispatchJobService>(MockBehavior.Strict);
        dispatchJob
            .Setup(x => x.ProcessTransactionAsync("CYCLE-01", 1, 321, "playwright-local-proc-contrapartidas", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContrapartidaCycleDispatchResult("CYCLE-01", 1, 1, 1, 0, 0, 1, "targeted"));

        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns("Development");
        var configuration = new ConfigurationBuilder().Build();
        var sut = new UatContrapartidasController(dispatchJob.Object, environment.Object, configuration);

        var result = await sut.DispatchCycle(new UatContrapartidasDispatchCycleRequest
        {
            CycleId = "CYCLE-01",
            ClearingHouseId = 1,
            TransactionId = 321,
            TriggeredBy = "playwright-local-proc-contrapartidas"
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<ContrapartidaCycleDispatchResult>(ok.Value);
        Assert.Equal(1, payload.Processed);
        dispatchJob.VerifyAll();
        dispatchJob.Verify(x => x.ProcessCycleAsync(
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
