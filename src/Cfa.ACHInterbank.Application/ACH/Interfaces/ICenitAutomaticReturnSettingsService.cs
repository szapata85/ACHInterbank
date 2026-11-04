using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ICenitAutomaticReturnSettingsService
{
    Task<CenitAutomaticReturnSettings> GetAsync(CancellationToken cancellationToken = default);
    Task<CenitAutomaticReturnSettings> UpdateAsync(UpdateCenitAutomaticReturnSettings request,
        CancellationToken cancellationToken = default);
}
