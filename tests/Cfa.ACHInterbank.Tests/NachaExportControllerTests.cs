using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        // Arrange
        const string cycleId = "cycle-42";
        const string nachaContent = "HEADER\nDETAIL";

        var builder = new Mock<INachaFileBuilder>(MockBehavior.Strict);
        builder
            .Setup(b => b.BuildNachaFileByCycleAsync(cycleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nachaContent);

        var controller = new NachaExportController(builder.Object);

        // Act
        var result = await controller.Export(cycleId, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/plain", fileResult.ContentType);
        Assert.StartsWith($"NACHA_{cycleId}_", fileResult.FileDownloadName);
        Assert.EndsWith(".txt", fileResult.FileDownloadName);
        Assert.Equal(nachaContent, Encoding.ASCII.GetString(fileResult.FileContents));

        builder.Verify(b => b.BuildNachaFileByCycleAsync(cycleId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
