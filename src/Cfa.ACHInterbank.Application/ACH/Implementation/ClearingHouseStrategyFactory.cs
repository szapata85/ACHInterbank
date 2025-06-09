using Cfa.ACHInterbank.Application.ACH.Interfaces;

namespace Cfa.ACHInterbank.Application.ACH.Implementation;

public class ClearingHouseStrategyFactory : IClearingHouseStrategyFactory
{
    public IClearingHouseStrategy GetStrategy(int clearingHouseId)
    {
        return clearingHouseId switch
        {
            1 => new AchClearingHouseStrategy(),
            2 => new CenitClearingHouseStrategy(),
            _ => throw new ArgumentException("Unsupported clearing house"),
        };
    }
}

