using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Api.Encryption;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaExportControllerTests
{
    [Fact]
    public async Task Export_ReturnsPlainTextFileWithNachaContent_AndAuditsExport()
    {
        const string cycleId = "cycle-42";
        const string nachaContent = "HEADER\nDETAIL";

        var builder = new Mock<INachaFileBuilder>(MockBehavior.Strict);
        var crypto = new Mock<ICryptoServiceScoped>(MockBehavior.Strict);
        var cycleService = new Mock<IAchCycleAppService>(MockBehavior.Strict);
        var clearingHouseService = new Mock<IClearingHouseService>(MockBehavior.Strict);
        var envelopePolicy = new Mock<IDigitalEnvelopePolicy>(MockBehavior.Strict);
        var identifierMapService = new Mock<INachaFileIdentifierMapService>(MockBehavior.Strict);
        var auditService = new Mock<IAchFileExportAuditService>(MockBehavior.Strict);
        var externalFileNamePolicy = new Mock<IExternalFileNamePolicy>(MockBehavior.Strict);

        builder
            .Setup(b => b.BuildNachaFileByCycleAsync(cycleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nachaContent);
        cycleService
            .Setup(c => c.GetByIdAsync(cycleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchCycleDto { Id = cycleId, ClearingHouseId = 1, CycleName = "CICLO-1", ProcessingDate = DateTime.UtcNow });
        clearingHouseService
            .Setup(c => c.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClearingHouseDto { Id = 1, Code = "ACHCOL", OriginCode = "12345678", Name = "ACH Colombia" });
        auditService
            .Setup(s => s.RecordGeneratedFileAsync(cycleId, 1, "NACHA", It.Is<string>(f => f.StartsWith($"NACHA_{cycleId}_") && f.EndsWith(".txt")), 0, 0, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        externalFileNamePolicy
            .Setup(p => p.GenerateExternalNameAsync(It.IsAny<ExternalFileNameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalFileNameContext ctx, CancellationToken _) => new ExternalFileNamePolicyResult
            {
                ExternalFileName = ctx.ProvidedExternalFileName ?? ctx.InternalFileName ?? "file.txt",
                Validation = new ExternalFileNameValidationResult { Disposition = ExternalFileValidationDisposition.Passed }
            });

        var controller = new NachaExportController(
            builder.Object,
            crypto.Object,
            cycleService.Object,
            clearingHouseService.Object,
            envelopePolicy.Object,
            identifierMapService.Object,
            auditService.Object,
            externalFileNamePolicy.Object);

        var result = await controller.Export(cycleId, CancellationToken.None);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/plain", fileResult.ContentType);
        Assert.StartsWith($"NACHA_{cycleId}_", fileResult.FileDownloadName);
        Assert.EndsWith(".txt", fileResult.FileDownloadName);
        Assert.Equal(nachaContent, Encoding.ASCII.GetString(fileResult.FileContents));

        auditService.VerifyAll();
    }

    [Fact]
    public async Task ExportEncrypted_ReturnsEnvelopeFile_AndAuditsEncryptedExport()
    {
        const string cycleId = "cycle-99";
        const string nachaContent = "HEADER\nDETAIL\nTRAILER";
        byte[] expectedEnvelope = Encoding.UTF8.GetBytes("<envelope/>\n");

        var builder = new Mock<INachaFileBuilder>(MockBehavior.Strict);
        var crypto = new Mock<ICryptoServiceScoped>(MockBehavior.Strict);
        var cycleService = new Mock<IAchCycleAppService>(MockBehavior.Strict);
        var clearingHouseService = new Mock<IClearingHouseService>(MockBehavior.Strict);
        var envelopePolicy = new Mock<IDigitalEnvelopePolicy>(MockBehavior.Strict);
        var identifierMapService = new Mock<INachaFileIdentifierMapService>(MockBehavior.Strict);
        var auditService = new Mock<IAchFileExportAuditService>(MockBehavior.Strict);
        var externalFileNamePolicy = new Mock<IExternalFileNamePolicy>(MockBehavior.Strict);

        builder
            .Setup(b => b.BuildNachaFileByCycleAsync(cycleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nachaContent);
        cycleService
            .Setup(c => c.GetByIdAsync(cycleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchCycleDto { Id = cycleId, ClearingHouseId = 7, CycleName = "cycle", ProcessingDate = DateTime.UtcNow });
        clearingHouseService
            .Setup(c => c.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClearingHouseDto { Id = 7, Code = "ACHCOL", OriginCode = "12345678", Name = "ACH Colombia" });
        envelopePolicy
            .Setup(p => p.ShouldEncrypt(7))
            .Returns(true);
        auditService
            .Setup(s => s.RecordGeneratedFileAsync(cycleId, 7, "NACHA", It.Is<string>(f => f.StartsWith($"NACHA_{cycleId}_") && f.EndsWith(".txt")), 0, 0, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        crypto
            .Setup(c => c.CreateEnvelopeAsync(It.Is<byte[]>(d => Encoding.ASCII.GetString(d) == nachaContent), It.Is<string>(f => f.StartsWith($"NACHA_{cycleId}_") && f.EndsWith(".txt"))))
            .ReturnsAsync(expectedEnvelope);
        externalFileNamePolicy
            .Setup(p => p.GenerateExternalNameAsync(It.IsAny<ExternalFileNameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalFileNameContext ctx, CancellationToken _) => new ExternalFileNamePolicyResult
            {
                ExternalFileName = ctx.ProvidedExternalFileName ?? ctx.InternalFileName ?? "file.txt",
                Validation = new ExternalFileNameValidationResult { Disposition = ExternalFileValidationDisposition.Passed }
            });

        var controller = new NachaExportController(
            builder.Object,
            crypto.Object,
            cycleService.Object,
            clearingHouseService.Object,
            envelopePolicy.Object,
            identifierMapService.Object,
            auditService.Object,
            externalFileNamePolicy.Object);

        var result = await controller.ExportEncrypted(cycleId, false, CancellationToken.None);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/xml", fileResult.ContentType);
        Assert.StartsWith($"NACHA_{cycleId}_", fileResult.FileDownloadName);
        Assert.EndsWith(".ENV", fileResult.FileDownloadName);
        Assert.Equal(expectedEnvelope, fileResult.FileContents);

        auditService.VerifyAll();
        crypto.VerifyAll();
    }

    [Fact]
    public async Task ExportEncrypted_UsesCenitNamingAndIdentifierNormalization()
    {
        const string cycleId = "cycle-cenit";
        var nachaContent = new string('1', 106) + new string('5', 106);

        var builder = new Mock<INachaFileBuilder>(MockBehavior.Strict);
        var crypto = new Mock<ICryptoServiceScoped>(MockBehavior.Strict);
        var cycleService = new Mock<IAchCycleAppService>(MockBehavior.Strict);
        var clearingHouseService = new Mock<IClearingHouseService>(MockBehavior.Strict);
        var envelopePolicy = new Mock<IDigitalEnvelopePolicy>(MockBehavior.Strict);
        var identifierMapService = new Mock<INachaFileIdentifierMapService>(MockBehavior.Strict);
        var auditService = new Mock<IAchFileExportAuditService>(MockBehavior.Strict);
        var externalFileNamePolicy = new Mock<IExternalFileNamePolicy>(MockBehavior.Strict);

        builder
            .Setup(b => b.BuildNachaFileByCycleAsync(cycleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nachaContent);
        cycleService
            .Setup(c => c.GetByIdAsync(cycleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchCycleDto { Id = cycleId, ClearingHouseId = 2, CycleName = "CICLO-3", ProcessingDate = DateTime.UtcNow });
        clearingHouseService
            .Setup(c => c.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClearingHouseDto { Id = 2, Code = "CENIT", OriginCode = "12345678", Name = "CENIT" });
        envelopePolicy
            .Setup(p => p.ShouldEncrypt(2))
            .Returns(false);
        identifierMapService
            .Setup(s => s.ResolveIdentifierAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync('B');
        auditService
            .Setup(s => s.RecordGeneratedFileAsync(cycleId, 2, "NACHA", "12345678.003.1", 2, 0, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        externalFileNamePolicy
            .Setup(p => p.GenerateExternalNameAsync(It.IsAny<ExternalFileNameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalFileNameContext ctx, CancellationToken _) => new ExternalFileNamePolicyResult
            {
                ExternalFileName = "12345678.003.1",
                Validation = new ExternalFileNameValidationResult { Disposition = ExternalFileValidationDisposition.Passed }
            });

        var controller = new NachaExportController(
            builder.Object,
            crypto.Object,
            cycleService.Object,
            clearingHouseService.Object,
            envelopePolicy.Object,
            identifierMapService.Object,
            auditService.Object,
            externalFileNamePolicy.Object);

        var result = await controller.ExportEncrypted(cycleId, false, CancellationToken.None);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/plain", fileResult.ContentType);
        Assert.Equal("12345678.003.1", fileResult.FileDownloadName);
        Assert.Equal('B', Encoding.ASCII.GetString(fileResult.FileContents)[35]);

        auditService.VerifyAll();
        identifierMapService.VerifyAll();
    }

    [Fact]
    public async Task Export_WhenBuilderThrowsFatalValidation_ReturnsUnprocessableEntity()
    {
        const string cycleId = "cycle-fail";
        const string fatalMessage = "Error Fatal ID 22: la transacción 2 no tiene Nombre del Usuario Receptor válido para posiciones 63-84 del registro tipo 6.";

        var builder = new Mock<INachaFileBuilder>(MockBehavior.Strict);
        var crypto = new Mock<ICryptoServiceScoped>(MockBehavior.Strict);
        var cycleService = new Mock<IAchCycleAppService>(MockBehavior.Strict);
        var clearingHouseService = new Mock<IClearingHouseService>(MockBehavior.Strict);
        var envelopePolicy = new Mock<IDigitalEnvelopePolicy>(MockBehavior.Strict);
        var identifierMapService = new Mock<INachaFileIdentifierMapService>(MockBehavior.Strict);
        var auditService = new Mock<IAchFileExportAuditService>(MockBehavior.Strict);
        var externalFileNamePolicy = new Mock<IExternalFileNamePolicy>(MockBehavior.Strict);

        cycleService
            .Setup(c => c.GetByIdAsync(cycleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchCycleDto { Id = cycleId, ClearingHouseId = 1, CycleName = "CICLO-1", ProcessingDate = DateTime.UtcNow });
        clearingHouseService
            .Setup(c => c.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClearingHouseDto { Id = 1, Code = "ACHCOL", OriginCode = "12345678", Name = "ACH Colombia" });
        builder
            .Setup(b => b.BuildNachaFileByCycleAsync(cycleId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(fatalMessage));

        var controller = new NachaExportController(
            builder.Object,
            crypto.Object,
            cycleService.Object,
            clearingHouseService.Object,
            envelopePolicy.Object,
            identifierMapService.Object,
            auditService.Object,
            externalFileNamePolicy.Object);

        var result = await controller.Export(cycleId, CancellationToken.None);

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result);
        var payload = unprocessable.Value?.ToString() ?? string.Empty;
        Assert.Contains("NACHA_VALIDATION_ERROR", payload);
        Assert.Contains("Error Fatal ID 22", payload);
    }
}
