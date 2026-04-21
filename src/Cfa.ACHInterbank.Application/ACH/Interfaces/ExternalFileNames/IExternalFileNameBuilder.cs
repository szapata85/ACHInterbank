using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;

public interface IExternalFileNameBuilder
{
    Task<ExternalFileNameComponents> BuildAsync(ExternalFileNameContext context, CancellationToken ct = default);
}
