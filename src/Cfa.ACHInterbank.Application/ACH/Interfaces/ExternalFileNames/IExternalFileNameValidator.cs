using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;

public interface IExternalFileNameValidator
{
    Task<ExternalFileNameValidationResult> ValidateAsync(ExternalFileNameContext context, ExternalFileNameComponents components, CancellationToken ct = default);
}
