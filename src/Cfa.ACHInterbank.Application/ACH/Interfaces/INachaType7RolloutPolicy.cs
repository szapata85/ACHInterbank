using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaType7RolloutPolicy
{
    Task<NachaType7RolloutDecision> EvaluateAsync(
        string clearingHouseCode,
        CfgLayoutVariant? layoutVariant,
        string generationMode,
        CancellationToken ct = default);
}
