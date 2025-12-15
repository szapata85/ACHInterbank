using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Api.Encryption;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
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
        var cycleService = new Mock<IAchCycleAppService>(MockBehavior.Strict);
        var envelopePolicy = new Mock<IDigitalEnvelopePolicy>(MockBehavior.Strict);
        builder
            .Setup(b => b.BuildNachaFileByCycleAsync(cycleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nachaContent);

        var controller = new NachaExportController(builder.Object, crypto.Object, cycleService.Object, envelopePolicy.Object);

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
        var cycleService = new Mock<IAchCycleAppService>(MockBehavior.Strict);
        var envelopePolicy = new Mock<IDigitalEnvelopePolicy>(MockBehavior.Strict);

        builder
            .Setup(b => b.BuildNachaFileByCycleAsync(cycleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nachaContent);

        cycleService
            .Setup(c => c.GetByIdAsync(cycleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchCycleDto { Id = cycleId, ClearingHouseId = 7, CycleName = "cycle", ProcessingDate = DateTime.UtcNow });

        envelopePolicy
            .Setup(p => p.ShouldEncrypt(7))
            .Returns(true);

        crypto
            .Setup(c => c.CreateEnvelopeAsync(It.Is<byte[]>(d => Encoding.ASCII.GetString(d) == nachaContent), It.Is<string>(f => f.StartsWith($"NACHA_{cycleId}_") && f.EndsWith(".txt"))))
            .ReturnsAsync(expectedEnvelope);

        var controller = new NachaExportController(builder.Object, crypto.Object, cycleService.Object, envelopePolicy.Object);

        // Act
        var result = await controller.ExportEncrypted(cycleId, false, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/xml", fileResult.ContentType);
        Assert.StartsWith($"NACHA_{cycleId}_", fileResult.FileDownloadName);
        Assert.EndsWith(".ENV", fileResult.FileDownloadName);
        Assert.Equal(expectedEnvelope, fileResult.FileContents);

        builder.Verify(b => b.BuildNachaFileByCycleAsync(cycleId, It.IsAny<CancellationToken>()), Times.Once);
        cycleService.Verify(c => c.GetByIdAsync(cycleId, It.IsAny<CancellationToken>()), Times.Once);
        envelopePolicy.Verify(p => p.ShouldEncrypt(7), Times.Once);
        crypto.Verify(c => c.CreateEnvelopeAsync(It.IsAny<byte[]>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExportEncrypted_ReturnsPlainFile_WhenPolicyDisablesEncryption()
    {
        // Arrange
        const string cycleId = "cycle-101";
        const int clearingHouseId = 5;
        const string nachaContent = "CONTENT";

        var builder = new Mock<INachaFileBuilder>(MockBehavior.Strict);
        var crypto = new Mock<ICryptoServiceScoped>(MockBehavior.Strict);
        var cycleService = new Mock<IAchCycleAppService>(MockBehavior.Strict);
        var envelopePolicy = new Mock<IDigitalEnvelopePolicy>(MockBehavior.Strict);

        builder
            .Setup(b => b.BuildNachaFileByCycleAsync(cycleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nachaContent);

        cycleService
            .Setup(c => c.GetByIdAsync(cycleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchCycleDto { Id = cycleId, ClearingHouseId = clearingHouseId, CycleName = "cycle", ProcessingDate = DateTime.UtcNow });

        envelopePolicy
            .Setup(p => p.ShouldEncrypt(clearingHouseId))
            .Returns(false);

        var controller = new NachaExportController(builder.Object, crypto.Object, cycleService.Object, envelopePolicy.Object);

        // Act
        var result = await controller.ExportEncrypted(cycleId, false, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/plain", fileResult.ContentType);
        Assert.Equal(nachaContent, Encoding.ASCII.GetString(fileResult.FileContents));
        Assert.StartsWith($"NACHA_{cycleId}_", fileResult.FileDownloadName);
        Assert.EndsWith(".txt", fileResult.FileDownloadName);

        builder.Verify(b => b.BuildNachaFileByCycleAsync(cycleId, It.IsAny<CancellationToken>()), Times.Once);
        cycleService.Verify(c => c.GetByIdAsync(cycleId, It.IsAny<CancellationToken>()), Times.Once);
        envelopePolicy.Verify(p => p.ShouldEncrypt(clearingHouseId), Times.Once);
        crypto.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExportEncrypted_UsesDigitalEnvelope_WhenForced()
    {
        // Arrange
        const string cycleId = "cycle-202";
        const int clearingHouseId = 11;
        const string nachaContent = "FORCED";
        byte[] expectedEnvelope = Encoding.UTF8.GetBytes("<env>forced</env>");

        var builder = new Mock<INachaFileBuilder>(MockBehavior.Strict);
        var crypto = new Mock<ICryptoServiceScoped>(MockBehavior.Strict);
        var cycleService = new Mock<IAchCycleAppService>(MockBehavior.Strict);
        var envelopePolicy = new Mock<IDigitalEnvelopePolicy>(MockBehavior.Strict);

        builder
            .Setup(b => b.BuildNachaFileByCycleAsync(cycleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nachaContent);

        cycleService
            .Setup(c => c.GetByIdAsync(cycleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchCycleDto { Id = cycleId, ClearingHouseId = clearingHouseId, CycleName = "cycle", ProcessingDate = DateTime.UtcNow });

        envelopePolicy
            .Setup(p => p.ShouldEncrypt(clearingHouseId))
            .Returns(false);

        crypto
            .Setup(c => c.CreateEnvelopeAsync(It.Is<byte[]>(d => Encoding.ASCII.GetString(d) == nachaContent), It.Is<string>(f => f.StartsWith($"NACHA_{cycleId}_") && f.EndsWith(".txt"))))
            .ReturnsAsync(expectedEnvelope);

        var controller = new NachaExportController(builder.Object, crypto.Object, cycleService.Object, envelopePolicy.Object);

        // Act
        var result = await controller.ExportEncrypted(cycleId, true, CancellationToken.None);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/xml", fileResult.ContentType);
        Assert.StartsWith($"NACHA_{cycleId}_", fileResult.FileDownloadName);
        Assert.EndsWith(".ENV", fileResult.FileDownloadName);
        Assert.Equal(expectedEnvelope, fileResult.FileContents);

        builder.Verify(b => b.BuildNachaFileByCycleAsync(cycleId, It.IsAny<CancellationToken>()), Times.Once);
        cycleService.Verify(c => c.GetByIdAsync(cycleId, It.IsAny<CancellationToken>()), Times.Once);
        envelopePolicy.Verify(p => p.ShouldEncrypt(clearingHouseId), Times.Once);
        crypto.Verify(c => c.CreateEnvelopeAsync(It.IsAny<byte[]>(), It.IsAny<string>()), Times.Once);
    }
}
