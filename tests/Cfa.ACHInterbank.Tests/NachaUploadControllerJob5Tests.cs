using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class NachaUploadControllerJob5Tests
{
    [Theory]
    [InlineData("0001283.001.20260723.1.OUT")]
    [InlineData("0001283.001.RET")]
    public async Task UploadNachaFile_AcceptsControlledExternalNames(string fileName)
    {
        await using var context = CreateContext();
        var ingestion = new Mock<IIncomingNachaIngestionAppService>();
        ingestion.Setup(x => x.IngestAsync(It.IsAny<IncomingNachaIngestionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaIngestionResponse
            {
                IngestionId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                OriginalFileName = fileName,
                FileHash = new string('a', 64),
                CorrelationId = "job5-test",
                IngestionStatus = IncomingNachaIngestionStatus.Completado,
                CycleResolutionStatus = IncomingNachaCycleResolutionStatus.ResueltoConfirmado,
                ParsingStatus = IncomingNachaParsingStatus.Exitoso
            });

        var controller = new NachaUploadController(
            ingestion.Object,
            context,
            Mock.Of<ILogger<NachaUploadController>>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        var bytes = System.Text.Encoding.ASCII.GetBytes("1" + new string(' ', 105));
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "File", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };

        var action = await controller.UploadNachaFile(
            new NachaUploadRequest { File = file },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(action);
        ingestion.Verify(x => x.IngestAsync(
            It.Is<IncomingNachaIngestionRequest>(request => request.FileName == fileName),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static AchDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
