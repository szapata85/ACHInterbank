using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaConfigOperationalTestService
{
    Task<NachaConfigOperationalTestResultDto?> TestAsync(int profileId, CancellationToken ct = default);
}
