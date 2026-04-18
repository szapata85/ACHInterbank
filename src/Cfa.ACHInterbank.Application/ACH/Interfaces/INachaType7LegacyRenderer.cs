using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaType7LegacyRenderer
{
    string Render(AchBatch batch, AchTransaction transaction, AchTransactionAddenda addenda);
}
