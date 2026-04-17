using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaFixedWidthRecordRenderer
{
    Task<string> RenderRecordAsync<T>(string recordType, T entity, NachaRecordLayout layout);
    Task<string> RenderRecordAsync(string recordType, IReadOnlyDictionary<string, object?> values, NachaRecordLayout layout);
}
