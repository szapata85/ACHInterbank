using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchColombiaCycleOperationalScheduleAdminService
{
    Task<IReadOnlyList<AchColombiaCycleOperationalScheduleAdministrationRowDto>> GetAdministrationAsync(
        CancellationToken ct = default);

    Task<IReadOnlyList<AchColombiaCycleOperationalScheduleDto>> GetAsync(
        AchColombiaCycleOperationalScheduleFilter filter,
        CancellationToken ct = default);

    Task<AchColombiaCycleOperationalScheduleDto?> GetByCycleAsync(
        int clearingHouseCycleConfigId,
        CancellationToken ct = default);

    Task<AchColombiaCycleOperationalScheduleDto> CreateAsync(
        CreateAchColombiaCycleOperationalScheduleRequest request,
        CancellationToken ct = default);

    Task<AchColombiaCycleOperationalScheduleDto?> UpdateAsync(
        int id,
        UpdateAchColombiaCycleOperationalScheduleRequest request,
        CancellationToken ct = default);

    Task<AchColombiaCycleOperationalScheduleDto?> DeactivateAsync(
        int id,
        DeactivateAchColombiaCycleOperationalScheduleRequest request,
        CancellationToken ct = default);
}
