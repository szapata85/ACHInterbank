using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class ProcessBulkIngestionBatchJobTests
{
    [Fact]
    public async Task Execute_CallsProcessingService_WhenBatchIdIsValid()
    {
        var processingService = new Mock<IAchBulkBatchProcessingService>();
        var logger = new Mock<ILogger<ProcessBulkIngestionBatchJob>>();

        var job = new ProcessBulkIngestionBatchJob(processingService.Object, logger.Object);
        var batchId = Guid.NewGuid();

        var context = new Mock<IJobExecutionContext>();
        context.SetupGet(x => x.MergedJobDataMap).Returns(new JobDataMap
        {
            { "BatchId", batchId.ToString() },
            { "AttemptId", "12" }
        });
        context.SetupGet(x => x.FireInstanceId).Returns("job-fire-1");
        context.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await job.Execute(context.Object);

        processingService.Verify(x => x.ProcessBatchAsync(batchId, 12, "job-fire-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Execute_DoesNotCallProcessingService_WhenBatchIdIsInvalid()
    {
        var processingService = new Mock<IAchBulkBatchProcessingService>();
        var logger = new Mock<ILogger<ProcessBulkIngestionBatchJob>>();

        var job = new ProcessBulkIngestionBatchJob(processingService.Object, logger.Object);

        var context = new Mock<IJobExecutionContext>();
        context.SetupGet(x => x.MergedJobDataMap).Returns(new JobDataMap
        {
            { "BatchId", "invalid-guid" },
            { "AttemptId", "1" }
        });

        await job.Execute(context.Object);

        processingService.Verify(x => x.ProcessBatchAsync(It.IsAny<Guid>(), It.IsAny<long?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
