using System.Reflection;
using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACHSobreDigital.ManagedDigitalEnvelope;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class SobreDigitalControllerTests
{
    [Fact]
    public void Routes_UseSingleApiContractAndMultipartPosts()
    {
        typeof(SobreDigitalController).GetCustomAttribute<RouteAttribute>()!.Template
            .Should().Be("api/nacha-security/digital-envelope");

        AssertPostRoute(nameof(SobreDigitalController.Encrypt), "encrypt");
        AssertPostRoute(nameof(SobreDigitalController.Decrypt), "decrypt");
    }

    [Fact]
    public async Task Encrypt_ReturnsBinaryAttachmentWithExactFileName()
    {
        var service = new Mock<IManagedDigitalEnvelopeService>();
        service.Setup(x => x.EncryptAsync(It.IsAny<ManagedDigitalEnvelopeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ManagedDigitalEnvelopeResult(
                [1, 2, 3],
                "archivo.OUT.ENV",
                "application/octet-stream",
                10,
                "THUMBPRINT",
                "PROFILE"));
        var controller = BuildController(service.Object);
        var request = BuildRequest("archivo.OUT", [9, 8, 7]);

        var result = await controller.Encrypt(request, CancellationToken.None);

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().Be("archivo.OUT.ENV");
        file.ContentType.Should().Be("application/octet-stream");
        file.FileContents.Should().Equal(1, 2, 3);
        controller.Response.Headers["X-Cryptographic-Profile"].ToString().Should().Be("PROFILE");
    }

    [Fact]
    public async Task Decrypt_ReturnsRecoveredOriginalName()
    {
        var service = new Mock<IManagedDigitalEnvelopeService>();
        service.Setup(x => x.DecryptAsync(It.IsAny<ManagedDigitalEnvelopeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ManagedDigitalEnvelopeResult(
                [9, 8, 7],
                "archivo.OUT",
                "application/octet-stream",
                10,
                "THUMBPRINT",
                "PROFILE"));
        var controller = BuildController(service.Object);

        var result = await controller.Decrypt(BuildRequest("archivo.OUT.ENV", [1, 2, 3]), CancellationToken.None);

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.FileDownloadName.Should().Be("archivo.OUT");
        file.FileContents.Should().Equal(9, 8, 7);
    }

    [Fact]
    public async Task Encrypt_RejectsEmptyFileBeforeInvokingCrypto()
    {
        var service = new Mock<IManagedDigitalEnvelopeService>(MockBehavior.Strict);
        var controller = BuildController(service.Object);

        var result = await controller.Encrypt(BuildRequest("archivo.OUT", []), CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        service.VerifyNoOtherCalls();
    }

    private static void AssertPostRoute(string methodName, string route)
    {
        var method = typeof(SobreDigitalController).GetMethod(methodName)!;
        method.GetCustomAttribute<HttpPostAttribute>()!.Template.Should().Be(route);
        method.GetCustomAttribute<ConsumesAttribute>()!.ContentTypes.Should().ContainSingle("multipart/form-data");
    }

    private static SobreDigitalController BuildController(IManagedDigitalEnvelopeService service)
    {
        var controller = new SobreDigitalController(service);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "unit-test")], "test"))
            }
        };
        return controller;
    }

    private static SobreDigitalController.DigitalEnvelopeFileRequest BuildRequest(string fileName, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new SobreDigitalController.DigitalEnvelopeFileRequest
        {
            CertificateVersionId = 10,
            File = new FormFile(stream, 0, content.Length, "file", fileName)
        };
    }
}
