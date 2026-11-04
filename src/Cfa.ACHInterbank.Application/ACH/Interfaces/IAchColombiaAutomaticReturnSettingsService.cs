using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchColombiaAutomaticReturnSettingsService
{
    Task<AchColombiaAutomaticReturnSettings> GetAsync(CancellationToken cancellationToken = default);
    Task<AchColombiaAutomaticReturnSettings> UpdateAsync(UpdateAchColombiaAutomaticReturnSettings request,
        CancellationToken cancellationToken = default);
}
