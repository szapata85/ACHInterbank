using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaOperationalReadModelTests
{
    [Fact]
    public async Task GetDashboard_ShouldReturnNoGoStatus()
    {
        var dashboard = await Service().GetDashboardAsync();

        dashboard.ProductiveStatus.Should().Be("NO-GO");
        dashboard.Summary.ProductiveStatus.Should().Be("NO-GO");
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnProductiveExecutionFalse()
    {
        var dashboard = await Service().GetDashboardAsync();

        dashboard.Summary.ProductiveExecution.Should().BeFalse();
        dashboard.Readiness.Should().OnlyContain(x => x.ProductiveExecution == false);
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnWouldInvokeRealSoapFalse()
    {
        var dashboard = await Service().GetDashboardAsync();

        dashboard.Summary.WouldInvokeRealSoap.Should().BeFalse();
        dashboard.Readiness.Should().OnlyContain(x => x.WouldInvokeRealSoap == false);
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnSanitizedReadModels()
    {
        var serialized = Serialize(await Service().GetDashboardAsync());

        serialized.Should().NotContain("password");
        serialized.Should().NotContain("token");
        serialized.Should().NotContain("secret");
        serialized.Should().NotContain("endpointurl");
    }

    [Fact]
    public async Task GetSummary_ShouldReturnOperationalCounts()
    {
        var summary = await Service().GetSummaryAsync();

        summary.TotalFiles.Should().BeGreaterThan(0);
        summary.TotalDecisions.Should().BeGreaterThan(0);
        summary.TotalReadinessChecks.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetFiles_ShouldReturnReadOnlyFiles()
    {
        var files = await Service().GetFilesAsync();

        files.Should().NotBeEmpty();
        files.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.FileName));
    }

    [Fact]
    public async Task GetDecisions_ShouldReturnReadOnlyDecisions()
    {
        var decisions = await Service().GetDecisionsAsync();

        decisions.Should().NotBeEmpty();
        decisions.Should().Contain(x => x.SoapOperationCandidate == "RegistrarRespuestaTransaccion");
    }

    [Fact]
    public async Task GetSoapReadiness_ShouldReturnNoRealInvocation()
    {
        var readiness = await Service().GetSoapReadinessAsync();

        readiness.Should().OnlyContain(x => x.WouldInvokeRealSoap == false);
        readiness.Should().OnlyContain(x => x.Phase == "6B.5");
    }

    [Fact]
    public async Task GetAudit_ShouldReturnSanitizedAuditEvents()
    {
        var audit = await Service().GetAuditAsync();

        audit.Should().NotBeEmpty();
        Serialize(audit).Should().NotContain("soap envelope");
    }

    [Fact]
    public async Task ReadModels_ShouldNotExposeCredentials()
    {
        Serialize(await Service().GetDashboardAsync()).Should().NotContain("credential");
    }

    [Fact]
    public async Task ReadModels_ShouldNotExposeFullPayload()
    {
        Serialize(await Service().GetDashboardAsync()).Should().NotContain("soap envelope");
    }

    [Fact]
    public async Task ReadModels_ShouldNotExposeFullAccountNumbers()
    {
        Serialize(await Service().GetDashboardAsync()).Should().NotContain("1234567890123456");
    }

    [Fact]
    public async Task GetDashboardEndpoint_ShouldReturnOk()
    {
        var result = await Controller().GetDashboard(default);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSummaryEndpoint_ShouldReturnOk()
    {
        var result = await Controller().GetSummary(default);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetFilesEndpoint_ShouldReturnOk()
    {
        var result = await Controller().GetFiles(default);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetDecisionsEndpoint_ShouldReturnOk()
    {
        var result = await Controller().GetDecisions(default);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetSoapReadinessEndpoint_ShouldReturnOk()
    {
        var result = await Controller().GetSoapReadiness(default);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAuditEndpoint_ShouldReturnOk()
    {
        var result = await Controller().GetAudit(default);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void Endpoints_ShouldBeReadOnlyGetOnly()
    {
        var actionMethods = typeof(NachaOperationalReadinessController)
            .GetMethods()
            .Where(x => x.DeclaringType == typeof(NachaOperationalReadinessController) && x.IsPublic)
            .Where(x => x.Name.StartsWith("Get", StringComparison.Ordinal));

        foreach (var method in actionMethods)
        {
            method.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true).Should().NotBeEmpty();
            method.GetCustomAttributes(inherit: true)
                .Select(x => x.GetType().Name)
                .Should()
                .NotContain(["HttpPostAttribute", "HttpPutAttribute", "HttpPatchAttribute", "HttpDeleteAttribute"]);
        }
    }

    [Fact]
    public async Task Endpoints_ShouldNotTriggerSoapExecution()
    {
        var service = new Mock<INachaOperationalReadModelService>();
        service.Setup(x => x.GetDashboardAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((await Service().GetDashboardAsync()));
        var controller = new NachaOperationalReadinessController(service.Object);

        await controller.GetDashboard(default);

        service.Verify(x => x.GetDashboardAsync(It.IsAny<CancellationToken>()), Times.Once);
        service.VerifyNoOtherCalls();
    }

    private static NachaOperationalReadModelService Service() => new();

    private static NachaOperationalReadinessController Controller() => new(Service());

    private static string Serialize(object value)
        => System.Text.Json.JsonSerializer.Serialize(value).ToLowerInvariant();
}
