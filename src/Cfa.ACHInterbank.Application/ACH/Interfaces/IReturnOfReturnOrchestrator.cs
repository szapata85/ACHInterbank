using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IReturnOfReturnOrchestrator
{
    Task<ReturnOfReturnFlow> RegisterAsync(AchTransaction sourceReturn, AchTransaction returnOfReturn, string reasonCode, CancellationToken ct);
}
