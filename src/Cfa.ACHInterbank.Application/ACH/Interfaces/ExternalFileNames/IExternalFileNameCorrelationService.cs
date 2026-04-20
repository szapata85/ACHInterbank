using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;

public interface IExternalFileNameCorrelationService
{
    Task<ExternalFileNameCorrelationEvidence> CorrelateAsync(ExternalFileNameContext context, ExternalFileNameComponents components, CancellationToken ct = default);
}
