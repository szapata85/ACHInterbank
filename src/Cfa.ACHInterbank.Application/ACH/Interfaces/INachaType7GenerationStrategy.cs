using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaType7GenerationStrategy
{
    IReadOnlyList<NachaType7RecordCandidate> BuildCandidates(IReadOnlyList<AchBatch> orderedBatches);
}
