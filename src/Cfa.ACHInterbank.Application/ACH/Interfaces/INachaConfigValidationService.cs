using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaConfigValidationService
{
    Task<NachaConfigValidationResultDto> ValidateBeforePublishAsync(int profileId, CancellationToken ct = default);
}
