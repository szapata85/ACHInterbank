using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IBulkFileParser
{
    bool CanParse(BulkIngestionFileTypeEnum fileType);
    Task<ParsedFileResult> ParseAsync(Stream stream, CancellationToken ct = default);
}
