using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaInboundSimulationService
{
    Task<PrenotificationTacitAcceptancePreviewResponse> PreviewTacitAcceptanceAsync(PrenotificationTacitAcceptancePreviewRequest request, CancellationToken ct = default);
    Task<PrenotificationTacitAcceptanceApplyResponse> ApplyDueTacitAcceptanceAsync(PrenotificationSimulationContractRequest request, string actor, CancellationToken ct = default);
    Task<GenerateNachaInboundSimulationResponse> GeneratePrenotificationReturnAsync(PrenotificationSimulationContractRequest request, string userName, CancellationToken ct = default);
    Task<PrenotificationCandidatePage> ListOriginatedPrenotificationsAsync(PrenotificationCandidateQuery request, CancellationToken ct = default);
    Task<PrenotificationBusinessEligibilityPreviewResponse> PreviewPrenotificationAsync(PrenotificationSimulationContractRequest request, CancellationToken ct = default);
    Task<GenerateNachaInboundSimulationResponse> GenerateAsync(GenerateNachaInboundSimulationRequest request, string userName, CancellationToken ct = default);
    Task<IReadOnlyList<NachaInboundSimulationDto>> ListAsync(CancellationToken ct = default);
    Task<NachaInboundSimulationDto?> GetAsync(int id, CancellationToken ct = default);
    Task<(string FileName, string ContentType, byte[] Content)?> GetFileAsync(int id, CancellationToken ct = default);
    Task<NachaInboundSimulationMetadataDto?> GetEvidenceAsync(int id, CancellationToken ct = default);
    Task<InboundSimulationEligibilityPreviewResponse> PreviewAsync(InboundSimulationEligibilityPreviewRequest request, CancellationToken ct = default);
    Task<DifferentialResponseTransactionPage> ListEligibleDifferentialTransactionsAsync(DifferentialResponseTransactionQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<AvailableInboundCycleDto>> ListAvailableCyclesAsync(AvailableInboundCycleQuery query, CancellationToken ct = default);
}

public interface INachaInboundSimulationFileGenerator;
public interface INachaInboundSimulationEvidenceService;
public interface INachaInboundSimulationEligibilityPolicy;
public interface INachaInboundSimulationQueryService;
