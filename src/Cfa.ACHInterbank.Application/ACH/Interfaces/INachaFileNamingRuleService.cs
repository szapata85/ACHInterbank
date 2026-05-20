using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaFileNamingRuleService
{
    Task<NachaFileNamingRulePolicy?> GetActiveOutboundRuleAsync(int clearingHouseId, DateTime processingDate, CancellationToken ct = default);
}
