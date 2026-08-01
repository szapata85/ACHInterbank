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
    [Fact]
    public async Task UploadNachaFile_AcceptsDigitalEnvelope_WhenClearingHouseIsSelected()
    {
        await using var context = CreateContext();
        var ingestion = new Mock<IIncomingNachaIngestionAppService>();
        ingestion.Setup(x => x.IngestAsync(
                It.Is<IncomingNachaIngestionRequest>(request =>
                    request.FileName == "0001283.001.20260731.1.OUT.env"
                    && request.RequestedClearingHouseId == 7),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaIngestionResponse
            {
                IngestionId = Guid.NewGuid(),
                OriginalFileName = "0001283.001.20260731.1.OUT.env",
                FileHash = new string('a', 64),
                CorrelationId = "env-test",
                IngestionStatus = IncomingNachaIngestionStatus.Completado,
                CycleResolutionStatus = IncomingNachaCycleResolutionStatus.ResueltoConfirmado,
                ParsingStatus = IncomingNachaParsingStatus.Exitoso
            });
        var controller = BuildController(ingestion.Object, context);
        var file = BuildFile("0001283.001.20260731.1.OUT.env");

        var action = await controller.UploadNachaFile(
            new NachaUploadRequest { File = file, ClearingHouseId = 7 },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(action);
        ingestion.VerifyAll();
    }

    [Fact]
    public async Task UploadNachaFile_RejectsDigitalEnvelope_WhenClearingHouseIsMissing()
    {
        await using var context = CreateContext();
        var ingestion = new Mock<IIncomingNachaIngestionAppService>(MockBehavior.Strict);
        var controller = BuildController(ingestion.Object, context);

        var action = await controller.UploadNachaFile(
            new NachaUploadRequest { File = BuildFile("0001283.001.20260731.1.OUT.env") },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(action);
        ingestion.Verify(x => x.IngestAsync(It.IsAny<IncomingNachaIngestionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadNachaFile_ForwardsControlledReprocessFields()
    {
        await using var context = CreateContext();
        var parentIngestionId = Guid.NewGuid();
        var ingestion = new Mock<IIncomingNachaIngestionAppService>();
        ingestion.Setup(x => x.IngestAsync(
                It.Is<IncomingNachaIngestionRequest>(request =>
                    request.ForceReprocess
                    && request.ParentIngestionId == parentIngestionId
                    && request.RequestedClearingHouseId == 7),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaIngestionResponse
            {
                IngestionId = Guid.NewGuid(),
                OriginalFileName = "0001283.001.20260731.1.OUT.env",
                FileHash = new string('a', 64),
                CorrelationId = "reprocess-test",
                IngestionStatus = IncomingNachaIngestionStatus.Completado,
                CycleResolutionStatus = IncomingNachaCycleResolutionStatus.ResueltoInferido,
                ParsingStatus = IncomingNachaParsingStatus.Exitoso
            });
        var controller = BuildController(ingestion.Object, context);

        var action = await controller.UploadNachaFile(
            new NachaUploadRequest
            {
                File = BuildFile("0001283.001.20260731.1.OUT.env"),
                ClearingHouseId = 7,
                ForceReprocess = true,
                ParentIngestionId = parentIngestionId
            },
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(action);
        ingestion.VerifyAll();
    }

    [Fact]
    public async Task UploadNachaFile_ReturnsUnprocessableEntity_WhenParsingFails()
    {
        await using var context = CreateContext();
        var ingestion = new Mock<IIncomingNachaIngestionAppService>();
        ingestion.Setup(x => x.IngestAsync(It.IsAny<IncomingNachaIngestionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IncomingNachaIngestionResponse
            {
                IngestionId = Guid.NewGuid(),
                OriginalFileName = "0001283.001.20260731.1.OUT.env",
                FileHash = new string('a', 64),
                CorrelationId = "failed-test",
                IngestionStatus = IncomingNachaIngestionStatus.Fallido,
                CycleResolutionStatus = IncomingNachaCycleResolutionStatus.ResueltoInferido,
                ParsingStatus = IncomingNachaParsingStatus.FallidoReprocesable,
                Errors = ["controlled"]
            });
        var controller = BuildController(ingestion.Object, context);

        var action = await controller.UploadNachaFile(
            new NachaUploadRequest
            {
                File = BuildFile("0001283.001.20260731.1.OUT.env"),
                ClearingHouseId = 7
            },
            CancellationToken.None);

        Assert.IsType<UnprocessableEntityObjectResult>(action);
    }

    [Fact]
    public async Task GetUploadedRecords_AggregatesMultiBatchFileWithoutMaterializingCollectionProduct()
    {
        await using var context = CreateContext();
        const string nachaId = "records-aggregate-test";
        context.NachaHeaders.Add(new NachaHeader
        {
            NachaID = nachaId,
            FileCreationDate = "20260731",
            FileCreationTime = "0840"
        });
        context.BatchHeaders.AddRange(Enumerable.Range(1, 40).Select(index => new BatchHeader
        {
            NachaID = nachaId,
            BatchNumber = index
        }));
        context.EntryDetails.AddRange(Enumerable.Range(1, 41).Select(index => new EntryDetail
        {
            NachaID = nachaId,
            BatchNumber = Math.Min(index, 40),
            Amount = 100m
        }));
        context.AddendaRecords.AddRange(Enumerable.Range(1, 41).Select(_ => new AddendaRecord
        {
            NachaID = nachaId
        }));
        context.FileControls.Add(new FileControl
        {
            NachaID = nachaId,
            BatchCount = 40,
            EntryAddendaCount = 82,
            TotalDebitAmount = 0m,
            TotalCreditAmount = 4100m
        });
        await context.SaveChangesAsync();
        var controller = BuildController(Mock.Of<IIncomingNachaIngestionAppService>(), context);

        var action = await controller.GetUploadedRecords(null, null, null, null, null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var records = Assert.IsAssignableFrom<IReadOnlyList<NachaUploadRecordResponse>>(ok.Value);
        var record = Assert.Single(records);
        Assert.Equal(40, record.TotalBatches);
        Assert.Equal(41, record.TotalEntries);
        Assert.Equal(41, record.TotalAddendas);
        Assert.Equal(4100m, record.TotalAmount);
        Assert.Equal(4100m, record.TotalCreditAmount);
    }

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

    private static NachaUploadController BuildController(
        IIncomingNachaIngestionAppService ingestion,
        AchDbContext context)
        => new(ingestion, context, Mock.Of<ILogger<NachaUploadController>>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

    private static FormFile BuildFile(string fileName)
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes("controlled-envelope");
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "File", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };
    }

}
