using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IOrdinaryTransactionCodeAuthority
{
    Task<string> ResolveAsync(
        OrdinaryTransactionCodeAuthorityContext context,
        OrdinaryTransactionCodeSemanticRequest semantic,
        CancellationToken ct = default);
}
