using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaFileBuilder
{
    Task<string> BuildRecordAsync<T>(string recordType, T entity, CancellationToken ct);
    Task<string> BuildNachaFileAsync(IEnumerable<int> batchIds, CancellationToken ct);
}
