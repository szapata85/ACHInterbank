using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Application.Reports.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Diagnostics;
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
                It.Is<TraceabilityReportFilter>(f =>
                    f.AchCycleId == "C1" &&
                    f.State == AchTransferStateEnum.Pending &&
                    f.FromUtc.HasValue &&
                    f.ToUtc.HasValue),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var controller = CreateController(generator.Object);

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
        var controller = CreateController(generator.Object);

        var result = await controller.GetTraceabilityPdf(
            fromUtc: new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            toUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            state: null,
            achCycleId: null,
            ct: CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        generator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetTraceabilityPdf_ReturnsBadRequest_WhenRangeExceedsLimit()
    {
        var generator = new Mock<IReportGenerator>(MockBehavior.Strict);
        var controller = CreateController(generator.Object);

        var result = await controller.GetTraceabilityPdf(
            fromUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            toUtc: new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc),
            state: null,
            achCycleId: null,
            ct: CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("rango máximo", badRequest.Value?.ToString());
        generator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetTraceabilityPdf_ReturnsRequestTimeout_WhenGeneratorTimesOut()
    {
        var generator = new Mock<IReportGenerator>(MockBehavior.Strict);

        generator
            .Setup(x => x.GenerateTraceabilityPdfAsync(It.IsAny<TraceabilityReportFilter>(), It.IsAny<CancellationToken>()))
            .Returns(async (TraceabilityReportFilter _, CancellationToken token) =>
            {
                await Task.Delay(TimeSpan.FromMinutes(2), token);
                return new GeneratedReportFile();
            });

        var controller = CreateController(generator.Object);

        var result = await controller.GetTraceabilityPdf(
            fromUtc: DateTime.UtcNow.AddDays(-1),
            toUtc: DateTime.UtcNow,
            state: null,
            achCycleId: null,
            ct: CancellationToken.None);

        var timeout = Assert.IsType<ObjectResult>(result);
        Assert.Equal(408, timeout.StatusCode);
    }

    [Fact]
    public async Task GetTraceabilityPdf_CompletesWithinExpectedTime_ForFastGenerator()
    {
        var generator = new Mock<IReportGenerator>(MockBehavior.Strict);
        generator
            .Setup(x => x.GenerateTraceabilityPdfAsync(It.IsAny<TraceabilityReportFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeneratedReportFile
            {
                Content = [1],
                ContentType = "application/pdf",
                FileName = "fast.pdf"
            });

        var controller = CreateController(generator.Object);
        var stopwatch = Stopwatch.StartNew();

        var result = await controller.GetTraceabilityPdf(
            fromUtc: DateTime.UtcNow.AddDays(-1),
            toUtc: DateTime.UtcNow,
            state: null,
            achCycleId: null,
            ct: CancellationToken.None);

        stopwatch.Stop();

        Assert.IsType<FileContentResult>(result);
        Assert.True(stopwatch.ElapsedMilliseconds < 1_000, $"Expected fast execution, got {stopwatch.ElapsedMilliseconds}ms");
    }

    private static ReportsController CreateController(IReportGenerator generator)
    {
        var transaction = new Mock<IAchTransactionReportService>().Object;
        var returns = new Mock<IAchReturnRejectionReportService>().Object;
        var nachaCycle = new Mock<IAchNachaCycleReportService>().Object;
        var reconciliation = new Mock<IAchReconciliationReportService>().Object;
        var auditHistory = new Mock<IAchAuditHistoryReportService>().Object;
        var clearingHouse = new Mock<IClearingHouseService>();
        clearingHouse.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Domain.Entities.Ach.Dtos.ClearingHouseDto { Id = 1, Name = "ACH", Code = "ACH", OriginCode = "000" });

        return new ReportsController(
            generator,
            transaction,
            returns,
            nachaCycle,
            reconciliation,
            auditHistory,
            clearingHouse.Object,
            NullLogger<ReportsController>.Instance);
    }
}
