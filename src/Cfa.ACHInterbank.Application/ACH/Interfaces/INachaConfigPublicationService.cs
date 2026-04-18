using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaConfigPublicationService
{
    Task<NachaConfigPublicationResultDto> PublishAsync(int profileId, string actor, string expectedRowVersion, CancellationToken ct = default);
}
