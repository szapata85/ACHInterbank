using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class AchReturnOfReturnControllerTests
{
    [Fact]
    public async Task Evaluate_ReturnOfReturn_ReturnsEligibilityResult()
    {
        var eligibility = new Mock<IAchReturnOfReturnEligibilityService>();
        eligibility.Setup(x => x.EvaluateAsync(It.IsAny<AchReturnOfReturnEligibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchReturnOfReturnEligibilityResult(true, 7001, 10, "R01", "R02", true, []));

        var generation = new Mock<IAchReturnOfReturnFileGenerationService>();
        var sut = new AchReturnOfReturnController(eligibility.Object, generation.Object);

        var result = await sut.Evaluate(new EvaluateReturnOfReturnRequest(10, "R02"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AchReturnOfReturnEligibilityResult>(ok.Value);
        Assert.True(payload.IsEligible);
        Assert.Equal(7001, payload.ClearingHouseId);
    }

    [Fact]
    public async Task Evaluate_ReturnOfReturn_InvalidRequest_ReturnsBadRequest()
    {
        var sut = new AchReturnOfReturnController(Mock.Of<IAchReturnOfReturnEligibilityService>(), Mock.Of<IAchReturnOfReturnFileGenerationService>());

        var result = await sut.Evaluate(new EvaluateReturnOfReturnRequest(0, " "), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Evaluate_ReturnOfReturn_NotEligible_Returns200WithFailures()
    {
        var eligibility = new Mock<IAchReturnOfReturnEligibilityService>();
        eligibility.Setup(x => x.EvaluateAsync(It.IsAny<AchReturnOfReturnEligibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchReturnOfReturnEligibilityResult(false, 7001, 10, "R01", "R09", true,
            [new AchReturnOfReturnEligibilityFailure("RETURN_OF_RETURN_POLICY_REJECTED", "No permitido") ]));

        var sut = new AchReturnOfReturnController(eligibility.Object, Mock.Of<IAchReturnOfReturnFileGenerationService>());
        var result = await sut.Evaluate(new EvaluateReturnOfReturnRequest(10, "R09"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AchReturnOfReturnEligibilityResult>(ok.Value);
        Assert.False(payload.IsEligible);
        Assert.NotEmpty(payload.Failures);
    }

    [Fact]
    public async Task GenerateAuditFile_ReturnOfReturn_ReturnsFile()
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes("ROR|CH:7001");
        var generation = new Mock<IAchReturnOfReturnFileGenerationService>();
        generation.Setup(x => x.GenerateAsync(It.IsAny<AchReturnOfReturnFileGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchReturnOfReturnFileGenerationResult(true, "ROR_7001_20260514123456.ach", "ROR|CH:7001", bytes, 1, [10], [], 11, "abc"));

        var sut = new AchReturnOfReturnController(Mock.Of<IAchReturnOfReturnEligibilityService>(), generation.Object);

        var result = await sut.GenerateAuditFile(new GenerateReturnOfReturnAuditFileRequest([10]), CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/plain", file.ContentType);
        Assert.Equal("ROR_7001_20260514123456.ach", file.FileDownloadName);
    }

    [Fact]
    public async Task GenerateAuditFile_ReturnOfReturn_EmptyRequest_ReturnsBadRequest()
    {
        var sut = new AchReturnOfReturnController(Mock.Of<IAchReturnOfReturnEligibilityService>(), Mock.Of<IAchReturnOfReturnFileGenerationService>());

        var result = await sut.GenerateAuditFile(new GenerateReturnOfReturnAuditFileRequest([]), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GenerateNachaFile_ReturnOfReturn_ReturnsFileContentResult()
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes("1HEADER");
        var generation = new Mock<IAchReturnOfReturnFileGenerationService>();
        generation.Setup(x => x.GenerateNachaAsync(It.IsAny<AchReturnOfReturnFileGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchReturnOfReturnFileGenerationResult(true, "RORNACHA_7001_20260514123456.ach", "1HEADER", bytes, 1, [10], [], 11, "abc"));
        var sut = new AchReturnOfReturnController(Mock.Of<IAchReturnOfReturnEligibilityService>(), generation.Object);
        var result = await sut.GenerateNachaFile(new GenerateReturnOfReturnAuditFileRequest([10]), CancellationToken.None);
        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("RORNACHA_7001_20260514123456.ach", file.FileDownloadName);
    }

    [Fact]
    public async Task GenerateNachaFile_EmptyRequest_ReturnsBadRequest()
    {
        var sut = new AchReturnOfReturnController(Mock.Of<IAchReturnOfReturnEligibilityService>(), Mock.Of<IAchReturnOfReturnFileGenerationService>());
        var result = await sut.GenerateNachaFile(new GenerateReturnOfReturnAuditFileRequest([]), CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GenerateNachaFile_NotEligible_ReturnsConflict()
    {
        var generation = new Mock<IAchReturnOfReturnFileGenerationService>();
        generation.Setup(x => x.GenerateNachaAsync(It.IsAny<AchReturnOfReturnFileGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchReturnOfReturnFileGenerationResult(false, null, null, null, 0, [10], [new("RETURN_OF_RETURN_POLICY_REJECTED", "No elegible")], null, null));
        var sut = new AchReturnOfReturnController(Mock.Of<IAchReturnOfReturnEligibilityService>(), generation.Object);
        var result = await sut.GenerateNachaFile(new GenerateReturnOfReturnAuditFileRequest([10]), CancellationToken.None);
        Assert.IsType<ConflictObjectResult>(result);
    }
}
