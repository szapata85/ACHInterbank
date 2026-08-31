using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cfa.ACHInterbank.Tests;

public sealed class CenitChamberResponseLifecycleTests
{
    [Theory]
    [InlineData("ACK", CenitChamberResponseType.Ack, CenitChamberResponseState.Accepted)]
    [InlineData("NACK", CenitChamberResponseType.Nack, CenitChamberResponseState.Rejected)]
    [InlineData("OPERATOR", CenitChamberResponseType.OperatorRejected, CenitChamberResponseState.OperatorRejected)]
    public async Task XmlResponse_ShouldClassifyCorrelateAndApply(
        string fixture,
        CenitChamberResponseType expectedType,
        CenitChamberResponseState expectedState)
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var service = CreateService(context);

        var result = await service.ImportAsync(Command(fixture));

        Assert.Equal(expectedType, result.ResponseType);
        Assert.Equal(expectedState, result.State);
        Assert.Equal(CenitChamberCorrelationOutcome.Matched, result.CorrelationOutcome);
        Assert.True(result.IsApplied);
        Assert.Equal(expectedState, (await context.AchFileExports.SingleAsync()).ChamberResponseState);
        Assert.Equal(expectedState == CenitChamberResponseState.Pending, (await context.AchFileExports.SingleAsync()).AllowsCenitChamberRetransmission());
        if (fixture == "OPERATOR") Assert.Equal(7301, result.RelatedTransactionId);
    }

    [Theory]
    [InlineData("Reconciliation", "NACHA-CONTENT", CenitChamberResponseType.Reconciliation, CenitChamberResponseState.Reconciliation)]
    [InlineData("NoActivity", "", CenitChamberResponseType.NoActivity, CenitChamberResponseState.NoActivity)]
    public async Task SessionOutput_ShouldClassifyAndApply(
        string messageType,
        string content,
        CenitChamberResponseType expectedType,
        CenitChamberResponseState expectedState)
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var service = CreateService(context);
        var command = new CenitChamberResponseImportCommand(
            $"SRC-{messageType}", $"{messageType}.dat", messageType, content,
            Utc(), "0001001.001.20260831.1", null, null, "CENIT-CYCLE-1");

        var result = await service.ImportAsync(command);

        Assert.Equal(expectedType, result.ResponseType);
        Assert.Equal(expectedState, result.State);
        Assert.Equal(CenitChamberCorrelationOutcome.Matched, result.CorrelationOutcome);
    }

    [Theory]
    [InlineData("ACK")]
    [InlineData("NACK")]
    public async Task DuplicateTerminalResponse_ShouldBeIdempotent(string fixture)
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var service = CreateService(context);
        var command = Command(fixture);

        var first = await service.ImportAsync(command);
        var duplicate = await service.ImportAsync(command);

        Assert.False(first.IsDuplicate);
        Assert.True(duplicate.IsDuplicate);
        Assert.Equal(first.Id, duplicate.Id);
        Assert.Equal(1, await context.CenitChamberResponses.CountAsync());
    }

    [Fact]
    public async Task AmbiguousFileCorrelation_ShouldFailClosedAndPersistOutcome()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        await AddSecondExportAsync(context);
        var service = CreateService(context);

        var result = await service.ImportAsync(Command("ACK") with { AchCycleId = null });

        Assert.Equal(CenitChamberCorrelationOutcome.Ambiguous, result.CorrelationOutcome);
        Assert.Equal("CENIT_CORRELATION_AMBIGUOUS", result.ProblemCode);
        Assert.False(result.IsApplied);
        Assert.Null(result.RelatedFileId);
        Assert.Equal(1, await context.CenitChamberResponses.CountAsync());
    }

    [Fact]
    public async Task MissingFileCorrelation_ShouldReturnControlledPersistedResult()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var service = CreateService(context);

        var result = await service.ImportAsync(Command("ACK") with
        {
            RelatedOutboundFileName = "missing-file",
            RelatedReference = "MISSING",
            AchCycleId = null
        });

        Assert.Equal(CenitChamberCorrelationOutcome.NotFound, result.CorrelationOutcome);
        Assert.Equal("CENIT_CORRELATION_NOT_FOUND", result.ProblemCode);
        Assert.False(result.IsApplied);
        Assert.Equal(1, await context.CenitChamberResponses.CountAsync());
    }

    [Fact]
    public async Task IncompatibleTerminalResponse_ShouldBeRejectedWithoutStateRegression()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var service = CreateService(context);
        await service.ImportAsync(Command("NACK"));

        var conflict = await service.ImportAsync(Command("ACK") with { SourceResponseId = "SRC-ACK-2" });

        Assert.Equal(CenitChamberCorrelationOutcome.InvalidTransition, conflict.CorrelationOutcome);
        Assert.Equal("CENIT_INVALID_LIFECYCLE_TRANSITION", conflict.ProblemCode);
        Assert.False(conflict.IsApplied);
        Assert.Equal(CenitChamberResponseState.Rejected, (await context.AchFileExports.SingleAsync()).ChamberResponseState);
    }

    [Fact]
    public async Task Persistence_ShouldSurviveReloadAndOperationalQuery()
    {
        await using var connection = await OpenAsync();
        Guid responseId;
        await using (var context = await CreateSeededContextAsync(connection))
        {
            responseId = (await CreateService(context).ImportAsync(Command("ACK"))).Id;
        }

        await using var reloaded = NewContext(connection);
        var service = CreateService(reloaded);
        var detail = await service.GetAsync(responseId);
        var page = await service.ListAsync();

        Assert.NotNull(detail);
        Assert.Equal(CenitChamberResponseState.Accepted, detail.State);
        Assert.Equal("0001001.001.20260831.1", detail.RelatedFileName);
        Assert.Single(page.Items);
    }

    [Fact]
    public async Task Api_ShouldExposeImportedStateAndControlledProblemDetails()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var controller = new CenitOperationsController(context, CreateService(context));

        var created = Assert.IsType<CreatedAtActionResult>(await controller.ImportChamberResponseAsync(Command("ACK")));
        var imported = Assert.IsType<CenitChamberResponseResult>(created.Value);
        var detail = Assert.IsType<OkObjectResult>(await controller.GetChamberResponseAsync(imported.Id));
        Assert.Equal(CenitChamberResponseState.Accepted, Assert.IsType<CenitChamberResponseResult>(detail.Value).State);

        var invalid = await controller.ImportChamberResponseAsync(Command("UNKNOWN") with
        {
            SourceResponseId = "SRC-UNKNOWN",
            MessageType = "Unsupported"
        });
        var problem = Assert.IsType<ObjectResult>(invalid);
        Assert.Equal(422, problem.StatusCode);
        Assert.Equal("CENIT_RESPONSE_NOT_RECOGNIZED", Assert.IsType<ProblemDetails>(problem.Value).Title);
    }

    private static CenitChamberResponseImportCommand Command(string fixture)
        => new(
            $"SRC-{fixture}",
            $"{fixture}.xml",
            "XML",
            Xml(fixture),
            Utc(),
            "0001001.001.20260831.1",
            null,
            fixture == "OPERATOR" ? "000010070007301" : null,
            "CENIT-CYCLE-1");

    private static string Xml(string fixture) => fixture switch
    {
        "ACK" => "<FileAck><GroupHeader><Status>ACK</Status></GroupHeader><AdditionalRefs><RelatedRef>REF-1</RelatedRef></AdditionalRefs></FileAck>",
        "NACK" => "<FileNack><AdditionalRefs><RelatedRef>REF-1</RelatedRef></AdditionalRefs><FileErrorHandling><Status>NACK</Status><ErrorCode>ERR_FILE</ErrorCode><AdditionalDesc>Archivo rechazado</AdditionalDesc></FileErrorHandling></FileNack>",
        "OPERATOR" => "<FileNack><AdditionalRefs><RelatedRef>REF-1</RelatedRef></AdditionalRefs><FileErrorHandling><Status>NACK</Status><BatchNo>1</BatchNo><TraceNo>000010070007301</TraceNo><ErrorCode>ERR_TRACE</ErrorCode><AdditionalDesc>Transacción rechazada</AdditionalDesc></FileErrorHandling></FileNack>",
        _ => "<Unknown />"
    };

    private static CenitChamberResponseService CreateService(AchDbContext context)
        => new(context, NullLogger<CenitChamberResponseService>.Instance);

    private static async Task<SqliteConnection> OpenAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    private static AchDbContext NewContext(SqliteConnection connection)
        => new(new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options) { AuditEnabled = false };

    private static async Task<AchDbContext> CreateSeededContextAsync(SqliteConnection connection)
    {
        var context = NewContext(connection);
        await context.Database.EnsureCreatedAsync();
        var config = new ClearingHouseConfig { ClearingHouseId = 9901, HolidayStrategy = "Colombian" };
        context.ClearingHouseConfigs.Add(config);
        await context.SaveChangesAsync();
        var clearingHouse = new ClearingHouse
        {
            Name = "CENIT",
            Code = "CENIT",
            OriginCode = "000101006",
            ClearingHouseId = config.Id
        };
        context.ClearingHouses.Add(clearingHouse);
        var source = Institution(7101, "007");
        var destination = Institution(7102, "001");
        context.FinancialInstitutions.AddRange(source, destination);
        await context.SaveChangesAsync();

        var cycle = Cycle("CENIT-CYCLE-1", clearingHouse.Id);
        var batch = Batch(7201, cycle.Id);
        context.AchCycles.Add(cycle);
        context.AchBatches.Add(batch);
        var transaction = Transaction(7301, cycle.Id, batch.Id, source.Id, destination.Id);
        context.AchTransactions.Add(transaction);
        context.AchFileExports.Add(new AchFileExport
        {
            AchCycleId = cycle.Id,
            ClearingHouseId = clearingHouse.Id,
            ExportKind = "NACHA",
            FileName = "0001001.001.20260831.1",
            TotalRecords = 10,
            TotalTransactions = 1,
            GeneratedAtUtc = Utc(),
            LifecycleStatus = AchFileExportLifecycleStatus.Generated,
            TransmissionReference = "REF-1",
            Transactions =
            [
                new AchFileExportTransaction
                {
                    AchTransactionId = transaction.Id,
                    AchCycleId = cycle.Id,
                    AchBatchId = batch.Id,
                    FileSequence = 1,
                    TraceNumber = transaction.TraceNumber,
                    Amount = transaction.Amount,
                    IncludedAtUtc = Utc()
                }
            ]
        });
        await context.SaveChangesAsync();
        return context;
    }

    private static async Task AddSecondExportAsync(AchDbContext context)
    {
        var clearingHouseId = await context.ClearingHouses.Where(x => x.Code == "CENIT").Select(x => x.Id).SingleAsync();
        var cycle = Cycle("CENIT-CYCLE-2", clearingHouseId);
        context.AchCycles.Add(cycle);
        context.AchFileExports.Add(new AchFileExport
        {
            AchCycleId = cycle.Id,
            ClearingHouseId = clearingHouseId,
            ExportKind = "NACHA",
            FileName = "0001001.001.20260831.1",
            TotalRecords = 10,
            TotalTransactions = 0,
            GeneratedAtUtc = Utc(),
            LifecycleStatus = AchFileExportLifecycleStatus.Generated,
            TransmissionReference = "REF-1"
        });
        await context.SaveChangesAsync();
    }

    private static FinancialInstitution Institution(int id, string transit)
    {
        var entity = new FinancialInstitution { Id = id, Name = $"FI-{id}", RoutingNumber = "00001", TransitCode = transit };
        entity.CalculateCheckDigit();
        return entity;
    }

    private static AchCycle Cycle(string id, int clearingHouseId) => new()
    {
        Id = id,
        CycleName = id,
        ProcessingDate = new DateTime(2026, 8, 31),
        StartTime = TimeSpan.Zero,
        EndTime = new TimeSpan(23, 59, 0),
        CutoffTime = new TimeSpan(23, 59, 0),
        ClearingHouseId = clearingHouseId
    };

    private static AchBatch Batch(int id, string cycleId) => new()
    {
        Id = id,
        AchCycleId = cycleId,
        BatchSequenceNumber = 1,
        CompanyEntryDescriptionId = 1,
        EffectiveEntryDate = new DateTime(2026, 8, 31)
    };

    private static AchTransaction Transaction(int id, string cycleId, int batchId, int sourceId, int destinationId) => new()
    {
        Id = id,
        TransactionExternalId = $"TX-{id}",
        Reference = $"REF-{id}",
        Amount = 100m,
        Type = TransactionTypeEnum.Credit,
        TransactionCode = "22",
        TraceNumber = "000010070007301",
        TraceSequenceNumber = id,
        EffectiveEntryDate = new DateTime(2026, 8, 31),
        State = AchTransferStateEnum.Pending,
        StateChangedAtUtc = Utc(),
        SourceAccountNumber = "MASKED-SOURCE",
        DestinationAccountNumber = "MASKED-DESTINATION",
        SourceInstitutionId = sourceId,
        DestinationInstitutionId = destinationId,
        AchCycleId = cycleId,
        AchBatchId = batchId,
        CompanyEntryDescriptionId = 1
    };

    private static DateTime Utc() => new(2026, 8, 31, 12, 0, 0, DateTimeKind.Utc);
}
