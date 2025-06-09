namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IHolidayStrategyFactory
{
    IHolidayStrategy GetStrategyForClearingHouse(int clearingHouseId);
}
