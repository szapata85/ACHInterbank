using System.Text;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaExportControllerTests
{
    [Fact]
    public async Task Export_ReturnsPlainTextFileWithNachaContent()
    {
        const string cycleId = "cycle-42";
        var exportService = new Mock<INachaExportService>(MockBehavior.Strict);
        exportService
            .Setup(service => service.ExportAsync(cycleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NachaExportResult(Encoding.ASCII.GetBytes("HEADERDETAIL"), "text/plain", "NACHA_cycle-42_20260322_120000.txt", false));

        var controller = new NachaExportController(exportService.Object);

        var result = await controller.Export(cycleId, CancellationToken.None);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/plain", fileResult.ContentType);
        Assert.Equal("NACHA_cycle-42_20260322_120000.txt", fileResult.FileDownloadName);
        Assert.Equal("HEADERDETAIL", Encoding.ASCII.GetString(fileResult.FileContents));
    }

    [Fact]
    public async Task ExportEncrypted_ReturnsEnvelopeFile()
    {
        const string cycleId = "cycle-99";
        var exportService = new Mock<INachaExportService>(MockBehavior.Strict);
        exportService
            .Setup(service => service.ExportEncryptedAsync(cycleId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NachaExportResult(Encoding.UTF8.GetBytes("<envelope/>"), "application/xml", "001.003.1.ENV", true));

        var controller = new NachaExportController(exportService.Object);

        var result = await controller.ExportEncrypted(cycleId, false, CancellationToken.None);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/xml", fileResult.ContentType);
        Assert.Equal("001.003.1.ENV", fileResult.FileDownloadName);
        Assert.Equal("<envelope/>", Encoding.UTF8.GetString(fileResult.FileContents));
    }

    [Fact]
    public async Task Export_ReturnsNotFound_WhenCycleDoesNotExist()
    {
        const string cycleId = "missing-cycle";
        var exportService = new Mock<INachaExportService>(MockBehavior.Strict);
        exportService
            .Setup(service => service.ExportAsync(cycleId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"No existe el ciclo {cycleId}."));

        var controller = new NachaExportController(exportService.Object);

        var result = await controller.Export(cycleId, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("No existe el ciclo", notFound.Value!.ToString());
    }
}
