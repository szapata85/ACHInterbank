using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchCycleAppService
{
    Task<IEnumerable<AchCycleDto>> GetAsync(
        int? clearingHouseId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default);
    Task<AchCycleDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<AchCycleDto> CreateAsync(AchCycleRequest request, CancellationToken ct = default);
    Task<AchCycleDto> UpdateAsync(string id, AchCycleRequest request, CancellationToken ct = default);
    Task<AchCycleConfigurationLinkRepairResult> RepairConfigurationLinksAsync(CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task<IEnumerable<AchCycleExportDto>> GetExecutedWithTransactionsAsync(
        int? clearingHouseId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default);
}
