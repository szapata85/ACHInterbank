using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IIncomingNachaCommandCenterService
{
    Task<IncomingNachaObservabilitySummaryDto> GetObservabilitySummaryAsync(int windowHours = 24, CancellationToken ct = default);
    Task<IncomingNachaPageResult<IncomingNachaIngestionListItemDto>> GetIngestionsAsync(IncomingNachaIngestionQuery query, CancellationToken ct = default);
    Task<IncomingNachaIngestionDetailDto?> GetIngestionDetailAsync(Guid ingestionId, CancellationToken ct = default);
    Task<IReadOnlyList<IncomingNachaValidationDto>?> GetIngestionValidationsAsync(Guid ingestionId, CancellationToken ct = default);
    Task<IncomingNachaPageResult<IncomingNachaQueueListItemDto>> GetQueueAsync(IncomingNachaQueueQuery query, CancellationToken ct = default);
    Task<IncomingNachaQueueDetailDto?> GetQueueDetailAsync(Guid queueId, CancellationToken ct = default);
    Task<IncomingNachaPageResult<IncomingNachaBatchDto>> GetBatchesAsync(Guid ingestionId, IncomingNachaBatchQuery query, CancellationToken ct = default);
    Task<IncomingNachaPageResult<IncomingNachaTransactionDto>> GetTransactionsAsync(Guid ingestionId, IncomingNachaTransactionQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<IncomingNachaAddendaDto>> GetAddendasAsync(Guid ingestionId, int entryDetailId, CancellationToken ct = default);
    Task<IncomingNachaPageResult<IncomingNachaOrphanDto>> GetOrphansAsync(IncomingNachaOrphanQuery query, CancellationToken ct = default);
    Task<IncomingNachaOrphanDto?> GetOrphanAsync(Guid linkId, CancellationToken ct = default);
    Task<IReadOnlyList<IncomingNachaOrphanCandidateDto>> GetOrphanCandidatesAsync(Guid linkId, string? search, CancellationToken ct = default);

    Task<IncomingNachaManualActionResultDto> RetryManualAsync(Guid queueId, IncomingNachaManualActionRequest request, string performedBy, CancellationToken ct = default);
    Task<IncomingNachaManualActionResultDto> UnblockManualAsync(Guid queueId, IncomingNachaManualActionRequest request, string performedBy, CancellationToken ct = default);
    Task<IncomingNachaManualActionResultDto> RequeueManualAsync(Guid queueId, IncomingNachaManualActionRequest request, string performedBy, CancellationToken ct = default);
    Task<IncomingNachaManualActionResultDto> MarkFailedFinalManualAsync(Guid queueId, IncomingNachaManualActionRequest request, string performedBy, CancellationToken ct = default);
}
