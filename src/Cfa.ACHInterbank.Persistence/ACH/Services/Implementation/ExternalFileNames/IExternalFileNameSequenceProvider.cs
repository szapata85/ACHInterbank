using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

public interface IExternalFileNameSequenceProvider
{
    bool CanHandle(string? providerName);

    Task<int> ReserveNextSequenceAsync(ExternalFileNameContext context, CancellationToken ct = default);
}
