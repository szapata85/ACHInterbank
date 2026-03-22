using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaBatchRuleService
{
    string ResolveBatchDescription(AchBatch batch, IReadOnlyCollection<AchTransaction> transactions);
    DateTime ResolveDescriptiveDate(AchBatch batch, IReadOnlyCollection<AchTransaction> transactions);
    bool RequiresDescriptiveDate(IReadOnlyCollection<AchTransaction> transactions);
}
