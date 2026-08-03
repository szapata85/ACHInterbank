namespace Cfa.ACHInterbank.Application.OutgoingTransactionMonitoring;

public interface IOutgoingTransactionMonitoringQueryService
{
    Task<OutgoingMonitoringPagedResult<OutgoingTransactionMonitoringListItem>> SearchAsync(
        OutgoingTransactionMonitoringQuery query,
        CancellationToken cancellationToken = default);

    Task<OutgoingTransactionMonitoringDetail?> GetDetailAsync(
        int transactionId,
        bool includeTechnicalDetail,
        CancellationToken cancellationToken = default);
}

public interface IOutgoingTransactionMonitoringStatusPolicy
{
    OutgoingTransactionMonitoringStatus Consolidate(OutgoingTransactionMonitoringFacts facts);
}

public interface IOutgoingTransactionMonitoringAuditWriter
{
    Task WriteAsync(OutgoingTransactionMonitoringAudit audit, CancellationToken cancellationToken = default);
}

public sealed record OutgoingTransactionMonitoringFacts(
    bool HasDispatchItem,
    bool HasSuccessfulIntegration,
    bool HasFunctionalRejection,
    bool HasTechnicalFailure,
    bool HasAccepted,
    bool HasCertified,
    bool HasReturn,
    bool HasManualReview,
    bool HasAmbiguousCorrelation,
    bool HasFileMembership);

public sealed record OutgoingTransactionMonitoringStatus(
    string ProcessStatusCode,
    string ProcessStatusDisplayName,
    string InitialResultCode,
    string InitialResultDisplayName,
    string SubsequentSituationCode,
    string SubsequentSituationDisplayName,
    bool RequiresAttention,
    string? AttentionReason);
