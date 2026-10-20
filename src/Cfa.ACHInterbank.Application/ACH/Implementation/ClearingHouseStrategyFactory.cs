using Cfa.ACHInterbank.Application.ACH.Interfaces;

namespace Cfa.ACHInterbank.Application.ACH.Implementation;

public class ClearingHouseStrategyFactory(ICycleNumberResolver cycleNumberResolver) : IClearingHouseStrategyFactory
{
    private readonly ICycleNumberResolver _cycleNumberResolver = cycleNumberResolver ?? throw new ArgumentNullException(nameof(cycleNumberResolver));
    public IClearingHouseStrategy GetStrategy(int clearingHouseId)
    {
        return clearingHouseId switch
        {
            1 => new AchClearingHouseStrategy(_cycleNumberResolver),
            2 => new CenitClearingHouseStrategy(_cycleNumberResolver),
            _ => throw new ArgumentException("Unsupported clearing house"),
        };
    }
}

