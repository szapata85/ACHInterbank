using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IClearingHouseStrategy
{
    bool ValidateTransaction(AchTransaction transaction);
    byte[] GenerateCycleFile(AchCycle cycle);
}

