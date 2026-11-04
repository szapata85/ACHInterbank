using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class IncomingPrenotificationReturnGuardI04Tests
{
    private const string OriginalTrace = "123456780000123";
    private static (IncomingNachaFileIngestion Ingestion, EntryDetail Entry, AddendaRecord Addenda,
        IncomingNachaEntryClassification Classification, IncomingNachaLinkingResult Link,
        AchTransaction Original, PrenotificationValidationAttempt Attempt, NachaInboundSimulation Simulation) Fixture(int house = 1)
    {
        var original = new AchTransaction
        {
            Id = 101, IsPrenotification = true, Type = TransactionTypeEnum.Credit,
            Amount = 0, TraceNumber = OriginalTrace, State = AchTransferStateEnum.Pending,
            SourceInstitutionId = 11, DestinationInstitutionId = 22,
            AchCycle = new AchCycle { ClearingHouseId = house }
        };
        var validation = new ThirdPartyAccountValidation { Id = 50, ClearingHouseId = house, CurrentAttemptId = 15 };
        var attempt = new PrenotificationValidationAttempt
        {
            Id = 15, AchTransactionId = 101, Status = PrenotificationValidationAttemptStatus.AwaitingResponse,
            ThirdPartyAccountValidation = validation, ThirdPartyAccountValidationId = 50
        };
        var simulation = new NachaInboundSimulation
        {
            ClearingHouseId = house,
            OriginFinancialInstitutionId = 22,
            DestinationFinancialInstitutionId = 11,
            ScenarioType = NachaInboundSimulationType.IncomingPrenotificationReturn,
            Status = NachaInboundSimulationStatus.Generated,
            ReasonCode = "R03",
            Entries = new List<NachaInboundSimulationEntry> {
                new() { TransactionId = 101, PrenotificationReference = OriginalTrace }
            }
        };
        return (
            new IncomingNachaFileIngestion { ResolvedClearingHouseId = house, FileHashSha256 = "ABCD" },
            new EntryDetail { SequenceNumber = "876543210000001", Amount = 0m },
            new AddendaRecord { CodeTypeAddendumRecord = "99", OriginalTraceNumber = OriginalTrace, ReturnReasonCode = "R03" },
            new IncomingNachaEntryClassification { FunctionalClass = IncomingNachaFunctionalClass.Devolucion,
                OriginalTraceRef = OriginalTrace, ReturnReasonCode = "R03" },
            new IncomingNachaLinkingResult { LinkType = IncomingNachaLinkType.ExactOriginalTraceRef, IsFinal = true, AchTransactionId = 101 },
            original, attempt, simulation);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void ValidSimulatedReturn_ForBothChambers_ContinuesThroughOfficialPipeline(int house)
    {
        var f = Fixture(house);
        var result = IncomingPrenotificationReturnGuard.Evaluate(f.Ingestion, f.Entry, f.Addenda,
            f.Classification, f.Link, f.Original, f.Attempt, f.Simulation);
        Assert.True(result.Allowed);
        Assert.True(result.Applies);
        Assert.Equal("PRENOTIFICATION_RETURN_CORRELATED", result.Code);
    }

    [Fact]
    public void DifferentOriginalTrace_IsBlocked()
    {
        var f = Fixture(); f.Addenda.OriginalTraceNumber = "999999990000123";
        Assert.Equal("PRENOTE_ORIGINAL_TRACE_MISMATCH", Evaluate(f).Code);
    }

    [Fact]
    public void NonzeroAmount_IsBlocked()
    {
        var f = Fixture(); f.Entry.Amount = 1m;
        Assert.Equal("PRENOTE_RETURN_NONZERO_AMOUNT", Evaluate(f).Code);
    }

    [Fact]
    public void MissingExactLink_IsBlocked()
    {
        var f = Fixture();
        var result = IncomingPrenotificationReturnGuard.Evaluate(f.Ingestion, f.Entry, f.Addenda,
            f.Classification, new IncomingNachaLinkingResult { AchTransactionId = 101, IsFinal = true,
                LinkType = IncomingNachaLinkType.ExactTrace15 }, f.Original, f.Attempt, f.Simulation);
        Assert.Equal("PRENOTE_LINK_NOT_EXACT", result.Code);
    }

    [Fact]
    public void SimulationDifferentBank_IsBlocked()
    {
        var f = Fixture(); f.Simulation.OriginFinancialInstitutionId = 999;
        Assert.Equal("SIMULATION_PROVENANCE_MISMATCH", Evaluate(f).Code);
    }

    [Fact]
    public void SimulationDifferentReason_IsBlocked()
    {
        var f = Fixture(); f.Simulation.ReasonCode = "R02";
        Assert.Equal("SIMULATION_PROVENANCE_MISMATCH", Evaluate(f).Code);
    }

    [Fact]
    public void MissingAttemptInSimulation_IsBlocked()
    {
        var f = Fixture();
        var result = IncomingPrenotificationReturnGuard.Evaluate(f.Ingestion, f.Entry, f.Addenda,
            f.Classification, f.Link, f.Original, null, f.Simulation);
        Assert.Equal("SIMULATION_PRENOTE_ATTEMPT_REQUIRED", result.Code);
    }

    [Fact]
    public void PreviouslyAcceptedAttempt_IsNotRewritten()
    {
        var f = Fixture(); f.Attempt.Resolution = PrenotificationValidationResolution.TacitlyAccepted;
        Assert.Equal("PRENOTE_ALREADY_RESOLVED", Evaluate(f).Code);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void I10_LateReturnAfterTacitAcceptance_RequiresManualReview_InBothChambers(int house)
    {
        var f = Fixture(house);
        f.Original.State = AchTransferStateEnum.AppliedTacitly;
        f.Attempt.Status = PrenotificationValidationAttemptStatus.TacitlyAccepted;
        f.Attempt.Resolution = PrenotificationValidationResolution.TacitlyAccepted;
        var result = Evaluate(f);
        Assert.False(result.Allowed);
        Assert.Equal("PRENOTE_LATE_RETURN_REQUIRES_MANUAL_REVIEW", result.Code);
    }

    [Fact]
    public void I10_LegacyCertifiedPrenoteWithoutAttempt_IsNotReversed()
    {
        var f = Fixture(2);
        f.Original.State = AchTransferStateEnum.Certified;
        var result = IncomingPrenotificationReturnGuard.Evaluate(
            f.Ingestion, f.Entry, f.Addenda, f.Classification, f.Link,
            f.Original, null, f.Simulation);
        Assert.False(result.Allowed);
        Assert.Equal("PRENOTE_LATE_RETURN_REQUIRES_MANUAL_REVIEW", result.Code);
    }

    [Fact]
    public void ReplaySameRejectedReturn_IsAllowedToReachIdempotentStateMachine()
    {
        var f = Fixture();
        f.Attempt.Resolution = PrenotificationValidationResolution.Rejected;
        f.Attempt.Status = PrenotificationValidationAttemptStatus.Rejected;
        f.Attempt.ReturnCode = "R03";
        f.Original.State = AchTransferStateEnum.ReturnedByEpr;
        Assert.True(Evaluate(f).Allowed);
    }

    [Fact]
    public void ConflictingRejectedReturn_IsBlocked()
    {
        var f = Fixture();
        f.Attempt.Resolution = PrenotificationValidationResolution.Rejected;
        f.Attempt.ReturnCode = "R02";
        f.Original.State = AchTransferStateEnum.ReturnedByEpr;
        Assert.Equal("PRENOTE_ALREADY_RESOLVED", Evaluate(f).Code);
    }

    [Fact]
    public void AmbiguousHash_IsBlockedWithoutThrowing()
    {
        var f = Fixture();
        var result = IncomingPrenotificationReturnGuard.Evaluate(f.Ingestion, f.Entry, f.Addenda,
            f.Classification, f.Link, f.Original, f.Attempt, f.Simulation, true);
        Assert.Equal("SIMULATION_FILE_HASH_AMBIGUOUS", result.Code);
    }

    [Fact]
    public void OrdinaryNonPrenoteReturn_IsNotChanged()
    {
        var f = Fixture(); f.Original.IsPrenotification = false;
        var result = IncomingPrenotificationReturnGuard.Evaluate(f.Ingestion, f.Entry, f.Addenda,
            f.Classification, f.Link, f.Original, null, null);
        Assert.True(result.Allowed);
        Assert.False(result.Applies);
    }

    private static IncomingPrenotificationReturnGuard.Result Evaluate(
        (IncomingNachaFileIngestion Ingestion, EntryDetail Entry, AddendaRecord Addenda,
         IncomingNachaEntryClassification Classification, IncomingNachaLinkingResult Link,
         AchTransaction Original, PrenotificationValidationAttempt Attempt, NachaInboundSimulation Simulation) f)
        => IncomingPrenotificationReturnGuard.Evaluate(f.Ingestion, f.Entry, f.Addenda,
            f.Classification, f.Link, f.Original, f.Attempt, f.Simulation);
}
