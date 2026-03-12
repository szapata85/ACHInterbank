using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaParserService
{
    Task<IReadOnlyList<NachaValidationFailure>> ParseAndSaveAsync(Stream nachaStream, string FileName, CancellationToken ct = default);
}
