using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cfa.ACHInterbank.Tests;

/// <summary>I03: pruebas escritas para ejecutarse con dotnet en CI; no se ejecutaron aquí.</summary>
public sealed class AchPendingCycleReassignmentHandlerTests
{
    [Fact]
    public async Task DefaultConfig_DisablesEvenEnabledTask()
    {
        await using var f = await Fixture.CreateAsync(2, false);
        var output = await f.Handler.ExecuteAsync(f.Task(), CancellationToken.None);
        Assert.Contains("deshabilitado", output);
        f.Command.Verify(c => c.ReassignAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PersistedTaskDisabled_CannotBeBypassedByFlag()
    {
        await using var f = await Fixture.CreateAsync(2, true);
        var output = await f.Handler.ExecuteAsync(f.Task(enabled: false), CancellationToken.None);
        Assert.Contains("deshabilitado", output);
        f.Command.Verify(c => c.ReassignAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PausedTask_Skips()
    {
        await using var f = await Fixture.CreateAsync(1, true);
        var task = f.Task();
        task.Paused = true;
        Assert.Contains("deshabilitado", await f.Handler.ExecuteAsync(task, CancellationToken.None));
        f.Command.Verify(c => c.ReassignAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BoundedBatch_MovesOneAndReturnsCounters()
    {
        await using var f = await Fixture.CreateAsync(3, true);
        var output = await f.Handler.ExecuteAsync(f.Task(batchSize: 1), CancellationToken.None);
        Assert.Contains("trasladadas=1", output);
        f.Command.Verify(c => c.ReassignAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NonExpiredCycle_DoesNotInvokeCommand()
    {
        await using var f = await Fixture.CreateAsync(2, true, new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero));
        Assert.Contains("no existen ciclos vencidos", await f.Handler.ExecuteAsync(f.Task(), CancellationToken.None));
        f.Command.Verify(c => c.ReassignAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExportedTransaction_IsNotSelected()
    {
        await using var f = await Fixture.CreateAsync(2, true);
        f.Db.AchFileExportTransactions.Add(new AchFileExportTransaction
        {
            AchTransactionId = 101, AchCycleId = "C1", AchBatchId = 51, AchFileExportId = 99
        });
        await f.Db.SaveChangesAsync();
        var output = await f.Handler.ExecuteAsync(f.Task(), CancellationToken.None);
        Assert.Contains("evaluadas=1", output);
        f.Command.Verify(c => c.ReassignAsync(101, It.IsAny<CancellationToken>()), Times.Never);
        f.Command.Verify(c => c.ReassignAsync(102, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SkippedCandidate_DoesNotBlockAnother()
    {
        await using var f = await Fixture.CreateAsync(2, true);
        f.Command.Setup(c => c.ReassignAsync(101, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchPendingCycleReassignmentResult(101, false, "NO_ELIGIBLE_TARGET_SAME_VALUE_DATE"));
        var output = await f.Handler.ExecuteAsync(f.Task(), CancellationToken.None);
        Assert.Contains("omitidas=1", output);
        Assert.Contains("trasladadas=1", output);
    }

    [Fact]
    public async Task FailedCandidate_IsReportedAfterContinuing()
    {
        await using var f = await Fixture.CreateAsync(2, true);
        f.Command.Setup(c => c.ReassignAsync(101, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("storage failed"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Handler.ExecuteAsync(f.Task(), CancellationToken.None));
        f.Command.Verify(c => c.ReassignAsync(102, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AlreadyCancelled_CannotProceed()
    {
        await using var f = await Fixture.CreateAsync(1, true);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => f.Handler.ExecuteAsync(f.Task(), cancellation.Token));
        f.Command.Verify(c => c.ReassignAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private static readonly DateTime Monday = new(2026, 10, 26);
        public AchDbContext Db { get; }
        public Mock<IAchPendingCycleReassignmentService> Command { get; }
        public AchPendingCycleReassignmentHandler Handler { get; }

        private Fixture(AchDbContext db, Mock<IAchPendingCycleReassignmentService> command, bool enabled, DateTimeOffset now)
        {
            Db = db;
            Command = command;
            var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> { [AchPendingCycleReassignmentHandler.ExecutionFlag] = enabled.ToString() })
                .Build();
            Handler = new AchPendingCycleReassignmentHandler(db, command.Object, new OperationalCycleWindowResolver(),
                config, new FixedClock(now), Mock.Of<ILogger<AchPendingCycleReassignmentHandler>>());
        }

        public TaskDefinition Task(bool enabled = true, int batchSize = 25)
            => new()
            {
                Status = enabled ? TaskStatusEnum.Enabled : TaskStatusEnum.Disabled,
                Parameters = new List<TaskParameter>
                {
                    new() { Key = "BatchSize", Value = batchSize.ToString() }
                }
            };

        public static async Task<Fixture> CreateAsync(int count, bool enabled, DateTimeOffset? now = null)
        {
            var db = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
                .UseInMemoryDatabase($"ach-cr-i03-{Guid.NewGuid():N}").Options);
            db.ClearingHouses.Add(new ClearingHouse { Id = 1, Name = "ACH Colombia", Code = "ACHCOL", OriginCode = "001" });
            db.AchCycles.Add(new AchCycle
            {
                Id = "C1", CycleName = "Ciclo 1", ClearingHouseId = 1,
                ProcessingDate = Monday, StartTime = TimeSpan.FromHours(7),
                CutoffTime = TimeSpan.FromHours(9), EndTime = TimeSpan.FromHours(9),
                OperationalStatus = AchCycleOperationalStatus.Closed
            });
            for (var i = 0; i < count; i++)
                db.AchTransactions.Add(new AchTransaction
                {
                    Id = 101 + i, AchCycleId = "C1", AchBatchId = 51,
                    Amount = 100, Reference = $"I03-{i}", SourceAccountNumber = "1", DestinationAccountNumber = "2",
                    State = AchTransferStateEnum.Pending, Direction = AchTransactionDirection.Outgoing,
                    Origin = AchTransactionOrigin.Cfa, ClassificationStatus = AchTransactionClassificationStatus.Determined,
                    Type = TransactionTypeEnum.Credit, EffectiveEntryDate = Monday
                });
            await db.SaveChangesAsync();
            var cmd = new Mock<IAchPendingCycleReassignmentService>();
            cmd.Setup(x => x.ReassignAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int id, CancellationToken _) => new AchPendingCycleReassignmentResult(id, true, "MOVED"));
            return new Fixture(db, cmd, enabled, now ?? new DateTimeOffset(2026, 10, 26, 16, 0, 0, TimeSpan.Zero));
        }
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
