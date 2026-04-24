using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IIncomingNachaCommandCenterService
{
    Task<IncomingNachaObservabilitySummaryDto> GetObservabilitySummaryAsync(int windowHours = 24, CancellationToken ct = default);
    Task<IncomingNachaPageResult<IncomingNachaIngestionListItemDto>> GetIngestionsAsync(IncomingNachaIngestionQuery query, CancellationToken ct = default);
    Task<IncomingNachaIngestionDetailDto?> GetIngestionDetailAsync(Guid ingestionId, CancellationToken ct = default);
    Task<IncomingNachaPageResult<IncomingNachaQueueListItemDto>> GetQueueAsync(IncomingNachaQueueQuery query, CancellationToken ct = default);
    Task<IncomingNachaQueueDetailDto?> GetQueueDetailAsync(Guid queueId, CancellationToken ct = default);

    Task<IncomingNachaManualActionResultDto> RetryManualAsync(Guid queueId, IncomingNachaManualActionRequest request, string performedBy, CancellationToken ct = default);
    Task<IncomingNachaManualActionResultDto> UnblockManualAsync(Guid queueId, IncomingNachaManualActionRequest request, string performedBy, CancellationToken ct = default);
    Task<IncomingNachaManualActionResultDto> RequeueManualAsync(Guid queueId, IncomingNachaManualActionRequest request, string performedBy, CancellationToken ct = default);
    Task<IncomingNachaManualActionResultDto> MarkFailedFinalManualAsync(Guid queueId, IncomingNachaManualActionRequest request, string performedBy, CancellationToken ct = default);
}
