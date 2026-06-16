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
    public void ExportDto_ShouldExposeIsExportableAndUnavailableReason()
    {
        var dto = new AchCycleExportDto
        {
            Id = "8dbe5a2ce0da7c9eaff2a82d6a9e704c34ef77fa",
            CycleId = "42",
            ExportIdentifier = "42",
            CycleName = "Ciclo exportable",
            ProcessingDate = new DateTime(2026, 5, 25),
            TransactionCount = 1,
            IsExportable = false,
            ExportUnavailableReason = "El ciclo tiene transacciones, pero no tiene lotes NACHA-M exportables asociados."
        };

        Assert.Equal("42", dto.CycleId);
        Assert.Equal("42", dto.ExportIdentifier);
        Assert.False(dto.IsExportable);
        Assert.Contains("no tiene lotes", dto.ExportUnavailableReason);
    }

    [Fact]
    public async Task NachaExport_ShouldReturnNotFoundWhenFileDoesNotExist()
    {
        const string identifier = "1b12995d45906869e194e237f3db64bfd7e07d2f";
        var builder = new Mock<INachaFileBuilder>(MockBehavior.Strict);
        var crypto = new Mock<ICryptoServiceScoped>(MockBehavior.Strict);
        var cycleService = new Mock<IAchCycleAppService>(MockBehavior.Strict);
        var clearingHouseService = new Mock<IClearingHouseService>(MockBehavior.Strict);
        var envelopePolicy = new Mock<IDigitalEnvelopePolicy>(MockBehavior.Strict);
        var identifierMapService = new Mock<INachaFileIdentifierMapService>(MockBehavior.Strict);
        var auditService = new Mock<IAchFileExportAuditService>(MockBehavior.Strict);
        var externalFileNamePolicy = new Mock<IExternalFileNamePolicy>(MockBehavior.Strict);

        cycleService
            .Setup(c => c.GetByIdAsync(identifier, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AchCycleDto?)null);

        var controller = new NachaExportController(
            builder.Object,
            crypto.Object,
            cycleService.Object,
            clearingHouseService.Object,
            envelopePolicy.Object,
            identifierMapService.Object,
            auditService.Object,
            externalFileNamePolicy.Object);

        var result = await controller.Export(identifier, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        builder.Verify(x => x.BuildNachaFileByCycleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NachaExport_ShouldReturnControlledErrorBodyFor422()
    {
        const string cycleId = "cycle-empty-controlled";
        var controller = BuildControllerForEmptyExport(cycleId);

        var result = await controller.Export(cycleId, CancellationToken.None);

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result);
        var payload = unprocessable.Value;
        Assert.NotNull(payload);
        Assert.Contains("NACHA_NO_EXPORTABLE_CONTENT", payload.ToString());
        Assert.Contains(cycleId, payload.ToString());
    }

    [Fact]
    public async Task NachaExport_ShouldNotExposeSensitiveDataInError()
    {
        const string cycleId = "cycle-sensitive-error";
        var controller = BuildControllerForEmptyExport(cycleId);

        var result = await controller.Export(cycleId, CancellationToken.None);

        var payload = Assert.IsType<UnprocessableEntityObjectResult>(result).Value?.ToString()?.ToLowerInvariant() ?? string.Empty;
        Assert.DoesNotContain("password", payload);
        Assert.DoesNotContain("token", payload);
        Assert.DoesNotContain("secret", payload);
        Assert.DoesNotContain("1234567890123456", payload);
    }

    [Fact]
    public void NachaExport_ShouldUseExpectedIdentifierType()
    {
        var action = typeof(NachaExportController).GetMethod(nameof(NachaExportController.Export));

        Assert.NotNull(action);
        var parameter = action!.GetParameters().Single(x => x.Name == "cycleId");
        Assert.Equal(typeof(string), parameter.ParameterType);
    }

    [Fact]
    public async Task Export_ReturnsPlainTextFileWithNachaContent_AndAuditsExport()
    {
        const string cycleId = "cycle-42";
        const string nachaContent = "HEADER\nDETAIL";
        const string externalFileName = "NACHA_cycle-42_20260520.txt";

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
            .Setup(s => s.RecordGeneratedFileAsync(cycleId, 1, "NACHA", externalFileName, 0, 0, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        externalFileNamePolicy
            .Setup(p => p.GenerateExternalNameAsync(It.IsAny<ExternalFileNameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalFileNameContext ctx, CancellationToken _) => new ExternalFileNamePolicyResult
            {
                ExternalFileName = externalFileName,
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
        Assert.Equal(externalFileName, fileResult.FileDownloadName);
        Assert.Equal(nachaContent, Encoding.ASCII.GetString(fileResult.FileContents));

        auditService.VerifyAll();
    }

    [Fact]
    public async Task ExportEncrypted_ReturnsEnvelopeFile_AndAuditsEncryptedExport()
    {
        const string cycleId = "cycle-99";
        const string nachaContent = "HEADER\nDETAIL\nTRAILER";
        const string externalFileName = "NACHA_cycle-99_20260520.txt";
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
            .ReturnsAsync(new AchCycleDto { Id = cycleId, ClearingHouseId = 7, CycleName = "Ciclo 7", ProcessingDate = DateTime.UtcNow });
        clearingHouseService
            .Setup(c => c.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClearingHouseDto { Id = 7, Code = "ACHCOL", OriginCode = "12345678", Name = "ACH Colombia" });
        envelopePolicy
            .Setup(p => p.ShouldEncrypt(7))
            .Returns(true);
        auditService
            .Setup(s => s.RecordGeneratedFileAsync(cycleId, 7, "NACHA", externalFileName, 0, 0, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        crypto
            .Setup(c => c.CreateEnvelopeAsync(It.Is<byte[]>(d => Encoding.ASCII.GetString(d) == nachaContent), externalFileName))
            .ReturnsAsync(expectedEnvelope);
        externalFileNamePolicy
            .Setup(p => p.GenerateExternalNameAsync(It.IsAny<ExternalFileNameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalFileNameContext ctx, CancellationToken _) => new ExternalFileNamePolicyResult
            {
                ExternalFileName = externalFileName,
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
        Assert.Equal($"{externalFileName}.ENV", fileResult.FileDownloadName);
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
        auditService
            .Setup(s => s.RecordGeneratedFileAsync(cycleId, 2, "NACHA", "12345678.003.1", 2, 0, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        externalFileNamePolicy
            .Setup(p => p.GenerateExternalNameAsync(It.IsAny<ExternalFileNameContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalFileNameContext ctx, CancellationToken _) => new ExternalFileNamePolicyResult
            {
                ExternalFileName = "12345678.003.1",
                Components = new ExternalFileNameComponents { FullName = "12345678.003.1", Prefix = "12345678", ExternalSequence = 3, FileIdModifier = 'B' },
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

    [Fact]
    public async Task Export_WhenBuilderReturnsEmptyContent_ReturnsUnprocessableEntity()
    {
        const string cycleId = "cycle-empty-export";

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
            .ReturnsAsync(string.Empty);

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
        Assert.Contains("NACHA_NO_EXPORTABLE_CONTENT", payload);
        Assert.Contains("No se gener", payload);
        auditService.Verify(
            x => x.RecordGeneratedFileAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Export_WhenBuilderThrowsPrenotificationPrerequisite_ReturnsUnprocessableEntity()
    {
        const string cycleId = "cycle-prenote";
        const string message = "La transaccion 4 no tiene prenotificacion previa.";

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
            .ThrowsAsync(new InvalidOperationException(message));

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
        Assert.Contains("NACHA_EXPORT_PREREQUISITE_FAILED", payload);
        Assert.Contains("prenotificacion", payload);
    }

    private static NachaExportController BuildControllerForEmptyExport(string cycleId)
    {
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
            .ReturnsAsync(string.Empty);

        return new NachaExportController(
            builder.Object,
            crypto.Object,
            cycleService.Object,
            clearingHouseService.Object,
            envelopePolicy.Object,
            identifierMapService.Object,
            auditService.Object,
            externalFileNamePolicy.Object);
    }
}
