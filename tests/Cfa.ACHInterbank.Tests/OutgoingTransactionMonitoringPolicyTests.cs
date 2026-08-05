using Cfa.ACHInterbank.Application.OutgoingTransactionMonitoring;
using FluentAssertions;

namespace Cfa.ACHInterbank.Tests;

public sealed class OutgoingTransactionMonitoringPolicyTests
{
    private readonly OutgoingTransactionMonitoringStatusPolicy _policy = new();

    [Fact]
    public void Consolidate_PreservesAcceptanceWhenReturnedLater()
    {
        var result = _policy.Consolidate(Facts(success: true, accepted: true, returned: true));

        result.ProcessStatusCode.Should().Be("Processed");
        result.InitialResultCode.Should().Be("Accepted");
        result.SubsequentSituationCode.Should().Be("ReturnedLater");
        result.SubsequentSituationDisplayName.Should().Be("Devuelta posteriormente");
    }

    [Fact]
    public void Consolidate_PreservesCertificationWhenReturnedLater()
    {
        var result = _policy.Consolidate(Facts(success: true, certified: true, returned: true));

        result.InitialResultCode.Should().Be("Certified");
        result.SubsequentSituationCode.Should().Be("ReturnedLater");
    }

    [Fact]
    public void Consolidate_DoesNotConvertTechnicalFailureIntoFunctionalRejection()
    {
        var result = _policy.Consolidate(Facts(technicalFailure: true));

        result.ProcessStatusCode.Should().Be("TechnicalError");
        result.InitialResultCode.Should().Be("NotDetermined");
        result.RequiresAttention.Should().BeTrue();
    }

    [Fact]
    public void Consolidate_UsesPersistedFunctionalRejectionOnly()
    {
        var result = _policy.Consolidate(Facts(functionalRejection: true));

        result.InitialResultCode.Should().Be("Rejected");
        result.RequiresAttention.Should().BeFalse();
    }

    [Fact]
    public void Consolidate_FlagsAmbiguousCorrelationForAttention()
    {
        var result = _policy.Consolidate(Facts(ambiguousCorrelation: true));

        result.RequiresAttention.Should().BeTrue();
        result.AttentionReason.Should().Contain("correlación");
    }

    private static OutgoingTransactionMonitoringFacts Facts(
        bool success = false,
        bool functionalRejection = false,
        bool technicalFailure = false,
        bool accepted = false,
        bool certified = false,
        bool returned = false,
        bool manualReview = false,
        bool ambiguousCorrelation = false,
        bool dispatchItem = false,
        bool fileMembership = false,
        bool futureCycle = false)
        => new(
            HasDispatchItem: dispatchItem || success || functionalRejection || technicalFailure,
            HasSuccessfulIntegration: success,
            HasFunctionalRejection: functionalRejection,
            HasTechnicalFailure: technicalFailure,
            HasAccepted: accepted,
            HasCertified: certified,
            HasReturn: returned,
            HasManualReview: manualReview,
            HasAmbiguousCorrelation: ambiguousCorrelation,
            HasFileMembership: fileMembership,
            HasResponse: accepted || certified || returned,
            IsFutureCycle: futureCycle);

    [Fact]
    public void Consolidate_RepresentsUnscheduledTransactionAsCreated()
    {
        var result = _policy.Consolidate(Facts());

        result.ProcessStatusCode.Should().Be("Created");
    }

    [Fact]
    public void Consolidate_RepresentsFutureCycleWithoutInventingProcessing()
    {
        var facts = Facts(futureCycle: true);

        var result = _policy.Consolidate(facts);

        result.ProcessStatusCode.Should().Be("Scheduled");
        result.ProcessStatusDisplayName.Should().Be("Asignada a un ciclo futuro");
        result.InitialResultCode.Should().Be("NotDetermined");
    }

    [Fact]
    public void Consolidate_DoesNotRepresentStartedDispatchAsScheduled()
    {
        var result = _policy.Consolidate(Facts(dispatchItem: true, futureCycle: true));

        result.ProcessStatusCode.Should().Be("Processing");
    }

    [Fact]
    public void Consolidate_GivesTechnicalFailurePriorityOverScheduled()
    {
        var result = _policy.Consolidate(Facts(technicalFailure: true, futureCycle: true));

        result.ProcessStatusCode.Should().Be("TechnicalError");
    }

    [Fact]
    public void Consolidate_GivesSuccessfulIntegrationPriorityOverScheduled()
    {
        var result = _policy.Consolidate(Facts(success: true, futureCycle: true));

        result.ProcessStatusCode.Should().Be("Processed");
    }

    [Fact]
    public void Consolidate_GivesFunctionalRejectionPriorityOverScheduled()
    {
        var result = _policy.Consolidate(Facts(functionalRejection: true, futureCycle: true));

        result.ProcessStatusCode.Should().Be("Processing");
        result.InitialResultCode.Should().Be("Rejected");
    }

    [Fact]
    public void Consolidate_GivesReturnPriorityOverScheduled()
    {
        var result = _policy.Consolidate(Facts(returned: true, futureCycle: true));

        result.ProcessStatusCode.Should().Be("Processed");
        result.SubsequentSituationCode.Should().Be("Returned");
    }

    [Fact]
    public void Consolidate_RepresentsSuccessfulIntegrationWithoutResponseAsPending()
    {
        var facts = Facts(success: true) with { HasResponse = false };

        var result = _policy.Consolidate(facts);

        result.ProcessStatusCode.Should().Be("Processed");
        result.InitialResultCode.Should().Be("PendingResponse");
        result.InitialResultDisplayName.Should().Contain("Pendiente de respuesta");
    }
}
