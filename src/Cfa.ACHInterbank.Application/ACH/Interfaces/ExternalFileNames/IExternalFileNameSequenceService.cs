using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;

public interface IExternalFileNameSequenceService
{
    Task<int> ReserveNextSequenceAsync(ExternalFileNameContext context, CancellationToken ct = default);
}
