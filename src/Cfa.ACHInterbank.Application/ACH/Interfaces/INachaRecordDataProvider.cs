using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaRecordDataProvider
{
    Task<IReadOnlyList<object>> GetRecordsAsync(NachaRecordDefinition definition, NachaBuildContext context, CancellationToken ct = default);
}
