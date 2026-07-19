using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaInboundSimulationService
{
    Task<GenerateNachaInboundSimulationResponse> GenerateAsync(GenerateNachaInboundSimulationRequest request, string userName, CancellationToken ct = default);
    Task<IReadOnlyList<NachaInboundSimulationDto>> ListAsync(CancellationToken ct = default);
    Task<NachaInboundSimulationDto?> GetAsync(int id, CancellationToken ct = default);
    Task<(string FileName, string ContentType, byte[] Content)?> GetFileAsync(int id, CancellationToken ct = default);
    Task<NachaInboundSimulationMetadataDto?> GetEvidenceAsync(int id, CancellationToken ct = default);
    Task<InboundSimulationEligibilityPreviewResponse> PreviewAsync(InboundSimulationEligibilityPreviewRequest request, CancellationToken ct = default);
    Task<DifferentialResponseTransactionPage> ListEligibleDifferentialTransactionsAsync(DifferentialResponseTransactionQuery query, CancellationToken ct = default);
}

public interface INachaInboundSimulationFileGenerator;
public interface INachaInboundSimulationEvidenceService;
public interface INachaInboundSimulationEligibilityPolicy;
public interface INachaInboundSimulationQueryService;
