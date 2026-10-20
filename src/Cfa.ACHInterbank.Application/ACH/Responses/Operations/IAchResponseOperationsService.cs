namespace Cfa.ACHInterbank.Application.ACH.Responses.Operations;

public interface IAchResponseOperationsService
{
    Task<IReadOnlyList<AchResponseMappingModel>> ListMappingsAsync(int? clearingHouseId, bool? active, CancellationToken ct = default);
    Task<AchResponseMappingModel?> GetMappingAsync(int id, CancellationToken ct = default);
    Task<AchResponseMappingModel> CreateMappingAsync(AchResponseMappingCommand command, string actor, string correlationId, CancellationToken ct = default);
    Task<AchResponseMappingModel> UpdateMappingAsync(int id, AchResponseMappingCommand command, string actor, string correlationId, CancellationToken ct = default);
    Task<AchResponseMappingModel> SetMappingActiveAsync(int id, bool active, Guid expectedVersion, string reason, string actor, string correlationId, CancellationToken ct = default);
    Task<IReadOnlyList<AchResponseAuditModel>> GetAuditAsync(string entityType, string entityId, CancellationToken ct = default);
    Task<IReadOnlyList<AchResponseOrphanModel>> ListOrphansAsync(int? clearingHouseId, string? status, CancellationToken ct = default);
    Task<AchResponseOrphanModel> CreateOrphanAsync(Guid responseId, string reason, string? candidates, string actor, string correlationId, CancellationToken ct = default);
    Task<AchResponseOrphanModel> BeginReviewAsync(Guid orphanId, Guid expectedVersion, string reason, string actor, string correlationId, CancellationToken ct = default);
    Task<AchResponseOrphanModel> ResolveOrphanAsync(Guid orphanId, ManualResolutionCommand command, string actor, CancellationToken ct = default);
    Task<AchResponseReprocessModel> RequestReprocessAsync(Guid responseId, ReprocessCommand command, string actor, CancellationToken ct = default);
    Task<IReadOnlyList<AchResponseReprocessModel>> ListReprocessAttemptsAsync(Guid responseId, CancellationToken ct = default);
    Task<AchResponseReprocessModel?> GetReprocessAttemptAsync(Guid responseId, long attemptId, CancellationToken ct = default);
    Task<IReadOnlyList<AchResponseReconciliationCaseModel>> ListReconciliationCasesAsync(int? clearingHouseId, string? status, CancellationToken ct = default);
    Task<AchResponseReconciliationCaseModel> ResolveReconciliationCaseAsync(Guid id, ReconciliationResolutionCommand command, string actor, CancellationToken ct = default);
}
