using System.IO;
using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Operations;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public class NachaSecurityOperationsControllerTests
{
    [Fact]
    public async Task GetByOperationId_ReturnsNotFound_WhenMissing()
    {
        var service = new Mock<INachaSecurityOperationService>();
        service
            .Setup(s => s.GetByOperationIdAsync("op_missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((DigitalEnvelopeOperationDto?)null);

        var controller = BuildController(service.Object);

        var result = await controller.GetByOperationIdAsync("op_missing", CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task AuthorizeDownload_ReturnsOk_WhenAuthorized()
    {
        var service = new Mock<INachaSecurityOperationService>();
        service
            .Setup(s => s.GetByOperationIdAsync("op_ok", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSuccessOperation("op_ok", NachaSecurityOperationType.NachaGenerateEncrypted, "application/xml", downloadAvailable: true));
        service
            .Setup(s => s.AuthorizeDownloadAsync("op_ok", It.IsAny<OperationRequestContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DownloadAuthorizationResult(true, DateTime.UtcNow.AddMinutes(5), null, null));

        var controller = BuildController(service.Object, FineGrainedPermissions.CanDownloadEnvelope);

        var result = await controller.AuthorizeDownloadAsync("op_ok", CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GeneratePlain_ReturnsDto()
    {
        var dto = BuildSuccessOperation("op_1", NachaSecurityOperationType.NachaGeneratePlain, "text/plain", true);

        var service = new Mock<INachaSecurityOperationService>();
        service
            .Setup(s => s.GeneratePlainAsync(It.IsAny<NachaGenerateRequest>(), It.IsAny<OperationRequestContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var controller = BuildController(service.Object);

        var result = await controller.GeneratePlainAsync(new NachaSecurityOperationsController.NachaGenerateApiRequest { CycleId = "cycle-1" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<DigitalEnvelopeOperationDto>(ok.Value);
        Assert.Equal("op_1", payload.OperationId);
    }

    [Fact]
    public async Task Download_ReturnsBadRequest_WhenUnauthorized()
    {
        var service = new Mock<INachaSecurityOperationService>();
        service
            .Setup(s => s.GetByOperationIdAsync("op_denied", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSuccessOperation("op_denied", NachaSecurityOperationType.NachaGenerateEncrypted, "application/xml", true));
        service
            .Setup(s => s.OpenDownloadAsync("op_denied", It.IsAny<OperationRequestContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OperationDownloadDescriptor?)null);

        var controller = BuildController(service.Object, FineGrainedPermissions.CanDownloadEnvelope);

        var result = await controller.DownloadAsync("op_denied", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ManualDecrypt_ReturnsSanitizedFailure_WithoutPlainContentInResponse()
    {
        var dto = new DigitalEnvelopeOperationDto(
            "op_fail",
            NachaSecurityOperationType.ManualEnvelopeDecrypt,
            NachaSecurityOperationStatus.Failed,
            1,
            "tester",
            DateTime.UtcNow,
            DateTime.UtcNow,
            true,
            false,
            new DigitalEnvelopeOperationArtifactDto(null, null, null, null, false, null, null),
            new DigitalEnvelopeOperationErrorDto("SIGNATURE_VALIDATION_FAILED", "No fue posible validar la firma del sobre digital.", false),
            new DigitalEnvelopeCertificateSummaryDto(null, null, null));

        var service = new Mock<INachaSecurityOperationService>();
        service
            .Setup(s => s.ManualDecryptAsync(It.IsAny<ManualEnvelopeRequest>(), It.IsAny<OperationRequestContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var controller = BuildController(service.Object);
        var content = new MemoryStream([1, 2, 3]);
        var file = new FormFile(content, 0, content.Length, "file", "sample.env");

        var result = await controller.ManualDecryptAsync(file, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<DigitalEnvelopeOperationDto>(ok.Value);
        Assert.Equal(NachaSecurityOperationStatus.Failed, payload.Status);
        Assert.False(payload.Artifact.DownloadAvailable);
        Assert.Null(payload.Artifact.ExternalFileName);
    }

    [Fact]
    public async Task AuthorizeDownload_ReturnsForbid_WhenMissingArtifactPermission()
    {
        var service = new Mock<INachaSecurityOperationService>();
        service
            .Setup(s => s.GetByOperationIdAsync("op_plain", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSuccessOperation("op_plain", NachaSecurityOperationType.ManualEnvelopeDecrypt, "text/plain", true));

        var controller = BuildController(service.Object, FineGrainedPermissions.CanDownloadEnvelope);

        var result = await controller.AuthorizeDownloadAsync("op_plain", CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task AuthorizeDownload_RejectsPlain_WhenSignatureValidationFailed()
    {
        var service = new Mock<INachaSecurityOperationService>();
        service
            .Setup(s => s.GetByOperationIdAsync("op_plain_failed", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DigitalEnvelopeOperationDto(
                "op_plain_failed",
                NachaSecurityOperationType.ManualEnvelopeDecrypt,
                NachaSecurityOperationStatus.Failed,
                1,
                "tester",
                DateTime.UtcNow,
                DateTime.UtcNow,
                true,
                false,
                new DigitalEnvelopeOperationArtifactDto("op_plain_failed.txt", "text/plain", null, null, false, null, null),
                new DigitalEnvelopeOperationErrorDto("SIGNATURE_VALIDATION_FAILED", "Firma inválida", false),
                new DigitalEnvelopeCertificateSummaryDto(null, null, null)));

        var controller = BuildController(service.Object, FineGrainedPermissions.CanDownloadPlainNacha);

        var result = await controller.AuthorizeDownloadAsync("op_plain_failed", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData(nameof(NachaSecurityOperationsController.GeneratePlainAsync), FineGrainedPermissions.CanGenerateNacha)]
    [InlineData(nameof(NachaSecurityOperationsController.GenerateEncryptedAsync), FineGrainedPermissions.CanGenerateEncryptedNacha)]
    [InlineData(nameof(NachaSecurityOperationsController.ManualEncryptAsync), FineGrainedPermissions.CanManualEncryptEnvelope)]
    [InlineData(nameof(NachaSecurityOperationsController.ManualDecryptAsync), FineGrainedPermissions.CanManualDecryptEnvelope)]
    [InlineData(nameof(NachaSecurityOperationsController.AuditAsync), FineGrainedPermissions.CanViewNachaSecurityAudit)]
    public void Endpoints_UseExpectedFineGrainedPolicy(string methodName, string expectedPolicy)
    {
        var method = typeof(NachaSecurityOperationsController).GetMethod(methodName);
        Assert.NotNull(method);

        var attr = method!.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .FirstOrDefault();

        Assert.NotNull(attr);
        Assert.Equal(expectedPolicy, attr!.Policy);
    }

    private static NachaSecurityOperationsController BuildController(INachaSecurityOperationService service, params string[] permissions)
    {
        var controller = new NachaSecurityOperationsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = BuildUser(permissions)
                }
            }
        };

        return controller;
    }

    private static ClaimsPrincipal BuildUser(params string[] permissions)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, "tester") };
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private static DigitalEnvelopeOperationDto BuildSuccessOperation(string operationId, NachaSecurityOperationType type, string contentType, bool downloadAvailable)
    {
        var now = DateTime.UtcNow;
        return new DigitalEnvelopeOperationDto(
            operationId,
            type,
            NachaSecurityOperationStatus.Success,
            1,
            "tester",
            now,
            now,
            false,
            false,
            new DigitalEnvelopeOperationArtifactDto("file.env", contentType, "hash", "envhash", downloadAvailable, now.AddMinutes(5), 120),
            null,
            new DigitalEnvelopeCertificateSummaryDto(null, null, null));
    }
}
