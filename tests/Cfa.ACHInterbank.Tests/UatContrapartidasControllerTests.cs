using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using System.Security.Claims;
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
            TransactionId = 321
        }, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        dispatchJob.Verify(x => x.ProcessCycleAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchCycle_RemainsBlockedInProduction_WhenFeatureFlagIsEnabled()
    {
        var dispatchJob = new Mock<IContrapartidaDispatchJobService>(MockBehavior.Strict);
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns("Production");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ACH_SOAP_LIVE_TESTS"] = "true"
            })
            .Build();

        var sut = new UatContrapartidasController(dispatchJob.Object, environment.Object, configuration);

        var result = await sut.DispatchCycle(new UatContrapartidasDispatchCycleRequest
        {
            TransactionId = 321
        }, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        dispatchJob.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DispatchCycle_WithTransactionId_CallsOnlyTargetedDispatch()
    {
        var dispatchJob = new Mock<IContrapartidaDispatchJobService>(MockBehavior.Strict);
        dispatchJob
            .Setup(x => x.ProcessTransactionAsync(321, "playwright-local-proc-contrapartidas", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContrapartidaCycleDispatchResult("CYCLE-01", 1, 1, 1, 0, 0, 1, "targeted"));

        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns("Development");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ACH_SOAP_LIVE_TESTS"] = "true"
            })
            .Build();
        var sut = new UatContrapartidasController(dispatchJob.Object, environment.Object, configuration);
        sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Name, "playwright-local-proc-contrapartidas")],
                    authenticationType: "Test"))
            }
        };

        var result = await sut.DispatchCycle(new UatContrapartidasDispatchCycleRequest
        {
            TransactionId = 321
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<ContrapartidaCycleDispatchResult>(ok.Value);
        Assert.Equal(1, payload.Processed);
        dispatchJob.VerifyAll();
        dispatchJob.Verify(x => x.ProcessCycleAsync(
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
