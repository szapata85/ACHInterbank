using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Compatibility facade retained during the incremental R6C refactor. Production consumers should depend on
/// the query or operations capability they actually use.
/// </summary>
[Scoped]
public sealed class IncomingNachaCommandCenterService(
    IIncomingNachaCommandCenterQueryService queryService,
    IIncomingNachaCommandCenterOperationsService operationsService) : IIncomingNachaCommandCenterService
{
    public Task<IncomingNachaObservabilitySummaryDto> GetObservabilitySummaryAsync(int windowHours = 24, CancellationToken ct = default)
        => queryService.GetObservabilitySummaryAsync(windowHours, ct);

    public Task<IncomingNachaPageResult<IncomingNachaIngestionListItemDto>> GetIngestionsAsync(IncomingNachaIngestionQuery query, CancellationToken ct = default)
        => queryService.GetIngestionsAsync(query, ct);

    public Task<IncomingNachaIngestionDetailDto?> GetIngestionDetailAsync(Guid ingestionId, CancellationToken ct = default)
        => queryService.GetIngestionDetailAsync(ingestionId, ct);

    public Task<IReadOnlyList<IncomingNachaValidationDto>?> GetIngestionValidationsAsync(Guid ingestionId, CancellationToken ct = default)
        => queryService.GetIngestionValidationsAsync(ingestionId, ct);

    public Task<IncomingNachaPageResult<IncomingNachaQueueListItemDto>> GetQueueAsync(IncomingNachaQueueQuery query, CancellationToken ct = default)
        => queryService.GetQueueAsync(query, ct);

    public Task<IncomingNachaQueueDetailDto?> GetQueueDetailAsync(Guid queueId, CancellationToken ct = default)
        => queryService.GetQueueDetailAsync(queueId, ct);

    public Task<IncomingNachaPageResult<IncomingNachaBatchDto>> GetBatchesAsync(Guid ingestionId, IncomingNachaBatchQuery query, CancellationToken ct = default)
        => queryService.GetBatchesAsync(ingestionId, query, ct);

    public Task<IncomingNachaPageResult<IncomingNachaTransactionDto>> GetTransactionsAsync(Guid ingestionId, IncomingNachaTransactionQuery query, CancellationToken ct = default)
        => queryService.GetTransactionsAsync(ingestionId, query, ct);

    public Task<IReadOnlyList<IncomingNachaAddendaDto>> GetAddendasAsync(Guid ingestionId, int entryDetailId, CancellationToken ct = default)
        => queryService.GetAddendasAsync(ingestionId, entryDetailId, ct);

    public Task<IncomingNachaPageResult<IncomingNachaOrphanDto>> GetOrphansAsync(IncomingNachaOrphanQuery query, CancellationToken ct = default)
        => queryService.GetOrphansAsync(query, ct);

    public Task<IncomingNachaOrphanDto?> GetOrphanAsync(Guid linkId, CancellationToken ct = default)
        => queryService.GetOrphanAsync(linkId, ct);

    public Task<IReadOnlyList<IncomingNachaOrphanCandidateDto>> GetOrphanCandidatesAsync(Guid linkId, string? search, CancellationToken ct = default)
        => queryService.GetOrphanCandidatesAsync(linkId, search, ct);

    public Task<IncomingNachaManualActionResultDto> RetryManualAsync(Guid queueId, IncomingNachaManualActionRequest request, string performedBy, CancellationToken ct = default)
        => operationsService.RetryManualAsync(queueId, request, performedBy, ct);

    public Task<IncomingNachaManualActionResultDto> UnblockManualAsync(Guid queueId, IncomingNachaManualActionRequest request, string performedBy, CancellationToken ct = default)
        => operationsService.UnblockManualAsync(queueId, request, performedBy, ct);

    public Task<IncomingNachaManualActionResultDto> RequeueManualAsync(Guid queueId, IncomingNachaManualActionRequest request, string performedBy, CancellationToken ct = default)
        => operationsService.RequeueManualAsync(queueId, request, performedBy, ct);

    public Task<IncomingNachaManualActionResultDto> MarkFailedFinalManualAsync(Guid queueId, IncomingNachaManualActionRequest request, string performedBy, CancellationToken ct = default)
        => operationsService.MarkFailedFinalManualAsync(queueId, request, performedBy, ct);
}
