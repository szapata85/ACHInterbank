using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;

public interface IExternalFileNameAuditService
{
    Task RegisterAsync(ExternalFileNameContext context, ExternalFileNamePolicyResult result, CancellationToken ct = default);
}
