using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;

public interface IExternalFileNamePolicy
{
    Task<ExternalFileNamePolicyResult> GenerateExternalNameAsync(ExternalFileNameContext context, CancellationToken ct = default);
    Task<ExternalFileNameValidationResult> ValidateExternalNameAsync(ExternalFileNameContext context, CancellationToken ct = default);
    Task<ExternalFileNameCorrelationEvidence> CorrelateExternalNameAsync(ExternalFileNameContext context, CancellationToken ct = default);
    Task RegisterExternalNameAsync(ExternalFileNameContext context, ExternalFileNamePolicyResult result, CancellationToken ct = default);
    Task<bool> CheckDuplicateAsync(ExternalFileNameContext context, CancellationToken ct = default);
    Task<ExternalFileNameComponents> PreviewExternalNameAsync(ExternalFileNameContext context, CancellationToken ct = default);
}
