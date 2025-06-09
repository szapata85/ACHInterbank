using Cfa.ACHInterbank.Application.ACH.Interfaces;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.StrategyImplementation;

public class HolidayStrategyFactory : IHolidayStrategyFactory
{
    public IHolidayStrategy GetStrategyForClearingHouse(int clearingHouseId)
    {
        // Future logic: select strategy based on clearingHouseId, region, or other config
        return new ColombianHolidayStrategy();
    }
}
