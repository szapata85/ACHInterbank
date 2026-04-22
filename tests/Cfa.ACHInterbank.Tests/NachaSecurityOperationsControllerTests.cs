using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Operations;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
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

        var controller = new NachaSecurityOperationsController(service.Object);

        var result = await controller.GetByOperationIdAsync("op_missing", CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task AuthorizeDownload_ReturnsOk_WhenAuthorized()
    {
        var service = new Mock<INachaSecurityOperationService>();
        service
            .Setup(s => s.AuthorizeDownloadAsync("op_ok", It.IsAny<OperationRequestContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DownloadAuthorizationResult(true, DateTime.UtcNow.AddMinutes(5), null, null));

        var controller = new NachaSecurityOperationsController(service.Object);

        var result = await controller.AuthorizeDownloadAsync("op_ok", CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GeneratePlain_ReturnsDto()
    {
        var now = DateTime.UtcNow;
        var dto = new DigitalEnvelopeOperationDto(
            "op_1",
            NachaSecurityOperationType.NachaGeneratePlain,
            NachaSecurityOperationStatus.Success,
            1,
            "tester",
            now,
            now,
            false,
            false,
            new DigitalEnvelopeOperationArtifactDto("file.txt", "text/plain", "hash", null, true, now.AddMinutes(30), 120),
            null,
            new DigitalEnvelopeCertificateSummaryDto(null, null, null));

        var service = new Mock<INachaSecurityOperationService>();
        service
            .Setup(s => s.GeneratePlainAsync(It.IsAny<NachaGenerateRequest>(), It.IsAny<OperationRequestContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var controller = new NachaSecurityOperationsController(service.Object);

        var result = await controller.GeneratePlainAsync(new NachaSecurityOperationsController.NachaGenerateApiRequest { CycleId = "cycle-1" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<DigitalEnvelopeOperationDto>(ok.Value);
        Assert.Equal("op_1", payload.OperationId);
    }
}
