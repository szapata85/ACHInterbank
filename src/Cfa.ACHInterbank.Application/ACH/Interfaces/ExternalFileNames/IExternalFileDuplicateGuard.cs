using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;

public interface IExternalFileDuplicateGuard
{
    Task<bool> IsDuplicateAsync(ExternalFileNameContext context, string externalFileName, CancellationToken ct = default);
}
