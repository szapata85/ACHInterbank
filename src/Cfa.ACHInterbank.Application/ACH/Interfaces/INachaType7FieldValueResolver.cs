using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface INachaType7FieldValueResolver
{
    IReadOnlyDictionary<string, object?> Resolve(AchBatch batch, AchTransaction transaction, AchTransactionAddenda addenda);
}
