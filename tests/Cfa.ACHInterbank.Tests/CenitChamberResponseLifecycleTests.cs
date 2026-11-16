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
using System.Text.Json;

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
        var expectedFileState = fixture == "OPERATOR" ? CenitChamberResponseState.Pending : expectedState;
        Assert.Equal(expectedFileState, (await context.AchFileExports.SingleAsync()).ChamberResponseState);
        Assert.Equal(expectedFileState == CenitChamberResponseState.Pending,
            (await context.AchFileExports.SingleAsync()).AllowsCenitChamberRetransmission());
        if (fixture == "OPERATOR")
        {
            Assert.Equal(7301, result.RelatedTransactionId);
            Assert.Equal(AchTransferStateEnum.ReturnedByOperator, (await context.AchTransactions.SingleAsync()).State);
            Assert.Single(await context.AchTransactionStateEvents.ToListAsync());
        }
    }

    [Fact]
    public async Task I06_ProvisionalLiquidityShortfall_OnlyOfficialTransactionNackCreatesReturn_AndReplayIsIdempotent()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var transaction = await context.AchTransactions.SingleAsync();
        var cycle = await context.AchCycles.SingleAsync();
        var execution = new CenitCycleExecution
        {
            AchCycleId = cycle.Id,
            Status = "AwaitingOfficialResult"
        };
        context.CenitCycleExecutions.Add(execution);
        await context.SaveChangesAsync();
        context.LiquidityOptimizationDecisions.Add(new LiquidityOptimizationDecision
        {
            CenitCycleExecutionId = execution.Id,
            AchTransactionId = transaction.Id,
            AchBatchId = transaction.AchBatchId,
            ValueDate = transaction.EffectiveEntryDate.Date,
            ClearingHouseId = cycle.ClearingHouseId,
            ClearingHouseCode = "CENIT",
            DecisionType = "SimulatedInsufficientLiquidity",
            DecisionReason = "I06 fixture: simulated only",
            LiquidityModelUsed = "Simulated",
            FromCycleId = cycle.Id
        });
        await context.SaveChangesAsync();
        Assert.Equal(AchTransferStateEnum.Pending, transaction.State);
        Assert.Empty(await context.AchTransactionStateEvents.ToListAsync());
        Assert.Empty(transaction.ReturnReasonCode);

        var service = CreateService(context);
        var imported = await service.ImportAsync(Command("OPERATOR"));
        Assert.True(imported.IsApplied);
        Assert.Equal(CenitChamberCorrelationOutcome.Matched, imported.CorrelationOutcome);
        Assert.Equal(AchTransferStateEnum.ReturnedByOperator, (await context.AchTransactions.SingleAsync()).State);
        var official = Assert.Single(await context.AchTransactionStateEvents.ToListAsync());
        Assert.Equal(AchStateEventSourceEnum.Operator, official.Source);
        Assert.Equal("ERR_TRACE", official.ReasonCode);
        var replay = await service.ImportAsync(Command("OPERATOR"));
        Assert.True(replay.IsDuplicate);
        Assert.Single(await context.AchTransactionStateEvents.ToListAsync());
        Assert.Single(await context.LiquidityOptimizationDecisions.ToListAsync());
        Assert.Equal("AwaitingOfficialResult", (await context.CenitCycleExecutions.SingleAsync()).Status);
    }

    [Theory]
    [InlineData(AchTransferStateEnum.Pending)]
    [InlineData(AchTransferStateEnum.AppliedTacitly)]
    public async Task I10_PrenoteWithoutCurrentAttempt_IsFlaggedForReview_NoDirectStateMutation(
        AchTransferStateEnum previous)
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var original = await context.AchTransactions.SingleAsync();
        original.IsPrenotification = true;
        original.Amount = 0m;
        original.State = previous;
        await context.SaveChangesAsync();

        var result = await CreateService(context).ImportAsync(Command("OPERATOR"));

        Assert.False(result.IsApplied);
        Assert.Equal(CenitChamberCorrelationOutcome.InvalidTransition, result.CorrelationOutcome);
        Assert.Equal("CENIT_PRENOTE_RETURN_REQUIRES_MANUAL_REVIEW", result.ProblemCode);
        Assert.Equal(previous, (await context.AchTransactions.SingleAsync()).State);
        Assert.Empty(await context.AchTransactionStateEvents.ToListAsync());
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
            Utc(), null, null, null, "CENIT-CYCLE-1");

        var result = await service.ImportAsync(command);

        Assert.Equal(expectedType, result.ResponseType);
        Assert.Equal(expectedState, result.State);
        Assert.Equal(CenitChamberCorrelationOutcome.Matched, result.CorrelationOutcome);
        Assert.Null(result.RelatedFileId);
        Assert.Equal("CENIT-CYCLE-1", result.AchCycleId);
        Assert.True(result.IsApplied);
        Assert.Equal(CenitChamberResponseState.Pending, (await context.AchFileExports.SingleAsync()).ChamberResponseState);
    }

    [Fact]
    public async Task OfficialAckFixture_ShouldPreserveNamespaceHeaderAndReferences()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var content = await File.ReadAllTextAsync(Fixture("official-file-ack.xml"));

        var result = await CreateService(context).ImportAsync(Command("ACK") with
        {
            Content = content,
            TransactionTraceNumber = null
        });

        Assert.Equal(CenitChamberResponseType.Ack, result.ResponseType);
        Assert.Equal("urn:xs:FileAck", result.XmlNamespace);
        Assert.Equal("ACK-20260831-01", result.MessageGroupId);
        Assert.Equal("ACK", result.MessageStatus);
        Assert.Equal(new DateTime(2026, 8, 31, 17, 5, 0, DateTimeKind.Utc), result.MessageCreatedAtUtc);
        Assert.Equal("0001001", result.OriginatingSender);
        Assert.Equal("REF-1", result.RelatedReference);
        Assert.Equal(CenitChamberCorrelationOutcome.Matched, result.CorrelationOutcome);
    }

    [Fact]
    public async Task ExplicitFileName_ShouldCorrelateWhenXmlReferenceIsNotTheTransmissionReference()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var command = Command("ACK") with
        {
            Content = "<FileAck><GroupHeader><Status>ACK</Status></GroupHeader><AdditionalRefs><RelatedRef>0001001.001.20260831.1</RelatedRef></AdditionalRefs></FileAck>"
        };

        var result = await CreateService(context).ImportAsync(command);

        Assert.Equal(CenitChamberCorrelationOutcome.Matched, result.CorrelationOutcome);
        Assert.NotNull(result.RelatedFileId);
        Assert.True(result.IsApplied);
    }

    [Fact]
    public async Task OfficialFileNackFixture_WithoutLocator_ShouldBeFileLevelNack()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var content = await File.ReadAllTextAsync(Fixture("official-file-nack.xml"));

        var result = await CreateService(context).ImportAsync(Command("NACK") with { Content = content });

        Assert.Equal(CenitChamberResponseType.Nack, result.ResponseType);
        Assert.Equal(CenitChamberResponseState.Rejected, result.State);
        Assert.Equal("ERR_FILENAME_EXTENSION", result.ReasonCode);
        Assert.Null(result.RelatedTransactionId);
        Assert.Equal("urn:xs:FileNack", result.XmlNamespace);
        Assert.Equal(AchTransferStateEnum.Pending, (await context.AchTransactions.SingleAsync()).State);
        Assert.Empty(await context.AchTransactionStateEvents.ToListAsync());
    }

    [Fact]
    public async Task MultiErrorFileNack_ShouldPersistAndCorrelateEveryOperatorError()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var export = await context.AchFileExports.Include(x => x.Transactions).SingleAsync();
        var first = await context.AchTransactions.SingleAsync();
        var second = Transaction(7302, first.AchCycleId, first.AchBatchId, first.SourceInstitutionId, first.DestinationInstitutionId,
            "000010070007302");
        context.AchTransactions.Add(second);
        export.TotalTransactions = 2;
        export.Transactions.Add(new AchFileExportTransaction
        {
            AchTransactionId = second.Id,
            AchCycleId = second.AchCycleId,
            AchBatchId = second.AchBatchId,
            FileSequence = 2,
            TraceNumber = second.TraceNumber,
            Amount = second.Amount,
            IncludedAtUtc = Utc()
        });
        await context.SaveChangesAsync();
        var content = await File.ReadAllTextAsync(Fixture("official-multi-operator-nack.xml"));

        var result = await CreateService(context).ImportAsync(Command("OPERATOR") with
        {
            Content = content,
            TransactionTraceNumber = null
        });

        var responses = await context.CenitChamberResponses.OrderBy(x => x.ItemSequence).ToListAsync();
        Assert.Equal(2, responses.Count);
        Assert.All(responses, x => Assert.Equal(2, x.ItemCount));
        Assert.Equal([1, 2], responses.Select(x => x.ItemSequence));
        Assert.Equal([7301, 7302], responses.Select(x => x.AchTransactionId));
        Assert.All(responses, x => Assert.Equal("ERR_TRACE_NO_INV", x.ReasonCode));
        Assert.Equal(2, result.ItemCount);
        Assert.Equal(CenitChamberResponseType.OperatorRejected, result.ResponseType);
        Assert.Equal(2, await context.AchTransactionStateEvents.CountAsync());
        Assert.Equal(CenitChamberResponseState.Pending, export.ChamberResponseState);
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
        var json = JsonSerializer.Serialize(imported);
        Assert.Contains("\"ResponseType\":\"Ack\"", json, StringComparison.Ordinal);
        Assert.Contains("\"State\":\"Accepted\"", json, StringComparison.Ordinal);
        Assert.Contains("\"CorrelationOutcome\":\"Matched\"", json, StringComparison.Ordinal);
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

    [Fact]
    public async Task IndividualNack_ShouldRejectOnlyMatchingTransaction_AndPreserveFileState()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var original = await context.AchTransactions.SingleAsync();
        var second = Transaction(7302, original.AchCycleId, original.AchBatchId,
            original.SourceInstitutionId, original.DestinationInstitutionId, "000010070007302");
        context.AchTransactions.Add(second);
        var export = await context.AchFileExports.Include(x => x.Transactions).SingleAsync();
        export.TotalTransactions = 2;
        export.Transactions.Add(new AchFileExportTransaction
        {
            AchTransactionId = second.Id, AchCycleId = second.AchCycleId,
            AchBatchId = second.AchBatchId, FileSequence = 2,
            TraceNumber = second.TraceNumber, Amount = second.Amount,
            IncludedAtUtc = Utc()
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var first = await service.ImportAsync(Command("OPERATOR"));
        Assert.True(first.IsApplied);
        Assert.Equal(AchTransferStateEnum.ReturnedByOperator, original.State);
        Assert.Equal(AchTransferStateEnum.Pending, second.State);
        Assert.Equal(CenitChamberResponseState.Pending, export.ChamberResponseState);
        var audit = Assert.Single(await context.AchTransactionStateEvents.ToListAsync());
        Assert.Equal(7301, audit.AchTransactionId);
        Assert.Equal(AchStateEventSourceEnum.Operator, audit.Source);
        Assert.Equal("ERR_TRACE", audit.ReasonCode);

        var repeated = await service.ImportAsync(Command("OPERATOR"));
        Assert.True(repeated.IsDuplicate);
        Assert.Single(await context.AchTransactionStateEvents.ToListAsync());
        var ack = await service.ImportAsync(Command("ACK"));
        Assert.True(ack.IsApplied);
        Assert.Equal(CenitChamberResponseState.Accepted, export.ChamberResponseState);
        Assert.Equal(AchTransferStateEnum.Pending, second.State);
    }

    [Fact]
    public async Task IndividualNackAfterTechnicalAck_ShouldStillRejectOnlyTheTransaction()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var service = CreateService(context);
        await service.ImportAsync(Command("ACK"));
        var item = await service.ImportAsync(Command("OPERATOR"));
        Assert.True(item.IsApplied);
        Assert.Equal(AchTransferStateEnum.ReturnedByOperator,
            (await context.AchTransactions.SingleAsync()).State);
        Assert.Equal(CenitChamberResponseState.Accepted,
            (await context.AchFileExports.SingleAsync()).ChamberResponseState);
        Assert.Single(await context.AchTransactionStateEvents.ToListAsync());
    }

    [Fact]
    public async Task BatchOnlyNack_ShouldNotRejectAnUnidentifiedTransaction()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var cmd = Command("OPERATOR") with
        {
            Content = Xml("OPERATOR").Replace("<TraceNo>000010070007301</TraceNo>", ""),
        };
        var result = await CreateService(context).ImportAsync(cmd);
        Assert.Equal("CENIT_OPERATOR_REJECTION_TRACE_REQUIRED", result.ProblemCode);
        Assert.False(result.IsApplied);
        Assert.Equal(AchTransferStateEnum.Pending, (await context.AchTransactions.SingleAsync()).State);
        Assert.Equal(CenitChamberResponseState.Pending, (await context.AchFileExports.SingleAsync()).ChamberResponseState);
    }

    [Fact]
    public async Task ItemNackWithUnmatchedTrace_ShouldNotAlterFileOrTransaction()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var unmatched = Command("OPERATOR") with
        {
            Content = Xml("OPERATOR").Replace("000010070007301", "000010070009999"),
            TransactionTraceNumber = "000010070007301" // No debe sustituir TraceNo oficial.
        };
        var result = await CreateService(context).ImportAsync(unmatched);
        Assert.Equal(CenitChamberCorrelationOutcome.TransactionNotFound, result.CorrelationOutcome);
        Assert.False(result.IsApplied);
        Assert.Equal(CenitChamberResponseState.Pending, (await context.AchFileExports.SingleAsync()).ChamberResponseState);
        Assert.Equal(AchTransferStateEnum.Pending, (await context.AchTransactions.SingleAsync()).State);
        Assert.Empty(await context.AchTransactionStateEvents.ToListAsync());
    }

    [Fact]
    public async Task Adv280_ShouldPersistEachCodeAndAmount_AsEvidenceOnly_AndBeIdempotent()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var command = new CenitChamberResponseImportCommand("ADV-1", "adv.ach", "ADV",
            AdvContent("81", "82"), Utc(), AchCycleId: "CENIT-CYCLE-1");
        var service = CreateService(context);
        var result = await service.ImportAsync(command);
        Assert.Equal(2, result.ItemCount);
        Assert.Equal(CenitChamberResponseType.Reconciliation, result.ResponseType);
        Assert.True(result.IsApplied);
        var responses = await context.CenitChamberResponses.OrderBy(x => x.ItemSequence).ToListAsync();
        Assert.Equal(["81", "82"], responses.Select(x => x.ReasonCode));
        Assert.All(responses, x => Assert.Contains("amountCOP=123.45", x.Description));
        Assert.All(responses, x => Assert.Equal("ADV-280", x.MessageStatus));
        Assert.Equal(CenitChamberResponseState.Pending, (await context.AchFileExports.SingleAsync()).ChamberResponseState);
        Assert.Equal(AchTransferStateEnum.Pending, (await context.AchTransactions.SingleAsync()).State);
        Assert.Empty(await context.AchTransactionStateEvents.ToListAsync());
        Assert.True((await service.ImportAsync(command)).IsDuplicate);
        Assert.Equal(2, await context.CenitChamberResponses.CountAsync());
    }

    [Fact]
    public async Task LegacyReconciliationEnvelope_ShouldInterpretPhysicalAdv280()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var result = await CreateService(context).ImportAsync(
            new CenitChamberResponseImportCommand("ADV-LEGACY", "adv.dat", "Reconciliation",
                AdvContent("87"), Utc(), AchCycleId: "CENIT-CYCLE-1"));
        Assert.Equal("87", result.ReasonCode);
        Assert.Contains("amountCOP=123.45", result.Description);
        Assert.Equal("ADV-280", result.MessageStatus);
        Assert.True(result.IsApplied);
    }

    [Theory]
    [InlineData("PPD", "280", "CENIT_ADV_BATCH_PROFILE_INVALID")]
    [InlineData("ADV", "220", "CENIT_ADV_BATCH_PROFILE_INVALID")]
    public async Task AdvWithWrongServiceOrClass_ShouldFailClosed(string serviceCode, string classCode, string problem)
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var content = AdvContent("81").Replace("280", classCode).Replace("ADV", serviceCode);
        var response = await CreateService(context).ImportAsync(
            new CenitChamberResponseImportCommand("ADV-BAD", "bad.ach", "ADV", content,
                Utc(), AchCycleId: "CENIT-CYCLE-1"));
        Assert.Equal(problem, response.ProblemCode);
        Assert.False(response.IsApplied);
        Assert.Equal(AchTransferStateEnum.Pending, (await context.AchTransactions.SingleAsync()).State);
    }

    [Fact]
    public async Task AdvWrongBatchTotals_ShouldNotApply()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var valid = AdvContent("81");
        var records = valid.Split('\n');
        var controlIndex = Array.FindIndex(records, x => x[0] == '8');
        records[controlIndex] = records[controlIndex].Remove(20, 18).Insert(20, "000000000000000000");
        var result = await CreateService(context).ImportAsync(
            new CenitChamberResponseImportCommand("ADV-WRONGTOTAL", "bad.ach", "ADV",
                string.Join("\n", records), Utc(), AchCycleId: "CENIT-CYCLE-1"));
        Assert.Equal("CENIT_ADV_BATCH_CONTROL_INVALID", result.ProblemCode);
        Assert.False(result.IsApplied);
        Assert.Empty(await context.AchTransactionStateEvents.ToListAsync());
    }

    [Fact]
    public async Task AdvWithoutCycle_ShouldRetainEvidenceButNotApply()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var response = await CreateService(context).ImportAsync(
            new CenitChamberResponseImportCommand("ADV-NOCYCLE", "adv.ach", "ADV",
                AdvContent("88"), Utc()));
        Assert.Equal("CENIT_SESSION_IDENTIFIER_REQUIRED", response.ProblemCode);
        Assert.False(response.IsApplied);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task F07_OfficialAckIndividualNackAndAdv_KeepFileTransactionAndReconciliationIndependent(bool ackFirst)
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var service = CreateService(context);
        if (ackFirst)
        {
            Assert.True((await service.ImportAsync(Command("ACK"))).IsApplied);
        }

        var individual = await service.ImportAsync(Command("OPERATOR"));
        Assert.True(individual.IsApplied);
        Assert.Equal(AchTransferStateEnum.ReturnedByOperator,
            (await context.AchTransactions.SingleAsync()).State);
        Assert.Single(await context.AchTransactionStateEvents.ToListAsync());
        if (!ackFirst)
        {
            Assert.True((await service.ImportAsync(Command("ACK"))).IsApplied);
        }

        var advance = new CenitChamberResponseImportCommand(
            "F07-ADV", "adv-f07.ach", "ADV", AdvContent("81", "82"),
            Utc(), AchCycleId: "CENIT-CYCLE-1");
        var reconciliation = await service.ImportAsync(advance);
        Assert.True(reconciliation.IsApplied);
        Assert.Equal(2, reconciliation.ItemCount);
        Assert.Equal(CenitChamberResponseState.Accepted,
            (await context.AchFileExports.SingleAsync()).ChamberResponseState);
        Assert.Equal(AchTransferStateEnum.ReturnedByOperator,
            (await context.AchTransactions.SingleAsync()).State);
        Assert.Single(await context.AchTransactionStateEvents.ToListAsync());
        Assert.True((await service.ImportAsync(advance)).IsDuplicate);
        Assert.True((await service.ImportAsync(Command("OPERATOR"))).IsDuplicate);
        Assert.Equal(4, await context.CenitChamberResponses.CountAsync());
        Assert.Single(await context.AchTransactionStateEvents.ToListAsync());
    }

    // A physical ADV is 106 positions per record, packed without CR/LF, and padded
    // with all-9 records to complete a ten-record block (NACHA-M §§3, 7.3.3).
    private static string AdvContent(params string[] codes)
    {
        static string Record(char type, params (int start, string content)[] fields)
        {
            var chars = new string(' ', 106).ToCharArray();
            chars[0] = type;
            foreach (var (start, content) in fields) content.CopyTo(0, chars, start - 1, content.Length);
            return new string(chars);
        }
        var records = new List<string>
        {
            Record('1', (37, "106"), (40, "10")),
            Record('5', (2, "280"), (51, "ADV"), (72, "20260831"), (92, "0000001"))
        };
        foreach (var (code, i) in codes.Select((code, i) => (code, i)))
        {
            records.Add(Record('6', (2, code), (4, "00010010"), (28, "000000000000012345"),
                (55, "0101A"), (85, "0"), (97, (i + 1).ToString("D4"))));
        }
        var debitCount = codes.Count(code => code is "81" or "84" or "86" or "88");
        var creditCount = codes.Length - debitCount;
        var routeSum = (10010L * codes.Length).ToString("D10");
        records.Add(Record('8', (2, "280"), (5, codes.Length.ToString("D6")),
            (11, routeSum),
            (21, (12345L * debitCount).ToString("D18")),
            (39, (12345L * creditCount).ToString("D18")), (100, "0000001")));
        var paddedCount = ((records.Count + 1 + 9) / 10) * 10;
        records.Add(Record('9', (2, "000001"), (8, (paddedCount / 10).ToString("D6")),
            (14, codes.Length.ToString("D8")), (22, routeSum),
            (32, (12345L * debitCount).ToString("D18")),
            (50, (12345L * creditCount).ToString("D18"))));
        while (records.Count < paddedCount) records.Add(new string('9', 106));
        // Existing gateway tests exercise the historic line-delimited representation.
        return string.Join("\n", records);
    }

    private static string PhysicalAdv(params string[] codes)
        => AdvContent(codes).Replace("\n", string.Empty, StringComparison.Ordinal);

    [Fact]
    public async Task PT23_PhysicalAdvWithoutLineEndings_AllEightCodes_PersistsEvidenceOnly_AndReplayIsIdempotent()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var input = PhysicalAdv("81", "82", "83", "84", "85", "86", "87", "88");
        Assert.Equal(0, input.Length % 106);
        Assert.DoesNotContain('\n', input);
        Assert.DoesNotContain('\r', input);
        var command = new CenitChamberResponseImportCommand("PT23-ADV", "adv.bin", "ADV", input,
            Utc(), AchCycleId: "CENIT-CYCLE-1");
        var service = CreateService(context);
        var result = await service.ImportAsync(command);
        Assert.True(result.IsApplied);
        Assert.Equal(8, result.ItemCount);
        var items = await context.CenitChamberResponses.OrderBy(x => x.ItemSequence).ToListAsync();
        Assert.Equal(new[] {"81", "82", "83", "84", "85", "86", "87", "88"}, items.Select(x => x.ReasonCode));
        Assert.All(items, x => Assert.Contains("amountCOP=123.45", x.Description));
        Assert.True((await service.ImportAsync(command)).IsDuplicate);
        Assert.Equal(8, await context.CenitChamberResponses.CountAsync());
        Assert.Equal(AchTransferStateEnum.Pending, (await context.AchTransactions.SingleAsync()).State);
        Assert.Empty(await context.AchTransactionStateEvents.ToListAsync());
    }

    [Fact]
    public async Task PT23_PhysicalAdvUnderLegacyReconciliationEnvelope_IsRecognized()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var result = await CreateService(context).ImportAsync(
            new CenitChamberResponseImportCommand("PT23-RECON", "adv.bin", "Reconciliation",
                PhysicalAdv("85"), Utc(), AchCycleId: "CENIT-CYCLE-1"));
        Assert.True(result.IsApplied);
        Assert.Equal("85", result.ReasonCode);
        Assert.Equal("ADV-280", result.MessageStatus);
    }

    [Fact]
    public async Task PT23_MalformedAdvUnderReconciliation_DoesNotBecomeOpaqueAppliedEvidence()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var physical = PhysicalAdv("81");
        physical = physical.Remove(107, 3).Insert(107, "220"); // batch class, not the file header
        var result = await CreateService(context).ImportAsync(
            new CenitChamberResponseImportCommand("PT23-BAD-RECON", "bad.adv", "Reconciliation",
                physical, Utc(), AchCycleId: "CENIT-CYCLE-1"));
        Assert.Equal("CENIT_ADV_BATCH_PROFILE_INVALID", result.ProblemCode);
        Assert.False(result.IsApplied);
        Assert.Empty(await context.AchTransactionStateEvents.ToListAsync());
    }

    [Theory]
    [InlineData("truncated", "CENIT_ADV_RECORD_LENGTH_INVALID")]
    [InlineData("wrong-start", "CENIT_ADV_FILE_HEADER_INVALID")]
    [InlineData("wrong-order", "CENIT_ADV_BATCH_CONTROL_REQUIRED")]
    [InlineData("invalid-character", "CENIT_ADV_RECORD_LENGTH_INVALID")]
    [InlineData("invalid-amount", "CENIT_ADV_DETAIL_INVALID")]
    [InlineData("wrong-batch-route", "CENIT_ADV_BATCH_CONTROL_INVALID")]
    [InlineData("wrong-batch-count", "CENIT_ADV_BATCH_CONTROL_INVALID")]
    [InlineData("adenda-flag", "CENIT_ADV_DETAIL_INVALID")]
    [InlineData("wrong-file-count", "CENIT_ADV_FILE_CONTROL_INVALID")]
    [InlineData("wrong-file-debit", "CENIT_ADV_FILE_CONTROL_INVALID")]
    [InlineData("wrong-file-route", "CENIT_ADV_FILE_CONTROL_INVALID")]
    [InlineData("missing-padding", "CENIT_ADV_FILE_STRUCTURE_INVALID")]
    [InlineData("changed-padding", "CENIT_ADV_FILE_STRUCTURE_INVALID")]
    public async Task PT23_MalformedPhysicalAdv_FailsClosedAndDoesNotPost(string variant, string expected)
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var records = Enumerable.Range(0, PhysicalAdv("81").Length / 106)
            .Select(i => PhysicalAdv("81").Substring(i * 106, 106)).ToArray();
        var fileControlIndex = Array.FindIndex(records, x => x[0] == '9');
        var batchControlIndex = Array.FindIndex(records, x => x[0] == '8');
        var detailIndex = Array.FindIndex(records, x => x[0] == '6');
        switch (variant)
        {
            case "wrong-start": records[0] = "6" + records[0][1..]; break;
            case "wrong-order": (records[batchControlIndex], records[fileControlIndex]) = (records[fileControlIndex], records[batchControlIndex]); break;
            case "invalid-character": records[detailIndex] = records[detailIndex].Remove(60, 1).Insert(60, "Ñ"); break;
            case "invalid-amount": records[detailIndex] = records[detailIndex].Remove(27, 1).Insert(27, "X"); break;
            case "wrong-batch-route": records[batchControlIndex] = records[batchControlIndex].Remove(10, 10).Insert(10, new string('0', 10)); break;
            case "wrong-batch-count": records[batchControlIndex] = records[batchControlIndex].Remove(4, 6).Insert(4, "000002"); break;
            case "adenda-flag": records[detailIndex] = records[detailIndex].Remove(84, 1).Insert(84, "1"); break;
            case "wrong-file-count": records[fileControlIndex] = records[fileControlIndex].Remove(13, 8).Insert(13, "00000002"); break;
            case "wrong-file-debit": records[fileControlIndex] = records[fileControlIndex].Remove(31, 18).Insert(31, new string('0', 18)); break;
            case "wrong-file-route": records[fileControlIndex] = records[fileControlIndex].Remove(21, 10).Insert(21, new string('0', 10)); break;
            case "changed-padding": records[^1] = new string('8', 106); break;
            case "missing-padding": records = records[..^1]; break;
        }
        var physical = string.Concat(records);
        if (variant == "truncated") physical = physical[..^1];
        var result = await CreateService(context).ImportAsync(
            new CenitChamberResponseImportCommand("PT23-BAD-" + variant, "bad.adv", "ADV",
                physical, Utc(), AchCycleId: "CENIT-CYCLE-1"));
        Assert.Equal(expected, result.ProblemCode);
        Assert.False(result.IsApplied);
        Assert.Equal(AchTransferStateEnum.Pending, (await context.AchTransactions.SingleAsync()).State);
        Assert.Empty(await context.AchTransactionStateEvents.ToListAsync());
    }

    // I13 / PT-14: a technical file ACK must not hide an individual NACK.
    // The other exported transfers remain unchanged and the file remains accepted.
    [Fact]
    public async Task I13_PT14_AckAndNackAcrossThreeTransactions_PreservesUnrejectedItems()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var original = await context.AchTransactions.SingleAsync();
        var export = await context.AchFileExports.Include(x => x.Transactions).SingleAsync();
        foreach (var (id, trace, sequence) in new[]
        {
            (7302, "000010070007302", 2),
            (7303, "000010070007303", 3)
        })
        {
            var transfer = Transaction(id, original.AchCycleId, original.AchBatchId,
                original.SourceInstitutionId, original.DestinationInstitutionId, trace);
            context.AchTransactions.Add(transfer);
            export.Transactions.Add(new AchFileExportTransaction
            {
                AchTransactionId = transfer.Id, AchCycleId = transfer.AchCycleId,
                AchBatchId = transfer.AchBatchId, FileSequence = sequence,
                TraceNumber = transfer.TraceNumber, Amount = transfer.Amount,
                IncludedAtUtc = Utc()
            });
        }
        export.TotalTransactions = 3;
        await context.SaveChangesAsync();

        var service = CreateService(context);
        Assert.True((await service.ImportAsync(Command("ACK"))).IsApplied);
        var rejected = await service.ImportAsync(Command("OPERATOR") with
        {
            SourceResponseId = "I13-MIXED-1",
            Content = I13NackItems(("000010070007302", "ERR_ACCOUNT")),
            TransactionTraceNumber = null
        });
        Assert.True(rejected.IsApplied);
        Assert.Equal(7302, rejected.RelatedTransactionId);
        Assert.Equal(CenitChamberResponseState.Accepted, export.ChamberResponseState);
        var states = await context.AchTransactions.OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.State }).ToListAsync();
        Assert.Equal(new[] { 7301, 7302, 7303 }, states.Select(x => x.Id));
        Assert.Equal(AchTransferStateEnum.Pending, states[0].State);
        Assert.Equal(AchTransferStateEnum.ReturnedByOperator, states[1].State);
        Assert.Equal(AchTransferStateEnum.Pending, states[2].State);
        var evt = Assert.Single(await context.AchTransactionStateEvents.ToListAsync());
        Assert.Equal(7302, evt.AchTransactionId);
        Assert.Equal("ERR_ACCOUNT", evt.ReasonCode);
        Assert.Equal(AchStateEventSourceEnum.Operator, evt.Source);
        Assert.Equal(2, await context.CenitChamberResponses.CountAsync());
    }

    [Fact]
    public async Task I13_PT14_TwoDistinctItemNacksInOnePayload_ApplyOnlyThoseTwoAndReplayOnce()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var first = await context.AchTransactions.SingleAsync();
        var export = await context.AchFileExports.Include(x => x.Transactions).SingleAsync();
        var second = Transaction(7302, first.AchCycleId, first.AchBatchId,
            first.SourceInstitutionId, first.DestinationInstitutionId, "000010070007302");
        var third = Transaction(7303, first.AchCycleId, first.AchBatchId,
            first.SourceInstitutionId, first.DestinationInstitutionId, "000010070007303");
        context.AchTransactions.AddRange(second, third);
        export.TotalTransactions = 3;
        foreach (var (transfer, sequence) in new[] { (second, 2), (third, 3) })
            export.Transactions.Add(new AchFileExportTransaction
            {
                AchTransactionId = transfer.Id, AchCycleId = transfer.AchCycleId,
                AchBatchId = transfer.AchBatchId, FileSequence = sequence,
                TraceNumber = transfer.TraceNumber, Amount = transfer.Amount,
                IncludedAtUtc = Utc()
            });
        await context.SaveChangesAsync();

        var command = Command("OPERATOR") with
        {
            SourceResponseId = "I13-TWO-NACKS",
            Content = I13NackItems(("000010070007302", "R02"), ("000010070007303", "R03")),
            TransactionTraceNumber = null
        };
        var service = CreateService(context);
        var imported = await service.ImportAsync(command);
        Assert.Equal(2, imported.ItemCount);
        Assert.True(imported.IsApplied);
        Assert.Equal(CenitChamberResponseState.Pending, export.ChamberResponseState);
        var rows = await context.CenitChamberResponses.OrderBy(x => x.ItemSequence).ToListAsync();
        Assert.Equal(new[] { 7302, 7303 }, rows.Select(x => x.AchTransactionId!.Value).ToArray());
        Assert.All(rows, x => Assert.True(x.IsApplied));
        Assert.Equal(AchTransferStateEnum.Pending, first.State);
        Assert.Equal(AchTransferStateEnum.ReturnedByOperator, second.State);
        Assert.Equal(AchTransferStateEnum.ReturnedByOperator, third.State);
        Assert.Equal(2, await context.AchTransactionStateEvents.CountAsync());

        await using var replayContext = NewContext(connection);
        var replay = await CreateService(replayContext).ImportAsync(command);
        Assert.True(replay.IsDuplicate);
        Assert.Equal(2, await replayContext.CenitChamberResponses.CountAsync());
        Assert.Equal(2, await replayContext.AchTransactionStateEvents.CountAsync());
    }

    [Fact]
    public async Task I13_PT15_ConflictingReasonsForSameTrace_ArePersistedForReviewWithoutMutations()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var cmd = Command("OPERATOR") with
        {
            SourceResponseId = "I13-CONFLICTING-TRACE",
            Content = I13NackItems(("000010070007301", "R02"),
                ("000010070007301", "R03")),
            TransactionTraceNumber = null
        };
        var service = CreateService(context);
        var response = await service.ImportAsync(cmd);
        Assert.False(response.IsApplied);
        Assert.Equal("CENIT_NACK_CONFLICTING_REASONS_FOR_TRACE", response.ProblemCode);
        var rows = await context.CenitChamberResponses.OrderBy(x => x.ItemSequence).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, x =>
        {
            Assert.False(x.IsApplied);
            Assert.Equal(CenitChamberCorrelationOutcome.Invalid, x.CorrelationOutcome);
            Assert.Equal("CENIT_NACK_CONFLICTING_REASONS_FOR_TRACE", x.ProblemCode);
        });
        Assert.Equal(AchTransferStateEnum.Pending, (await context.AchTransactions.SingleAsync()).State);
        Assert.Equal(CenitChamberResponseState.Pending, (await context.AchFileExports.SingleAsync()).ChamberResponseState);
        Assert.Empty(await context.AchTransactionStateEvents.ToListAsync());
        Assert.True((await service.ImportAsync(cmd)).IsDuplicate);
        Assert.Equal(2, await context.CenitChamberResponses.CountAsync());
    }

    [Fact]
    public async Task I13_PT15_StaleExportMembershipCannotRejectDifferentUnderlyingTransaction()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var first = await context.AchTransactions.SingleAsync();
        var second = Transaction(7302, first.AchCycleId, first.AchBatchId,
            first.SourceInstitutionId, first.DestinationInstitutionId, "000010070007302");
        context.AchTransactions.Add(second);
        var membership = await context.AchFileExportTransactions.SingleAsync();
        membership.AchTransactionId = second.Id; // corrupted/stale association
        await context.SaveChangesAsync();
        var result = await CreateService(context).ImportAsync(Command("OPERATOR"));
        Assert.False(result.IsApplied);
        Assert.Equal("CENIT_TRANSACTION_TRACE_MISMATCH", result.ProblemCode);
        Assert.Equal(CenitChamberCorrelationOutcome.TransactionNotFound, result.CorrelationOutcome);
        Assert.Null(result.RelatedTransactionId);
        Assert.Equal(AchTransferStateEnum.Pending, first.State);
        Assert.Equal(AchTransferStateEnum.Pending, second.State);
        Assert.Empty(await context.AchTransactionStateEvents.ToListAsync());
        Assert.Equal(CenitChamberResponseState.Pending, (await context.AchFileExports.SingleAsync()).ChamberResponseState);
    }

    [Fact]
    public async Task I13_PT15_SameSourceDifferentContent_ReportsConflictWithoutStateRegression()
    {
        await using var connection = await OpenAsync();
        await using var context = await CreateSeededContextAsync(connection);
        var command = Command("OPERATOR");
        var service = CreateService(context);
        Assert.True((await service.ImportAsync(command)).IsApplied);
        var conflict = await service.ImportAsync(command with
        {
            Content = I13NackItems(("000010070007301", "ERR_CHANGED"))
        });
        Assert.False(conflict.IsApplied);
        Assert.Equal("CENIT_RESPONSE_ID_CONFLICT", conflict.ProblemCode);
        Assert.Single(await context.CenitChamberResponses.ToListAsync());
        Assert.Single(await context.AchTransactionStateEvents.ToListAsync());
    }

    private static string I13NackItems(params (string Trace, string Reason)[] items)
        => "<FileNack><AdditionalRefs><RelatedRef>REF-1</RelatedRef></AdditionalRefs>"
            + string.Concat(items.Select(item =>
                $"<FileErrorHandling><Status>NACK</Status><BatchNo>1</BatchNo><TraceNo>{item.Trace}</TraceNo><ErrorCode>{item.Reason}</ErrorCode></FileErrorHandling>"))
            + "</FileNack>";

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

    private static AchTransaction Transaction(
        int id,
        string cycleId,
        int batchId,
        int sourceId,
        int destinationId,
        string traceNumber = "000010070007301") => new()
    {
        Id = id,
        TransactionExternalId = $"TX-{id}",
        Reference = $"REF-{id}",
        Amount = 100m,
        Type = TransactionTypeEnum.Credit,
        TransactionCode = "22",
        TraceNumber = traceNumber,
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

    private static string Fixture(string name)
        => Path.Combine(AppContext.BaseDirectory, "TestData", "Cenit", "ChamberResponses", name);
}
