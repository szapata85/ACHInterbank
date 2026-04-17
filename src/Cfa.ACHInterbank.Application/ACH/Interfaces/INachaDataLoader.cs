using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaDataLoader
{
    Task<IReadOnlyList<AchBatch>> LoadBatchesByIdsAsync(IEnumerable<int> batchIds, CancellationToken ct = default);
    Task<NachaBuildContext> LoadByCycleAsync(string cycleId, CancellationToken ct = default);
    Task<NachaHeader?> LoadHeaderAsync(string cycleId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, NachaRecordLayout>> LoadLayoutsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<NachaRecordDefinition>> LoadDefinitionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<(string Term, string StandardEntryClassCode)>> LoadCompanyEntryDescriptionCatalogAsync(CancellationToken ct = default);
}
