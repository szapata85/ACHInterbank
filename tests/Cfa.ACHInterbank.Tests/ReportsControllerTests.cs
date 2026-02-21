using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class ReportsControllerTests
{
    [Fact]
    public async Task GetTraceabilityPdf_ReturnsPdfFile_WhenFilterIsValid()
    {
        var generator = new Mock<IReportGenerator>(MockBehavior.Strict);
        var expected = new GeneratedReportFile
        {
            Content = [1, 2, 3],
            ContentType = "application/pdf",
            FileName = "traceability.pdf"
        };

        generator
            .Setup(x => x.GenerateTraceabilityPdfAsync(
                It.Is<TraceabilityReportFilter>(f => f.AchCycleId == "C1" && f.State == AchTransferStateEnum.Pending),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = new ReportsController(generator.Object);

        var result = await controller.GetTraceabilityPdf(
            fromUtc: null,
            toUtc: null,
            state: AchTransferStateEnum.Pending,
            achCycleId: "C1",
            ct: CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("traceability.pdf", file.FileDownloadName);
        Assert.Equal(expected.Content, file.FileContents);

        generator.VerifyAll();
    }

    [Fact]
    public async Task GetTraceabilityPdf_ReturnsBadRequest_WhenFromIsGreaterThanTo()
    {
        var generator = new Mock<IReportGenerator>(MockBehavior.Strict);
        var controller = new ReportsController(generator.Object);

        var result = await controller.GetTraceabilityPdf(
            fromUtc: new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            toUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            state: null,
            achCycleId: null,
            ct: CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        generator.VerifyNoOtherCalls();
    }
}

