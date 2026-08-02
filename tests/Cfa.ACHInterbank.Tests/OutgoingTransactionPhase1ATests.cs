using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class OutgoingTransactionPhase1ATests
{
    [Theory]
    [InlineData(TransactionTypeEnum.Debit, false, true, false, AchTransactionDirection.Outgoing, AchTransactionOrigin.Cfa, AchMonetaryIntegrationRoute.ProcContrapartidas, AchTransactionClassificationStatus.Determined)]
    [InlineData(TransactionTypeEnum.Credit, false, false, true, AchTransactionDirection.Incoming, AchTransactionOrigin.ExternalInstitution, AchMonetaryIntegrationRoute.ProcTransacciones, AchTransactionClassificationStatus.Determined)]
    [InlineData(TransactionTypeEnum.Debit, true, true, false, AchTransactionDirection.Outgoing, AchTransactionOrigin.Cfa, AchMonetaryIntegrationRoute.None, AchTransactionClassificationStatus.Determined)]
    [InlineData(TransactionTypeEnum.Credit, false, true, false, AchTransactionDirection.Outgoing, AchTransactionOrigin.Cfa, AchMonetaryIntegrationRoute.ManualReview, AchTransactionClassificationStatus.Invalid)]
    [InlineData(TransactionTypeEnum.Debit, false, false, false, AchTransactionDirection.Unknown, AchTransactionOrigin.Unknown, AchMonetaryIntegrationRoute.ManualReview, AchTransactionClassificationStatus.Ambiguous)]
    public void ClassificationPolicy_IsDeterministicAndContextual(
        TransactionTypeEnum type,
        bool isPrenotification,
        bool sourceIsCfa,
        bool destinationIsCfa,
        AchTransactionDirection expectedDirection,
        AchTransactionOrigin expectedOrigin,
        AchMonetaryIntegrationRoute expectedRoute,
        AchTransactionClassificationStatus expectedStatus)
    {
        var result = new AchTransactionClassificationPolicy().Classify(new(
            type, isPrenotification, sourceIsCfa, destinationIsCfa));

        result.Direction.Should().Be(expectedDirection);
        result.Origin.Should().Be(expectedOrigin);
        result.MonetaryIntegrationRoute.Should().Be(expectedRoute);
        result.Status.Should().Be(expectedStatus);
        result.ClassificationVersion.Should().Be(1);
    }

    [Fact]
    public async Task PersistedClassification_CannotBeChangedAfterCreation()
    {
        await using var context = CreateInMemoryContext();
        var transaction = MinimalTransaction(10, AchTransferStateEnum.Pending);
        transaction.Direction = AchTransactionDirection.Outgoing;
        transaction.Origin = AchTransactionOrigin.Cfa;
        transaction.MonetaryIntegrationRoute = AchMonetaryIntegrationRoute.ProcContrapartidas;
        transaction.ClassificationStatus = AchTransactionClassificationStatus.Determined;
        transaction.SourceInstitutionWasDefaultAtCreation = true;
        transaction.ClassifiedAtUtc = DateTime.UtcNow;
        transaction.ClassificationVersion = 1;
        context.AchTransactions.Add(transaction);
        await context.SaveChangesAsync();

        transaction.Direction = AchTransactionDirection.Incoming;
        var action = () => context.SaveChangesAsync();

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*clasificación histórica*inmutable*");
    }

    [Fact]
    public async Task MonetaryRoute_DoesNotChangeWhenDefaultSourceFlagChangesLater()
    {
        await using var context = CreateInMemoryContext();
        var source = new FinancialInstitution
        {
            Id = 1,
            Name = "CFA",
            RoutingNumber = "0001",
            TransitCode = "0001",
            IsDefaultSource = true
        };
        source.CalculateCheckDigit();
        var transaction = MinimalTransaction(11);
        transaction.SourceInstitutionId = source.Id;
        transaction.Direction = AchTransactionDirection.Outgoing;
        transaction.Origin = AchTransactionOrigin.Cfa;
        transaction.MonetaryIntegrationRoute = AchMonetaryIntegrationRoute.ProcContrapartidas;
        transaction.ClassificationStatus = AchTransactionClassificationStatus.Determined;
        transaction.SourceInstitutionWasDefaultAtCreation = true;
        transaction.ClassifiedAtUtc = DateTime.UtcNow;
        transaction.ClassificationVersion = 1;
        context.AddRange(source, transaction);
        await context.SaveChangesAsync();

        source.IsDefaultSource = false;
        await context.SaveChangesAsync();
        var resolved = await new TransactionIntegrationOperationResolver(context).ResolveAsync(transaction);

        resolved.IsSupported.Should().BeTrue();
        resolved.OperationKey.Should().Be("Proc_Contrapartidas");
        transaction.SourceInstitutionWasDefaultAtCreation.Should().BeTrue();
        transaction.Direction.Should().Be(AchTransactionDirection.Outgoing);
    }

    [Fact]
    public async Task HistoricalTransactionWithoutDeterministicClassification_RequiresReview()
    {
        await using var context = CreateInMemoryContext();
        var historical = MinimalTransaction(12);
        historical.Direction = AchTransactionDirection.Unknown;
        historical.Origin = AchTransactionOrigin.Unknown;
        historical.MonetaryIntegrationRoute = AchMonetaryIntegrationRoute.ManualReview;
        historical.ClassificationStatus = AchTransactionClassificationStatus.Unknown;

        var resolved = await new TransactionIntegrationOperationResolver(context).ResolveAsync(historical);

        resolved.IsSupported.Should().BeFalse();
        resolved.MovesMoney.Should().BeFalse();
        resolved.Errors.Should().Contain("DEBIT_NOT_CLASSIFIED_FOR_PROC_CONTRAPARTIDAS");
    }

    [Theory]
    [InlineData(AchTransferStateEnum.AppliedTacitly)]
    [InlineData(AchTransferStateEnum.Certified)]
    public async Task AcceptedTransaction_CanBeReturnedWithoutLosingAcceptanceHistory(AchTransferStateEnum acceptedState)
    {
        await using var context = CreateInMemoryContext();
        var transaction = MinimalTransaction(20, acceptedState);
        transaction.StateEvents.Add(new AchTransactionStateEvent
        {
            FromState = AchTransferStateEnum.Pending,
            ToState = acceptedState,
            Source = AchStateEventSourceEnum.System,
            ReasonCode = "ACCEPTED",
            OccurredAtUtc = DateTime.UtcNow.AddMinutes(-1)
        });
        context.AchTransactions.Add(transaction);
        await context.SaveChangesAsync();
        var service = new AchStateTransitionService(context);
        var request = new AchStateTransitionRequest(
            transaction.Id,
            AchTransferStateEnum.ReturnedByEpr,
            AchStateEventSourceEnum.Epr,
            "R01",
            OriginalTraceRef: "123456780000020",
            IdempotencyKey: "return-semantic-20-r01",
            ClearingHouseId: null,
            ResolvedReasonDescription: "Fondos insuficientes");

        var first = await service.TransitionAsync(request);
        var duplicate = await service.TransitionAsync(request);

        first.Applied.Should().BeTrue();
        duplicate.WasDuplicate.Should().BeTrue();
        transaction.State.Should().Be(AchTransferStateEnum.ReturnedByEpr);
        var events = await context.AchTransactionStateEvents.OrderBy(x => x.OccurredAtUtc).ToListAsync();
        events.Should().HaveCount(2);
        events[0].ToState.Should().Be(acceptedState);
        events[1].ResolvedReasonDescription.Should().Be("Fondos insuficientes");
    }

    [Fact]
    public async Task ReturnCodeResolution_TreatsR96AccordingToChamberAndFlow_NotGlobally()
    {
        await using var context = CreateInMemoryContext();
        context.AchReturnCodes.AddRange(
            new AchReturnCode
            {
                Id = 1, ClearingHouseId = 1, Code = "R96", FlowType = AchReturnFlowType.Return,
                Description = "Resultado exitoso configurado", BusinessOutcome = IncomingNachaBusinessOutcome.Successful,
                AppliesToDebit = true, AppliesToReturn = true, EffectiveFrom = new DateTime(2024, 1, 1), IsActive = true
            },
            new AchReturnCode
            {
                Id = 2, ClearingHouseId = 2, Code = "R96", FlowType = AchReturnFlowType.Return,
                Description = "Devolución configurada", BusinessOutcome = IncomingNachaBusinessOutcome.Returned,
                AppliesToDebit = true, AppliesToReturn = true, EffectiveFrom = new DateTime(2024, 1, 1), IsActive = true
            });
        await context.SaveChangesAsync();
        var resolver = new IncomingNachaAchResultResolver(context);
        var requestDate = new DateTime(2026, 8, 2);

        var first = await resolver.ResolveAsync(new(1, "R96", AchReturnFlowType.Return, true, false, false, false, requestDate));
        var second = await resolver.ResolveAsync(new(2, "R96", AchReturnFlowType.Return, true, false, false, false, requestDate));
        var unknown = await resolver.ResolveAsync(new(3, "R96", AchReturnFlowType.Return, true, false, false, false, requestDate));

        first.BusinessOutcome.Should().Be(IncomingNachaBusinessOutcome.Successful);
        second.BusinessOutcome.Should().Be(IncomingNachaBusinessOutcome.Returned);
        unknown.IsResolved.Should().BeFalse();
    }

    [Fact]
    public async Task ExportAudit_PersistsExactMembershipAndSeparateVersions()
    {
        await using var context = CreateInMemoryContext();
        context.AchTransactions.AddRange(MinimalTransaction(31), MinimalTransaction(32));
        await context.SaveChangesAsync();
        var service = new AchFileExportAuditService(context);

        await service.RecordGeneratedFileAsync("C1", 1, "NACHA", "file-v1.OUT", 4, 1, false, [31], new string('A', 64));
        await service.RecordGeneratedFileAsync("C1", 1, "NACHA", "file-v2.OUT", 4, 1, false, [32], new string('B', 64));

        var exports = await context.AchFileExports.Include(x => x.Transactions).OrderBy(x => x.Version).ToListAsync();
        exports.Should().HaveCount(2);
        exports.Select(x => x.Version).Should().Equal(1, 2);
        exports.Should().OnlyContain(x => x.LifecycleStatus == AchFileExportLifecycleStatus.Generated);
        exports[0].Transactions.Select(x => x.AchTransactionId).Should().Equal(31);
        exports[1].Transactions.Select(x => x.AchTransactionId).Should().Equal(32);
        exports.Select(x => x.ContentSha256).Should().Equal(new string('A', 64), new string('B', 64));
    }

    [Fact]
    public async Task Export_CannotBeMarkedAsTransmittedWithoutExternalEvidence()
    {
        await using var context = CreateInMemoryContext();
        context.AchFileExports.Add(new AchFileExport
        {
            AchCycleId = "C1",
            ClearingHouseId = 1,
            ExportKind = "NACHA",
            FileName = "file.OUT",
            LifecycleStatus = AchFileExportLifecycleStatus.Transmitted
        });

        var action = () => context.SaveChangesAsync();

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sin referencia externa y fecha verificables*");
    }

    [Fact]
    public async Task ResponseCorrelation_RequiresOneStrongExactMatch()
    {
        await using var context = CreateInMemoryContext();
        var first = MinimalTransaction(41);
        first.TransactionExternalId = "EXT-UNIQUE";
        var second = MinimalTransaction(42);
        second.TransactionExternalId = "EXT-DUPLICATE";
        var third = MinimalTransaction(43);
        third.TransactionExternalId = "EXT-DUPLICATE";
        context.AchTransactions.AddRange(first, second, third);
        await context.SaveChangesAsync();
        var service = new AchResponseTransactionCorrelationService(context);

        (await service.CorrelateAsync("EXT-UNIQUE")).Status.Should().Be(AchResponseCorrelationStatus.Matched);
        (await service.CorrelateAsync("EXT-DUPLICATE")).Status.Should().Be(AchResponseCorrelationStatus.Ambiguous);
        (await service.CorrelateAsync("RECIPIENT-NOT-AN-EXTERNAL-ID")).Status.Should().Be(AchResponseCorrelationStatus.NotFound);
    }

    [Fact]
    public async Task ContrapartidaQueue_RejectsAnIncompatibleClassificationBeforeEnqueueing()
    {
        await using var context = CreateInMemoryContext();
        var incompatible = MinimalTransaction(50);
        incompatible.Direction = AchTransactionDirection.Outgoing;
        incompatible.Origin = AchTransactionOrigin.Cfa;
        incompatible.MonetaryIntegrationRoute = AchMonetaryIntegrationRoute.ManualReview;
        incompatible.ClassificationStatus = AchTransactionClassificationStatus.Invalid;
        var service = new ContrapartidaDispatchPersistenceService(context);

        var action = () => service.EnsurePendingDispatchAsync(incompatible, 1);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no es elegible*");
        context.ContrapartidaDispatchItems.Should().BeEmpty();
    }

    private static AchDbContext CreateInMemoryContext()
        => new(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static AchTransaction MinimalTransaction(int id, AchTransferStateEnum state = AchTransferStateEnum.Pending)
        => new()
        {
            Id = id,
            Amount = 100m,
            TransactionExternalId = $"EXT-{id}",
            Reference = $"REF-{id}",
            Type = TransactionTypeEnum.Debit,
            TransactionCode = "27",
            TraceNumber = $"12345678{id:0000000}",
            EffectiveEntryDate = new DateTime(2026, 8, 2),
            State = state,
            StateChangedAtUtc = DateTime.UtcNow,
            SourceAccountNumber = "000000000001",
            DestinationAccountNumber = "000000000002",
            AchCycleId = "C1",
            AchBatchId = 1
        };
}
