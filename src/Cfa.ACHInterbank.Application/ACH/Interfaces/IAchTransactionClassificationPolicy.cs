using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchTransactionClassificationPolicy
{
    AchTransactionClassificationResult Classify(AchTransactionClassificationRequest request);
}
