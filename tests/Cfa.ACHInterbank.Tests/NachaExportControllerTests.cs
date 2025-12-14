using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
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
        var crypto = new Mock<ICryptoServiceScoped>(MockBehavior.Strict);
        builder
            .Setup(b => b.BuildNachaFileByCycleAsync(cycleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nachaContent);

        var controller = new NachaExportController(builder.Object, crypto.Object);

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

    [Fact]
    public async Task ExportEncrypted_ReturnsEnvelopeFile()
    {
        // Arrange
        const string cycleId = "cycle-99";
        const string nachaContent = "HEADER\nDETAIL\nTRAILER";
        byte[] expectedEnvelope = Encoding.UTF8.GetBytes("<envelope/>\n");

        var builder = new Mock<INachaFileBuilder>(MockBehavior.Strict);
        var crypto = new Mock<ICryptoServiceScoped>(MockBehavior.Strict);

        builder
            .Setup(b => b.BuildNachaFileByCycleAsync(cycleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nachaContent);

        crypto
            .Setup(c => c.CreateEnvelopeAsync(It.Is<byte[]>(d => Encoding.ASCII.GetString(d) == nachaContent), It.Is<string>(f => f.StartsWith($"NACHA_{cycleId}_") && f.EndsWith(".txt"))))
            .ReturnsAsync(expectedEnvelope);

        var controller = new NachaExportController(builder.Object, crypto.Object);

        // Act
        var result = await controller.ExportEncrypted(cycleId, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/xml", fileResult.ContentType);
        Assert.StartsWith($"NACHA_{cycleId}_", fileResult.FileDownloadName);
        Assert.EndsWith(".ENV", fileResult.FileDownloadName);
        Assert.Equal(expectedEnvelope, fileResult.FileContents);

        builder.Verify(b => b.BuildNachaFileByCycleAsync(cycleId, It.IsAny<CancellationToken>()), Times.Once);
        crypto.Verify(c => c.CreateEnvelopeAsync(It.IsAny<byte[]>(), It.IsAny<string>()), Times.Once);
    }
}
